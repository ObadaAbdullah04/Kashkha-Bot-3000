using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Catch Mini-Game Manager - WORLD SPACE EDITION.
///
/// ARCHITECTURE PIVOT:
/// - Player and Items are standard 2D GameObjects (Transform, SpriteRenderer)
/// - UI Canvas is ONLY for Score and Timer text overlays
/// - World space movement eliminates all UI input bleed and anchor issues
/// - Screen Halves touch input + Keyboard fallback
/// - Input Action assigned via Inspector
/// </summary>
public class CatchMiniGame : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Singleton instance for FallingItem scripts to call.
    /// </summary>
    public static CatchMiniGame Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // Debug.LogWarning("[CatchMiniGame] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Validate input action assignment
        if (moveAction == null || moveAction.action == null)
        {
            // Debug.LogWarning("[CatchMiniGame] moveAction not assigned! Please assign MoveHorizontal from DeviceControls in Inspector.");
        }
    }

    #endregion

    [Header("Movement Settings")]
    [Tooltip("Player basket horizontal movement speed (world units per second)")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Input")]
    [Tooltip("Input Action for horizontal movement (assign MoveHorizontal from DeviceControls)")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Spawner Settings")]
    [Tooltip("Time between item spawns (lower = more items)")]
    [SerializeField] private float spawnInterval = 0.8f;

    [Tooltip("Chance to spawn Eidia (good item) vs Ma'amoul (bad item)")]
    [Range(0f, 1f)]
    [SerializeField] private float eidiaSpawnChance = 0.75f;

    [Header("World Space References")]
    [Tooltip("Player basket prefab to instantiate at runtime (standard 2D sprite, NOT UI)")]
    [SerializeField] private GameObject playerBasketPrefab;

    [Tooltip("Y position for player basket in world space")]
    [SerializeField] private float _playerY = -3f;

    [Tooltip("Parent transform for spawned items (empty GameObject)")]
    [SerializeField] private Transform itemsParent;

    [Tooltip("Falling item prefab (Eidia - standard 2D sprite)")]
    [SerializeField] private GameObject fallingItemPrefab;

    [Tooltip("Falling item prefab (Ma'amoul - standard 2D sprite)")]
    [SerializeField] private GameObject fallingBadItemPrefab;

    [Tooltip("Second falling item prefab (Ma'amoul variant - standard 2D sprite)")]
    [SerializeField] private GameObject fallingBadItemPrefab2;

    [Header("Feedback Settings")]
    [SerializeField] private Vector3 catchPunchScale = new Vector3(0.2f, 0.2f, 1f);
    [SerializeField] private float catchPunchDuration = 0.3f;
    [SerializeField] private float avoidShakeDuration = 0.3f;
    [SerializeField] private float avoidShakeStrength = 0.3f;
    [SerializeField] private int avoidShakeVibrato = 22;
    [SerializeField] private float avoidShakeRandomness = 90f;

    [Header("Reward Balancing")]
    [Tooltip("Scrap earned per score point (e.g., 0.5 = 1 scrap per 2 points)")]
    [SerializeField] private float scrapPerPoint = 0.5f;

    // World space references (set at runtime)
    private Transform playerBasket;

    [Header("UI Overlays (Text Only)")]
    [SerializeField] private RTLTMPro.RTLTextMeshPro timerText;
    [SerializeField] private RTLTMPro.RTLTextMeshPro scoreText;

    // World space boundaries (calculated at runtime)
    private float _minX;
    private float _maxX;
    private float _spawnY;
    private float _destroyY;

    // Game state - TIME ATTACK
    private float _timeRemaining;
    private int _lastCachedSecond = -1; // NEW: Cache for timer text
    private int _score = 0;
    private bool _isPlaying = false;
    private float _spawnTimer = 0f;

    // Active items tracking (for cleanup only)
    private List<Transform> _activeItems = new List<Transform>();

    // Track spawned player basket for cleanup
    private GameObject _spawnedPlayerBasket;
    private SpriteRenderer _playerSpriteRenderer;

    /// <summary>
    /// Enable input action when component is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    /// <summary>
    /// Clean up all DOTween sequences, disable input, and destroy spawned objects.
    /// </summary>
    private void OnDisable()
    {
        // Disable input action
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }

        // Kill all tweens on playerBasket
        if (playerBasket != null)
        {
            playerBasket.DOKill();
        }

        // Kill all tweens on active items
        foreach (var item in _activeItems)
        {
            if (item != null)
                item.DOKill();
        }

        // Destroy spawned player basket
        if (_spawnedPlayerBasket != null)
        {
            Destroy(_spawnedPlayerBasket);
        }

        // Clear tracking list to prevent stale references
        _activeItems.Clear();
    }

    private void Start()
    {
        // === DIAGNOSTIC LOGGER ===
#if UNITY_EDITOR
        // Debug.Log("=== [CatchMiniGame] WORLD SPACE SETUP ===");
        // Debug.Log($"[CatchMiniGame] playerBasketPrefab: {(playerBasketPrefab != null ? "ASSIGNED" : "NULL!")}");
        // Debug.Log($"[CatchMiniGame] itemsParent: {(itemsParent != null ? "ASSIGNED" : "NULL!")}");
        // Debug.Log($"[CatchMiniGame] fallingItemPrefab: {(fallingItemPrefab != null ? "ASSIGNED" : "NULL!")}");
        // Debug.Log($"[CatchMiniGame] fallingBadItemPrefab: {(fallingBadItemPrefab != null ? "ASSIGNED" : "NULL!")}");
        // Debug.Log($"[CatchMiniGame] moveAction: {(moveAction != null ? "ASSIGNED" : "NULL! Assign in Inspector!")}");
        // Debug.Log($"[CatchMiniGame] _playerY: {_playerY}");

        // Debug.Log($"[CatchMiniGame] Spawn Interval: {spawnInterval}s | Eidia Chance: {eidiaSpawnChance * 100:F0}%");
        // Debug.Log("========================================");
#endif

        // Calculate world space boundaries from camera
        CalculateWorldBoundaries();

#if UNITY_EDITOR
        // Debug.Log("[CatchMiniGame] Waiting for Initialize() call from MiniGameManager...");
#endif
    }

    /// <summary>
    /// Task 1: Calculate world space boundaries using Camera viewport.
    /// </summary>
    private void CalculateWorldBoundaries()
    {
        if (Camera.main == null)
        {
            // Debug.LogError("[CatchMiniGame] Main Camera not found! Using fallback values.");
            _minX = -4f;
            _maxX = 4f;
            _spawnY = 6f;
            _destroyY = -7f;
            return;
        }

        // Calculate screen edges in world space
        Vector3 leftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        Vector3 rightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane));

        // Get player sprite width for padding (from prefab if basket not spawned yet)
        float playerHalfWidth = 0.5f; // Default fallback
        SpriteRenderer sr = null;

        if (playerBasket != null)
        {
            sr = playerBasket.GetComponent<SpriteRenderer>();
        }
        else if (playerBasketPrefab != null)
        {
            sr = playerBasketPrefab.GetComponent<SpriteRenderer>();
        }

        if (sr != null && sr.sprite != null)
        {
            playerHalfWidth = sr.sprite.bounds.extents.x;
        }

        // Set boundaries with padding (so player doesn't go off-screen)
        _minX = leftEdge.x + playerHalfWidth;
        _maxX = rightEdge.x - playerHalfWidth;

        // Spawn Y: just above the top of the screen (1.1 = 10% above viewport)
        _spawnY = Camera.main.ViewportToWorldPoint(new Vector3(0, 1.1f, 0)).y;

        // Destroy Y: just below the bottom of the screen
        _destroyY = Camera.main.ViewportToWorldPoint(new Vector3(0, -0.1f, 0)).y;

#if UNITY_EDITOR
        // Debug.Log($"[CatchMiniGame] World Boundaries: minX={_minX:F2}, maxX={_maxX:F2}, spawnY={_spawnY:F2}, destroyY={_destroyY:F2}");
#endif
    }

    /// <summary>
    /// Initialize the mini-game with duration from MiniGameManager.
    /// Spawns the player basket prefab in world space.
    /// </summary>
    public void Initialize(float gameDuration)
    {
        _timeRemaining = gameDuration;
        _score = 0;
        _isPlaying = true;
        _spawnTimer = 0f;
        _activeItems.Clear();

        // Spawn the basket in the WORLD, not the canvas!
        if (playerBasketPrefab != null)
        {
            GameObject basketGo = Instantiate(playerBasketPrefab, new Vector3(0, _playerY, 0), Quaternion.identity);
            playerBasket = basketGo.transform;
            _spawnedPlayerBasket = basketGo;
            _playerSpriteRenderer = basketGo.GetComponentInChildren<SpriteRenderer>();
#if UNITY_EDITOR
            // Debug.Log($"[CatchMiniGame] PlayerBasket spawned at (0, {_playerY}, 0)");
#endif
        }
        else
        {
            // Debug.LogError("[CatchMiniGame] playerBasketPrefab not assigned!");
        }

        if (scoreText != null)
            scoreText.text = "0";

#if UNITY_EDITOR
        // Debug.Log($"[CatchMiniGame] TIME ATTACK STARTED! Duration: {gameDuration}s");
#endif
    }

    /// <summary>
    /// Main update loop - handles timer, spawning, and item movement.
    /// </summary>
    private void Update()
    {
        if (!_isPlaying) return;

        // === TIME ATTACK TIMER ===
        _timeRemaining -= Time.deltaTime;

        // Update UI timer (CACHED to reduce GC)
        int currentSecond = Mathf.CeilToInt(_timeRemaining);
        if (currentSecond != _lastCachedSecond)
        {
            _lastCachedSecond = currentSecond;
            if (timerText != null)
                timerText.text = _lastCachedSecond.ToString();
        }

        // === SPAWNER LOOP ===
        if (_timeRemaining > 0f)
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnItem();
            }
        }
        else
        {
            // Time's up!
            _timeRemaining = 0f;
            EndGame();
            return;
        }

        // === PLAYER MOVEMENT ===
        HandlePlayerMovement();

        // === UPDATE FALLING ITEMS ===
        UpdateFallingItems();
    }

    /// <summary>
    /// Smooth World Space Movement using New Input System + Touch override.
    /// Fixed: No more snapping - direct position application with clamped input.
    /// </summary>
    private void HandlePlayerMovement()
    {
        if (playerBasket == null) return;
        if (moveAction == null || moveAction.action == null) return;

        // Read input from New Input System (consistent Vector2 read avoids stale fallback values)
        float moveInput = moveAction.action.ReadValue<Vector2>().x;

        // === MOBILE TOUCH OVERRIDE (Screen Halves) ===
        // Check if touch is actually pressed (not just touchscreen exists)
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            // Read touch pixel position
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();

            // Screen halves: left half = -1, right half = +1
            if (touchPos.x < Screen.width / 2f)
            {
                moveInput = -1f;
            }
            else
            {
                moveInput = 1f;
            }
        }

        // Clamp input to prevent overflow (-1 to 1)
        moveInput = Mathf.Clamp(moveInput, -1f, 1f);

        // === SPRITE FLIPPING ===
        if (_playerSpriteRenderer != null)
        {
            if (moveInput > 0.01f) _playerSpriteRenderer.flipX = false; // Face Right
            else if (moveInput < -0.01f) _playerSpriteRenderer.flipX = true; // Face Left (flipped)
        }

        // Early exit if no input
        if (moveInput == 0f) return;

        // Calculate new X position by adding to current position
        float newX = playerBasket.position.x + (moveInput * moveSpeed * Time.deltaTime);

        // Clamp to world boundaries
        newX = Mathf.Clamp(newX, _minX, _maxX);

        // Apply position (preserve Y and Z)
        playerBasket.position = new Vector3(newX, playerBasket.position.y, playerBasket.position.z);
    }

    /// <summary>
    /// Task 2: Spawn items in World Space.
    private void SpawnItem()
    {
        if (fallingItemPrefab == null && fallingBadItemPrefab == null)
        {
            // Debug.LogWarning("[CatchMiniGame] No item prefabs assigned!");
            return;
        }

        // Random chance: 75% Eidia (good), 25% Ma'amoul (bad)
        bool isEidia = Random.value < eidiaSpawnChance;
        GameObject prefabToSpawn = null;

        if (isEidia)
        {
            prefabToSpawn = fallingItemPrefab;
        }
        else
        {
            // Pick randomly between the two bad item prefabs
            if (fallingBadItemPrefab2 != null)
            {
                float rand = Random.value;
                prefabToSpawn = rand < 0.5f ? fallingBadItemPrefab : fallingBadItemPrefab2;
                
                #if UNITY_EDITOR
                // // Debug.Log($"[CatchMiniGame] Selected Bad Prefab: {(rand < 0.5f ? "1" : "2")}");
                #endif
            }
            else
            {
                prefabToSpawn = fallingBadItemPrefab;
            }
        }

        if (prefabToSpawn == null)
        {
            // Debug.LogWarning($"[CatchMiniGame] Prefab to spawn is null! isEidia={isEidia}");
            return;
        }

        #if UNITY_EDITOR
        // // Debug.Log($"[CatchMiniGame] Spawning: {prefabToSpawn.name}");
        #endif

        // Random X between boundaries
        float randomX = Random.Range(_minX, _maxX);

        // Spawn position in world space
        Vector3 spawnPos = new Vector3(randomX, _spawnY, 0f);

        // Instantiate in world space
        GameObject newItem = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        
        // Ensure Z is exactly 0 and sorting order is high for visibility
        newItem.transform.position = new Vector3(randomX, _spawnY, 0f);
        SpriteRenderer sr = newItem.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100; // Items well above background
        }

        // JUICE: Scale up animation (preserve prefab's original scale)
        Vector3 originalScale = newItem.transform.localScale;
        newItem.transform.localScale = Vector3.zero;
        newItem.transform.DOScale(originalScale, 0.3f).SetEase(Ease.OutBack);

        // NOTE: Items stay in world space (no parent) to prevent Canvas transform issues.
        // If you need organization, create a world-space empty GameObject as container.

        // Track for cleanup
        _activeItems.Add(newItem.transform);

#if UNITY_EDITOR
        // Debug.Log($"[CatchMiniGame] Spawned {(isEidia ? "Eidia" : "Ma'amoul")} at X={randomX:F2}");
#endif
    }

    /// <summary>
    /// Update falling items - cleanup destroyed references only.
    /// Movement and collision handled by FallingItem script on each prefab.
    /// </summary>
    private void UpdateFallingItems()
    {
        // Cleanup destroyed items from tracking list
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            if (_activeItems[i] == null)
            {
                _activeItems.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Called by FallingItem script when an item is caught.
    /// </summary>
    public void OnItemCaught(bool isEidia)
    {
        if (!_isPlaying) return;

        if (isEidia)
        {
            // Caught Eidia (good)!
            _score++;
            if (scoreText != null)
                scoreText.text = _score.ToString();

            // Visual feedback - punch effect
            if (playerBasket != null)
            {
                playerBasket.DOPunchScale(catchPunchScale, catchPunchDuration).SetUpdate(true);
            }

            // Play catch sound (use enum-based system)
            AudioManager.Instance?.PlaySFX(AudioManager.SFXType.CatchGood);

#if UNITY_EDITOR
            // Debug.Log($"[CatchMiniGame] Eidia caught! Score: {_score}");
#endif
        }
        else
        {
            // Caught Ma'amoul (bad)!
            _score = Mathf.Max(0, _score - 1);
            if (scoreText != null)
                scoreText.text = _score.ToString();

            // Visual feedback - shake effect
            if (playerBasket != null)
            {
                playerBasket.DOShakeScale(avoidShakeDuration, avoidShakeStrength, avoidShakeVibrato, avoidShakeRandomness).SetUpdate(true);
            }

            // JUICE: Floating Text
            FloatingTextManager.Instance?.SpawnCustom("-1", Color.red, playerBasket != null ? playerBasket.position + Vector3.up : Vector3.zero);

            // Play avoid sound (use enum-based system)
            AudioManager.Instance?.PlaySFX(AudioManager.SFXType.CatchBad);

#if UNITY_EDITOR
            // Debug.Log($"[CatchMiniGame] Ma'amoul caught! Score: {_score}");
#endif
        }
    }

    /// <summary>
    /// End the mini-game and transition to next house.
    /// </summary>
    private void EndGame()
    {
        _isPlaying = false;

#if UNITY_EDITOR
        // Debug.Log($"[CatchMiniGame] TIME'S UP! Final Score: {_score}");
#endif

        // Closing the Economic Loop (Phase 3)
        // Balancing: 1 Eidia per score
        int scrapReward = _score > 0 ? Mathf.Max(1, Mathf.FloorToInt(_score * scrapPerPoint)) : 0;

        // Return to MiniGameManager - this will handle GameManager.OnMiniGameComplete
        if (MiniGameManager.Instance != null)
            MiniGameManager.Instance.EndMiniGame(_score, scrapReward);

        Destroy(gameObject);
    }
}
