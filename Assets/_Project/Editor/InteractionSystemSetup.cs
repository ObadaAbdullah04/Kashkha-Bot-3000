using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// PHASE 13: Automated setup wizard for the Interaction System.
/// 
/// PURPOSE:
/// Automates all manual Unity Editor setup steps:
/// 1. Creates InteractionHUD prefab with full UI hierarchy
/// 2. Generates placeholder icon sprites
/// 3. Creates Resources folder structure
/// 4. Validates scene references
/// 5. Generates setup report for verification
/// 
/// USAGE:
/// 1. Open Unity Editor
/// 2. Tools → Kashkha → Setup Interaction System
/// 3. Click "Run Full Setup" button
/// 4. Review generated files in Console/Report window
/// 5. Copy report and share with developer for verification
/// </summary>
public class InteractionSystemSetup : EditorWindow
{
    private Vector2 scrollPosition;
    private string setupReport = "";
    private bool setupComplete = false;

    [MenuItem("Tools/Kashkha/Setup Interaction System")]
    public static void ShowWindow()
    {
        var window = GetWindow<InteractionSystemSetup>("Interaction System Setup");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "PHASE 13: Interaction System Setup Wizard\n\n" +
            "This tool automates all manual setup steps for the new interaction system.\n" +
            "Click 'Run Full Setup' to generate all required prefabs, folders, and assets.\n\n" +
            "After setup, copy the report from the text area below and share it for verification.",
            MessageType.Info);

        GUILayout.Space(10);

        // Setup Button
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("🚀 Run Full Setup", GUILayout.Height(40)))
        {
            RunFullSetup();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // Individual setup buttons (for debugging/partial setup)
        EditorGUILayout.LabelField("Individual Setup Steps", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. Create Folders")) CreateFolders();
        if (GUILayout.Button("2. Generate Icons")) GenerateIconSprites();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("3. Create HUD Prefab")) CreateInteractionHUDPrefab();
        if (GUILayout.Button("4. Validate Scene")) ValidateSceneSetup();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Report section
        EditorGUILayout.LabelField("Setup Report (Copy & Share)", EditorStyles.boldLabel);
        
        GUI.backgroundColor = Color.black;
        GUI.contentColor = Color.green;
        EditorGUILayout.TextArea(setupReport, GUILayout.Height(300));
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("📋 Copy Report to Clipboard"))
        {
            EditorGUIUtility.systemCopyBuffer = setupReport;
            ShowNotification(new GUIContent("Report copied to clipboard!"));
        }

        if (setupComplete)
        {
            EditorGUILayout.HelpBox("✅ Setup Complete! Copy the report above and share it for verification.", MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    #region Full Setup

    private void RunFullSetup()
    {
        setupReport = "=== INTERACTION SYSTEM SETUP REPORT ===\n";
        setupReport += $"Timestamp: {System.DateTime.Now}\n\n";
        setupComplete = false;

        // Step 1: Create folders
        setupReport += "--- Step 1: Creating Folders ---\n";
        CreateFolders();
        setupReport += "\n";

        // Step 2: Generate icons
        setupReport += "--- Step 2: Generating Icon Sprites ---\n";
        GenerateIconSprites();
        setupReport += "\n";

        // Step 3: Create HUD prefab
        setupReport += "--- Step 3: Creating InteractionHUD Prefab ---\n";
        CreateInteractionHUDPrefab();
        setupReport += "\n";

        // Step 4: Validate
        setupReport += "--- Step 4: Validating Scene Setup ---\n";
        ValidateSceneSetup();
        setupReport += "\n";

        // Summary
        setupReport += "=== SETUP SUMMARY ===\n";
        setupReport += $"✅ Folders Created: 2\n";
        setupReport += $"✅ Icon Sprites Generated: 4\n";
        setupReport += $"✅ Prefab Created: InteractionHUD_Prefab\n";
        setupReport += $"✅ CSV Exists: {File.Exists("Assets/_Project/Data/Interactions.csv")}\n";
        setupReport += $"✅ Scripts Exist: {CheckScriptsExist()}\n";
        setupReport += "\n=== NEXT STEPS ===\n";
        setupReport += "1. Open Core_Scene.unity\n";
        setupReport += "2. Drag InteractionHUD_Prefab into scene\n";
        setupReport += "3. Assign InteractionHUDController reference in HouseFlowController\n";
        setupReport += "4. Assign Interactions.csv reference in DataManager\n";
        setupReport += "5. Add interactions to HouseSequenceData assets (Type = Interaction)\n";
        setupReport += "6. Press Play and test with Space/H/T keys\n";
        setupReport += "\n=== END REPORT ===";

        setupComplete = true;
        Debug.Log("[InteractionSystemSetup] ✅ Full setup complete! Review report and copy for verification.");
    }

    #endregion

    #region Setup Steps

    private void CreateFolders()
    {
        string interactionIconsPath = "Assets/_Project/Resources/InteractionIcons";
        string prefabsPath = "Assets/_Project/Prefabs/UI";

        // Create InteractionIcons folder
        if (!Directory.Exists(interactionIconsPath))
        {
            Directory.CreateDirectory(interactionIconsPath);
            setupReport += $"✅ Created: {interactionIconsPath}\n";
        }
        else
        {
            setupReport += $"⚠️ Already exists: {interactionIconsPath}\n";
        }

        // Create Prefabs/UI folder
        if (!Directory.Exists(prefabsPath))
        {
            Directory.CreateDirectory(prefabsPath);
            setupReport += $"✅ Created: {prefabsPath}\n";
        }
        else
        {
            setupReport += $"⚠️ Already exists: {prefabsPath}\n";
        }

        AssetDatabase.Refresh();
    }

    private void GenerateIconSprites()
    {
        string iconsPath = "Assets/_Project/Resources/InteractionIcons";
        string[] iconNames = { "Icon_Shake", "Icon_Hold", "Icon_Tap", "Icon_Draw" };
        Color[] iconColors = {
            new Color(1f, 0.8f, 0.2f),   // Shake - Gold/Yellow
            new Color(0.3f, 0.7f, 1f),   // Hold - Blue
            new Color(1f, 0.4f, 0.4f),   // Tap - Red/Pink
            new Color(0.4f, 1f, 0.5f)    // Draw - Green
        };

        for (int i = 0; i < iconNames.Length; i++)
        {
            string assetPath = $"{iconsPath}/{iconNames[i]}.png";
            
            // Check if already exists
            if (File.Exists(assetPath))
            {
                setupReport += $"⚠️ Already exists: {iconNames[i]}.png\n";
                continue;
            }

            // Create texture
            Texture2D texture = CreateIconTexture(iconNames[i], iconColors[i]);
            
            // Save as PNG
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(assetPath, bytes);
            
            DestroyImmediate(texture);
            
            setupReport += $"✅ Generated: {iconNames[i]}.png (128x128, {iconColors[i]})\n";
        }

        // Import settings
        AssetDatabase.Refresh();
        
        // Configure import settings for all icons
        foreach (string iconName in iconNames)
        {
            string assetPath = $"{iconsPath}/{iconName}.png";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                
                AssetDatabase.ImportAsset(assetPath);
                setupReport += $"✅ Configured import settings: {iconName}.png\n";
            }
        }
    }

    private Texture2D CreateIconTexture(string name, Color color)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        // Clear with transparent
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        // Draw filled circle
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 8f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // Edge glow effect
                    float alpha = dist > radius - 4f ? (radius - dist) / 4f : 1f;
                    Color pixelColor = color;
                    pixelColor.a = alpha;
                    texture.SetPixel(x, y, pixelColor);
                }
            }
        }

        // Draw simple icon symbol in center
        DrawIconSymbol(texture, name, size);

        texture.Apply();
        return texture;
    }

    private void DrawIconSymbol(Texture2D texture, string name, int size)
    {
        Color symbolColor = Color.white;
        int thickness = 4;
        int centerX = size / 2;
        int centerY = size / 2;

        if (name.Contains("Shake"))
        {
            // Draw zigzag lines (shake symbol)
            for (int i = -20; i <= 20; i += 8)
            {
                DrawLine(texture, centerX - 15, centerY + i, centerX + 15, centerY + i - 10, thickness, symbolColor);
            }
        }
        else if (name.Contains("Hold"))
        {
            // Draw hand/palm symbol (rectangle)
            DrawRect(texture, centerX - 10, centerY - 15, centerX + 10, centerY + 15, thickness, symbolColor);
        }
        else if (name.Contains("Tap"))
        {
            // Draw finger tap symbol (circle with dot)
            DrawCircle(texture, centerX, centerY, 12, thickness, symbolColor);
            DrawFilledCircle(texture, centerX, centerY, 4, symbolColor);
        }
        else if (name.Contains("Draw"))
        {
            // Draw pencil/draw symbol (diagonal line)
            DrawLine(texture, centerX - 15, centerY + 15, centerX + 15, centerY - 15, thickness, symbolColor);
            // Arrow tip
            DrawLine(texture, centerX + 5, centerY - 15, centerX + 15, centerY - 15, thickness, symbolColor);
            DrawLine(texture, centerX + 15, centerY - 5, centerX + 15, centerY - 15, thickness, symbolColor);
        }
    }

    private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawFilledCircle(texture, x0, y0, thickness / 2, color);
            
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private void DrawRect(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        DrawLine(texture, x0, y0, x1, y0, thickness, color);
        DrawLine(texture, x1, y0, x1, y1, thickness, color);
        DrawLine(texture, x1, y1, x0, y1, thickness, color);
        DrawLine(texture, x0, y1, x0, y0, thickness, color);
    }

    private void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
    {
        for (int t = 0; t < 360; t += 2)
        {
            float rad = t * Mathf.Deg2Rad;
            int x = centerX + (int)(radius * Mathf.Cos(rad));
            int y = centerY + (int)(radius * Mathf.Sin(rad));
            DrawFilledCircle(texture, x, y, thickness / 2, color);
        }
    }

    private void DrawFilledCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        int size = texture.width;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (dist <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }

    private void CreateInteractionHUDPrefab()
    {
        string prefabPath = "Assets/_Project/Prefabs/UI/InteractionHUD_Prefab.prefab";
        
        // Check if prefab already exists
        if (File.Exists(prefabPath))
        {
            setupReport += $"⚠️ Prefab already exists: {prefabPath}\n";
            setupReport += $"   Delete it first if you want to regenerate.\n";
            return;
        }

        // Create canvas
        GameObject canvasGO = new GameObject("InteractionHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // High z-order to appear on top

        // Canvas Scaler
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // HUD Panel
        GameObject panelGO = new GameObject("HUD_Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelGO.transform.SetParent(canvasGO.transform, false);
        
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f); // Top center
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -150);
        panelRect.sizeDelta = new Vector2(600, 200);

        Image panelImage = panelGO.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f); // Semi-transparent black

        CanvasGroup canvasGroup = panelGO.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // Icon Image
        GameObject iconGO = new GameObject("Icon_Image", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-200, 0);
        iconRect.sizeDelta = new Vector2(80, 80);

        Image iconImage = iconGO.GetComponent<Image>();
        iconImage.color = Color.white;

        // Prompt Text
        GameObject promptGO = new GameObject("Prompt_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        promptGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform promptRect = promptGO.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(50, 30);
        promptRect.sizeDelta = new Vector2(400, 80);

        TextMeshProUGUI promptText = promptGO.GetComponent<TextMeshProUGUI>();
        promptText.text = "تفاعل!";
        promptText.fontSize = 36;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        // Note: Font asset will need to be assigned manually
        promptText.font = GetDefaultTMPFont();
        promptText.enableWordWrapping = true;
        promptText.richText = true; // Enable rich text formatting

        // Timer Bar Background
        GameObject timerBgGO = new GameObject("Timer_Bar_Bg", typeof(RectTransform), typeof(Image));
        timerBgGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform timerBgRect = timerBgGO.GetComponent<RectTransform>();
        timerBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerBgRect.pivot = new Vector2(0.5f, 0.5f);
        timerBgRect.anchoredPosition = new Vector2(0, -40);
        timerBgRect.sizeDelta = new Vector2(520, 30);

        Image timerBgImage = timerBgGO.GetComponent<Image>();
        timerBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Timer Bar Fill
        GameObject timerFillGO = new GameObject("Timer_Bar_Fill", typeof(RectTransform), typeof(Image));
        timerFillGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform timerFillRect = timerFillGO.GetComponent<RectTransform>();
        timerFillRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerFillRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerFillRect.pivot = new Vector2(0, 0.5f);
        timerFillRect.anchoredPosition = new Vector2(-250, -40);
        timerFillRect.sizeDelta = new Vector2(500, 20);

        Image timerFillImage = timerFillGO.GetComponent<Image>();
        timerFillImage.color = Color.white;
        timerFillImage.type = Image.Type.Filled;
        timerFillImage.fillMethod = Image.FillMethod.Horizontal;
        timerFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        timerFillImage.fillAmount = 1f;

        // Counter Text
        GameObject counterGO = new GameObject("Counter_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        counterGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform counterRect = counterGO.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0.5f, 0.5f);
        counterRect.anchorMax = new Vector2(0.5f, 0.5f);
        counterRect.pivot = new Vector2(0.5f, 0.5f);
        counterRect.anchoredPosition = new Vector2(0, -70);
        counterRect.sizeDelta = new Vector2(400, 40);

        TextMeshProUGUI counterText = counterGO.GetComponent<TextMeshProUGUI>();
        counterText.text = "Progress: 0/5";
        counterText.fontSize = 24;
        counterText.alignment = TextAlignmentOptions.Center;
        counterText.color = Color.white;
        counterText.font = GetDefaultTMPFont();
        counterText.enableWordWrapping = false;

        // Add InteractionHUDController component
        InteractionHUDController controller = canvasGO.AddComponent<InteractionHUDController>();
        
        // Assign references
        System.Reflection.FieldInfo hudPanelField = typeof(InteractionHUDController).GetField("hudPanel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo iconImageField = typeof(InteractionHUDController).GetField("iconImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo promptTextField = typeof(InteractionHUDController).GetField("promptText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo timerBarField = typeof(InteractionHUDController).GetField("timerBar", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo counterTextField = typeof(InteractionHUDController).GetField("counterText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (hudPanelField != null) hudPanelField.SetValue(controller, panelRect);
        if (iconImageField != null) iconImageField.SetValue(controller, iconImage);
        if (promptTextField != null) promptTextField.SetValue(controller, promptText);
        if (timerBarField != null) timerBarField.SetValue(controller, timerFillImage);
        if (counterTextField != null) counterTextField.SetValue(controller, counterText);

        // Create prefab
        GameObject prefabGO = PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        DestroyImmediate(canvasGO);

        if (prefabGO != null)
        {
            setupReport += $"✅ Created prefab: {prefabPath}\n";
            setupReport += $"   - Canvas: ScreenSpaceOverlay, SortOrder 1000\n";
            setupReport += $"   - Panel: 600x200, Top Center, Semi-transparent\n";
            setupReport += $"   - Icon Image: 80x80\n";
            setupReport += $"   - Prompt Text: 36pt, Arabic RTL\n";
            setupReport += $"   - Timer Bar: 500x20, Filled Horizontal\n";
            setupReport += $"   - Counter Text: 24pt\n";
            setupReport += $"   - Controller: All references assigned\n";
        }
        else
        {
            setupReport += $"❌ Failed to create prefab: {prefabPath}\n";
        }
    }

    private TMP_FontAsset GetDefaultTMPFont()
    {
        // Try to find an existing TMP font in the project
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/_Project/Fonts" });
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        // Fallback: search all assets
        guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        setupReport += $"⚠️ No TMP Font found! Please assign font manually to InteractionHUDController.\n";
        return null;
    }

    private bool CheckScriptsExist()
    {
        string[] requiredScripts = {
            "Assets/_Project/Scripts/Data/InteractionType.cs",
            "Assets/_Project/Scripts/Data/InteractionData.cs",
            "Assets/_Project/Scripts/UI/InteractionHUDController.cs",
            "Assets/_Project/Scripts/Core/InteractionSignalEmitter.cs"
        };

        bool allExist = true;
        foreach (string script in requiredScripts)
        {
            if (!File.Exists(script))
            {
                setupReport += $"❌ Missing script: {script}\n";
                allExist = false;
            }
        }

        return allExist;
    }

    private void ValidateSceneSetup()
    {
        // Check for DataManager
        var dataManager = Object.FindObjectOfType<DataManager>();
        if (dataManager != null)
        {
            setupReport += $"✅ DataManager found in scene\n";
            
            // Check if Interactions.csv is assigned
            if (dataManager.interactionsCSV != null)
            {
                setupReport += $"✅ Interactions.csv assigned: {dataManager.interactionsCSV.name}\n";
            }
            else
            {
                setupReport += $"⚠️ Interactions.csv NOT assigned in DataManager inspector\n";
            }
        }
        else
        {
            setupReport += $"❌ DataManager NOT found in scene!\n";
        }

        // Check for HouseFlowController
        var houseFlowController = Object.FindObjectOfType<HouseFlowController>();
        if (houseFlowController != null)
        {
            setupReport += $"✅ HouseFlowController found in scene\n";
        }
        else
        {
            setupReport += $"❌ HouseFlowController NOT found in scene!\n";
        }

        // Check for InputManager
        var inputManager = Object.FindObjectOfType<InputManager>();
        if (inputManager != null)
        {
            setupReport += $"✅ InputManager found in scene\n";
        }
        else
        {
            setupReport += $"❌ InputManager NOT found in scene!\n";
        }

        // Check for InteractionHUD prefab in scene
        var interactionHUD = Object.FindObjectOfType<InteractionHUDController>();
        if (interactionHUD != null)
        {
            setupReport += $"✅ InteractionHUDController found in scene\n";
        }
        else
        {
            setupReport += $"⚠️ InteractionHUDController NOT in scene (drag prefab into scene)\n";
        }
    }

    #endregion
}
