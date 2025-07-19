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

        [Tooltip("Name of the texture property used on Renderer materials")] 
        public string materialProperty = "_SurfaceIdMap";

        [Tooltip("Surface ID to use when map is missing")]
        [Range(0, 255)]
        public int defaultSurfaceId;

        /// <summary>
        /// Returns surface ID sampled from provided texture or material.
        /// </summary>
        /// <param name="uv">Texture coordinates from collision.</param>
        /// <param name="sourceRenderer">Renderer to query when <see cref="surfaceIdMap"/> is not set.</param>
        /// <returns>ID in range 0..255.</returns>
        public int GetSurfaceId(Vector2 uv, Renderer sourceRenderer = null)
        {
            Texture2D map = surfaceIdMap;
            if (map == null && sourceRenderer != null)
            {
                if (sourceRenderer.sharedMaterial != null &&
                    sourceRenderer.sharedMaterial.HasProperty(materialProperty))
                {
                    map = sourceRenderer.sharedMaterial.GetTexture(materialProperty) as Texture2D;
                }
            }

            if (map == null)
                return defaultSurfaceId;

            Color c = map.GetPixelBilinear(uv.x, uv.y);
            return Mathf.Clamp(Mathf.RoundToInt(c.grayscale * 255f), 0, 255);
        }

        /// <summary>
        /// Helper that fetches surface ID from a collider using <see cref="ImpactSurface"/> if present
        /// or via the collider's renderer material property.
        /// </summary>
        public static int GetSurfaceId(Collider collider, Vector2 uv)
        {
            if (collider == null) return 0;

            var surface = collider.GetComponent<ImpactSurface>();
            if (surface != null)
            {
                var renderer = collider.GetComponent<Renderer>();
                return surface.GetSurfaceId(uv, renderer);
            }

            var rend = collider.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null &&
                rend.sharedMaterial.HasProperty("_SurfaceIdMap"))
            {
                var tex = rend.sharedMaterial.GetTexture("_SurfaceIdMap") as Texture2D;
                if (tex != null)
                {
                    Color c = tex.GetPixelBilinear(uv.x, uv.y);
                    return Mathf.Clamp(Mathf.RoundToInt(c.grayscale * 255f), 0, 255);
                }
            }
            return 0;
        }
    }
}
