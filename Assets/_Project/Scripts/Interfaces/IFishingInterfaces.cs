using System;
using UnityEngine;

namespace VirtualFishing.Interfaces
{
    public interface IGrabbable
    {
        bool IsGrabbed { get; }
        void OnGrab(Transform hand);
        void OnRelease();
        event Action OnGrabbed;
        event Action OnReleased;
    }

    public interface IFishingRod
    {
        RodState CurrentState { get; }
        float Acceleration { get; }
        Vector3 Direction { get; }
        float ReelingSpeed { get; }
        bool IsInCastingZone { get; }
        bool IsInHookingZone { get; }
        void Attach(Transform hand);
        void Detach();

        // [내부 컨트롤러 연결용] — 설계 문서에는 없으며,
        // XR/입력 어댑터에서 컨트롤러 속도/회전 입력을 본 컨트롤러로 전달하기 위한 메서드.
        // 외부 도메인(Fish/MiniGame/Feedback)에서는 호출하지 말 것.
        void UpdateCastingInput(Vector3 controllerVelocity, Vector3 controllerDirection);
        void UpdateReelingInput(float rotationDelta);

        event Action<RodState> OnRodStateChanged;
    }

    public interface ICastable
    {
        void Cast(float power, Vector3 direction);
        bool IsCasting { get; }
        event Action OnCastComplete;
    }

    public interface IFishingFloat
    {
        Vector3 Position { get; }
        float SinkingDepth { get; }
        float Velocity { get; }
        void Launch(float speed, Vector3 direction);
        void OnWaterContact();
        event Action OnWaterLanded;
    }
}
