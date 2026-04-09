using System;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Text.RegularExpressions;
using System.Globalization;

/// <summary>
/// PHASE 9 REFACTORED: Parses 3 CSV files into data pools.
///
/// CSV FILES:
/// 1. Questions.csv (13 columns): ID, HouseLevel, Speaker, CardName, Question, OptionCorrect, OptionWrong, CorrectSide,
///    CorrectFB, IncorrectFB, CorrectBat, IncorrectBat, BaseEid
/// 2. QTEs.csv (8 columns): ID, HouseLevel, QTEType, Duration, SuccessText, FailText, SuccessBatteryEffect, FailBatteryEffect
/// 3. Cutscenes.csv (6 columns): ID, HouseLevel, CutsceneType, Text, Duration, AnimationType
///
/// KEY CHANGES:
/// - Questions are now pooled by HouseLevel (no wave assignments)
/// - QTEs and Cutscenes are loaded from separate CSVs
/// - HouseSequenceData ScriptableObject defines element order
/// - No shuffling or randomization - sequence is explicit
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("CSV Files")]
    [Tooltip("Questions CSV (Questions.csv)")]
    public TextAsset questionsCSV;
    
    [Tooltip("QTEs CSV (QTEs.csv)")]
    public TextAsset qtesCSV;
    
    [Tooltip("Cutscenes CSV (Cutscenes.csv)")]
    public TextAsset cutscenesCSV;

    [Header("Parsed Data")]
    [ReadOnly]
    [Tooltip("Questions pooled by HouseLevel")]
    public Dictionary<int, List<SwipeCardData>> questionPoolsByHouse = new Dictionary<int, List<SwipeCardData>>();
    
    [ReadOnly]
    [Tooltip("QTEs pooled by HouseLevel")]
    public Dictionary<int, List<QTEData>> qtePoolsByHouse = new Dictionary<int, List<QTEData>>();
    
    [ReadOnly]
    [Tooltip("Cutscenes pooled by HouseLevel")]
    public Dictionary<int, List<CutsceneData>> cutscenePoolsByHouse = new Dictionary<int, List<CutsceneData>>();

    // Questions CSV Column indices (13 columns)
    private const int Q_COL_ID = 0;
    private const int Q_COL_HOUSE_LEVEL = 1;
    private const int Q_COL_SPEAKER = 2;
    private const int Q_COL_CARD_NAME = 3;
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

    // QTEs CSV Column indices (8 columns)
    private const int QTE_COL_ID = 0;
    private const int QTE_COL_HOUSE_LEVEL = 1;
    private const int QTE_COL_TYPE = 2;
    private const int QTE_COL_DURATION = 3;
    private const int QTE_COL_SUCCESS_TEXT = 4;
    private const int QTE_COL_FAIL_TEXT = 5;
    private const int QTE_COL_SUCCESS_BAT = 6;
    private const int QTE_COL_FAIL_BAT = 7;
    private const int QTE_TOTAL_COLS = 8;

    // Cutscenes CSV Column indices (6 columns)
    private const int CS_COL_ID = 0;
    private const int CS_COL_HOUSE_LEVEL = 1;
    private const int CS_COL_TYPE = 2;
    private const int CS_COL_TEXT = 3;
    private const int CS_COL_DURATION = 4;
    private const int CS_COL_ANIMATION = 5;
    private const int CS_TOTAL_COLS = 6;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            ParseAllCSVs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Button("▶ Parse All CSVs")]
    public void ParseAllCSVs()
    {
        questionPoolsByHouse.Clear();
        qtePoolsByHouse.Clear();
        cutscenePoolsByHouse.Clear();

        ParseQuestionsCSV();
        ParseQTEsCSV();
        ParseCutscenesCSV();

        Debug.Log("[DataManager] ✅ All CSVs parsed!");
        PrintSummary();
    }

    [Button("▶ Parse Questions")]
    private void ParseQuestionsCSV()
    {
        questionPoolsByHouse.Clear();

        if (questionsCSV == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ No Questions CSV assigned!");
            return;
        }

        string[] lines = questionsCSV.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("[DataManager] ❌ Questions CSV is empty!");
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

        Debug.Log($"[DataManager] ✅ Questions: {parsed} parsed, {skipped} skipped");
    }

    [Button("▶ Parse QTEs")]
    private void ParseQTEsCSV()
    {
        qtePoolsByHouse.Clear();

        if (qtesCSV == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ No QTEs CSV assigned!");
            return;
        }

        string[] lines = qtesCSV.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("[DataManager] ❌ QTEs CSV is empty!");
            return;
        }

        int parsed = 0, skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i].Trim(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            QTEData qte = ParseQTE(fields, i + 1);

            if (qte != null)
            {
                if (!qtePoolsByHouse.ContainsKey(qte.HouseLevel))
                    qtePoolsByHouse[qte.HouseLevel] = new List<QTEData>();

                qtePoolsByHouse[qte.HouseLevel].Add(qte);
                parsed++;
            }
            else
            {
                skipped++;
            }
        }

        Debug.Log($"[DataManager] ✅ QTEs: {parsed} parsed, {skipped} skipped");
    }

    [Button("▶ Parse Cutscenes")]
    private void ParseCutscenesCSV()
    {
        cutscenePoolsByHouse.Clear();

        if (cutscenesCSV == null)
        {
            Debug.LogWarning("[DataManager] ⚠️ No Cutscenes CSV assigned!");
            return;
        }

        string[] lines = cutscenesCSV.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("[DataManager] ❌ Cutscenes CSV is empty!");
            return;
        }

        int parsed = 0, skipped = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i].Trim(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            CutsceneData cutscene = ParseCutscene(fields, i + 1);

            if (cutscene != null)
            {
                if (!cutscenePoolsByHouse.ContainsKey(cutscene.HouseLevel))
                    cutscenePoolsByHouse[cutscene.HouseLevel] = new List<CutsceneData>();

                cutscenePoolsByHouse[cutscene.HouseLevel].Add(cutscene);
                parsed++;
            }
            else
            {
                skipped++;
            }
        }

        Debug.Log($"[DataManager] ✅ Cutscenes: {parsed} parsed, {skipped} skipped");
    }

    [Button("👁 Preview All Data")]
    public void PreviewData()
    {
        Debug.Log("=== QUESTION POOLS ===");
        foreach (var kvp in questionPoolsByHouse)
        {
            Debug.Log($"House {kvp.Key}: {kvp.Value.Count} questions");
            foreach (var q in kvp.Value)
            {
                Debug.Log($"  [{q.CardName}] \"{q.QuestionAR}\"");
                Debug.Log($"    Correct: {q.OptionCorrectAR} | Wrong: {q.OptionWrongAR}");
                Debug.Log($"    CorrectSide: {(q.RightIsCorrect ? "RIGHT" : "LEFT")} | BaseEid: {q.BaseEid}");
            }
        }

        Debug.Log("=== QTE POOLS ===");
        foreach (var kvp in qtePoolsByHouse)
        {
            Debug.Log($"House {kvp.Key}: {kvp.Value.Count} QTEs");
            foreach (var q in kvp.Value)
            {
                Debug.Log($"  [{q.ID}] Type:{q.QTEType} | Duration:{q.Duration}s");
            }
        }

        Debug.Log("=== CUTSCENE POOLS ===");
        foreach (var kvp in cutscenePoolsByHouse)
        {
            Debug.Log($"House {kvp.Key}: {kvp.Value.Count} cutscenes");
            foreach (var c in kvp.Value)
            {
                Debug.Log($"  [{c.ID}] Type:{c.CutsceneType} | Text:\"{c.TextAR}\"");
            }
        }
        Debug.Log("=== END PREVIEW ===");
    }

    private SwipeCardData ParseQuestion(string[] fields, int row)
    {
        if (fields.Length < Q_TOTAL_COLS)
        {
            Debug.LogWarning($"[DataManager] Questions Line {row}: Expected {Q_TOTAL_COLS} cols, got {fields.Length}");
            return null;
        }

        string id = SafeField(fields, Q_COL_ID);
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"[DataManager] Questions Line {row}: Empty ID");
            return null;
        }

        string question = SafeField(fields, Q_COL_QUESTION);
        if (string.IsNullOrWhiteSpace(question) || question == "_")
        {
            Debug.Log($"[DataManager] Questions Line {row}: Question skipped (empty)");
            return null;
        }

        int correctSide = ParseInt(SafeField(fields, Q_COL_CORRECT_SIDE));
        bool rightIsCorrect = (correctSide == 1);

        return new SwipeCardData
        {
            ID = id,
            HouseLevel = ParseInt(SafeField(fields, Q_COL_HOUSE_LEVEL)),
            Speaker = SafeField(fields, Q_COL_SPEAKER),
            CardName = SafeField(fields, Q_COL_CARD_NAME),
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
    }

    private QTEData ParseQTE(string[] fields, int row)
    {
        if (fields.Length < QTE_TOTAL_COLS)
        {
            Debug.LogWarning($"[DataManager] QTEs Line {row}: Expected {QTE_TOTAL_COLS} cols, got {fields.Length}");
            return null;
        }

        string id = SafeField(fields, QTE_COL_ID);
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"[DataManager] QTEs Line {row}: Empty ID");
            return null;
        }

        if (!Enum.TryParse<QTEType>(SafeField(fields, QTE_COL_TYPE), true, out QTEType qteType))
        {
            Debug.LogWarning($"[DataManager] QTEs Line {row}: Invalid QTEType '{SafeField(fields, QTE_COL_TYPE)}'");
            return null;
        }

        return new QTEData
        {
            ID = id,
            HouseLevel = ParseInt(SafeField(fields, QTE_COL_HOUSE_LEVEL)),
            QTEType = qteType,
            Duration = ParseFloat(SafeField(fields, QTE_COL_DURATION)),
            SuccessTextAR = SafeField(fields, QTE_COL_SUCCESS_TEXT),
            FailTextAR = SafeField(fields, QTE_COL_FAIL_TEXT),
            SuccessBatteryEffect = ParseFloat(SafeField(fields, QTE_COL_SUCCESS_BAT)),
            FailBatteryEffect = ParseFloat(SafeField(fields, QTE_COL_FAIL_BAT))
        };
    }

    private CutsceneData ParseCutscene(string[] fields, int row)
    {
        if (fields.Length < CS_TOTAL_COLS)
        {
            Debug.LogWarning($"[DataManager] Cutscenes Line {row}: Expected {CS_TOTAL_COLS} cols, got {fields.Length}");
            return null;
        }

        string id = SafeField(fields, CS_COL_ID);
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"[DataManager] Cutscenes Line {row}: Empty ID");
            return null;
        }

        if (!Enum.TryParse<CutsceneType>(SafeField(fields, CS_COL_TYPE), true, out CutsceneType cutsceneType))
        {
            Debug.LogWarning($"[DataManager] Cutscenes Line {row}: Invalid CutsceneType '{SafeField(fields, CS_COL_TYPE)}'");
            return null;
        }

        if (!Enum.TryParse<AnimationType>(SafeField(fields, CS_COL_ANIMATION), true, out AnimationType animType))
        {
            Debug.LogWarning($"[DataManager] Cutscenes Line {row}: Invalid AnimationType '{SafeField(fields, CS_COL_ANIMATION)}', defaulting to FadeIn");
            animType = AnimationType.FadeIn;
        }

        return new CutsceneData
        {
            ID = id,
            HouseLevel = ParseInt(SafeField(fields, CS_COL_HOUSE_LEVEL)),
            CutsceneType = cutsceneType,
            TextAR = SafeField(fields, CS_COL_TEXT),
            Duration = ParseFloat(SafeField(fields, CS_COL_DURATION)),
            Animation = animType
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
        Debug.LogWarning($"[DataManager] Question not found: {id}");
        return null;
    }

    /// <summary>
    /// Gets a QTE by ID from the pool.
    /// </summary>
    public QTEData GetQTEByID(string id)
    {
        foreach (var kvp in qtePoolsByHouse)
        {
            foreach (var q in kvp.Value)
            {
                if (q.ID == id) return q;
            }
        }
        Debug.LogWarning($"[DataManager] QTE not found: {id}");
        return null;
    }

    /// <summary>
    /// Gets a cutscene by ID from the pool.
    /// </summary>
    public CutsceneData GetCutsceneByID(string id)
    {
        foreach (var kvp in cutscenePoolsByHouse)
        {
            foreach (var c in kvp.Value)
            {
                if (c.ID == id) return c;
            }
        }
        Debug.LogWarning($"[DataManager] Cutscene not found: {id}");
        return null;
    }

    /// <summary>
    /// Gets all questions for a house level (no shuffling).
    /// </summary>
    public List<SwipeCardData> GetQuestionsForHouse(int houseLevel)
    {
        if (!questionPoolsByHouse.ContainsKey(houseLevel))
        {
            Debug.LogWarning($"[DataManager] No questions for House {houseLevel}!");
            return new List<SwipeCardData>();
        }

        return new List<SwipeCardData>(questionPoolsByHouse[houseLevel]);
    }

    [Button("🗑 Clear Data")]
    public void ClearData()
    {
        questionPoolsByHouse.Clear();
        qtePoolsByHouse.Clear();
        cutscenePoolsByHouse.Clear();
    }

    private void PrintSummary()
    {
        Debug.Log("[DataManager] === PARSE SUMMARY ===");
        for (int h = 1; h <= 4; h++)
        {
            int qCount = questionPoolsByHouse.ContainsKey(h) ? questionPoolsByHouse[h].Count : 0;
            int qteCount = qtePoolsByHouse.ContainsKey(h) ? qtePoolsByHouse[h].Count : 0;
            int csCount = cutscenePoolsByHouse.ContainsKey(h) ? cutscenePoolsByHouse[h].Count : 0;
            Debug.Log($"[DataManager]   House {h}: {qCount} questions, {qteCount} QTEs, {csCount} cutscenes");
        }
        Debug.Log("[DataManager] === END SUMMARY ===");
    }
}
