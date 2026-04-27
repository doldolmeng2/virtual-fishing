using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Fish Database")]
    public class FishDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<FishSpeciesDataSO> allSpecies = new();

        public IReadOnlyList<FishSpeciesDataSO> AllSpecies => allSpecies;

        public FishSpeciesDataSO GetRandomByRarity()
        {
            if (allSpecies == null || allSpecies.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var species in allSpecies)
            {
                if (species == null) continue;
                totalWeight += 1f / Mathf.Max(1, species.Rarity);
            }

            if (totalWeight <= 0f) return null;

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var species in allSpecies)
            {
                if (species == null) continue;

                cumulative += 1f / Mathf.Max(1, species.Rarity);
                if (random <= cumulative)
                    return species;
            }

            for (int i = allSpecies.Count - 1; i >= 0; i--)
            {
                if (allSpecies[i] != null)
                    return allSpecies[i];
            }

            return null;
        }
    }
}
