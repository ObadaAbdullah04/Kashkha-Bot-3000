using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    /// <summary>
    /// Static flag to trigger a run start after a scene reload.
    /// Used for "Try Again" functionality.
    /// </summary>
    public static bool StartRunOnLoad = false;

    private void Awake()
    {
        // Robust Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Optimization: Cap frame rate at 60 for mobile performance
        Application.targetFrameRate = 60;

        DOTween.Init(recycleAllByDefault: true, useSafeMode: false, logBehaviour: LogBehaviour.ErrorsOnly)
               .SetCapacity(200, 50);
    }

    private void Start()
    {
        if (StartRunOnLoad)
        {
            StartRunOnLoad = false;
            StartRun();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // PHASE 18: Clear static events to prevent ghost references between scene reloads
            OnStateChanged = null;
            OnRunStarted = null;
            OnRunEidiaUpdated = null;
            
            Instance = null;
        }
    }

    #endregion

    #region Inspector Fields

    [Header("Win Condition")]
    [Tooltip("Eidia needed to win")]
    [SerializeField] private int eidiaToWin = 100;

    [Header("House Transition Texts")]
    [SerializeField] private string introTransitionText = "جاري التحميل...";
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
    public static Action<int> OnRunEidiaUpdated;
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
        HouseFlowController.OnHouseCompleted += HandleHouseFlowCompleted;
    }

    #endregion

    #region State Management

    /// <summary>
    /// Updates the game state and notifies all listeners.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        currentState = newState;
        OnStateChanged?.Invoke(currentState);
    }

    #endregion

    #region Eidia & Reward Tracking

    /// <summary>
    /// Adds earned Eidia to the current run total and persists the reward delta.
    /// </summary>
    private void HandleEidiaEarned(int amount)
    {
        if (amount <= 0) return;
        
        accumulatedEidia += amount;
        SaveManager.Instance?.AddRunRewards(amount);
        OnRunEidiaUpdated?.Invoke(accumulatedEidia);
    }

    /// <summary>
    /// Processes rewards and visual feedback when a card encounter is swiped.
    /// </summary>
    private void HandleCardProcessed(float batteryDelta, int eidia, bool wasCorrect)
    {
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        if (eidia > 0)
        {
            HandleEidiaEarned(eidia);
        }

        PlayFeedbackEffects(wasCorrect);
    }

    #endregion

    #region Run Lifecycle

    /// <summary>
    /// Initializes a fresh game session, resetting all meters, house progress, and currencies.
    /// Starts the FTUE (First Time User Experience) video sequence for new players.
    /// </summary>
    public void StartRun()
    {
        HouseFlowController.Instance?.CancelActiveSequence();
        MiniGameManager.Instance?.CleanupActiveMiniGame();

        currentHouseLevel = 0; 
        isHouse4Active = false;
        accumulatedEidia = 0;
        encounterStreakBonus = 0;
        completedHouses = new bool[5];
        currentRunSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        OnRunEidiaUpdated?.Invoke(accumulatedEidia);

        WardrobeManager.Instance?.SyncScrap();
        MeterManager.Instance?.ResetMeters();
        URPPostProcessing.Instance?.ResetEffects();

        OnRunStarted?.Invoke();

        if (SaveManager.Instance != null && !SaveManager.Instance.HasSeenTutorial("HubWalkthrough"))
        {
            UIManager.Instance?.HideAllPanels();
            
            if (TransitionPlayer.Instance != null)
            {
                TransitionPlayer.Instance.PlayTransition(introTransitionText, onMidpoint: () => {
                    CinematicController.Instance?.PlayVideo("Intro", (videoID) =>
                    {
                        ShowUnifiedHub();
                        if (UnifiedHubManager.Instance != null)
                        {
                            UnifiedHubManager.Instance.StartHubTutorial();
                        }
                    }, 
                    onPrepared: () => {
                        TransitionPlayer.Instance.SkipTransition();
                    });
                }, overrideTextDuration: 10f, instant: true); 
            }
            else
            {
                CinematicController.Instance?.PlayVideo("Intro", (videoID) => {
                    ShowUnifiedHub();
                    UnifiedHubManager.Instance?.StartHubTutorial();
                });
            }
        }
        else
        {
            ShowUnifiedHub();
        }
    }

    #endregion

    #region House Management

    /// <summary>
    /// Starts a house visit by triggering transitions and resetting house-specific counters.
    /// Hand control to HouseFlowController for sequence execution.
    /// </summary>
    public void StartHouse(int houseLevel)
    {
        currentHouseLevel = houseLevel;
        encounterStreakBonus = 0;
        eidiaAtStartOfHouse = accumulatedEidia;
        MeterManager.Instance?.ResetHouseCounters();

        UIManager.Instance?.RefreshMetersPublic();
        UIManager.Instance?.HideSwipeEncounter();
        UIManager.Instance?.HideInteractionHUD();

        if (TransitionPlayer.Instance != null)
        {
            string text = GetHouseTransitionText(houseLevel);
            TransitionPlayer.Instance.PlayTransition(text, 
                onMidpoint: () =>
                {
                    UIManager.Instance?.HideUnifiedHub();
                    ChangeState(GameState.Encounter);
                    UIManager.Instance?.ShowSwipeEncounter();
                }, 
                overrideTextDuration: 0f, 
                onReady: () =>
                {
                    StartHouseFlowController(houseLevel);
                },
                instant: true);
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
    /// Initiates the house sequence via the HouseFlowController.
    /// </summary>
    private void StartHouseFlowController(int houseLevel)
    {
        if (HouseFlowController.Instance == null)
        {
            EndHouse();
            return;
        }

        HouseSequenceData sequence = GetHouseSequenceForLevel(houseLevel);

        if (sequence == null || sequence.Sequence == null || sequence.Sequence.Count == 0)
        {
            EndHouse();
            return;
        }

        StartCoroutine(HouseFlowController.Instance.PlayHouseSequence(houseLevel, sequence));
    }

    /// <summary>
    /// Loads the house sequence asset from Resources or generates a test sequence if missing.
    /// </summary>
    private HouseSequenceData GetHouseSequenceForLevel(int houseLevel)
    {
        string path = $"Sequences/House{houseLevel}_Sequence";
        HouseSequenceData sequence = Resources.Load<HouseSequenceData>(path);

        if (sequence == null)
        {
            sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
            sequence.name = $"House{houseLevel}_Test";
            sequence.HouseLevel = houseLevel;
            sequence.Sequence = CreateTestSequence(houseLevel);
        }

        return sequence;
    }

    /// <summary>
    /// Creates a fallback test sequence from the question pool.
    /// </summary>
    private List<SequenceElement> CreateTestSequence(int houseLevel)
    {
        var elements = new List<SequenceElement>();

        var questions = DataManager.Instance?.GetQuestionsForHouse(houseLevel);
        if (questions != null && questions.Count > 0)
        {
            int count = Mathf.Min(4, questions.Count);
            for (int i = 0; i < count; i++)
            {
                elements.Add(new SequenceElement(ElementType.Question, questions[i].ID));
            }
        }
        else
        {
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
    /// Called when the HouseFlowController finishes a house sequence.
    /// </summary>
    private void HandleHouseFlowCompleted(int houseLevel)
    {
        EndHouse();
    }

    /// <summary>
    /// Marks the house as complete and either wins the game (if House 4) or returns to the Hub.
    /// </summary>
    private void EndHouse()
    {
        if (currentHouseLevel >= 1 && currentHouseLevel <= 4)
        {
            completedHouses[currentHouseLevel] = true;
        }

        if (currentHouseLevel == 4)
        {
            WinGame(isHouse4Clear: isHouse4Active);
            return;
        }

        UnifiedHubManager.Instance?.MarkHouseComplete(currentHouseLevel);
        ShowUnifiedHub();
    }

    /// <summary>
    /// Called when an inter-house mini-game finishes.
    /// </summary>
    public void OnMiniGameComplete(int eidiaEarned)
    {
        HandleEidiaEarned(eidiaEarned);
        ShowUnifiedHub();
    }

    #endregion

    #region Unified Hub Flow

    /// <summary>
    /// Returns the player to the Unified Hub and triggers the transition.
    /// </summary>
    private void ShowUnifiedHub()
    {
        if (TransitionPlayer.Instance != null && currentState != GameState.HouseHub && currentState != GameState.MainMenu)
        {
            TransitionPlayer.Instance.PlayTransition(backToHubTransitionText, () =>
            {
                MiniGameManager.Instance?.CleanupActiveMiniGame();
                ChangeState(GameState.HouseHub);
                int next = currentHouseLevel + 1;
                UnifiedHubManager.Instance?.InitializeHub(next, completedHouses);
                UIManager.Instance?.ShowUnifiedHub();
            }, instant: true);
        }
        else
        {
            MiniGameManager.Instance?.CleanupActiveMiniGame();
            ChangeState(GameState.HouseHub);
            int next = currentHouseLevel + 1;
            UnifiedHubManager.Instance?.InitializeHub(next, completedHouses);
            UIManager.Instance?.ShowUnifiedHub();
        }
    }

    /// <summary>
    /// Validates house entry and starts the selected house flow.
    /// </summary>
    private void EnterHouse(int houseLevel)
    {
        if (houseLevel > currentHouseLevel + 1 && !completedHouses[houseLevel - 1])
        {
            return;
        }

        StartHouse(houseLevel);
    }

    /// <summary>
    /// Starts an inter-house mini-game selected from the hub.
    /// </summary>
    private void HandleMiniGameSelected(int miniGameIndex)
    {
        if (TransitionPlayer.Instance != null)
        {
            string text = GetMiniGameTransitionText(miniGameIndex);
            TransitionPlayer.Instance.PlayTransition(text, 
                onMidpoint: () =>
                {
                    UIManager.Instance?.HideUnifiedHub();
                    ChangeState(GameState.InterHouseMiniGame);
                }, 
                overrideTextDuration: 2.5f, 
                onReady: () =>
                {
                    MiniGameManager.Instance?.StartAssignedMiniGame(miniGameIndex);
                },
                instant: true);
        }
        else
        {
            UIManager.Instance?.HideUnifiedHub();
            ChangeState(GameState.InterHouseMiniGame);
            MiniGameManager.Instance?.StartAssignedMiniGame(miniGameIndex);
        }
    }

    /// <summary>
    /// Returns the transition text associated with the assigned mini-game in a specific hub slot.
    /// </summary>
    private string GetMiniGameTransitionText(int index)
    {
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

    /// <summary>
    /// Performs a clean scene reload to restart the game, returning to the main menu.
    /// </summary>
    private void HandlePlayAgain()
    {
        DOTween.KillAll();
        StartRunOnLoad = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Handles cosmetic outfit changes from the wardrobe.
    /// </summary>
    private void HandleOutfitEquipped(int outfitID)
    {
        // Cosmetic only for now
    }

    private void OnTransitionFinished()
    {
        // Transition complete hook
    }

    #endregion

    #region Game Over / Win

    private void HandleBatteryDrained() => HandleGameOver("Battery");
    private void HandleStomachFull() => HandleGameOver("Stomach");

    /// <summary>
    /// Triggers the terminal Game Over state, halting sequences and showing results.
    /// </summary>
    private void HandleGameOver(string reason)
    {
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        TransitionPlayer.Instance?.SkipTransition();
        AudioManager.Instance?.StopAllSFX();
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.GameOver);
        PlayGameOverEffects(reason);

        UnifiedHubManager.Instance?.EnterGameOverMode();
        ChangeState(GameState.GameOver);
        UIManager.Instance?.ShowGameOver(accumulatedEidia);
    }

    /// <summary>
    /// Triggers the terminal Win state, halting sequences and showing results.
    /// </summary>
    public void WinGame(bool isHouse4Clear = false)
    {
        if (currentState == GameState.GameOver || currentState == GameState.Win) return;

        TransitionPlayer.Instance?.SkipTransition();
        AudioManager.Instance?.StopAllSFX();
        AudioManager.Instance?.PlaySFX(AudioManager.SFXType.Win);

        UnifiedHubManager.Instance?.EnterWinMode();
        ChangeState(GameState.Win);
        UIManager.Instance?.ShowWin(accumulatedEidia);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Triggers visual and audio feedback for correct/incorrect actions.
    /// </summary>
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

    /// <summary>
    /// Triggers haptics and screen shake associated with game-over reasons.
    /// </summary>
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
