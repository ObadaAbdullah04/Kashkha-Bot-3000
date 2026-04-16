using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Text.RegularExpressions;
using System.Globalization;

/// <summary>
/// PHASE 17: Parses CSV files into data pools.
///
/// CSV FILES:
/// 1. Questions2.csv (13 columns): ID, HouseLevel, Speaker, SpriteName, Question, OptionCorrect, OptionWrong, CorrectSide,
///    CorrectFB, IncorrectFB, CorrectBat, IncorrectBat, BaseEid
/// 2. Interactions.csv (12 columns): ID, HouseLevel, InteractionType, PromptTextAR, Duration, Threshold, CorrectBat, IncorrectBat, CorrectStomach, IncorrectStomach, CorrectEid, IncorrectEid
///
/// CHANGES FROM PHASE 15:
/// - Removed CardName column from Questions CSV (now uses Speaker as CardName)
/// - Updated column indices: 14 columns → 13 columns
///
/// CINEMATICS:
/// - Cinematics are NOT loaded from CSV — they are defined in Unity (Timeline assets) or via DOTween
/// - CinematicData objects are created manually or loaded from ScriptableObjects
/// - HouseSequenceData ScriptableObject defines element order
/// - No shuffling or randomization - sequence is explicit
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("CSV Files")]
    [Tooltip("Questions CSV (Questions2.csv)")]
    public TextAsset questionsCSV;

    [Tooltip("Interactions CSV (Interactions.csv)")]
    public TextAsset interactionsCSV;

    [Tooltip("Cinematics CSV (Cinematics.csv)")]
    public TextAsset cinematicsCSV;

    [Header("Cinematics (Unity Timeline / DOTween)")]
    [Tooltip("Character data pool for looking up speakers in cinematics")]
    [SerializeField] private List<CharacterExpressionSO> characterDataPool = new List<CharacterExpressionSO>();

    [Tooltip("Pre-defined cinematics (optional — can also be loaded from Resources)")]
    [SerializeField] private CinematicData[] preDefinedCinematics;

    [Header("Parsed Data")]
    [ReadOnly]
    [Tooltip("Questions pooled by HouseLevel")]
    public Dictionary<int, List<SwipeCardData>> questionPoolsByHouse = new Dictionary<int, List<SwipeCardData>>();

    [ReadOnly]
    [Tooltip("Cinematics by ID")]
    public Dictionary<string, CinematicData> cinematicByID = new Dictionary<string, CinematicData>();

    [ReadOnly]
    [Tooltip("Interactions pooled by HouseLevel")]
    public Dictionary<int, List<InteractionData>> interactionPoolsByHouse = new Dictionary<int, List<InteractionData>>();

    // Questions CSV Column indices (13 columns)
    private const int Q_COL_ID = 0;
    private const int Q_COL_HOUSE_LEVEL = 1;
    private const int Q_COL_SPEAKER = 2;
    private const int Q_COL_SPRITE_NAME = 3;
    private const int Q_COL_QUESTION = 4;
    private const int Q_COL_OPTION_CORRECT = 5;
    private const int Q_COL_OPTION_WRONG = 6;
    private const int Q_COL_CORRECT_SIDE = 7;
    private const int Q_COL_CORRECT_FB = 8;
    private const int Q_COL_INCORRECT_FB = 9;
    private const int Q_COL_CORRECT_BAT = 10;
    private const int Q_COL_INCORRECT_BAT = 11;
    private const int Q_COL_BASE_EID = 12;
    private const int Q_TOTAL_COLS = 13;

    // Interactions CSV Column indices (12 columns - PHASE 14 updated with stomach)
    private const int INT_COL_ID = 0;
    private const int INT_COL_HOUSE_LEVEL = 1;
    private const int INT_COL_INTERACTION_TYPE = 2;
    private const int INT_COL_PROMPT_TEXT = 3;
    private const int INT_COL_DURATION = 4;
    private const int INT_COL_THRESHOLD = 5;
    private const int INT_COL_CORRECT_BAT = 6;
    private const int INT_COL_INCORRECT_BAT = 7;
    private const int INT_COL_CORRECT_STOMACH = 8;
    private const int INT_COL_INCORRECT_STOMACH = 9;
    private const int INT_COL_CORRECT_EID = 10;
    private const int INT_COL_INCORRECT_EID = 11;
    private const int INT_COL_SPEAKER = 12;
    private const int INT_COL_SUCCESS_EXPR = 13;
    private const int INT_COL_FAILURE_EXPR = 14;
    private const int INT_TOTAL_COLS = 15;

    // Cinematics CSV Column indices (10 columns)
    private const int C_COL_ID = 0;
    private const int C_COL_HOUSE_LEVEL = 1;
    private const int C_COL_TYPE = 2; // 0=Timeline, 1=DOTween
    private const int C_COL_TIMELINE = 3;
    private const int C_COL_DURATION = 4;
    private const int C_COL_TEXT = 5;
    private const int C_COL_ANIMATION = 6;
    private const int C_COL_SPEAKER = 7;
    private const int C_COL_EXPRESSION = 8;
    private const int C_COL_RESOURCE = 9;
    private const int C_TOTAL_COLS = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            
            // Automatically find character data if pool is empty
            if (characterDataPool == null || characterDataPool.Count == 0)
            {
                LoadCharactersFromResources();
            }
            
            ParseAllCSVs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadCharactersFromResources()
    {
        characterDataPool.Clear();
        var characters = Resources.LoadAll<CharacterExpressionSO>("Characters");
        if (characters != null && characters.Length > 0)
        {
            characterDataPool.AddRange(characters);
            // Debug.Log($"[DataManager] Auto-loaded {characters.Length} characters from Resources/Characters/");
        }
        else
        {
            // Debug.LogWarning("[DataManager] No characters found in Resources/Characters/!");
        }
    }

    [Button("Parse All CSVs")]
    public void ParseAllCSVs()
    {
        questionPoolsByHouse.Clear();
        cinematicByID.Clear();
        interactionPoolsByHouse.Clear();

        ParseQuestionsCSV();
        LoadCinematics();
        ParseCinematicsCSV(); // New CSV loader
        ParseInteractionsCSV();

        // Debug.Log("[DataManager] All CSVs parsed!");
        PrintSummary();
    }

    [Button("Parse Cinematics CSV")]
    private void ParseCinematicsCSV()
    {
        if (cinematicsCSV == null)
        {
            // Debug.Log("[DataManager] No Cinematics CSV assigned. Skipping.");
            return;
        }

        string[] lines = cinematicsCSV.text.Split('\n');
        int parsed = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i].Trim(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            if (fields.Length < C_TOTAL_COLS) continue;

            CinematicData cinematic = ParseCinematic(fields, i + 1);
            if (cinematic != null)
            {
                cinematicByID[cinematic.ID] = cinematic;
                parsed++;
            }
        }

        // Debug.Log($"[DataManager] ✅ Cinematics CSV: {parsed} parsed from file");
    }

    private CinematicData ParseCinematic(string[] fields, int line)
    {
        try
        {
            string id = fields[C_COL_ID].Trim();
            if (string.IsNullOrEmpty(id)) return null;

            CinematicData data = new CinematicData();
            data.ID = id;
            data.HouseLevel = int.Parse(fields[C_COL_HOUSE_LEVEL], CultureInfo.InvariantCulture);
            
            // Type: 0=Timeline, 1=DOTween
            int typeInt = int.Parse(fields[C_COL_TYPE], CultureInfo.InvariantCulture);
            data.Type = (CinematicType)typeInt;

            data.TimelineAssetName = fields[C_COL_TIMELINE].Trim();
            data.Duration = float.Parse(fields[C_COL_DURATION], CultureInfo.InvariantCulture);
            data.TextAR = fields[C_COL_TEXT].Trim().Replace("\"", "");

            // Animation enum
            if (int.TryParse(fields[C_COL_ANIMATION], out int animInt))
                data.Animation = (AnimationType)animInt;

            // Speaker lookup
            string speakerName = fields[C_COL_SPEAKER].Trim();
            if (!string.IsNullOrEmpty(speakerName))
                data.Speaker = GetSpeakerByName(speakerName);

            data.Expression = fields[C_COL_EXPRESSION].Trim();
            data.ResourceImageName = fields[C_COL_RESOURCE].Trim();

            return data;
        }
        catch (Exception)
        {
            // Debug.LogError($"[DataManager] Error parsing cinematic at line {line}: {e.Message}");
            return null;
        }
    }

    public CharacterExpressionSO GetSpeakerByName(string name)
    {
        if (characterDataPool == null || characterDataPool.Count == 0)
        {
            // Try emergency load if pool is empty
            LoadCharactersFromResources();
        }

        if (characterDataPool == null) return null;

        return characterDataPool.Find(s => s != null && 
            (s.characterName.Equals(name, StringComparison.OrdinalIgnoreCase) || 
             s.name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
             s.name.EndsWith(name, StringComparison.OrdinalIgnoreCase)));
    }

    [Button("Parse Questions")]
    private void ParseQuestionsCSV()
    {
        questionPoolsByHouse.Clear();

        if (questionsCSV == null)
        {
            // Debug.LogError("[DataManager] ⚠️ No Questions CSV assigned! Please assign Questions.csv in inspector.");
            return;
        }

        // Debug.Log($"[DataManager] Parsing Questions CSV: {questionsCSV.name} ({questionsCSV.text.Length} bytes)");

        string[] lines = questionsCSV.text.Split('\n');
        if (lines.Length < 2)
        {
            // Debug.LogError("[DataManager] Questions CSV is empty or has no data rows!");
            return;
        }

        int parsed = 0, skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i].Trim(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            SwipeCardData question = ParseQuestion(fields, i + 1);

            if (question != null)
            {
                if (!questionPoolsByHouse.ContainsKey(question.HouseLevel))
                    questionPoolsByHouse[question.HouseLevel] = new List<SwipeCardData>();

                questionPoolsByHouse[question.HouseLevel].Add(question);
                parsed++;
            }
            else
            {
                skipped++;
            }
        }

        // Debug.Log($"[DataManager] ✅ Questions: {parsed} parsed, {skipped} skipped");
    }

    /// <summary>
    /// Loads cinematics from inspector array (not CSV).
    /// Cinematics are either pre-defined here or loaded from Timeline assets at runtime.
    /// </summary>
    private void LoadCinematics()
    {
        cinematicByID.Clear();

        if (preDefinedCinematics == null || preDefinedCinematics.Length == 0)
        {
            // Debug.Log("[DataManager] No pre-defined cinematics. Using Unity Timeline assets directly.");
            return;
        }

        foreach (var cinematic in preDefinedCinematics)
        {
            if (cinematic != null && !string.IsNullOrEmpty(cinematic.ID))
            {
                cinematicByID[cinematic.ID] = cinematic;
            }
        }

        // Debug.Log($"[DataManager] Cinematics: {cinematicByID.Count} loaded from inspector");
    }

    [Button("Parse Interactions")]
    private void ParseInteractionsCSV()
    {
        interactionPoolsByHouse.Clear();

        if (interactionsCSV == null)
        {
            // Debug.LogWarning("[DataManager] ⚠️ No Interactions CSV assigned!");
            return;
        }

        string[] lines = interactionsCSV.text.Split('\n');
        if (lines.Length < 2)
        {
            // Debug.LogError("[DataManager] Interactions CSV is empty!");
            return;
        }

        int parsed = 0, skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i].Trim(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            InteractionData interaction = ParseInteraction(fields, i + 1);

            if (interaction != null)
            {
                if (!interactionPoolsByHouse.ContainsKey(interaction.HouseLevel))
                    interactionPoolsByHouse[interaction.HouseLevel] = new List<InteractionData>();

                interactionPoolsByHouse[interaction.HouseLevel].Add(interaction);
                parsed++;
            }
            else
            {
                skipped++;
            }
        }

        // Debug.Log($"[DataManager] Interactions: {parsed} parsed, {skipped} skipped");
    }

    [Button("Preview All Data")]
    public void PreviewData()
    {
        // Debug.Log("=== QUESTION POOLS ===");
        foreach (var kvp in questionPoolsByHouse)
        {
            // Debug.Log($"House {kvp.Key}: {kvp.Value.Count} questions");
            foreach (var q in kvp.Value)
            {
                // Debug.Log($"  [{q.CardName}] \"{q.QuestionAR}\"");
                // Debug.Log($"    Correct: {q.OptionCorrectAR} | Wrong: {q.OptionWrongAR}");
                // Debug.Log($"    CorrectSide: {(q.RightIsCorrect ? "RIGHT" : "LEFT")} | BaseEid: {q.BaseEid}");
            }
        }

        // Debug.Log("=== CINEMATICS ===");
        foreach (var kvp in cinematicByID)
        {
            // Debug.Log($"  [{kvp.Key}] Type:{kvp.Value.Type} | {(kvp.Value.Type == CinematicType.UnityTimeline ? kvp.Value.TimelineAssetName : kvp.Value.TextAR)}");
        }

        // Debug.Log("=== INTERACTION POOLS ===");
        foreach (var kvp in interactionPoolsByHouse)
        {
            // Debug.Log($"House {kvp.Key}: {kvp.Value.Count} interactions");
            foreach (var i in kvp.Value)
            {
                // Debug.Log($"  [{i.ID}] Type:{i.InteractionType} | Prompt:\"{i.PromptTextAR}\" | Duration:{i.Duration}s | Threshold:{i.Threshold}");
            }
        }
        // Debug.Log("=== END PREVIEW ===");
    }

    private SwipeCardData ParseQuestion(string[] fields, int row)
    {
        if (fields.Length < Q_TOTAL_COLS)
        {
            // Debug.LogWarning($"[DataManager] Questions Line {row}: Expected {Q_TOTAL_COLS} cols, got {fields.Length}. Line: {string.Join(",", fields)}");
            return null;
        }

        string id = SafeField(fields, Q_COL_ID);
        if (string.IsNullOrWhiteSpace(id))
        {
            // Debug.LogWarning($"[DataManager] Questions Line {row}: Empty ID");
            return null;
        }

        string question = SafeField(fields, Q_COL_QUESTION);
        if (string.IsNullOrWhiteSpace(question) || question == "_")
        {
            // Debug.Log($"[DataManager] Questions Line {row}: Question skipped (empty)");
            return null;
        }

        int correctSide = ParseInt(SafeField(fields, Q_COL_CORRECT_SIDE));
        bool rightIsCorrect = (correctSide == 1);

        var cardData = new SwipeCardData
        {
            ID = id,
            HouseLevel = ParseInt(SafeField(fields, Q_COL_HOUSE_LEVEL)),
            Speaker = SafeField(fields, Q_COL_SPEAKER),
            CardName = SafeField(fields, Q_COL_SPEAKER), // Use Speaker as CardName
            SpriteName = SafeField(fields, Q_COL_SPRITE_NAME),
            QuestionAR = question,
            OptionCorrectAR = SafeField(fields, Q_COL_OPTION_CORRECT),
            OptionWrongAR = SafeField(fields, Q_COL_OPTION_WRONG),
            RightIsCorrect = rightIsCorrect,
            CorrectFeedbackAR = SafeField(fields, Q_COL_CORRECT_FB),
            IncorrectFeedbackAR = SafeField(fields, Q_COL_INCORRECT_FB),
            CorrectBatteryDelta = ParseFloat(SafeField(fields, Q_COL_CORRECT_BAT)),
            IncorrectBatteryDelta = ParseFloat(SafeField(fields, Q_COL_INCORRECT_BAT)),
            BaseEid = ParseInt(SafeField(fields, Q_COL_BASE_EID))
        };

#if UNITY_EDITOR
        if (row <= 3) {} // Log first 3 questions for verification
        {
            // Debug.Log($"[DataManager] Parsed Q[{id}]: {cardData.CardName} | Sprite:{cardData.SpriteName} | House:{cardData.HouseLevel}");
        }
#endif

        return cardData;
    }

    private InteractionData ParseInteraction(string[] fields, int row)
    {
        if (fields.Length < INT_TOTAL_COLS)
        {
            // Debug.LogWarning($"[DataManager] Interactions Line {row}: Expected {INT_TOTAL_COLS} cols, got {fields.Length}");
            return null;
        }

        string id = SafeField(fields, INT_COL_ID);
        if (string.IsNullOrWhiteSpace(id))
        {
            // Debug.LogWarning($"[DataManager] Interactions Line {row}: Empty ID");
            return null;
        }

        if (!Enum.TryParse<InteractionType>(SafeField(fields, INT_COL_INTERACTION_TYPE), true, out InteractionType interactionType))
        {
            // Debug.LogWarning($"[DataManager] Interactions Line {row}: Invalid InteractionType '{SafeField(fields, INT_COL_INTERACTION_TYPE)}'");
            return null;
        }

        string promptText = SafeField(fields, INT_COL_PROMPT_TEXT);
        if (string.IsNullOrWhiteSpace(promptText) || promptText == "_")
        {
            // Debug.LogWarning($"[DataManager] Interactions Line {row}: Empty PromptText");
            return null;
        }

        float threshold = ParseFloat(SafeField(fields, INT_COL_THRESHOLD));
        // For Draw type, threshold is unused (set to 1 as placeholder)
        if (interactionType == InteractionType.Draw && threshold == 0)
            threshold = 1f;

        return new InteractionData
        {
            ID = id,
            HouseLevel = ParseInt(SafeField(fields, INT_COL_HOUSE_LEVEL)),
            InteractionType = interactionType,
            PromptTextAR = promptText,
            Duration = ParseFloat(SafeField(fields, INT_COL_DURATION)),
            Threshold = threshold,
            CorrectBatteryDelta = ParseFloat(SafeField(fields, INT_COL_CORRECT_BAT)),
            IncorrectBatteryDelta = ParseFloat(SafeField(fields, INT_COL_INCORRECT_BAT)),
            CorrectStomachDelta = ParseFloat(SafeField(fields, INT_COL_CORRECT_STOMACH)),
            IncorrectStomachDelta = ParseFloat(SafeField(fields, INT_COL_INCORRECT_STOMACH)),
            CorrectEid = ParseInt(SafeField(fields, INT_COL_CORRECT_EID)),
            IncorrectEid = ParseInt(SafeField(fields, INT_COL_INCORRECT_EID)),
            SpeakerName = SafeField(fields, INT_COL_SPEAKER),
            SuccessExpression = SafeField(fields, INT_COL_SUCCESS_EXPR),
            FailureExpression = SafeField(fields, INT_COL_FAILURE_EXPR)
        };
    }

    #region Parse Helpers

    private string SafeField(string[] f, int i) => (i >= 0 && i < f.Length) ? f[i].Trim('"').Trim() : "";
    private int ParseInt(string v) { v = v.Trim().Trim('"'); return (v == "_" || string.IsNullOrEmpty(v)) ? 0 : (int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out int r) ? r : 0); }
    private float ParseFloat(string v) { v = v.Trim().Trim('"'); return (v == "_" || string.IsNullOrEmpty(v)) ? 0f : (float.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out float r) ? r : 0f); }

    #endregion

    /// <summary>
    /// Gets a question by ID from the pool.
    /// </summary>
    public SwipeCardData GetQuestionByID(string id)
    {
        foreach (var kvp in questionPoolsByHouse)
        {
            foreach (var q in kvp.Value)
            {
                if (q.ID == id) return q;
            }
        }
        // Debug.LogWarning($"[DataManager] Question not found: {id}");
        return null;
    }

    /// <summary>
    /// Gets a cinematic by ID. Returns pre-defined cinematic or creates wrapper for Timeline asset.
    /// </summary>
    public CinematicData GetCinematicByID(string id)
    {
        // First check pre-defined cinematics
        if (cinematicByID.ContainsKey(id))
            return cinematicByID[id];

        // Otherwise, create a UnityTimeline wrapper
        return new CinematicData
        {
            ID = id,
            HouseLevel = 0,
            Type = CinematicType.UnityTimeline,
            TimelineAssetName = id,
            Duration = 0f
        };
    }

    /// <summary>
    /// Gets an interaction by ID from the pool.
    /// </summary>
    public InteractionData GetInteractionByID(string id)
    {
        foreach (var kvp in interactionPoolsByHouse)
        {
            foreach (var i in kvp.Value)
            {
                if (i.ID == id) return i;
            }
        }
        // Debug.LogWarning($"[DataManager] Interaction not found: {id}");
        return null;
    }

    /// <summary>
    /// Gets all interactions for a house level.
    /// </summary>
    public List<InteractionData> GetInteractionsForHouse(int houseLevel)
    {
        if (!interactionPoolsByHouse.ContainsKey(houseLevel))
        {
            // Debug.LogWarning($"[DataManager] No interactions for House {houseLevel}!");
            return new List<InteractionData>();
        }

        return new List<InteractionData>(interactionPoolsByHouse[houseLevel]);
    }

    /// <summary>
    /// Gets all questions for a house level (no shuffling).
    /// </summary>
    public List<SwipeCardData> GetQuestionsForHouse(int houseLevel)
    {
        if (!questionPoolsByHouse.ContainsKey(houseLevel))
        {
            // Debug.LogWarning($"[DataManager] No questions for House {houseLevel}!");
            return new List<SwipeCardData>();
        }

        return new List<SwipeCardData>(questionPoolsByHouse[houseLevel]);
    }

    private void PrintSummary()
    {
        // Debug.Log("[DataManager] === PARSE SUMMARY ===");
        for (int h = 1; h <= 4; h++)
        {
            int qCount = questionPoolsByHouse.ContainsKey(h) ? questionPoolsByHouse[h].Count : 0;
            int intCount = interactionPoolsByHouse.ContainsKey(h) ? interactionPoolsByHouse[h].Count : 0;
            // Debug.Log($"[DataManager]   House {h}: {qCount} questions, {intCount} interactions");
        }
        // Debug.Log($"[DataManager]   Cinematics: {cinematicByID.Count} pre-defined");
        // Debug.Log("[DataManager] === END SUMMARY ===");
    }

    [Button("Clear Data")]
    public void ClearData()
    {
        questionPoolsByHouse.Clear();
        cinematicByID.Clear();
        interactionPoolsByHouse.Clear();
    }
}
