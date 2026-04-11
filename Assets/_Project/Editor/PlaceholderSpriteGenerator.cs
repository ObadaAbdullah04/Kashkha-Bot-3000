using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to generate placeholder character sprites.
/// Run from: Tools → Kashkha → Generate Placeholder Character Sprites
/// 
/// Creates 256x256 colored circle sprites for each character expression.
/// Saves to: Assets/_Project/Art/CharacterSprites/
/// 
/// Characters (4):
/// - Khala_Um_Mohammed (خالة أم محمد)
/// - Amm_Abu_Mohammed (عمو أبو أحمد)
/// - Grandma (خالة نادية / الجدة)
/// - House4_Boss (الجنون)
/// 
/// Expressions (5 per character):
/// - Neutral, Happy, Angry, Surprised, Hospitality
/// 
/// Total: 20 sprites
/// </summary>
public class PlaceholderSpriteGenerator : Editor
{
    private const int SPRITE_SIZE = 256;
    private const string OUTPUT_FOLDER = "Assets/_Project/Art/CharacterSprites";

    private enum ExpressionType
    {
        Neutral,
        Happy,
        Angry,
        Surprised,
        Hospitality
    }

    private struct CharacterDef
    {
        public string Name;
        public Color BaseColor;
    }

    private static readonly CharacterDef[] Characters = new CharacterDef[]
    {
        new CharacterDef { Name = "Khala_Um_Mohammed", BaseColor = new Color(1.0f, 0.7f, 0.8f) },  // Pink
        new CharacterDef { Name = "Amm_Abu_Mohammed", BaseColor = new Color(0.6f, 0.8f, 1.0f) },    // Blue
        new CharacterDef { Name = "Grandma", BaseColor = new Color(1.0f, 0.9f, 0.6f) },             // Yellow
        new CharacterDef { Name = "House4_Boss", BaseColor = new Color(0.8f, 0.4f, 0.4f) }          // Red
    };

    private static readonly Color[] ExpressionColors = new Color[]
    {
        new Color(0.75f, 0.75f, 0.75f),  // Neutral - Gray
        new Color(0.3f, 0.9f, 0.3f),     // Happy - Green
        new Color(0.9f, 0.3f, 0.3f),     // Angry - Red
        new Color(0.9f, 0.9f, 0.3f),     // Surprised - Yellow
        new Color(0.3f, 0.7f, 0.9f)      // Hospitality - Light Blue
    };

    [MenuItem("Tools/Kashkha/Generate Placeholder Character Sprites")]
    public static void GenerateAllSprites()
    {
        Debug.Log("=== Placeholder Sprite Generator ===");

        // Create output folder
        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            AssetDatabase.Refresh();
            Debug.Log($"Created folder: {OUTPUT_FOLDER}");
        }

        int totalGenerated = 0;

        // Generate sprites for each character
        foreach (var character in Characters)
        {
            Debug.Log($"Generating sprites for: {character.Name}");

            for (int i = 0; i < System.Enum.GetNames(typeof(ExpressionType)).Length; i++)
            {
                string expressionName = System.Enum.GetNames(typeof(ExpressionType))[i];
                Color expressionColor = ExpressionColors[i];

                // Create sprite
                Texture2D texture = CreateCircleTexture(character.BaseColor, expressionColor);
                
                // Save to file
                string fileName = $"{character.Name}_{expressionName}.png";
                string filePath = Path.Combine(OUTPUT_FOLDER, fileName);
                
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
                
                Debug.Log($"  ✓ {fileName}");
                totalGenerated++;

                // Clean up
                Object.DestroyImmediate(texture);
            }
        }

        // Also create a simple default sprite
        Texture2D defaultSprite = CreateCircleTexture(Color.white, Color.gray);
        string defaultPath = Path.Combine(OUTPUT_FOLDER, "Default.png");
        File.WriteAllBytes(defaultPath, defaultSprite.EncodeToPNG());
        Debug.Log("  ✓ Default.png");
        totalGenerated++;
        Object.DestroyImmediate(defaultSprite);

        // Refresh Unity assets
        AssetDatabase.Refresh();

        Debug.Log($"=== Generated {totalGenerated} placeholder sprites! ===");
        Debug.Log($"Saved to: {OUTPUT_FOLDER}/");
        Debug.Log("You can now assign these to CharacterExpressionSO assets.");
    }

    private static Texture2D CreateCircleTexture(Color bgColor, Color circleColor)
    {
        Texture2D texture = new Texture2D(SPRITE_SIZE, SPRITE_SIZE, TextureFormat.RGBA32, false);
        
        int centerX = SPRITE_SIZE / 2;
        int centerY = SPRITE_SIZE / 2;
        int radius = SPRITE_SIZE / 2 - 10; // 10px margin

        for (int y = 0; y < SPRITE_SIZE; y++)
        {
            for (int x = 0; x < SPRITE_SIZE; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance <= radius)
                {
                    // Inside circle - draw expression color with gradient
                    float gradient = 1.0f - (distance / radius);
                    texture.SetPixel(x, y, circleColor * gradient);
                }
                else
                {
                    // Outside circle - background color
                    texture.SetPixel(x, y, bgColor);
                }
            }
        }

        texture.Apply();
        return texture;
    }
}
