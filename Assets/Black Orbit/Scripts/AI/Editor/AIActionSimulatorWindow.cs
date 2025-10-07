using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Black_Orbit.Scripts.AI.Editor
{
    /// <summary>
    /// Симулятор выбора действий: позволяет в Play Mode задавать стимулы (LOS, шум, здоровье, цель)
    /// и смотреть utility-оценки всех действий, какое действие выберется сейчас, без фактического выполнения.
    /// </summary>
    public class AIActionSimulatorWindow : EditorWindow
    {
        private Runtime.AI _ai;
        private Vector2 _scroll;

        // Stimuli controls
        private bool _los;
        private float _timeSinceLastSeen;
        private Vector3 _lastSeen;
        private float _health = 100f;
        private float _suppressionDesired;
        private Transform _attackTarget;
        private Vector3 _noisePos;
        private float _noiseLevel = 1f;
        private float _noiseRadius = 25f;

        [MenuItem("Tools/AI/Action Simulator")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<AIActionSimulatorWindow>(false, "AI Action Simulator", true);
            wnd.minSize = new Vector2(560, 420);
            wnd.Show();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Симулятор работает в Play Mode.", MessageType.Info);
            }

            DrawHeader();
            EditorGUILayout.Space(6);

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                using (new EditorGUI.DisabledScope(_ai == null))
                {
                    DrawStimuli();
                    EditorGUILayout.Space(6);
                    DrawScores();
                }
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _ai = (Runtime.AI)EditorGUILayout.ObjectField(new GUIContent("Target AI"), _ai, typeof(Runtime.AI), true);
                if (GUILayout.Button("Pick From Selection", GUILayout.Width(160)))
                {
                    var go = Selection.activeGameObject;
                    if (go != null) _ai = go.GetComponent<Runtime.AI>();
                }
                if (_ai != null && GUILayout.Button("Focus", GUILayout.Width(80)))
                {
                    Selection.activeObject = _ai.gameObject;
                    EditorGUIUtility.PingObject(_ai.gameObject);
                }
            }

            if (_ai != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Faction: {_ai.faction?.factionName ?? "None"}");
                    GUILayout.Label($"HP: {_ai.Health:F0}");
                    GUILayout.Label($"LOS: {(_ai.hasLineOfSight ? "✓" : "✗")}");
                    GUILayout.Label($"LastSeen: {_ai.timeSinceLastSeen:F1}s");
                }
            }
        }

        private void DrawStimuli()
        {
            EditorGUILayout.LabelField("Stimuli & Overrides", EditorStyles.boldLabel);

            // LOS, Last Seen
            _los = EditorGUILayout.ToggleLeft("Has Line Of Sight", _ai.hasLineOfSight);
            _timeSinceLastSeen = EditorGUILayout.Slider("Time Since Last Seen (s)", _ai.timeSinceLastSeen, 0f, 30f);
            _lastSeen = EditorGUILayout.Vector3Field("Last Seen Pos", _ai.lastSeenTargetPos);

            // Health & Suppression
            _health = EditorGUILayout.Slider("Health", _ai.Health, 0f, Mathf.Max(1f, _ai.Health + 100f));
            _suppressionDesired = EditorGUILayout.Slider("Suppression (set target)", _ai.SuppressionLevel, 0f, 1f);

            // Target
            _attackTarget = (Transform)EditorGUILayout.ObjectField("Attack Target", _ai.AttackTarget, typeof(Transform), true);

            // Noise
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Noise", EditorStyles.boldLabel);
            _noisePos = EditorGUILayout.Vector3Field("Position", _noisePos);
            _noiseLevel = EditorGUILayout.Slider("Level", _noiseLevel, 0f, 1f);
            _noiseRadius = EditorGUILayout.Slider("Radius", _noiseRadius, 1f, 100f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Overrides")) ApplyOverrides();
                if (GUILayout.Button("Emit Noise")) Runtime.AI.EmitNoise(_noisePos, _noiseLevel, _noiseRadius);
            }
        }

        private void ApplyOverrides()
        {
            if (_ai == null) return;
            Undo.RecordObject(_ai, "AI Stimuli Overrides");

            // LOS & Last seen
            _ai.hasLineOfSight = _los;
            _ai.timeSinceLastSeen = _timeSinceLastSeen;
            _ai.lastSeenTargetPos = _lastSeen;

            // Attack target
            _ai.AttackTarget = _attackTarget;

            // Health
            _ai.Health = _health;

            // Suppression: Add or decay to approach desired value (approx)
            float currentSupp = _ai.SuppressionLevel;
            float delta = Mathf.Clamp01(_suppressionDesired) - currentSupp;
            if (delta > 0f) _ai.AddSuppression(delta);
            else if (Mathf.Abs(delta) > 0.001f)
            {
                // Быстрое обнуление до желаемого: несколько тиков затухания
                // (точной установки нет в API, избегаем приватного доступа)
                // Можно имитировать временем — оставим как есть, если нужно точнее, добавим SetSuppression в AI позже.
            }

            EditorUtility.SetDirty(_ai);
        }

        private void DrawScores()
        {
            if (_ai.Actions == null || _ai.Actions.Length == 0)
            {
                EditorGUILayout.HelpBox("У AI нет назначенных действий.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Utility Scores", EditorStyles.boldLabel);

            var actions = _ai.Actions;
            float bestScore = -1f; int bestIndex = -1;
            float[] scores = new float[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                var a = actions[i];
                if (a == null) { scores[i] = -1f; continue; }
                float s = a.Evaluate(_ai);
                scores[i] = s;
                if (s > bestScore) { bestScore = s; bestIndex = i; }
            }

            for (int i = 0; i < actions.Length; i++)
            {
                var a = actions[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    string title = a != null ? a.name : "<null>";
                    if (i == bestIndex) title = $"★ {title}";
                    EditorGUILayout.LabelField(title);
                    float s = scores[i];
                    EditorGUILayout.Slider(s, 0f, 1f);
                }
            }

            if (bestIndex >= 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox($"Наивысший score: {actions[bestIndex].name} = {bestScore:F3}", MessageType.Info);
            }
        }
    }
}
