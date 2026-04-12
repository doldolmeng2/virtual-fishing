using System;
using System.Collections;
using UnityEngine;
using VirtualFishing.Core.Events;
using VirtualFishing.Data;
using VirtualFishing.Interfaces;

namespace VirtualFishing.Fish
{
    public class FishSpawner : MonoBehaviour, IFishSpawner
    {
        [SerializeField] private FishingSiteDataSO siteData;
        [SerializeField] private MiniGameSettingsSO settings;
        [SerializeField] private MonoBehaviour fishControllerRef; // IFish를 구현한 FishController 연결
        [SerializeField] private VoidEventSO onWarningBiteEvent;
        [SerializeField] private VoidEventSO onBiteOccurredEvent;

        private IFish _fish;
        private Coroutine _biteCoroutine;

        public event Action OnWarningBite;
        public event Action<FishSpeciesDataSO> OnBiteOccurred;

        private void Awake()
        {
            _fish = fishControllerRef as IFish;
        }

        /// <summary>
        /// 낚시 대기 상태 진입 시 호출. 예고/본 입질 타이머를 시작한다.
        /// </summary>
        public void StartBiteTimer()
        {
            if (_biteCoroutine != null)
                StopCoroutine(_biteCoroutine);
            _biteCoroutine = StartCoroutine(BiteRoutine());
        }

        public void CancelBite()
        {
            if (_biteCoroutine == null) return;
            StopCoroutine(_biteCoroutine);
            _biteCoroutine = null;
        }

        private IEnumerator BiteRoutine()
        {
            // 본 입질 대기 시간: biteMinTime ~ biteMaxTime
            float biteTime = UnityEngine.Random.Range(settings.biteMinTime, settings.biteMaxTime);

            // 예고 입질 대기 시간: 본 입질 범위의 절반 구간
            float warningTime = UnityEngine.Random.Range(settings.biteMinTime / 2f, settings.biteMaxTime / 2f);

            // 최소 간격 보장: 예고와 본 입질 사이 biteGapMinTime 이상 확보
            if (biteTime - warningTime < settings.biteGapMinTime)
                warningTime = biteTime - settings.biteGapMinTime;

            warningTime = Mathf.Max(0f, warningTime);

            // 예고 입질
            yield return new WaitForSeconds(warningTime);
            OnWarningBite?.Invoke();
            onWarningBiteEvent?.Raise();

            // 본 입질 (나머지 시간 대기)
            yield return new WaitForSeconds(biteTime - warningTime);

            FishSpeciesDataSO species = SelectFishSpecies();
            if (species == null)
            {
                _biteCoroutine = null;
                yield break;
            }

            // 물고기 데이터 초기화 (담당자 C가 구현한 FishController)
            _fish?.Initialize(species);

            OnBiteOccurred?.Invoke(species);
            onBiteOccurredEvent?.Raise();

            _biteCoroutine = null;
        }

        /// <summary>
        /// spawnFishList의 확률에 따라 어종을 선택한다.
        /// </summary>
        private FishSpeciesDataSO SelectFishSpecies()
        {
            if (siteData == null || siteData.spawnFishList == null || siteData.spawnFishList.Count == 0)
                return null;

            float total = 0f;
            foreach (var entry in siteData.spawnFishList)
                total += entry.spawnProbability;

            float roll = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;

            foreach (var entry in siteData.spawnFishList)
            {
                cumulative += entry.spawnProbability;
                if (roll <= cumulative)
                    return entry.speciesData;
            }

            // 부동소수점 오차 방어
            return siteData.spawnFishList[siteData.spawnFishList.Count - 1].speciesData;
        }
    }
}
