using UnityEngine;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// 릴 회전 입력 컨트롤러 (proximity 기반).
    /// 두 가지 회전 신호를 동시에 측정해서 큰 쪽을 사용:
    ///   1) 손 위치 회전 — 손이 릴 중심 주위로 원을 그리며 도는 속도 (옛 구현)
    ///   2) 컨트롤러 자체 회전 — 손목을 비트는 동작 (대부분의 자연스러운 cranking은 이쪽)
    /// 둘 중 하나만 충족돼도 릴이 돌아가도록 → 인식률 대폭 향상.
    /// </summary>
    public class FishingReelController : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private FishingRodController rodController;
        [Tooltip("회전을 측정할 손 컨트롤러 transform (보통 Left Controller)")]
        [SerializeField] private Transform handTransform;

        [Header("활성 조건 (히스테리시스)")]
        [Tooltip("손이 이 거리 이내로 들어오면 engage")]
        [SerializeField] private float engageDistance = 0.25f;
        [Tooltip("engage 후 이 거리를 넘어가야 disengage")]
        [SerializeField] private float disengageDistance = 0.40f;

        [Header("회전 축")]
        [Tooltip("회전 축 기준 — None: 이 GameObject 로컬 X / Rod: 낚싯대 right / Hand: 손 forward")]
        [SerializeField] private AxisReference axisReference = AxisReference.RodRight;
        [Tooltip("AxisReference=ReelLocal일 때 사용할 로컬 축")]
        [SerializeField] private Vector3 reelAxisLocal = Vector3.right;

        [Header("측정 파라미터")]
        [Tooltip("입력 1.0에 매핑할 각속도 (deg/sec). 360=1회전/sec → 일반 cranking 속도")]
        [SerializeField] private float maxAngularSpeed = 360f;
        [Tooltip("최소 각속도 (deg/sec) — 미만은 0으로 처리해 노이즈 제거. 너무 높으면 느린 회전 인식 안됨")]
        [SerializeField] private float deadzoneAngularSpeed = 8f;
        [Tooltip("음의 회전(반대 방향)도 입력으로 인정할지")]
        [SerializeField] private bool allowReverse = true;
        [Tooltip("입력 스무딩 (0~1, 1=즉각, 0.5~0.7이 반응 좋음)")]
        [SerializeField, Range(0.05f, 1f)] private float smoothing = 0.6f;
        [Tooltip("손이 회전 축에 너무 가까우면(이 거리 이내) 위치 기반 신호 무시. 손목 회전만 사용")]
        [SerializeField] private float minProjectionRadius = 0.02f;

        [Header("디버그")]
        [SerializeField] private bool verboseLog = false;
        [Tooltip("로그 출력 주기(s)")]
        [SerializeField] private float logInterval = 0.25f;

        public enum AxisReference
        {
            ReelLocal,   // 이 GameObject(rod03) 로컬 reelAxisLocal
            RodRight,    // FishingRod transform.right (가장 직관적 — 낚싯대 옆 방향)
            HandForward, // 손 컨트롤러 forward (손목 비틀기 동작에 자연스러움)
        }

        private bool _isEngaged;
        private Vector3 _previousHandDir;
        private Quaternion _previousHandRotation;
        private float _smoothedReelInput;
        private float _logTimer;
        private float _lastPosSpeed, _lastRotSpeed;

        public bool IsEngaged => _isEngaged;
        public bool IsBeingReeled => _isEngaged && _smoothedReelInput > 0.01f;

        private void Update()
        {
            if (rodController == null || handTransform == null) return;

            if (!rodController.IsGrabbed)
            {
                if (_isEngaged) Disengage();
                return;
            }

            float dist = Vector3.Distance(handTransform.position, transform.position);
            if (!_isEngaged && dist <= engageDistance) Engage();
            else if (_isEngaged && dist > disengageDistance) { Disengage(); return; }
            if (!_isEngaged) return;

            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            Vector3 axisWorld = ComputeAxisWorld();

            // ============ 신호 1: 손 위치 회전 (손이 원을 그림) ============
            float posAngularSpeed = 0f;
            Vector3 handFromCenter = handTransform.position - transform.position;
            Vector3 projected = Vector3.ProjectOnPlane(handFromCenter, axisWorld);
            float projRadius = projected.magnitude;
            Vector3 currentDir = projRadius > 1e-4f ? projected / projRadius : _previousHandDir;

            if (projRadius >= minProjectionRadius)
            {
                float signedAngle = Vector3.SignedAngle(_previousHandDir, currentDir, axisWorld);
                posAngularSpeed = signedAngle / dt;
            }
            _previousHandDir = currentDir;

            // ============ 신호 2: 컨트롤러 자체 회전 (손목 비틀기) ============
            Quaternion deltaRot = handTransform.rotation * Quaternion.Inverse(_previousHandRotation);
            deltaRot.ToAngleAxis(out float deltaAngle, out Vector3 deltaAxis);
            // ToAngleAxis는 항상 양수 각도 반환. axis 방향에 부호가 들어있음
            // axis와 우리 reel axis의 내적으로 부호 결정
            float axisDot = Vector3.Dot(deltaAxis.normalized, axisWorld);
            float rotAngularSpeed = (deltaAngle * axisDot) / dt;
            _previousHandRotation = handTransform.rotation;

            // ============ 둘 중 큰 신호 채택 ============
            float chosenSpeed = Mathf.Abs(posAngularSpeed) > Mathf.Abs(rotAngularSpeed)
                ? posAngularSpeed : rotAngularSpeed;
            _lastPosSpeed = posAngularSpeed;
            _lastRotSpeed = rotAngularSpeed;

            float effectiveSpeed = allowReverse ? Mathf.Abs(chosenSpeed) : Mathf.Max(0f, chosenSpeed);
            if (effectiveSpeed < deadzoneAngularSpeed) effectiveSpeed = 0f;

            float rawInput = Mathf.Clamp01(effectiveSpeed / Mathf.Max(1f, maxAngularSpeed));
            _smoothedReelInput = Mathf.Lerp(_smoothedReelInput, rawInput, smoothing);
            rodController.UpdateReelingInput(_smoothedReelInput);

            if (verboseLog)
            {
                _logTimer += dt;
                if (_logTimer > logInterval)
                {
                    _logTimer = 0f;
                    Debug.Log($"[Reel] pos={posAngularSpeed:+000;-000;000}°/s rot={rotAngularSpeed:+000;-000;000}°/s eff={effectiveSpeed:F0}°/s → in={_smoothedReelInput:F2}  (radius={projRadius*100:F1}cm)");
                }
            }
        }

        private void Engage()
        {
            _isEngaged = true;
            Vector3 axisWorld = ComputeAxisWorld();
            Vector3 projected = Vector3.ProjectOnPlane(handTransform.position - transform.position, axisWorld);
            _previousHandDir = projected.sqrMagnitude > 1e-6f ? projected.normalized : transform.up;
            _previousHandRotation = handTransform.rotation;
            _smoothedReelInput = 0f;
            Debug.Log("[Reel] engage");
        }

        private void Disengage()
        {
            _isEngaged = false;
            _smoothedReelInput = 0f;
            if (rodController != null) rodController.UpdateReelingInput(0f);
            Debug.Log("[Reel] disengage");
        }

        private Vector3 ComputeAxisWorld()
        {
            switch (axisReference)
            {
                case AxisReference.RodRight:
                    if (rodController != null) return rodController.transform.right;
                    break;
                case AxisReference.HandForward:
                    if (handTransform != null) return handTransform.forward;
                    break;
            }
            return transform.TransformDirection(reelAxisLocal).normalized;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, engageDistance);
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, disengageDistance);

            if (Application.isPlaying)
            {
                Vector3 axis = ComputeAxisWorld() * 0.2f;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position - axis, transform.position + axis);
            }
        }
    }
}
