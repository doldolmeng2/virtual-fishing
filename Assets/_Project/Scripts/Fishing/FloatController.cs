using System;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Core.Fish;
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
        [SerializeField] private VoidEventSO onWaterLanded;

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
        private float _launchTime;
        private const float LaunchCollisionGrace = 0.2f; // 발사 직후 충돌 정지 무시 시간(초)
        private bool _isLaunched;
        private bool _hasLanded;
        private float _sinkingDepth;
        private bool _isReeling;
        private int _waterLayer;

        private enum FloatState { AttachedToRod, InFlight, OnWater, Reeling, Idle }
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

        /// <summary>물이 아닌 곳(땅 등)에 떨어진 찌를 수동 회수로 낚싯대까지 끌어왔을 때 발행. (낚싯대 캐스팅 취소→준비 복귀용)</summary>
        public event Action OnReeledBack;

        /// <summary>자동 회수 중(릴을 감아 찌를 rodTip으로 끌어오는 Reeling 상태)인지. 시각적 릴 회전용.</summary>
        public bool IsAutoReeling => _state == FloatState.Reeling;

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
            _launchTime = Time.time;

            _rb.isKinematic = false;
            // 직접 할당 — AddForce(VelocityChange)는 isKinematic 토글 직후
            // 1프레임 동안 적용되지 않을 수 있음 (특히 unfocused editor).
            _rb.linearVelocity = direction.normalized * speed;
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
            onWaterLanded?.Raise();
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
            TryResolveWaterSurface();

            if (rodTip != null)
                _prevRodTipPos = rodTip.position;
        }

        /// <summary>런타임/프리팹 환경 생성 후 FishEnvironmentController 등에서 호출.</summary>
        public void BindWaterSurface(Transform surface)
        {
            if (surface == null)
            {
                return;
            }

            waterSurface = surface;
        }

        private void TryResolveWaterSurface()
        {
            if (waterSurface != null)
            {
                return;
            }

            PondWaterSurface pond = FindFirstObjectByType<PondWaterSurface>();
            if (pond != null)
            {
                waterSurface = pond.transform;
                Debug.Log($"[Float] waterSurface 자동 연결: {pond.name}");
            }
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
                case FloatState.Idle:
                    UpdateIdleReelBack();
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
                // 캐스팅 가능 영역을 벗어남(장외) → 물이 아니므로 진행하지 않고 정지(무시)
                HaltWithoutLanding();
                return;
            }

            // 수면 '높이' 아래로 내려오면 표면 판정 (Trigger 실패 fallback)
            float surfaceY = waterSurface != null ? waterSurface.position.y : 0f;
            if (transform.position.y <= surfaceY)
            {
                if (IsOverWater())
                {
                    // 물 위 → 착수(진행)
                    Vector3 pos = transform.position;
                    pos.y = surfaceY;
                    transform.position = pos;
                    OnWaterContact();
                }
                else
                {
                    // 물이 아님(땅 등) → 진행하지 않고 그 자리에 정지(무시). 수동 회수로만 복귀.
                    HaltWithoutLanding();
                }
            }
        }

        // [물 판정] 착수 높이에서 아래로 레이캐스트. 레이어 무관, PondWaterSurface 컴포넌트로 물 판정.
        private bool IsOverWater()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 3f, ~0, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue; // 찌 자신 무시
                if (IsWaterCollider(hit.collider)) return true;
            }
            return false;
        }

        // [물 콜라이더 판정] PondWaterSurface 컴포넌트(레이어 무관) 또는 Water 레이어면 물로 인정.
        private bool IsWaterCollider(Collider col)
        {
            if (col == null) return false;
            if (col.GetComponentInParent<PondWaterSurface>() != null) return true;
            return _waterLayer >= 0 && col.gameObject.layer == _waterLayer;
        }

        // [물 아님] 진행하지 않고 그 자리에 정지(무시). 이벤트 발행 안 함 → 낚시 흐름 진행 X. 수동 회수로만 복귀.
        private void HaltWithoutLanding()
        {
            _isLaunched = false;
            _hasLanded = false;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }
            _state = FloatState.Idle;
            Debug.Log("[Float] 물이 아닌 곳에 떨어짐 → 정지(무시). 회수 전까지 진행 안 함.");
        }

        // [Idle 회수] 물이 아닌 곳에 멈춘 찌를 수동 릴 입력(_currentReelSpeed)으로 낚싯대까지 끌어옴.
        private void UpdateIdleReelBack()
        {
            if (rodTip == null || _currentReelSpeed <= 0f) return;

            Vector3 target = rodTip.position + Vector3.down * hangDistance;
            transform.position = Vector3.MoveTowards(transform.position, target,
                reelInSpeed * _currentReelSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                _state = FloatState.AttachedToRod;
                _swingAngle = 0f;
                _swingVelocity = 0f;
                OnReeledBack?.Invoke();
                Debug.Log("[Float] 회수 완료 → 낚싯대 준비 복귀");
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

        // [충돌 착수] non-trigger 물/땅 콜라이더와 물리 충돌 시 즉시 처리.
        // 물이면 착수(진행), 물이 아니면(땅 등) 곧바로 정지 → 미끄러져 물까지 흘러가는 것 방지.
        private void OnCollisionEnter(Collision collision)
        {
            if (_state != FloatState.InFlight) return;
            // 발사 직후(낚싯대 근처) 충돌은 무시
            if (Time.time - _launchTime < LaunchCollisionGrace) return;

            if (IsWaterCollider(collision.collider))
                OnWaterContact();
            else if (IsOverWater())
                OnWaterContact();
            else
                HaltWithoutLanding();
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Float] OnTriggerEnter: {other.name}, layer={other.gameObject.layer}, waterLayer={_waterLayer}, state={_state}");
            if (_state != FloatState.InFlight) return;
            if (!IsWaterCollider(other)) return;

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
