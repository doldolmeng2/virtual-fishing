using UnityEngine;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// 낚싯대가 컨트롤러를 따라갈 때 생기는 미세한 손 떨림을 감쇠한다. (이슈 #35)
    ///
    /// XRGrabInteractable(MovementType=Instantaneous)이 매 프레임/렌더 직전에 낚싯대 회전을
    /// 컨트롤러에 맞춰 세팅한 뒤, 그 회전을 '각속도 적응형 저역통과 필터'로 보정한다.
    ///   - 느리고 작은 회전(가만히 들고 있을 때의 떨림) → 강하게 스무딩(덜 반영)
    ///   - 빠르고 큰 회전(의도적인 스윙/챔질)           → 거의 그대로 통과(실시간 반영)
    /// 위치는 건드리지 않는다 — 캐스팅·챔질 판정이 컨트롤러 위치/속도를 그대로 사용하기 때문.
    ///
    /// XRI는 지연을 줄이려고 렌더 직전(onBeforeRender)에도 포즈를 갱신하므로, LateUpdate가 아니라
    /// 더 늦은 BeforeRenderOrder에서 보정해야 XRI에 덮어쓰이지 않는다.
    /// </summary>
    public class RodTremorDamper : MonoBehaviour
    {
        [Tooltip("그랩 상태일 때만 보정 (미할당 시 항상 보정)")]
        [SerializeField] private FishingRodController rodController;

        [Header("적응형 감쇠")]
        [Tooltip("이 각속도(deg/s) 이하 → 떨림으로 간주해 최대로 스무딩")]
        [SerializeField] private float idleAngularSpeed = 25f;
        [Tooltip("이 각속도(deg/s) 이상 → 의도적 움직임으로 간주해 거의 그대로 통과")]
        [SerializeField] private float activeAngularSpeed = 200f;
        [Tooltip("떨림 영역 스무딩 시정수(s) — 클수록 떨림이 더 줄지만 반응이 느려짐")]
        [SerializeField] private float maxSmoothTime = 0.10f;
        [Tooltip("의도적 움직임 영역 스무딩 시정수(s) — 0이면 지연 없이 즉각")]
        [SerializeField] private float minSmoothTime = 0f;

        private Quaternion _filtered;
        private bool _initialized;

        private void Awake()
        {
            if (rodController == null) rodController = GetComponent<FishingRodController>();
        }

        private void OnEnable() => Application.onBeforeRender += ApplyFilter;

        private void OnDisable()
        {
            Application.onBeforeRender -= ApplyFilter;
            _initialized = false;
        }

        // XRI의 포즈 갱신(onBeforeRender) 이후에 실행되도록 충분히 큰 순서값 사용.
        [BeforeRenderOrder(2000)]
        private void ApplyFilter()
        {
            // 그랩되지 않은 동안에는 XRI가 낚싯대를 제어하지 않으므로 보정하지 않는다.
            if (rodController != null && !rodController.IsGrabbed)
            {
                _initialized = false;
                return;
            }

            Quaternion target = transform.rotation; // XRI가 이번 렌더에 맞춰 세팅한 컨트롤러 추종 회전
            if (!_initialized)
            {
                _filtered = target;
                _initialized = true;
                return;
            }

            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            float angularSpeed = Quaternion.Angle(_filtered, target) / dt; // 의도된 각속도(deg/s)

            // 각속도가 낮으면 maxSmoothTime(강한 감쇠), 높으면 minSmoothTime(즉각)으로 보간.
            float t = Mathf.InverseLerp(idleAngularSpeed, activeAngularSpeed, angularSpeed);
            float tau = Mathf.Lerp(maxSmoothTime, minSmoothTime, t);
            float k = tau <= 0f ? 1f : 1f - Mathf.Exp(-dt / tau);

            _filtered = Quaternion.Slerp(_filtered, target, k);
            transform.rotation = _filtered;
        }
    }
}
