using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor Window that parses a UTF-8 CSV file and generates
/// or updates EncounterData ScriptableObject assets.
///
/// Open via: KashkhaBot > Encounter Importer
///
/// One CSV row = one choice. A new encounter begins on any row
/// where column 0 (EncounterId) is non-empty.
/// Encounter-level fields (columns 0-6) are only read from that row.
/// Choice fields (columns 7-13) are read from every row in the group.
/// </summary>
public class EncounterDataImporter : EditorWindow
{
    // ── Inspector State ────────────────────────────────────────────

    private string _csvPath        = "";
    private string _outputBasePath = "Assets/_Project/ScriptableObjects/Encounters";

    private Vector2 _scrollPos;
    private List<string> _log = new List<string>();

    // ── Column Indices — change here if you ever reorder the CSV ──

    private const int COL_ENCOUNTER_ID   = 0;
    private const int COL_HOUSE          = 1;
    private const int COL_TYPE           = 2;
    private const int COL_QUESTION_AR    = 3;
    private const int COL_PANIC_TIMER    = 4;
    private const int COL_MINI_GAME      = 5;
    private const int COL_UPGRADE_IDS    = 6;
    private const int COL_CHOICE_AR      = 7;
    private const int COL_BATTERY_DELTA  = 8;
    private const int COL_STOMACH_DELTA  = 9;
    private const int COL_EIDIA_REWARD   = 10;
    private const int COL_SCRAP_REWARD   = 11;
    private const int COL_IS_CORRECT     = 12;
    private const int COL_FEEDBACK_TEXT  = 13;
    private const int MIN_COLUMNS        = 14;

    // ── Window Setup ───────────────────────────────────────────────

    [MenuItem("KashkhaBot/Encounter Importer")]
    public static void ShowWindow()
    {
        var window = GetWindow<EncounterDataImporter>("Encounter Importer");
        window.minSize = new Vector2(480, 360);
    }

    // ── GUI ────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Kashkha-Bot — Encounter CSV Importer",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Save your spreadsheet as CSV UTF-8 from Excel or Google Sheets.\n" +
            "One row per choice. Leave columns 0–6 blank on rows 2+ of the same encounter.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // ── CSV file path ────────────────────────────────────────
        EditorGUILayout.LabelField("CSV File", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        _csvPath = EditorGUILayout.TextField(_csvPath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string picked = EditorUtility.OpenFilePanel(
                "Select Encounter CSV", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(picked))
                _csvPath = picked;
        }
        EditorGUILayout.EndHorizontal();

        // ── Output path ──────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Output Base Path (relative to project)",
            EditorStyles.miniBoldLabel);
        _outputBasePath = EditorGUILayout.TextField(_outputBasePath);

        EditorGUILayout.Space(12);

        // ── Import button ────────────────────────────────────────
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Import Encounters", GUILayout.Height(36)))
        {
            _log.Clear();
            RunImport();
        }
        GUI.backgroundColor = Color.white;

        // ── Log panel ────────────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Log", EditorStyles.miniBoldLabel);
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,
            GUILayout.ExpandHeight(true));

        foreach (string line in _log)
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.EndScrollView();
    }

    // ── Import Logic ───────────────────────────────────────────────

    private void RunImport()
    {
        // ── Validate inputs ──────────────────────────────────────
        if (string.IsNullOrEmpty(_csvPath) || !File.Exists(_csvPath))
        {
            LogError("No valid CSV file selected.");
            EditorUtility.DisplayDialog("Import Failed",
                "Please select a valid CSV file first.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(_outputBasePath))
        {
            LogError($"Output path '{_outputBasePath}' does not exist in the project.");
            EditorUtility.DisplayDialog("Import Failed",
                $"The output folder does not exist:\n{_outputBasePath}", "OK");
            return;
        }

        // ── Read CSV ──────────────────────────────────────────────
        // UTF-8 with BOM detection — Excel sometimes prepends a BOM.
        string[] lines = File.ReadAllLines(_csvPath, Encoding.UTF8);

        if (lines.Length < 2)
        {
            LogError("CSV has no data rows (only a header or is empty).");
            return;
        }

        Log($"Read {lines.Length - 1} data rows from CSV.");

        // ── Parse rows, skip header (line 0) ─────────────────────
        var allRows = new List<string[]>();
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] fields = ParseCSVLine(line);
            if (fields.Length < MIN_COLUMNS)
            {
                LogWarning($"Row {i + 1} has only {fields.Length} columns " +
                           $"(need {MIN_COLUMNS}). Skipping.");
                continue;
            }
            allRows.Add(fields);
        }

        // ── Group rows into encounter blocks ──────────────────────
        // A new block starts whenever column 0 (EncounterId) is non-empty.
        var encounterBlocks = new List<List<string[]>>();
        List<string[]> currentBlock = null;

        foreach (string[] row in allRows)
        {
            bool isNewEncounter = !string.IsNullOrWhiteSpace(row[COL_ENCOUNTER_ID]);

            if (isNewEncounter)
            {
                currentBlock = new List<string[]>();
                encounterBlocks.Add(currentBlock);
            }

            // Guard: a choice row before any encounter header
            if (currentBlock == null)
            {
                LogWarning("Found a choice row before any EncounterId row. " +
                           "Check your CSV structure. Skipping row.");
                continue;
            }

            currentBlock.Add(row);
        }

        Log($"Found {encounterBlocks.Count} encounter block(s).");

        // ── Process each encounter block ──────────────────────────
        int created = 0;
        int updated = 0;
        int skipped = 0;

        foreach (List<string[]> block in encounterBlocks)
        {
            bool success = ProcessEncounterBlock(block, ref created, ref updated);
            if (!success) skipped++;
        }

        // ── Finalise ──────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = $"Import complete — Created: {created}  " +
                         $"Updated: {updated}  Skipped: {skipped}";
        Log(summary);
        EditorUtility.DisplayDialog("Import Complete", summary, "OK");
    }

    // ── Process One Encounter Block ────────────────────────────────

    private bool ProcessEncounterBlock(
        List<string[]> block, ref int created, ref int updated)
    {
        if (block == null || block.Count == 0) return false;

        string[] header = block[0];

        // ── Encounter-level fields (from the first row only) ──────
        string encounterId = header[COL_ENCOUNTER_ID].Trim();
        string houseStr    = header[COL_HOUSE].Trim();
        string typeStr     = header[COL_TYPE].Trim();
        string questionAR  = header[COL_QUESTION_AR].Trim();
        string timerStr    = header[COL_PANIC_TIMER].Trim();
        string miniGameStr = header[COL_MINI_GAME].Trim();
        string upgradeStr  = header[COL_UPGRADE_IDS].Trim();

        if (string.IsNullOrEmpty(encounterId))
        {
            LogWarning("Skipping block with empty EncounterId.");
            return false;
        }

        if (string.IsNullOrEmpty(houseStr))
        {
            LogWarning($"Encounter '{encounterId}' has no House number. Skipping.");
            return false;
        }

        // ── Build choices from every row in the block ─────────────
        var choices    = new List<ChoiceData>();
        int correctCount = 0;
        bool hasError  = false;

        foreach (string[] row in block)
        {
            string choiceAR = row[COL_CHOICE_AR].Trim();
            if (string.IsNullOrEmpty(choiceAR)) continue; // skip empty choice rows

            string feedbackText = row[COL_FEEDBACK_TEXT].Trim();

            if (string.IsNullOrEmpty(feedbackText))
            {
                LogWarning($"[{encounterId}] Choice '{choiceAR}' has empty " +
                           $"FeedbackText — this is a build error per the GDD.");
                hasError = true;
            }

            bool isCorrect = ParseBool(row[COL_IS_CORRECT]);
            if (isCorrect) correctCount++;

            choices.Add(new ChoiceData
            {
                TextAR       = choiceAR,
                BatteryDelta = ParseFloat(row[COL_BATTERY_DELTA]),
                StomachDelta = ParseFloat(row[COL_STOMACH_DELTA]),
                EidiaReward  = ParseInt(row[COL_EIDIA_REWARD]),
                ScrapReward  = ParseInt(row[COL_SCRAP_REWARD]),
                IsCorrect    = isCorrect,
                FeedbackText = feedbackText
            });
        }

        // Validate correct answer count
        if (choices.Count > 0 && correctCount != 1)
        {
            LogWarning($"[{encounterId}] Expected exactly 1 correct choice, " +
                       $"found {correctCount}. Asset will be created but needs review.");
            hasError = true;
        }

        // ── Ensure house subfolder exists ─────────────────────────
        string houseFolderName = $"House{houseStr}";
        string folderPath      = $"{_outputBasePath}/{houseFolderName}";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(_outputBasePath, houseFolderName);
            Log($"Created folder: {folderPath}");
        }

        // ── Find or create the SO asset ───────────────────────────
        string assetPath = $"{folderPath}/{encounterId}.asset";
        EncounterData data = AssetDatabase.LoadAssetAtPath<EncounterData>(assetPath);
        bool isNew = data == null;

        if (isNew)
            data = ScriptableObject.CreateInstance<EncounterData>();

        // ── Populate SO fields ────────────────────────────────────
        data.EncounterId        = encounterId;
        data.Type               = ParseEncounterType(typeStr);
        data.QuestionTextAR     = questionAR;
        data.PanicTimerDuration = string.IsNullOrEmpty(timerStr) ? 8f : ParseFloat(timerStr);
        data.TriggersMiniGame   = ParseMiniGameType(miniGameStr);
        data.RequiredUpgradeIds = string.IsNullOrEmpty(upgradeStr)
            ? Array.Empty<string>()
            : upgradeStr.Split('|');
        data.Choices            = choices.ToArray();

        // ── Write to disk ─────────────────────────────────────────
        if (isNew)
        {
            AssetDatabase.CreateAsset(data, assetPath);
            created++;
            Log($"✅ Created: {assetPath}{(hasError ? "  ⚠ (has warnings)" : "")}");
        }
        else
        {
            EditorUtility.SetDirty(data);
            updated++;
            Log($"🔄 Updated: {assetPath}{(hasError ? "  ⚠ (has warnings)" : "")}");
        }

        return true;
    }

    // ── CSV Parser — RFC 4180 compliant ───────────────────────────
    // Handles: quoted fields, commas inside quotes,
    // escaped double-quotes (""), and Arabic Unicode correctly.

    private static string[] ParseCSVLine(string line)
    {
        var fields  = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // An escaped quote inside a quoted field: ""
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // consume the second quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString()); // last field
        return fields.ToArray();
    }

    // ── Type Parsers ───────────────────────────────────────────────

    private static EncounterType ParseEncounterType(string val)
    {
        return val.Trim() switch
        {
            "MicroGame" => EncounterType.MicroGame,
            "Forced"    => EncounterType.Forced,
            _           => EncounterType.Dialogue   // safe default
        };
    }

    private static MiniGameType ParseMiniGameType(string val)
    {
        return val.Trim() switch
        {
            "ForcedFeast"        => MiniGameType.ForcedFeast,
            "EidTrafficEscape"   => MiniGameType.EidTrafficEscape,
            "InteractiveLoading" => MiniGameType.InteractiveLoading,
            _                    => MiniGameType.None
        };
    }

    private static float ParseFloat(string val)
    {
        return float.TryParse(val.Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float result) ? result : 0f;
    }

    private static int ParseInt(string val)
    {
        return int.TryParse(val.Trim(), out int result) ? result : 0;
    }

    private static bool ParseBool(string val)
    {
        string v = val.Trim().ToUpperInvariant();
        return v == "TRUE" || v == "1" || v == "YES";
    }

    // ── Log Helpers ────────────────────────────────────────────────

    private void Log(string msg)
    {
        _log.Add(msg);
        Debug.Log($"[EncounterImporter] {msg}");
        Repaint();
    }

    private void LogWarning(string msg)
    {
        _log.Add($"⚠ {msg}");
        Debug.LogWarning($"[EncounterImporter] {msg}");
        Repaint();
    }

    private void LogError(string msg)
    {
        _log.Add($"❌ {msg}");
        Debug.LogError($"[EncounterImporter] {msg}");
        Repaint();
    }
}