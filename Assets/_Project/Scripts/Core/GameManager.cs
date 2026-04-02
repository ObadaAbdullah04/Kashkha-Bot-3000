using System;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

/// <summary>
/// Core game state machine and encounter loop manager.
/// Handles the 4-House Gauntlet flow including Crossroads decision and House 4 Boss Mode.
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

    #region Inspector Fields - Tunable Values

    [Header("3-House Progression")]
    [Tooltip("Eidia threshold to unlock Crossroads (Win condition)")]
    [SerializeField] private int eidiaToWin = 100;

    [Header("House 4 Boss Mode")]
    [Tooltip("If true, House 4 is optional (Crossroads choice). If false, always play House 4.")]
    [SerializeField] private bool house4IsOptional = true;

    #endregion

    #region Game State

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Wardrobe;

    public static Action<GameState> OnStateChanged;
    public static Action OnRunStarted;
    public GameState CurrentState => currentState;

    #endregion

    #region 4-House Progression

    [Header("Run Progression")]
    [SerializeField] private int currentHouseLevel = 1;
    [SerializeField] private int encounterIndex = 0;
    [SerializeField] private bool isHouse4Active = false;
    [SerializeField] private int currentRunSeed = 0;

    [Header("Encounter Limits Per House")]
    [Tooltip("Number of encounters to load in House 1")]
    [SerializeField] private int encountersPerHouse1 = 5;
    [Tooltip("Number of encounters to load in House 2")]
    [SerializeField] private int encountersPerHouse2 = 6;
    [Tooltip("Number of encounters to load in House 3")]
    [SerializeField] private int encountersPerHouse3 = 7;

    public int CurrentHouseLevel => currentHouseLevel;
    public bool IsHouse4Active => isHouse4Active;
    public int CurrentRunSeed => currentRunSeed;

    #endregion

    #region Encounter Loop

    [Header("Encounter Loop")]
    [SerializeField] private EncounterData currentEncounter;
    public EncounterData CurrentEncounter => currentEncounter;
    [SerializeField] private bool isQTEActive = false;
    [SerializeField] private bool isProcessingChoice = false;

    #endregion

    #region Run Stats

    [Header("Run Stats")]
    [SerializeField] private int accumulatedEidia = 0;
    [SerializeField] private int accumulatedScrap = 0;

    public int AccumulatedEidia => accumulatedEidia;
    public int AccumulatedScrap => accumulatedScrap;
    public int EidiaToWin => eidiaToWin;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        QTEController.OnQTEResolved += HandleQTEResult;
        MeterManager.OnBatteryDrained += HandleBatteryDrained;
        MeterManager.OnStomachFull += HandleStomachFull;
        TimerController.OnTimeRanOut += HandleTimeRanOut;
        MeterManager.OnOfferAccepted += HandleOfferAccepted;
    }

    private void OnDisable()
    {
        QTEController.OnQTEResolved -= HandleQTEResult;
        MeterManager.OnBatteryDrained -= HandleBatteryDrained;
        MeterManager.OnStomachFull -= HandleStomachFull;
        TimerController.OnTimeRanOut -= HandleTimeRanOut;
        MeterManager.OnOfferAccepted -= HandleOfferAccepted;
    }

    #endregion

    #region State Management

    public void ChangeState(GameState newState)
    {
        GameState previousState = currentState;
        currentState = newState;
        Debug.Log($"[GameManager] State: {previousState} → {currentState}");
        OnStateChanged?.Invoke(currentState);
    }

    #endregion

    #region Run Lifecycle

    /// <summary>
    /// Starts a new run from House 1. Called from Wardrobe UI.
    /// </summary>
    public void StartRun()
    {
        currentHouseLevel = 1;
        encounterIndex = 0;
        isHouse4Active = false;
        isQTEActive = false;
        isProcessingChoice = false;
        accumulatedEidia = 0;
        accumulatedScrap = 0;
        
        // NEW: Generate run seed for shuffle reproducibility
        currentRunSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        Debug.Log($"[GameManager] Run Seed: {currentRunSeed}");

        // Apply outfit bonuses before resetting meters
        if (TimerController.Instance != null)
            TimerController.Instance.ApplyOutfitBonus();

        if (MeterManager.Instance != null)
            MeterManager.Instance.ResetMeters();

        OnRunStarted?.Invoke();
        ChangeState(GameState.Encounter);
        StartHouse(1);
    }

    /// <summary>
    /// Initializes a new house. Resets strike counter and loads first encounter.
    /// </summary>
    public void StartHouse(int houseLevel)
    {
        currentHouseLevel = houseLevel;
        encounterIndex = 0;

        if (MeterManager.Instance != null)
            MeterManager.Instance.ResetHouseCounters();

        Debug.Log($"[GameManager] Starting House {currentHouseLevel}!");

        // Small delay before first encounter
        DOTween.Sequence()
            .AppendInterval(1f)
            .OnComplete(() =>
            {
                ChangeState(GameState.Encounter);
                LoadNextEncounter();
            });
    }

    #endregion

    #region Encounter Loop

    private void LoadNextEncounter()
    {
        isProcessingChoice = false;

        if (DataManager.Instance == null || DataManager.Instance.allEncounters.Count == 0)
        {
            Debug.LogError("[GameManager] No encounters available!");
            return;
        }

        // Get encounters for current house (SHUFFLE - no ordering)
        var houseEncounters = DataManager.Instance.allEncounters
            .Where(e => e.HouseLevel == currentHouseLevel)
            .ToList();

        if (houseEncounters.Count == 0)
        {
            Debug.LogError($"[GameManager] No encounters for House {currentHouseLevel}!");
            EndHouse();
            return;
        }

        // SHUFFLE: Fisher-Yates algorithm with run seed
        System.Random rng = new System.Random(currentRunSeed);
        int n = houseEncounters.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var temp = houseEncounters[k];
            houseEncounters[k] = houseEncounters[n];
            houseEncounters[n] = temp;
        }
        
        // TAKE: Only load X encounters per house (configurable)
        int encountersToLoad = GetEncountersPerHouse(currentHouseLevel);
        var selectedEncounters = houseEncounters.Take(encountersToLoad).ToList();

        // Check if we've completed all selected encounters
        if (encounterIndex >= selectedEncounters.Count)
        {
            EndHouse();
            return;
        }

        // Load encounter
        currentEncounter = selectedEncounters[encounterIndex];

        Debug.Log($"[GameManager] House {currentHouseLevel} | Encounter {encounterIndex + 1}/{selectedEncounters.Count}: {currentEncounter.QuestionAR} (Type: {currentEncounter.EncounterType}, Seed: {currentRunSeed})");

        // Branch based on encounter type
        if (currentEncounter.EncounterType == EncounterType.HospitalityOffer)
        {
            StartHospitalityOffer();
        }
        else
        {
            StartTriviaEncounter();
        }
    }

    private void StartTriviaEncounter()
    {
        // Display 3-choice trivia question
        UIManager.Instance.DisplayEncounter(currentEncounter);
        
        if (TimerController.Instance != null)
            TimerController.Instance.StartTimer(currentHouseLevel);
        
        ChangeState(GameState.Encounter);
    }

    private void StartHospitalityOffer()
    {
        // Display hospitality offer (food/drink pressure)
        UIManager.Instance.DisplayEncounter(currentEncounter);

        // Check if this offer has a QTE
        if (!string.IsNullOrEmpty(currentEncounter.QTEType) && currentEncounter.QTEType != "None")
        {
            StartQTESequence(currentEncounter.QTEType);
        }
        else
        {
            // No QTE: Just display choices and start timer
            if (TimerController.Instance != null)
                TimerController.Instance.StartTimer(currentHouseLevel);
        }

        ChangeState(GameState.Encounter);
    }
    
    /// <summary>
    /// Gets the number of encounters to load for the current house.
    /// House 1/2/3 use inspector fields, House 4 loads all 8.
    /// </summary>
    private int GetEncountersPerHouse(int houseLevel)
    {
        return houseLevel switch
        {
            1 => encountersPerHouse1,
            2 => encountersPerHouse2,
            3 => encountersPerHouse3,
            4 => 8, // House 4: All encounters (boss mode)
            _ => 5
        };
    }

    #endregion

    #region Choice Processing & Hospitality Strike System

    public void ProcessChoice(int choiceIndex)
    {
        if (isQTEActive || isProcessingChoice)
        {
            Debug.LogWarning("[GameManager] Cannot process choice - QTE active or already processing!");
            return;
        }

        if (currentState != GameState.Encounter && currentState != GameState.House4Boss)
        {
            Debug.LogWarning($"[GameManager] Cannot process choice - wrong state: {currentState}");
            return;
        }

        isProcessingChoice = true;

        if (currentEncounter == null)
        {
            Debug.LogWarning("[GameManager] No current encounter!");
            isProcessingChoice = false;
            return;
        }

        if (TimerController.Instance != null)
            TimerController.Instance.StopTimer();

        bool isCorrect = GetChoiceCorrectness(choiceIndex);
        string feedback = GetChoiceFeedback(choiceIndex);

        Debug.Log($"[GameManager] Choice {choiceIndex}: isCorrect={isCorrect}");

        UIManager.Instance.PlayCardAnimation(choiceIndex, isCorrect);

        // Screen Flash Effect (Phase 4 Juice)
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
                CameraShakeManager.Instance?.ShakeWrongAnswer();
            }
        }
        else if (!isCorrect)
        {
            CameraShakeManager.Instance?.ShakeWrongAnswer();
        }

        UIManager.Instance.ShowFeedback(feedback, isCorrect, () =>
        {
            isProcessingChoice = false;

            // Check if mini-game should trigger after this encounter
            if (currentEncounter.MiniGameAfter && !isHouse4Active)
            {
                Debug.Log($"[GameManager] MiniGameAfter flag set! Starting mini-game after encounter {currentEncounter.ID}");
                if (MiniGameManager.Instance != null)
                {
                    MiniGameManager.Instance.StartCatchGame(currentHouseLevel);
                    return; // Don't increment index yet, mini-game will handle it
                }
                else
                {
                    Debug.LogWarning("[GameManager] MiniGameManager not found! Skipping mini-game.");
                }
            }

            LoadNextEncounter();
        });

        // Handle Hospitality Offer logic
        if (currentEncounter.EncounterType == EncounterType.HospitalityOffer)
        {
            // For Hospitality Offers: Check if player chose to accept (Choice 1 = correct = accepting politely)
            if (isCorrect && choiceIndex == 0)
            {
                // Player accepted the offer - trigger strike system
                if (MeterManager.Instance != null)
                    MeterManager.Instance.RegisterAcceptedOffer();
            }
            else
            {
                // Player refused/deflected - just apply base deltas from CSV
                if (MeterManager.Instance != null)
                {
                    MeterManager.Instance.ModifyBattery(currentEncounter.BatteryDelta);
                    MeterManager.Instance.ModifyStomach(currentEncounter.StomachDelta);
                }
            }
        }
        else
        {
            // Trivia encounter - apply base deltas
            if (MeterManager.Instance != null)
            {
                MeterManager.Instance.ModifyBattery(currentEncounter.BatteryDelta);
                MeterManager.Instance.ModifyStomach(currentEncounter.StomachDelta);
            }
        }

        // Award Eidia based on encounter type and choice
        // Hospitality Offer: Eidia handled by HandleOfferAccepted (with multipliers)
        // Trivia: Always award based on choice correctness
        if (currentEncounter.EncounterType != EncounterType.HospitalityOffer || !isCorrect)
        {
            // Not a hospitality offer, OR player refused the offer - award base Eidia
            accumulatedEidia += currentEncounter.EidiaReward;
            accumulatedScrap += currentEncounter.ScrapReward;
            Debug.Log($"[GameManager] +{currentEncounter.EidiaReward} Eidia, +{currentEncounter.ScrapReward} Scrap. Total Eidia: {accumulatedEidia}");
        }
        // Note: If HospitalityOffer + isCorrect, HandleOfferAccepted will add Eidia with multipliers

        // Check for win condition (only in normal houses, not House 4)
        if (accumulatedEidia >= eidiaToWin && !isHouse4Active && house4IsOptional)
        {
            Debug.Log("[GameManager] 100 Eidia threshold reached! Crossroads unlocked.");
            // Don't end run yet - player continues house, then chooses at Crossroads
        }

        encounterIndex++;
    }

    /// <summary>
    /// Called by MeterManager when player accepts a hospitality offer.
    /// Applies strike-based multipliers to Eidia, Stomach, and Battery.
    /// </summary>
    private void HandleOfferAccepted(HospitalityStrike strike)
    {
        if (currentEncounter == null) return;

        // Get multipliers from MeterManager
        float eidiaMult = MeterManager.Instance.GetEidiaMultiplier(strike);
        float stomachMult = MeterManager.Instance.GetStomachMultiplier(strike);
        float batteryDrain = MeterManager.Instance.GetBatteryDrain(strike);

        // Apply stomach multiplier
        if (currentEncounter.StomachDelta > 0 && stomachMult > 0)
        {
            float adjustedStomach = currentEncounter.StomachDelta * stomachMult;
            MeterManager.Instance.ModifyStomach(adjustedStomach);
            Debug.Log($"[GameManager] Hospitality Strike {strike}: Stomach +{adjustedStomach:F0} (base: {currentEncounter.StomachDelta}, mult: {stomachMult})");
        }

        // Apply battery drain
        if (batteryDrain > 0)
        {
            MeterManager.Instance.ModifyBattery(-batteryDrain);
            Debug.Log($"[GameManager] Hospitality Strike {strike}: Battery -{batteryDrain:F0}");
        }

        // Apply Eidia multiplier (1st/2nd strike = full reward, 3rd strike = no reward)
        if (eidiaMult > 0 && currentEncounter.EidiaReward > 0)
        {
            int adjustedEidia = Mathf.RoundToInt(currentEncounter.EidiaReward * eidiaMult);
            accumulatedEidia += adjustedEidia;
            accumulatedScrap += currentEncounter.ScrapReward; // Scrap always awarded
            Debug.Log($"[GameManager] Hospitality Strike {strike}: Eidia +{adjustedEidia} (base: {currentEncounter.EidiaReward}, mult: {eidiaMult}), +{currentEncounter.ScrapReward} Scrap");
        }
        else
        {
            // 3rd strike - no Eidia but still get scrap
            accumulatedScrap += currentEncounter.ScrapReward;
            Debug.Log($"[GameManager] Hospitality Strike Third: NO EIDIA (exhausted!), +{currentEncounter.ScrapReward} Scrap");
        }
    }

    #endregion

    #region QTE Handling

    private void StartQTESequence(string qteType)
    {
        isQTEActive = true;
        ChangeState(GameState.QTE);
        
        // Get QTE parameters from current encounter
        string inputType = currentEncounter.QTEInputType;
        int count = currentEncounter.QTECount;
        float timeLimit = currentEncounter.QTETimeLimit;
        string direction = currentEncounter.QTEDirection;
        float holdDuration = currentEncounter.QTEHoldDuration;
        
        // Fallback to legacy QTEType mapping if QTEInputType is empty
        if (string.IsNullOrEmpty(inputType) || inputType == "_")
        {
            // Map legacy types to new input types
            inputType = qteType.ToLower() switch
            {
                "coffeerefuse" => "Shake",
                "handonheart" => "Tap",
                "tugofwar" => "Swipe",
                _ => "Shake"
            };
            
            // Set defaults for legacy encounters
            if (count == 0) count = inputType == "Shake" ? 1 : (inputType == "Swipe" ? 2 : 1);
            if (timeLimit == 0) timeLimit = 3f;
            if (string.IsNullOrEmpty(direction) || direction == "_") direction = "Up";
            if (holdDuration == 0) holdDuration = 2f;
        }
        
        // Get Arabic instruction based on QTE input type
        string instructionAR = GetQTEInstructionAR(inputType);
        
        UIManager.Instance.ShowQTEWarning(instructionAR);

        if (QTEController.Instance != null)
            QTEController.Instance.StartQTE(inputType, count, timeLimit, direction, holdDuration);
    }
    
    /// <summary>
    /// Gets Arabic instruction text for QTE warning based on input type.
    /// </summary>
    private string GetQTEInstructionAR(string inputType)
    {
        return inputType.ToLower() switch
        {
            "shake" => "هز الجوال",
            "tap" => "اضغط بسرعة",
            "swipe" => "اسحب للأعلى",
            "hold" => "اضغط واستمر",
            _ => "انتبه!"
        };
    }

    private void HandleQTEResult(bool success)
    {
        isQTEActive = false;
        UIManager.Instance.HideQTEWarning();

        // QTE Success = avoided eating (0 Stomach), small battery drain
        // QTE Failure = forced to eat (+Stomach), more battery drain
        if (success)
        {
            // Success: Avoided the food, minimal penalty
            if (MeterManager.Instance != null)
                MeterManager.Instance.ModifyBattery(-5f); // Small drain for physical effort
            Debug.Log("[GameManager] QTE Success! Avoided food, -5 Battery");

            if (HapticFeedback.Instance != null)
                HapticFeedback.Instance.LightTap();
        }
        else
        {
            // Failure: Forced to eat the offered food
            if (MeterManager.Instance != null)
            {
                MeterManager.Instance.ModifyBattery(-10f); // More drain for awkwardness
                MeterManager.Instance.ModifyStomach(currentEncounter.StomachDelta); // Fill stomach
            }
            Debug.LogWarning($"[GameManager] QTE Failed! Forced to eat, -10 Battery, +{currentEncounter.StomachDelta} Stomach");

            UIManager.Instance.ShakeQTEFail();

            if (HapticFeedback.Instance != null)
                HapticFeedback.Instance.HeavyVibration();
        }

        // Check if game over triggered from QTE
        if (currentState == GameState.GameOver)
            return;

        // Continue to next encounter after QTE resolves
        // Check if mini-game should trigger after this encounter
        if (currentEncounter.MiniGameAfter && !isHouse4Active)
        {
            Debug.Log($"[GameManager] MiniGameAfter flag set! Starting mini-game after QTE");
            if (MiniGameManager.Instance != null)
            {
                MiniGameManager.Instance.StartCatchGame(currentHouseLevel);
                return;
            }
        }

        encounterIndex++;
        LoadNextEncounter();
    }

    #endregion

    #region House Completion & Mini-Game

    /// <summary>
    /// Called when all encounters in a house are complete.
    /// Triggers mini-game or Crossroads evaluation.
    /// </summary>
    private void EndHouse()
    {
        if (isHouse4Active)
        {
            // House 4 complete = instant win
            Debug.Log("[GameManager] House 4 cleared! INSANE MODE COMPLETE!");
            WinGame(isHouse4Clear: true);
            return;
        }

        if (currentHouseLevel >= 3)
        {
            // After House 3, evaluate Crossroads
            Debug.Log("[GameManager] House 3 complete! Evaluating Crossroads...");
            EvaluateCrossroads();
            return;
        }

        // Houses 1 or 2: Start mini-game
        Debug.Log($"[GameManager] House {currentHouseLevel} complete! Starting mini-game...");
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.StartCatchGame(currentHouseLevel);
        }
        else
        {
            Debug.LogWarning("[GameManager] MiniGameManager not found! Skipping mini-game.");
            StartNextHouse();
        }
    }

    /// <summary>
    /// Called by MiniGameManager when the catch mini-game ends.
    /// Increments house level and resumes encounters.
    /// </summary>
    public void OnMiniGameComplete(int eidiaEarned, int scrapEarned)
    {
        accumulatedEidia += eidiaEarned;
        accumulatedScrap += scrapEarned;

        Debug.Log($"[GameManager] Mini-game rewards: +{eidiaEarned} Eidia, +{scrapEarned} Scrap. Total Eidia: {accumulatedEidia}");

        StartNextHouse();
    }

    /// <summary>
    /// Increments house level and starts next house.
    /// </summary>
    public void StartNextHouse()
    {
        currentHouseLevel++;
        StartHouse(currentHouseLevel);
    }

    #endregion

    #region Crossroads Decision

    /// <summary>
    /// Shows Crossroads UI panel with Escape/Risk choice.
    /// Called after House 3 completion.
    /// </summary>
    public void EvaluateCrossroads()
    {
        ChangeState(GameState.Crossroads);
        
        Debug.Log($"[GameManager] CROSSROADS! Current Eidia: {accumulatedEidia}/{eidiaToWin}");
        
        // Show Crossroads UI panel
        if (UIManager.Instance != null)
            UIManager.Instance.ShowCrossroadsPanel(accumulatedEidia >= eidiaToWin);
    }

    /// <summary>
    /// Player chooses to escape with current Eidia (Win).
    /// </summary>
    public void ChooseEscape()
    {
        Debug.Log("[GameManager] Player chose ESCAPE! Banking Eidia...");
        WinGame(isHouse4Clear: false);
    }

    /// <summary>
    /// Player chooses to risk House 4 (Boss Mode).
    /// Immediately enters House 4 with current battery/stomach state.
    /// </summary>
    public void ChooseRiskHouse4()
    {
        Debug.Log("[GameManager] Player chose RISK HOUSE 4! Insane mode activated...");
        
        isHouse4Active = true;
        
        if (MeterManager.Instance != null)
            MeterManager.Instance.EnableHouse4Mode();
        
        StartHouse4();
    }

    #endregion

    #region House 4 Boss Mode

    /// <summary>
    /// Starts House 4 Boss Mode with custom encounter list.
    /// </summary>
    private void StartHouse4()
    {
        ChangeState(GameState.House4Boss);
        
        Debug.Log("[GameManager] HOUSE 4 BOSS MODE STARTED! Fast timers, double penalties!");
        
        DOTween.Sequence()
            .AppendInterval(1.5f)
            .OnComplete(() =>
            {
                encounterIndex = 0;
                LoadNextEncounter();
            });
    }

    #endregion

    #region Game Over / Win

    private void HandleBatteryDrained() => HandleGameOver("Battery");
    private void HandleStomachFull() => HandleGameOver("Stomach");

    private void HandleTimeRanOut()
    {
        if (currentState != GameState.Encounter && currentState != GameState.House4Boss) return;
        if (isProcessingChoice) return;

        Debug.Log("[GameManager] Time ran out for encounter!");

        // Treat as wrong answer (penalty)
        if (MeterManager.Instance != null)
            MeterManager.Instance.ModifyBattery(-15f);

        CameraShakeManager.Instance?.ShakeWrongAnswer();

        UIManager.Instance.ShowFeedback("تأخرت كثير! نقصت البطارية", false, () =>
        {
            LoadNextEncounter();
        });
    }

    private void HandleGameOver(string reason)
    {
        if (TimerController.Instance != null)
            TimerController.Instance.StopTimer();

        UIManager.Instance.HideQTEWarning();
        isQTEActive = false;

        string message = reason switch
        {
            "Battery" => "Game Over: Social Shutdown",
            "Stomach" => "Game Over: Ma'amoul Explosion",
            _ => "Game Over!"
        };

        Debug.Log($"[GameManager] {message}");

        if (SaveManager.Instance != null)
            SaveManager.Instance.AddRunRewards(accumulatedScrap, accumulatedEidia);

        if (reason == "Stomach")
        {
            UIManager.Instance.ShakeMaamoulExplosion();

            if (HapticFeedback.Instance != null)
                HapticFeedback.Instance.ExplosionVibration();
        }
        else
        {
            UIManager.Instance.ShakeSocialShutdown();

            if (HapticFeedback.Instance != null)
                HapticFeedback.Instance.HeavyVibration();
        }

        if (URPPostProcessing.Instance != null)
            URPPostProcessing.Instance.EnableGameOverEffect();

        ChangeState(GameState.GameOver);
    }

    /// <summary>
    /// Triggers Win state. isHouse4Clear = true if player cleared House 4 (bonus).
    /// </summary>
    public void WinGame(bool isHouse4Clear = false)
    {
        if (TimerController.Instance != null)
            TimerController.Instance.StopTimer();

        string winMessage = isHouse4Clear 
            ? "INSANE MODE CLEAR! You survived House 4!" 
            : "You survived Eid! Congratulations!";

        Debug.Log($"[GameManager] {winMessage}");

        if (SaveManager.Instance != null)
            SaveManager.Instance.AddRunRewards(accumulatedScrap, accumulatedEidia);

        ChangeState(GameState.Win);
    }

    #endregion

    #region Helper Methods

    private bool GetChoiceCorrectness(int choiceIndex)
    {
        return choiceIndex switch
        {
            0 => currentEncounter.Choice1IsCorrect,
            1 => currentEncounter.Choice2IsCorrect,
            2 => currentEncounter.Choice3IsCorrect,
            _ => false
        };
    }

    private string GetChoiceFeedback(int choiceIndex)
    {
        return choiceIndex switch
        {
            0 => currentEncounter.Choice1Feedback,
            1 => currentEncounter.Choice2Feedback,
            2 => currentEncounter.Choice3Feedback,
            _ => ""
        };
    }

    #endregion

    #region Inspector Test Buttons

    [Button("▶ Start New Run")]
    private void TestStartRun() => StartRun();

    [Button("⚠ Trigger Battery Game Over")]
    private void TestBatteryGameOver() => HandleGameOver("Battery");

    [Button("⚠ Trigger Stomach Game Over")]
    private void TestStomachGameOver() => HandleGameOver("Stomach");

    [Button("✓ Trigger Win")]
    private void TestWin() => WinGame();

    [Button("☠️ Start House 4")]
    private void TestHouse4() => StartHouse4();

    [Button("🔄 Evaluate Crossroads")]
    private void TestCrossroads() => EvaluateCrossroads();

    [Button("→ Force Next House")]
    private void TestNextHouse() => StartNextHouse();

    #endregion
}
