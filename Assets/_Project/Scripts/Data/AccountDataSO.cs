using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Account Data")]
    public class AccountDataSO : ScriptableObject
    {
        public string accountId;
        public string lastPlayedAt;
        public List<FishCatchRecord> encyclopedia = new();
        public int totalScore;
    }
}
