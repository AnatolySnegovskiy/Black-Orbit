#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Black_Orbit.Scripts.ImpactSystem.Runtime;

namespace Black_Orbit.Scripts.ImpactSystem.Editor
{
    [CustomEditor(typeof(ImpactSurface))]
    public class ImpactSurfaceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Add ImpactSurface To Children"))
            {
                var root = ((ImpactSurface)target).transform;
                foreach (Transform child in root)
                {
                    if (child.GetComponent<ImpactSurface>() == null)
                        child.gameObject.AddComponent<ImpactSurface>();
                }
            }
        }
    }

    public static class ImpactSurfaceMenu
    {
        [MenuItem("GameObject/Impact/Add Impact Surface", false, 10)]
        private static void AddImpactSurface(MenuCommand command)
        {
            var go = command.context as GameObject;
            if (go != null && go.GetComponent<ImpactSurface>() == null)
                go.AddComponent<ImpactSurface>();
        }
    }
}
#endif
