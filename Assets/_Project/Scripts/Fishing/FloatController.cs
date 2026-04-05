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
        [SerializeField] private float waterDrag = 5f;
        [SerializeField] private float sinkSpeed = 0.5f;

        private Rigidbody _rb;
        private Vector3 _launchOrigin;
        private bool _isLaunched;
        private bool _hasLanded;
        private float _sinkingDepth;

        #region IFishingFloat

        public Vector3 Position => transform.position;
        public float SinkingDepth => _sinkingDepth;
        public float Velocity => _rb != null ? _rb.linearVelocity.magnitude : 0f;
        public event Action OnWaterLanded;

        public void Launch(float speed, Vector3 direction)
        {
            if (_rb == null) return;

            _launchOrigin = transform.position;
            _isLaunched = true;
            _hasLanded = false;
            _sinkingDepth = 0f;

            gameObject.SetActive(true);
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(direction.normalized * speed, ForceMode.VelocityChange);
        }

        public void OnWaterContact()
        {
            if (_hasLanded) return;
            _hasLanded = true;
            _isLaunched = false;

            // 물리 정지 후 수면에 안착
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
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (!_isLaunched) return;

            // 커스텀 중력
            _rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

            // 최대 영역 제한: 발사 원점에서 castingBoundaryRadius 초과 시 강제 착수
            float distFromOrigin = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(_launchOrigin.x, 0f, _launchOrigin.z)
            );

            if (distFromOrigin > gameSettings.castingBoundaryRadius)
            {
                OnWaterContact();
            }
        }

        /// <summary>
        /// 수면 Trigger Collider에 의해 호출됨.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!_isLaunched) return;
            if (!other.CompareTag("Water")) return;

            // 최소 거리 보정: 너무 가까우면 최소 거리까지 이동
            float distFromOrigin = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(_launchOrigin.x, 0f, _launchOrigin.z)
            );

            if (distFromOrigin < gameSettings.minCastingDistance)
            {
                // 진행 방향으로 최소 거리까지 보정
                Vector3 horizontal = new Vector3(
                    _rb.linearVelocity.x, 0f, _rb.linearVelocity.z
                ).normalized;

                if (horizontal.sqrMagnitude < 0.01f)
                    horizontal = Vector3.forward;

                Vector3 correctedPos = _launchOrigin + horizontal * gameSettings.minCastingDistance;
                correctedPos.y = other.bounds.max.y; // 수면 높이
                transform.position = correctedPos;
            }

            OnWaterContact();
        }

        /// <summary>
        /// 찌 초기화. 줄 회수 시 호출.
        /// </summary>
        public void ResetFloat()
        {
            _isLaunched = false;
            _hasLanded = false;
            _sinkingDepth = 0f;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            gameObject.SetActive(false);
        }
    }
}
