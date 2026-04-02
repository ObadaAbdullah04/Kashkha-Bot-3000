using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// Manages the Wardrobe meta-progression system.
/// Handles outfit purchasing, equipping, and stat application.
/// </summary>
public class WardrobeManager : MonoBehaviour
{
    #region Singleton

    public static WardrobeManager Instance { get; private set; }
    #endregion


    #region Inspector Fields

    [Header("CSV Data")]
    [SerializeField] private TextAsset outfitsCSV;

    [Header("Parsed Data")]
    [ReadOnly] [SerializeField] private List<OutfitData> allOutfits = new List<OutfitData>();

    [Header("Runtime State")]
    [ReadOnly] [SerializeField] private int currentScrap = 0;
    [ReadOnly] [SerializeField] private int equippedOutfitID = 0;

    #endregion

    #region Events

    public static Action OnWardrobeDataLoaded;
    public static Action OnScrapChanged;
    public static Action OnOutfitPurchased;
    public static Action OnOutfitEquipped;

    #endregion

    #region Public Properties

    public List<OutfitData> AllOutfits => allOutfits;
    public int CurrentScrap => currentScrap;
    public int EquippedOutfitID => equippedOutfitID;

    /// <summary>
    /// Gets the currently equipped outfit data (null if none equipped).
    /// </summary>
    public OutfitData EquippedOutfit
    {
        get
        {
            if (equippedOutfitID == 0) return null;
            return allOutfits.FirstOrDefault(o => o.ID == equippedOutfitID);
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            
            // Auto-parse CSV on startup
            ParseOutfitsCSV();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Sync scrap when save data loads
        if (SaveManager.Instance != null)
            currentScrap = SaveManager.Instance.CurrentData.TotalScrap;
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Parses the Outfits.csv file. Call in Editor or at runtime start.
    /// </summary>
    [Button("Parse Outfits CSV")]
    public void ParseOutfitsCSV()
    {
        allOutfits.Clear();

        if (outfitsCSV == null)
        {
            Debug.LogError("[WardrobeManager] No Outfits.csv file assigned!");
            return;
        }

        string[] lines = outfitsCSV.text.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("[WardrobeManager] Outfits.csv is empty!");
            return;
        }

        // Skip header line
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // Simple CSV split (no quoted strings expected in outfit data)
            string[] fields = lines[i].Split(',');

            if (fields.Length < 9)
            {
                Debug.LogWarning($"[WardrobeManager] Line {i + 1}: Expected 9 columns, got {fields.Length}");
                continue;
            }

            OutfitData data = new OutfitData
            {
                ID = int.Parse(fields[0]),
                internalName = fields[1].Trim('"'),
                displayNameAR = fields[2].Trim('"'),
                descriptionAR = fields[3].Trim('"'),
                iconSpritePath = fields[4].Trim('"'),
                scrapCost = int.Parse(fields[5]),
                statType = (OutfitStatType)int.Parse(fields[6]),
                statValue = float.Parse(fields[7]),
                rarity = (OutfitRarity)int.Parse(fields[8])
            };

            allOutfits.Add(data);
        }

        Debug.Log($"[WardrobeManager] Parsed {allOutfits.Count} outfits.");
        OnWardrobeDataLoaded?.Invoke();
    }

    #endregion

    #region Scrap Management

    /// <summary>
    /// Syncs scrap from SaveManager. Call when entering Wardrobe state.
    /// Does NOT fire OnScrapChanged to avoid event loops.
    /// </summary>
    public void SyncScrap()
    {
        if (SaveManager.Instance != null)
        {
            currentScrap = SaveManager.Instance.CurrentData.TotalScrap;
            Debug.Log($"[WardrobeManager] Scrap synced: {currentScrap}");
            // Note: Don't fire OnScrapChanged here - caller should refresh UI directly
        }
    }

    #endregion

    #region Purchase System

    /// <summary>
    /// Returns true if player owns this outfit.
    /// </summary>
    public bool OwnsOutfit(int outfitID)
    {
        if (SaveManager.Instance == null) return false;
        return SaveManager.Instance.CurrentData.ownedOutfitIDs.Contains(outfitID);
    }

    /// <summary>
    /// Attempts to purchase an outfit. Returns true if successful.
    /// </summary>
    public bool PurchaseOutfit(int outfitID)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[WardrobeManager] SaveManager not available!");
            return false;
        }

        OutfitData outfit = allOutfits.FirstOrDefault(o => o.ID == outfitID);
        if (outfit == null)
        {
            Debug.LogError($"[WardrobeManager] Outfit ID {outfitID} not found!");
            return false;
        }

        if (OwnsOutfit(outfitID))
        {
            Debug.LogWarning($"[WardrobeManager] Already own outfit {outfit.internalName}!");
            return false;
        }

        if (currentScrap < outfit.scrapCost)
        {
            Debug.LogWarning($"[WardrobeManager] Not enough scrap! Need {outfit.scrapCost}, have {currentScrap}");
            return false;
        }

        // Deduct scrap and add outfit
        SaveManager.Instance.CurrentData.TotalScrap -= outfit.scrapCost;
        SaveManager.Instance.CurrentData.ownedOutfitIDs.Add(outfitID);
        SaveManager.Instance.SaveGame();

        currentScrap = SaveManager.Instance.CurrentData.TotalScrap;

        Debug.Log($"[WardrobeManager] Purchased {outfit.internalName} for {outfit.scrapCost} scrap. Remaining: {currentScrap}");
        OnScrapChanged?.Invoke();
        OnOutfitPurchased?.Invoke();

        return true;
    }

    #endregion

    #region Equip System

    /// <summary>
    /// Equips an outfit. Returns true if successful.
    /// </summary>
    public bool EquipOutfit(int outfitID)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[WardrobeManager] SaveManager not available!");
            return false;
        }

        // ID = 0 means unequip (no outfit)
        if (outfitID == 0)
        {
            equippedOutfitID = 0;
            SaveManager.Instance.CurrentData.equippedOutfitID = 0;
            SaveManager.Instance.SaveGame();
            Debug.Log("[WardrobeManager] Outfit unequipped.");
            OnOutfitEquipped?.Invoke();
            return true;
        }

        if (!OwnsOutfit(outfitID))
        {
            Debug.LogWarning($"[WardrobeManager] Cannot equip outfit {outfitID} - not owned!");
            return false;
        }

        equippedOutfitID = outfitID;
        SaveManager.Instance.CurrentData.equippedOutfitID = outfitID;
        SaveManager.Instance.SaveGame();

        Debug.Log($"[WardrobeManager] Equipped outfit ID {outfitID}");
        OnOutfitEquipped?.Invoke();

        return true;
    }

    /// <summary>
    /// Unequips current outfit.
    /// </summary>
    public void UnequipOutfit() => EquipOutfit(0);

    #endregion

    #region Stat Application

    /// <summary>
    /// Gets the stat bonus from the currently equipped outfit.
    /// Returns (statType, value) tuple. Use for applying bonuses at run start.
    /// </summary>
    public (OutfitStatType statType, float value) GetEquippedStatBonus()
    {
        OutfitData outfit = EquippedOutfit;
        if (outfit == null) return (OutfitStatType.BatteryStart, 0f);

        return (outfit.statType, outfit.statValue);
    }

    /// <summary>
    /// Applies outfit stat bonuses to managers. Call at start of run.
    /// </summary>
    public void ApplyOutfitBonuses()
    {
        var (statType, value) = GetEquippedStatBonus();

        if (value == 0) return;

        switch (statType)
        {
            case OutfitStatType.BatteryStart:
                if (MeterManager.Instance != null)
                {
                    // Apply as starting bonus (handled by MeterManager)
                    Debug.Log($"[Wardrobe] Outfit bonus: Starting Battery +{value}%");
                }
                break;

            case OutfitStatType.StomachResist:
                if (MeterManager.Instance != null)
                {
                    // Apply as stomach fill rate reduction
                    Debug.Log($"[Wardrobe] Outfit bonus: Stomach fill rate -{Mathf.Abs(value)}%");
                }
                break;

            case OutfitStatType.TimerExtension:
                if (TimerController.Instance != null)
                {
                    // Apply as timer duration extension
                    Debug.Log($"[Wardrobe] Outfit bonus: Timer +{value}s");
                }
                break;
        }
    }

    #endregion

    #region Inspector Test Buttons

    [Button("▶ Parse CSV")]
    private void TestParse() => ParseOutfitsCSV();

    [Button("💰 Add 50 Scrap")]
    private void TestAddScrap()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentData.TotalScrap += 50;
            SaveManager.Instance.SaveGame();
            currentScrap = SaveManager.Instance.CurrentData.TotalScrap;
            OnScrapChanged?.Invoke();
        }
    }

    [Button("👕 Purchase Outfit 1")]
    private void TestPurchase1() => PurchaseOutfit(1);

    [Button("👕 Purchase Outfit 2")]
    private void TestPurchase2() => PurchaseOutfit(2);

    [Button("👕 Purchase Outfit 3")]
    private void TestPurchase3() => PurchaseOutfit(3);

    [Button("⚡ Equip Outfit 1")]
    private void TestEquip1() => EquipOutfit(1);

    [Button("⚡ Equip Outfit 2")]
    private void TestEquip2() => EquipOutfit(2);

    [Button("⚡ Equip Outfit 3")]
    private void TestEquip3() => EquipOutfit(3);

    [Button("❌ Unequip")]
    private void TestUnequip() => UnequipOutfit();

    [Button("📊 Show Stats")]
    private void TestShowStats()
    {
        var (statType, value) = GetEquippedStatBonus();
        Debug.Log($"[Wardrobe] Equipped Stat: {statType} = {value}");
    }

    #endregion
}
