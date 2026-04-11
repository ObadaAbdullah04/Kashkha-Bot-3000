using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Save data structure for JSON serialization.
/// </summary>
[Serializable]
public class SaveData
{
    public int TotalScrap = 0;
    public int TotalEidia = 0;
    public int HighScore = 0;
    public string LastSaveDate = "";

    // Wardrobe Progression
    public List<int> ownedOutfitIDs = new List<int>();  // IDs of purchased outfits
    public int equippedOutfitID = 0;                     // 0 = no outfit equipped
}
