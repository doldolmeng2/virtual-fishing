using System;
using UnityEngine;

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
        public FishSpeciesDataSO speciesData;
        [Range(0f, 1f)] public float spawnProbability;
        public float minWaitTime;
        public float maxWaitTime;
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
