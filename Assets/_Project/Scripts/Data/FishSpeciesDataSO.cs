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
        [SerializeField] private int rarity = 1;

        [Header("Catch Data")]
        [SerializeField] private FloatRange sizeRangeCm = new FloatRange(10f, 30f);
        [FormerlySerializedAs("weightRange")]
        [SerializeField] private FloatRange weightRangeKg = new FloatRange(0.5f, 2f);
        [Min(0f)]
        [SerializeField] private float baseResistance = 1f;
        [SerializeField] private MovementPattern movementPattern = MovementPattern.Calm;
        [SerializeField] private FloatRange moveModeDurationRange = new FloatRange(1f, 2.5f);
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
        public string SpeciesName => fishId;
        public string DisplayName => displayName;
        public int Rarity => rarity;
        public FloatRange SizeRangeCm => sizeRangeCm;
        public FloatRange WeightRangeKg => weightRangeKg;
        public float BaseResistance => baseResistance;
        public MovementPattern MovementPattern => movementPattern;
        public FloatRange MoveModeDurationRange => moveModeDurationRange;
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

        public float GetRandomMoveModeDuration()
        {
            return moveModeDurationRange.GetRandom();
        }

        private void OnValidate()
        {
            rarity = Mathf.Max(1, rarity);
            baseResistance = Mathf.Max(0f, baseResistance);
            moveModeDurationRange.min = Mathf.Max(0.1f, moveModeDurationRange.min);
            moveModeDurationRange.max = Mathf.Max(moveModeDurationRange.min, moveModeDurationRange.max);
            minWaitTime = Mathf.Max(0f, minWaitTime);
            maxWaitTime = Mathf.Max(minWaitTime, maxWaitTime);
            baseScore = Mathf.Max(0, baseScore);
        }
    }
}
