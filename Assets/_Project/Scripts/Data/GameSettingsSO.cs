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
        public Vector3 castingZoneOffset = new Vector3(0f, 0.3f, -0.1f);
        public float minCastingHoldTime = 0.15f;
        public float minCastingAcceleration = 1.0f;
        public float minCastingPower = 0.3f;
        public float maxCastingPower = 1.0f;

        [Header("낚시 - 이지 캐스팅")]
        public bool easyCastingEnabled = false;
        public float easyCastingPower = 0.6f;

        [Header("낚시 - 찌")]
        public float minCastingDistance = 2.0f;
        public float castingBoundaryRadius = 15.0f;

        [Header("낚시 - 챔질")]
        public float hookTimingWindow = 3f;
        public float hookingZoneRadius = 0.4f;
        public Vector3 hookingZoneOffset = new Vector3(0f, 0.5f, 0f);
        public float hookingMinAcceleration = 1.5f;

        [Header("컨트롤러")]
        public ControllerHand dominantHand = ControllerHand.Right;

        [Header("안전")]
        public float safetyCheckInterval = 0.2f;
        public float nearBoundaryDistance = 0.3f;
        public float emergencyTimeout = 10f;

        [Header("일반")]
        public float autoConfirmDelay = 10f;
        public float autoSaveInterval = 120f;
    }
}
