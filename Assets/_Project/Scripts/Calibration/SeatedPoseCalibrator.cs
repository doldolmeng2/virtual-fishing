using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Calibration
{
    /// <summary>
    /// 시니어 친화 "스니키 캘리브레이션".
    ///
    /// 전제: 타겟 유저가 노년층이라 양팔 뻗기·트리거 입력 같은 명시적 측정 단계는
    /// 신체적·인지적 부담을 줘서 진입 장벽이 됨.
    ///
    /// 동작:
    ///   1) 그랩 가능한 객체를 처음 잡는 순간 자동 발동 (XRGrabInteractable.selectEntered 구독)
    ///   2) XR Origin을 Target Eye Anchor 위치/방향으로 즉시(Snap) 정렬
    ///      - Yaw(요)만 회전 정렬 — Pitch/Roll은 사용자 자세 보존
    ///      - XZ만 위치 정렬 — Y(앉은키)는 보존해 멀미 방지
    ///   3) HMD↔손 거리로 팔 길이를 자연 측정
    ///   4) PlayerDataSO에 측정값 저장
    ///   5) OnCalibrationComplete 이벤트 발행 → GameFlowManager가 FishingReady로 전이
    ///
    /// 일반화: 트리거가 되는 그랩 객체는 낚싯대에 한정되지 않음. 큐브·구 등 어떤 모델이든
    /// XRGrabInteractable을 가진 객체면 그레이박싱 단계에서 사용 가능.
    /// </summary>
    public class SeatedPoseCalibrator : MonoBehaviour, ICalibrationService
    {
        [Header("XR 참조")]
        [SerializeField] private XROrigin xrOrigin;
        [Tooltip("플레이어 머리가 위치해야 할 이상적 시야점. 정면 방향(forward)도 사용됨.")]
        [SerializeField] private Transform targetEyeAnchor;

        [Header("데이터")]
        [SerializeField] private PlayerDataSO playerData;
        [Tooltip("팔 길이 × 이 값 = 안전 반경. 시니어 권장 1.3 (넉넉)")]
        [SerializeField, Range(1.0f, 2.0f)] private float safetyRadiusMultiplier = 1.3f;

        [Header("이벤트")]
        [Tooltip("정렬 + 측정 완료 시 발행. GameFlowManager가 수신해 FishingReady로 전이.")]
        [SerializeField] private VoidEventSO onCalibrationComplete;

        [Header("그랩 트리거")]
        [Tooltip("이 객체들 중 하나라도 첫 그랩 시 캘리브레이션 자동 발동. " +
                 "비워두면 autoFindAllGrabbables 옵션 동작.")]
        [SerializeField] private List<XRGrabInteractable> grabTriggers = new();
        [Tooltip("ON이면 grabTriggers가 비었을 때 씬의 모든 XRGrabInteractable을 자동 트리거로 등록 " +
                 "(그레이박싱 편의용). 통합 시엔 명시적으로 채우는 게 안전.")]
        [SerializeField] private bool autoFindAllGrabbables = true;

        [Header("디버그")]
        [SerializeField] private bool verboseLog = true;

        public bool IsCalibrated { get; private set; }

        private readonly List<XRGrabInteractable> _subscribed = new();

        private void Awake()
        {
            // 명시적 트리거가 비어 있고 자동 검색이 켜져 있으면 씬 전체 자동 등록
            if (grabTriggers.Count == 0 && autoFindAllGrabbables)
            {
                var all = FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
                grabTriggers.AddRange(all);
                if (verboseLog)
                    Debug.Log($"[Calibration] 그랩 트리거 자동 등록: {all.Length}개");
            }

            foreach (var grab in grabTriggers)
            {
                if (grab == null) continue;
                grab.selectEntered.AddListener(OnGrabTriggered);
                _subscribed.Add(grab);
            }
        }

        private void OnDestroy()
        {
            foreach (var grab in _subscribed)
            {
                if (grab == null) continue;
                grab.selectEntered.RemoveListener(OnGrabTriggered);
            }
            _subscribed.Clear();
        }

        private void OnGrabTriggered(SelectEnterEventArgs args)
        {
            if (IsCalibrated) return;
            var hand = args.interactorObject?.transform;
            if (hand != null) CalibrateFromGrab(hand);
        }

        /// <summary>
        /// 외부에서 직접 호출 가능 (인터페이스 일관성).
        /// 내부 selectEntered 핸들러도 이걸 호출.
        /// 1회성 — IsCalibrated == true면 무시(ResetCalibration으로 리셋 가능).
        /// </summary>
        public void CalibrateFromGrab(Transform handTransform)
        {
            if (IsCalibrated) return;
            if (!ValidateReferences(handTransform)) return;

            Transform hmd = xrOrigin.Camera.transform;

            // 1) Yaw 회전 정렬 (Snap — VR 멀미 방지를 위해 Lerp 금지)
            float yaw = Vector3.SignedAngle(
                ProjectOnXZ(hmd.forward),
                ProjectOnXZ(targetEyeAnchor.forward),
                Vector3.up);
            xrOrigin.transform.RotateAround(hmd.position, Vector3.up, yaw);

            // 2) XZ 위치 정렬 (Y는 보존해 사용자 실제 앉은키 유지)
            Vector3 offset = targetEyeAnchor.position - hmd.position;
            offset.y = 0f;
            xrOrigin.transform.position += offset;

            // 3) 신체 데이터 스니키 측정 (HMD↔손 거리)
            float reach = Vector3.Distance(hmd.position, handTransform.position);
            if (playerData != null)
            {
                playerData.armLength = reach;
                playerData.safetyRadius = reach * safetyRadiusMultiplier;
                playerData.sittingHeight = hmd.position.y;        // 메타데이터 기록 (게임플레이 미사용)
                playerData.currentPosition = new Vector3(hmd.position.x, 0f, hmd.position.z);
            }

            IsCalibrated = true;

            if (verboseLog)
            {
                Debug.Log($"[Calibration] 스니키 정렬 완료. yaw={yaw:F1}° offsetXZ=({offset.x:F2},{offset.z:F2}) " +
                          $"armLength={reach:F2}m safetyRadius={reach * safetyRadiusMultiplier:F2}m");
            }

            // 4) 완료 이벤트 발행
            if (onCalibrationComplete != null) onCalibrationComplete.Raise();
        }

        /// <summary>메뉴 버튼 등 수동 재캘리브레이션 트리거용.</summary>
        public void ResetCalibration()
        {
            IsCalibrated = false;
            if (verboseLog) Debug.Log("[Calibration] 재캘리브레이션 무장 — 다음 그랩 시 정렬 재실행");
        }

        private bool ValidateReferences(Transform handTransform)
        {
            if (xrOrigin == null) { Debug.LogError("[Calibration] xrOrigin 미할당"); return false; }
            if (xrOrigin.Camera == null) { Debug.LogError("[Calibration] XROrigin.Camera 미설정"); return false; }
            if (targetEyeAnchor == null) { Debug.LogError("[Calibration] targetEyeAnchor 미할당"); return false; }
            if (handTransform == null) { Debug.LogError("[Calibration] handTransform == null"); return false; }
            return true;
        }

        private static Vector3 ProjectOnXZ(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (targetEyeAnchor == null) return;
            // 목표 시야점 표시
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.7f);
            Gizmos.DrawWireSphere(targetEyeAnchor.position, 0.08f);
            Gizmos.DrawLine(targetEyeAnchor.position, targetEyeAnchor.position + targetEyeAnchor.forward * 0.3f);
        }
    }
}
