using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to automatically generate all 4 CharacterExpressionSO assets.
/// Run from: Tools → Kashkha → Generate Character Expression SOs
/// 
/// This script:
/// 1. Scans Assets/_Project/Art/CharacterSprites/ for generated sprites
/// 2. Creates 4 CharacterExpressionSO assets with proper assignments
/// 3. Saves to Assets/_Project/Data/CharacterExpressions/
/// 
/// Characters created:
/// - Khala_Um_Mohammed (خالة أم محمد)
/// - Amm_Abu_Mohammed (عمو أبو أحمد)
/// - Grandma (خالة نادية)
/// - House4_Boss (الجنون)
/// </summary>
public class CharacterExpressionSOGenerator : Editor
{
    private const string SPRITES_FOLDER = "Assets/_Project/Art/CharacterSprites";
    private const string OUTPUT_FOLDER = "Assets/_Project/Data/CharacterExpressions";

    private struct CharacterConfig
    {
        public string FileName;
        public string CharacterName;
        public string SpritePrefix;
    }

    private static readonly CharacterConfig[] Characters = new CharacterConfig[]
    {
        new CharacterConfig
        {
            FileName = "Khala_Um_Mohammed_Expressions",
            CharacterName = "خالة أم محمد",
            SpritePrefix = "Khala_Um_Mohammed"
        },
        new CharacterConfig
        {
            FileName = "Amm_Abu_Mohammed_Expressions",
            CharacterName = "عمو أبو أحمد",
            SpritePrefix = "Amm_Abu_Mohammed"
        },
        new CharacterConfig
        {
            FileName = "Grandma_Expressions",
            CharacterName = "خالة نادية",
            SpritePrefix = "Grandma"
        },
        new CharacterConfig
        {
            FileName = "House4_Boss_Expressions",
            CharacterName = "الجنون",
            SpritePrefix = "House4_Boss"
        }
    };

    private static readonly string[] ExpressionNames = new string[]
    {
        "Neutral",
        "Happy",
        "Angry",
        "Surprised",
        "Hospitality"
    };

    [MenuItem("Tools/Kashkha/Generate Character Expression SOs")]
    public static void GenerateAllCharacterExpressions()
    {
        Debug.Log("=== Character Expression SO Generator ===");

        // Verify sprites folder exists
        if (!Directory.Exists(SPRITES_FOLDER))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Character Sprites folder not found!\n\nPlease run 'Generate Placeholder Character Sprites' first.",
                "OK"
            );
            Debug.LogError($"[CharacterExpressionSO] Sprites folder not found: {SPRITES_FOLDER}");
            return;
        }

        // Create output folder if needed
        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            AssetDatabase.Refresh();
            Debug.Log($"Created folder: {OUTPUT_FOLDER}");
        }

        // Refresh asset database to find all sprites
        AssetDatabase.Refresh();

        int generated = 0;

        // Generate each character's SO
        foreach (var config in Characters)
        {
            Debug.Log($"Generating: {config.CharacterName} ({config.SpritePrefix})");

            var so = GenerateCharacterExpressionSO(config);
            
            if (so != null)
            {
                string assetPath = $"{OUTPUT_FOLDER}/{config.FileName}.asset";
                
                // Delete existing if present
                if (File.Exists(assetPath))
                {
                    File.Delete(assetPath);
                    File.Delete(assetPath + ".meta");
                }

                AssetDatabase.CreateAsset(so, assetPath);
                Debug.Log($"  ✓ Created: {config.FileName}.asset");
                generated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"=== Generated {generated}/4 CharacterExpressionSO assets! ===");
        Debug.Log($"Saved to: {OUTPUT_FOLDER}/");

        EditorUtility.DisplayDialog(
            "Success!",
            $"Generated {generated} CharacterExpressionSO assets.\n\nLocation: {OUTPUT_FOLDER}/\n\nNext step: Assign these to CutsceneTrigger's 'Character Expressions' array.",
            "OK"
        );
    }

    private static CharacterExpressionSO GenerateCharacterExpressionSO(CharacterConfig config)
    {
        var so = ScriptableObject.CreateInstance<CharacterExpressionSO>();
        so.name = config.FileName;
        so.characterName = config.CharacterName;

        // Load default sprite (Neutral)
        string defaultSpritePath = $"{SPRITES_FOLDER}/{config.SpritePrefix}_Neutral.png";
        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(defaultSpritePath);
        
        if (defaultSprite != null)
        {
            so.defaultSprite = defaultSprite;
            Debug.Log($"  ✓ Default sprite: {config.SpritePrefix}_Neutral");
        }
        else
        {
            Debug.LogWarning($"  ⚠ Default sprite not found: {defaultSpritePath}");
        }

        // Load all expressions
        so.expressions = new System.Collections.Generic.List<Expression>();

        foreach (var exprName in ExpressionNames)
        {
            string spritePath = $"{SPRITES_FOLDER}/{config.SpritePrefix}_{exprName}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            var expression = new Expression
            {
                name = exprName,
                sprite = sprite
            };

            so.expressions.Add(expression);

            if (sprite != null)
            {
                Debug.Log($"  ✓ Expression: {exprName} → {config.SpritePrefix}_{exprName}");
            }
            else
            {
                Debug.LogWarning($"  ⚠ Sprite not found for expression: {exprName}");
            }
        }

        return so;
    }
}
