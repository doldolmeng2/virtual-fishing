using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace VirtualFishing.Data
{
    [Serializable]
    public struct FishCatchData
    {
        public FishSpeciesDataSO species;
        public float weight;
        public string caughtAt;
        public BackgroundType siteType;
    }

    [Serializable]
    public class FishCatchRecord
    {
        public string speciesName;
        public float weight;
        public string caughtAt;
        public string siteName;
        public int score;
    }

    [Serializable]
    public class FishSpawnEntry
    {
        [SerializeField] private FishSpeciesDataSO speciesData;
        [FormerlySerializedAs("spawnProbability")]
        [Min(0f)]
        [SerializeField] private float spawnWeight = 1f;

        public FishSpeciesDataSO SpeciesData => speciesData;
        public float SpawnWeight => spawnWeight;
        public bool IsValid => speciesData != null && spawnWeight > 0f;
        public FishSpeciesDataSO speciesData => this.speciesData;
        public float spawnProbability => spawnWeight;
    }

    [Serializable]
    public struct FloatRange
    {
        public float min;
        public float max;

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public float GetRandom() => UnityEngine.Random.Range(min, max);
    }
}
