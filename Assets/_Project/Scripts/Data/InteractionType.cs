/// <summary>
/// Interaction types for the standalone interaction system.
/// Each type corresponds to a different player input method.
/// 
/// USAGE:
/// - Defined in Interactions.csv per interaction
/// - Drives which HUD prefab and input detection logic to use
/// - Mapped to DeviceControls input actions via InputManager
/// </summary>
public enum InteractionType
{
    Shake,      // Shake phone (mobile) / Space key (editor simulation)
    Hold,       // Hold button/finger (mobile) / Hold Space (editor)
    Tap,        // Rapid taps (mobile) / Rapid clicks (editor)
    Draw        // Draw path on screen (uses existing PathDrawingGame logic)
}

/// <summary>
/// Extension methods for InteractionType enum.
/// </summary>
public static class InteractionTypeExtensions
{
    /// <summary>
    /// Returns the icon sprite name for this interaction type.
    /// Sprites should be in Resources/InteractionIcons/
    /// </summary>
    public static string GetIconSpriteName(this InteractionType type)
    {
        return type switch
        {
            InteractionType.Shake => "Icon_Shake",
            InteractionType.Hold => "Icon_Hold",
            InteractionType.Tap => "Icon_Tap",
            InteractionType.Draw => "Icon_Draw",
            _ => "Icon_Shake"
        };
    }

    /// <summary>
    /// Returns a human-readable Arabic label for this interaction type.
    /// Used as fallback if PromptTextAR is missing.
    /// </summary>
    public static string GetArabicLabel(this InteractionType type)
    {
        return type switch
        {
            InteractionType.Shake => "هز!",
            InteractionType.Hold => "امسك!",
            InteractionType.Tap => "اضغط!",
            InteractionType.Draw => "ارسم!",
            _ => "تفاعل!"
        };
    }
}
