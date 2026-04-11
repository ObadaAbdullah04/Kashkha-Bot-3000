using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor script to regenerate all House Sequence ScriptableObjects with valid data.
/// Run this from: Tools → Kashkha → Regenerate House Sequences
/// 
/// This script:
/// 1. Removes all invalid QTE (Type:1) elements
/// 2. Fixes Cutscene IDs to match CSV (CS_* prefix)
/// 3. Ensures all Question IDs exist in Questions.csv
/// 4. Creates proper sequences with only Question (Type:0) and Cutscene (Type:2)
/// </summary>
public class HouseSequenceRegenerator : Editor
{
    private const string SEQUENCES_FOLDER = "Assets/_Project/Resources/Sequences";

    [MenuItem("Tools/Kashkha/Regenerate House Sequences")]
    public static void RegenerateAllSequences()
    {
        Debug.Log("=== House Sequence Regenerator ===");

        // Ensure folder exists
        if (!Directory.Exists(SEQUENCES_FOLDER))
        {
            Directory.CreateDirectory(SEQUENCES_FOLDER);
            AssetDatabase.Refresh();
            Debug.Log($"Created folder: {SEQUENCES_FOLDER}");
        }

        // Generate each house sequence
        RegenerateHouseSequence(1, CreateHouse1Sequence());
        RegenerateHouseSequence(2, CreateHouse2Sequence());
        RegenerateHouseSequence(3, CreateHouse3Sequence());
        RegenerateHouseSequence(4, CreateHouse4Sequence());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== All House Sequences Regenerated Successfully! ===");
        Debug.Log("IMPORTANT: Open each sequence in Unity Editor and verify the elements are correct.");
    }

    private static void RegenerateHouseSequence(int houseLevel, HouseSequenceData sequence)
    {
        string path = $"{SEQUENCES_FOLDER}/House{houseLevel}_Sequence.asset";
        
        // Delete existing asset if it exists
        if (File.Exists(path))
        {
            File.Delete(path);
            File.Delete(path + ".meta");
        }

        // Create new asset
        AssetDatabase.CreateAsset(sequence, path);
        
        Debug.Log($"House {houseLevel} Sequence: {sequence.Sequence.Count} elements created");
        foreach (var element in sequence.Sequence)
        {
            Debug.Log($"  [{element.Type}] {element.ElementID}");
        }
    }

    #region House Sequence Definitions

    private static HouseSequenceData CreateHouse1Sequence()
    {
        var sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
        sequence.name = "House1_Sequence";
        sequence.HouseLevel = 1;

        // House 1: Gentle introduction with خالة أم محمد
        // Mix of questions and welcoming cutscenes
        sequence.Sequence = new System.Collections.Generic.List<SequenceElement>
        {
            new SequenceElement(ElementType.Question, "Q1", "تفضلي معمول مع الشاي! - Hospitality test"),
            new SequenceElement(ElementType.Cutscene, "CS_H1_Welcome", "Welcome greeting with Happy expression"),
            new SequenceElement(ElementType.Question, "Q4", "بدك قهوة؟ - Coffee offer"),
            new SequenceElement(ElementType.Cutscene, "CS_H1_FinishCoffee", "Finished coffee moment"),
            new SequenceElement(ElementType.Question, "Q7", "تفضلي شاي؟ - Tea offer"),
            new SequenceElement(ElementType.Question, "Q5", "كيف حالك؟ - How are you?"),
            new SequenceElement(ElementType.Cutscene, "CS_H1_Aunt_Smile", "Aunt smiles - Happy expression"),
            new SequenceElement(ElementType.Question, "Q8", "وينك يا حبيبي؟ - Grandma hug"),
            new SequenceElement(ElementType.Cutscene, "CS_Celebration", "House 1 complete celebration"),
        };

        return sequence;
    }

    private static HouseSequenceData CreateHouse2Sequence()
    {
        var sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
        sequence.name = "House2_Sequence";
        sequence.HouseLevel = 2;

        // House 2: عمو أبو أحمد - More complex social situations
        sequence.Sequence = new System.Collections.Generic.List<SequenceElement>
        {
            new SequenceElement(ElementType.Question, "Q11", "هز الفنجان إذا اكتفيت - Coffee cup signal"),
            new SequenceElement(ElementType.Cutscene, "CS_H2_Greeting", "Uncle greeting - Neutral expression"),
            new SequenceElement(ElementType.Question, "Q14", "بدك تتزوج قريب؟ - Marriage question"),
            new SequenceElement(ElementType.Question, "Q12", "القهوة مرة؟ - Coffee bitter?"),
            new SequenceElement(ElementType.Cutscene, "CS_H2_Coffee_Pour", "Coffee pouring - Hospitality expression"),
            new SequenceElement(ElementType.Question, "Q13", "تسلم يا بطل - Thank you response"),
            new SequenceElement(ElementType.Question, "Q15", "أبوك شغال وين？ - Father work question"),
            new SequenceElement(ElementType.Cutscene, "CS_H2_Uncle_Nod", "Uncle nods approval"),
        };

        return sequence;
    }

    private static HouseSequenceData CreateHouse3Sequence()
    {
        var sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
        sequence.name = "House3_Sequence";
        sequence.HouseLevel = 3;

        // House 3: خالة نادية - Family values and respect
        sequence.Sequence = new System.Collections.Generic.List<SequenceElement>
        {
            new SequenceElement(ElementType.Question, "Q21", "بدك شاي ولا قهوة؟ - Tea or coffee choice"),
            new SequenceElement(ElementType.Question, "Q23", "بتعرف تصلي؟ - Do you know prayer?"),
            new SequenceElement(ElementType.Cutscene, "CS_H3_Blessing", "Grandma blessing - Happy expression"),
            new SequenceElement(ElementType.Question, "Q25", "اللمة حلوة - Family gathering sweet"),
            new SequenceElement(ElementType.Question, "Q27", "كل أكلك يا بطل - Eat your food"),
            new SequenceElement(ElementType.Cutscene, "CS_H3_Family_Gather", "Family gathered - Happy expression"),
            new SequenceElement(ElementType.Question, "Q29", "يا حبيبي وينك؟ - Grandma affection"),
            new SequenceElement(ElementType.Question, "Q22", "شو رأيك بالضرب؟ - Opinion on hitting"),
            new SequenceElement(ElementType.Cutscene, "CS_H3_Respect_Elder", "Elder respect moment"),
        };

        return sequence;
    }

    private static HouseSequenceData CreateHouse4Sequence()
    {
        var sequence = ScriptableObject.CreateInstance<HouseSequenceData>();
        sequence.name = "House4_Sequence";
        sequence.HouseLevel = 4;

        // House 4: الجنون - INSANE MODE, fast-paced challenge
        sequence.Sequence = new System.Collections.Generic.List<SequenceElement>
        {
            new SequenceElement(ElementType.Cutscene, "CS_H4_Boss_Intro", "Boss introduction - Angry expression"),
            new SequenceElement(ElementType.Question, "Q31", "هز هز هز！ - Shake shake shake!"),
            new SequenceElement(ElementType.Question, "Q33", "بدك قهوة？ - Coffee in insane mode"),
            new SequenceElement(ElementType.Question, "Q35", "كيف حالك？ - How are you under pressure"),
            new SequenceElement(ElementType.Cutscene, "CS_H4_Boss_Win", "Boss impressed - Surprised expression"),
            new SequenceElement(ElementType.Question, "Q37", "بتحب البلد？ - Love for country"),
            new SequenceElement(ElementType.Question, "Q32", "هز أقوى！ - Shake stronger!"),
            new SequenceElement(ElementType.Question, "Q34", "العيد إيد？ - Eid money question"),
            new SequenceElement(ElementType.Cutscene, "CS_H4_Boss_Fail", "Boss angry - Angry expression"),
            new SequenceElement(ElementType.Question, "Q39", "ربنا يحميك - God protect you"),
        };

        return sequence;
    }

    #endregion
}
