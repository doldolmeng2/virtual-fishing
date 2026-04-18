using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VirtualFishing.Data
{
    [CreateAssetMenu(menuName = "VirtualFishing/Data/Fishing Site")]
    public class FishingSiteDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string siteId = "Site_New";
        [FormerlySerializedAs("siteName")]
        [SerializeField] private string displayName = "New Site";
        [SerializeField] private string sceneName = "Main_FishingSite";

        [Header("Environment")]
        [SerializeField] private BackgroundType backgroundType = BackgroundType.Pond;
        [SerializeField] private AudioClip ambientSound;
        [SerializeField] private Material skyboxMaterial;
        [FormerlySerializedAs("backgroundImage")]
        [SerializeField] private Sprite backgroundImage;
        [SerializeField] private GameObject environmentPrefab;

        [Header("Fish Spawn")]
        public List<FishSpawnEntry> spawnFishList = new();

        public string SiteId => siteId;
        public string DisplayName => displayName;
        public string SiteName => displayName;
        public string SceneName => sceneName;
        public BackgroundType BackgroundType => backgroundType;
        public AudioClip AmbientSound => ambientSound;
        public Material SkyboxMaterial => skyboxMaterial;
        public Sprite BackgroundImage => backgroundImage;
        public GameObject EnvironmentPrefab => environmentPrefab;
        public IReadOnlyList<FishSpawnEntry> SpawnFishList => spawnFishList;

        private void OnValidate()
        {
            spawnFishList ??= new List<FishSpawnEntry>();
        }
    }
}
