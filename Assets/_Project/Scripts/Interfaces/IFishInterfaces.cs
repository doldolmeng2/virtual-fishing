using System;
using UnityEngine;
using VirtualFishing.Data;

namespace VirtualFishing.Interfaces
{
    public interface IFish
    {
        FishSpeciesDataSO CurrentSpecies { get; }
        string SpeciesName { get; }
        float Weight { get; }
        float Resistance { get; }
        MovementPattern Pattern { get; }
        FishMoveMode CurrentMoveMode { get; }
        void Initialize(FishSpeciesDataSO speciesData);
        void ResetFish();
        void ExecuteMovement();
        void StartRandomMovementModeLoop();
        void StopRandomMovementModeLoop();
        event Action<Vector3> OnFishMoved;
    }

    public interface IFishSpawner
    {
        void StartBiteTimer();
        void CancelBite();
        event Action<FishSpeciesDataSO> BiteOccurred;
    }
}
