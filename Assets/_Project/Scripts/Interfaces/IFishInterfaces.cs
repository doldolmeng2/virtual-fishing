using System;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.Interfaces
{
    public interface IFish
    {
        string SpeciesName { get; }
        float Weight { get; }
        float Resistance { get; }
        MovementPattern Pattern { get; }
        void Initialize(FishSpeciesDataSO speciesData);
        void ExecuteMovement();
        event Action<Vector3> OnFishMoved;
    }

    public interface IFishSpawner
    {
        void StartBiteTimer();
        void CancelBite();
        event Action OnWarningBite;                    // 예고 입질
        event Action<FishSpeciesDataSO> OnBiteOccurred; // 본 입질
    }
}
