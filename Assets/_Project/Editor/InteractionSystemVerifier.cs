using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// Quick verification tool for Phase 13 setup.
/// Generates a report of all created files and scene references.
/// 
/// USAGE:
/// Tools → Kashkha → Verify Interaction Setup
/// </summary>
public class InteractionSystemVerifier : EditorWindow
{
    private string verificationReport = "";

    [MenuItem("Tools/Kashkha/Verify Interaction Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<InteractionSystemVerifier>("Interaction System Verification");
        window.minSize = new Vector2(600, 700);
        window.GenerateReport();
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Phase 13: Interaction System Verification\n\n" +
            "This tool checks if all required files and scene references are correct.\n" +
            "Copy the report below and share it for verification.",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("🔄 Refresh Report", GUILayout.Height(30)))
        {
            GenerateReport();
        }

        GUILayout.Space(10);

        // Report display
        EditorGUILayout.LabelField("Verification Report", EditorStyles.boldLabel);
        
        GUI.backgroundColor = Color.black;
        GUI.contentColor = verificationReport.Contains("❌") ? Color.red : Color.green;
        verificationReport = EditorGUILayout.TextArea(verificationReport, GUILayout.Height(500));
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📋 Copy Report", GUILayout.Height(30)))
        {
            EditorGUIUtility.systemCopyBuffer = verificationReport;
            ShowNotification(new GUIContent("Report copied!"));
        }

        if (GUILayout.Button("💾 Save to File", GUILayout.Height(30)))
        {
            SaveReportToFile();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void GenerateReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== PHASE 13: INTERACTION SYSTEM VERIFICATION ===");
        sb.AppendLine($"Generated: {System.DateTime.Now}");
        sb.AppendLine($"Unity Version: {Application.unityVersion}");
        sb.AppendLine();

        // 1. Check CSV files
        sb.AppendLine("--- CSV Files ---");
        CheckFile(sb, "Assets/_Project/Data/Interactions.csv", "Interactions CSV");
        CheckFile(sb, "Assets/_Project/Data/Questions.csv", "Questions CSV");
        CheckFile(sb, "Assets/_Project/Data/Cutscenes.csv", "Cutscenes CSV");
        sb.AppendLine();

        // 2. Check Scripts
        sb.AppendLine("--- Scripts ---");
        CheckFile(sb, "Assets/_Project/Scripts/Data/InteractionType.cs", "InteractionType");
        CheckFile(sb, "Assets/_Project/Scripts/Data/InteractionData.cs", "InteractionData");
        CheckFile(sb, "Assets/_Project/Scripts/UI/InteractionHUDController.cs", "InteractionHUDController");
        CheckFile(sb, "Assets/_Project/Scripts/Core/InteractionSignalEmitter.cs", "InteractionSignalEmitter");
        CheckFile(sb, "Assets/_Project/Scripts/Core/DataManager.cs", "DataManager (modified)");
        CheckFile(sb, "Assets/_Project/Scripts/Core/InputManager.cs", "InputManager (modified)");
        CheckFile(sb, "Assets/_Project/Scripts/Core/HouseFlowController.cs", "HouseFlowController (modified)");
        CheckFile(sb, "Assets/_Project/Scripts/Data/HouseSequenceData.cs", "HouseSequenceData (modified)");
        sb.AppendLine();

        // 3. Check Generated Assets
        sb.AppendLine("--- Generated Assets ---");
        CheckFile(sb, "Assets/_Project/Resources/InteractionIcons/Icon_Shake.png", "Shake Icon");
        CheckFile(sb, "Assets/_Project/Resources/InteractionIcons/Icon_Hold.png", "Hold Icon");
        CheckFile(sb, "Assets/_Project/Resources/InteractionIcons/Icon_Tap.png", "Tap Icon");
        CheckFile(sb, "Assets/_Project/Resources/InteractionIcons/Icon_Draw.png", "Draw Icon");
        CheckFile(sb, "Assets/_Project/Prefabs/UI/InteractionHUD_Prefab.prefab", "InteractionHUD Prefab");
        sb.AppendLine();

        // 4. Check Documentation
        sb.AppendLine("--- Documentation ---");
        CheckFile(sb, "Assets/_Project/Data/INTERACTION_SYSTEM_GUIDE.md", "Setup Guide");
        CheckFile(sb, "PHASE13_INTERACTION_SYSTEM.md", "Phase Summary");
        sb.AppendLine();

        // 5. Check Scene Objects (if scene is open)
        sb.AppendLine("--- Scene Validation ---");
        var dataManager = Object.FindObjectOfType<DataManager>();
        if (dataManager != null)
        {
            sb.AppendLine("✅ DataManager in scene");
            sb.AppendLine($"   - Interactions CSV: {(dataManager.interactionsCSV != null ? $"Assigned ({dataManager.interactionsCSV.name})" : "❌ NOT Assigned")}");
        }
        else
        {
            sb.AppendLine("❌ DataManager NOT in scene");
        }

        var houseFlow = Object.FindObjectOfType<HouseFlowController>();
        if (houseFlow != null)
        {
            sb.AppendLine("✅ HouseFlowController in scene");
        }
        else
        {
            sb.AppendLine("❌ HouseFlowController NOT in scene");
        }

        var inputManager = Object.FindObjectOfType<InputManager>();
        if (inputManager != null)
        {
            sb.AppendLine("✅ InputManager in scene");
        }
        else
        {
            sb.AppendLine("❌ InputManager NOT in scene");
        }

        var interactionHUD = Object.FindObjectOfType<InteractionHUDController>();
        if (interactionHUD != null)
        {
            sb.AppendLine("✅ InteractionHUDController in scene");
        }
        else
        {
            sb.AppendLine("⚠️ InteractionHUDController NOT in scene (drag prefab to scene)");
        }

        sb.AppendLine();

        // 6. Check Interactions.csv content
        sb.AppendLine("--- Interactions.csv Validation ---");
        string csvPath = "Assets/_Project/Data/Interactions.csv";
        if (File.Exists(csvPath))
        {
            string[] lines = File.ReadAllLines(csvPath);
            sb.AppendLine($"Total lines (including header): {lines.Length}");
            
            int validEntries = 0;
            int invalidEntries = 0;
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] fields = lines[i].Split(',');
                if (fields.Length >= 10)
                {
                    validEntries++;
                }
                else
                {
                    invalidEntries++;
                    sb.AppendLine($"❌ Line {i + 1}: Expected 10 columns, got {fields.Length}");
                }
            }
            
            sb.AppendLine($"✅ Valid entries: {validEntries}");
            if (invalidEntries > 0)
            {
                sb.AppendLine($"❌ Invalid entries: {invalidEntries}");
            }
        }
        else
        {
            sb.AppendLine("❌ Interactions.csv not found");
        }

        sb.AppendLine();
        sb.AppendLine("=== END VERIFICATION ===");
        sb.AppendLine();
        sb.AppendLine("INSTRUCTIONS:");
        sb.AppendLine("1. Copy this entire report");
        sb.AppendLine("2. Share it with the developer for verification");
        sb.AppendLine("3. Any ❌ marks indicate missing files or incorrect setup");
        sb.AppendLine("4. Run 'Tools → Kashkha → Setup Interaction System' to fix missing items");

        verificationReport = sb.ToString();
    }

    private void CheckFile(StringBuilder sb, string path, string label)
    {
        if (File.Exists(path))
        {
            sb.AppendLine($"✅ {label}: {path}");
        }
        else
        {
            sb.AppendLine($"❌ {label} MISSING: {path}");
        }
    }

    private void SaveReportToFile()
    {
        string path = "Assets/_Project/Data/INTERACTION_VERIFICATION_REPORT.txt";
        File.WriteAllText(path, verificationReport);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Report Saved", $"Report saved to:\n{path}", "OK");
    }
}
