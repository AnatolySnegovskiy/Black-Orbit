using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;

namespace Black_Orbit.Scripts.AI.Editor
{
    /// <summary>
    /// Prefabs-only AI Manager: редактирование параметров AI и наборов UtilityAction прямо в префаб-ассетах.
    /// Папки:
    /// - Префабы: Assets/Black Orbit/Prefabs/AI/Prefabs
    /// - Actions: Assets/Black Orbit/GameData/AI/Actions
    /// </summary>
    public class AIPrefabsManagerWindow : EditorWindow
    {
        private const string PREFABS_FOLDER = "Assets/Black Orbit/Prefabs/AI/Prefabs";
        private const string ACTIONS_FOLDER = "Assets/Black Orbit/GameData/AI/Actions";

        [MenuItem("Tools/AI/Prefabs Manager")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<AIPrefabsManagerWindow>(false, "AI Prefabs Manager", true);
            wnd.minSize = new Vector2(560, 420);
            wnd.RefreshPrefabs();
            wnd.RefreshActions();
            wnd.Show();
        }

        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _expandAll;

        private readonly List<PrefabEntry> _prefabs = new();
        private readonly List<UtilityAction> _allActions = new();
        private readonly Dictionary<UnityEngine.Object, Editor> _actionEditors = new();

        private class PrefabEntry
        {
            public string path;
            public GameObject asset;
            public bool foldout;
            public bool isSquad;
            public SquadBlock squad;
            public List<AIBlock> members = new(); // для сквадов
            public AIBlock ai; // для одиночного бота
        }

        private class AIBlock
        {
            public string name;
            public float hp, detect, visRange, visAngle, melee, ranged, searchInt;
            public List<UtilityAction> actions = new();
            public List<bool> actionFoldouts = new();
        }

        private class SquadBlock
        {
            public string squadName;
            public float searchRadius;
            public float coordinationInterval;
            public int minMembersForCoordination;
        }

        private void OnEnable()
        {
            RefreshPrefabs();
            RefreshActions();
        }

        private void OnGUI()
        {
            DrawTopBar();

            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;
                DrawPrefabsList();
                EditorGUILayout.Space(12);
                DrawActionsBrowser();
            }
        }

        private void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(new GUIContent("Refresh", "Перечитать префабы и экшены"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    RefreshPrefabs();
                    RefreshActions();
                }
                _expandAll = GUILayout.Toggle(_expandAll, new GUIContent("Expand All"), EditorStyles.toolbarButton, GUILayout.Width(90));
                GUILayout.Space(6);
                GUILayout.Label("Search:", GUILayout.Width(48));
                _search = GUILayout.TextField(_search ?? string.Empty, EditorStyles.toolbarTextField, GUILayout.Width(240));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Open Squad Panel"), EditorStyles.toolbarButton, GUILayout.Width(120))) AISquadCommandPanel.ShowWindow();
            }
        }

        private void DrawPrefabsList()
        {
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            if (_prefabs.Count == 0)
            {
                EditorGUILayout.HelpBox($"Префабы не найдены в папке: {PREFABS_FOLDER}", MessageType.Info);
                return;
            }

            foreach (var p in _prefabs)
            {
                if (!string.IsNullOrEmpty(_search) && p.asset != null && !p.asset.name.ToLowerInvariant().Contains((_search ?? string.Empty).ToLowerInvariant()))
                    continue;

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        p.foldout = EditorGUILayout.Foldout(_expandAll || p.foldout, p.asset != null ? p.asset.name : p.path, true);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Load", GUILayout.Width(60))) LoadPrefabSnapshot(p);
                        if (GUILayout.Button("Apply", GUILayout.Width(60))) ApplyPrefabSnapshot(p);
                    }

                    if (_expandAll || p.foldout)
                    {
                        if (p.isSquad)
                        {
                            DrawSquadUI(p);
                        }
                        else
                        {
                            DrawAIUI(p.ai);
                        }
                    }
                }
            }
        }

        private void DrawAIUI(AIBlock ai)
        {
            if (ai == null)
            {
                EditorGUILayout.HelpBox("AI компонент не найден.", MessageType.Warning);
                return;
            }
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            ai.hp = EditorGUILayout.FloatField("HP", ai.hp);
            ai.detect = EditorGUILayout.FloatField("DetectionRange", ai.detect);
            ai.visRange = EditorGUILayout.FloatField("VisionRange", ai.visRange);
            ai.visAngle = EditorGUILayout.FloatField("VisionAngle", ai.visAngle);
            ai.melee = EditorGUILayout.FloatField("MeleeAttackRange", ai.melee);
            ai.ranged = EditorGUILayout.FloatField("RangedAttackRange", ai.ranged);
            ai.searchInt = EditorGUILayout.FloatField("TargetSearchInterval", ai.searchInt);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Action", GUILayout.Width(100))) ShowAddActionMenu(ai);
                if (GUILayout.Button("Remove All", GUILayout.Width(100))) { ai.actions.Clear(); ai.actionFoldouts.Clear(); }
            }
            if (ai.actions.Count == 0)
            {
                EditorGUILayout.HelpBox("Список пуст.", MessageType.None);
            }
            else
            {
                for (int i = 0; i < ai.actions.Count; i++)
                {
                    var act = ai.actions[i];
                    if (ai.actionFoldouts.Count != ai.actions.Count) ai.actionFoldouts = Enumerable.Repeat(false, ai.actions.Count).ToList();
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            ai.actionFoldouts[i] = EditorGUILayout.Foldout(ai.actionFoldouts[i], act != null ? act.name : "<null>", true);
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("▲", GUILayout.Width(28)) && i > 0) { (ai.actions[i - 1], ai.actions[i]) = (ai.actions[i], ai.actions[i - 1]); }
                            if (GUILayout.Button("▼", GUILayout.Width(28)) && i < ai.actions.Count - 1) { (ai.actions[i + 1], ai.actions[i]) = (ai.actions[i], ai.actions[i + 1]); }
                            if (GUILayout.Button("Remove", GUILayout.Width(70))) { ai.actions.RemoveAt(i); ai.actionFoldouts.RemoveAt(i); break; }
                        }
                        if (ai.actionFoldouts[i] && act != null)
                        {
                            DrawActionInspector(act);
                        }
                    }
                }
            }
        }

        private void DrawSquadUI(PrefabEntry p)
        {
            if (p.squad == null || p.members == null)
            {
                EditorGUILayout.HelpBox("Данные сквада не загружены. Нажмите Load.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Squad", EditorStyles.boldLabel);
            p.squad.squadName = EditorGUILayout.TextField("Name", p.squad.squadName);
            p.squad.searchRadius = EditorGUILayout.FloatField("Search Radius", p.squad.searchRadius);
            p.squad.coordinationInterval = EditorGUILayout.FloatField("Coordination Interval", p.squad.coordinationInterval);
            p.squad.minMembersForCoordination = EditorGUILayout.IntField("Min Members", p.squad.minMembersForCoordination);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Members", EditorStyles.boldLabel);
            for (int i = 0; i < p.members.Count; i++)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField($"[{i}] {p.members[i].name}", EditorStyles.boldLabel);
                    DrawAIUI(p.members[i]);
                }
            }
        }

        private void DrawActionsBrowser()
        {
            EditorGUILayout.LabelField("Actions Assets", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create...", GUILayout.Width(100))) ShowCreateActionMenu();
                if (GUILayout.Button("Refresh", GUILayout.Width(80))) RefreshActions();
            }
            if (_allActions.Count == 0)
            {
                EditorGUILayout.HelpBox($"Экшены не найдены в папке: {ACTIONS_FOLDER}", MessageType.Info);
                return;
            }
            foreach (var act in _allActions)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(act, typeof(UtilityAction), false);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Delete", GUILayout.Width(70)))
                        {
                            var path = AssetDatabase.GetAssetPath(act);
                            if (EditorUtility.DisplayDialog("Delete Action", $"Delete {act.name}?", "Yes", "No"))
                            {
                                AssetDatabase.DeleteAsset(path);
                                AssetDatabase.SaveAssets();
                                RefreshActions();
                                break;
                            }
                        }
                    }
                    DrawActionInspector(act);
                }
            }
        }

        private void RefreshPrefabs()
        {
#if UNITY_EDITOR
            _prefabs.Clear();
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_FOLDER });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;
                var entry = new PrefabEntry { path = path, asset = asset, foldout = false, squad = new SquadBlock(), members = new List<AIBlock>() };
                // определить тип и прочитать снапшот
                var contents = PrefabUtility.LoadPrefabContents(path);
                if (contents != null)
                {
                    var squad = contents.GetComponentInChildren<Runtime.AISquad>(true);
                    entry.isSquad = squad != null;
                    if (entry.isSquad)
                    {
                        entry.squad.squadName = squad.squadName;
                        entry.squad.searchRadius = squad.searchRadius;
                        entry.squad.coordinationInterval = squad.coordinationInterval;
                        entry.squad.minMembersForCoordination = squad.minMembersForCoordination;
                        var ais = contents.GetComponentsInChildren<Runtime.AI>(true);
                        foreach (var ai in ais) entry.members.Add(ToAIBlock(ai));
                    }
                    else
                    {
                        var ai = contents.GetComponentInChildren<Runtime.AI>(true);
                        if (ai != null) entry.ai = ToAIBlock(ai);
                    }
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                _prefabs.Add(entry);
            }
#endif
        }

        private void RefreshActions()
        {
#if UNITY_EDITOR
            _allActions.Clear();
            var guids = AssetDatabase.FindAssets("t:Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction", new[] { ACTIONS_FOLDER });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<UtilityAction>(path);
                if (a != null) _allActions.Add(a);
            }
            _allActions.Sort((a,b)=>string.Compare(a.name,b.name,StringComparison.Ordinal));
#endif
        }

        private AIBlock ToAIBlock(Runtime.AI ai)
        {
            var b = new AIBlock
            {
                name = ai.gameObject.name,
                hp = ai.Health,
                detect = ai.DetectionRange,
                visRange = ai.VisionRange,
                visAngle = ai.VisionAngle,
                melee = ai.MeleeAttackRange,
                ranged = ai.RangedAttackRange,
                searchInt = ai.TargetSearchInterval
            };
#if UNITY_EDITOR
            try
            {
                var so = new SerializedObject(ai);
                var ap = so.FindProperty("actions");
                if (ap != null)
                {
                    for (int i = 0; i < ap.arraySize; i++)
                    {
                        var el = ap.GetArrayElementAtIndex(i).objectReferenceValue as UtilityAction;
                        if (el != null) b.actions.Add(el);
                    }
                }
            }
            catch { }
#endif
            b.actionFoldouts = Enumerable.Repeat(false, b.actions.Count).ToList();
            return b;
        }

        private void ApplyPrefabSnapshot(PrefabEntry p)
        {
#if UNITY_EDITOR
            var contents = PrefabUtility.LoadPrefabContents(p.path);
            if (contents == null) return;
            if (p.isSquad)
            {
                var squad = contents.GetComponentInChildren<Runtime.AISquad>(true);
                if (squad != null)
                {
                    squad.squadName = p.squad.squadName;
                    squad.searchRadius = p.squad.searchRadius;
                    squad.coordinationInterval = p.squad.coordinationInterval;
                    squad.minMembersForCoordination = p.squad.minMembersForCoordination;
                }
                var ais = contents.GetComponentsInChildren<Runtime.AI>(true);
                int idx = 0;
                foreach (var ai in ais)
                {
                    var block = idx < p.members.Count ? p.members[idx] : null;
                    if (block != null) ApplyAIBlock(ai, block);
                    idx++;
                }
            }
            else
            {
                var ai = contents.GetComponentInChildren<Runtime.AI>(true);
                if (ai != null && p.ai != null) ApplyAIBlock(ai, p.ai);
            }
            PrefabUtility.SaveAsPrefabAsset(contents, p.path);
            PrefabUtility.UnloadPrefabContents(contents);
            ShowNotification(new GUIContent("Saved prefab"));
#endif
        }

        private void ApplyAIBlock(Runtime.AI ai, AIBlock b)
        {
            ai.InitializeParameters(
                moveSpd: ai.MoveSpeed,
                rotSpd: ai.RotationSpeed,
                detectRange: b.detect,
                meleeRange: b.melee,
                rangedRange: b.ranged,
                visAngle: b.visAngle,
                visRange: b.visRange,
                hp: b.hp,
                targetSearchInt: b.searchInt
            );
            if (b.actions != null)
            {
                ai.SetActions(b.actions.ToArray());
            }
        }

        private void LoadPrefabSnapshot(PrefabEntry p)
        {
#if UNITY_EDITOR
            var contents = PrefabUtility.LoadPrefabContents(p.path);
            if (contents == null) return;
            var squad = contents.GetComponentInChildren<Runtime.AISquad>(true);
            p.isSquad = squad != null;
            p.members.Clear();
            if (p.isSquad)
            {
                p.squad ??= new SquadBlock();
                if (squad != null)
                {
                    p.squad.squadName = squad.squadName;
                    p.squad.searchRadius = squad.searchRadius;
                    p.squad.coordinationInterval = squad.coordinationInterval;
                    p.squad.minMembersForCoordination = squad.minMembersForCoordination;
                }
                var ais = contents.GetComponentsInChildren<Runtime.AI>(true);
                foreach (var ai in ais) p.members.Add(ToAIBlock(ai));
            }
            else
            {
                var ai = contents.GetComponentInChildren<Runtime.AI>(true);
                if (ai != null) p.ai = ToAIBlock(ai);
            }
            PrefabUtility.UnloadPrefabContents(contents);
#endif
        }

        private void DrawActionInspector(UtilityAction action)
        {
            if (action == null) return;
            if (!_actionEditors.TryGetValue(action, out var ed) || ed == null)
            {
                ed = Editor.CreateEditor(action);
                _actionEditors[action] = ed;
            }
            ed.OnInspectorGUI();
        }

        private void ShowAddActionMenu(AIBlock ai)
        {
#if UNITY_EDITOR
            var menu = new GenericMenu();
            if (_allActions.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("<No Actions Found>"));
            }
            else
            {
                foreach (var act in _allActions)
                {
                    var a = act; // capture
                    menu.AddItem(new GUIContent(a.name), false, () => { ai.actions.Add(a); ai.actionFoldouts.Add(false); });
                }
            }
            menu.ShowAsContext();
#endif
        }

        private void ShowCreateActionMenu()
        {
#if UNITY_EDITOR
            var menu = new GenericMenu();
            var utilityActionType = typeof(UtilityAction);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t => utilityActionType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();
            if (types.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("<No UtilityAction subclasses found>"));
            }
            else
            {
                foreach (var t in types)
                {
                    var tt = t; // capture
                    menu.AddItem(new GUIContent(tt.Name), false, () => CreateActionAsset(tt));
                }
            }
            menu.ShowAsContext();
#endif
        }

        private void CreateActionAsset(Type actionType)
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder("Assets/Black Orbit/GameData")) AssetDatabase.CreateFolder("Assets/Black Orbit", "GameData");
            if (!AssetDatabase.IsValidFolder("Assets/Black Orbit/GameData/AI")) AssetDatabase.CreateFolder("Assets/Black Orbit/GameData", "AI");
            if (!AssetDatabase.IsValidFolder(ACTIONS_FOLDER)) AssetDatabase.CreateFolder("Assets/Black Orbit/GameData/AI", "Actions");
            var asset = ScriptableObject.CreateInstance(actionType);
            string path = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(ACTIONS_FOLDER, actionType.Name + ".asset"));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshActions();
#endif
        }
    }
}
