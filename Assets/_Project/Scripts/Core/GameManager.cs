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

    #endregion

    #region State

    [SerializeField] private GameState currentState = GameState.Wardrobe;
    [SerializeField] private int currentHouseLevel = 1;
    [SerializeField] private bool isHouse4Active = false;
    [SerializeField] private int currentRunSeed = 0;
    [SerializeField] private bool[] completedHouses = new bool[5];
    [SerializeField] private bool house4Unlocked = false;
    [SerializeField] private int accumulatedEidia = 0;
    [SerializeField] private int encounterStreakBonus = 0; // Streak bonus from current encounter

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
        UnifiedHubManager.OnStartNextHouse += EnterHouse;
        UnifiedHubManager.OnStartMiniGame += HandleMiniGameSelected;
        UnifiedHubManager.OnPlayAgain += HandlePlayAgain;
        UnifiedHubManager.OnOutfitEquipped += HandleOutfitEquipped;
        TransitionPlayer.OnTransitionComplete += OnTransitionFinished;
        MeterManager.OnBatteryDrained += HandleBatteryDrained;
        MeterManager.OnStomachFull += HandleStomachFull;
        HouseFlowController.OnHouseCompleted += HandleHouseFlowCompleted;
    }

    private void OnDisable()
    {
        SwipeEncounterManager.OnCardProcessed -= HandleCardProcessed;
        UnifiedHubManager.OnStartNextHouse -= EnterHouse;
        UnifiedHubManager.OnStartMiniGame -= HandleMiniGameSelected;
        UnifiedHubManager.OnPlayAgain -= HandlePlayAgain;
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
        Debug.Log($"[GameManager] State: {previous} → {currentState}");
        OnStateChanged?.Invoke(currentState);
    }

    #endregion

    #region Run Lifecycle

    public void StartRun()
    {
        currentHouseLevel = 1;
        isHouse4Active = false;
        accumulatedEidia = 0;
        encounterStreakBonus = 0;
        completedHouses = new bool[5];
        house4Unlocked = false;
        currentRunSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        Debug.Log($"[GameManager] Run Seed: {currentRunSeed}");

        FloatingTextManager.Instance?.gameObject.SetActive(true);
        MeterManager.Instance?.ResetMeters();

        // Apply equipped outfit stats at run start
        if (WardrobeManager.Instance != null)
            WardrobeManager.Instance.ApplyOutfitBonuses();

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
        MeterManager.Instance?.ResetHouseCounters();

        Debug.Log($"[GameManager] Starting House {currentHouseLevel}!");

        if (TransitionPlayer.Instance != null)
        {
            string text = GetHouseTransitionText(houseLevel);
            TransitionPlayer.Instance.PlayTransition(text, () =>
            {
                ChangeState(GameState.Encounter);
                StartHouseFlowController(houseLevel);
            });
        }
        else
        {
            ChangeState(GameState.Encounter);
            StartHouseFlowController(houseLevel);
        }
    }

    /// <summary>
    /// PHASE 9.6: Starts the self-driving house flow coroutine.
    /// No Timeline needed — HouseFlowController drives itself.
    /// </summary>
    private void StartHouseFlowController(int houseLevel)
    {
        if (HouseFlowController.Instance == null)
        {
            Debug.LogError("[GameManager] HouseFlowController not available! Cannot start house.");
            EndHouse();
            return;
        }

        HouseSequenceData sequence = GetHouseSequenceForLevel(houseLevel);

        if (sequence == null || sequence.Sequence == null || sequence.Sequence.Count == 0)
        {
            Debug.LogError($"[GameManager] No sequence data for House {houseLevel}!");
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
            Debug.LogWarning($"[GameManager] HouseSequenceData not found at '{path}'. Generating test sequence.");
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
            for (int i = 0; i < Mathf.Min(4, questions.Count); i++)
            {
                elements.Add(new SequenceElement(ElementType.Question, questions[i].ID));
            }
        }
        else
        {
            Debug.LogWarning($"[GameManager] No questions in CSV for House {houseLevel}!");
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

    #region Swipe Card Processing

    private void HandleCardProcessed(float batteryDelta, int eidia, bool wasCorrect)
    {
        accumulatedEidia += eidia;

        PlayFeedbackEffects(wasCorrect);

        Debug.Log($"[GameManager] Card: {(wasCorrect ? "CORRECT" : "INCORRECT")} | +{eidia} Eidia");
    }

    /// <summary>
    /// PHASE 9: Called when HouseFlowController completes a house.
    /// </summary>
    private void HandleHouseFlowCompleted(int houseLevel)
    {
        Debug.Log($"[GameManager] House {houseLevel} flow completed via Timeline!");
        
        // Get streak bonus if applicable
        if (SwipeEncounterManager.Instance != null)
        {
            encounterStreakBonus = SwipeEncounterManager.Instance.GetStreakBonus();
            if (encounterStreakBonus > 0)
            {
                accumulatedEidia += encounterStreakBonus;
                Debug.Log($"[GameManager] Streak bonus: +{encounterStreakBonus} Eidia!");
            }
        }

        // House complete - move to next
        EndHouse();
    }

    #endregion

    #region House Completion

    private void EndHouse()
    {
        if (currentHouseLevel >= 1 && currentHouseLevel <= 4)
            completedHouses[currentHouseLevel] = true;

        if (isHouse4Active)
        {
            Debug.Log("[GameManager] House 4 cleared! INSANE MODE COMPLETE!");
            WinGame(isHouse4Clear: true);
            return;
        }

        Debug.Log($"[GameManager] House {currentHouseLevel} complete! Going to Hub...");

        // Mark this house as complete in the Unified Hub
        UnifiedHubManager.Instance?.MarkHouseComplete(currentHouseLevel);

        // Show hub for next action
        ShowUnifiedHub();
    }

    public void OnMiniGameComplete(int eidiaEarned)
    {
        accumulatedEidia += eidiaEarned;
        Debug.Log($"[GameManager] Mini-game: +{eidiaEarned} Eidia");
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
        ChangeState(GameState.HouseHub);
        int next = currentHouseLevel + 1;

        if (currentHouseLevel >= 3)
            house4Unlocked = true;

        UnifiedHubManager.Instance?.InitializeHub(next, completedHouses, house4Unlocked);
        UIManager.Instance?.ShowUnifiedHub();

        Debug.Log($"[GameManager] Unified Hub. Next: {next}, House 4: {house4Unlocked}");
    }

    private void EnterHouse(int houseLevel)
    {
        if (houseLevel > currentHouseLevel + 1 && !completedHouses[houseLevel - 1])
        {
            Debug.LogWarning($"[GameManager] Cannot enter House {houseLevel} - previous not complete!");
            return;
        }

        UIManager.Instance?.HideUnifiedHub();
        StartHouse(houseLevel);
    }

    private void HandleMiniGameSelected(int miniGameIndex)
    {
        Debug.Log($"[GameManager] Mini-game {miniGameIndex + 1} selected from Hub.");
        
        UIManager.Instance?.HideUnifiedHub();
        ChangeState(GameState.InterHouseMiniGame);
        
        // Start the assigned mini-game
        MiniGameManager.Instance?.StartAssignedMiniGame(miniGameIndex);
    }

    private void HandlePlayAgain()
    {
        Debug.Log("[GameManager] Play Again!");
        SaveManager.Instance?.AddRunRewards(accumulatedEidia);
        UIManager.Instance?.HideUnifiedHub();
        StartRun();
    }

    private void HandleOutfitEquipped(int outfitID)
    {
        Debug.Log($"[GameManager] Outfit equipped (ID: {outfitID}). Applying stats...");
        
        // Apply the outfit's stat bonus immediately
        if (WardrobeManager.Instance != null)
            WardrobeManager.Instance.ApplyOutfitBonuses();
    }

    private void OnTransitionFinished()
    {
#if UNITY_EDITOR
        Debug.Log("[GameManager] Transition finished.");
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

        Debug.Log($"[GameManager] {msg}");
        SaveRewardsAndGameOver(reason);
    }

    public void WinGame(bool isHouse4Clear = false)
    {
        string msg = isHouse4Clear
            ? "INSANE MODE CLEAR! You survived House 4!"
            : "You survived Eid! Congratulations!";

        Debug.Log($"[GameManager] {msg}");
        SaveManager.Instance?.AddRunRewards(accumulatedEidia);
        ChangeState(GameState.Win);
    }

    private void SaveRewardsAndGameOver(string reason)
    {
        SaveManager.Instance?.AddRunRewards(accumulatedEidia);
        PlayGameOverEffects(reason);
        ChangeState(GameState.GameOver);
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

    [Button("▶ Start Run")]
    private void TestStartRun() => StartRun();

    [Button("⚠ Battery Game Over")]
    private void TestBatteryGO() => HandleGameOver("Battery");

    [Button("⚠ Stomach Game Over")]
    private void TestStomachGO() => HandleGameOver("Stomach");

    [Button("✓ Win")]
    private void TestWin() => WinGame();

    [Button("☠️ Start House 4")]
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
