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

    [Header("Mini-Game Settings")]
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
            // Fallback: skip mini-game and go to next house
            if (GameManager.Instance != null)
                GameManager.Instance.StartNextHouse();
            return;
        }

        // Hide encounter UI during mini-game
        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanelsForMiniGame();

        // Change state
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.InterHouseMiniGame);

        // Task 1: Calculate duration based on house level (MiniGameManager is source of truth)
        float duration = houseLevel switch
        {
            1 => house1Duration,
            2 => house2Duration,
            3 => house3Duration,
            _ => house1Duration
        };

        Debug.Log($"[MiniGameManager] Starting Catch Game (House {houseLevel}, {duration}s)");

        // Instantiate the prefab
        _activeMiniGameInstance = Instantiate(catchGamePrefab, Vector3.zero, Quaternion.identity);

        // CRITICAL FIX: Force reset transform for Overlay Canvas
        Transform miniGameTransform = _activeMiniGameInstance.transform;
        miniGameTransform.SetParent(null); // Ensure root-level
        miniGameTransform.localScale = Vector3.one;
        miniGameTransform.localPosition = Vector3.zero;
        miniGameTransform.localRotation = Quaternion.identity;

        // CRITICAL FIX: Configure Canvas render mode with explicit fallback
        Canvas canvas = _activeMiniGameInstance.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            // Find main camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                // Fallback: find any camera with MainCamera tag or first enabled camera
                mainCam = Camera.allCameras.FirstOrDefault(c => c.CompareTag("MainCamera"))
                          ?? Camera.allCameras.FirstOrDefault(c => c.enabled);
            }

            if (mainCam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCam;
                canvas.planeDistance = 100f;
                Debug.Log($"[MiniGameManager] Canvas assigned to camera: {mainCam.gameObject.name}");
            }
            else
            {
                // FIX 7.4: Explicitly set Overlay mode if no camera found
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Debug.LogWarning("[MiniGameManager] No camera found! Using ScreenSpaceOverlay mode.");
            }

            // Fix RectTransform
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
        else
        {
            Debug.LogError("[MiniGameManager] No Canvas found on prefab!");
        }

        // Task 1: Get CatchMiniGame component and Initialize with duration from MiniGameManager
        CatchMiniGame catchGame = _activeMiniGameInstance.GetComponent<CatchMiniGame>();
        if (catchGame != null)
        {
            // MiniGameManager is the single source of truth - tells CatchMiniGame when to start and how long
            catchGame.Initialize(duration);
            Debug.Log($"[MiniGameManager] Initialized CatchMiniGame with duration: {duration}s");
        }
        else
        {
            Debug.LogError("[MiniGameManager] CatchMiniGame component not found on prefab!");
        }
    }

    /// <summary>
    /// Called by CatchMiniGame when it ends. Fires event for FloatingTextManager, then cleans up.
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

        // Hide encounter UI during mini-game
        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanelsForMiniGame();

        // Change state
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.InterHouseMiniGame);

        // Calculate difficulty based on house level (time only - obstacles are manual)
        float timeLimit = houseLevel switch
        {
            1 => pathTimeHouse1,
            2 => pathTimeHouse2,
            3 => pathTimeHouse3,
            _ => pathTimeHouse1
        };

        Debug.Log($"[MiniGameManager] Starting Path Drawing Game (House {houseLevel}, {timeLimit}s)");

        // Instantiate the prefab
        _activeMiniGameInstance = Instantiate(pathDrawingPrefab, Vector3.zero, Quaternion.identity);

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
}
