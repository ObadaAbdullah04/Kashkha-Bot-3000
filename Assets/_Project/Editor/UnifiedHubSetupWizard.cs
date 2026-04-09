using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using RTLTMPro;
using TMPro;

/// <summary>
/// Editor script to automatically build the UnifiedHubPanel UI hierarchy.
/// Run via: Tools → Kashkha → Setup Unified Hub
/// </summary>
public class UnifiedHubSetupWizard : EditorWindow
{
    private Canvas targetCanvas;

    [MenuItem("Tools/Kashkha/Setup Unified Hub")]
    public static void ShowWindow()
    {
        GetWindow<UnifiedHubSetupWizard>("Unified Hub Setup");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unified Hub Auto-Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetCanvas = EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true) as Canvas;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create the entire UnifiedHubPanel UI hierarchy with all buttons, text, and panels pre-configured.", MessageType.Info);

        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("🏗️ Create UnifiedHubPanel", GUILayout.Height(40)))
        {
            CreateUnifiedHub();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(1f, 0.5f, 0.2f);
        if (GUILayout.Button("🗑️ Delete Old Panels", GUILayout.Height(30)))
        {
            DeleteOldPanels();
        }
        GUI.backgroundColor = Color.white;
    }

    private void CreateUnifiedHub()
    {
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Canvas first!", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(targetCanvas.gameObject, "Create UnifiedHub");

        // Create main panel
        GameObject hubPanel = CreatePanel("UnifiedHubPanel", targetCanvas.transform);
        hubPanel.AddComponent<UnifiedHubManager>();

        // Add CanvasGroup for fade animations
        CanvasGroup canvasGroup = hubPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Add background image
        Image bgImage = hubPanel.GetComponent<Image>();
        bgImage.color = new Color(0.1f, 0.15f, 0.25f, 0.95f);

        // Set anchor to stretch
        RectTransform rect = hubPanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // ─── Tab Buttons ───
        GameObject tabButtonsPanel = CreatePanel("TabButtonsPanel", hubPanel.transform);
        tabButtonsPanel.transform.localPosition = new Vector3(0, 220, 0);
        HorizontalLayoutGroup hLayout = tabButtonsPanel.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = false;
        hLayout.spacing = 20;
        hLayout.padding = new RectOffset(20, 20, 10, 10);

        CreateTabButton("HousesTabButton", tabButtonsPanel.transform, "🏠 البيوت", out GameObject housesButton);
        CreateTabButton("WardrobeTabButton", tabButtonsPanel.transform, "👗 الملابس", out GameObject wardrobeButton);
        CreateTabButton("UpgradesTabButton", tabButtonsPanel.transform, "🛠️ التطويرات", out GameObject upgradesButton);

        // ─── Houses Tab Panel ───
        GameObject housesTabPanel = CreatePanel("HousesTabPanel", hubPanel.transform);
        housesTabPanel.transform.localPosition = new Vector3(0, -20, 0);
        VerticalLayoutGroup housesLayout = housesTabPanel.AddComponent<VerticalLayoutGroup>();
        housesLayout.childAlignment = TextAnchor.UpperCenter;
        housesLayout.spacing = 15;
        housesLayout.padding = new RectOffset(30, 30, 20, 20);

        // Houses Section
        GameObject housesSection = CreatePanel("HousesSection", housesTabPanel.transform);
        VerticalLayoutGroup housesSectionLayout = housesSection.AddComponent<VerticalLayoutGroup>();
        housesSectionLayout.spacing = 10;
        housesSectionLayout.childForceExpandWidth = true;
        housesSectionLayout.childForceExpandHeight = false;

        CreateHouseButton("House1Button", housesSection.transform, "بيت خالة أم محمد", 1, out GameObject h1Btn);
        CreateHouseButton("House2Button", housesSection.transform, "بيت أبو علي", 2, out GameObject h2Btn);
        CreateHouseButton("House3Button", housesSection.transform, "بيت الجارة فاطمة", 3, out GameObject h3Btn);
        CreateHouseButton("House4Button", housesSection.transform, "بيت العم خالد (INSANE)", 4, out GameObject h4Btn);

        // Mini-games Section
        GameObject miniGamesSection = CreatePanel("MiniGamesSection", housesTabPanel.transform);
        VerticalLayoutGroup miniGamesLayout = miniGamesSection.AddComponent<VerticalLayoutGroup>();
        miniGamesLayout.spacing = 10;
        miniGamesLayout.childForceExpandWidth = true;
        miniGamesLayout.childForceExpandHeight = false;

        CreateMiniGameButton("MiniGame1Button", miniGamesSection.transform, "لعبة تجميع العيدية");
        CreateMiniGameButton("MiniGame2Button", miniGamesSection.transform, "لعبة تجنب المعمول");
        CreateMiniGameButton("MiniGame3Button", miniGamesSection.transform, "لعبة الرسم على الطريق");

        // Action Button
        CreateActionButton("ActionButton", housesTabPanel.transform, "ابدأ البيت 1", out GameObject actionBtn, out GameObject actionBtnText);

        // ─── Wardrobe Tab Panel ───
        GameObject wardrobeTabPanel = CreatePanel("WardrobeTabPanel", hubPanel.transform);
        wardrobeTabPanel.transform.localPosition = new Vector3(0, -20, 0);
        wardrobeTabPanel.SetActive(false);

        VerticalLayoutGroup wardrobeLayout = wardrobeTabPanel.AddComponent<VerticalLayoutGroup>();
        wardrobeLayout.childAlignment = TextAnchor.UpperCenter;
        wardrobeLayout.spacing = 15;
        wardrobeLayout.padding = new RectOffset(30, 30, 20, 20);

        // Scrap Counter
        GameObject scrapText = CreateText("ScrapCounterText", wardrobeTabPanel.transform, "0 خردة", 36, TextAlignmentOptions.TopRight);
        scrapText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, -30);

        // Outfits Grid
        GameObject outfitsGrid = CreatePanel("OutfitsGrid", wardrobeTabPanel.transform);
        GridLayoutGroup grid = outfitsGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250, 120);
        grid.spacing = new Vector2(15, 15);
        grid.childAlignment = TextAnchor.UpperCenter;

        for (int i = 1; i <= 3; i++)
        {
            CreateOutfitSlot($"OutfitSlot{i}", outfitsGrid.transform, i);
        }

        // ─── Upgrades Tab Panel ───
        GameObject upgradesTabPanel = CreatePanel("UpgradesTabPanel", hubPanel.transform);
        upgradesTabPanel.transform.localPosition = new Vector3(0, -20, 0);
        upgradesTabPanel.SetActive(false);

        VerticalLayoutGroup upgradesLayout = upgradesTabPanel.AddComponent<VerticalLayoutGroup>();
        upgradesLayout.childAlignment = TextAnchor.UpperCenter;
        upgradesLayout.spacing = 15;
        upgradesLayout.padding = new RectOffset(40, 40, 30, 30);

        CreateUpgradeButton("RechargeUpgradeButton", upgradesTabPanel.transform,
            "إعادة شحن البطارية", "+25 بطارية", "5 خردة", "0/3");

        CreateUpgradeButton("ExpandUpgradeButton", upgradesTabPanel.transform,
            "توسيع البطارية", "+20 حد أقصى", "10 خردة", "0/2");

        CreateUpgradeButton("TitaniumUpgradeButton", upgradesTabPanel.transform,
            "معدة تيتانيوم", "-10% امتلاء", "8 خردة", "0/2");

        // ─── Celebration Panel ───
        GameObject celebrationPanel = CreatePanel("CelebrationPanel", hubPanel.transform);
        celebrationPanel.SetActive(false);
        VerticalLayoutGroup celebLayout = celebrationPanel.AddComponent<VerticalLayoutGroup>();
        celebLayout.childAlignment = TextAnchor.MiddleCenter;
        celebLayout.spacing = 20;

        CreateText("CelebrationText", celebrationPanel.transform, "أحسنت! خلصت كل البيوت!", 40, TextAlignmentOptions.Center);
        CreateButton("PlayAgainButton", celebrationPanel.transform, "العب مرة ثانية");

        // Set initial states
        housesTabPanel.SetActive(true);
        wardrobeTabPanel.SetActive(false);
        upgradesTabPanel.SetActive(false);
        celebrationPanel.SetActive(false);

        Selection.activeGameObject = hubPanel;
        EditorUtility.DisplayDialog("Success!", "UnifiedHubPanel created successfully!\n\nNow wire the fields in the Inspector.", "OK");
        Debug.Log("[Setup] UnifiedHubPanel created!");
    }

    #region Helper Methods

    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image img = panel.GetComponent<Image>();
        img.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
        return panel;
    }

    private GameObject CreateButton(string name, Transform parent, string text)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
        btnObj.transform.SetParent(parent, false);

        Button btn = btnObj.GetComponent<Button>();
        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.25f, 0.35f, 0.5f, 1f);

        CreateText("Text", btnObj.transform, text, 24, TextAlignmentOptions.Center);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 60);

        return btnObj;
    }

    private void CreateTabButton(string name, Transform parent, string text, out GameObject button)
    {
        button = CreateButton(name, parent, text);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);
    }

    private void CreateHouseButton(string name, Transform parent, string houseName, int houseNum, out GameObject button)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
        btnObj.transform.SetParent(parent, false);

        Button btn = btnObj.GetComponent<Button>();
        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.3f, 0.4f, 0.6f, 1f);

        CreateText("Text", btnObj.transform, houseName, 22, TextAlignmentOptions.Center);

        // Add checkmark (hidden by default)
        GameObject check = new GameObject($"{name}Checkmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(btnObj.transform, false);
        check.GetComponent<Image>().color = Color.green;
        check.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
        check.GetComponent<RectTransform>().anchoredPosition = new Vector2(140, 0);
        check.SetActive(false);

        // Add lock for houses 2-4
        if (houseNum > 1)
        {
            GameObject lockObj = new GameObject($"{name}Lock", typeof(RectTransform), typeof(Image));
            lockObj.transform.SetParent(btnObj.transform, false);
            lockObj.GetComponent<Image>().color = Color.red;
            lockObj.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
            lockObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(140, 0);
            lockObj.SetActive(true);
        }

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350, 50);

        button = btnObj;
    }

    private void CreateMiniGameButton(string name, Transform parent, string text)
    {
        CreateButton(name, parent, text);
    }

    private void CreateActionButton(string name, Transform parent, string text, out GameObject button, out GameObject textObj)
    {
        button = CreateButton(name, parent, text);
        textObj = button.transform.Find("Text").gameObject;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 70);
    }

    private void CreateOutfitSlot(string name, Transform parent, int slotNum)
    {
        GameObject slotObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image), typeof(OutfitSlot));
        slotObj.transform.SetParent(parent, false);

        Image img = slotObj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.25f, 0.35f, 1f);

        CreateText("NameText", slotObj.transform, $"ملابس {slotNum}", 20, TextAlignmentOptions.Center);

        // Add owned checkmark (hidden)
        GameObject check = new GameObject("OwnedCheckmark", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(slotObj.transform, false);
        check.GetComponent<Image>().color = Color.green;
        check.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);
        check.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, 0);
        check.SetActive(false);

        slotObj.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 120);
    }

    private void CreateUpgradeButton(string name, Transform parent, string title, string desc, string cost, string level)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
        btnObj.transform.SetParent(parent, false);

        Button btn = btnObj.GetComponent<Button>();
        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.3f, 0.45f, 1f);

        // Add VerticalLayoutGroup for internal layout
        VerticalLayoutGroup layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 10, 10);
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("TitleText", btnObj.transform, title, 22, TextAlignmentOptions.Left);
        CreateText("DescText", btnObj.transform, desc, 18, TextAlignmentOptions.Left);
        CreateText("CostText", btnObj.transform, cost, 20, TextAlignmentOptions.Right);
        CreateText("LevelText", btnObj.transform, level, 16, TextAlignmentOptions.Right);

        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 100);
    }

    private GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(RTLTextMeshPro));
        textObj.transform.SetParent(parent, false);

        RTLTextMeshPro tmp = textObj.GetComponent<RTLTextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 40);

        return textObj;
    }

    #endregion

    #region Delete Old Panels

    private void DeleteOldPanels()
    {
        string[] oldNames = { "HouseHubPanel", "TechPitStopPanel", "WardrobePanel" };
        int deleted = 0;

        foreach (string name in oldNames)
        {
            GameObject old = GameObject.Find(name);
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
                deleted++;
                Debug.Log($"[Setup] Deleted old panel: {name}");
            }
        }

        // Delete old manager GameObjects
        string[] oldManagers = { "HouseHubManager", "HubUpgradeManager" };
        foreach (string name in oldManagers)
        {
            GameObject old = GameObject.Find(name);
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old);
                deleted++;
                Debug.Log($"[Setup] Deleted old manager: {name}");
            }
        }

        if (deleted > 0)
        {
            EditorUtility.DisplayDialog("Cleanup Complete", $"Deleted {deleted} old GameObject(s).", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Nothing to Delete", "No old panels/managers found.", "OK");
        }
    }

    #endregion
}
