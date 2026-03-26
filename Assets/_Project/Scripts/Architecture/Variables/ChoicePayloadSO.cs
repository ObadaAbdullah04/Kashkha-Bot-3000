using UnityEngine;

[CreateAssetMenu(fileName = "New ChoicePayload", menuName = "KashkhaBot/Variables/Choice Payload")]
public class ChoicePayloadSO : ScriptableObject
{
    [Tooltip("The EncounterManager sets this right before raising the ChoiceMadeEvent.")]
    public ChoiceData ActiveChoice;
}