using System;
using System.Collections.Generic;

namespace VirtualFishing.Data
{
    [Serializable]
    public class AccountSaveData
    {
        public string accountId;
        public string lastPlayedAt;
        public List<FishCatchRecord> encyclopedia = new();
        public int totalScore;
    }
}
