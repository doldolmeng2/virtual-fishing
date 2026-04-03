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
}
