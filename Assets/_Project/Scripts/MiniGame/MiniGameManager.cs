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

        [Header("낚싯대 위치 참조 (Phase 판정용)")]
        [Tooltip("오른손 컨트롤러 Transform (XR) 또는 시뮬레이션 손 Transform")]
        [SerializeField] private Transform rodHandTransform;
        [Tooltip("HMD(카메라) Transform. 비우면 월드 X 절댓값으로 판정")]
        [SerializeField] private Transform hmdTransform;
        [Tooltip("켜면 매 프레임 좌/우 판정(sideFish/sideHmd) 값을 콘솔에 로깅한다. 손맛 튜닝용.")]
        [SerializeField] private bool logPhaseDirectionDebug = true;

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
            _normalPhaseTimer = 0f;

            if (_currentFishMoveState == FishMoveState.Normal)
            {
                PickNormalPhaseDuration();
            }
        }

        private void UpdatePhaseHold(Vector3 rodDirection)
        {
            // 충전 조건일 때만 누적하되, 임계값을 많이 넘길수록 빠르게(fillSpeed 큼) 찬다.
            // 조건을 잃어도(fillSpeed 0) 떨어뜨리지 않고 현재값을 유지한다.
            float fillSpeed = GetPhaseFillSpeed(rodDirection);
            if (fillSpeed <= 0f)
            {
                return;
            }

            _phaseHoldTimer += Time.deltaTime * fillSpeed;
            if (_phaseHoldTimer >= settings.phaseHoldDuration)
            {
                _phaseHoldTimer = 0f;
                OnPhaseComplete?.Invoke();
            }
        }

        private void UpdateNormalPhase()
        {
            if (_currentFishMoveState != FishMoveState.Normal)
            {
                _normalPhaseTimer = 0f;
                return;
            }

            _normalPhaseTimer += Time.deltaTime;
            if (_normalPhaseTimer < _normalPhaseDuration)
            {
                return;
            }

            _normalPhaseTimer = 0f;
            PickNormalPhaseDuration();
            OnPhaseComplete?.Invoke();
        }

        private void PickNormalPhaseDuration()
        {
            if (settings == null)
            {
                _normalPhaseDuration = 4f;
                return;
            }

            float min = Mathf.Max(0.1f, settings.normalPhaseDurationMin);
            float max = Mathf.Max(min, settings.normalPhaseDurationMax);
            _normalPhaseDuration = UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// 챔질 방향(물고기 진행 반대쪽) 충족도에 따른 phase 게이지 충전 속도 배율을 반환한다.
        /// 0 이면 미충족(충전 안 함). 임계값을 아슬아슬하게 넘기면 느리게(phaseFillSpeedMin),
        /// 많이 넘길수록(팔을 강하게 뻗을수록) 빠르게(phaseFillSpeedMax) 찬다.
        /// </summary>
        private float GetPhaseFillSpeed(Vector3 rodDirection)
        {
            float threshold = settings != null ? settings.phaseDirectionThreshold : 0.3f;

            // HMD(머리)를 기준으로 컨트롤러를 좌/우로 얼마나 뻗었는지(단순 X 차이).
            // xValue > 0 : 컨트롤러가 머리 오른쪽, xValue < 0 : 머리 왼쪽.
            float xValue;
            if (rodHandTransform != null)
            {
                Vector3 center = hmdTransform != null ? hmdTransform.position : Vector3.zero;
                xValue = rodHandTransform.position.x - center.x;
            }
            else
            {
                xValue = rodDirection.x;
            }

            // 임계값을 얼마나 초과했는지(m). 0 이상이면 충족, 클수록 "많이" 뻗은 상태.
            float margin = _currentFishMoveState switch
            {
                FishMoveState.Left  =>  xValue - threshold,
                FishMoveState.Right => -xValue - threshold,
                _                   => -1f
            };

            float fillSpeed = 0f;
            if (margin >= 0f)
            {
                float minRate    = settings != null ? settings.phaseFillSpeedMin : 0.4f;
                float maxRate    = settings != null ? settings.phaseFillSpeedMax : 2f;
                float fullMargin = settings != null ? Mathf.Max(0.0001f, settings.phaseFillFullMargin) : 0.3f;
                float t = Mathf.Clamp01(margin / fullMargin);
                fillSpeed = Mathf.Lerp(minRate, maxRate, t);
            }

            if (logPhaseDirectionDebug)
            {
                // 팔 뻗음 정도(margin)에 따라 충전 속도(fillSpeed)가 어떻게 변하는지 확인용.
                Debug.Log(
                    $"[MiniGame] state={_currentFishMoveState} thr={threshold:F2} | " +
                    $"xValue={xValue:F2} margin={margin:F2} fillSpeed={fillSpeed:F2} " +
                    $"=> {(fillSpeed > 0f ? "충전" : "미충족")}");
            }

            return fillSpeed;
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
