using System;
using UnityEngine;

/// <summary>
/// Type of stat bonus provided by an outfit.
/// </summary>
public enum OutfitStatType
{
    BatteryStart,      // Starting battery percentage
    StomachResist,     // Stomach fill rate reduction
    TimerExtension     // Extra seconds on timer
}

/// <summary>
/// Outfit data parsed from CSV.
/// Each outfit provides one stat bonus that applies to all runs once purchased.
/// </summary>
[Serializable]
public class OutfitData
{
    [Header("Identification")]
    public int ID;
    public string internalName;        // Code reference (e.g. "ExtendedBattery")
    
    [Header("Display")]
    public string displayNameAR;       // Arabic name for UI
    public string descriptionAR;       // Arabic description of stat bonus
    public string iconSpritePath;      // Resources path for icon sprite
    
    [Header("Economy")]
    public int scrapCost;              // Cost to purchase
    
    [Header("Stat Bonus")]
    public OutfitStatType statType;    // What stat this modifies
    public float statValue;            // Bonus value (e.g. +10% = 10, -10% = -10, +1s = 1)
    
    [Header("Rarity")]
    public OutfitRarity rarity;        // Visual rarity indicator
}

/// <summary>
/// Outfit rarity for visual distinction and progression gating.
/// </summary>
public enum OutfitRarity
{
    Common,    // White
    Rare,      // Blue
    Epic       // Purple
}
