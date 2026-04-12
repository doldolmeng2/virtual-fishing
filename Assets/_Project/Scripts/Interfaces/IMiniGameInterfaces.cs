using System;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.Interfaces
{
    public interface IMiniGame
    {
        float Difficulty { get; }
        float RemainingTime { get; }
        float SuccessGauge { get; }
        void StartMiniGame(FishCatchData fishData);
        void UpdateReeling(float reelingSpeed, Vector3 rodDirection);
        void EndMiniGame(bool success);
        event Action<bool> OnMiniGameEnded;
        event Action<float> OnSuccessGaugeChanged;
    }

    public interface ITensionCalculator
    {
        float CurrentTension { get; }
        TensionZone CurrentZone { get; }
        void Calculate(float fishResistance, float reelingSpeed, FishMoveState fishMoveState, Vector3 rodDirection);
        void Reset();
        event Action<float> OnTensionChanged;
        event Action<TensionZone> OnTensionZoneChanged;
    }
}
