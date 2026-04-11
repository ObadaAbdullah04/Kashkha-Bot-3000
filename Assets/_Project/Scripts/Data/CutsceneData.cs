using System;

/// <summary>
/// PHASE 9: Data model for a single cutscene definition from Cutscenes.csv
///
/// Cutscenes are triggered by HouseFlowController during house sequences.
/// They play DOTween animations, show text, and create cinematic moments.
///
/// CSV FORMAT (8 columns):
/// ID, HouseLevel, CutsceneType, CharacterName, ExpressionName, Text, Duration, AnimationType
/// </summary>
[Serializable]
public class CutsceneData
{
    #region Identity

    public string ID;                 // Unique cutscene identifier (e.g., "FinishCoffee")
    public int HouseLevel;            // Which house this cutscene belongs to (1-4)

    #endregion

    #region Configuration

    public CutsceneType CutsceneType;   // Type of cutscene animation
    public float Duration;              // How long the cutscene plays (seconds)

    #endregion

    #region Content

    public string TextAR;               // Arabic text to display during cutscene
    public AnimationType Animation;     // DOTween animation style

    #endregion

    #region Expression System (PHASE 12)

    public string CharacterName;        // Character name (matches CharacterExpressionSO)
    public string ExpressionName;       // Expression name (Neutral, Happy, Angry, etc.)

    #endregion

    public override string ToString()
    {
        return $"[Cutscene {ID}] Type:{CutsceneType} | Character:{CharacterName} | Expression:{ExpressionName} | Duration:{Duration}s | Text:\"{TextAR}\"";
    }
}

/// <summary>
/// Types of cutscenes supported by the game.
/// </summary>
public enum CutsceneType
{
    TextReveal,         // Text fades in with typewriter effect
    CharacterReaction,  // Character sprite changes expression + text
    CameraPan,          // Camera moves to show different area
    Dialogue,           // Two characters exchange lines
    ReactionShot        // Single character reacts to previous event
}

/// <summary>
/// DOTween animation styles for cutscenes.
/// </summary>
public enum AnimationType
{
    FadeIn,         // Alpha 0 → 1 with ease
    Bounce,         // Scale pop-in with overshoot
    Slide,          // Translate from off-screen
    Pulse,          // Scale up and down repeatedly
    Typewriter      // Text reveals character by character
}
