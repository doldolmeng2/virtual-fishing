using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VirtualFishing.Calibration
{
    /// <summary>
    /// 그랩 가능한 객체가 캘리브레이션 전엔 HMD를 따라다니다가 첫 그랩 시 follow 멈춤.
    ///
    /// 닭과 달걀 해결: VR 사용자가 어디서 시작하든(머리가 게임 (0,0,0)에서 멀리 떨어져 있어도)
    /// 트리거 객체는 항상 시야 안에 있어 잡을 수 있음. 첫 그랩 시 SeatedPoseCalibrator가 정렬을
    /// 발동시키므로 그 이후엔 follow가 필요 없음.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class FollowHmdUntilGrabbed : MonoBehaviour
    {
        [SerializeField] private Transform hmd;

        [Header("배치 오프셋 (HMD 기준)")]
        [Tooltip("HMD 정면 forward 방향으로 떨어진 거리 (손 닿기 편한 거리)")]
        [SerializeField] private float distanceFromHmd = 0.4f;
        [Tooltip("HMD 시점에서 수직 오프셋. 음수 = 아래쪽 (앉은 자세에서 손이 자연히 가는 위치)")]
        [SerializeField] private float verticalOffset = -0.3f;

        [Header("동작")]
        [Tooltip("HMD forward를 수평면(XZ)으로만 투영. ON이면 위/아래 시선엔 영향 안 받음.")]
        [SerializeField] private bool horizontalForwardOnly = true;
        [Tooltip("자석처럼 부드럽게 끌려오는 시간(초). 클수록 천천히. 0.4~0.8이 자연스러움.")]
        [SerializeField, Range(0.05f, 1.5f)] private float smoothTime = 0.6f;
        [Tooltip("최대 추적 속도(m/s). 빠르게 머리 돌릴 때 객체가 따라잡지 못하게 제한.")]
        [SerializeField] private float maxSpeed = 1.2f;

        private XRGrabInteractable _grab;
        private bool _followActive = true;
        private Vector3 _velocity;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _grab.selectEntered.AddListener(OnFirstGrab);
        }

        private void OnDestroy()
        {
            if (_grab != null) _grab.selectEntered.RemoveListener(OnFirstGrab);
        }

        private void OnFirstGrab(SelectEnterEventArgs args) => _followActive = false;

        private void LateUpdate()
        {
            if (!_followActive || hmd == null) return;

            Vector3 forward = horizontalForwardOnly
                ? Vector3.ProjectOnPlane(hmd.forward, Vector3.up).normalized
                : hmd.forward;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

            Vector3 target = hmd.position + forward * distanceFromHmd + Vector3.up * verticalOffset;
            // SmoothDamp = 자석처럼 가속·감속하며 따라옴 (Lerp보다 자연스러움)
            transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, smoothTime, maxSpeed);
        }
    }
}
