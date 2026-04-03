using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [Header("캘리브레이션")]
        public float calibrationTimeout = 30f;

        [Header("낚시 - 캐스팅")]
        public float castingPowerMultiplier = 1f;
        public float castingZoneRadius = 0.5f;

        [Header("낚시 - 챔질")]
        public float hookTimingWindow = 3f;
        public float hookingZoneRadius = 0.4f;
        public Vector3 hookingZoneOffset = new Vector3(0f, 0.5f, 0f);
        public float hookingMinAcceleration = 1.5f;

        [Header("안전")]
        public float safetyCheckInterval = 0.2f;
        public float nearBoundaryDistance = 0.3f;
        public float emergencyTimeout = 10f;

        [Header("일반")]
        public float autoConfirmDelay = 10f;
        public float autoSaveInterval = 120f;
    }
}
