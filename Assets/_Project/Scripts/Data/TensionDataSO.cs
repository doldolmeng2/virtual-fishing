using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Tension Data")]
    public class TensionDataSO : ScriptableObject
    {
        [Range(0f, 100f)] public float currentTension;
        public float maxTension = 100f;
        public float tooLowThreshold = 10f;  // 이 값 미만으로 내려가면 성공 게이지 감소, 텐션은 이 값으로 고정
        public float dangerThreshold = 90f;  // 이 값 이상이면 위험 구간 (TensionZone.Critical)

        // Safe: tooLowThreshold ~ dangerThreshold (10~90)
        // Critical: dangerThreshold 이상 (90~100)
        public TensionZone GetCurrentZone()
        {
            if (currentTension < dangerThreshold) return TensionZone.Safe;
            return TensionZone.Critical;
        }

        public void ResetTension() => currentTension = tooLowThreshold;
    }
}
