using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Data
{
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

            float random = Random.Range(0f, totalWeight);
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
}
