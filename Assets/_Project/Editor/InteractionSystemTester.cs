using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// PHASE 13: Testing tool for Interaction System.
/// 
/// PURPOSE:
/// Lets you test house sequences with Interactions in Play Mode
/// without needing full GameManager integration.
/// 
/// USAGE:
/// 1. Open Core_Scene.unity
/// 2. Enter Play Mode
/// 3. Tools → Kashkha → Test Interaction System
/// 4. Select a House Sequence and click "Play Sequence"
/// 5. Watch the live state update as elements play
/// </summary>
public class InteractionSystemTester : EditorWindow
{
    private Vector2 scrollPosition;
    private HouseSequenceData selectedSequence;
    private bool isPlaying = false;
    private string currentElementStatus = "Idle";
    private int currentElementIndex = 0;
    private string testLog = "";

    [MenuItem("Tools/Kashkha/Test Interaction System")]
    public static void ShowWindow()
    {
        var window = GetWindow<InteractionSystemTester>("Interaction System Tester");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "PHASE 13: Interaction System Tester\n\n" +
            "Test house sequences with Questions, Cutscenes, and Interactions.\n" +
            "Requires Core_Scene.unity to be open with all managers in the scene.\n\n" +
            "In Play Mode: Select a sequence and click 'Play Sequence'",
            isPlaying ? MessageType.Warning : MessageType.Info);

        GUILayout.Space(10);

        // Sequence selector
        EditorGUILayout.LabelField("Select House Sequence", EditorStyles.boldLabel);
        selectedSequence = (HouseSequenceData)EditorGUILayout.ObjectField(
            "Sequence Asset", selectedSequence, typeof(HouseSequenceData), false);

        // Auto-find buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find House 1")) selectedSequence = FindSequence(1);
        if (GUILayout.Button("Find House 2")) selectedSequence = FindSequence(2);
        if (GUILayout.Button("Find House 3")) selectedSequence = FindSequence(3);
        if (GUILayout.Button("Find House 4")) selectedSequence = FindSequence(4);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Play button
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode first to test sequences.", MessageType.Warning);
        }

        GUI.enabled = Application.isPlaying && selectedSequence != null && !isPlaying;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("▶ Play Sequence", GUILayout.Height(40)))
        {
            PlaySelectedSequence();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        GUI.enabled = Application.isPlaying && isPlaying;
        GUI.backgroundColor = new Color(1f, 0.5f, 0.3f);
        if (GUILayout.Button("⏹ Stop Sequence", GUILayout.Height(30)))
        {
            StopSequence();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        GUILayout.Space(10);

        // Live status
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Live Status", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Status", currentElementStatus);
            EditorGUILayout.IntField("Current Element", currentElementIndex);
            EditorGUI.EndDisabledGroup();

            if (isPlaying)
            {
                EditorGUILayout.HelpBox("⏳ Sequence is playing... Watch the Game view!", MessageType.None);
            }
        }

        GUILayout.Space(10);

        // Sequence preview
        if (selectedSequence != null)
        {
            EditorGUILayout.LabelField("Sequence Preview", EditorStyles.boldLabel);
            ShowSequencePreview(selectedSequence);
        }

        GUILayout.Space(10);

        // Test log
        EditorGUILayout.LabelField("Test Log", EditorStyles.boldLabel);
        GUI.backgroundColor = Color.black;
        GUI.contentColor = Color.green;
        EditorGUILayout.TextArea(testLog, GUILayout.Height(150));
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("📋 Copy Log"))
        {
            EditorGUIUtility.systemCopyBuffer = testLog;
            ShowNotification(new GUIContent("Log copied!"));
        }

        EditorGUILayout.EndScrollView();
    }

    private HouseSequenceData FindSequence(int houseLevel)
    {
        // Search in Resources/Sequences/ first (correct location)
        string[] guids = AssetDatabase.FindAssets($"t:HouseSequenceData House{houseLevel}", 
            new[] { "Assets/_Project/Resources/Sequences" });
        
        // Fallback: search everywhere
        if (guids.Length == 0)
        {
            guids = AssetDatabase.FindAssets($"t:HouseSequenceData");
        }
        
        if (guids.Length > 0)
        {
            // Find the one matching the house level
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains($"House{houseLevel}"))
                {
                    return AssetDatabase.LoadAssetAtPath<HouseSequenceData>(path);
                }
            }
        }
        return null;
    }

    private void PlaySelectedSequence()
    {
        if (selectedSequence == null)
        {
            EditorUtility.DisplayDialog("Error", "No sequence selected!", "OK");
            return;
        }

        isPlaying = true;
        currentElementIndex = 0;
        currentElementStatus = "Starting...";
        testLog = $"=== TEST STARTED ===\n";
        testLog += $"Sequence: {selectedSequence.name}\n";
        testLog += $"House Level: {selectedSequence.HouseLevel}\n";
        testLog += $"Total Elements: {selectedSequence.Sequence.Count}\n\n";

        LogMessage($"▶ Playing: {selectedSequence.name}");
        LogMessage($"House {selectedSequence.HouseLevel} - {selectedSequence.GetSequenceSummary()}");
        LogMessage("");

        // Start the sequence via HouseFlowController
        if (HouseFlowController.Instance != null)
        {
            LogMessage("✅ HouseFlowController found");
            LogMessage("Starting sequence...");
            LogMessage("");
            
            // The HouseFlowController will handle the rest
            // We just need to subscribe to its events for live tracking
            SubscribeToEvents();
        }
        else
        {
            LogMessage("❌ HouseFlowController NOT found in scene!");
            LogMessage("Make sure Core_Scene.unity is open and HouseFlowController is in the scene.");
            isPlaying = false;
        }
    }

    private void StopSequence()
    {
        isPlaying = false;
        currentElementStatus = "Stopped";
        LogMessage("\n⏹ Sequence stopped by user");
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        HouseFlowController.OnHouseStarted += OnHouseStarted;
        HouseFlowController.OnElementCompleted += OnElementCompleted;
        HouseFlowController.OnHouseCompleted += OnHouseCompleted;
    }

    private void UnsubscribeFromEvents()
    {
        HouseFlowController.OnHouseStarted -= OnHouseStarted;
        HouseFlowController.OnElementCompleted -= OnElementCompleted;
        HouseFlowController.OnHouseCompleted -= OnHouseCompleted;
    }

    private void OnHouseStarted(int houseLevel)
    {
        currentElementStatus = $"House {houseLevel} Started";
        currentElementIndex = 0;
        LogMessage($"🏠 House {houseLevel} started!");
        LogMessage("");
    }

    private void OnElementCompleted(ElementType type, string elementID)
    {
        currentElementIndex++;
        
        string icon = type == ElementType.Question ? "❓" :
                     type == ElementType.Cutscene ? "🎬" : "🎮";
        
        currentElementStatus = $"Completed: {icon} [{type}] {elementID}";
        LogMessage($"✅ Element {currentElementIndex}: {icon} [{type}] {elementID}");
        
        if (type == ElementType.Interaction)
        {
            LogMessage($"   → Interaction completed (check Console for InputManager logs)");
        }
        
        LogMessage("");
    }

    private void OnHouseCompleted(int houseLevel)
    {
        currentElementStatus = $"House {houseLevel} Complete!";
        isPlaying = false;
        
        LogMessage($"🎉 House {houseLevel} COMPLETE!");
        LogMessage("");
        LogMessage("=== TEST FINISHED SUCCESSFULLY ===");
        LogMessage("");
        LogMessage("NEXT STEPS:");
        LogMessage("1. Check if all interactions triggered correctly");
        LogMessage("2. Verify HUD appeared with correct prompts");
        LogMessage("3. Test shake/hold/tap with Space/H/T keys");
        LogMessage("4. Check Console for any errors or warnings");
        
        UnsubscribeFromEvents();
    }

    private void ShowSequencePreview(HouseSequenceData sequence)
    {
        if (sequence.Sequence == null || sequence.Sequence.Count == 0)
        {
            EditorGUILayout.HelpBox("Sequence is empty!", MessageType.Warning);
            return;
        }

        for (int i = 0; i < sequence.Sequence.Count; i++)
        {
            var elem = sequence.Sequence[i];
            if (elem == null) continue;

            string icon = elem.Type == ElementType.Question ? "❓" :
                         elem.Type == ElementType.Cutscene ? "🎬" : "🎮";
            
            string color = elem.Type == ElementType.Interaction ? "#FFD700" : "#FFFFFF";
            
            EditorGUILayout.LabelField(
                $"{i + 1}. {icon} <color={color}>[{elem.Type}]</color> {elem.ElementID}",
                new GUIStyle(EditorStyles.label) { richText = true }
            );

            if (!string.IsNullOrEmpty(elem.DesignerNote))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"💬 {elem.DesignerNote}", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }
    }

    private void LogMessage(string message)
    {
        testLog += message + "\n";
        Debug.Log($"[InteractionTester] {message}");
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}
