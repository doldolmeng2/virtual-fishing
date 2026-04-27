using System;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.MiniGame
{
    public class TensionCalculator : MonoBehaviour, ITensionCalculator
    {
        [SerializeField] private TensionDataSO tensionData;
        [SerializeField] private MiniGameSettingsSO settings;
        [SerializeField] private FloatEventSO onTensionChangedEvent;

        private float _difficulty = 1f;
        private TensionZone _prevZone;

        public float CurrentTension => tensionData.currentTension;
        public TensionZone CurrentZone => tensionData.GetCurrentZone();

        public event Action<float> OnTensionChanged;
        public event Action<TensionZone> OnTensionZoneChanged;

        public void SetDifficulty(float difficulty)
        {
            _difficulty = Mathf.Max(1f, difficulty);
        }

        /// <summary>
        /// 매 프레임 MiniGameManager.UpdateReeling()에서 호출.
        /// 텐션 값을 계산하고 TensionDataSO에 기록한 뒤 이벤트를 발행한다.
        /// </summary>
        public void Calculate(float fishResistance, float reelingSpeed, FishMoveState fishMoveState, Vector3 rodDirection)
        {
            float dt = Time.deltaTime;
            bool isReeling = reelingSpeed > 0f;

            // 물고기 저항에 의한 기본 텐션 증가
            float resistanceDelta = fishResistance * settings.resistanceFactor * dt;

            // 낚싯대 방향 보정
            // rodDirection이 아래쪽(물고기 방향)을 향할수록 oppositeDirectionBonus가 커져 텐션 증가를 줄임
            float rodUpFactor = Vector3.Dot(rodDirection.normalized, Vector3.up); // -1 ~ 1
            float directionBonus = Mathf.Max(0f, -rodUpFactor) * settings.tensionDecreaseRate * dt;

            // 릴링 여부에 따른 증감
            float reelingDelta = isReeling
                ? reelingSpeed * settings.tensionIncreaseRate * dt
                : -settings.tensionDecreaseRate * dt; // 릴링 안 할 때 감소 (배율 없음)

            float totalDelta = resistanceDelta - directionBonus + reelingDelta;

            // 증가할 때만 Difficulty × FishMoveState 배율 적용
            if (totalDelta > 0f)
            {
                float stateMult = GetStateMultiplier(fishMoveState);
                totalDelta *= stateMult * _difficulty;
            }

            tensionData.currentTension += totalDelta;
            tensionData.currentTension = Mathf.Clamp(
                tensionData.currentTension,
                tensionData.tooLowThreshold,
                tensionData.maxTension
            );

            OnTensionChanged?.Invoke(tensionData.currentTension);
            onTensionChangedEvent?.Raise(tensionData.currentTension);

            TensionZone zone = tensionData.GetCurrentZone();
            if (zone != _prevZone)
            {
                OnTensionZoneChanged?.Invoke(zone);
                _prevZone = zone;
            }
        }

        public void Reset()
        {
            _difficulty = 1f;
            tensionData.ResetTension();
            _prevZone = tensionData.GetCurrentZone();
            OnTensionChanged?.Invoke(tensionData.currentTension);
        }

        private float GetStateMultiplier(FishMoveState state)
        {
            return state switch
            {
                FishMoveState.Left or FishMoveState.Right => settings.tensionMultiplierSide,
                FishMoveState.Opposite                    => settings.tensionMultiplierOpposite,
                _                                         => 1f
            };
        }
    }
}
