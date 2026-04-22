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
        [SerializeField] private FishSpeciesDataSO speciesDataField;
        [FormerlySerializedAs("spawnProbability")]
        [Min(0f)]
        [SerializeField] private float spawnWeightField = 1f;

        public FishSpeciesDataSO SpeciesData => speciesDataField;
        public float SpawnWeight => spawnWeightField;
        public bool IsValid => speciesDataField != null && spawnWeightField > 0f;
        public FishSpeciesDataSO speciesData => speciesDataField;
        public float spawnProbability => spawnWeightField;
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
