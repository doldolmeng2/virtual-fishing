using System;
using System.Collections;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Core.Fish
{
    public class FishSpawner : MonoBehaviour, IFishSpawner
    {
        [Header("References")]
        [SerializeField] private FishingSiteDataSO currentSite;
        [SerializeField] private FishController fishController;

        [Header("Optional Event Hook")]
        [SerializeField] private VoidEventSO onBiteOccurredEvent;

        [Header("Debug Settings")]
        [SerializeField] private bool autoCancelPreviousCycle = true;
        [SerializeField] private Vector2 mainBiteDelayRange = new(0.5f, 1.5f);

        private Coroutine biteRoutine;

        public FishingSiteDataSO CurrentSite => currentSite;
        public event Action<FishSpeciesDataSO> BiteOccurred;

        public void StartBiteTimer()
        {
            StartBiteCycle();
        }

        public void StartBiteCycle()
        {
            if (!CanStartCycle())
            {
                return;
            }

            if (biteRoutine != null)
            {
                if (!autoCancelPreviousCycle)
                {
                    Debug.LogWarning("[FishSpawner] Bite cycle is already running.");
                    return;
                }

                CancelBite();
            }

            biteRoutine = StartCoroutine(BiteRoutine());
        }

        public void CancelBite()
        {
            if (biteRoutine == null)
            {
                return;
            }

            StopCoroutine(biteRoutine);
            biteRoutine = null;
            Debug.Log("[FishSpawner] Bite cycle cancelled.");
        }

        private bool CanStartCycle()
        {
            if (currentSite == null)
            {
                Debug.LogWarning("[FishSpawner] StartBiteTimer failed: currentSite is not assigned.");
                return false;
            }

            if (fishController == null)
            {
                Debug.LogWarning("[FishSpawner] StartBiteTimer failed: fishController is not assigned.");
                return false;
            }

            if (currentSite.SpawnFishList == null || currentSite.SpawnFishList.Count == 0)
            {
                Debug.LogWarning("[FishSpawner] StartBiteTimer failed: currentSite spawnFishList is empty.");
                return false;
            }

            return true;
        }

        private IEnumerator BiteRoutine()
        {
            FishSpeciesDataSO selectedSpecies = SelectFishByWeight();
            if (selectedSpecies == null)
            {
                biteRoutine = null;
                yield break;
            }

            float waitTime = selectedSpecies.GetRandomWaitTime();
            Debug.Log(
                $"[FishSpawner] Selected fish: id={selectedSpecies.FishId}, name={selectedSpecies.DisplayName}, " +
                $"wait={waitTime:F2}s, pattern={selectedSpecies.MovementPattern}, resistance={selectedSpecies.BaseResistance:F2}");

            yield return new WaitForSeconds(waitTime);

            Debug.Log($"[FishSpawner] Preview bite occurred for {selectedSpecies.DisplayName}. TODO: ripple/light haptic hook point.");

            float mainBiteDelay = UnityEngine.Random.Range(mainBiteDelayRange.x, mainBiteDelayRange.y);
            yield return new WaitForSeconds(mainBiteDelay);

            Debug.Log($"[FishSpawner] Main bite occurred after preview delay {mainBiteDelay:F2}s.");

            fishController.Initialize(selectedSpecies);

            // TODO: Connect FloatController sink animation / FeedbackManager sound-haptic here.
            // TODO: Connect GameFlowManager or FishingRodController through SO event listeners when integration starts.
            BiteOccurred?.Invoke(selectedSpecies);
            onBiteOccurredEvent?.Raise();

            biteRoutine = null;
        }

        private FishSpeciesDataSO SelectFishByWeight()
        {
            float totalWeight = 0f;

            foreach (FishSpawnEntry entry in currentSite.SpawnFishList)
            {
                if (entry != null && entry.IsValid)
                {
                    totalWeight += entry.SpawnWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                Debug.LogWarning("[FishSpawner] Failed to select fish: no valid spawn entry with positive spawnWeight.");
                return null;
            }

            float randomPoint = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (FishSpawnEntry entry in currentSite.SpawnFishList)
            {
                if (entry == null || !entry.IsValid)
                {
                    continue;
                }

                cumulativeWeight += entry.SpawnWeight;
                if (randomPoint <= cumulativeWeight)
                {
                    return entry.SpeciesData;
                }
            }

            FishSpawnEntry fallbackEntry = null;
            for (int i = currentSite.SpawnFishList.Count - 1; i >= 0; i--)
            {
                FishSpawnEntry entry = currentSite.SpawnFishList[i];
                if (entry != null && entry.IsValid)
                {
                    fallbackEntry = entry;
                    break;
                }
            }

            return fallbackEntry?.SpeciesData;
        }

        private void OnValidate()
        {
            if (fishController == null)
            {
                fishController = GetComponent<FishController>();
            }

            if (mainBiteDelayRange.y < mainBiteDelayRange.x)
            {
                mainBiteDelayRange.y = mainBiteDelayRange.x;
            }
        }
    }
}
