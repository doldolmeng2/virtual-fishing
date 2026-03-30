using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/MiniGame Settings")]
    public class MiniGameSettingsSO : ScriptableObject
    {
        public float baseDifficulty = 1f;
        public float baseTimeLimit = 60f;
        public float tensionIncreaseRate = 5f;
        public float gaugeIncreaseRate = 10f;
        public float tensionDecreaseRate = 3f;
    }
}
