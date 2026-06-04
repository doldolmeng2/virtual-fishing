using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VirtualFishing.Core.Fish
{
    public class FishSpawner : MonoBehaviour, IFishSpawner, IVoidEventListener
    {
        private const string DefaultFakeBiteResourceName = "OnFakeBite";
#if UNITY_EDITOR
        private const string DefaultFakeBiteAssetPath = "Assets/_Project/SO/Events/Resources/OnFakeBite.asset";
#endif

        [FormerlySerializedAs("currentSite")]
        [SerializeField] private FishingSiteDataSO siteData;
        [SerializeField] private MiniGameSettingsSO settings;
        [FormerlySerializedAs("fishController")]
        [SerializeField] private MonoBehaviour fishControllerRef;
        [SerializeField] private VoidEventSO onWarningBiteEvent;
        [SerializeField] private VoidEventSO onFakeBiteEvent;
        [SerializeField] private VoidEventSO onBiteOccurredEvent;
        [SerializeField] private FishSpeciesDataSO debugForcedSpecies;
        [SerializeField, Range(0f, 1f)] private float fakeBiteChance = 0.65f;
        [SerializeField, Range(0, 3)] private int maxFakeBiteCount = 2;
        [SerializeField] private float fakeBiteMinGap = 0.35f;
        [SerializeField] private float fakeBiteMaxGap = 0.9f;
        [Tooltip("찌 착수 이벤트 구독 → 착수 시 StartBiteTimer() 자동 호출")]
        [SerializeField] private VoidEventSO onWaterLandedEvent;

        private IFish fish;
        private Coroutine biteCoroutine;

        public event Action OnWarningBite;
        public event Action OnFakeBite;
        public event Action<FishSpeciesDataSO> OnBiteOccurred;

        private void Awake()
        {
            fish = fishControllerRef as IFish;
            TryAssignDefaultFakeBiteEvent();
        }

        private void OnEnable()  => onWaterLandedEvent?.Register(this);
        private void OnDisable() => onWaterLandedEvent?.Unregister(this);

        void IVoidEventListener.OnEventRaised() => StartBiteTimer();

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

        public void DebugForceBiteImmediately()
        {
            FishSpeciesDataSO species = debugForcedSpecies != null ? debugForcedSpecies : SelectFishSpecies();
            DebugForceBite(species);
        }

        public void DebugForceBite(FishSpeciesDataSO species)
        {
            if (species == null)
            {
                Debug.LogWarning("[FishSpawner] DebugForceBiteImmediately failed: no valid fish species found.");
                return;
            }

            CancelBite();
            fish?.Initialize(species);

            Debug.Log($"[FishSpawner] Debug force bite occurred immediately: species={species.DisplayName}");
            OnBiteOccurred?.Invoke(species);
            onBiteOccurredEvent?.Raise();
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

            yield return EmitFakeBitesBeforeMainBite();

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

        public void DebugForceFakeBite()
        {
            RaiseFakeBite();
        }

        private IEnumerator EmitFakeBitesBeforeMainBite()
        {
            int fakeBiteCount = PickFakeBiteCount();
            for (int i = 0; i < fakeBiteCount; i++)
            {
                float gap = UnityEngine.Random.Range(fakeBiteMinGap, fakeBiteMaxGap);
                yield return new WaitForSeconds(gap);
                RaiseFakeBite();
            }
        }

        private int PickFakeBiteCount()
        {
            if (maxFakeBiteCount <= 0 || UnityEngine.Random.value > fakeBiteChance)
            {
                return 0;
            }

            return UnityEngine.Random.Range(1, maxFakeBiteCount + 1);
        }

        private void RaiseFakeBite()
        {
            Debug.Log("[FishSpawner] Fake bite occurred.");
            OnFakeBite?.Invoke();
            onFakeBiteEvent?.Raise();
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

        private void OnValidate()
        {
            fakeBiteMinGap = Mathf.Max(0f, fakeBiteMinGap);
            fakeBiteMaxGap = Mathf.Max(fakeBiteMinGap, fakeBiteMaxGap);
            TryAssignDefaultFakeBiteEvent();
        }

        private void TryAssignDefaultFakeBiteEvent()
        {
            VoidEventSO defaultEvent = LoadDefaultFakeBiteEvent();
            if (defaultEvent != null)
            {
                onFakeBiteEvent = defaultEvent;
            }
        }

        private static VoidEventSO LoadDefaultFakeBiteEvent()
        {
#if UNITY_EDITOR
            VoidEventSO editorAsset = AssetDatabase.LoadAssetAtPath<VoidEventSO>(DefaultFakeBiteAssetPath);
            if (editorAsset != null)
            {
                return editorAsset;
            }
#endif

            return Resources.Load<VoidEventSO>(DefaultFakeBiteResourceName);
        }
    }
}
