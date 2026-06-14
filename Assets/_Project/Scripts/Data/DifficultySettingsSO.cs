using UnityEngine;

namespace VirtualFishing.Data
{
    /// <summary>
    /// 로그인 화면에서 선택한 난이도 설정.
    /// ScriptableObject 런타임 상태는 씬 전환 후에도 메모리에 유지된다.
    /// </summary>
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Difficulty Settings")]
    public class DifficultySettingsSO : ScriptableObject
    {
        [Header("활성 상태")]
        [Tooltip("로그인 화면에서 쉬운 난이도를 켰는지 여부")]
        public bool isEasyMode;

        [Header("캐스팅")]
        [Tooltip("쉬운 난이도일 때 캐스팅 존 반경 배율")]
        public float castingZoneRadiusMultiplier = 1.5f;

        [Tooltip("쉬운 난이도일 때 필요 캐스팅 가속도 배율 (작을수록 쉬움)")]
        public float castingMinAccelerationMultiplier = 0.7f;

        [Header("챔질")]
        [Tooltip("쉬운 난이도일 때 챔질 존 반경 배율")]
        public float hookingZoneRadiusMultiplier = 1.25f;

        [Tooltip("쉬운 난이도일 때 필요 가속도 배율 (작을수록 쉬움)")]
        public float hookingMinAccelerationMultiplier = 0.75f;

        [Tooltip("쉬운 난이도일 때 챔질 타이밍 윈도우 배율")]
        public float hookTimingWindowMultiplier = 1.2f;

        [Header("텐션 — 릴링 미실행 시 감소율")]
        [Tooltip("정규화 텐션(0~1)이 이 값 이하면 감소 속도를 줄인다")]
        [Range(0f, 1f)] public float lowTensionNormalizedThreshold = 0.1f;

        [Tooltip("정규화 텐션(0~1)이 이 값 이상이면 감소 속도를 높인다")]
        [Range(0f, 1f)] public float highTensionNormalizedThreshold = 0.9f;

        [Tooltip("낮은 텐션 구간 감소율 배율 (작을수록 천천히 떨어짐)")]
        public float lowTensionDecreaseMultiplier = 0.9f;

        [Tooltip("높은 텐션 구간 감소율 배율 (클수록 빠르게 떨어짐)")]
        public float highTensionDecreaseMultiplier = 1.1f;

        [Header("성공 게이지")]
        [Tooltip("쉬운 난이도일 때 성공 게이지 증가율 배율")]
        public float successGaugeIncreaseMultiplier = 1.5f;

        [Tooltip("일반 난이도 물고기 회수 진행도 배율. 1=해안까지, 0.9=90% 지점에서 잡힘")]
        [Range(0.3f, 1f)] public float baseReelingProgressScale = 0.9f;

        [Tooltip("쉬운 난이도일 때 물고기 회수 진행도 배율 (일반 배율 대신 적용)")]
        [Range(0.3f, 1f)] public float reelingProgressScale = 0.8f;

        public bool IsEasyMode => isEasyMode;

        public void SetEasyMode(bool enabled) => isEasyMode = enabled;

        public float GetEffectiveCastingZoneRadius(float baseRadius)
        {
            return IsEasyMode ? baseRadius * castingZoneRadiusMultiplier : baseRadius;
        }

        public float GetEffectiveCastingMinAcceleration(float baseAcceleration)
        {
            return IsEasyMode ? baseAcceleration * castingMinAccelerationMultiplier : baseAcceleration;
        }

        public float GetEffectiveHookingZoneRadius(float baseRadius)
        {
            return IsEasyMode ? baseRadius * hookingZoneRadiusMultiplier : baseRadius;
        }

        public float GetEffectiveHookingMinAcceleration(float baseAcceleration)
        {
            return IsEasyMode ? baseAcceleration * hookingMinAccelerationMultiplier : baseAcceleration;
        }

        public float GetEffectiveHookTimingWindow(float baseWindow)
        {
            return IsEasyMode ? baseWindow * hookTimingWindowMultiplier : baseWindow;
        }

        /// <summary>
        /// 릴링하지 않을 때 텐션 감소율에 곱할 배율.
        /// 낮은 텐션 → 감소 느림, 높은 텐션 → 감소 빠름.
        /// </summary>
        public float GetTensionDecreaseMultiplier(float currentTension, float maxTension)
        {
            if (!IsEasyMode || maxTension <= 0f)
                return 1f;

            float normalized = Mathf.Clamp01(currentTension / maxTension);

            if (normalized <= lowTensionNormalizedThreshold)
                return lowTensionDecreaseMultiplier;

            if (normalized >= highTensionNormalizedThreshold)
                return highTensionDecreaseMultiplier;

            float t = (normalized - lowTensionNormalizedThreshold)
                      / Mathf.Max(0.0001f, highTensionNormalizedThreshold - lowTensionNormalizedThreshold);
            return Mathf.Lerp(lowTensionDecreaseMultiplier, highTensionDecreaseMultiplier, t);
        }

        public float GetSuccessGaugeIncreaseRate(float baseRate)
        {
            return IsEasyMode ? baseRate * successGaugeIncreaseMultiplier : baseRate;
        }

        public float GetReelingProgress(float gaugeProgress)
        {
            float scale = IsEasyMode ? reelingProgressScale : baseReelingProgressScale;
            return gaugeProgress * scale;
        }
    }
}
