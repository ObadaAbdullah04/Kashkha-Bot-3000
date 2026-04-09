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

    [Header("Mini-Game Assignment (PHASE 6 UPDATE)")]
    [Tooltip("Mini-game for slot 1 (between House 1 and 2)")]
    [SerializeField] private MiniGameType miniGameSlot1 = MiniGameType.CatchGame;

    [Tooltip("Mini-game for slot 2 (between House 2 and 3)")]
    [SerializeField] private MiniGameType miniGameSlot2 = MiniGameType.PathDrawing;

    [Tooltip("Mini-game for slot 3 (between House 3 and 4)")]
    [SerializeField] private MiniGameType miniGameSlot3 = MiniGameType.CatchGame;

    [Header("Catch Game Settings")]
    [Tooltip("Duration for House 1 catch mini-game")]
    [SerializeField] private float house1Duration = 10f;
    [Tooltip("Duration for House 2 catch mini-game")]
    [SerializeField] private float house2Duration = 12f;
    [Tooltip("Duration for House 3 catch mini-game")]
    [SerializeField] private float house3Duration = 15f;

    [Header("Path Drawing Settings")]
    [Tooltip("Time limit for House 1 path drawing")]
    [SerializeField] private float pathTimeHouse1 = 35f;
    [Tooltip("Time limit for House 2 path drawing")]
    [SerializeField] private float pathTimeHouse2 = 30f;
    [Tooltip("Time limit for House 3 path drawing")]
    [SerializeField] private float pathTimeHouse3 = 40f;

    private GameObject _activeMiniGameInstance;

    #region Events

    /// <summary>
    /// Fires when mini-game ends with rewards. Used by FloatingTextManager.
    /// </summary>
    public static Action<int, int> OnMiniGameEnded;

    #endregion

    #region Public API

    /// <summary>
    /// Starts the mini-game assigned to the specified slot (0-2).
    /// Slot 0 = Between House 1-2, Slot 1 = Between House 2-3, Slot 2 = Between House 3-4
    /// </summary>
    public void StartAssignedMiniGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2)
        {
            Debug.LogError($"[MiniGameManager] Invalid slot index: {slotIndex}. Must be 0-2.");
            return;
        }

        MiniGameType gameType = slotIndex switch
        {
            0 => miniGameSlot1,
            1 => miniGameSlot2,
            2 => miniGameSlot3,
            _ => MiniGameType.CatchGame
        };

        // House level for difficulty scaling (use slotIndex + 1)
        int houseLevel = slotIndex + 1;

        Debug.Log($"[MiniGameManager] Starting assigned mini-game: Slot {slotIndex + 1}, Type: {gameType}, House: {houseLevel}");

        StartMiniGame(gameType, houseLevel);
    }

    /// <summary>
    /// Starts a specific mini-game type with house-based difficulty.
    /// </summary>
    public void StartMiniGame(MiniGameType type, int houseLevel)
    {
        PrepareForMiniGame();

        switch (type)
        {
            case MiniGameType.CatchGame:
                StartCatchGame(houseLevel);
                break;

            case MiniGameType.PathDrawing:
                StartPathDrawingGame(houseLevel);
                break;

            default:
                Debug.LogWarning($"[MiniGameManager] Unknown mini-game type: {type}. Defaulting to CatchGame.");
                StartCatchGame(houseLevel);
                break;
        }
    }

    #endregion

    #region Existing Start Methods

    #region Helper Methods

    /// <summary>
    /// Prepares UI and game state for mini-game start.
    /// </summary>
    private void PrepareForMiniGame()
    {
        // Hide encounter UI during mini-game
        UIManager.Instance?.HideAllPanelsForMiniGame();

        // Change state
        GameManager.Instance?.ChangeState(GameState.InterHouseMiniGame);
    }

    /// <summary>
    /// Generic duration calculator based on house level.
    /// </summary>
    private float GetDurationForHouse(int houseLevel, float h1, float h2, float h3)
    {
        return houseLevel switch
        {
            1 => h1,
            2 => h2,
            3 => h3,
            _ => h1
        };
    }

    /// <summary>
    /// Instantiates and configures a mini-game prefab with proper canvas setup.
    /// </summary>
    private GameObject InstantiateMiniGamePrefab(GameObject prefab)
    {
        var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        ConfigureCanvasForInstance(instance);
        return instance;
    }

    /// <summary>
    /// Configures canvas and transform for a mini-game instance.
    /// </summary>
    private void ConfigureCanvasForInstance(GameObject instance)
    {
        // Force reset transform for Overlay Canvas
        Transform t = instance.transform;
        t.SetParent(null);
        t.localScale = Vector3.one;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        Canvas canvas = instance.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MiniGameManager] No Canvas found on prefab!");
            return;
        }

        Camera mainCam = GetMainCamera();
        canvas.renderMode = mainCam != null
            ? RenderMode.ScreenSpaceCamera
            : RenderMode.ScreenSpaceOverlay;

        if (mainCam != null)
        {
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 100f;
            Debug.Log($"[MiniGameManager] Canvas assigned to camera: {mainCam.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[MiniGameManager] No camera found! Using ScreenSpaceOverlay mode.");
        }

        // Fix RectTransform
        var rect = canvas.GetComponent<RectTransform>();
        if (rect != null) ConfigureRectTransform(rect);
    }

    /// <summary>
    /// Configures RectTransform to fill the screen.
    /// </summary>
    private void ConfigureRectTransform(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    /// <summary>
    /// Finds the main camera with fallback logic.
    /// </summary>
    private Camera GetMainCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = Camera.allCameras.FirstOrDefault(c => c.CompareTag("MainCamera"))
                      ?? Camera.allCameras.FirstOrDefault(c => c.enabled);
        }
        return mainCam;
    }

    /// <summary>
    /// Fallback behavior when mini-game prefab is missing.
    /// </summary>
    private void FallbackToNextHouse()
    {
        GameManager.Instance?.OnMiniGameComplete(0);
    }

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Task 1: Starts the Eidia Catch mini-game between houses.
    /// MiniGameManager determines duration based on house level and initializes CatchMiniGame.
    /// </summary>
    /// <param name="houseLevel">The house level just completed (determines duration)</param>
    public void StartCatchGame(int houseLevel)
    {
        if (catchGamePrefab == null)
        {
            Debug.LogError("[MiniGameManager] catchGamePrefab not assigned!");
            FallbackToNextHouse();
            return;
        }

        PrepareForMiniGame();

        float duration = GetDurationForHouse(houseLevel, house1Duration, house2Duration, house3Duration);

        Debug.Log($"[MiniGameManager] Starting Catch Game (House {houseLevel}, {duration}s)");

        // Instantiate and configure
        _activeMiniGameInstance = InstantiateMiniGamePrefab(catchGamePrefab);

        // Initialize CatchMiniGame
        CatchMiniGame catchGame = _activeMiniGameInstance.GetComponent<CatchMiniGame>();
        if (catchGame != null)
        {
            catchGame.Initialize(duration);
            Debug.Log($"[MiniGameManager] Initialized CatchMiniGame with duration: {duration}s");
        }
        else
        {
            Debug.LogError("[MiniGameManager] CatchMiniGame component not found on prefab!");
        }
    }

    /// <summary>
    /// Called by CatchMiniGame when it ends. Fires event for FloatingTextManager, then goes to HouseHub.
    /// </summary>
    public void EndMiniGame(int eidiaEarned, int scrapEarned)
    {
        // Fire event for FloatingTextManager BEFORE destroying the instance
        OnMiniGameEnded?.Invoke(eidiaEarned, scrapEarned);
        Debug.Log($"[MiniGameManager] Mini-game ended. Rewards: +{eidiaEarned} Eidia, +{scrapEarned} Scrap");

        if (_activeMiniGameInstance != null)
        {
            Destroy(_activeMiniGameInstance);
            _activeMiniGameInstance = null;
        }

        // PHASE 10: After mini-game, show unified hub
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMiniGameComplete(eidiaEarned);
        }
    }
    
    /// <summary>
    /// PHASE 5C (REVISED): Starts the Path-Drawing Maze mini-game.
    /// Alternates with Catch game or can be set as primary mini-game.
    /// Obstacles are manually placed in the prefab - no auto-spawning.
    /// </summary>
    public void StartPathDrawingGame(int houseLevel)
    {
        if (pathDrawingPrefab == null)
        {
            Debug.LogError("[MiniGameManager] pathDrawingPrefab not assigned! Fallback to Catch game.");
            StartCatchGame(houseLevel);
            return;
        }

        PrepareForMiniGame();

        float timeLimit = GetDurationForHouse(houseLevel, pathTimeHouse1, pathTimeHouse2, pathTimeHouse3);

        Debug.Log($"[MiniGameManager] Starting Path Drawing Game (House {houseLevel}, {timeLimit}s)");

        // Instantiate and configure
        _activeMiniGameInstance = InstantiateMiniGamePrefab(pathDrawingPrefab);

        Debug.Log($"[MiniGameManager] PathDrawingGame initialized with manual obstacles");
    }

    [Button("Test Catch Game (House 1)")]
    private void TestCatchGame1() => StartCatchGame(1);

    [Button("Test Catch Game (House 2)")]
    private void TestCatchGame2() => StartCatchGame(2);

    [Button("Test Catch Game (House 3)")]
    private void TestCatchGame3() => StartCatchGame(3);
    
    [Button("🗺️ Test Path Game (House 1)")]
    private void TestPathGame1() => StartPathDrawingGame(1);
    
    [Button("🗺️ Test Path Game (House 2)")]
    private void TestPathGame2() => StartPathDrawingGame(2);
    
    [Button("🗺️ Test Path Game (House 3)")]
    private void TestPathGame3() => StartPathDrawingGame(3);

    #endregion
}