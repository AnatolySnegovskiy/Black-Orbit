using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Core
{
    [CreateAssetMenu(menuName = "AI/Settings", fileName = "AISettings")]
    public class AISettings : ScriptableObject
    {
        [Header("Gizmos Defaults")] 
        public bool showGizmosVision = true;
        public bool showGizmosDetection = true;
        public bool showGizmosAttackRanges = true;
        public bool showGizmosLastSeen = true;
        public bool showGizmosNavTarget = true;
        public bool showGizmosInfoLabel = true;
    }
}
