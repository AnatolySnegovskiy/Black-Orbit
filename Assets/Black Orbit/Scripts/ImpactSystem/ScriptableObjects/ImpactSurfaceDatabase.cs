using System.Collections.Generic;
using Black_Orbit.Scripts.ImpactSystem.Base;
using UnityEngine;

namespace Black_Orbit.Scripts.ImpactSystem.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ImpactSurfaceDatabase", menuName = "Black Orbit/Impact/Surface Database")]
    public class ImpactSurfaceDatabase : ScriptableObject
    {
        [Tooltip("Маппинг ID поверхности на эффекты и звуки")]
        [SerializeField] private List<SurfaceType> surfaceMappings = new List<SurfaceType>();

        [Tooltip("Поверхность по умолчанию, если ID не найден")]
        public SurfaceType defaultSurface;

        public SurfaceType GetSurfaceEntry(int id)
        {
            foreach (var entry in surfaceMappings)
            {
                if (entry.surfaceID == id)
                    return entry;
            }
            return defaultSurface;
        }

        public List<SurfaceType> GetAllEntries() => surfaceMappings;
    }
}
