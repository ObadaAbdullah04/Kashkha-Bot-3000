using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// PHASE 13: Generates sample HouseSequenceData ScriptableObjects with all element types.
/// 
/// PURPOSE:
/// Creates ready-to-test HouseSequenceData assets for all 4 houses,
/// including Questions, Cutscenes, AND Interactions mixed together.
/// 
/// USAGE:
/// 1. Tools → Kashkha → Generate Sample House Sequences
/// 2. Click "Generate All 4 Houses" button
/// 3. Assets appear in Assets/_Project/Data/Sequences/
/// 4. Assign to HouseFlowController or load via GameManager
/// </summary>
public class SampleSequenceGenerator : EditorWindow
{
    private Vector2 scrollPosition;
    private string generationReport = "";
    private bool generationComplete = false;

    // Toggle which houses to generate
    private bool generateHouse1 = true;
    private bool generateHouse2 = true;
    private bool generateHouse3 = true;
    private bool generateHouse4 = true;

    // Sequence design options
    private bool includeInteractions = true;
    private bool includeCutscenes = true;
    private bool includeQuestions = true;

    [MenuItem("Tools/Kashkha/Generate Sample House Sequences")]
    public static void ShowWindow()
    {
        var window = GetWindow<SampleSequenceGenerator>("Generate House Sequences");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "PHASE 13: Sample House Sequence Generator\n\n" +
            "Generates HouseSequenceData ScriptableObject assets with Questions, Cutscenes, and Interactions.\n" +
            "These are ready-to-test sequences that demonstrate the full game flow.\n\n" +
            "Generated files will appear in: Assets/_Project/Data/Sequences/",
            MessageType.Info);

        GUILayout.Space(10);

        // Options section
        EditorGUILayout.LabelField("Generation Options", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        generateHouse1 = EditorGUILayout.Toggle("House 1 (Easy)", generateHouse1);
        generateHouse2 = EditorGUILayout.Toggle("House 2 (Medium)", generateHouse2);
        generateHouse3 = EditorGUILayout.Toggle("House 3 (Hard)", generateHouse3);
        generateHouse4 = EditorGUILayout.Toggle("House 4 (Insane)", generateHouse4);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        includeQuestions = EditorGUILayout.ToggleLeft("Include Questions (Swipe Cards)", includeQuestions);
        includeCutscenes = EditorGUILayout.ToggleLeft("Include Cutscenes (Cinematics)", includeCutscenes);
        includeInteractions = EditorGUILayout.ToggleLeft("Include Interactions (Shake/Hold/Tap)", includeInteractions);

        EditorGUILayout.Space(10);

        // Generate button
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("🏠 Generate Selected Houses", GUILayout.Height(40)))
        {
            GenerateSequences();
        }
        GUI.backgroundColor = Color.white;

        // Quick generate all
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("⚡ Quick: Generate All 4 Houses (Default)", GUILayout.Height(30)))
        {
            generateHouse1 = generateHouse2 = generateHouse3 = generateHouse4 = true;
            includeQuestions = includeCutscenes = includeInteractions = true;
            GenerateSequences();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // Preview section
        EditorGUILayout.LabelField("Sequence Preview", EditorStyles.boldLabel);
        ShowSequencePreview();

        GUILayout.Space(10);

        // Report section
        EditorGUILayout.LabelField("Generation Report", EditorStyles.boldLabel);
        
        GUI.backgroundColor = Color.black;
        GUI.contentColor = generationReport.Contains("❌") ? Color.red : Color.green;
        EditorGUILayout.TextArea(generationReport, GUILayout.Height(200));
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("📋 Copy Report to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = generationReport;
            ShowNotification(new GUIContent("Report copied!"));
        }

        if (generationComplete)
        {
            EditorGUILayout.HelpBox("✅ Generation Complete! Check the Project window for new .asset files.", MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ShowSequencePreview()
    {
        // Show what each house sequence will look like
        string[] previews = {
            "House 1: Cutscene(Greeting) → Question → Interaction(Shake) → Question → Cutscene(Reaction)",
            "House 2: Question → Cutscene → Question → Interaction(Hold) → Cutscene → Question",
            "House 3: Cutscene → Question → Interaction(Tap) → Question → Cutscene → Interaction(Shake) → Question",
            "House 4: Question → Interaction(Shake) → Question → Interaction(Hold) → Question → Interaction(Tap)"
        };

        for (int i = 0; i < 4; i++)
        {
            bool[] toggles = { generateHouse1, generateHouse2, generateHouse3, generateHouse4 };
            GUI.enabled = toggles[i];
            EditorGUILayout.HelpBox(previews[i], MessageType.None);
            GUI.enabled = true;
        }
    }

    private void GenerateSequences()
    {
        generationReport = "=== HOUSE SEQUENCE GENERATION REPORT ===\n";
        generationReport += $"Timestamp: {System.DateTime.Now}\n\n";
        generationComplete = false;

        // Create Sequences folder INSIDE Resources (required for Resources.Load)
        string sequencesFolder = "Assets/_Project/Resources/Sequences";
        if (!Directory.Exists(sequencesFolder))
        {
            Directory.CreateDirectory(sequencesFolder);
            generationReport += $"✅ Created folder: {sequencesFolder}\n";
        }
        AssetDatabase.Refresh();

        int generatedCount = 0;

        // Generate House 1
        if (generateHouse1)
        {
            if (GenerateHouseSequence(1, sequencesFolder))
                generatedCount++;
        }

        // Generate House 2
        if (generateHouse2)
        {
            if (GenerateHouseSequence(2, sequencesFolder))
                generatedCount++;
        }

        // Generate House 3
        if (generateHouse3)
        {
            if (GenerateHouseSequence(3, sequencesFolder))
                generatedCount++;
        }

        // Generate House 4
        if (generateHouse4)
        {
            if (GenerateHouseSequence(4, sequencesFolder))
                generatedCount++;
        }

        generationReport += $"\n=== SUMMARY ===\n";
        generationReport += $"✅ Generated: {generatedCount} house sequence(s)\n";
        generationReport += $"📁 Location: {sequencesFolder}/\n";
        generationReport += $"\n=== NEXT STEPS ===\n";
        generationReport += "1. Open Core_Scene.unity\n";
        generationReport += "2. Find HouseFlowController in the scene\n";
        generationReport += "3. Test by starting a game (GameManager will load sequences)\n";
        generationReport += "4. Or manually assign: Drag House1_Sequence.asset to HouseFlowController (for testing)\n";
        generationReport += "\n=== END REPORT ===";

        generationComplete = true;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SampleSequenceGenerator] ✅ Generated {generatedCount} house sequence(s)!");
    }

    private bool GenerateHouseSequence(int houseLevel, string folderPath)
    {
        string assetName = $"House{houseLevel}_Sequence";
        string assetPath = $"{folderPath}/{assetName}.asset";

        // Delete existing asset if present
        if (File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
            generationReport += $"⚠️ Deleted existing: {assetName}.asset\n";
        }

        // Create ScriptableObject
        HouseSequenceData sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
        sequence.HouseLevel = houseLevel;
        sequence.Sequence = new List<SequenceElement>();

        // Build sequence based on house level
        switch (houseLevel)
        {
            case 1:
                BuildHouse1Sequence(sequence);
                break;
            case 2:
                BuildHouse2Sequence(sequence);
                break;
            case 3:
                BuildHouse3Sequence(sequence);
                break;
            case 4:
                BuildHouse4Sequence(sequence);
                break;
        }

        // Save asset
        AssetDatabase.CreateAsset(sequence, assetPath);
        AssetDatabase.SaveAssets();

        // Report
        generationReport += $"✅ Created: {assetName}.asset\n";
        generationReport += $"   Elements: {sequence.Sequence.Count}\n";
        
        int qCount = 0, csCount = 0, intCount = 0;
        foreach (var elem in sequence.Sequence)
        {
            switch (elem.Type)
            {
                case ElementType.Question: qCount++; break;
                case ElementType.Cutscene: csCount++; break;
                case ElementType.Interaction: intCount++; break;
            }
        }
        
        generationReport += $"   Composition: {qCount} Questions, {csCount} Cutscenes, {intCount} Interactions\n";
        generationReport += $"   Flow Preview:\n";
        
        for (int i = 0; i < Mathf.Min(sequence.Sequence.Count, 6); i++)
        {
            var elem = sequence.Sequence[i];
            string icon = elem.Type == ElementType.Question ? "❓" : 
                         elem.Type == ElementType.Cutscene ? "🎬" : "🎮";
            generationReport += $"     {i + 1}. {icon} [{elem.Type}] {elem.ElementID}\n";
        }
        
        if (sequence.Sequence.Count > 6)
        {
            generationReport += $"     ... and {sequence.Sequence.Count - 6} more elements\n";
        }
        
        generationReport += "\n";

        return true;
    }

    private void BuildHouse1Sequence(HouseSequenceData sequence)
    {
        // House 1: Tutorial house - gentle introduction
        // Flow: Greeting → Question → Interaction(Shake) → Question → Question → Cutscene → Interaction(Hold) → Question
        
        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H1_Welcome", "Aunt welcomes you"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q1", "Hospitality question"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q5", "How are you?"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "SHAKE_Cup_1", "Shake the coffee cup"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q8", "Grandma loves you"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q10", "Prayer blessing"));
        }

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H1_Aunt_Smile", "Aunt smiles"));

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "HOLD_Hand_1", "Handshake interaction"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q3", "Studies question"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q6", "Eid greeting"));
        }

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_Celebration", "House complete!"));
    }

    private void BuildHouse2Sequence(HouseSequenceData sequence)
    {
        // House 2: Medium difficulty - more interactions
        // Flow: Question → Cutscene → Question → Interaction(Hold) → Question → Interaction(Tap) → Cutscene → Question

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q11", "Coffee cup shake"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q13", "You're a hero"));
        }

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H2_Greeting", "Uncle greets you"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q14", "Marriage question"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q16", "Eid joy"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "HOLD_Cup_2", "Hold the cup steady"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q17", "Work question"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q19", "Love the country"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "TAP_Heart_2", "Tap the heart quickly"));

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H2_Uncle_Nod", "Uncle approves"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q12", "Coffee taste"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q20", "Family unity"));
        }

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_Timeout", "House complete!"));
    }

    private void BuildHouse3Sequence(HouseSequenceData sequence)
    {
        // House 3: Hard difficulty - frequent interactions
        // Flow: Cutscene → Question → Interaction(Tap) → Question → Cutscene → Interaction(Shake) → Question → Question → Interaction(Hold)

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H3_Blessing", "Grandma blesses you"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q21", "Tea or coffee"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q23", "Prayer question"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "TAP_Door_1", "Knock on the door"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q25", "Family gathering"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q28", "Prayer blessing"));
        }

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H3_Family_Gather", "Family gathers"));

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "SHAKE_Hand_3", "Warm handshake"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q26", "Where's grandma"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q30", "Story time"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "HOLD_Gift_3", "Hold the gift tightly"));

        if (includeCutscenes)
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H3_Respect_Elder", "Respect elder"));
    }

    private void BuildHouse4Sequence(HouseSequenceData sequence)
    {
        // House 4: Insane mode - rapid-fire everything!
        // Flow: Question → Interaction(Shake) → Question → Interaction(Hold) → Question → Interaction(Tap) → Question → Cutscene

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q31", "Shake shake shake"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q33", "Coffee in boss mode"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "SHAKE_Insane_4", "SHAKE CRAZY!"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q35", "How are you (insane)"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q37", "Love country (hard)"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "HOLD_Strong_4", "Hold strong!"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q34", "Eid Eid? (fast)"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q36", "Prayer (insane)"));
        }

        if (includeInteractions)
            sequence.Sequence.Add(new SequenceElement(ElementType.Interaction, "TAP_Fast_4", "Tap FASTER!"));

        if (includeQuestions)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q38", "Family all of us"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Question, "Q40", "Story time (boss)"));
        }

        if (includeCutscenes)
        {
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H4_Boss_Intro", "Boss intro"));
            sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, "CS_H4_Boss_Win", "Boss defeated!"));
        }
    }
}
