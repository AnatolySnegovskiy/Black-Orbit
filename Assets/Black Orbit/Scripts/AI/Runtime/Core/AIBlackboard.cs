using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Core
{
    /// <summary>
    /// Shared runtime state for AI decision making.
    /// Keeps perception signals and target reference in one place.
    /// </summary>
    public class AIBlackboard
    {
        /// <summary>
        /// Цель для атаки/прицеливания (Transform)
        /// </summary>
        public Transform AttackTarget { get; set; }

        /// <summary>
        /// Навигационная цель (точка), куда движемся при отсутствии LOS/для поиска
        /// </summary>
        public Vector3 NavTargetPos { get; set; } = Vector3.positiveInfinity;

        public Transform Target { get => AttackTarget; set => AttackTarget = value; }

        // Perception signals
        public bool HasLineOfSight { get; set; }
        public Vector3 LastSeenTargetPos { get; set; } = Vector3.positiveInfinity;
        public float TimeSinceLastSeen { get; set; } = float.MaxValue;

        // Подавление огнём: 0..1 (растёт при попаданиях/обстреле, падает со временем)
        public float SuppressionLevel { get; set; } = 0f;

        // Аудио-события (шум): позиция последнего шума и его сила (0..1)
        public Vector3 HeardNoisePos { get; set; } = Vector3.positiveInfinity;
        public float NoiseLevel { get; set; } = 0f;

    }
}
