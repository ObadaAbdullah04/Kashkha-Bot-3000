using System;
using UnityEngine;

/// <summary>
/// Type of encounter: Trivia (social question) or HospitalityOffer (forced food/drink).
/// </summary>
public enum EncounterType
{
    Trivia,
    HospitalityOffer
}

/// <summary>
/// Encounter data parsed from CSV.
/// Supports flexible sequencing via SequenceOrder and MiniGameAfter columns.
/// </summary>
[Serializable]
public class EncounterData
{
    [Header("Identification")]
    public int ID;
    public int HouseLevel;
    
    [Header("Sequencing (Flexible Gauntlet)")]
    public int SequenceOrder;           // Order within house (1, 2, 3, 4...)
    public bool MiniGameAfter;          // Trigger inter-house mini-game after this encounter

    [Header("Encounter Type")]
    public EncounterType EncounterType; // Trivia or HospitalityOffer
    public string QTEType;              // Legacy QTE type (CoffeeShake, HandOnHeart, TugOfWar) - for backward compatibility
    public string QTEInputType;         // NEW: Generic input type (Shake, Tap, Swipe, Hold)
    public int QTECount;                // NEW: Number of inputs required (shakes, taps, swipes)
    public float QTETimeLimit;          // NEW: Time to complete QTE (seconds)
    public string QTEDirection;         // NEW: Swipe direction (Up, Down, Left, Right) - for Swipe QTE
    public float QTEHoldDuration;       // NEW: Hold duration in seconds - for Hold QTE
    
    [Header("Trivia Content")]
    public string Speaker;
    public string QuestionAR;
    public string Choice1AR;
    public bool Choice1IsCorrect;
    public string Choice1Feedback;
    public string Choice2AR;
    public bool Choice2IsCorrect;
    public string Choice2Feedback;
    public string Choice3AR;
    public bool Choice3IsCorrect;
    public string Choice3Feedback;
    
    [Header("Hospitality Offer Content")]
    public string OfferTextAR;          // Displayed for HospitalityOffer type (e.g., "تفضل معمول مخصوص")
    
    [Header("Rewards & Penalties")]
    public float BatteryDelta;          // Always applies (Trivia: answer-based, Offer: reaction-based)
    public float StomachDelta;          // Only applies to HospitalityOffer (0 for Trivia)
    public int EidiaReward;             // Always awarded (amount varies by choice quality)
    public int ScrapReward;
    
    [Header("Visual")]
    public string ColorHex;             // Hex color for floating text (e.g. #FFD700)
}
