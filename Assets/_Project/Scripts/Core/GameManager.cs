using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// PHASE 6 (FINAL): Core game state machine for swipe-card encounters.
/// Flow: Wardrobe → Unified Hub → Houses (Swipe Cards) → Mini-Game → Win/Game Over
/// 
/// UNIFIED HUB ARCHITECTURE:
/// - Single panel with 3 tabs: Houses, Wardrobe, Upgrades
/// - No mid-run wardrobe visits (wardrobe is just a tab in the hub)
/// - Clean state machine: Hub appears after each house/mini-game
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Optimization: Cap frame rate at 60 for mobile performance
        Application.targetFrameRate = 60;

        DOTween.Init(recycleAllByDefault: true, useSafeMode: false, logBehaviour: LogBehaviour.ErrorsOnly)
               .SetCapacity(200, 50);

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

    #endregion

    #region Inspector Fields

    [Header("Win Condition")]
    [Tooltip("Eidia needed to win")]
    [SerializeField] private int eidiaToWin = 100;

    [Header("House Transition Texts")]
    [SerializeField] private string house1TransitionText = "السفر إلى بيت خالة أم محمد...";
    [SerializeField] private string house2TransitionText = "الذهاب إلى بيت عمو أبو أحمد...";
    [SerializeField] private string house3TransitionText = "زيارة بيت جدو الحاج...";
    [SerializeField] private string house4TransitionText = "⚠️ دخول بيت الجنون...";
    [SerializeField] private string defaultTransitionText = "السفر...";

    [Header("Mini-Game Transition Texts")]
    [SerializeField] private string catchGameTransitionText = "وقت العيدية!";
    [SerializeField] private string pathDrawingTransitionText = "تحدي المتاهة!";
    [SerializeField] private string memorySwapTransitionText = "تحدي الذاكرة!";
    [SerializeField] private string backToHubTransitionText = "العودة للمجلس...";

    #endregion

    #region State

    [SerializeField] private GameState currentState = GameState.MainMenu;
    [SerializeField] private int currentHouseLevel = 1;
    [SerializeField] private bool isHouse4Active = false;
    [SerializeField] private int currentRunSeed = 0;
    [SerializeField] private bool[] completedHouses = new bool[5];
    [SerializeField] private int accumulatedEidia = 0;
    [SerializeField] private int encounterStreakBonus = 0; // Streak bonus from current encounter
    [SerializeField] private int eidiaAtStartOfHouse = 0; // Track for scrap delta calculation

    public static Action<GameState> OnStateChanged;
    public static Action OnRunStarted;
    public GameState CurrentState => currentState;
    public int CurrentHouseLevel => currentHouseLevel;
    public bool IsHouse4Active => isHouse4Active;
    public int CurrentRunSeed => currentRunSeed;
    public int AccumulatedEidia => accumulatedEidia;
    public int EidiaToWin => eidiaToWin;
    public int EncounterStreakBonus => encounterStreakBonus;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        SwipeEncounterManager.OnCardProcessed += HandleCardProcessed;
        InteractionHUDController.OnEidiaEarned += HandleEidiaEarned;
        UnifiedHubManager.OnStartNextHouse += EnterHouse;
        UnifiedHubManager.OnStartMiniGame += HandleMiniGameSelected;
        UnifiedHubManager.OnPlayAgain += HandlePlayAgain;
        UIManager.OnPlayAgain += HandlePlayAgain; // UNIFIED RESTART
        UnifiedHubManager.OnOutfitEquipped += HandleOutfitEquipped;
        TransitionPlayer.OnTransitionComplete += OnTransitionFinished;
        MeterManager.OnBatteryDrained += HandleBatteryDrained;
        MeterManager.OnStomachFull += HandleStomachFull;
        HouseFlowController.OnHouseCompleted += HandleHouseFlowCompleted;
    }

    private void OnDisable()
    {
        SwipeEncounterManager.OnCardProcessed -= HandleCardProcessed;
        InteractionHUDController.OnEidiaEarned -= HandleEidiaEarned;
        UnifiedHubManager.OnStartNextHouse -= EnterHouse;
        UnifiedHubManager.OnStartMiniGame -= HandleMiniGameSelected;
        UnifiedHubManager.OnPlayAgain -= HandlePlayAgain;
        UIManager.OnPlayAgain -= HandlePlayAgain; // UNIFIED RESTART
        UnifiedHubManager.OnOutfitEquipped -= HandleOutfitEquipped;
        TransitionPlayer.OnTransitionComplete -= OnTransitionFinished;
        MeterManager.OnBatteryDrained -= HandleBatteryDrained;
        MeterManager.OnStomachFull -= HandleStomachFull;
        HouseFlowController.OnHouseCompleted -= HandleHouseFlowCompleted;
    }

    #endregion

    #region State Management

    public void ChangeState(GameState newState)
    {
        GameState previous = currentState;
        currentState = newState;
        // Debug.Log($"[GameManager] State: {previous} → {currentState}");
        OnStateChanged?.Invoke(currentState);
    }

    #endregion

    #region Eidia & Reward Tracking

    private void HandleEidiaEarned(int amount)
    {
        if (amount <= 0) return;
        
        accumulatedEidia += amount;
        
        // Persist lifetime Eidia immediately (delta only)
        SaveManager.Instance?.AddRunRewards(amount);
        
        // Debug.Log($"[GameManager] Eidia Earned: +{amount}. Run Total: {accumulatedEidia}");
    }

    private void HandleCardProcessed(float batteryDelta, int eidia, bool wasCorrect)
    {
        // Prevent eidia accumulation after game over or win
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        if (eidia > 0)
        {
            HandleEidiaEarned(eidia);
        }

        PlayFeedbackEffects(wasCorrect);

        // Debug.Log($"[GameManager] Card: {(wasCorrect ? "CORRECT" : "INCORRECT")} | +{eidia} Eidia");
    }

    #endregion

    #region Run Lifecycle

    public void StartRun()
    {
        // PHASE 18: Ensure a clean state when restarting
        HouseFlowController.Instance?.CancelActiveSequence();
        MiniGameManager.Instance?.CleanupActiveMiniGame();

        currentHouseLevel = 0; // Reset to 0 so next house is 1
        isHouse4Active = false;
        accumulatedEidia = 0;
        encounterStreakBonus = 0;
        completedHouses = new bool[5];
        currentRunSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        // Debug.Log($"[GameManager] Run Seed: {currentRunSeed}");

        // SYNC SCRAP at start of run so wardrobe shows correct values
        WardrobeManager.Instance?.SyncScrap();

        FloatingTextManager.Instance?.gameObject.SetActive(true);
        MeterManager.Instance?.ResetMeters();
        
        // CRITICAL: Reset post-processing on restart
        URPPostProcessing.Instance?.ResetEffects();

        OnRunStarted?.Invoke();

        // Show unified hub (Houses tab) to start the run
        ShowUnifiedHub();
    }

    #endregion

    #region House Management

    /// <summary>
    /// PHASE 9.6: Starts a house using self-driving coroutine flow.
    /// Loads the house sequence and hands control to HouseFlowController.
    /// </summary>
    public void StartHouse(int houseLevel)
    {
        currentHouseLevel = houseLevel;
        encounterStreakBonus = 0;
        eidiaAtStartOfHouse = accumulatedEidia; // Capture for scrap delta
        MeterManager.Instance?.ResetHouseCounters();

        // Debug.Log($"[GameManager] Starting House {currentHouseLevel}!");

        // REFRESH HUD BEFORE ANYTHING HAPPENS (ensure meters show correct values)
        UIManager.Instance?.RefreshMetersPublic();

        // CRITICAL FIX: Hide gameplay HUD before transition so nothing shows behind it
        // BUT don't hide the hub yet - it will be covered by the transition fade-in
        UIManager.Instance?.HideSwipeEncounter();
        UIManager.Instance?.HideInteractionHUD();

        if (TransitionPlayer.Instance != null)
        {
            string text = GetHouseTransitionText(houseLevel);
            TransitionPlayer.Instance.PlayTransition(text, 
                onMidpoint: () =>
                {
                    // This callback fires when screen is fully black (mid-point)
                    // IDEAL FOR SETUP: Hide hub and change state while hidden
                    UIManager.Instance?.HideUnifiedHub();
                    ChangeState(GameState.Encounter);
                    UIManager.Instance?.ShowSwipeEncounter();
                }, 
                overrideTextDuration: 0f, 
                onReady: () =>
                {
                    // This callback fires when wait duration is OVER (just before fade-out)
                    // IDEAL FOR STARTING ACTION: Start the actual house flow here
                    StartHouseFlowController(houseLevel);
                });
        }
        else
        {
            UIManager.Instance?.HideUnifiedHub();
            ChangeState(GameState.Encounter);
            UIManager.Instance?.ShowSwipeEncounter();
            StartHouseFlowController(houseLevel);
        }
    }

    /// <summary>
    /// PHASE 9.6: Starts the self-driving house flow coroutine.
    /// HouseFlowController drives itself via coroutines.
    /// </summary>
    private void StartHouseFlowController(int houseLevel)
    {
        if (HouseFlowController.Instance == null)
        {
            // Debug.LogError("[GameManager] HouseFlowController not available! Cannot start house.");
            EndHouse();
            return;
        }

        HouseSequenceData sequence = GetHouseSequenceForLevel(houseLevel);

        if (sequence == null || sequence.Sequence == null || sequence.Sequence.Count == 0)
        {
            // Debug.LogError($"[GameManager] No sequence data for House {houseLevel}!");
            EndHouse();
            return;
        }

        // Start the self-driving coroutine
        StartCoroutine(HouseFlowController.Instance.PlayHouseSequence(houseLevel, sequence));
    }

    /// <summary>
    /// PHASE 9.6: Gets the HouseSequenceData for a house level.
    /// Loads from Resources folder (Sequences/House{level}_Sequence.asset).
    /// Falls back to auto-generated test sequence if not found.
    /// </summary>
    private HouseSequenceData GetHouseSequenceForLevel(int houseLevel)
    {
        // Resources.Load searches relative to any Resources/ folder
        string path = $"Sequences/House{houseLevel}_Sequence";
        HouseSequenceData sequence = Resources.Load<HouseSequenceData>(path);

        if (sequence == null)
        {
            // Debug.LogWarning($"[GameManager] HouseSequenceData not found at '{path}'. Generating test sequence.");
            sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
            sequence.name = $"House{houseLevel}_Test";
            sequence.HouseLevel = houseLevel;
            sequence.Sequence = CreateTestSequence(houseLevel);
        }

        return sequence;
    }

    /// <summary>
    /// PHASE 9.6: Creates a test sequence from CSV question pool.
    /// Used as fallback when HouseSequenceData assets aren't available.
    /// </summary>
    private List<SequenceElement> CreateTestSequence(int houseLevel)
    {
        var elements = new List<SequenceElement>();

        var questions = DataManager.Instance?.GetQuestionsForHouse(houseLevel);
        if (questions != null && questions.Count > 0)
        {
            // Pick first 4 questions as a simple test
            int count = Mathf.Min(4, questions.Count);
            for (int i = 0; i < count; i++)
            {
                elements.Add(new SequenceElement(ElementType.Question, questions[i].ID));
            }
        }
        else
        {
            // Debug.LogWarning($"[GameManager] No questions in CSV for House {houseLevel}!");
            // Add a dummy element so the sequence isn't empty
            elements.Add(new SequenceElement(ElementType.Question, "Q1"));
        }

        return elements;
    }

    private string GetHouseTransitionText(int houseLevel)
    {
        return houseLevel switch
        {
            1 => house1TransitionText,
            2 => house2TransitionText,
            3 => house3TransitionText,
            4 => house4TransitionText,
            _ => defaultTransitionText
        };
    }

    #endregion

    #region House Completion

    /// <summary>
    /// PHASE 9: Called when HouseFlowController completes a house.
    /// </summary>
    private void HandleHouseFlowCompleted(int houseLevel)
    {
        // Debug.Log($"[GameManager] House {houseLevel} flow completed!");
        
        // Get streak bonus if applicable
        if (SwipeEncounterManager.Instance != null)
        {
            encounterStreakBonus = SwipeEncounterManager.Instance.GetStreakBonus();
            if (encounterStreakBonus > 0)
            {
                // Use centralized handler to track run total and persist lifetime total
                HandleEidiaEarned(encounterStreakBonus);
                // Debug.Log($"[GameManager] Streak bonus: +{encounterStreakBonus} Eidia!");
            }
        }

        // House complete - move to next
        EndHouse();
    }

    private void EndHouse()
    {
        // Mark house as complete if valid house level
        if (currentHouseLevel >= 1 && currentHouseLevel <= 4)
        {
            completedHouses[currentHouseLevel] = true;
        }

        // PHASE 18: Award scrap based on HOUSE eidia so player can buy things
        int houseEidia = accumulatedEidia - eidiaAtStartOfHouse;
        SaveManager.Instance?.AddScrap(houseEidia);

        // House 4 completion - trigger win immediately (no hub shown)
        if (currentHouseLevel == 4)
        {
            // Debug.Log("[GameManager] House 4 completed! Triggering win...");
            if (isHouse4Active)
            {
                // Debug.Log("[GameManager] INSANE MODE COMPLETE!");
                WinGame(isHouse4Clear: true);
            }
            else
            {
                // Debug.Log("[GameManager] Normal House 4 complete - winning!");
                WinGame(isHouse4Clear: false);
            }
            return;
        }

        // Debug.Log($"[GameManager] House {currentHouseLevel} complete! Going to Hub...");

        // Mark this house as complete in the Unified Hub
        UnifiedHubManager.Instance?.MarkHouseComplete(currentHouseLevel);

        // Show hub for next action
        ShowUnifiedHub();
    }

    public void OnMiniGameComplete(int eidiaEarned)
    {
        // Debug.Log($"[GameManager] === OnMiniGameComplete === Eidia earned: {eidiaEarned}");
        
        // Use centralized handler
        HandleEidiaEarned(eidiaEarned);

        // NOTE: Scrap is already awarded by MiniGameManager.EndMiniGame using the specific scrapEarned calculation.

        // Debug.Log($"[GameManager] Accumulated Eidia: {accumulatedEidia}");
        ShowUnifiedHub();
    }

    #endregion

    #region Unified Hub Flow

    /// <summary>
    /// Shows the unified hub panel and updates game state.
    /// Called after each house/mini-game completion.
    /// </summary>
    private void ShowUnifiedHub()
    {
        // PHASE 18: Transition back to hub
        if (TransitionPlayer.Instance != null && currentState != GameState.HouseHub && currentState != GameState.MainMenu)
        {
            TransitionPlayer.Instance.PlayTransition(backToHubTransitionText, () =>
            {
                // Mid-point (screen is black)
                
                // CRITICAL: Cleanup any active mini-game instance here
                MiniGameManager.Instance?.CleanupActiveMiniGame();

                ChangeState(GameState.HouseHub);
                int next = currentHouseLevel + 1;
                UnifiedHubManager.Instance?.InitializeHub(next, completedHouses);
                UIManager.Instance?.ShowUnifiedHub();
            });
        }
        else
        {
            MiniGameManager.Instance?.CleanupActiveMiniGame();
            ChangeState(GameState.HouseHub);
            int next = currentHouseLevel + 1;
            UnifiedHubManager.Instance?.InitializeHub(next, completedHouses);
            UIManager.Instance?.ShowUnifiedHub();
        }

        // Debug.Log($"[GameManager] Unified Hub. Next: {currentHouseLevel + 1}");
    }

    private void EnterHouse(int houseLevel)
    {
        if (houseLevel > currentHouseLevel + 1 && !completedHouses[houseLevel - 1])
        {
            // Debug.LogWarning($"[GameManager] Cannot enter House {houseLevel} - previous not complete!");
            return;
        }

        // DON'T hide hub here anymore - StartHouse handles it via transition callback
        StartHouse(houseLevel);
    }

    private void HandleMiniGameSelected(int miniGameIndex)
    {
        // Debug.Log($"[GameManager] Mini-game {miniGameIndex + 1} selected from Hub.");

        if (TransitionPlayer.Instance != null)
        {
            string text = GetMiniGameTransitionText(miniGameIndex);
            // PHASE 18: Use explicit duration for mini-games (2.5s) to ensure they show long enough
            TransitionPlayer.Instance.PlayTransition(text, 
                onMidpoint: () =>
                {
                    // SETUP: Hide hub and change state while hidden
                    UIManager.Instance?.HideUnifiedHub();
                    ChangeState(GameState.InterHouseMiniGame);
                }, 
                overrideTextDuration: 2.5f, 
                onReady: () =>
                {
                    // START: Action begins as transition starts fading out
                    MiniGameManager.Instance?.StartAssignedMiniGame(miniGameIndex);
                });
        }
        else
        {
            UIManager.Instance?.HideUnifiedHub();
            ChangeState(GameState.InterHouseMiniGame);
            MiniGameManager.Instance?.StartAssignedMiniGame(miniGameIndex);
        }
    }

    private string GetMiniGameTransitionText(int index)
    {
        // PHASE 18: Get the actual game type assigned to this slot from MiniGameManager
        if (MiniGameManager.Instance != null)
        {
            MiniGameType type = MiniGameManager.Instance.GetMiniGameTypeForSlot(index);
            return type switch
            {
                MiniGameType.CatchGame => catchGameTransitionText,
                MiniGameType.PathDrawing => pathDrawingTransitionText,
                MiniGameType.MemorySwap => memorySwapTransitionText,
                _ => "وقت اللعب!"
            };
        }

        return "وقت اللعب!";
    }

    private void HandlePlayAgain()
    {
        // Debug.Log("[GameManager] Play Again!");
        
        if (TransitionPlayer.Instance != null)
        {
            TransitionPlayer.Instance.PlayTransition("بدء جولة جديدة...", () =>
            {
                UIManager.Instance?.HideUnifiedHub();
                StartRun();
            });
        }
        else
        {
            UIManager.Instance?.HideUnifiedHub();
            StartRun();
        }
    }

    private void HandleOutfitEquipped(int outfitID)
    {
        // Debug.Log($"[GameManager] Outfit equipped (ID: {outfitID}).");

        // For now, outfits are cosmetic only
        // Stat bonuses will be re-added later when needed
    }

    private void OnTransitionFinished()
    {
#if UNITY_EDITOR
        // Debug.Log("[GameManager] Transition finished.");
#endif
    }

    #endregion

    #region Game Over / Win

    private void HandleBatteryDrained() => HandleGameOver("Battery");
    private void HandleStomachFull() => HandleGameOver("Stomach");

    private void HandleGameOver(string reason)
    {
        string msg = reason switch
        {
            "Battery" => "Game Over: Social Shutdown",
            "Stomach" => "Game Over: Ma'amoul Explosion",
            _ => "Game Over!"
        };

        // Debug.Log($"[GameManager] {msg}");
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.GameOver);
        PlayGameOverEffects(reason);

        // CRITICAL FIX: Put hub in post-game mode BEFORE changing state
        UnifiedHubManager.Instance?.EnterGameOverMode();

        ChangeState(GameState.GameOver);
        
        // PHASE 18: Show results with total eidia
        UIManager.Instance?.ShowGameOver(accumulatedEidia);
    }

    public void WinGame(bool isHouse4Clear = false)
    {
        string msg = isHouse4Clear
            ? "INSANE MODE CLEAR! You survived House 4!"
            : "You survived Eid! Congratulations!";

        // Debug.Log($"[GameManager] {msg}");
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.Win);

        // CRITICAL FIX: Put hub in win mode BEFORE changing state
        UnifiedHubManager.Instance?.EnterWinMode();

        ChangeState(GameState.Win);

        // PHASE 18: Show results with total eidia
        UIManager.Instance?.ShowWin(accumulatedEidia);
    }

    #endregion

    #region Helpers

    private void PlayFeedbackEffects(bool isCorrect, bool includeCameraShake = true)
    {
        if (ScreenFlash.Instance != null)
        {
            if (isCorrect) ScreenFlash.Instance.FlashCorrect();
            else ScreenFlash.Instance.FlashWrong();
        }

        if (AudioManager.Instance != null)
        {
            if (isCorrect) AudioManager.Instance.PlayCorrectAnswer();
            else
            {
                AudioManager.Instance.PlayWrongAnswer();
                if (includeCameraShake) CameraShakeManager.Instance?.ShakeWrongAnswer();
            }
        }
        else if (!isCorrect && includeCameraShake)
        {
            CameraShakeManager.Instance?.ShakeWrongAnswer();
        }
    }

    private void PlayGameOverEffects(string reason)
    {
        if (reason == "Stomach")
        {
            UIManager.Instance?.ShakeMaamoulExplosion();
            HapticFeedback.Instance?.ExplosionVibration();
        }
        else
        {
            UIManager.Instance?.ShakeSocialShutdown();
            HapticFeedback.Instance?.HeavyVibration();
        }

        URPPostProcessing.Instance?.EnableGameOverEffect();
    }

    #endregion

    #region Test Buttons

    [Button("🧪 DEBUG: Win Game")]
    private void DebugWin() => WinGame();

    [Button("🧪 DEBUG: Game Over (Battery)")]
    private void DebugGameOverBattery() => HandleGameOver("Battery");

    [Button("🧪 DEBUG: Game Over (Stomach)")]
    private void DebugGameOverStomach() => HandleGameOver("Stomach");

    [Button("Start Run")]
    private void TestStartRun() => StartRun();

    [Button("Start House 4")]
    private void TestHouse4()
    {
        isHouse4Active = true;
        MeterManager.Instance?.EnableHouse4Mode();
        currentHouseLevel = 4;
        ChangeState(GameState.Encounter);
        StartHouseFlowController(4);
    }

    #endregion
}
