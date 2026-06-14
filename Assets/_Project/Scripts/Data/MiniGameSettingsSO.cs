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
        public float gaugeIncreaseRate = 12f;          // 릴링 시 성공 게이지 증가율
        public float gaugeDecreaseRate = 5f;           // 텐션이 tooLowThreshold에 고정될 때 성공 게이지 감소율
        public float successGaugeMax = 100f;

        [Header("Bite Timer")]
        public float biteMinTime = 10f;                // 본 입질 최소 대기 시간 (초)
        public float biteMaxTime = 20f;                // 본 입질 최대 대기 시간 (초)
        public float biteGapMinTime = 1f;              // 예고 입질과 본 입질 사이 최소 간격 (초)

        [Header("Phase Completion")]
        public float phaseHoldDuration = 2f;           // 반대 방향 유지 시간 (초). fillSpeed=1 기준 충전 완료 시간
        public float phaseDirectionThreshold = 1.0f;   // HMD 기준 컨트롤러 좌/우 판정 임계값 (단순 X 차이 m)
        public float phaseFillSpeedMin = 0.1f;         // 임계값을 아슬아슬하게 넘겼을 때 충전 속도 배율
        public float phaseFillSpeedMax = 1f;           // 임계값을 충분히 넘겼을 때(많이 뻗음) 충전 속도 배율
        public float phaseFillFullMargin = 0.8f;       // 임계값을 이만큼(m) 초과하면 최대 속도(phaseFillSpeedMax) 도달
        public float normalPhaseDurationMin = 3f;      // Normal 상태에서 자동 phase 완료까지 최소 시간 (초)
        public float normalPhaseDurationMax = 5f;      // Normal 상태에서 자동 phase 완료까지 최대 시간 (초)

        [Tooltip("페이즈 완료 시 텐션이 이 값 이상이면 phaseCompleteTensionReduction 만큼 감소")]
        public float phaseCompleteTensionRewardThreshold = 65f;

        [Tooltip("페이즈 완료 텐션 보상 — 감소량")]
        public float phaseCompleteTensionReduction = 10f;
    }
}
