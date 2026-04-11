using System;
using UnityEngine;

/// <summary>
/// Data model for a single interaction definition from Interactions.csv
///
/// Interactions are standalone gameplay moments that require player input
/// (shake, hold, tap, draw). They are triggered by HouseFlowController
/// during house sequences, independent from questions or cutscenes.
///
/// CSV FORMAT (12 columns):
/// ID, HouseLevel, InteractionType, PromptTextAR, Duration, Threshold, CorrectBat, IncorrectBat, CorrectStomach, IncorrectStomach, CorrectEid, IncorrectEid
///
/// USAGE:
/// - Parsed by DataManager at startup
/// - Placed in HouseSequenceData alongside Questions and Cutscenes
/// - Triggered by HouseFlowController or Timeline signals
/// - InteractionHUDController manages the UI/input lifecycle
/// </summary>
[Serializable]
public class InteractionData
{
    #region Identity

    public string ID;                     // Unique interaction ID (e.g., "SHAKE_Cup_1")
    public int HouseLevel;                // Which house this interaction belongs to (1-4)

    #endregion

    #region Configuration

    public InteractionType InteractionType;  // Type of player input required
    public string PromptTextAR;              // Arabic instruction shown to player (e.g., "هز الكوب!")
    public float Duration;                   // Time limit in seconds (0 = no timer, unlimited)
    public float Threshold;                  // Required input count/value (shake count, hold seconds, tap count)
                                             // For Draw type: unused (path completion determines success)

    #endregion

    #region Rewards/Penalties

    public float CorrectBatteryDelta;     // Battery change on success (negative = drain, positive = gain)
    public float IncorrectBatteryDelta;   // Battery change on failure
    
    public float CorrectStomachDelta;     // Stomach change on success (usually positive for hospitality)
    public float IncorrectStomachDelta;   // Stomach change on failure

    public int CorrectEid;                // Eidia reward on success
    public int IncorrectEid;              // Eidia reward on failure

    #endregion

    #region Helper Methods

    /// <summary>
    /// Returns battery delta based on whether the interaction succeeded.
    /// </summary>
    public float GetBatteryDelta(bool succeeded)
    {
        return succeeded ? CorrectBatteryDelta : IncorrectBatteryDelta;
    }

    /// <summary>
    /// Returns stomach delta based on whether the interaction succeeded.
    /// </summary>
    public float GetStomachDelta(bool succeeded)
    {
        return succeeded ? CorrectStomachDelta : IncorrectStomachDelta;
    }

    /// <summary>
    /// Returns Eidia reward based on whether the interaction succeeded.
    /// </summary>
    public int GetEidReward(bool succeeded)
    {
        return succeeded ? CorrectEid : IncorrectEid;
    }

    /// <summary>
    /// Returns whether the interaction result meets the threshold.
    /// </summary>
    public bool CheckThreshold(float currentValue)
    {
        // For Draw type, threshold is unused - success is path completion
        if (InteractionType == InteractionType.Draw)
            return currentValue > 0;

        return currentValue >= Threshold;
    }

    #endregion

    public override string ToString()
    {
        return $"[Interaction {ID}] Type:{InteractionType} | Prompt:\"{PromptTextAR}\" | Duration:{Duration}s | Threshold:{Threshold} | Bat:{CorrectBatteryDelta}/{IncorrectBatteryDelta} | Stomach:{CorrectStomachDelta}/{IncorrectStomachDelta} | Eid:{CorrectEid}/{IncorrectEid}";
    }
}
