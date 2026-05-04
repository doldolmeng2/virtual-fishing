using UnityEngine;

namespace VirtualFishing.Core.Fish
{
    [CreateAssetMenu(menuName = "VirtualFishing/Fish/Dev Fish Environment Layout")]
    public class DevFishEnvironmentLayoutSO : ScriptableObject
    {
        [Header("Grass")]
        [SerializeField] private int sideGrassCount = 190;
        [SerializeField] private int rearGrassCount = 170;
        [SerializeField] private float sideGrassStartZ = 24f;
        [SerializeField] private float rearGrassZ = 38f;
        [SerializeField] private float sideGrassMinX = 34f;
        [SerializeField] private float rearGrassHalfWidth = 50f;
        [SerializeField] private Vector2 sideGrassScaleRange = new(0.42f, 0.58f);
        [SerializeField] private Vector2 rearGrassScaleRange = new(0.38f, 0.52f);
        [SerializeField] private int aquaticGrassCount = 150;
        [SerializeField] private float aquaticGrassZ = 13.5f;
        [SerializeField] private float aquaticGrassHalfWidth = 43f;
        [SerializeField] private Vector2 aquaticGrassScaleRange = new(0.42f, 0.56f);

        [Header("Trees")]
        [SerializeField] private Vector2 sideTreeXRange = new(48f, 57.6f);
        [SerializeField] private Vector2 sideTreeZRange = new(4f, 43f);
        [SerializeField] private Vector2 sideTreeScaleRange = new(0.8f, 1.13f);

        [Header("Fallback Grass Blade")]
        [SerializeField] private Vector3 bladeScale = new(0.16f, 1.18f, 0.16f);
        [SerializeField] private float bladeBaseHeight = 0.58f;
        [SerializeField] private float bladeHeightStep = 0.045f;

        public int SideGrassCount => Mathf.Max(0, sideGrassCount);
        public int RearGrassCount => Mathf.Max(0, rearGrassCount);
        public float SideGrassStartZ => sideGrassStartZ;
        public float RearGrassZ => rearGrassZ;
        public float SideGrassMinX => sideGrassMinX;
        public float RearGrassHalfWidth => rearGrassHalfWidth;
        public Vector2 SideGrassScaleRange => sideGrassScaleRange;
        public Vector2 RearGrassScaleRange => rearGrassScaleRange;
        public int AquaticGrassCount => Mathf.Max(0, aquaticGrassCount);
        public float AquaticGrassZ => aquaticGrassZ;
        public float AquaticGrassHalfWidth => aquaticGrassHalfWidth;
        public Vector2 AquaticGrassScaleRange => aquaticGrassScaleRange;
        public Vector2 SideTreeXRange => sideTreeXRange;
        public Vector2 SideTreeZRange => sideTreeZRange;
        public Vector2 SideTreeScaleRange => sideTreeScaleRange;
        public Vector3 BladeScale => bladeScale;
        public float BladeBaseHeight => bladeBaseHeight;
        public float BladeHeightStep => bladeHeightStep;
    }
}
