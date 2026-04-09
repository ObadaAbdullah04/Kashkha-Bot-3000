using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 9.6 Setup Wizard — Automates creation of all assets for the self-driving house flow.
///
/// Run via: Tools → Kashkha → Phase 9 Setup → [command]
///
/// This script creates:
/// 1. HouseSequenceData ScriptableObjects (Houses 1-4) — pre-populated with CSV question IDs
/// 2. QTE UI Prefab — prompt panel, instruction text, timer, success/fail feedback
/// 3. Cutscene UI Prefab — dialogue text, character sprite slots, CanvasGroup
/// 4. Scene GameObjects — HouseFlowController, QTEController, CutsceneTrigger (auto-wired)
///
/// No Timeline needed! HouseFlowController drives itself with coroutines.
///
/// All operations are idempotent — safe to re-run, won't duplicate existing assets.
/// </summary>
public class Phase9SetupWizard : EditorWindow
{
    private Vector2 scrollPos;
    private bool[] sectionFoldouts = new bool[4] { true, true, true, true };

    [MenuItem("Tools/Kashkha/Phase 9 Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<Phase9SetupWizard>("Phase 9 Setup");
        window.minSize = new Vector2(400, 550);
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Phase 9.6 Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Self-Driving House Flow (No Timeline Needed)", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        // Section 1: HouseSequenceData
        DrawSection(0, "1. HouseSequenceData Assets",
            "Creates ScriptableObject assets for Houses 1-4 with pre-populated element sequences from CSV.",
            "Create Sequences", () => CreateHouseSequences());

        // Section 2: QTE UI Prefab
        DrawSection(1, "2. QTE UI Prefab",
            "Creates QTE_Panel.prefab with prompt panel, instruction text, timer, and feedback panels.",
            "Create QTE Prefab", () => CreateQTEUIPrefab());

        // Section 3: Cutscene UI Prefab
        DrawSection(2, "3. Cutscene UI Prefab",
            "Creates Cutscene_Panel.prefab with dialogue text, character sprite slots, and CanvasGroup.",
            "Create Cutscene Prefab", () => CreateCutsceneUIPrefab());

        // Section 4: Scene GameObjects
        DrawSection(3, "4. Scene GameObjects",
            "Creates HouseFlowController, QTEController, CutsceneTrigger GameObjects with auto-wired references.",
            "Setup Scene", () => SetupSceneGameObjects());

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Tip: Run 'Full Setup' below to create everything at once.", MessageType.Info);
        EditorGUILayout.Space();

        // Full Setup Button
        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.2f);
        if (GUILayout.Button("Run Full Setup (All 4 Steps)", GUILayout.Height(45)))
        {
            if (EditorUtility.DisplayDialog("Full Setup",
                "This will create all Phase 9 assets and scene objects.\n\nExisting assets will NOT be overwritten.\n\nContinue?",
                "Continue", "Cancel"))
            {
                RunFullSetup();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void DrawSection(int index, string title, string description, string buttonText, System.Action action)
    {
        EditorGUILayout.BeginVertical("box");

        sectionFoldouts[index] = EditorGUILayout.Foldout(sectionFoldouts[index], title, true, EditorStyles.foldoutHeader);

        if (sectionFoldouts[index])
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);

            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                action();
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void RunFullSetup()
    {
        Log("--- PHASE 9.6 FULL SETUP STARTED ---");
        CreateHouseSequences();
        CreateQTEUIPrefab();
        CreateCutsceneUIPrefab();
        SetupSceneGameObjects();
        Log("--- PHASE 9.6 FULL SETUP COMPLETE ---");
        EditorUtility.DisplayDialog("Full Setup Complete",
            "All Phase 9 assets have been created.\n\nCheck the Console for details.\n\nNext steps:\n1. Assign CSV files to DataManager\n2. Press Play to test!",
            "OK");
    }

    private static void Log(string message)
    {
        Debug.Log($"[Phase9Setup] {message}");
    }

    #region 1. HouseSequenceData Creation

    [MenuItem("Tools/Kashkha/Phase 9 Setup/1. Create HouseSequenceData Assets")]
    public static void CreateHouseSequences()
    {
        string folder = "Assets/_Project/Resources/Sequences";
        EnsureFolderExists(folder);

        // Pre-defined sequences for each house (using actual CSV IDs)
        var houseConfigs = new Dictionary<int, (string[] questions, string[] qtes, string[] cutscenes)>
        {
            {
                1, (
                    questions: new[] { "Q1", "Q4", "Q7", "Q5", "Q8" },
                    qtes: new[] { "QTE_Shake_Coffee" },
                    cutscenes: new[] { "Cutscene_FinishCoffee", "Cutscene_Aunt_Smile" }
                )
            },
            {
                2, (
                    questions: new[] { "Q11", "Q14", "Q12", "Q13" },
                    qtes: new[] { "QTE_Swipe_Greeting", "QTE_Hold_Dua" },
                    cutscenes: new[] { "Cutscene_Uncle_Nod", "Cutscene_Coffee_Pour" }
                )
            },
            {
                3, (
                    questions: new[] { "Q21", "Q23", "Q25", "Q27", "Q29", "Q22" },
                    qtes: new[] { "QTE_Shake_Cup", "QTE_Swipe_Food", "QTE_Hold_Respect" },
                    cutscenes: new[] { "Cutscene_Respect_Elder", "Cutscene_Family_Gather" }
                )
            },
            {
                4, (
                    questions: new[] { "Q31", "Q33", "Q35", "Q37", "Q32", "Q34" },
                    qtes: new[] { "QTE_Double_Shake", "QTE_Fast_Swipe", "QTE_Hold_Endure" },
                    cutscenes: new[] { "Cutscene_Boss_Intro", "Cutscene_Boss_Win", "Cutscene_Boss_Fail" }
                )
            }
        };

        int created = 0, skipped = 0;

        foreach (var kvp in houseConfigs)
        {
            int houseLevel = kvp.Key;
            var config = kvp.Value;
            string assetPath = $"{folder}/House{houseLevel}_Sequence.asset";

            // Check if already exists
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), assetPath)))
            {
                Log($"House {houseLevel} Sequence already exists — skipping.");
                skipped++;
                continue;
            }

            // Create the ScriptableObject
            var sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
            sequence.name = $"House{houseLevel}_Sequence";
            sequence.HouseLevel = houseLevel;
            sequence.Sequence = new List<SequenceElement>();

            // Build interleaved sequence: Question → Question → QTE → Cutscene → Question...
            int qIdx = 0, qteIdx = 0, csIdx = 0;
            bool first = true;

            while (qIdx < config.questions.Length || qteIdx < config.qtes.Length || csIdx < config.cutscenes.Length)
            {
                // Always start with 2 questions
                if (!first || qIdx < 2)
                {
                    if (qIdx < config.questions.Length)
                    {
                        sequence.Sequence.Add(new SequenceElement(ElementType.Question, config.questions[qIdx]));
                        qIdx++;
                    }
                }

                // Then alternate: QTE → Question → Cutscene → Question
                if (qteIdx < config.qtes.Length)
                {
                    sequence.Sequence.Add(new SequenceElement(ElementType.QTE, config.qtes[qteIdx]));
                    qteIdx++;
                }

                if (qIdx < config.questions.Length)
                {
                    sequence.Sequence.Add(new SequenceElement(ElementType.Question, config.questions[qIdx]));
                    qIdx++;
                }

                if (csIdx < config.cutscenes.Length)
                {
                    sequence.Sequence.Add(new SequenceElement(ElementType.Cutscene, config.cutscenes[csIdx]));
                    csIdx++;
                }

                first = false;

                // Safety break
                if (sequence.Sequence.Count > 30) break;
            }

            // Save asset
            AssetDatabase.CreateAsset(sequence, assetPath);
            created++;
            Log($"Created House {houseLevel} Sequence: {sequence.GetSequenceSummary()}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Log($"HouseSequenceData: {created} created, {skipped} skipped.");
        EditorUtility.DisplayDialog("HouseSequenceData Complete",
            $"{created} created, {skipped} skipped.\n\nAssets saved to: {folder}", "OK");
    }

    #endregion

    #region 2. QTE UI Prefab Creation

    [MenuItem("Tools/Kashkha/Phase 9 Setup/2. Create QTE UI Prefab")]
    public static void CreateQTEUIPrefab()
    {
        string folder = "Assets/_Project/Prefabs/UI";
        EnsureFolderExists(folder);

        string prefabPath = $"{folder}/QTE_Panel.prefab";

        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), prefabPath)))
        {
            Log("QTE_Panel.prefab already exists — skipping.");
            EditorUtility.DisplayDialog("Prefab Exists", "QTE_Panel.prefab already exists.\nDelete it first if you want to recreate.", "OK");
            return;
        }

        // Create root GameObject
        var root = new GameObject("QTE_Panel", typeof(RectTransform), typeof(CanvasGroup));
        root.layer = LayerMask.NameToLayer("UI");

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        CanvasGroup rootCanvasGroup = root.GetComponent<CanvasGroup>();
        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        // Add Image for background
        var bgImage = root.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.85f);

        // ─── QTE Prompt Panel ───
        var promptPanel = CreatePanelChild("PromptPanel", root, new Color(0.15f, 0.2f, 0.35f, 0.95f));
        promptPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 300);
        promptPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);

        // Add border via Outline
        var promptOutline = promptPanel.AddComponent<UnityEngine.UI.Outline>();
        promptOutline.effectColor = new Color(0.8f, 0.7f, 0.3f, 1f);
        promptOutline.effectDistance = new Vector2(3, 3);

        // Instruction Text
        var instructionText = CreateRTLTextChild("InstructionText", promptPanel, "هز الهاتف للشرب!", 32, TextAlignmentOptions.Center);
        instructionText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
        instructionText.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 50);

        // Timer Text
        var timerText = CreateRTLTextChild("TimerText", promptPanel, "5", 48, TextAlignmentOptions.Center);
        timerText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        timerText.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 60);
        timerText.GetComponent<RTLTMPro.RTLTextMeshPro>().color = new Color(1f, 0.85f, 0f);

        // Hint Text
        var hintText = CreateRTLTextChild("HintText", promptPanel, "اضغط Space في المحرر", 18, TextAlignmentOptions.Center);
        hintText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        hintText.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 30);
        hintText.GetComponent<RTLTMPro.RTLTextMeshPro>().color = new Color(0.6f, 0.6f, 0.6f);

        // ─── Success Feedback Panel ───
        var successPanel = CreatePanelChild("SuccessFeedbackPanel", root, new Color(0.1f, 0.3f, 0.1f, 0.9f));
        successPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 100);
        successPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
        successPanel.SetActive(false);

        var successText = CreateRTLTextChild("SuccessText", successPanel, "أحسنت! ✅", 28, TextAlignmentOptions.Center);
        successText.GetComponent<RTLTMPro.RTLTextMeshPro>().color = new Color(0.3f, 1f, 0.3f);

        // ─── Fail Feedback Panel ───
        var failPanel = CreatePanelChild("FailFeedbackPanel", root, new Color(0.3f, 0.1f, 0.1f, 0.9f));
        failPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 100);
        failPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
        failPanel.SetActive(false);

        var failText = CreateRTLTextChild("FailText", failPanel, "فشلت! ❌", 28, TextAlignmentOptions.Center);
        failText.GetComponent<RTLTMPro.RTLTextMeshPro>().color = new Color(1f, 0.3f, 0.3f);

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Log($"Created QTE_Panel.prefab at {prefabPath}");
        EditorUtility.DisplayDialog("QTE Prefab Created",
            "QTE_Panel.prefab created successfully!\n\nNext: Assign to QTEController inspector fields.",
            "OK");
    }

    #endregion

    #region 3. Cutscene UI Prefab Creation

    [MenuItem("Tools/Kashkha/Phase 9 Setup/3. Create Cutscene UI Prefab")]
    public static void CreateCutsceneUIPrefab()
    {
        string folder = "Assets/_Project/Prefabs/UI";
        EnsureFolderExists(folder);

        string prefabPath = $"{folder}/Cutscene_Panel.prefab";

        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), prefabPath)))
        {
            Log("Cutscene_Panel.prefab already exists — skipping.");
            EditorUtility.DisplayDialog("Prefab Exists", "Cutscene_Panel.prefab already exists.\nDelete it first if you want to recreate.", "OK");
            return;
        }

        // Create root
        var root = new GameObject("Cutscene_Panel", typeof(RectTransform), typeof(CanvasGroup));
        root.layer = LayerMask.NameToLayer("UI");

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        CanvasGroup rootCanvasGroup = root.GetComponent<CanvasGroup>();
        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        // Background
        var bgImage = root.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

        // ─── Character Sprite Slot ───
        var spriteContainer = CreatePanelChild("CharacterSpriteContainer", root, new Color(0.1f, 0.1f, 0.15f, 0.5f));
        spriteContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 200);
        spriteContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 50);

        var characterSprite = CreateImageChild("CharacterSprite", spriteContainer, new Color(0.3f, 0.3f, 0.4f, 1f));
        characterSprite.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 180);

        var spriteLabel = CreateRTLTextChild("SpriteLabel", spriteContainer, "الشخصية", 18, TextAlignmentOptions.Center);
        spriteLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -115);
        spriteLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 25);
        spriteLabel.GetComponent<RTLTMPro.RTLTextMeshPro>().color = new Color(0.5f, 0.5f, 0.5f);

        // ─── Dialogue Text ───
        var dialogueText = CreateRTLTextChild("DialogueText", root, "نص المشهد...", 30, TextAlignmentOptions.Center);
        dialogueText.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 80);
        dialogueText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

        // ─── Secondary Text (for dialogue type) ───
        var secondaryText = CreateRTLTextChild("SecondaryText", root, "", 24, TextAlignmentOptions.Center);
        secondaryText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 50);
        secondaryText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        secondaryText.gameObject.SetActive(false);

        // ─── Secondary Character Sprite ───
        var secondarySpriteContainer = CreatePanelChild("SecondarySpriteContainer", root, new Color(0.1f, 0.1f, 0.15f, 0.5f));
        secondarySpriteContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 150);
        secondarySpriteContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(250, 50);
        secondarySpriteContainer.SetActive(false);

        var secondarySprite = CreateImageChild("SecondarySprite", secondarySpriteContainer, new Color(0.3f, 0.3f, 0.4f, 1f));
        secondarySprite.GetComponent<RectTransform>().sizeDelta = new Vector2(130, 130);

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Log($"Created Cutscene_Panel.prefab at {prefabPath}");
        EditorUtility.DisplayDialog("Cutscene Prefab Created",
            "Cutscene_Panel.prefab created successfully!\n\nNext: Assign to CutsceneTrigger inspector fields.",
            "OK");
    }

    #endregion

    #region 4. Scene GameObject Setup

    [MenuItem("Tools/Kashkha/Phase 9 Setup/4. Setup Scene GameObjects")]
    public static void SetupSceneGameObjects()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Log("Warning: Active scene has unsaved changes.");
        }

        int created = 0;

        // ─── HouseFlowController ───
        var hfc = GameObject.Find("HouseFlowController");
        if (hfc == null)
        {
            hfc = new GameObject("HouseFlowController");
            hfc.AddComponent<HouseFlowController>();

            var hfcComp = hfc.GetComponent<HouseFlowController>();

            // Find SwipeEncounterManager
            var swipeMgr = FindAnyComponent<SwipeEncounterManager>();
            if (swipeMgr != null)
            {
                SetPrivateField(hfcComp, "swipeEncounterManager", swipeMgr);
                Log("  HouseFlowController → SwipeEncounterManager wired.");
            }

            // Find QTEController
            var qteCtrl = FindAnyComponent<QTEController>();
            if (qteCtrl != null)
            {
                SetPrivateField(hfcComp, "qteController", qteCtrl);
                Log("  HouseFlowController → QTEController wired.");
            }

            // Find CutsceneTrigger
            var cutscene = FindAnyComponent<CutsceneTrigger>();
            if (cutscene != null)
            {
                SetPrivateField(hfcComp, "cutsceneTrigger", cutscene);
                Log("  HouseFlowController → CutsceneTrigger wired.");
            }

            SetPrivateField(hfcComp, "debugLogging", true);

            created++;
            Log("Created HouseFlowController GameObject with auto-wired references.");
        }
        else
        {
            Log("HouseFlowController already exists — skipping.");
        }

        // ─── QTEController ───
        var qte = GameObject.Find("QTEController");
        if (qte == null)
        {
            qte = new GameObject("QTEController");
            qte.AddComponent<QTEController>();
            SetPrivateField(qte.GetComponent<QTEController>(), "debugLogging", true);
            created++;
            Log("Created QTEController GameObject.");
        }
        else
        {
            Log("QTEController already exists — skipping.");
        }

        // ─── CutsceneTrigger ───
        var cut = GameObject.Find("CutsceneTrigger");
        if (cut == null)
        {
            cut = new GameObject("CutsceneTrigger");
            cut.AddComponent<CutsceneTrigger>();
            SetPrivateField(cut.GetComponent<CutsceneTrigger>(), "debugLogging", true);
            created++;
            Log("Created CutsceneTrigger GameObject.");
        }
        else
        {
            Log("CutsceneTrigger already exists — skipping.");
        }

        // ─── Phase9_Controllers parent ───
        var parent = GameObject.Find("Phase9_Controllers");
        if (parent == null)
        {
            parent = new GameObject("Phase9_Controllers");
            if (hfc != null) hfc.transform.SetParent(parent.transform, false);
            if (qte != null) qte.transform.SetParent(parent.transform, false);
            if (cut != null) cut.transform.SetParent(parent.transform, false);
            created++;
            Log("Created Phase9_Controllers parent and organized GameObjects.");
        }
        else
        {
            Log("Phase9_Controllers parent already exists — skipping.");
        }

        // Mark scene dirty
        if (created > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Log($"Scene setup: {created} GameObjects created.");
        EditorUtility.DisplayDialog("Scene Setup Complete",
            $"{created} GameObject(s) created.\n\nCheck the Hierarchy for:\n- Phase9_Controllers (parent)\n  - HouseFlowController\n  - QTEController\n  - CutsceneTrigger\n\nNext: Assign UI prefab references in inspectors.",
            "OK");
    }

    #endregion

    #region Helper Methods

    private static GameObject CreatePanelChild(string name, GameObject parent, Color bgColor)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(parent.transform, false);
        panel.layer = LayerMask.NameToLayer("UI");
        var img = panel.GetComponent<UnityEngine.UI.Image>();
        img.color = bgColor;
        var rect = panel.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return panel;
    }

    private static GameObject CreateRTLTextChild(string name, GameObject parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        var textObj = new GameObject(name, typeof(RectTransform), typeof(RTLTMPro.RTLTextMeshPro));
        textObj.transform.SetParent(parent.transform, false);
        textObj.layer = LayerMask.NameToLayer("UI");
        var tmp = textObj.GetComponent<RTLTMPro.RTLTextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        var rect = textObj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(300, 40);
        return textObj;
    }

    private static GameObject CreateImageChild(string name, GameObject parent, Color color)
    {
        var imgObj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        imgObj.transform.SetParent(parent.transform, false);
        imgObj.layer = LayerMask.NameToLayer("UI");
        var img = imgObj.GetComponent<UnityEngine.UI.Image>();
        img.color = color;
        var rect = imgObj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return imgObj;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), folderPath)))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), folderPath));
            AssetDatabase.Refresh();
        }
    }

    private static T FindAnyComponent<T>() where T : Component
    {
        var all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        return all.Length > 0 ? all[0] : null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    #endregion
}
