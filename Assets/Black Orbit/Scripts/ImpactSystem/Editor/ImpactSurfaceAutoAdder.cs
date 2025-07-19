#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Black_Orbit.Scripts.ImpactSystem.Runtime;

namespace Black_Orbit.Scripts.ImpactSystem.Editor
{
    /// <summary>
    /// Автоматически добавляет ImpactSurface ко всем объектам с коллайдерами
    /// в сцене и при добавлении новых коллайдеров.
    /// </summary>
    [InitializeOnLoad]
    public static class ImpactSurfaceAutoAdder
    {
        static ImpactSurfaceAutoAdder()
        {
            ObjectFactory.componentWasAdded += OnComponentAdded;
            EditorApplication.delayCall += AddToExistingColliders;
        }

        private static void OnComponentAdded(Component component)
        {
            if (component is Collider collider)
            {
                AddImpactSurface(collider.gameObject);
            }
        }

        private static void AddToExistingColliders()
        {
            foreach (var collider in Object.FindObjectsOfType<Collider>(true))
            {
                AddImpactSurface(collider.gameObject);
            }
        }

        private static void AddImpactSurface(GameObject go)
        {
            if (go.GetComponent<ImpactSurface>() == null)
            {
                Undo.AddComponent<ImpactSurface>(go);
            }
        }
    }
}
#endif
