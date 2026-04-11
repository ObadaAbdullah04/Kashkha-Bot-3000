using UnityEngine;
using System.IO;
using System;
using NaughtyAttributes;

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
        currentSaveData.TotalEidia += eidia;
        if (eidia > currentSaveData.HighScore)
            currentSaveData.HighScore = eidia;

        SaveGame();
        Debug.Log($"[Save] Saved! Eidia: {currentSaveData.TotalEidia}");
    }

    /// <summary>
    /// Adds scrap to the global save data and persists immediately.
    /// Called when mini-games complete to ensure scrap persists across runs.
    /// </summary>
    public void AddScrap(int amount)
    {
        if (amount <= 0) return;

        currentSaveData.TotalScrap += amount;
        SaveGame();
        
        // Fire event to notify UI updates
        OnScrapChanged?.Invoke(currentSaveData.TotalScrap);
        
        Debug.Log($"[Save] Scrap added: +{amount}. Total: {currentSaveData.TotalScrap}");
    }

    [Button("Save")]
    public void SaveGame()
    {
        try
        {
            currentSaveData.LastSaveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string json = JsonUtility.ToJson(currentSaveData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] Game saved");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Save] Failed: {e.Message}");
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
                Debug.Log($"[Save] Loaded. Scrap: {currentSaveData.TotalScrap}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Save] Load failed: {e.Message}");
                currentSaveData = new SaveData();
            }
        }
        else
        {
            Debug.Log("[Save] No save file. Creating new.");
            currentSaveData = new SaveData();
            SaveGame();
        }
    }

    [Button("Reset")]
    public void ResetProgress()
    {
        currentSaveData = new SaveData();
        SaveGame();
        Debug.Log("[Save] Progress reset.");
    }
}
