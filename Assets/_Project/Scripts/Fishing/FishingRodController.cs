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
        [Tooltip("VR 헤드셋(Main Camera) Transform. 할당 시 캐스팅·챔질 존이 헤드셋 위치를 실시간 추적. 미할당 시 playerData 기반(테스트·시뮬레이션용).")]
        [SerializeField] private Transform hmdReference;

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
            // 방향 보정: 뒤로 날아가지 않도록 HMD(또는 rod) 앞쪽으로 강제 + 최소 위쪽 아크
            Vector3 castDir = direction;
            Vector3 forwardFlat;
            if (hmdReference != null)
                forwardFlat = new Vector3(hmdReference.forward.x, 0f, hmdReference.forward.z).normalized;
            else
                forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            // XZ는 항상 HMD 앞쪽 + 입력 방향의 측면 성분 약간 반영
            Vector3 dirFlat = new Vector3(direction.x, 0f, direction.z);
            float forwardComp = Vector3.Dot(dirFlat, forwardFlat);
            // 입력의 앞쪽 성분이 음수(뒤로 휘두름)면 forward만 사용
            if (forwardComp < 0f) dirFlat = forwardFlat;
            else dirFlat = (forwardFlat + dirFlat * 0.3f).normalized; // 측면 약간 반영

            // 위쪽 아크: 최소 0.4 (포물선 비행 보장)
            float upY = Mathf.Max(0.4f, direction.y);
            castDir = (dirFlat + Vector3.up * upY).normalized;

            float clampedPower = Mathf.Clamp(power, gameSettings.minCastingPower, gameSettings.maxCastingPower);
            Debug.Log($"[Rod] Cast! power={clampedPower:F2}, dir={castDir}");
            SetState(RodState.Casting);
            floatCtrl.Launch(clampedPower, castDir);
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

            // Hysteresis: 진입은 엄격(원래 기준), 이탈은 너그럽게(반경 1.5배 + Y는 중심에서 0.5R 아래까지 허용)
            // → 손 떨림 / 짧은 탈선으로 인한 IN/OUT 깜빡임 방지, 자연스러운 스윙 유지
            float effectiveRadius;
            float yThreshold;
            if (_wasInCastingZone)
            {
                effectiveRadius = gameSettings.castingZoneRadius * 1.5f;
                yThreshold = zoneCenter.y - gameSettings.castingZoneRadius * 0.5f;
            }
            else
            {
                effectiveRadius = gameSettings.castingZoneRadius;
                yThreshold = zoneCenter.y;
            }

            bool inSphere = distance < effectiveRadius;
            bool isAboveYThreshold = controllerPos.y > yThreshold;

            // 이지 모드: Y 조건 무시 / 일반 모드: Y 임계값 (히스테리시스 적용된) 위쪽만 활성
            IsInCastingZone = gameSettings.easyCastingEnabled
                ? inSphere
                : (inSphere && isAboveYThreshold);

            _debugLogTimer += Time.deltaTime;
            if (_debugLogTimer > 0.5f)
            {
                _debugLogTimer = 0f;
                Debug.Log($"[Rod:Zone] hand={controllerPos:F2} center={zoneCenter:F2} dist={distance:F2}/R={effectiveRadius:F2} y>{yThreshold:F2}? {isAboveYThreshold} inZone={IsInCastingZone} (was={_wasInCastingZone}) hold={_castingZoneHoldTime:F2} accel={_acceleration:F2}");
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

        /// <summary>
        /// 캐스팅 존 중심 (월드 좌표). 시각화 컴포넌트 등 외부에서도 참조.
        /// </summary>
        public Vector3 CastingZoneCenter => GetCastingZoneCenter();

        /// <summary>현재 캐스팅 존 체류 시간 (초). minCastingHoldTime 비교용.</summary>
        public float CastingHoldTime => _castingZoneHoldTime;

        /// <summary>현재 가속도(=속도 크기) 기준 예상 캐스팅 파워. clamp 적용됨.</summary>
        public float PredictedCastingPower
        {
            get
            {
                if (gameSettings == null) return 0f;
                return Mathf.Clamp(
                    _acceleration * gameSettings.castingPowerMultiplier,
                    gameSettings.minCastingPower,
                    gameSettings.maxCastingPower);
            }
        }

        private Vector3 GetCastingZoneCenter()
        {
            // hmdReference 할당 시 헤드셋 실시간 위치 추적, 미할당 시 playerData 기반 (테스트용)
            Vector3 center;
            if (hmdReference != null)
            {
                center = hmdReference.position;
            }
            else
            {
                center = playerData.currentPosition;
                center.y = playerData.sittingHeight;
            }
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
            Vector3 controllerPos = _attachedHand != null ? _attachedHand.position : transform.position;
            float distance = Vector3.Distance(controllerPos, hookZoneCenter);
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

        /// <summary>
        /// 챔질 존 중심 (월드 좌표).
        /// </summary>
        public Vector3 HookingZoneCenter => GetHookingZoneCenter();

        private Vector3 GetHookingZoneCenter()
        {
            Vector3 center;
            if (hmdReference != null)
            {
                center = hmdReference.position;
            }
            else
            {
                center = playerData.currentPosition;
                center.y = playerData.sittingHeight;
            }
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
