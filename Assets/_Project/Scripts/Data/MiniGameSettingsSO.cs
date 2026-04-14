using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/MiniGame Settings")]
    public class MiniGameSettingsSO : ScriptableObject
    {
        public float baseDifficulty = 1f;
        public float baseTimeLimit = 60f;

        [Header("Tension")]
        public float resistanceFactor = 1f;            // fish.Resistance에 곱하는 기본 계수
        public float tensionIncreaseRate = 5f;         // 릴링 시 텐션 증가율 (reelingSpeed에 곱)
        public float tensionDecreaseRate = 3f;         // 릴링 안 할 때 텐션 감소율
        public float tensionMultiplierSide = 1.5f;     // FishMoveState Left/Right 시 텐션 증가 배율
        public float tensionMultiplierOpposite = 2.0f; // FishMoveState Opposite 시 텐션 증가 배율

        [Header("Success Gauge")]
        public float gaugeIncreaseRate = 10f;          // 릴링 시 성공 게이지 증가율
        public float gaugeDecreaseRate = 5f;           // 텐션이 tooLowThreshold에 고정될 때 성공 게이지 감소율
        public float successGaugeMax = 100f;

        [Header("Bite Timer")]
        public float biteMinTime = 10f;                // 본 입질 최소 대기 시간 (초)
        public float biteMaxTime = 20f;                // 본 입질 최대 대기 시간 (초)
        public float biteGapMinTime = 1f;              // 예고 입질과 본 입질 사이 최소 간격 (초)
    }
}
