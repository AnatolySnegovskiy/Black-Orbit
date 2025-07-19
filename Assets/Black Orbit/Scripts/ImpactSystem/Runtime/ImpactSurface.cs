using UnityEngine;

namespace Black_Orbit.Scripts.ImpactSystem.Runtime
{
    /// <summary>
    /// Компонент, выдающий ID поверхности на основе оттенка серого
    /// в текстуре. Значение 0 соответствует ID 0, а 255 – ID 255.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ImpactSurface : MonoBehaviour
    {
        public const string DefaultProperty = "_SurfaceIdMap";
        [Tooltip("Карта оттенков серого, где яркость соответствует ID поверхности")]
        public Texture2D surfaceIdMap;

        [Tooltip("Имя текстурного свойства в материале")]
        public string materialProperty = DefaultProperty;

        [Tooltip("ID поверхности по умолчанию, если карта отсутствует")]
        [Range(0, 255)]
        public int defaultSurfaceId;

        /// <summary>
        /// Возвращает ID поверхности из заданной карты или материала.
        /// </summary>
        /// <param name="uv">Текстурные координаты точки столкновения.</param>
        /// <param name="sourceRenderer">Рендерер, из которого брать карту, если <see cref="surfaceIdMap"/> не задана.</param>
        /// <returns>ID в диапазоне 0..255.</returns>
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
        /// Статический помощник, возвращающий ID поверхности из коллайдера.
        /// Сначала ищется компонент <see cref="ImpactSurface"/>, затем карта в материале рендерера.
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
                rend.sharedMaterial.HasProperty(DefaultProperty))
            {
                var tex = rend.sharedMaterial.GetTexture(DefaultProperty) as Texture2D;
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
