using UnityEngine;
using System.IO;
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

    public void AddRunRewards(int scrap, int eidia)
    {
        currentSaveData.TotalScrap += scrap;
        currentSaveData.TotalEidia += eidia;
        if (eidia > currentSaveData.HighScore)
            currentSaveData.HighScore = eidia;

        SaveGame();
        Debug.Log($"[Save] Saved! Scrap: {currentSaveData.TotalScrap}, Eidia: {currentSaveData.TotalEidia}");
    }

    [Button("💾 Save")]
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

    [Button("🗑 Reset")]
    public void ResetProgress()
    {
        currentSaveData = new SaveData();
        SaveGame();
        Debug.Log("[Save] Progress reset.");
    }
}
