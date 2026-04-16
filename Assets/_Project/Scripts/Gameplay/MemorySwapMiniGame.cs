using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RTLTMPro;

/// <summary>
/// PHASE 17: Memory Swap Mini-Game - Tile Matching Game.
/// 
/// GAMEPLAY:
/// - Player flips two tiles at a time to find matching pairs
/// - All tiles briefly revealed at start for memorization
/// - Tap tiles to flip and find matches
/// - Each match awards Tech Scrap (3 per match + 10 bonus for perfect game)
/// - Hint button reveals all unmatched tiles (10s cooldown)
/// 
/// INTEGRATION:
/// - Works as a mini-game slot in MiniGameManager
/// - Awards Tech Scrap via SaveManager
/// - Uses Unity UI Buttons (touch-compatible by default)
/// - DOTween for flip animations (project standard)
/// 
/// SETUP INSTRUCTIONS:
/// 1. Assign tile sprites in Inspector (_tiles array)
/// 2. Configure grid layout (default: 6 pairs = 12 tiles in 3x4)
/// 3. Assign UI references (ScoreText, HintButton, Grid container)
/// 4. Add to MiniGameManager inspector slot assignment
/// </summary>
public class MemorySwapMiniGame : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Singleton instance for external access.
    /// </summary>
    public static MemorySwapMiniGame Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // Debug.LogWarning("[MemorySwapMiniGame] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    #endregion

    [Header("Tiles/Grid References")]
    [Tooltip("Grid container to spawn tiles under")]
    [SerializeField] private GameObject Grid;

    [Tooltip("Tile button prefab (must have TileValue component)")]
    [SerializeField] private Button _tilePrefab;

    [Tooltip("Array of tile sprites (each appears twice)")]
    [SerializeField] private Sprite[] _tiles;

    [Header("Durations")]
    [Tooltip("How long tiles are shown during initial reveal and hints")]
    [SerializeField] private float _revealDuration = 1.5f;

    [Tooltip("Pause duration between flip operations")]
    [SerializeField] private float _delay = 0.3f;

    [Tooltip("Time for flip animation")]
    [SerializeField] private float _flipTime = 0.2f;

    [Header("UI References")]
    [Tooltip("Hint button (optional - can leave unassigned to disable)")]
    [SerializeField] private Button hintButton;

    [Tooltip("Score display text")]
    [SerializeField] private RTLTextMeshPro _scoreText;

    [Header("Scoring")]
    [Tooltip("Tech Scrap earned per correct match")]
    [SerializeField] private int techScrapPerMatch = 3;

    [Tooltip("Bonus Tech Scrap for completing all pairs")]
    [SerializeField] private int bonusScrapForPerfect = 10;

    // === PRIVATE FIELDS ===

    private GameObject firstButton, secondButton;
    private TileValue firstTileValue, secondTileValue;
    private bool lockInput;
    private int _score = 0;
    private int _matchCount = 0;
    private int _totalPairs = 0;

    // Track all tiles for hint system
    private List<GameObject> _allTiles = new List<GameObject>();

    // Track awarded scrap for this game
    private int _totalScrapAwarded = 0;

    #region Unity Lifecycle

    private void OnEnable()
    {
        // Re-enable hint button if assigned
        if (hintButton != null)
            hintButton.interactable = true;
    }

    void Start()
    {
        // Validate references
        if (Grid == null || _tilePrefab == null || _tiles == null || _tiles.Length == 0)
        {
            // Debug.LogError("[MemorySwapMiniGame] Missing required references! Check Inspector.");
            EndGameEarly();
            return;
        }

        _totalPairs = _tiles.Length;
        _score = 0;
        _matchCount = 0;
        _totalScrapAwarded = 0;
        UpdateScoreText();

        // Debug.Log($"[MemorySwapMiniGame] Starting game with {_totalPairs} pairs ({_totalPairs * 2} tiles)");

        // Auto-configure canvas with main camera
        ConfigureCanvas();

        InitializeGrid();
    }

    /// <summary>
    /// Auto-configure canvas to use main camera for proper rendering.
    /// Follows same pattern as PathDrawingGame and CatchMiniGame.
    /// </summary>
    private void ConfigureCanvas()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;
        }

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            // Debug.LogError("[MemorySwapMiniGame] No Canvas found on this GameObject or children!");
            return;
        }

        if (mainCam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 100f;

            // Debug.Log($"[MemorySwapMiniGame] Canvas configured with camera: {mainCam.gameObject.name}");
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Debug.LogWarning("[MemorySwapMiniGame] No camera found! Using ScreenSpaceOverlay.");
        }

        // Fix RectTransform to fill screen
        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
        }
    }

    #endregion

    #region Grid Initialization

    /// <summary>
    /// Initialize the grid with shuffled tile pairs.
    /// </summary>
    private void InitializeGrid()
    {
        int overallTiles = _tiles.Length * 2;

        // 1. Create list with each sprite index appearing twice
        List<int> tileValues = new List<int>(overallTiles);
        for (int i = 0; i < _tiles.Length; i++)
        {
            tileValues.Add(i);
            tileValues.Add(i);
        }

        // 2. Shuffle using Fisher-Yates algorithm
        ShuffleList(tileValues);

        // Clear hint button listener (prevent duplicates)
        if (hintButton != null)
            hintButton.onClick.RemoveAllListeners();

        _allTiles.Clear();

        // 3. Spawn tiles with shuffled values
        for (int i = 0; i < overallTiles; i++)
        {
            GameObject tile = Instantiate(_tilePrefab.gameObject, Grid.transform);
            _allTiles.Add(tile);

            var btn = tile.GetComponent<Button>();
            var tv = tile.GetComponent<TileValue>();
            var frontImage = tile.transform.GetChild(0).GetComponent<Image>();

            int spriteIndex = tileValues[i]; // Shuffled index

            // Set sprite and value
            frontImage.sprite = _tiles[spriteIndex];
            tv.SetValue(spriteIndex);

            // Add click listener
            btn.onClick.AddListener(() => OnTileClicked(tile, tv));

            // Start with initial reveal animation
            StartCoroutine(InitialTileReveal(tile));
        }

        // Add hint button listener
        if (hintButton != null)
        {
            hintButton.onClick.AddListener(() => StartCoroutine(HintReveal()));
        }
    }

    /// <summary>
    /// Fisher-Yates shuffle algorithm.
    /// </summary>
    private void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    #endregion

    #region Tile Interaction

    /// <summary>
    /// Handle tile click - flip and check for matches.
    /// </summary>
    public void OnTileClicked(GameObject tile, TileValue tileValue)
    {
        if (lockInput) return;

        var back = tile.transform.GetChild(1).gameObject;
        if (!back.activeSelf) return; // Already face-up, ignore

        var btn = tile.GetComponent<Button>();
        if (btn != null && !btn.interactable) return;

        // Disable button temporarily during flip
        if (btn) btn.interactable = false;

        var openSeq = FlipOpen(tile);
        openSeq.OnComplete(() =>
        {
            // Re-enable if still unmatched (back is now hidden)
            if (btn && btn.interactable == false && tile.transform.GetChild(1).gameObject.activeSelf == false)
                btn.interactable = true;
        });

        // First pick
        if (firstTileValue == null)
        {
            firstButton = tile;
            firstTileValue = tileValue;
            return;
        }

        // Prevent selecting the same tile twice
        if (tile == firstButton) return;

        // Second pick - check for match
        secondButton = tile;
        secondTileValue = tileValue;

        // Lock input and resolve pair
        lockInput = true;
        StartCoroutine(ResolvePair(openSeq));
    }

    #endregion

    #region Pair Resolution

    /// <summary>
    /// Resolve a pair of flipped tiles - check for match.
    /// </summary>
    private IEnumerator ResolvePair(Sequence secondOpen)
    {
        // Wait for second flip to complete
        yield return secondOpen.WaitForCompletion();

        // Small pause so player sees both tiles
        yield return new WaitForSeconds(_delay);

        // Check if values match
        if (firstTileValue.Value == secondTileValue.Value)
        {
            // MATCH! - Award scrap and disable buttons
            firstButton.GetComponent<Button>().interactable = false;
            secondButton.GetComponent<Button>().interactable = false;

            _matchCount++;
            _score += 10; // Visual score (for display)
            UpdateScoreText();

            // Award Tech Scrap immediately
            AwardScrap(techScrapPerMatch);

            // Debug.Log($"[MemorySwapMiniGame] Match found! ({_matchCount}/{_totalPairs}) +{techScrapPerMatch} scrap");

            // Check if all pairs matched
            if (_matchCount == _totalPairs)
            {
                // Debug.Log("[MemorySwapMiniGame] All pairs matched! Perfect game bonus!");
                AwardScrap(bonusScrapForPerfect);
                yield return new WaitForSeconds(0.5f);
                CompleteGame();
            }
        }
        else
        {
            // NO MATCH - Flip both back
            var s1 = FlipClose(firstButton);
            var s2 = FlipClose(secondButton);
            yield return DOTween.Sequence().Join(s1).Join(s2).WaitForCompletion();
        }

        // Reset selections
        firstButton = secondButton = null;
        firstTileValue = secondTileValue = null;
        lockInput = false;

        yield return new WaitForSeconds(_delay);
    }

    /// <summary>
    /// Award Tech Scrap and persist to SaveManager.
    /// </summary>
    private void AwardScrap(int amount)
    {
        if (amount <= 0) return;

        _totalScrapAwarded += amount;

        // Persist to SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AddScrap(amount);
            // Debug.Log($"[MemorySwapMiniGame] Awarded {amount} Tech Scrap (total: {_totalScrapAwarded})");
        }
        else
        {
            // Debug.LogWarning("[MemorySwapMiniGame] SaveManager.Instance is null! Scrap not persisted.");
        }
    }

    #endregion

    #region Hint System

    /// <summary>
    /// Reveal all unmatched covered tiles briefly (hint feature).
    /// </summary>
    private IEnumerator HintReveal()
    {
        if (lockInput) yield break;
        if (hintButton == null) yield break;

        hintButton.interactable = false;
        lockInput = true;

        // Debug.Log("[MemorySwapMiniGame] Hint activated!");

        // 1. Open all unmatched + covered tiles together
        var openSeq = DOTween.Sequence();
        foreach (var t in _allTiles)
        {
            var btn = t.GetComponent<Button>();
            var back = t.transform.GetChild(1).gameObject;

            // Only show tiles that are covered and interactable
            if (btn != null && btn.interactable && back.activeSelf)
                openSeq.Join(FlipOpen(t));
        }
        yield return openSeq.WaitForCompletion();

        // 2. Hold for preview
        yield return new WaitForSeconds(_revealDuration);

        // 3. Close only those unmatched tiles that were covered before
        var closeSeq = DOTween.Sequence();
        foreach (var t in _allTiles)
        {
            var btn = t.GetComponent<Button>();
            var back = t.transform.GetChild(1).gameObject;

            // Close tiles that are now face-up but unmatched
            if (btn != null && btn.interactable && back.activeSelf == false)
                closeSeq.Join(FlipClose(t));
        }
        yield return closeSeq.WaitForCompletion();

        lockInput = false;

        // 4. Cooldown before hint usable again
        yield return new WaitForSeconds(10f);

        if (hintButton != null)
            hintButton.interactable = true;
    }

    #endregion

    #region Initial Tile Reveal

    /// <summary>
    /// Show initial tile reveal at game start (memorization phase).
    /// </summary>
    private IEnumerator InitialTileReveal(GameObject tile)
    {
        // Start covered
        tile.transform.GetChild(1).gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);

        // Open (show sprite)
        yield return FlipOpen(tile).WaitForCompletion();

        // Pause for memorization
        yield return new WaitForSeconds(_revealDuration);

        // Close (show back again)
        yield return FlipClose(tile).WaitForCompletion();
    }

    #endregion

    #region Flip Animations (DOTween)

    /// <summary>
    /// Flip tile to show front (hide back panel).
    /// </summary>
    private Sequence FlipOpen(GameObject tile)
    {
        var t = tile.transform;
        t.DOKill();
        t.localEulerAngles = Vector3.zero;

        var seq = DOTween.Sequence();
        seq.Append(t.DOLocalRotate(new Vector3(0, 90, 0), _flipTime));
        seq.AppendCallback(() => tile.transform.GetChild(1).gameObject.SetActive(false)); // Hide BACK
        seq.Append(t.DOLocalRotate(Vector3.zero, _flipTime));

        return seq;
    }

    /// <summary>
    /// Flip tile to show back (cover front).
    /// </summary>
    private Sequence FlipClose(GameObject tile)
    {
        var t = tile.transform;
        t.DOKill();
        t.localEulerAngles = Vector3.zero;

        var seq = DOTween.Sequence();
        seq.Append(t.DOLocalRotate(new Vector3(0, 90, 0), _flipTime));
        seq.AppendCallback(() => tile.transform.GetChild(1).gameObject.SetActive(true)); // Show BACK
        seq.Append(t.DOLocalRotate(Vector3.zero, _flipTime));

        return seq;
    }

    #endregion

    #region Score & Completion

    /// <summary>
    /// Update score display text.
    /// </summary>
    private void UpdateScoreText()
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"{_score} ({_matchCount}/{_totalPairs})";
        }
    }

    /// <summary>
    /// Complete the mini-game and notify MiniGameManager.
    /// </summary>
    private void CompleteGame()
    {
        // Debug.Log($"[MemorySwapMiniGame] Game complete! Total scrap awarded: {_totalScrapAwarded}");

        // Notify MiniGameManager to transition to next state
        if (MiniGameManager.Instance != null)
        {
            // Pass 0 for eidia (memory game doesn't award eidia, only scrap)
            // Pass 0 for scrap (already persisted per-match)
            MiniGameManager.Instance.EndMiniGame(0, 0);
            // Debug.Log("[MemorySwapMiniGame] Notified MiniGameManager.EndMiniGame(0, 0)");
        }
        else
        {
            // Debug.LogError("[MemorySwapMiniGame] MiniGameManager.Instance is null! Cannot transition!");
            // Fallback: destroy ourselves to at least clean up
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// End game early (error state or manual cancel).
    /// </summary>
    private void EndGameEarly()
    {
        // Debug.LogWarning("[MemorySwapMiniGame] Ending game early due to error.");

        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.EndMiniGame(0, 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Cleanup: kill all tweens when disabled.
    /// </summary>
    private void OnDisable()
    {
        // Kill all tile tweens
        foreach (var tile in _allTiles)
        {
            if (tile != null)
                tile.transform.DOKill();
        }
        
        // Kill any remaining sequences
        DOTween.Kill(this);
    }

    #endregion
}
