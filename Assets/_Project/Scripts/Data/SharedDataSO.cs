using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Player Data")]
    public class PlayerDataSO : ScriptableObject
    {
        public float sittingHeight;
        public float armLength;
        public Vector3 currentPosition;
        public float safetyRadius;
    }

    [CreateAssetMenu(menuName = "VirtualFishing/Data/Account Data")]
    public class AccountDataSO : ScriptableObject
    {
        public string accountId;
        public string lastPlayedAt;
        public List<FishCatchRecord> encyclopedia = new();
        public int totalScore;
    }

    [CreateAssetMenu(menuName = "VirtualFishing/Data/Fish Species")]
    public class FishSpeciesDataSO : ScriptableObject
    {
        public string speciesName;
        public string displayName;
        public FloatRange weightRange;
        public float baseResistance;
        public MovementPattern movementPattern;
        [Range(1, 5)] public int rarity;
        public Sprite icon;
        public GameObject prefab;
    }

    [CreateAssetMenu(menuName = "VirtualFishing/Data/Fish Database")]
    public class FishDatabaseSO : ScriptableObject
    {
        public List<FishSpeciesDataSO> allSpecies = new();

        public FishSpeciesDataSO GetRandomByRarity()
        {
            if (allSpecies == null || allSpecies.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var species in allSpecies)
                totalWeight += 1f / species.rarity;

            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var species in allSpecies)
            {
                cumulative += 1f / species.rarity;
                if (random <= cumulative)
                    return species;
            }

            return allSpecies[^1];
        }
    }

    [CreateAssetMenu(menuName = "VirtualFishing/Data/Fishing Site")]
    public class FishingSiteDataSO : ScriptableObject
    {
        public string siteName;
        public BackgroundType backgroundType;
        public List<FishSpawnEntry> spawnFishList = new();
        public string sceneName;
        public Sprite backgroundImage;
        public AudioClip ambientSound;
    }

    [CreateAssetMenu(menuName = "VirtualFishing/Data/Tension Data")]
    public class TensionDataSO : ScriptableObject
    {
        [Range(0f, 100f)] public float currentTension;
        public float maxTension = 100f;
        public float safeZoneMin = 20f;
        public float safeZoneMax = 60f;
        public float dangerThreshold = 80f;

        public TensionZone GetCurrentZone()
        {
            if (currentTension < safeZoneMin) return TensionZone.Safe;
            if (currentTension < safeZoneMax) return TensionZone.Warning;
            if (currentTension < dangerThreshold) return TensionZone.Danger;
            return TensionZone.Critical;
        }

        public void ResetTension() => currentTension = 0f;
    }

    [CreateAssetMenu(menuName = "VirtualFishing/Data/Game Settings")]
    public class GameSettingsSO : ScriptableObject
    {
        [Header("캘리브레이션")]
        public float calibrationTimeout = 30f;

        [Header("낚시 - 캐스팅")]
        public float castingPowerMultiplier = 1f;
        public float castingZoneRadius = 0.5f;

        [Header("낚시 - 챔질")]
        public float hookTimingWindow = 3f;
        public float hookingZoneRadius = 0.4f;
        public Vector3 hookingZoneOffset = new Vector3(0f, 0.5f, 0f);
        public float hookingMinAcceleration = 1.5f;

        [Header("안전")]
        public float safetyCheckInterval = 0.2f;
        public float nearBoundaryDistance = 0.3f;
        public float emergencyTimeout = 10f;

        [Header("일반")]
        public float autoConfirmDelay = 10f;
        public float autoSaveInterval = 120f;
    }

    [CreateAssetMenu(menuName = "VirtualFishing/Data/MiniGame Settings")]
    public class MiniGameSettingsSO : ScriptableObject
    {
        public float baseDifficulty = 1f;
        public float baseTimeLimit = 60f;
        public float tensionIncreaseRate = 5f;
        public float gaugeIncreaseRate = 10f;
        public float tensionDecreaseRate = 3f;
    }
}
