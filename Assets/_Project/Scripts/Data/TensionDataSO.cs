using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Tension Data")]
    public class TensionDataSO : ScriptableObject
    {
        [Range(0f, 100f)] public float currentTension;
        public float maxTension = 100f;
        public float safeZoneMin = 20f;
        public float safeZoneMax = 60f;
        public float dangerThreshold = 80f;

        public TensionZone GetCurrentZone()
        {
            if (currentTension < safeZoneMin) return TensionZone.Safe;
            if (currentTension < safeZoneMax) return TensionZone.Warning;
            if (currentTension < dangerThreshold) return TensionZone.Danger;
            return TensionZone.Critical;
        }

        public void ResetTension() => currentTension = 0f;
    }
}
