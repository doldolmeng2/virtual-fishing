using System;
using UnityEngine;

namespace VirtualFishing.Interfaces
{
    public interface ICalibrationService
    {
        void StartCalibration();
        void CaptureForwardGaze();
        void CaptureArmLength();
        float SittingHeight { get; }
        float ArmLength { get; }
        Vector3 CalibratedPosition { get; }
        event Action OnCalibrationComplete;
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
