using System;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Fishing
{
    public class FishingRodController : MonoBehaviour, IFishingRod, IGrabbable, ICastable, IVoidEventListener
    {
        [Header("설정")]
        [SerializeField] private GameSettingsSO gameSettings;
        [SerializeField] private PlayerDataSO playerData;

        [Header("참조")]
        [SerializeField] private FloatController floatController;
        [SerializeField] private Transform rodTip;

        [Header("SO 이벤트 - 발행")]
        [SerializeField] private VoidEventSO onRodGrabbed;
        [SerializeField] private VoidEventSO onCastStarted;
        [SerializeField] private VoidEventSO onHookSuccess;
        [SerializeField] private VoidEventSO onHookFailed;
        [SerializeField] private VoidEventSO onRodStateChanged;

        [Header("SO 이벤트 - 구독")]
        [SerializeField] private VoidEventSO onBiteOccurred;
        [SerializeField] private VoidEventSO onWaterLanded;

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
        public event Action<RodState> OnRodStateChanged;

        public void Attach(Transform hand)
        {
            _attachedHand = hand;
            transform.SetParent(hand);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
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
            onRodGrabbed?.Raise();
        }

        public void OnRelease()
        {
            _isGrabbed = false;
            Detach();
            ResetCastingState();
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
            SetState(RodState.Casting);
            onCastStarted?.Raise();
            floatController.Launch(clampedPower, direction);
        }

        #endregion

        #region IVoidEventListener

        public void OnEventRaised()
        {
            // onWaterLanded와 onBiteOccurred 모두 VoidEventSO이므로
            // 별도 핸들러 메서드에서 처리
        }

        #endregion

        private void OnEnable()
        {
            if (onWaterLanded != null) onWaterLanded.Register(this);
            if (onBiteOccurred != null) onBiteOccurred.Register(this);

            // 개별 핸들러를 위해 floatController 이벤트 직접 구독
            if (floatController != null)
                floatController.OnWaterLanded += HandleWaterLanded;
        }

        private void OnDisable()
        {
            if (onWaterLanded != null) onWaterLanded.Unregister(this);
            if (onBiteOccurred != null) onBiteOccurred.Unregister(this);

            if (floatController != null)
                floatController.OnWaterLanded -= HandleWaterLanded;
        }

        private void Update()
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
            _acceleration = (_currentVelocity - _previousVelocity).magnitude / Time.deltaTime;
            _previousVelocity = _currentVelocity;
        }

        #region 캐스팅 존 판정

        private void UpdateCastingZoneCheck()
        {
            Vector3 zoneCenter = GetCastingZoneCenter();
            Vector3 controllerPos = transform.position;
            float distance = Vector3.Distance(controllerPos, zoneCenter);
            bool isAboveCenter = controllerPos.y > zoneCenter.y;

            IsInCastingZone = distance < gameSettings.castingZoneRadius && isAboveCenter;

            if (IsInCastingZone)
            {
                _castingZoneHoldTime += Time.deltaTime;
            }

            // 이지 캐스팅: 존 진입만 하면 자동 투척
            if (gameSettings.easyCastingEnabled && IsInCastingZone)
            {
                Vector3 defaultDir = transform.forward;
                Cast(gameSettings.easyCastingPower, defaultDir);
                ResetCastingState();
                return;
            }

            // 일반 캐스팅: 존 이탈 시 이중 필터 판정
            if (_wasInCastingZone && !IsInCastingZone)
            {
                bool holdValid = _castingZoneHoldTime >= gameSettings.minCastingHoldTime;
                bool accelValid = _acceleration >= gameSettings.minCastingAcceleration;

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
                onHookSuccess?.Raise();

                // Hit → MiniGame 자동 전이
                SetState(RodState.MiniGame);
                return;
            }

            // 타이밍 초과 → 실패
            if (_hookTimingTimer <= 0f)
            {
                _isBiteActive = false;
                IsInHookingZone = false;
                onHookFailed?.Raise();
                ResetToIdle();
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
            _currentState = newState;
            OnRodStateChanged?.Invoke(newState);
            onRodStateChanged?.Raise();
        }

        private void ResetToIdle()
        {
            floatController?.ResetFloat();
            ReelingSpeed = 0f;
            SetState(RodState.Idle);
        }

        public void OnMiniGameEnded()
        {
            ResetToIdle();
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
