using System;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Fishing
{
    [RequireComponent(typeof(Rigidbody))]
    public class FloatController : MonoBehaviour, IFishingFloat
    {
        [Header("설정")]
        [SerializeField] private GameSettingsSO gameSettings;

        [Header("SO 이벤트")]
        [SerializeField] private VoidEventSO onWaterLandedEvent;

        [Header("물리")]
        [SerializeField] private float gravityScale = 1f;

        [Header("참조")]
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform waterSurface;

        [Header("매달림 설정")]
        [SerializeField] private float hangDistance = 0.1f;
        [SerializeField] private float swingDamping = 5f;
        [SerializeField] private float swingSpeed = 3f;

        [Header("회수 설정")]
        [SerializeField] private float reelInSpeed = 8f;
        [SerializeField] private float reelPullSpeed = 2f;

        private Rigidbody _rb;
        private Vector3 _launchOrigin;
        private bool _isLaunched;
        private bool _hasLanded;
        private float _sinkingDepth;
        private bool _isReeling;
        private int _waterLayer;

        private enum FloatState { AttachedToRod, InFlight, OnWater, Reeling }
        private FloatState _state = FloatState.AttachedToRod;
        private float _currentReelSpeed;

        // 매달림 흔들림용
        private float _swingAngle;
        private float _swingVelocity;
        private Vector3 _prevRodTipPos;

        #region IFishingFloat

        public Vector3 Position => transform.position;
        public float SinkingDepth => _sinkingDepth;
        public float Velocity => _rb != null && !_rb.isKinematic ? _rb.linearVelocity.magnitude : 0f;
        public event Action OnWaterLanded;

        public void Launch(float speed, Vector3 direction)
        {
            if (_rb == null) return;

            Debug.Log($"[Float] Launch! speed={speed:F2}, dir={direction}");
            _state = FloatState.InFlight;
            _launchOrigin = transform.position;
            _isLaunched = true;
            _hasLanded = false;
            _isReeling = false;
            _sinkingDepth = 0f;

            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(direction.normalized * speed, ForceMode.VelocityChange);
        }

        public void OnWaterContact()
        {
            if (_hasLanded) return;
            _hasLanded = true;
            _isLaunched = false;
            _state = FloatState.OnWater;

            Debug.Log("[Float] 착수!");
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;

            OnWaterLanded?.Invoke();
            onWaterLandedEvent?.Raise();
        }

        public void Sink(float depth)
        {
            _sinkingDepth = depth;
            Vector3 pos = transform.position;
            pos.y -= depth;
            transform.position = pos;
        }

        #endregion

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.isKinematic = true;
            _waterLayer = LayerMask.NameToLayer("Water");
        }

        private void Start()
        {
            if (rodTip != null)
                _prevRodTipPos = rodTip.position;
        }

        private void Update()
        {
            switch (_state)
            {
                case FloatState.AttachedToRod:
                    UpdateHanging();
                    break;
                case FloatState.OnWater:
                    UpdateOnWater();
                    break;
                case FloatState.Reeling:
                    UpdateReeling();
                    break;
            }

            if (rodTip != null)
                _prevRodTipPos = rodTip.position;
        }

        private void FixedUpdate()
        {
            if (_state != FloatState.InFlight) return;

            _rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

            // 영역 제한
            float distFromOrigin = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(_launchOrigin.x, 0f, _launchOrigin.z)
            );

            if (distFromOrigin > gameSettings.castingBoundaryRadius)
            {
                OnWaterContact();
                return;
            }

            // 수면 아래로 떨어지면 강제 착수 (Trigger 실패 fallback)
            float surfaceY = waterSurface != null ? waterSurface.position.y : 0f;
            if (transform.position.y <= surfaceY)
            {
                Debug.Log("[Float] 수면 아래 도달 → 강제 착수");
                Vector3 pos = transform.position;
                pos.y = surfaceY;
                transform.position = pos;
                OnWaterContact();
            }
        }

        /// <summary>
        /// rodTip 아래에 자연스럽게 매달려 흔들리는 동작
        /// </summary>
        private void UpdateHanging()
        {
            if (rodTip == null) return;

            // rodTip 이동에 따른 흔들림
            Vector3 tipDelta = rodTip.position - _prevRodTipPos;
            float push = tipDelta.x * 30f; // 좌우 이동이 흔들림에 영향

            // 진자 물리 (단순 스프링-댐퍼)
            float restoreForce = -Mathf.Sin(_swingAngle) * swingSpeed;
            _swingVelocity += (restoreForce + push) * Time.deltaTime;
            _swingVelocity *= (1f - swingDamping * Time.deltaTime);
            _swingAngle += _swingVelocity * Time.deltaTime;
            _swingAngle = Mathf.Clamp(_swingAngle, -0.5f, 0.5f);

            // 매달린 위치 계산
            Vector3 hangOffset = new Vector3(
                Mathf.Sin(_swingAngle) * hangDistance,
                -hangDistance,
                0f
            );

            transform.position = rodTip.position + hangOffset;
        }

        /// <summary>
        /// 수면 위에서 릴 감기에 의해 천천히 당겨짐
        /// </summary>
        private void UpdateOnWater()
        {
            if (rodTip == null || _currentReelSpeed <= 0f) return;

            Vector3 toRod = (rodTip.position - transform.position);
            Vector3 pullDir = new Vector3(toRod.x, 0f, toRod.z).normalized;

            transform.position += pullDir * _currentReelSpeed * reelPullSpeed * Time.deltaTime;

            // rodTip 가까이 오면 회수 완료
            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(rodTip.position.x, 0f, rodTip.position.z)
            );

            if (dist < 0.3f)
            {
                Debug.Log("[Float] 릴 감기로 회수 완료");
                _state = FloatState.AttachedToRod;
                _swingAngle = 0f;
                _swingVelocity = 0f;
            }
        }

        /// <summary>
        /// 릴 감기 속도 업데이트 (FishingRodController에서 호출)
        /// </summary>
        public void SetReelSpeed(float speed)
        {
            _currentReelSpeed = speed;
        }

        /// <summary>
        /// 찌를 rodTip으로 빠르게 당겨옴 (강제 회수)
        /// </summary>
        private void UpdateReeling()
        {
            if (rodTip == null) return;

            Vector3 target = rodTip.position + Vector3.down * hangDistance;
            transform.position = Vector3.MoveTowards(transform.position, target, reelInSpeed * Time.deltaTime);

            float dist = Vector3.Distance(transform.position, target);
            if (dist < 0.05f)
            {
                Debug.Log("[Float] 회수 완료");
                _state = FloatState.AttachedToRod;
                _swingAngle = 0f;
                _swingVelocity = 0f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Float] OnTriggerEnter: {other.name}, layer={other.gameObject.layer}, waterLayer={_waterLayer}, state={_state}");
            if (_state != FloatState.InFlight) return;
            if (other.gameObject.layer != _waterLayer) return;

            float distFromOrigin = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(_launchOrigin.x, 0f, _launchOrigin.z)
            );

            if (distFromOrigin < gameSettings.minCastingDistance)
            {
                Vector3 horizontal = new Vector3(
                    _rb.linearVelocity.x, 0f, _rb.linearVelocity.z
                ).normalized;

                if (horizontal.sqrMagnitude < 0.01f)
                    horizontal = Vector3.forward;

                Vector3 correctedPos = _launchOrigin + horizontal * gameSettings.minCastingDistance;
                correctedPos.y = other.bounds.max.y;
                transform.position = correctedPos;
            }

            OnWaterContact();
        }

        /// <summary>
        /// 줄 회수 시작. 찌가 rodTip으로 부드럽게 이동.
        /// </summary>
        public void ResetFloat()
        {
            Debug.Log("[Float] ResetFloat → 회수 시작");
            _isLaunched = false;
            _hasLanded = false;
            _sinkingDepth = 0f;

            if (_rb != null)
            {
                _rb.isKinematic = true;
            }

            // 이미 rodTip 근처면 즉시 Attached, 아니면 Reeling
            if (rodTip != null)
            {
                float dist = Vector3.Distance(transform.position, rodTip.position);
                if (dist < 0.3f)
                {
                    _state = FloatState.AttachedToRod;
                    _swingAngle = 0f;
                    _swingVelocity = 0f;
                }
                else
                {
                    _state = FloatState.Reeling;
                }
            }
            else
            {
                _state = FloatState.AttachedToRod;
            }
        }
    }
}
