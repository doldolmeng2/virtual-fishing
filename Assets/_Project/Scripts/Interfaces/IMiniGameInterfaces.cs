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
        // 담당자 C(FishController)가 물고기 이동 상태 변경 시 호출
        void SetFishMoveState(FishMoveState fishMoveState);
        event Action<bool> OnMiniGameEnded;
        event Action<float> OnSuccessGaugeChanged;
    }

    public interface ITensionCalculator
    {
        float CurrentTension { get; }
        TensionZone CurrentZone { get; }
        void SetDifficulty(float difficulty);
        void Calculate(float fishResistance, float reelingSpeed, FishMoveState fishMoveState, Vector3 rodDirection);
        void Reset();
        event Action<float> OnTensionChanged;
        event Action<TensionZone> OnTensionZoneChanged;
    }
}
