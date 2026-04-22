using UnityEngine;
using UnityEngine.Serialization;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Fish Species")]
    public class FishSpeciesDataSO : ScriptableObject
    {
        [Header("Identity")]
        [FormerlySerializedAs("speciesName")]
        [SerializeField] private string fishId = "Fish_New";
        [SerializeField] private string displayName = "New Fish";
        [Range(1, 5)]
        [FormerlySerializedAs("rarity")]
        [SerializeField] private int rarityValue = 1;

        [Header("Catch Data")]
        [SerializeField] private FloatRange sizeRangeCm = new FloatRange(10f, 30f);
        [FormerlySerializedAs("weightRange")]
        [SerializeField] private FloatRange weightRangeKg = new FloatRange(0.5f, 2f);
        [FormerlySerializedAs("baseResistance")]
        [Min(0f)]
        [SerializeField] private float baseResistanceValue = 1f;
        [SerializeField] private MovementPattern movementPattern = MovementPattern.Calm;
        [Min(0f)]
        [SerializeField] private float minWaitTime = 2f;
        [Min(0f)]
        [SerializeField] private float maxWaitTime = 5f;
        [Min(0)]
        [SerializeField] private int baseScore = 10;

        [Header("Presentation")]
        [FormerlySerializedAs("prefab")]
        [SerializeField] private GameObject fishPrefab;
        [SerializeField] private Sprite icon;
        [TextArea]
        [SerializeField] private string description;

        public string FishId => fishId;
        public string DisplayName => displayName;
        public int Rarity => rarityValue;
        public FloatRange SizeRangeCm => sizeRangeCm;
        public FloatRange WeightRangeKg => weightRangeKg;
        public float BaseResistance => baseResistanceValue;
        public MovementPattern MovementPattern => movementPattern;
        public float MinWaitTime => minWaitTime;
        public float MaxWaitTime => maxWaitTime;
        public int BaseScore => baseScore;
        public GameObject FishPrefab => fishPrefab;
        public GameObject Prefab => fishPrefab;
        public Sprite Icon => icon;
        public string Description => description;

        public float GetRandomWeightKg()
        {
            return weightRangeKg.GetRandom();
        }

        public float GetRandomSizeCm()
        {
            return sizeRangeCm.GetRandom();
        }

        public float GetRandomWaitTime()
        {
            return Random.Range(minWaitTime, maxWaitTime);
        }

        private void OnValidate()
        {
            rarityValue = Mathf.Max(1, rarityValue);
            baseResistanceValue = Mathf.Max(0f, baseResistanceValue);
            minWaitTime = Mathf.Max(0f, minWaitTime);
            maxWaitTime = Mathf.Max(minWaitTime, maxWaitTime);
            baseScore = Mathf.Max(0, baseScore);
        }
    }
}
