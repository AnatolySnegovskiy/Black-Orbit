using UnityEditor;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Editor
{
    /// <summary>
    /// SceneTool для генерации шумов AI прямо из SceneView.
    /// Меню: Tools/AI/Noise Emitter Tool
    /// Управление: ЛКМ — эмит шума в точку; удерживайте Ctrl, чтобы эмитить без открытия окна.
    /// </summary>
    public class AINoiseEmitterTool : EditorWindow
    {
        private static bool s_enabled;
        private static float s_level = 1.0f;
        private static float s_radius = 20f;
        private static Color s_color = new Color(1f, 0.85f, 0.2f, 0.75f);
        private static LayerMask s_raycastMask = Physics.DefaultRaycastLayers;
        private static bool s_ignoreTriggers = true;

        [MenuItem("Tools/AI/Noise Emitter Tool")] 
        public static void Toggle()
        {
            if (s_enabled)
            {
                Disable();
            }
            else
            {
                var wnd = GetWindow<AINoiseEmitterTool>(false, "AI Noise Emitter", true);
                wnd.minSize = new Vector2(260, 120);
                wnd.Show();
                Enable();
            }
        }

        private static void Enable()
        {
            if (s_enabled) return;
            s_enabled = true;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void Disable()
        {
            if (!s_enabled) return;
            s_enabled = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnDisable()
        {
            // Закрытие окна не обязательно выключает тул — но сделаем, чтобы избежать висящих хэндлеров
            Disable();
        }

        private void OnGUI()
        {
            GUILayout.Label("Emit Noise", EditorStyles.boldLabel);
            s_level = EditorGUILayout.Slider("Level", s_level, 0.05f, 1f);
            s_radius = EditorGUILayout.Slider("Radius", s_radius, 5f, 80f);
            s_color = EditorGUILayout.ColorField("Gizmo Color", s_color);
            s_raycastMask = LayerMaskField("Raycast Mask", s_raycastMask);
            s_ignoreTriggers = EditorGUILayout.ToggleLeft("Ignore Triggers", s_ignoreTriggers);

            EditorGUILayout.HelpBox("ЛКМ в сцене — создать шум. Удерживайте Ctrl, чтобы эмитить без открытия окна.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!s_enabled))
            {
                if (GUILayout.Button("Disable Tool")) Disable();
            }
            if (!s_enabled)
            {
                if (GUILayout.Button("Enable Tool")) Enable();
            }
        }

        private static void OnSceneGUI(SceneView view)
        {
            Event e = Event.current;
            Handles.color = s_color;
            Vector3 hitPoint = Vector3.zero;
            bool hasHit = false;

            Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(r, out var hit, 1000f, s_raycastMask, s_ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide))
            {
                hitPoint = hit.point;
                hasHit = true;
            }
            else
            {
                hitPoint = r.origin + r.direction * 10f;
            }

            if (hasHit)
            {
                Handles.DrawWireDisc(hitPoint, Vector3.up, s_radius);
                Handles.DrawSolidDisc(hitPoint + Vector3.up * 0.05f, Vector3.up, 0.15f);
                Handles.Label(hitPoint + Vector3.up * 0.25f, $"Noise L={s_level:F2} R={s_radius:F0}");
            }

            if ((e.type == EventType.MouseDown && e.button == 0) || (e.type == EventType.MouseDrag && e.button == 0 && e.control))
            {
                if (hasHit)
                {
                    Runtime.AI.EmitNoise(hitPoint, Mathf.Clamp01(s_level), s_radius);
                    e.Use();
                }
            }
        }

        // Helper to edit LayerMask like a mask field
        private static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                int layerNumber = LayerMask.NameToLayer(layers[i]);
                if (((layerMask.value >> layerNumber) & 1) == 1)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) != 0)
                    mask |= (1 << LayerMask.NameToLayer(layers[i]));
            }
            layerMask.value = mask;
            return layerMask;
        }
    }
}
