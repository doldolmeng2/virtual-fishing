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
        [SerializeField] private FishingSiteDataSO siteData;
        [SerializeField] private MiniGameSettingsSO settings;
        [SerializeField] private MonoBehaviour fishControllerRef;
        [SerializeField] private VoidEventSO onWarningBiteEvent;
        [SerializeField] private VoidEventSO onBiteOccurredEvent;

        private IFish fish;
        private Coroutine biteCoroutine;

        public event Action OnWarningBite;
        public event Action<FishSpeciesDataSO> OnBiteOccurred;

        private void Awake()
        {
            fish = fishControllerRef as IFish;
        }

        public void StartBiteTimer()
        {
            if (biteCoroutine != null)
            {
                StopCoroutine(biteCoroutine);
            }

            biteCoroutine = StartCoroutine(BiteRoutine());
        }

        public void CancelBite()
        {
            if (biteCoroutine == null)
            {
                return;
            }

            StopCoroutine(biteCoroutine);
            biteCoroutine = null;
        }

        private IEnumerator BiteRoutine()
        {
            FishSpeciesDataSO species = SelectFishSpecies();
            if (species == null)
            {
                Debug.LogWarning("[FishSpawner] Bite routine aborted: no valid fish species found.");
                biteCoroutine = null;
                yield break;
            }

            float waitTime = species.GetRandomWaitTime();
            yield return new WaitForSeconds(waitTime);

            Debug.Log($"[FishSpawner] Warning bite occurred: species={species.DisplayName}, wait={waitTime:F2}s");
            OnWarningBite?.Invoke();
            onWarningBiteEvent?.Raise();

            float mainBiteDelay = settings != null
                ? Mathf.Max(settings.biteGapMinTime, UnityEngine.Random.Range(0.5f, 1.5f))
                : UnityEngine.Random.Range(0.5f, 1.5f);

            yield return new WaitForSeconds(mainBiteDelay);

            fish?.Initialize(species);

            Debug.Log($"[FishSpawner] Main bite occurred: species={species.DisplayName}, mainDelay={mainBiteDelay:F2}s");
            OnBiteOccurred?.Invoke(species);
            onBiteOccurredEvent?.Raise();

            biteCoroutine = null;
        }

        private FishSpeciesDataSO SelectFishSpecies()
        {
            if (siteData == null || siteData.spawnFishList == null || siteData.spawnFishList.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            foreach (FishSpawnEntry entry in siteData.spawnFishList)
            {
                if (entry != null && entry.IsValid)
                {
                    totalWeight += entry.spawnProbability;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (FishSpawnEntry entry in siteData.spawnFishList)
            {
                if (entry == null || !entry.IsValid)
                {
                    continue;
                }

                cumulative += entry.spawnProbability;
                if (roll <= cumulative)
                {
                    return entry.speciesData;
                }
            }

            for (int i = siteData.spawnFishList.Count - 1; i >= 0; i--)
            {
                FishSpawnEntry entry = siteData.spawnFishList[i];
                if (entry != null && entry.IsValid)
                {
                    return entry.speciesData;
                }
            }

            return null;
        }
    }
}
