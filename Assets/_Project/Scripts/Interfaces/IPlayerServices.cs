using System;
using UnityEngine;

namespace VirtualFishing.Interfaces
{
    /// <summary>
    /// 시니어 친화 "스니키 캘리브레이션" 인터페이스.
    /// 별도 측정 단계 없이 첫 그랩 시점에 자동으로 자세 정렬 + 신체 데이터 측정.
    /// </summary>
    public interface ICalibrationService
    {
        /// <summary>
        /// 낚싯대 첫 그랩 시 호출. XR Origin을 Target Pose로 정렬하고
        /// HMD↔손 거리로 팔 길이를 자연 측정한 뒤 OnCalibrationComplete 이벤트를 발행.
        /// </summary>
        void CalibrateFromGrab(Transform handTransform);

        /// <summary>
        /// 메뉴 버튼 등으로 수동 재캘리브레이션이 필요할 때 호출.
        /// </summary>
        void ResetCalibration();

        /// <summary>1회 캘리브레이션이 완료되었는지 여부.</summary>
        bool IsCalibrated { get; }

        // [Deprecated — 양팔 뻗기 강제 측정 방식. 시니어 UX 개선으로 제거됨]
        [Obsolete("스니키 캘리브레이션으로 대체됨. CalibrateFromGrab을 사용하세요.")]
        void StartCalibration() { }

        [Obsolete("스니키 캘리브레이션으로 대체됨. CalibrateFromGrab을 사용하세요.")]
        void CaptureForwardGaze() { }

        [Obsolete("스니키 캘리브레이션으로 대체됨. 팔 길이는 첫 그랩 시 자동 측정됨.")]
        void CaptureArmLength() { }
    }

    public interface ISafetyMonitor
    {
        void StartMonitoring();
        void StopMonitoring();
        float DistanceFromCenter { get; }
        SafetyWarningLevel CurrentWarningLevel { get; }
        event Action<SafetyWarningLevel> OnWarningLevelChanged;
    }
}
