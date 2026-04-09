using System;

/// <summary>
/// PHASE 9: Data model for a single QTE definition from QTEs.csv
/// 
/// QTEs are triggered by Timeline signals during house sequences.
/// Each QTE has a type (Shake, Swipe, Hold), duration, and success/fail effects.
/// 
/// CSV FORMAT (8 columns):
/// ID, HouseLevel, QTEType, Duration, SuccessTextAR, FailTextAR, SuccessBatteryEffect, FailBatteryEffect
/// </summary>
[Serializable]
public class QTEData
{
    #region Identity

    public string ID;                 // Unique QTE identifier (e.g., "Shake_Coffee")
    public int HouseLevel;            // Which house this QTE belongs to (1-4)

    #endregion

    #region Configuration

    public QTEType QTEType;           // Type of input required
    public float Duration;            // Time limit in seconds

    #endregion

    #region Feedback

    public string SuccessTextAR;      // Arabic text shown on success
    public string FailTextAR;         // Arabic text shown on failure

    #endregion

    #region Effects

    public float SuccessBatteryEffect;    // Battery change on success (negative = good)
    public float FailBatteryEffect;       // Battery change on failure (negative = bad)

    #endregion

    public override string ToString()
    {
        return $"[QTE {ID}] Type:{QTEType} | Duration:{Duration}s | House:{HouseLevel}";
    }
}

/// <summary>
/// Types of QTE inputs supported by the game.
/// </summary>
public enum QTEType
{
    Shake,      // Player shakes device (or presses Space in editor)
    Swipe,      // Player swipes left/right rapidly
    Hold        // Player holds button/touch for duration
}
