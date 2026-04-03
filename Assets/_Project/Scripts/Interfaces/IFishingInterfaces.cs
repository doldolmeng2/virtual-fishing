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
        void Sink(float depth);
        event Action OnWaterLanded;
    }
}
