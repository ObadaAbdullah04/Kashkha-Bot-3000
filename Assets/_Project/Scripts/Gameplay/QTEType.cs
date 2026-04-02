
    /// <summary>
/// Generic input types for QTE system - NOT context-specific.
/// This allows the same QTE logic to be reused for any cultural interaction.
/// CSV column "QTEInputType" uses these values.
/// </summary>
public enum QTEInputType
{
    None,       // No QTE
    Shake,      // Shake device N times
    Tap,        // Tap N times
    Swipe,      // Swipe in direction
    Hold        // Hold for N seconds, then release
    // FUTURE PHASES: Draw (draw shape), Rotate (rotate device), Tilt (tilt to angle), Combo (sequence)
}

/// <summary>
/// Swipe direction for Swipe QTE.
/// </summary>
public enum SwipeDirection
{
    Up,
    Down,
    Left,
    Right
}