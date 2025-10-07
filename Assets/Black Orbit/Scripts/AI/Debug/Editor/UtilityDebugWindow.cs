#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Controller;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Debug.Editor
{
    public class UtilityDebugWindow : EditorWindow
    {
        private Vector2 _scrollLeft;
        private Vector2 _scrollRight;
        private AIController _selected;
        private bool _autoSelectFirst = true;
        private bool _followSelection = true;

        [MenuItem("Black Orbit/AI/Utility Debug View")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<UtilityDebugWindow>();
            wnd.titleContent = new GUIContent("Utility Debug");
            wnd.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }
        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawAgentsList();
            DrawAgentDetails();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAgentsList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            EditorGUILayout.LabelField("AI Agents", EditorStyles.boldLabel);
            _autoSelectFirst = EditorGUILayout.ToggleLeft("Auto-select first", _autoSelectFirst);
            _followSelection = EditorGUILayout.ToggleLeft("Follow Scene Selection", _followSelection);

            _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft, GUILayout.ExpandHeight(true));
            var list = AIController.Registry;
            if (list != null && list.Count > 0)
            {
                foreach (var ai in list)
                {
                    if (ai == null) continue;
                    EditorGUILayout.BeginHorizontal();
                    bool isSel = _selected == ai;
                    if (GUILayout.Toggle(isSel, ai.name, "Button"))
                    {
                        _selected = ai;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (_selected == null && _autoSelectFirst)
                    _selected = list[0];
            }
            else
            {
                EditorGUILayout.HelpBox("Нет активных AIController в сцене.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawAgentDetails()
        {
            EditorGUILayout.BeginVertical();
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Выберите AIController слева.", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_selected.name, EditorStyles.largeLabel);
                if (GUILayout.Button("Ping")) EditorGUIUtility.PingObject(_selected);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Domains & Actions", EditorStyles.boldLabel);

            _scrollRight = EditorGUILayout.BeginScrollView(_scrollRight);

            var scores = _selected.LastScores;
            if (scores != null)
            {
                foreach (var kv in scores)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField($"Domain: {kv.Key}", EditorStyles.miniBoldLabel);

                    var list = kv.Value.OrderByDescending(s => s.score);
                    foreach (var s in list)
                    {
                        DrawScoreRow(s);
                    }
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Behavior Log (last 16)", EditorStyles.boldLabel);
            int shown = 0;
            foreach (var frame in _selected.GetBehaviorLog().Reverse())
            {
                if (shown++ >= 16) break;
                EditorGUILayout.LabelField($"t={frame.time:F2}");
                EditorGUI.indentLevel++;
                foreach (var w in frame.winners)
                {
                    EditorGUILayout.LabelField($"{w.Key}: {w.Value.name} ({w.Value.exec})");
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawScoreRow(AIController.ActionScore s)
        {
            var rect = EditorGUILayout.GetControlRect();
            float bar = Mathf.Clamp01(s.score);
            EditorGUI.ProgressBar(rect, bar, $"{s.name}  [{s.exec}]  {s.score:F2}");
        }
    }
}
#endif
