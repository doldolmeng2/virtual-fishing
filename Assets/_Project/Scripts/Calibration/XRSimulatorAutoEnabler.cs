using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace VirtualFishing.Core
{
    /// <summary>
    /// Play 모드 진입 시 XR 기기 연결 여부를 감지해 XR Interaction Simulator를 자동 활성/비활성.
    ///
    /// 동작:
    ///   - XR 기기 미연결(에디터에서 Quest 없이 테스트) → simulatorRoot 활성화
    ///   - XR 기기 연결(PC Link/Air Link 등)              → simulatorRoot 비활성 유지
    ///   - 빌드                                            → 항상 비활성 (시뮬레이터는 에디터 전용)
    ///
    /// 주의: XR Origin (Minimal)의 구조·기능(위치 이동·시야 회전·원거리 상호작용)을
    ///       추가하지 않음. 시뮬레이터는 입력 레이어만 에뮬레이션.
    /// </summary>
    public class XRSimulatorAutoEnabler : MonoBehaviour
    {
        [Tooltip("XR 기기 미연결 시 활성화할 'XR Interaction Simulator' 루트 GameObject.\n" +
                 "기본값은 비활성(Inactive) 상태로 씬에 배치.")]
        [SerializeField] private GameObject simulatorRoot;

        private IEnumerator Start()
        {
            // XR 서브시스템 초기화 대기 (1프레임)
            yield return null;

#if UNITY_EDITOR
            bool xrActive = IsXRDeviceActive();
            simulatorRoot.SetActive(!xrActive);

            if (!xrActive)
                Debug.Log("[XRSimulator] XR 기기 미감지 → Simulator 활성화 (키보드/마우스로 테스트)");
            else
                Debug.Log("[XRSimulator] XR 기기 감지 → Simulator 비활성 유지 (실제 기기 사용)");
#else
            // 빌드에서는 시뮬레이터 비활성
            if (simulatorRoot != null)
                simulatorRoot.SetActive(false);
#endif
        }

        /// <summary>XR 디바이스가 초기화·활성 상태인지 확인.</summary>
        private static bool IsXRDeviceActive()
        {
            // XRSettings.isDeviceActive: OpenXR/OculusXR 로더가 초기화되어 있으면 true
            if (XRSettings.isDeviceActive)
                return true;

            // 추가 보호: 디바이스 목록이 비어 있으면 미연결로 판단
            var displays = new System.Collections.Generic.List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays)
            {
                if (d.running) return true;
            }

            return false;
        }
    }
}
