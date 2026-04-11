using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PHASE 9: ScriptableObject defining the ordered sequence of elements for a house.
///
/// This defines the exact order of Questions and Cutscenes that will play
/// during a house visit. The sequence drives the pacing, this data defines the content.
///
/// USAGE:
/// 1. Right-click in Project window → Create → Game → House Sequence
/// 2. Name it (e.g., "House1_Sequence")
/// 3. Set HouseLevel (1-4)
/// 4. Add elements in order (Question IDs, Cutscene IDs)
/// 5. Assign to HouseFlowController.SequenceData
/// </summary>
[CreateAssetMenu(menuName = "Game/House Sequence", fileName = "HouseSequence_New")]
public class HouseSequenceData : ScriptableObject
{
    [Header("House Configuration")]
    [Tooltip("Which house this sequence is for (1-4)")]
    [Range(1, 4)]
    public int HouseLevel = 1;

    [Header("Element Sequence")]
    [Tooltip("Ordered list of elements to trigger during this house")]
    public List<SequenceElement> Sequence = new List<SequenceElement>();

    /// <summary>
    /// Validates the sequence, checking all element IDs exist in CSV pools.
    /// Returns true if valid, false otherwise.
    /// </summary>
    public bool ValidateSequence(out List<string> errors)
    {
        errors = new List<string>();

        if (HouseLevel < 1 || HouseLevel > 4)
        {
            errors.Add($"Invalid HouseLevel: {HouseLevel} (must be 1-4)");
        }

        if (Sequence == null || Sequence.Count == 0)
        {
            errors.Add("Sequence is empty! Add at least one element.");
            return false;
        }

        // Check for null elements
        for (int i = 0; i < Sequence.Count; i++)
        {
            if (Sequence[i] == null)
            {
                errors.Add($"Element at index {i} is null!");
            }
        }

        // Check for empty IDs
        for (int i = 0; i < Sequence.Count; i++)
        {
            if (Sequence[i] != null && string.IsNullOrWhiteSpace(Sequence[i].ElementID))
            {
                errors.Add($"Element at index {i} has empty ElementID!");
            }
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Returns a summary of the sequence for debugging.
    /// </summary>
    public string GetSequenceSummary()
    {
        if (Sequence == null || Sequence.Count == 0)
            return "Empty sequence";

        int questionCount = 0, cutsceneCount = 0, interactionCount = 0;
        foreach (var element in Sequence)
        {
            if (element != null)
            {
                switch (element.Type)
                {
                    case ElementType.Question: questionCount++; break;
                    case ElementType.Cutscene: cutsceneCount++; break;
                    case ElementType.Interaction: interactionCount++; break;
                }
            }
        }

        return $"Total: {Sequence.Count} elements | {questionCount} Questions | {cutsceneCount} Cutscenes | {interactionCount} Interactions";
    }
}

/// <summary>
/// A single element in the house sequence.
/// </summary>
[Serializable]
public class SequenceElement
{
    [Tooltip("Type of element to trigger")]
    public ElementType Type;

    [Tooltip("ID of the element (must match ID in CSV)")]
    public string ElementID;

    [Tooltip("Optional note for designers (not used at runtime)")]
    public string DesignerNote;

    public SequenceElement()
    {
        Type = ElementType.Question;
        ElementID = "";
        DesignerNote = "";
    }

    public SequenceElement(ElementType type, string elementID, string note = "")
    {
        Type = type;
        ElementID = elementID;
        DesignerNote = note;
    }

    public override string ToString()
    {
        return $"[{Type}] {ElementID}" + (string.IsNullOrWhiteSpace(DesignerNote) ? "" : $" ({DesignerNote})");
    }
}

/// <summary>
/// Types of elements that can appear in a house sequence.
/// PHASE 13: Added Interaction type for standalone gameplay moments (shake, hold, tap, draw).
/// </summary>
public enum ElementType
{
    Question,       // Triggers SwipeEncounterManager.ShowSingleCard()
    Cutscene,       // Triggers CutsceneTrigger.PlayCutscene()
    Interaction     // Triggers InteractionHUDController.RunInteraction()
}
