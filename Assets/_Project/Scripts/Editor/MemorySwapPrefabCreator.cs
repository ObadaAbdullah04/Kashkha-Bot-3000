using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// PHASE 17: Quick setup helper for Memory Swap mini-game prefab.
/// Run this via: Tools → Kashkha → Memory Swap → Create Prefab
/// </summary>
public class MemorySwapPrefabCreator : EditorWindow
{
    private static MemorySwapPrefabCreator window;

    [MenuItem("Tools/Kashkha/Memory Swap/Create Prefab Helper")]
    public static void ShowWindow()
    {
        window = GetWindow<MemorySwapPrefabCreator>("Memory Swap Setup");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Memory Swap Mini-Game", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "This tool helps you set up the MemorySwap_Canvas prefab.\n\n" +
            "Follow these steps:\n" +
            "1. Create a new Canvas (UI → Canvas)\n" +
            "2. Add Grid, ScoreText, HintButton\n" +
            "3. Create Tile_Button prefab with Front/Back children\n" +
            "4. Add MemorySwapMiniGame script\n" +
            "5. Assign all references in Inspector\n" +
            "6. Save as prefab in Prefabs/MiniGames/\n\n" +
            "See: MemorySwap_PrefabSetup.md for detailed guide",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Open Setup Guide (Markdown)", GUILayout.Height(30)))
        {
            string guidePath = "Assets/_Project/Prefabs/MiniGames/MemorySwap_PrefabSetup.md";
            UnityEngine.Object guide = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(guidePath);
            if (guide != null)
            {
                AssetDatabase.OpenAsset(guide);
            }
            else
            {
                EditorUtility.DisplayDialog("Guide Not Found", 
                    "Could not find MemorySwap_PrefabSetup.md at:\n" + guidePath, 
                    "OK");
            }
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Open MiniGameManager", GUILayout.Height(30)))
        {
            var manager = MiniGameManager.Instance;
            if (manager != null)
            {
                Selection.activeGameObject = manager.gameObject;
                EditorGUIUtility.PingObject(manager.gameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("Manager Not Found", 
                    "MiniGameManager not found in scene. Make sure you're in Core_Scene.unity", 
                    "OK");
            }
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Quick Checklist:\n" +
            "☐ Canvas created\n" +
            "☐ Grid Layout Group (3 columns)\n" +
            "☐ Tile prefab with Front/Back children\n" +
            "☐ 6 tile sprites assigned\n" +
            "☐ ScoreText (TMP) assigned\n" +
            "☐ MemorySwapMiniGame script added\n" +
            "☐ Saved as prefab\n" +
            "☐ Assigned in MiniGameManager",
            MessageType.None
        );
    }
}
