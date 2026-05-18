using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Tension Data")]
    public class TensionDataSO : ScriptableObject
    {
        [Range(0f, 100f)] public float currentTension;
        public float maxTension = 100f;
        [Tooltip("미니게임 시작 시 텐션 초기값. 여기서 시작해 0(도망 실패) 또는 maxTension(줄 끊김 실패) 으로 향함.")]
        [Range(0f, 100f)] public float initialTension = 50f;
        [Tooltip("이 값 미만이면 성공 게이지가 감소(소프트 페널티). 텐션 자체는 더 내려가 0이 되면 도망 실패.")]
        public float tooLowThreshold = 10f;
        [Tooltip("이 값 이상이면 위험 구간(TensionZone.Critical)")]
        public float dangerThreshold = 90f;

        // Safe: tooLowThreshold ~ dangerThreshold (10~90)
        // Critical: dangerThreshold 이상 (90~100)
        public TensionZone GetCurrentZone()
        {
            if (currentTension < dangerThreshold) return TensionZone.Safe;
            return TensionZone.Critical;
        }

        public void ResetTension() => currentTension = initialTension;
    }
}
