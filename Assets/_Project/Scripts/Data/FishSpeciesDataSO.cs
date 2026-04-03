using UnityEngine;

namespace VirtualFishing.Data
{
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
}
