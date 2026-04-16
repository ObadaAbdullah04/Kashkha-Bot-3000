using UnityEngine;
using System.IO;
using System;
using NaughtyAttributes;
using Unity.Barracuda;

/// <summary>
/// Simple save manager using JSON.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Save")]
    [SerializeField] private string fileName = "save_data.json";
    [ReadOnly] [SerializeField] private SaveData currentSaveData;

    public SaveData CurrentData => currentSaveData;
    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    /// <summary>
    /// Fires when scrap currency is modified.
    /// Used by UnifiedHubManager to refresh wardrobe/upgrade UI in real-time.
    /// </summary>
    public static Action<int> OnScrapChanged; // (newScrapTotal)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddRunRewards(int eidia)
    {
        if (eidia == 0) return;

        currentSaveData.TotalEidia += eidia;
        if (eidia > currentSaveData.HighScore)
            currentSaveData.HighScore = eidia;

        SaveGame();
        
        // PHASE 18: Notify that Currency has changed (UI uses OnScrapChanged as the event)
        OnScrapChanged?.Invoke(currentSaveData.TotalEidia);
        
        // Debug.Log($"[Save] Currency added: +{eidia}. Total Eidia: {currentSaveData.TotalEidia}");
    }

    /// <summary>
    /// Redirects to AddRunRewards to maintain unified currency (Eidia).
    /// </summary>
    public void AddScrap(int amount)
    {
        AddRunRewards(amount);
    }

    /// <summary>
    /// Deducts Eidia if player can afford it.
    /// Returns true if successful.
    /// </summary>
    public bool SpendScrap(int amount)
    {
        if (amount <= 0) return true;
        if (currentSaveData.TotalEidia < amount) return false;

        currentSaveData.TotalEidia -= amount;
        SaveGame();

        // Fire event to notify UI updates (Wardrobe, Upgrades, etc.)
        OnScrapChanged?.Invoke(currentSaveData.TotalEidia);

        // Debug.Log($"[Save] Currency spent: -{amount}. Remaining Eidia: {currentSaveData.TotalEidia}");
        return true;
    }

    [Button("Save")]
    public void SaveGame()
    {
        try
        {
            currentSaveData.LastSaveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(currentSaveData, true);
            File.WriteAllText(SavePath, json);
            // Debug.Log($"[Save] Game saved");
        }
        catch (System.Exception)
        {
            // Debug.LogError($"[Save] Failed: {e.Message}");
        }
    }

    [Button("📂 Load")]
    public void LoadGame()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                currentSaveData = JsonUtility.FromJson<SaveData>(json);
                // Debug.Log($"[Save] Loaded. Total Eidia: {currentSaveData.TotalEidia}");
            }
            catch (System.Exception)
            {
                // Debug.LogError($"[Save] Load failed: {e.Message}");
                currentSaveData = new SaveData();
            }
        }
        else
        {
            // Debug.Log("[Save] No save file. Creating new.");
            currentSaveData = new SaveData();
            SaveGame();
        }
    }

    [Button("Reset")]
    public void ResetProgress()
    {
        currentSaveData = new SaveData();
        SaveGame();
        // Debug.Log("[Save] Progress reset.");
    }

    [Button("Add 100 Scrap")]
    public void AddTempScrap()
    {
        AddScrap(100);    
    }

}
