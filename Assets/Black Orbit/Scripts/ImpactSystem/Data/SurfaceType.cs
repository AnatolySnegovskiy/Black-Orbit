using UnityEngine;

namespace Black_Orbit.Scripts.ImpactSystem.Base
{
    [System.Serializable]
    public class SurfaceType
    {
        [Tooltip("ID поверхности (0 - 255)")]
        [Range(0, 255)]
        public int surfaceID;

        [Tooltip("Название для понимания")]
        public string surfaceName;

        [Tooltip("Префаб партиклов попадания")]
        public GameObject impactEffectPrefab;

        [Tooltip("Префаб декали")]
        public GameObject decalPrefab;

        [Tooltip("Звук попадания")]
        public AudioClip impactSound;

        [Tooltip("Множитель масштаба эффекта")]
        public float impactScaleMultiplier = 1f;
    }
}
