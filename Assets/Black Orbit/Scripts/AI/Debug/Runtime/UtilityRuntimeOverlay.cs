using System.Linq;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Controller;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Debug.Runtime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Black Orbit/AI/Debug/Utility Runtime Overlay")]
    [HelpURL("https://github.com/AnatolySnegovskiy/Black-Orbit/blob/main/Assets/Black%20Orbit/Docs/AI_Bots_GDD.md")]
    public class UtilityRuntimeOverlay : MonoBehaviour
    {
        [Header("Цель (AIController)")]
        [Tooltip("Если не задано и включено 'Автовыбор', при старте возьмёт первого доступного AIController на сцене.")]
        public AIController target;

        [Header("Показ и управление")]
        [Tooltip("Автоматически выбирать первого найденного AIController, если поле 'Цель' пусто.")]
        public bool autoPickFirst = true;

        [Tooltip("Отображать оверлей на экране во время выполнения.")]
        public bool show = true;

        [Tooltip("Клавиша переключения видимости оверлея.")]
        public KeyCode toggleKey = KeyCode.F9;

        [Header("Журнал поведения")]
        [Tooltip("Максимальное количество последних кадров решений, отображаемых в оверлее.")]
        [Min(1)]
        public int maxLogLines = 8;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) show = !show;
            if (show && (target == null) && autoPickFirst && AIController.Registry.Count > 0)
                target = AIController.Registry[0];
        }

        private void OnGUI()
        {
            if (!show || target == null) return;
            var styleHeader = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            var styleMono = new GUIStyle(GUI.skin.label) { fontSize = 12 };

            GUILayout.BeginArea(new Rect(12, 12, 420, Screen.height - 24), GUI.skin.window);
            GUILayout.Label($"AI: {target.name}", styleHeader);

            GUILayout.Space(6);
            GUILayout.Label("Domains & Actions", styleHeader);

            if (target.LastScores != null)
            {
                foreach (var kv in target.LastScores)
                {
                    GUILayout.Label($"Domain: {kv.Key}", styleMono);
                    foreach (var s in kv.Value.OrderByDescending(x => x.score))
                    {
                        float pct = Mathf.Clamp01(s.score);
                        GUILayout.HorizontalSlider(pct, 0f, 1f);
                        GUILayout.Label($"{s.name} [{s.exec}]  {s.score:F2}", styleMono);
                    }
                    GUILayout.Space(4);
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Behavior Log", styleHeader);
            int shown = 0;
            foreach (var frame in target.GetBehaviorLog().Reverse())
            {
                if (shown++ >= maxLogLines) break;
                GUILayout.Label($"t={frame.time:F2}", styleMono);
                foreach (var w in frame.winners)
                {
                    GUILayout.Label($" - {w.Key}: {w.Value.name} ({w.Value.exec})", styleMono);
                }
            }

            GUILayout.EndArea();
        }
    }
}
