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
/// - 1 outfit locked (ID 3, requires Tech Scrap)
/// </summary>
public class WardrobeManager : MonoBehaviour
{
    public static WardrobeManager Instance { get; private set; }

    [Header("CSV Data")]
    [SerializeField] private TextAsset outfitsCSV;

    [Header("Runtime State")]
    [ReadOnly] [SerializeField] private List<OutfitData> allOutfits = new List<OutfitData>();
    [ReadOnly] [SerializeField] private int currentScrap = 0;
    [ReadOnly] [SerializeField] private int equippedOutfitID = 0;

    public static Action OnWardrobeDataLoaded;
    public static Action OnScrapChanged;
    public static Action OnOutfitPurchased;
    public static Action OnOutfitEquipped;

    public List<OutfitData> AllOutfits => allOutfits;
    public int CurrentScrap => currentScrap;
    public int EquippedOutfitID => equippedOutfitID;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            ParseOutfitsCSV();
            LoadEquippedOutfit();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SyncScrap();
    }

    [Button("Parse Outfits CSV")]
    public void ParseOutfitsCSV()
    {
        allOutfits.Clear();
        if (outfitsCSV == null) return;

        string[] lines = outfitsCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] fields = Regex.Split(lines[i].Trim(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            if (fields.Length < 6) continue;

            int.TryParse(fields[0], out int id);
            int.TryParse(fields[4], out int scrapCost);
            int.TryParse(fields[5], out int lockedInt);

            allOutfits.Add(new OutfitData
            {
                ID = id,
                internalName = fields[1].Trim('"').Trim(),
                displayNameAR = fields[2].Trim('"').Trim(),
                spriteName = fields[3].Trim('"').Trim(),
                scrapCost = scrapCost,
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
            Debug.Log($"[WardrobeManager] Currency (Eidia) synced: {currentScrap}");
            OnScrapChanged?.Invoke();
        }
    }

    public bool OwnsOutfit(int id)
    {
        if (id == 0) return true; // Default skin always owned
        if (SaveManager.Instance == null) return false;

        // If ID 1 or 2, they should be marked isLocked = 0 in CSV
        OutfitData data = allOutfits.Find(o => o.ID == id);
        if (data != null && !data.isLocked) return true;

        return SaveManager.Instance.CurrentData.ownedOutfitIDs.Contains(id);
    }

    public bool UnlockOutfit(int id)
    {
        if (SaveManager.Instance == null) return false;
        OutfitData outfit = allOutfits.Find(o => o.ID == id);
        if (outfit == null || !outfit.isLocked || OwnsOutfit(id)) return false;

        if (currentScrap < outfit.scrapCost) return false;

        if (!SaveManager.Instance.SpendScrap(outfit.scrapCost)) return false;

        SaveManager.Instance.CurrentData.ownedOutfitIDs.Add(id);
        SaveManager.Instance.SaveGame();

        // Sync local scrap after purchase
        currentScrap = SaveManager.Instance.CurrentData.TotalEidia;
        OnScrapChanged?.Invoke();
        OnOutfitPurchased?.Invoke();
        return true;
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
