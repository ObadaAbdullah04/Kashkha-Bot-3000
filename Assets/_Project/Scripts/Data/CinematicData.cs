using System;
using UnityEngine;

/// <summary>
/// PHASE 15: Unified cinematic data — supports both Unity Timeline and DOTween modes.
///
/// USAGE:
/// - UnityTimeline mode: Plays a Unity Timeline asset from Resources/Timelines/
///   → Use for complex animations with multiple objects, camera moves, audio
///
/// - DOTween mode: Plays a DOTween-based text reveal
///   → Use for simple text-only cinematic moments
///
/// Both modes are handled by CinematicController through the same API.
/// </summary>
[Serializable]
public class CinematicData
{
    #region Identity

    public string ID;                         // Unique cinematic ID (e.g., "House1_Intro")
    public int HouseLevel;                    // Which house this belongs to (1-4)

    #endregion

    #region Configuration

    public CinematicType Type;                // UnityTimeline or DOTween
    public string TimelineAssetName;          // If UnityTimeline: Timeline file name (e.g., "House1_Intro")
    public float Duration;                    // Duration in seconds (for DOTween, Timeline uses its own)

    #endregion

    #region DOTween Fields (only used if Type == DOTween)

    public string TextAR;                     // Arabic text to display
    public AnimationType Animation;           // DOTween animation style

    [Header("Visuals (DOTween Only)")]
    [Tooltip("If assigned, shows the character's face.")]
    public CharacterExpressionSO Speaker;     // The character speaking

    [Tooltip("The expression to show (e.g., 'Happy', 'Sad')")]
    public string Expression = "Neutral";     // Expression name

    [Tooltip("If assigned, loads this image from Resources/InteractionIcons/ (e.g. 'Coffee')")]
    public string ResourceImageName;          // Sprite name in Resources

    #endregion

    public override string ToString()
    {
        if (Type == CinematicType.UnityTimeline)
            return $"[Cinematic {ID}] Type:Timeline | Asset:{TimelineAssetName}";
        else
            return $"[Cinematic {ID}] Type:DOTween | Text:\"{TextAR}\"";
    }
}

/// <summary>
/// Types of cinematics supported.
/// </summary>
public enum CinematicType
{
    UnityTimeline,    // Plays a Unity Timeline asset (complex animations)
    DOTween           // Plays DOTween-based text reveal (simple)
}

/// <summary>
/// DOTween animation styles for cinematics.
/// </summary>
public enum AnimationType
{
    FadeIn,         // Text fades in with typewriter effect
    Bounce,         // Scale pop-in with overshoot
    Slide,          // Translate from off-screen
    Pulse           // Scale up and down repeatedly
}
