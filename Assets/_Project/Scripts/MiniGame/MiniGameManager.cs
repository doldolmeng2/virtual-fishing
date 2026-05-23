using System;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Core.Fish;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.MiniGame
{
    /// <summary>미니게임 종료 사유. 피드백/UI 팀이 어떤 이벤트가 발생했는지 구분할 때 사용.</summary>
    public enum MiniGameResult
    {
        Caught,       // 성공: SuccessGauge 가 가득 참 → 물고기 잡음
        LineBreak,    // 실패: 텐션이 maxTension 도달 → 줄 끊김
        FishEscaped   // 실패: 텐션이 0 도달 → 물고기 도망
    }

    public class MiniGameManager : MonoBehaviour, IMiniGame
    {
        [SerializeField] private TensionDataSO tensionData;
        [SerializeField] private MiniGameSettingsSO settings;
        [SerializeField] private TensionCalculator tensionCalculator;
        [SerializeField] private VoidEventSO onMiniGameResultEvent;

        [Header("결과 이벤트 (피드백/UI 팀 구독용)")]
        [Tooltip("성공: 물고기를 잡았을 때 한 번 발행")]
        [SerializeField] private VoidEventSO onFishCaughtEvent;
        [Tooltip("실패: 텐션이 100 에 도달해 줄이 끊어졌을 때 발행")]
        [SerializeField] private VoidEventSO onLineBreakEvent;
        [Tooltip("실패: 텐션이 0 까지 떨어져 물고기가 도망쳤을 때 발행")]
        [SerializeField] private VoidEventSO onFishEscapedEvent;

        [Header("UI")]
        [SerializeField] private FishController fishController;
        [SerializeField] private FishIndicatorUI fishIndicatorUI;

        private FishCatchData _fishData;
        private FishMoveState _currentFishMoveState = FishMoveState.Normal;
        private bool _isRunning;
        private float _phaseHoldTimer;
        private float _normalPhaseTimer;     // Normal 상태에서 누적되는 타이머
        private float _normalPhaseDuration;  // 이번 Normal 사이클의 랜덤 목표 시간 (초)

        public float Difficulty { get; private set; }
        public float RemainingTime { get; private set; }
        public float SuccessGauge { get; private set; }

        // FeedbackManager에서 현재 잡은 물고기 데이터를 읽어갈 수 있게함
        public FishCatchData CurrentFishData => _fishData;

        public event Action<bool> OnMiniGameEnded;
        public event Action<MiniGameResult> OnMiniGameResultDetailed; // 종료 사유까지 포함된 신호
        public event Action<float> OnSuccessGaugeChanged;
        public event Action OnPhaseComplete;

        public MiniGameResult LastResult { get; private set; }

        public float PhaseHoldProgress => settings != null
            ? Mathf.Clamp01(_phaseHoldTimer / settings.phaseHoldDuration)
            : 0f;

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

            _phaseHoldTimer = 0f;
            _normalPhaseTimer = 0f;
            PickNormalPhaseDuration();

            if (fishIndicatorUI != null && fishController != null)
                fishIndicatorUI.Initialize(fishController, this, tensionCalculator);
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

            UpdatePhaseHold(rodDirection);
            UpdateNormalPhase();
            UpdateSuccessGauge(reelingSpeed);
        }

        /// <summary>
        /// 기존 호환용 진입점. 성공/실패 bool 만 받음(실패 사유는 LineBreak 로 가정).
        /// 내부 로직은 EndWith() 로 사유를 명시해서 호출.
        /// </summary>
        public void EndMiniGame(bool success)
            => EndWith(success ? MiniGameResult.Caught : MiniGameResult.LineBreak);

        /// <summary>종료 사유까지 명시해서 미니게임을 마무리한다.</summary>
        public void EndWith(MiniGameResult result)
        {
            if (!_isRunning) return;
            _isRunning = false;
            LastResult = result;

            fishIndicatorUI?.Shutdown();
            tensionCalculator.Reset();

            // 사유별 전용 이벤트 SO 발행 (피드백/UI 팀 구독용)
            switch (result)
            {
                case MiniGameResult.Caught:      onFishCaughtEvent?.Raise();   break;
                case MiniGameResult.LineBreak:   onLineBreakEvent?.Raise();    break;
                case MiniGameResult.FishEscaped: onFishEscapedEvent?.Raise();  break;
            }

            // 기존 generic 이벤트들도 같이 발행
            onMiniGameResultEvent?.Raise();
            OnMiniGameEnded?.Invoke(result == MiniGameResult.Caught);
            OnMiniGameResultDetailed?.Invoke(result);
        }

        /// <summary>
        /// 담당자 C(FishController)가 물고기 이동 상태 변경 시 호출.
        /// Normal / Left / Right / Opposite 상태에 따라 텐션 증가 배율이 달라진다.
        /// </summary>
        public void SetFishMoveState(FishMoveState fishMoveState)
        {
            _currentFishMoveState = fishMoveState;
            _phaseHoldTimer = 0f;
        }

        private void UpdatePhaseHold(Vector3 rodDirection)
        {
            if (!IsRodInOppositeDirection(rodDirection))
            {
                _phaseHoldTimer = Mathf.Max(0f, _phaseHoldTimer - Time.deltaTime);
                return;
            }

            _phaseHoldTimer += Time.deltaTime;
            if (_phaseHoldTimer >= settings.phaseHoldDuration)
            {
                _phaseHoldTimer = 0f;
                OnPhaseComplete?.Invoke();
            }
        }

        private bool IsRodInOppositeDirection(Vector3 rodDirection)
        {
            float threshold = settings != null ? settings.phaseDirectionThreshold : 0.3f;
            return _currentFishMoveState switch
            {
                FishMoveState.Left  => rodDirection.x >  threshold,
                FishMoveState.Right => rodDirection.x < -threshold,
                _                   => false
            };
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
                EndWith(MiniGameResult.Caught);
        }

        private void CheckFailure()
        {
            // 줄 끊김: 텐션이 최댓값(100) 도달
            if (tensionData.currentTension >= tensionData.maxTension)
            {
                EndWith(MiniGameResult.LineBreak);
                return;
            }

            // 도망: 텐션이 0 도달 (초기값 50 에서 시작해 너무 낮아짐)
            if (tensionData.currentTension <= 0f)
            {
                EndWith(MiniGameResult.FishEscaped);
            }
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
