using UnityEngine;

/// <summary>
/// Subscribes to GameEvents (via GameEventListener components on the same GameObject)
/// to apply deltas to our runtime SO Variables and monitor for Win/Loss states.
/// </summary>
public class MeterSystem : MonoBehaviour
{
    [Header("Meter Variables (SO References)")]
    [SerializeField] private FloatVariable _socialBattery;
    [SerializeField] private FloatVariable _stomachMeter;
    [SerializeField] private IntVariable _eidia;
    [SerializeField] private IntVariable _scrap;

    [Header("Data Payload")]
    [Tooltip("Reads the latest choice from here when a ChoiceMadeEvent fires.")]
    [SerializeField] private ChoicePayloadSO _currentChoicePayload;

    [Header("Game State Events to Raise")]
    [SerializeField] private GameEvent _onSocialShutdown;   // Battery <= 0
    [SerializeField] private GameEvent _onMaamoulExplosion; // Stomach >= 1
    [SerializeField] private GameEvent _onWinConditionMet;  // Eidia >= 100

    private const int WIN_EIDIA_TARGET = 100;

    [SerializeField] private GameEvent _onMetersUpdated;

    /// <summary>
    /// Call this via a GameEventListener listening to a "RunStartedEvent".
    /// </summary>
    public void ResetAllMeters()
    {
        _socialBattery.ResetToInitial();
        _stomachMeter.ResetToInitial();
        _eidia.ResetToInitial();
        
        // CRITICAL: We DO NOT reset Scrap. Scrap is our persistent 
        // meta-currency handled by the SaveManager across runs.
    }

    /// <summary>
    /// Call this via a GameEventListener listening to a "ChoiceMadeEvent".
    /// </summary>
    public void ProcessActiveChoice()
    {
        if (_currentChoicePayload == null || _currentChoicePayload.ActiveChoice == null)
        {
            Debug.LogError("[MeterSystem] ChoiceMadeEvent received, but no ActiveChoice payload found!");
            return;
        }

        ChoiceData choice = _currentChoicePayload.ActiveChoice;

        // Apply Deltas. 
        // Note: The clamping logic (0-1 for floats, min 0 for ints) 
        // is already safely handled inside the Variable SOs themselves!
        _socialBattery.ApplyDelta(choice.BatteryDelta);
        _stomachMeter.ApplyDelta(choice.StomachDelta);
        _eidia.ApplyDelta(choice.EidiaReward);
        _scrap.ApplyDelta(choice.ScrapReward);

        _onMetersUpdated?.Raise();

        EvaluateRunConditions();
    }

    /// <summary>
    /// Evaluates if the new meter states should trigger a win or loss.
    /// </summary>
    private void EvaluateRunConditions()
    {
        // 1. Win Condition takes precedence
        if (_eidia.Value >= WIN_EIDIA_TARGET)
        {
            _onWinConditionMet?.Raise();
            return; // Stop checking, run is over
        }

        // 2. Loss Condition: Social Battery Depleted
        if (_socialBattery.Value <= 0f)
        {
            _onSocialShutdown?.Raise();
            return;
        }

        // 3. Loss Condition: Ma'amoul Overload
        if (_stomachMeter.Value >= 1f)
        {
            _onMaamoulExplosion?.Raise();
            return;
        }
    }
}