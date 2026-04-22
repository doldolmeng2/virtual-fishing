using System;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.MiniGame
{
    public class MiniGameManager : MonoBehaviour, IMiniGame
    {
        [SerializeField] private TensionDataSO tensionData;
        [SerializeField] private MiniGameSettingsSO settings;
        [SerializeField] private TensionCalculator tensionCalculator;
        [SerializeField] private VoidEventSO onMiniGameResultEvent;

        private FishCatchData _fishData;
        private FishMoveState _currentFishMoveState = FishMoveState.Normal;
        private bool _isRunning;

        public float Difficulty { get; private set; }
        public float RemainingTime { get; private set; }
        public float SuccessGauge { get; private set; }

        public event Action<bool> OnMiniGameEnded;
        public event Action<float> OnSuccessGaugeChanged;

        private void OnEnable()
        {
            if (tensionCalculator != null)
            {
                tensionCalculator.OnTensionChanged += HandleTensionChanged;
            }
        }

        private void OnDisable()
        {
            if (tensionCalculator != null)
            {
                tensionCalculator.OnTensionChanged -= HandleTensionChanged;
            }
        }

        public void StartMiniGame(FishCatchData fishData)
        {
            _fishData = fishData;
            _currentFishMoveState = FishMoveState.Normal;
            SuccessGauge = 0f;
            _isRunning = true;

            // 난이도: 저항력 0.7 + 무게 0.3
            float resistance = fishData.species != null ? fishData.species.BaseResistance : 1f;
            Difficulty = Mathf.Max(1f, resistance * 0.7f + fishData.weight * 0.3f);

            tensionCalculator.SetDifficulty(Difficulty);
            tensionCalculator.Reset();
        }

        /// <summary>
        /// 매 프레임 FishingRodController(담당자 B)에서 호출.
        /// reelingSpeed == 0 이면 릴을 감지 않는 상태.
        /// </summary>
        public void UpdateReeling(float reelingSpeed, Vector3 rodDirection)
        {
            if (!_isRunning) return;

            float resistance = _fishData.species != null ? _fishData.species.BaseResistance : 1f;
            tensionCalculator.Calculate(resistance, reelingSpeed, _currentFishMoveState, rodDirection);

            UpdateSuccessGauge(reelingSpeed);
        }

        public void EndMiniGame(bool success)
        {
            if (!_isRunning) return;
            _isRunning = false;

            tensionCalculator.Reset();
            onMiniGameResultEvent?.Raise();
            OnMiniGameEnded?.Invoke(success);
        }

        /// <summary>
        /// 담당자 C(FishController)가 물고기 이동 상태 변경 시 호출.
        /// Normal / Left / Right / Opposite 상태에 따라 텐션 증가 배율이 달라진다.
        /// </summary>
        public void SetFishMoveState(FishMoveState fishMoveState)
        {
            _currentFishMoveState = fishMoveState;
        }

        private void UpdateSuccessGauge(float reelingSpeed)
        {
            bool isReeling = reelingSpeed > 0f;
            bool isTensionAtMin = tensionData.currentTension <= tensionData.tooLowThreshold;

            if (isTensionAtMin)
            {
                // 텐션이 최솟값에 고정 → 물고기가 멀어지는 상황 → 게이지 감소
                SuccessGauge -= settings.gaugeDecreaseRate * Time.deltaTime;
            }
            else if (isReeling)
            {
                // 릴링 시 게이지 상승 (텐션 구간 무관)
                SuccessGauge += settings.gaugeIncreaseRate * reelingSpeed * Time.deltaTime;
            }

            SuccessGauge = Mathf.Clamp(SuccessGauge, 0f, settings.successGaugeMax);
            OnSuccessGaugeChanged?.Invoke(SuccessGauge);

            if (SuccessGauge >= settings.successGaugeMax)
                EndMiniGame(true);
        }

        private void CheckFailure()
        {
            if (tensionData.currentTension >= tensionData.maxTension)
                EndMiniGame(false);
        }

        private void HandleTensionChanged(float _)
        {
            if (!_isRunning)
            {
                return;
            }

            CheckFailure();
        }
    }
}
