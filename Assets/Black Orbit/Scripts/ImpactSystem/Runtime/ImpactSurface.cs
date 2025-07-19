using UnityEngine;

namespace Black_Orbit.Scripts.ImpactSystem.Runtime
{
    /// <summary>
    /// Component that provides surface ID based on a grayscale
    /// texture where pixel intensity defines surface type.
    /// 0 -> surfaceID 0, 255 -> surfaceID 255.
    /// </summary>
    public class ImpactSurface : MonoBehaviour
    {
        [Tooltip("Grayscale texture where value maps to surface ID")] 
        public Texture2D surfaceIdMap;

        [Tooltip("Surface ID to use when map is missing")] 
        [Range(0, 255)]
        public int defaultSurfaceId;

        /// <summary>
        /// Returns surface ID sampled from <see cref="surfaceIdMap"/>.
        /// </summary>
        /// <param name="uv">Texture coordinates from collision.</param>
        /// <returns>ID in range 0..255.</returns>
        public int GetSurfaceId(Vector2 uv)
        {
            if (surfaceIdMap == null)
                return defaultSurfaceId;

            Color c = surfaceIdMap.GetPixelBilinear(uv.x, uv.y);
            return Mathf.Clamp(Mathf.RoundToInt(c.grayscale * 255f), 0, 255);
        }
    }
}
