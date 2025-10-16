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
        private string _selectionWarning;

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
            Selection.selectionChanged += OnSelectionChanged;
            UpdateSelectionFromScene(force: true);
        }
        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
            Selection.selectionChanged -= OnSelectionChanged;
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
            bool followPrev = _followSelection;
            _followSelection = EditorGUILayout.ToggleLeft("Follow Scene Selection", _followSelection);
            if (_followSelection && !followPrev)
            {
                UpdateSelectionFromScene(force: true);
            }
            else if (!_followSelection && followPrev)
            {
                _selectionWarning = null;
            }

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
                        _selectionWarning = null;
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
                if (!string.IsNullOrEmpty(_selectionWarning))
                {
                    EditorGUILayout.HelpBox(_selectionWarning, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Выберите AIController слева.", MessageType.None);
                }
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

        private void OnSelectionChanged()
        {
            UpdateSelectionFromScene();
        }

        private void UpdateSelectionFromScene(bool force = false)
        {
            if (!_followSelection && !force)
                return;

            string warning = null;
            _selected = ResolveSelectionFromActive(ref warning);
            _selectionWarning = warning;
            Repaint();
        }

        private AIController ResolveSelectionFromActive(ref string warning)
        {
            warning = null;

            if (Selection.activeObject is AIController directController)
            {
                return directController;
            }

            var go = Selection.activeGameObject;
            if (go == null)
            {
                warning = "Нет выбранного объекта в сцене.";
                return null;
            }

            var controller = go.GetComponentInParent<AIController>(true);
            if (controller != null)
            {
                return controller;
            }

            warning = $"Объект '{go.name}' не содержит AIController.";
            return null;
        }
    }
}
#endif
