using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Text.RegularExpressions;

/// <summary>
/// Manages CSV data loading and parsing.
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("CSV")]
    public TextAsset csvFile;

    [Header("Parsed Data")]
    public List<EncounterData> allEncounters = new List<EncounterData>();

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

    [Button("Parse CSV")]
    public void ParseCSV()
    {
        allEncounters.Clear();

        if (csvFile == null)
        {
            Debug.LogError("[DataManager] No CSV file assigned!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("[DataManager] CSV is empty!");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            // Column structure: 28 columns for QTE input system (Phase 5)
            // Backward compatible with old 23-column format
            // ID, HouseLevel, SequenceOrder, EncounterType, MiniGameAfter, QTEType, QTEInputType, QTECount, QTETimeLimit, QTEDirection, QTEHoldDuration,
            // Speaker, QuestionAR, OfferTextAR, Choice1AR, Choice1IsCorrect, Choice1Feedback, Choice2AR, Choice2IsCorrect, Choice2Feedback,
            // Choice3AR, Choice3IsCorrect, Choice3Feedback, BatteryDelta, StomachDelta, EidiaReward, ScrapReward, ColorHex
            if (fields.Length < 23)
            {
                Debug.LogWarning($"[DataManager] Line {i + 1}: Expected at least 23 columns, got {fields.Length}. Skipping.");
                continue;
            }

            // Parse EncounterType
            string encounterTypeStr = fields[3].Trim('"');
            EncounterType encounterType = EncounterType.Trivia;
            if (encounterTypeStr == "HospitalityOffer")
                encounterType = EncounterType.HospitalityOffer;

            // Parse MiniGameAfter (boolean)
            bool miniGameAfter = ParseBool(fields[4]);
            
            // Parse QTE fields - support both old (23 col) and new (28 col) formats
            string legacyQTEType = fields.Length > 5 ? fields[5].Trim('"') : "None";
            string qteInputType = fields.Length > 6 ? fields[6].Trim('"') : "_";
            int qteCount = fields.Length > 7 ? ParseInt(fields[7]) : 0;
            float qteTimeLimit = fields.Length > 8 ? ParseFloat(fields[8]) : 0f;
            string qteDirection = fields.Length > 9 ? fields[9].Trim('"') : "_";
            float qteHoldDuration = fields.Length > 10 ? ParseFloat(fields[10]) : 0f;
            
            // Map legacy QTE types to new input types if QTEInputType is empty
            if (string.IsNullOrEmpty(qteInputType) || qteInputType == "_")
            {
                qteInputType = legacyQTEType.ToLower() switch
                {
                    "coffeerefuse" => "Shake",
                    "handonheart" => "Tap",
                    "tugofwar" => "Swipe",
                    _ => "_"
                };
                
                // Set defaults for legacy encounters
                if (qteCount == 0) qteCount = qteInputType == "Shake" ? 1 : (qteInputType == "Swipe" ? 2 : 1);
                if (qteTimeLimit == 0) qteTimeLimit = 3f;
                if (qteDirection == "_") qteDirection = "Up";
                if (qteHoldDuration == 0) qteHoldDuration = 2f;
            }

            EncounterData data = new EncounterData
            {
                ID = int.Parse(fields[0]),
                HouseLevel = int.Parse(fields[1]),
                SequenceOrder = int.Parse(fields[2]),
                EncounterType = encounterType,
                MiniGameAfter = miniGameAfter,
                QTEType = legacyQTEType, // Keep for backward compatibility
                QTEInputType = qteInputType,
                QTECount = qteCount,
                QTETimeLimit = qteTimeLimit,
                QTEDirection = qteDirection,
                QTEHoldDuration = qteHoldDuration,
                Speaker = fields.Length > 11 ? fields[11].Trim('"') : "",
                QuestionAR = fields.Length > 12 ? fields[12].Trim('"') : "",
                OfferTextAR = fields.Length > 13 ? fields[13].Trim('"') : "",
                Choice1AR = fields.Length > 14 ? fields[14].Trim('"') : "",
                Choice1IsCorrect = fields.Length > 15 ? ParseBool(fields[15]) : false,
                Choice1Feedback = fields.Length > 16 ? fields[16].Trim('"') : "",
                Choice2AR = fields.Length > 17 ? fields[17].Trim('"') : "",
                Choice2IsCorrect = fields.Length > 18 ? ParseBool(fields[18]) : false,
                Choice2Feedback = fields.Length > 19 ? fields[19].Trim('"') : "",
                Choice3AR = fields.Length > 20 ? fields[20].Trim('"') : "",
                Choice3IsCorrect = fields.Length > 21 ? ParseBool(fields[21]) : false,
                Choice3Feedback = fields.Length > 22 ? fields[22].Trim('"') : "",
                BatteryDelta = fields.Length > 23 ? float.Parse(fields[23]) : 0f,
                StomachDelta = fields.Length > 24 ? float.Parse(fields[24]) : 0f,
                EidiaReward = fields.Length > 25 ? int.Parse(fields[25]) : 0,
                ScrapReward = fields.Length > 26 ? int.Parse(fields[26]) : 0,
                ColorHex = fields.Length > 27 ? fields[27].Trim('"') : ""
            };

            allEncounters.Add(data);
        }

        Debug.Log($"[DataManager] Parsed {allEncounters.Count} encounters with flexible gauntlet sequencing.");
    }

    /// <summary>
    /// Parses boolean values from CSV (supports "1"/"0", "True"/"False", "true"/"false").
    /// </summary>
    private bool ParseBool(string value)
    {
        value = value.Trim().ToLower();
        return value == "1" || value == "true";
    }
    
    /// <summary>
    /// Parses integer values from CSV (supports "_" as 0).
    /// </summary>
    private int ParseInt(string value)
    {
        value = value.Trim().Trim('"');
        if (value == "_" || string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value, out int result) ? result : 0;
    }
    
    /// <summary>
    /// Parses float values from CSV (supports "_" as 0f).
    /// </summary>
    private float ParseFloat(string value)
    {
        value = value.Trim().Trim('"');
        if (value == "_" || string.IsNullOrEmpty(value)) return 0f;
        return float.TryParse(value, out float result) ? result : 0f;
    }

    [Button("Clear Data")]
    public void ClearData() => allEncounters.Clear();
}
