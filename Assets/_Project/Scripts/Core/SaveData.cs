using System;
using System.Collections.Generic;

namespace KashkhaBot.Core
{
    /// <summary>
    /// Serialized data structure for permanent game progress.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int TotalScrap = 0;
        public int TotalEidia = 0;
        public int HighScore = 0;
        
        // Example for future upgrades:
        // public List<string> UnlockedUpgrades = new List<string>();
        
        public string LastSaveDate;

        public SaveData()
        {
            LastSaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
