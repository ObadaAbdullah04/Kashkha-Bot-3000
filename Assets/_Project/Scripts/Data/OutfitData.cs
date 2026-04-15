using System;
using UnityEngine;

/// <summary>
/// SIMPLIFIED Outfit data - Phase 18
/// Each outfit is a cosmetic choice with optional unlock cost.
/// No complex stats - just character appearance selection.
/// </summary>
[Serializable]
public class OutfitData
{
    [Header("Identification")]
    public int ID;
    public string internalName;        // Code reference (e.g. "FormalSuit")

    [Header("Display")]
    public string displayNameAR;       // Arabic name for UI
    public string spriteName;          // Character sprite name (e.g. "Character_Formal")

    [Header("Economy")]
    public int scrapCost;              // Cost to unlock (0 if already available)
    public bool isLocked;              // True = requires scrap to unlock
}
