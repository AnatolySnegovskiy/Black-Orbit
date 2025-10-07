using UnityEditor;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime;

namespace Black_Orbit.Scripts.AI.Editor
{
    public class AISquadCommandPanel : EditorWindow
    {
        private AISquad _squad;
        private float _duration = 3f;
        private int _maxFlankers = 2;
        private int _suppressors = 2;
        private int _flankers = 1;
        private Vector3 _targetPos = Vector3.zero;
        private bool _useSquadTarget = true;
        private bool _pickFromScene;
        private LayerMask _raycastMask = Physics.DefaultRaycastLayers;
        private bool _ignoreTriggers = true;

        [MenuItem("Tools/AI/Squad Command Panel")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<AISquadCommandPanel>(false, "Squad Commands", true);
            wnd.minSize = new Vector2(340, 260);
            wnd.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Squad", EditorStyles.boldLabel);
            _squad = (AISquad)EditorGUILayout.ObjectField("Target Squad", _squad, typeof(AISquad), true);
            if (_squad == null && Selection.activeGameObject != null)
            {
                _squad = Selection.activeGameObject.GetComponent<AISquad>();
            }

            using (new EditorGUI.DisabledScope(_squad == null))
            {
                GUILayout.Space(6);
                EditorGUILayout.LabelField("Order Settings", EditorStyles.boldLabel);
                _duration = EditorGUILayout.Slider("Duration (s)", _duration, 0.5f, 15f);
                _useSquadTarget = EditorGUILayout.ToggleLeft("Use Squad Target (if available)", _useSquadTarget);

                if (!_useSquadTarget)
                {
                    _targetPos = EditorGUILayout.Vector3Field("Target Pos", _targetPos);
                    _raycastMask = LayerMaskField("Raycast Mask", _raycastMask);
                    _ignoreTriggers = EditorGUILayout.ToggleLeft("Ignore Triggers", _ignoreTriggers);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool newPick = GUILayout.Toggle(_pickFromScene, "Pick From Scene", "Button");
                        if (newPick != _pickFromScene)
                        {
                            _pickFromScene = newPick;
                            SceneView.duringSceneGui -= OnSceneGUI;
                            if (_pickFromScene) SceneView.duringSceneGui += OnSceneGUI;
                        }
                        if (GUILayout.Button("Use Camera Forward"))
                        {
                            var sv = SceneView.lastActiveSceneView;
                            if (sv != null)
                            {
                                Ray r = new Ray(sv.camera.transform.position, sv.camera.transform.forward);
                                if (Physics.Raycast(r, out var hit, 500f, _raycastMask, _ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide)) _targetPos = hit.point; else _targetPos = r.origin + r.direction * 20f;
                            }
                        }
                    }
                }

                GUILayout.Space(6);
                EditorGUILayout.LabelField("Flank", EditorStyles.boldLabel);
                _maxFlankers = EditorGUILayout.IntSlider("Max Flankers", _maxFlankers, 1, 6);
                if (GUILayout.Button("Order Flank"))
                {
                    Vector3 pos = ResolveTargetPos();
                    Undo.RecordObject(_squad, "Order Flank");
                    _squad.OrderFlank(pos, Mathf.Max(0.1f, _duration), Mathf.Max(1, _maxFlankers));
                    EditorUtility.SetDirty(_squad);
                }

                GUILayout.Space(6);
                EditorGUILayout.LabelField("Suppress + Flank", EditorStyles.boldLabel);
                _suppressors = EditorGUILayout.IntSlider("Suppressors", _suppressors, 1, 6);
                _flankers = EditorGUILayout.IntSlider("Flankers", _flankers, 0, 6);
                if (GUILayout.Button("Order SuppressAt"))
                {
                    Vector3 pos = ResolveTargetPos();
                    Undo.RecordObject(_squad, "Order SuppressAt");
                    _squad.OrderSuppressAt(pos, Mathf.Max(0.1f, _duration), Mathf.Max(1, _suppressors), Mathf.Max(0, _flankers));
                    EditorUtility.SetDirty(_squad);
                }

                GUILayout.Space(10);
                if (GUILayout.Button("Order TakeCover (All)"))
                {
                    Undo.RecordObject(_squad, "Order Squad TakeCover");
                    _squad.OrderSquad("TakeCover", Mathf.Max(0.1f, _duration));
                    EditorUtility.SetDirty(_squad);
                }

                if (GUILayout.Button("Cancel Orders (All)"))
                {
                    Undo.RecordObject(_squad, "Cancel Orders");
                    foreach (var m in _squad.members)
                    {
                        if (m != null) m.CancelOrder();
                    }
                    EditorUtility.SetDirty(_squad);
                }
            }

            GUILayout.FlexibleSpace();
            DrawInfo();
        }

        private Vector3 ResolveTargetPos()
        {
            if (_useSquadTarget && _squad != null && _squad.SquadTarget != null)
                return _squad.SquadTarget.position;
            return _targetPos;
        }

        private void OnSceneGUI(SceneView view)
        {
            if (!_pickFromScene) return;
            Event e = Event.current;
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(_targetPos, Vector3.up, 0.5f);
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(r, out var hit, 1000f, _raycastMask, _ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide)) _targetPos = hit.point; else _targetPos = r.origin + r.direction * 20f;
                _pickFromScene = false;
                Repaint();
                e.Use();
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _pickFromScene = false;
                Repaint();
                e.Use();
            }
        }

        // LayerMaskField helper similar to EditorGUILayout.LayerField but returns LayerMask directly.
        private static LayerMask LayerMaskField(string label, LayerMask selected)
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            var layerNumbers = UnityEditorInternal.InternalEditorUtility.layers;
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                int layerNumber = LayerMask.NameToLayer(layers[i]);
                if (((selected.value >> layerNumber) & 1) == 1)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) != 0)
                    mask |= (1 << LayerMask.NameToLayer(layers[i]));
            }
            selected.value = mask;
            return selected;
        }

        private void DrawInfo()
        {
            if (_squad == null) return;
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Squad Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Members", _squad.members != null ? _squad.members.Count.ToString() : "0");
            EditorGUILayout.LabelField("Alive", _squad.AliveCount.ToString());
            var tgt = _squad.SquadTarget; 
            EditorGUILayout.LabelField("SquadTarget", tgt != null ? tgt.name : "<none>");
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }
}
