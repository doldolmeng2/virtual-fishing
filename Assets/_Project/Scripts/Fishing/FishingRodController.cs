using System;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Fishing.Events;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Fishing
{
    public class FishingRodController : MonoBehaviour, IFishingRod, IGrabbable, ICastable
    {
        [Header("설정")]
        [SerializeField] private GameSettingsSO gameSettings;
        [SerializeField] private PlayerDataSO playerData;

        [Header("참조")]
        [SerializeField] private FloatController floatCtrl;
        [SerializeField] private Transform rodTip;

        [Header("SO 이벤트 - 발행")]
        [SerializeField] private RodStateTransitionEventSO onRodStateChanged;

        // [SO 이벤트 - 구독]
        // 설계 문서의 SO Event 패턴(VoidEventListener bridge → UnityEvent → 메서드)을 따라
        // 씬에 별도 VoidEventListener 컴포넌트가 OnBiteOccurred.asset을 듣고 HandleBiteOccurred를 호출하도록 연결.
        // FloatController.OnWaterLanded는 같은 프리팹 내부 C# 이벤트로 직접 구독 (아래 OnEnable 참조).

        // 상태
        private RodState _currentState = RodState.Idle;
        private Transform _attachedHand;
        private bool _isGrabbed;

        // 캐스팅 존 판정
        private bool _wasInCastingZone;
        private float _castingZoneHoldTime;

        // 챔질 판정
        private bool _isBiteActive;
        private float _hookTimingTimer;

        // 가속도 측정 (XR deviceVelocity 기반)
        private Vector3 _previousVelocity;
        private Vector3 _currentVelocity;
        private float _acceleration;
        private Vector3 _direction;

        #region IFishingRod

        public RodState CurrentState => _currentState;
        public float Acceleration => _acceleration;
        public Vector3 Direction => _direction;
        public float ReelingSpeed { get; private set; }
        public bool IsInCastingZone { get; private set; }
        public bool IsInHookingZone { get; private set; }
        public event Action<RodStateTransition> OnRodStateChanged;

        public void Attach(Transform hand)
        {
            _attachedHand = hand;
            transform.SetParent(hand);
            // 잡은 위치의 상대 오프셋 유지 (현실적 그랩)
            // VR에서는 XR Grab Interactable이 이걸 자동 처리함
        }

        public void Detach()
        {
            _attachedHand = null;
            transform.SetParent(null);
        }

        public void UpdateCastingInput(Vector3 controllerVelocity, Vector3 controllerDirection)
        {
            _currentVelocity = controllerVelocity;
            _direction = controllerDirection.normalized;
        }

        public void UpdateReelingInput(float rotationDelta)
        {
            ReelingSpeed = rotationDelta;
            floatCtrl?.SetReelSpeed(rotationDelta);
        }

        #endregion

        #region IGrabbable

        public bool IsGrabbed => _isGrabbed;
        public event Action OnGrabbed;
        public event Action OnReleased;

        public void OnGrab(Transform hand)
        {
            if (_currentState != RodState.Idle) return;

            _isGrabbed = true;
            Attach(hand);
            SetState(RodState.Attached);
            OnGrabbed?.Invoke();
        }

        public void OnRelease()
        {
            _isGrabbed = false;

            // 캐스팅 후 그랩 해제 시 찌가 수면에 방치되지 않도록 회수.
            // ResetFloat은 idempotent — 이미 Attached 상태여도 안전.
            floatCtrl?.ResetFloat();

            Detach();
            ResetCastingState();
            _isBiteActive = false;
            SetState(RodState.Idle);
            OnReleased?.Invoke();
        }

        #endregion

        #region ICastable

        public bool IsCasting => _currentState == RodState.Casting;
        public event Action OnCastComplete;

        public void Cast(float power, Vector3 direction)
        {
            float clampedPower = Mathf.Clamp(power, gameSettings.minCastingPower, gameSettings.maxCastingPower);
            Debug.Log($"[Rod] Cast! power={clampedPower:F2}, dir={direction}");
            SetState(RodState.Casting);
            floatCtrl.Launch(clampedPower, direction);
        }

        #endregion

        private void OnEnable()
        {
            // 같은 프리팹 내 직접 참조 — 설계 다이어그램의 'FishingRodController --> FloatController' 관계에 부합.
            if (floatCtrl != null)
                floatCtrl.OnWaterLanded += HandleWaterLanded;
        }

        private void OnDisable()
        {
            if (floatCtrl != null)
                floatCtrl.OnWaterLanded -= HandleWaterLanded;
        }

        private void LateUpdate()
        {
            if (!_isGrabbed) return;

            UpdateAcceleration();

            switch (_currentState)
            {
                case RodState.Attached:
                    UpdateCastingZoneCheck();
                    break;
                case RodState.WaitingForBite:
                    UpdateHookingCheck();
                    break;
            }
        }

        private void UpdateAcceleration()
        {
            // 속도의 크기 변화 = 가속도
            _acceleration = _currentVelocity.magnitude;
            _direction = _currentVelocity.normalized;
        }

        #region 캐스팅 존 판정

        private float _debugLogTimer;

        private void UpdateCastingZoneCheck()
        {
            Vector3 zoneCenter = GetCastingZoneCenter();
            // 손(컨트롤러) 위치를 사용 — 낚싯대가 아닌 부착된 손 기준
            Vector3 controllerPos = _attachedHand != null ? _attachedHand.position : transform.position;
            float distance = Vector3.Distance(controllerPos, zoneCenter);
            bool isAboveCenter = controllerPos.y > zoneCenter.y;

            IsInCastingZone = distance < gameSettings.castingZoneRadius && isAboveCenter;

            // 주기적 디버그 로그
            _debugLogTimer += Time.deltaTime;
            if (_debugLogTimer > 0.5f)
            {
                _debugLogTimer = 0f;
                Debug.Log($"[Rod:Zone] hand={controllerPos:F2} center={zoneCenter:F2} dist={distance:F2} radius={gameSettings.castingZoneRadius} above={isAboveCenter} inZone={IsInCastingZone} hold={_castingZoneHoldTime:F2} accel={_acceleration:F2}");
            }

            if (IsInCastingZone)
            {
                _castingZoneHoldTime += Time.deltaTime;
            }

            // 이지 캐스팅: 존 진입만 하면 자동 투척
            if (gameSettings.easyCastingEnabled && IsInCastingZone)
            {
                Vector3 defaultDir = _attachedHand != null ? _attachedHand.forward : transform.forward;
                Cast(gameSettings.easyCastingPower, defaultDir);
                ResetCastingState();
                return;
            }

            // 일반 캐스팅: 존 이탈 시 이중 필터 판정
            if (_wasInCastingZone && !IsInCastingZone)
            {
                bool holdValid = _castingZoneHoldTime >= gameSettings.minCastingHoldTime;
                bool accelValid = _acceleration >= gameSettings.minCastingAcceleration;
                Debug.Log($"[Rod:Cast?] 존 이탈! holdTime={_castingZoneHoldTime:F2}(>={gameSettings.minCastingHoldTime}) holdOK={holdValid}, accel={_acceleration:F2}(>={gameSettings.minCastingAcceleration}) accelOK={accelValid}");

                if (holdValid && accelValid)
                {
                    float power = _acceleration * gameSettings.castingPowerMultiplier;
                    Cast(power, _direction);
                }

                ResetCastingState();
            }

            _wasInCastingZone = IsInCastingZone;
        }

        private Vector3 GetCastingZoneCenter()
        {
            // 하이브리드: y축은 캘리 고정, x/z는 HMD 실시간
            // HMD 위치를 직접 가져올 수 없는 경우 playerData 사용
            Vector3 center = playerData.currentPosition;
            center.y = playerData.sittingHeight;
            return center + gameSettings.castingZoneOffset;
        }

        private void ResetCastingState()
        {
            _wasInCastingZone = false;
            _castingZoneHoldTime = 0f;
            IsInCastingZone = false;
        }

        #endregion

        #region 챔질 판정

        public void HandleBiteOccurred()
        {
            if (_currentState != RodState.WaitingForBite) return;

            _isBiteActive = true;
            _hookTimingTimer = gameSettings.hookTimingWindow;
        }

        private void UpdateHookingCheck()
        {
            if (!_isBiteActive) return;

            _hookTimingTimer -= Time.deltaTime;

            // 챔질 존 판정
            Vector3 hookZoneCenter = GetHookingZoneCenter();
            float distance = Vector3.Distance(transform.position, hookZoneCenter);
            IsInHookingZone = distance < gameSettings.hookingZoneRadius;

            // 이중 조건: 존 진입 + 위쪽 가속도
            if (IsInHookingZone && _acceleration >= gameSettings.hookingMinAcceleration)
            {
                _isBiteActive = false;
                IsInHookingZone = false;
                SetState(RodState.Hit);

                // Hit → MiniGame 자동 전이
                SetState(RodState.MiniGame);
                return;
            }

            // 타이밍 초과 → 실패
            if (_hookTimingTimer <= 0f)
            {
                _isBiteActive = false;
                IsInHookingZone = false;
                ReelIn();
            }
        }

        private Vector3 GetHookingZoneCenter()
        {
            Vector3 center = playerData.currentPosition;
            center.y = playerData.sittingHeight;
            return center + gameSettings.hookingZoneOffset;
        }

        #endregion

        #region 이벤트 핸들러

        private void HandleWaterLanded()
        {
            Debug.Log($"[Rod] HandleWaterLanded, currentState={_currentState}");
            if (_currentState == RodState.Casting)
            {
                SetState(RodState.WaitingForBite);
                OnCastComplete?.Invoke();
            }
        }

        #endregion

        #region 상태 관리

        private void SetState(RodState newState)
        {
            if (_currentState == newState) return;
            var previous = _currentState;
            Debug.Log($"[Rod] {previous} → {newState}");
            _currentState = newState;
            var transition = new RodStateTransition(previous, newState);
            OnRodStateChanged?.Invoke(transition);
            onRodStateChanged?.Raise(transition);
        }

        /// <summary>
        /// 줄 회수. 그랩 유지한 채 Attached로 복귀.
        /// </summary>
        public void ReelIn()
        {
            Debug.Log("[Rod] 줄 회수 (ReelIn)");
            _isBiteActive = false;
            IsInHookingZone = false;
            floatCtrl?.ResetFloat();
            ReelingSpeed = 0f;

            if (_isGrabbed)
                SetState(RodState.Attached);
            else
                SetState(RodState.Idle);
        }

        /// <summary>
        /// 디버그: 강제 상태 전환
        /// </summary>
        public void ForceState(RodState state)
        {
            Debug.Log($"[Rod] 강제 전환: {_currentState} → {state}");
            _currentState = state;

            if (state == RodState.WaitingForBite)
            {
                _isBiteActive = false;
            }
        }

        /// <summary>
        /// 미니게임 종료 시 호출되는 외부 진입점. ReelIn으로 위임.
        ///
        /// [통합 미정 — 10주차 회의 결정 사항]
        /// 설계 다이어그램은 OnMiniGameResult SO의 흐름을 MiniGameManager → GameFlowManager로만 명시.
        /// rod 리셋 트리거 경로가 미정이므로 본 메서드는 직접 호출(GameFlowManager가 IFishingRod 참조로 호출)
        /// 또는 신규 SO 채널(예: OnRodResetRequested) 도입 후 wiring될 예정.
        /// 단독으로 OnMiniGameResult.asset에 wiring하지 말 것 — D/A 도메인과 충돌 가능.
        /// </summary>
        public void OnMiniGameEnded()
        {
            ReelIn();
        }

        #endregion

        #region 디버그

        private void OnDrawGizmosSelected()
        {
            if (gameSettings == null || playerData == null) return;

            // 캐스팅 존 시각화
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(GetCastingZoneCenter(), gameSettings.castingZoneRadius);

            // 챔질 존 시각화
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(GetHookingZoneCenter(), gameSettings.hookingZoneRadius);
        }

        #endregion
    }
}
