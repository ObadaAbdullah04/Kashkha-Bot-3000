using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using System.Text.RegularExpressions;

/// <summary>
/// SIMPLIFIED Wardrobe System - Phase 18
/// Manages exactly 4 choices: Default skin + 3 outfits.
/// - Default skin (ID 0, always available)
/// - 2 outfits available from start (ID 1, 2)
/// - 1 outfit locked (ID 3, requires Eidia)
/// </summary>
public class WardrobeManager : MonoBehaviour
{
    public static WardrobeManager Instance { get; private set; }

    [Header("CSV Data")]
    [SerializeField] private TextAsset outfitsCSV;

    [Header("Runtime State")]
    [ReadOnly] [SerializeField] private List<OutfitData> allOutfits = new List<OutfitData>();
    [ReadOnly] [SerializeField] private int currentScrap = 0; // Represents Eidia balance for outfits
    [ReadOnly] [SerializeField] private int equippedOutfitID = 0;

    public static Action OnWardrobeDataLoaded;
    public static Action OnScrapChanged; // Fired when Eidia balance changes (for wardrobe sync)
    public static Action OnOutfitPurchased;
    public static Action OnOutfitEquipped;

    public List<OutfitData> AllOutfits => allOutfits;
    public int CurrentScrap => currentScrap;
    public int EquippedOutfitID => equippedOutfitID;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        ParseOutfitsCSV();
        LoadEquippedOutfit();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            OnWardrobeDataLoaded = null;
            OnScrapChanged = null;
            OnOutfitPurchased = null;
            OnOutfitEquipped = null;
            Instance = null;
        }
    }

    private void OnEnable()
    {
        SyncScrap();
    }

    [Button("Parse Outfits CSV")]
    public void ParseOutfitsCSV()
    {
        if (outfitsCSV == null) return;
        
        allOutfits.Clear();
        string[] lines = outfitsCSV.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = Regex.Split(lines[i].Trim(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            if (fields.Length < 6) continue;

            int.TryParse(fields[0], out int id);
            int.TryParse(fields[4], out int eidiaCost);
            int.TryParse(fields[5], out int lockedInt);

            allOutfits.Add(new OutfitData
            {
                ID = id,
                internalName = fields[1].Trim('"').Trim(),
                displayNameAR = fields[2].Trim('"').Trim(),
                spriteName = fields[3].Trim('"').Trim(),
                scrapCost = eidiaCost, // Using the existing field name to avoid struct refactor
                isLocked = (lockedInt == 1)
            });
        }
        OnWardrobeDataLoaded?.Invoke();
    }

    private void LoadEquippedOutfit()
    {
        if (SaveManager.Instance != null)
            equippedOutfitID = SaveManager.Instance.CurrentData.equippedOutfitID;
    }

    public void SyncScrap()
    {
        if (SaveManager.Instance != null)
        {
            currentScrap = SaveManager.Instance.CurrentData.TotalEidia;
            OnScrapChanged?.Invoke();
        }
    }

    public bool OwnsOutfit(int id)
    {
        if (id == 0) return true; // Default skin always owned
        if (SaveManager.Instance == null) return false;

        OutfitData data = allOutfits.Find(o => o.ID == id);
        if (data != null && !data.isLocked) return true;

        return SaveManager.Instance.CurrentData.ownedOutfitIDs.Contains(id);
    }

    public bool UnlockOutfit(int id)
    {
        if (SaveManager.Instance == null) return false;
        OutfitData outfit = allOutfits.Find(o => o.ID == id);
        if (outfit == null || !outfit.isLocked || OwnsOutfit(id)) return false;

        // Sync local currency first to be sure
        currentScrap = SaveManager.Instance.CurrentData.TotalEidia;

        if (currentScrap < outfit.scrapCost) return false;

        if (SaveManager.Instance.SpendEidia(outfit.scrapCost))
        {
            SaveManager.Instance.CurrentData.ownedOutfitIDs.Add(id);
            SaveManager.Instance.SaveGame();

            currentScrap = SaveManager.Instance.CurrentData.TotalEidia;
            OnScrapChanged?.Invoke();
            OnOutfitPurchased?.Invoke();
            return true;
        }

        return false;
    }

    public bool EquipOutfit(int id)
    {
        if (SaveManager.Instance == null || !OwnsOutfit(id)) return false;

        equippedOutfitID = id;
        SaveManager.Instance.CurrentData.equippedOutfitID = id;
        SaveManager.Instance.SaveGame();
        OnOutfitEquipped?.Invoke();
        return true;
    }
}
