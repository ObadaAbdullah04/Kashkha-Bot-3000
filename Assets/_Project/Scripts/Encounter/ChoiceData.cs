using UnityEngine;

[System.Serializable]
public class ChoiceData
{
    [Header("Display")]
    [Tooltip("The choice text shown to the player. Write in Arabic.")]
    public string TextAR;

    [Header("Outcome — Meters")]
    [Tooltip("Applied to Social Battery on selection.\n" +
             "Negative = drain (wrong/awkward answer).\n" +
             "Positive = recharge (perfect cultural answer).")]
    [Range(-1f, 1f)]
    public float BatteryDelta;

    [Tooltip("Applied to Stomach Meter on selection.\n" +
             "Positive = fills meter (forced eating).\n" +
             "Negative = slight relief (successful deflection).")]
    [Range(-1f, 1f)]
    public float StomachDelta;

    [Header("Outcome — Currency")]
    [Tooltip("Eidia (JOD) awarded on selection. 0 for wrong answers.")]
    [Min(0)]
    public int EidiaReward;

    [Tooltip("Tech Scrap awarded on selection.")]
    [Min(0)]
    public int ScrapReward;

    [Header("Correctness")]
    [Tooltip("Marks this as the culturally correct response.\n" +
             "Drives the Cultural Proficiency Score calculation.")]
    public bool IsCorrect;

    [Header("Cultural Education — MANDATORY")]
    [Tooltip("THE teaching moment. Displayed on the Feedback Card " +
             "after this choice is selected, correct or not.\n\n" +
             "Wrong answers: explain why the correct answer is correct.\n" +
             "Correct answers: reinforce the cultural context with warmth.\n\n" +
             "This field must never be empty. The editor validator " +
             "will treat an empty feedbackText as a build error.")]
    [TextArea(2, 4)]
    public string FeedbackText;
}