using UnityEngine;

[CreateAssetMenu(
    fileName = "New EncounterData",
    menuName = "KashkhaBot/Encounter/Encounter Data")]
public class EncounterData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier for this encounter.\n" +
             "Convention: HOUSE_SHORT_DESCRIPTION\n" +
             "Example: H1_GREETING, H3_INTERROGATION")]
    public string EncounterId;

    [Tooltip("How this encounter is resolved.")]
    public EncounterType Type;

    [Header("Dialogue")]
    [Tooltip("The situation prompt shown to the player. Write in Arabic.")]
    [TextArea(2, 5)]
    public string QuestionTextAR;

    [Tooltip("All selectable responses for this encounter.\n" +
             "Dialogue encounters should have 3 choices.\n" +
             "Exactly one choice should have IsCorrect = true.")]
    public ChoiceData[] Choices;

    [Header("Timing")]
    [Tooltip("Duration of the Panic Timer for this encounter in seconds.\n" +
             "GDD defaults: House 1 = 8s, House 2 = 7s, House 3 = 6s.\n" +
             "Override here for specific high-pressure encounters.")]
    [Min(1f)]
    public float PanicTimerDuration = 8f;

    [Header("Minigame")]
    [Tooltip("Set to anything other than None if this encounter " +
             "should immediately trigger a minigame.\n" +
             "Used for house transition encounters.")]
    public MiniGameType TriggersMiniGame = MiniGameType.None;

    [Header("Upgrade Gating")]
    [Tooltip("Optional. List upgrade IDs that must be owned for this " +
             "encounter to appear in a random pool.\n" +
             "Leave empty for encounters with no unlock requirement.")]
    public string[] RequiredUpgradeIds;

#if UNITY_EDITOR
    // ── Editor Validation ──────────────────────────────────────────
    // A quick in-asset safety check. The full build-blocking validator
    // lives in EncounterDataValidator.cs — this provides immediate
    // feedback while a designer is filling in the SO in the Inspector.

    private void OnValidate()
    {
        ValidateChoices();
    }

    private void ValidateChoices()
    {
        if (Type != EncounterType.Dialogue || Choices == null)
            return;

        int correctCount = 0;

        for (int i = 0; i < Choices.Length; i++)
        {
            // Flag missing feedback text immediately in the console.
            if (string.IsNullOrWhiteSpace(Choices[i].FeedbackText))
            {
                Debug.LogWarning(
                    $"[EncounterData] '{EncounterId}' — " +
                    $"Choice [{i}] ('{Choices[i].TextAR}') " +
                    $"has empty FeedbackText. This is a build error.",
                    this);
            }

            if (Choices[i].IsCorrect)
                correctCount++;
        }

        // Exactly one correct answer enforced.
        if (Choices.Length > 0 && correctCount != 1)
        {
            Debug.LogWarning(
                $"[EncounterData] '{EncounterId}' — " +
                $"Expected exactly 1 correct choice, found {correctCount}.",
                this);
        }
    }
#endif
}