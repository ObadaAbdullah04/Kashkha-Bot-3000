using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// PHASE 13: Unity Timeline Signal Emitter for triggering interactions.
/// 
/// PURPOSE:
/// This component is added to Unity Timeline tracks as a Signal Emitter.
/// When the Timeline playback head reaches this emitter's position,
/// it fires an event that HouseFlowController listens to, triggering
/// the specified interaction.
/// 
/// USAGE IN UNITY EDITOR:
/// 1. Open your Timeline window (Window > Sequencing > Timeline)
/// 2. Select a Signal Track (or create one: Right-click track area > Signal Track)
/// 3. Right-click on the track at desired time > Add Signal Emitter
/// 4. Add this component: InteractionSignalEmitter
/// 5. Set the InteractionID field to match an ID from Interactions.csv
/// 6. HouseFlowController will pause the Timeline, run the interaction, then resume
/// 
/// RUNTIME FLOW:
/// Timeline plays → Signal Emitter fires → HouseFlowController.TriggerInteraction()
/// → Timeline pauses → InteractionHUDController shows HUD → Player interacts
/// → Success/Failure → HouseFlowController resumes Timeline
/// </summary>
public class InteractionSignalEmitter : SignalEmitter
{
    [Header("Interaction Configuration")]
    [Tooltip("ID of the interaction to trigger (must match Interactions.csv)")]
    public string InteractionID;

    [Tooltip("Pause timeline while interaction is active")]
    public bool PauseTimeline = true;

    /// <summary>
    /// Fires when this emitter is triggered by Timeline.
    /// HouseFlowController subscribes to this event.
    /// </summary>
    public static System.Action<string> OnInteractionTriggered;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validate InteractionID format in editor
        if (string.IsNullOrEmpty(InteractionID))
        {
            Debug.LogWarning("[InteractionSignalEmitter] InteractionID is empty! " +
                "Set this to an ID from Interactions.csv (e.g., 'SHAKE_Cup_1')");
        }
    }
#endif

    /// <summary>
    /// Called by Unity Timeline when playback reaches this emitter.
    /// Triggers the interaction via HouseFlowController.
    /// Note: SignalEmitter does not use an 'Emit' override; it is a data container.
    /// The SignalReceiver on the GameObject handles the actual trigger.
    /// </summary>
    public void TriggerSignal(PlayableDirector director)
    {
        if (string.IsNullOrEmpty(InteractionID))
        {
            Debug.LogWarning("[InteractionSignalEmitter] No InteractionID set! Skipping.");
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[InteractionSignalEmitter] Triggering interaction: {InteractionID}");
#endif

        // Fire event for HouseFlowController to handle
        OnInteractionTriggered?.Invoke(InteractionID);

        // Optional: Pause timeline (HouseFlowController will resume it)
        if (PauseTimeline && director != null)
        {
            director.Pause();
        }
    }
}
