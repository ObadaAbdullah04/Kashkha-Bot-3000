using System;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Manages the inter-house arcade mini-game (Eidia Catch).
/// Uses a prefab sandbox approach to keep the single-scene architecture clean.
/// 
/// Task 1: MiniGameManager is the SINGLE SOURCE OF TRUTH for game duration.
/// - Determines duration based on house level
/// - Calls CatchMiniGame.Initialize() to start the game
/// - CatchMiniGame cannot auto-start on its own
/// </summary>
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [Header("Catch Game Prefab")]
    [Tooltip("The instantiated prefab that contains the CatchMiniGame component")]
    [SerializeField] private GameObject catchGamePrefab;

    [Header("Path Drawing Prefab (NEW - Phase 5C)")]
    [Tooltip("The instantiated prefab for Path-Drawing Maze mini-game")]
    [SerializeField] private GameObject pathDrawingPrefab;

    [Header("Memory Swap Prefab (PHASE 17)")]
    [Tooltip("The instantiated prefab for Memory Swap tile matching game")]
    [SerializeField] private GameObject memorySwapPrefab;

    [Header("Mini-Game Assignment (PHASE 6 UPDATE)")]
    [Tooltip("Mini-game for slot 1 (between House 1 and 2)")]
    [SerializeField] private MiniGameType miniGameSlot1 = MiniGameType.CatchGame;

    [Tooltip("Mini-game for slot 2 (between House 2 and 3)")]
    [SerializeField] private MiniGameType miniGameSlot2 = MiniGameType.PathDrawing;

    [Tooltip("Mini-game for slot 3 (between House 3 and 4)")]
    [SerializeField] private MiniGameType miniGameSlot3 = MiniGameType.CatchGame;

    [Header("Mini-Game Scaling")]
    [Tooltip("Base duration for Catch game (House 1)")]
    [SerializeField] private float baseCatchDuration = 10f;
    [Tooltip("Multiplier for Catch game duration per level (e.g. 1.2 = +2s each house)")]
    [SerializeField] private float catchDurationMultiplier = 1.2f;

    [Space]
    [Tooltip("Base time limit for Path Drawing (House 1)")]
    [SerializeField] private float basePathTime = 35f;
    [Tooltip("Multiplier for Path Drawing difficulty (less time = harder)")]
    [SerializeField] private float pathTimeMultiplier = 0.85f;

    private GameObject _activeMiniGameInstance;
    private string _pendingInstruction;

    #region Events

    /// <summary>
    /// Fires when mini-game ends with rewards. Used by FloatingTextManager.
    /// </summary>
    public static Action<int, int> OnMiniGameEnded;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            OnMiniGameEnded = null;
            Instance = null;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets the assigned mini-game type for a slot (0-2).
    /// </summary>
    public MiniGameType GetMiniGameTypeForSlot(int slotIndex)
    {
        return slotIndex switch
        {
            0 => miniGameSlot1,
            1 => miniGameSlot2,
            2 => miniGameSlot3,
            _ => MiniGameType.CatchGame
        };
    }

    /// <summary>
    /// Starts the mini-game assigned to the specified slot (0-2).
    /// Slot 0 = Between House 1-2, Slot 1 = Between House 2-3, Slot 2 = Between House 3-4
    /// </summary>
    public void StartAssignedMiniGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2)
        {
            return;
        }

        MiniGameType gameType = slotIndex switch
        {
            0 => miniGameSlot1,
            1 => miniGameSlot2,
            2 => miniGameSlot3,
            _ => MiniGameType.CatchGame
        };

        int houseLevel = slotIndex + 1;
        StartMiniGame(gameType, houseLevel);
    }

    /// <summary>
    /// Instantiates a specific mini-game type with house-based difficulty scaling.
    /// Instructions are delayed to synchronize with the transition fade-out.
    /// </summary>
    public void StartMiniGame(MiniGameType type, int houseLevel)
    {
        string instruction = type switch
        {
            MiniGameType.CatchGame => "امسك العيدية! وتجنب المعمول!",
            MiniGameType.PathDrawing => "ارسم المسار للهدف!",
            MiniGameType.MemorySwap => "طابق الصور المتشابهة!",
            _ => "العب واربح!"
        };

        _pendingInstruction = instruction;
        Invoke(nameof(ShowDelayedInstruction), 2.5f);

        switch (type)
        {
            case MiniGameType.CatchGame:
                StartCatchGame(houseLevel);
                break;

            case MiniGameType.PathDrawing:
                StartPathDrawingGame(houseLevel);
                break;

            case MiniGameType.MemorySwap:
                StartMemorySwapGame(houseLevel);
                break;

            default:
                StartCatchGame(houseLevel);
                break;
        }
    }

    /// <summary>
    /// Spawns and initializes the Eidia Catch mini-game.
    /// </summary>
    public void StartCatchGame(int houseLevel)
    {
        if (catchGamePrefab == null)
        {
            FallbackToNextHouse();
            return;
        }

        float duration = baseCatchDuration * Mathf.Pow(catchDurationMultiplier, houseLevel - 1);
        _activeMiniGameInstance = InstantiateMiniGamePrefab(catchGamePrefab, houseLevel);

        CatchMiniGame catchGame = _activeMiniGameInstance.GetComponent<CatchMiniGame>();
        if (catchGame != null)
        {
            catchGame.Initialize(duration);
        }
    }

    /// <summary>
    /// Spawns and initializes the Path-Drawing Maze mini-game.
    /// </summary>
    public void StartPathDrawingGame(int houseLevel)
    {
        if (pathDrawingPrefab == null)
        {
            StartCatchGame(houseLevel);
            return;
        }

        float timeLimit = basePathTime * Mathf.Pow(pathTimeMultiplier, houseLevel - 1);
        _activeMiniGameInstance = InstantiateMiniGamePrefab(pathDrawingPrefab, houseLevel);

        PathDrawingGame pathGame = _activeMiniGameInstance.GetComponent<PathDrawingGame>();
        if (pathGame != null)
        {
            pathGame.Initialize(timeLimit);
        }
    }

    /// <summary>
    /// Spawns and initializes the Memory Swap matching mini-game.
    /// </summary>
    public void StartMemorySwapGame(int houseLevel)
    {
        if (memorySwapPrefab == null)
        {
            FallbackToNextHouse();
            return;
        }

        _activeMiniGameInstance = InstantiateMiniGamePrefab(memorySwapPrefab, houseLevel);
    }

    /// <summary>
    /// Finalizes the current mini-game, awarding accumulated rewards and cleaning up the instance.
    /// </summary>
    public void EndMiniGame(int eidiaEarned, int scrapEarned)
    {
        if (scrapEarned > 0 && SaveManager.Instance != null)
        {
            SaveManager.Instance.AddScrap(scrapEarned);
        }

        OnMiniGameEnded?.Invoke(eidiaEarned, scrapEarned);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMiniGameComplete(eidiaEarned);
        }
        else
        {
            CleanupActiveMiniGame();
        }
    }

    /// <summary>
    /// Destroys the active mini-game instance.
    /// </summary>
    public void CleanupActiveMiniGame()
    {
        if (_activeMiniGameInstance != null)
        {
            Destroy(_activeMiniGameInstance);
            _activeMiniGameInstance = null;
        }
    }

    #endregion

    #region Helper Methods

    private void ShowDelayedInstruction()
    {
        if (!string.IsNullOrEmpty(_pendingInstruction))
        {
            UIManager.Instance?.ShowInstruction(_pendingInstruction);
            _pendingInstruction = null;
        }
    }

    private GameObject InstantiateMiniGamePrefab(GameObject prefab, int houseLevel)
    {
        var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        ConfigureCanvasForInstance(instance);
        BumpSortingOrders(instance);

        var bgLoader = instance.GetComponentInChildren<MiniGameBackgroundLoader>();
        if (bgLoader != null)
        {
            bgLoader.Initialize(houseLevel);
        }

        return instance;
    }

    private void BumpSortingOrders(GameObject instance)
    {
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingOrder += 50;
        }
    }

    private void ConfigureCanvasForInstance(GameObject instance)
    {
        Transform t = instance.transform;
        t.SetParent(null);
        t.localScale = Vector3.one;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        Canvas canvas = instance.GetComponentInChildren<Canvas>();
        if (canvas == null) return;

        Camera mainCam = GetMainCamera();
        canvas.renderMode = mainCam != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        if (mainCam != null)
        {
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 100f;
        }
        canvas.sortingOrder = 60; 

        var rect = canvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    private Camera GetMainCamera()
    {
        return Camera.main ?? Camera.allCameras.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? Camera.allCameras.FirstOrDefault(c => c.enabled);
    }

    private void FallbackToNextHouse()
    {
        GameManager.Instance?.OnMiniGameComplete(0);
    }

    #endregion

    #region Debug Buttons

    [Button("Test Catch Game (House 1)")]
    private void TestCatchGame1() => StartCatchGame(1);

    [Button("🗺️ Test Path Game (House 1)")]
    private void TestPathGame1() => StartPathDrawingGame(1);

    [Button("🧠 Test Memory Swap Game")]
    private void TestMemorySwapGame() => StartMemorySwapGame(1);

    #endregion
}