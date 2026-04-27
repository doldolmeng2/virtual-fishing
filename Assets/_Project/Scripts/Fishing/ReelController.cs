using UnityEngine;

namespace VirtualFishing.Fishing
{
    /// <summary>
    /// 낚싯대의 릴(드럼+핸들)을 시각적으로 회전시키는 컴포넌트.
    /// FishingRodController.ReelingSpeed 값을 읽어 reelPivot Transform을 회전.
    /// 실제 게임 로직(찌 회수)은 FishingRodController가 담당.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("회전시킬 릴 피벗 (드럼+핸들이 같이 돌아감)")]
        [SerializeField] private Transform reelPivot;
        [Tooltip("Reel input source. 비어있으면 부모에서 자동 탐색.")]
        [SerializeField] private FishingRodController rodController;

        [Header("회전 설정")]
        [Tooltip("ReelingSpeed=1 일 때 초당 회전 각도(deg/s)")]
        [SerializeField] private float degreesPerSecondAtFullSpeed = 540f;
        [Tooltip("회전축 (로컬). 보통 X축 = 낚싯대 방향에 수직인 가로축.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.right;
        [Tooltip("미세한 떨림(idle 상태에서도 살짝 진동) — 0이면 비활성화")]
        [SerializeField] private float idleJitterDegrees = 0f;

        private float _accumulatedAngle;

        private void Reset()
        {
            // 같은 GameObject가 피벗인 경우 자동 할당
            if (reelPivot == null) reelPivot = transform;
        }

        private void Awake()
        {
            if (reelPivot == null) reelPivot = transform;
            if (rodController == null) rodController = GetComponentInParent<FishingRodController>();
        }

        private void Update()
        {
            if (reelPivot == null) return;

            float speed = rodController != null ? rodController.ReelingSpeed : 0f;
            float deltaAngle = speed * degreesPerSecondAtFullSpeed * Time.deltaTime;

            // idle 진동 (선택)
            if (Mathf.Approximately(speed, 0f) && idleJitterDegrees > 0f)
            {
                deltaAngle = Mathf.Sin(Time.time * 8f) * idleJitterDegrees * Time.deltaTime;
            }

            _accumulatedAngle += deltaAngle;
            reelPivot.localRotation = Quaternion.AngleAxis(_accumulatedAngle, rotationAxis.normalized);
        }
    }
}
