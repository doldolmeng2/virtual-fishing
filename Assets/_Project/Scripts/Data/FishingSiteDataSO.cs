using System.Collections.Generic;
using UnityEngine;

namespace VirtualFishing.Data
{
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
}
