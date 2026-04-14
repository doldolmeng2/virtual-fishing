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

        [Header("Fish Spawn")]
        [SerializeField] private List<FishSpawnEntry> spawnFishListField = new();

        public string SiteId => siteId;
        public string DisplayName => displayName;
        public string SiteName => displayName;
        public string SceneName => sceneName;
        public BackgroundType BackgroundType => backgroundType;
        public AudioClip AmbientSound => ambientSound;
        public Material SkyboxMaterial => skyboxMaterial;
        public Sprite BackgroundImage => backgroundImage;
        public IReadOnlyList<FishSpawnEntry> SpawnFishList => spawnFishListField;
        public List<FishSpawnEntry> spawnFishList => spawnFishListField;

        private void OnValidate()
        {
            spawnFishListField ??= new List<FishSpawnEntry>();
        }
    }
}
