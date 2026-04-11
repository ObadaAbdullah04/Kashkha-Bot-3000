using System;
using UnityEngine;

/// <summary>
/// PHASE 7 REFACTORED: Single swipe card with explicit correct answers from CSV.
/// Player sees both options before swiping - correctness revealed AFTER swipe.
/// 
/// NEW FEATURES:
/// - CardName: Displayed as title (e.g., "خال كريم")
/// - Explicit correct/wrong options (no randomization)
/// - Simplified rewards: Only Battery + Eidia (scrap/stomach removed)
/// </summary>
[Serializable]
public class SwipeCardData
{
    #region Identity

    public string ID;                   // Unique question ID (e.g., "Q1", "Q2")
    public int HouseLevel;              // Which house this question belongs to (1-4)

    #endregion

    #region Display Fields

    public string CardName;             // Title shown above question (e.g., "خال كريم")
    public string SpriteName;           // Sprite file name in Resources/CharacterSprites/ (e.g., "KhalKarim")
    public string Speaker;              // Character name (for attribution)
    public string QuestionAR;           // Arabic question/situation text

    // Options shown on card (player sees BOTH before swiping)
    public string OptionCorrectAR;    // Text of the correct answer
    public string OptionWrongAR;      // Text of the wrong answer

    // Correctness (explicit from CSV, NOT randomized)
    public bool RightIsCorrect;       // true = right side is correct, false = left is correct

    #endregion
    
    #region Feedback
    
    public string CorrectFeedbackAR;      // Shown when player answers correctly
    public string IncorrectFeedbackAR;    // Shown when player answers incorrectly
    
    #endregion
    
    #region Rewards/Penalties

    // Battery changes (negative = drain, positive = gain)
    public float CorrectBatteryDelta;
    public float IncorrectBatteryDelta;

    // Eidia rewards (base reward before streak bonus)
    public int BaseEid;             // Base Eidia reward (streak bonus applied at runtime)

    // Wave assignment (for multi-wave houses)
    public int WaveNumber;          // Which wave this question belongs to (1, 2, 3, etc.)

    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Returns battery delta based on whether the swipe was correct.
    /// </summary>
    public float GetBatteryDelta(bool swipedRight)
    {
        bool wasCorrect = (swipedRight && RightIsCorrect) || (!swipedRight && !RightIsCorrect);
        return wasCorrect ? CorrectBatteryDelta : IncorrectBatteryDelta;
    }
    
    /// <summary>
    /// Returns eidia reward based on whether the swipe was correct.
    /// Note: Streak bonus is applied at runtime by SwipeEncounterManager.
    /// </summary>
    public int GetEidiaReward(bool swipedRight)
    {
        bool wasCorrect = (swipedRight && RightIsCorrect) || (!swipedRight && !RightIsCorrect);
        return wasCorrect ? BaseEid : Mathf.CeilToInt(BaseEid / 2f); // Wrong answer gets half (rounded up)
    }
    
    /// <summary>
    /// Returns feedback text based on whether the swipe was correct.
    /// </summary>
    public string GetFeedback(bool swipedRight)
    {
        bool wasCorrect = (swipedRight && RightIsCorrect) || (!swipedRight && !RightIsCorrect);
        return wasCorrect ? CorrectFeedbackAR : IncorrectFeedbackAR;
    }
    
    /// <summary>
    /// Returns true if the swipe was correct.
    /// </summary>
    public bool WasSwipeCorrect(bool swipedRight)
    {
        return (swipedRight && RightIsCorrect) || (!swipedRight && !RightIsCorrect);
    }
    
    #endregion
    
    public override string ToString()
    {
        return $"[{CardName}] ({SpriteName}) \"{QuestionAR}\" | Correct:{(RightIsCorrect ? "RIGHT" : "LEFT")} | BaseEid:{BaseEid}";
    }
}
