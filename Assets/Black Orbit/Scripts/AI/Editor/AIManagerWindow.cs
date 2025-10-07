using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Black_Orbit.Scripts.AI.ScriptableObjects;

namespace Black_Orbit.Scripts.AI.Editor
{
    /// <summary>
    /// AI Manager: окно для дизайнеров. Позволяет выбирать профиль, применять к ботам в сцене,
    /// работать с выборкой и быстро открывать связанные инструменты.
    /// </summary>
    public class AIManagerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private AIProfile _profile;
        private List<Runtime.AI> _cache = new List<Runtime.AI>(128);
        private string _filter = string.Empty;
        private bool _onlyActive = true;
        private int _tab = 0; // 0-Overview, 1-Profiles, 2-Batch, 3-Actions, 4-Prefab
        private bool _showHelp = true; // онбординг/подсказки

        // Profiles tab state
        private List<AIProfile> _profiles = new List<AIProfile>(32);
        private int _selectedProfileIndex = -1;
        private string _factionFilter = string.Empty;
        private string _tagFilter = string.Empty;

        // Prefab (donor) tab state
        private GameObject _donorPrefab;
        private string _donorPath;
        private bool _donorValid;
        private struct DonorSnapshot
        {
            public float hp, detect, visRange, visAngle, melee, ranged, searchInt;
            public Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction[] actions;
            public bool has;
        }

        // =====================
        // PREFAB (DONOR) TAB
        // =====================
        private void DrawPrefabTab()
        {
            EditorGUILayout.LabelField("Donor Prefab", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                var newPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab", "Эталонный префаб с Runtime.AI"), _donorPrefab, typeof(GameObject), false);
                if (newPrefab != _donorPrefab)
                {
                    _donorPrefab = newPrefab;
                    RebuildDonorSnapshot();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(90))) RebuildDonorSnapshot();
            }

            if (!_donor.has)
            {
                EditorGUILayout.HelpBox("Выберите префаб-донора (Asset) для копирования параметров и списка действий.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Snapshot", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"HP: {_donor.hp:F0}");
                EditorGUILayout.LabelField($"Detect: {_donor.detect:F1}");
                EditorGUILayout.LabelField($"Vision: R={_donor.visRange:F1} A={_donor.visAngle:F0}°");
                EditorGUILayout.LabelField($"Melee/Ranged: {_donor.melee:F1}/{_donor.ranged:F1}");
                EditorGUILayout.LabelField($"TargetSearchInt: {_donor.searchInt:F2}");
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
                if (_donor.actions == null || _donor.actions.Length == 0)
                    EditorGUILayout.HelpBox("У донора пустой список действий.", MessageType.None);
                else
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (int i = 0; i < _donor.actions.Length; i++)
                        {
                            EditorGUILayout.ObjectField($"[{i}]", _donor.actions[i], typeof(Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction), false);
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply To Selected (Scene)", GUILayout.Height(26)))
                {
                    int count = 0;
                    foreach (var go in Selection.gameObjects)
                    {
                        if (go == null) continue;
                        var ai = go.GetComponent<Runtime.AI>();
                        if (ai == null) continue;
                        Undo.RecordObject(ai, "Apply Donor Prefab");
                        CopyDonorToAI(ai);
                        EditorUtility.SetDirty(ai);
                        count++;
                    }
                    ShowNotify($"Applied to {count} selected AI");
                }

                if (GUILayout.Button("Apply To All (Scene)", GUILayout.Height(26)))
                {
                    int count = 0;
                    foreach (var ai in _cache)
                    {
                        if (ai == null) continue;
                        Undo.RecordObject(ai, "Apply Donor Prefab");
                        CopyDonorToAI(ai);
                        EditorUtility.SetDirty(ai);
                        count++;
                    }
                    ShowNotify($"Applied to {count} AI in scene");
                }

                if (GUILayout.Button("Apply To Prefabs (Folder)", GUILayout.Height(26)))
                {
#if UNITY_EDITOR
                    int count = 0;
                    var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_FOLDER });
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var contents = PrefabUtility.LoadPrefabContents(path);
                        if (contents == null) continue;
                        var ai = contents.GetComponentInChildren<Runtime.AI>(true);
                        if (ai != null)
                        {
                            CopyDonorToAI(ai);
                            PrefabUtility.SaveAsPrefabAsset(contents, path);
                            count++;
                        }
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                    ShowNotify($"Applied to {count} prefabs");
#endif
                }
            }
        }

        private void RebuildDonorSnapshot()
        {
#if UNITY_EDITOR
            _donor = new DonorSnapshot();
            _donor.has = false;
            _donorValid = false;
            _donorPath = null;
            if (_donorPrefab == null) return;
            _donorPath = AssetDatabase.GetAssetPath(_donorPrefab);
            if (string.IsNullOrEmpty(_donorPath)) return; // должно быть asset
            var contents = PrefabUtility.LoadPrefabContents(_donorPath);
            if (contents == null) return;
            var ai = contents.GetComponentInChildren<Runtime.AI>(true);
            if (ai != null)
            {
                _donor.hp = ai.Health;
                _donor.detect = ai.DetectionRange;
                _donor.visRange = ai.VisionRange;
                _donor.visAngle = ai.VisionAngle;
                _donor.melee = ai.MeleeAttackRange;
                _donor.ranged = ai.RangedAttackRange;
                _donor.searchInt = ai.TargetSearchInterval;
#if UNITY_EDITOR
                // Считать actions через SerializedObject, если публичного массива нет
                try
                {
                    var so = new SerializedObject(ai);
                    var ap = so.FindProperty("actions");
                    if (ap != null)
                    {
                        var list = new System.Collections.Generic.List<Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction>();
                        for (int i = 0; i < ap.arraySize; i++)
                        {
                            var el = ap.GetArrayElementAtIndex(i).objectReferenceValue as Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction;
                            if (el != null) list.Add(el);
                        }
                        _donor.actions = list.ToArray();
                    }
                }
                catch { }
#endif
                _donor.has = true;
                _donorValid = true;
            }
            PrefabUtility.UnloadPrefabContents(contents);
#endif
        }

        private void CopyDonorToAI(Runtime.AI ai)
        {
            if (ai == null || !_donor.has) return;
            ai.InitializeParameters(
                moveSpd: ai.MoveSpeed, // не трогаем скорость, если управляется агентом
                rotSpd: ai.RotationSpeed,
                detectRange: _donor.detect,
                meleeRange: _donor.melee,
                rangedRange: _donor.ranged,
                visAngle: _donor.visAngle,
                visRange: _donor.visRange,
                hp: _donor.hp,
                targetSearchInt: _donor.searchInt
            );
            if (_donor.actions != null && _donor.actions.Length > 0)
            {
                ai.SetActions(_donor.actions);
            }
        }
        private DonorSnapshot _donor;

        // Batch tab state
        private enum BatchSource { Selected, Scene, Prefabs }
        private BatchSource _batchSource = BatchSource.Selected;
        private Dictionary<int, bool> _batchSelection = new Dictionary<int, bool>();
        private const string PREFABS_FOLDER = "Assets/Black Orbit/Prefabs/AI/Prefabs";
        private bool _batchUseDonor = false;

        // Actions tab state
        private const string ACTIONS_FOLDER = "Assets/Black Orbit/GameData/AI/Actions";
        private Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction[] _allActions;
        private string _actionsSearch = string.Empty;
        private AIProfile GetTargetProfileForActions()
        {
            if (_selectedProfileIndex >= 0 && _selectedProfileIndex < _profiles.Count) return _profiles[_selectedProfileIndex];
            return _profile; // fallback на выбранный в тулбаре
        }
 // =====================
        // ACTIONS TAB
        // =====================
        private void DrawActionsTab()
        {
            var targetProfile = GetTargetProfileForActions();
            using (new EditorGUILayout.HorizontalScope())
            {
                // Left: pick profile if none
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(300)))
                {
                    EditorGUILayout.LabelField("Target Profile", EditorStyles.boldLabel);
                    targetProfile = (AIProfile)EditorGUILayout.ObjectField(new GUIContent("Profile", "Профиль для редактирования списка действий"), targetProfile, typeof(AIProfile), false);
                    if (targetProfile == null)
                    {
                        EditorGUILayout.HelpBox("Выберите профиль слева (вкладка Profiles) или укажите здесь, чтобы редактировать набор действий.", MessageType.Info);
                    }

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Available Actions", EditorStyles.boldLabel);
                    _actionsSearch = EditorGUILayout.TextField(new GUIContent("Search", "Фильтр по имени ассета"), _actionsSearch);
                    EnsureAllActionsLoaded();

                    var filtered = string.IsNullOrEmpty(_actionsSearch)
                        ? _allActions
                        : _allActions.Where(a => a != null && a.name.ToLowerInvariant().Contains(_actionsSearch.ToLowerInvariant())).ToArray();

                    using (var sv = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(260)))
                    {
                        if (filtered == null || filtered.Length == 0)
                        {
                            EditorGUILayout.HelpBox("Действия не найдены в папке: " + ACTIONS_FOLDER, MessageType.Info);
                        }
                        else
                        {
                            foreach (var act in filtered)
                            {
                                if (act == null) continue;
                                using (new EditorGUILayout.HorizontalScope("box"))
                                {
                                    EditorGUILayout.ObjectField(act, typeof(Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction), false);
                                    using (new EditorGUI.DisabledScope(targetProfile == null))
                                    {
                                        if (GUILayout.Button("Add", GUILayout.Width(60))) AddActionToProfile(targetProfile, act);
                                    }
                                }
                            }
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Profile Actions", EditorStyles.boldLabel);
                    if (targetProfile == null)
                    {
                        EditorGUILayout.HelpBox("Нет выбранного профиля.", MessageType.Warning);
                    }
                    else
                    {
                        var list = targetProfile.actions != null ? targetProfile.actions.ToList() : new System.Collections.Generic.List<Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction>();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Remove All", GUILayout.Width(100))) { list.Clear(); SaveProfileActions(targetProfile, list); }
                            if (_allActions != null && GUILayout.Button("Add All", GUILayout.Width(100))) { list = _allActions.Where(a => a != null).ToList(); SaveProfileActions(targetProfile, list); }
                            GUILayout.FlexibleSpace();
                        }

                        EditorGUILayout.Space(4);
                        if (list.Count == 0)
                        {
                            EditorGUILayout.HelpBox("В профиле нет действий. Добавьте слева.", MessageType.None);
                        }
                        else
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                var act = list[i];
                                using (new EditorGUILayout.HorizontalScope("box"))
                                {
                                    EditorGUILayout.ObjectField($"[{i}]", act, typeof(Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction), false);
                                    if (GUILayout.Button("▲", GUILayout.Width(28)) && i > 0)
                                    {
                                        (list[i - 1], list[i]) = (list[i], list[i - 1]);
                                        SaveProfileActions(targetProfile, list);
                                    }
                                    if (GUILayout.Button("▼", GUILayout.Width(28)) && i < list.Count - 1)
                                    {
                                        (list[i + 1], list[i]) = (list[i], list[i + 1]);
                                        SaveProfileActions(targetProfile, list);
                                    }
                                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                                    {
                                        list.RemoveAt(i);
                                        SaveProfileActions(targetProfile, list);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void EnsureAllActionsLoaded()
        {
#if UNITY_EDITOR
            if (_allActions != null && _allActions.Length > 0) return;
            var guids = AssetDatabase.FindAssets("t:Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction", new[] { ACTIONS_FOLDER });
            var list = new System.Collections.Generic.List<Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction>(guids.Length);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction>(path);
                if (a != null) list.Add(a);
            }
            _allActions = list.OrderBy(a => a.name).ToArray();
#endif
        }

        private void AddActionToProfile(AIProfile profile, Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction action)
        {
            if (profile == null || action == null) return;
#if UNITY_EDITOR
            var list = profile.actions != null ? profile.actions.ToList() : new System.Collections.Generic.List<Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction>();
            if (!list.Contains(action))
            {
                list.Add(action);
                SaveProfileActions(profile, list);
            }
#endif
        }

        private void SaveProfileActions(AIProfile profile, System.Collections.Generic.List<Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction> list)
        {
#if UNITY_EDITOR
            var so = new SerializedObject(profile);
            var prop = so.FindProperty("actions");
            if (prop != null)
            {
                prop.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }
#endif
        }
        

        [MenuItem("Tools/AI/AI Manager")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<AIManagerWindow>(false, "AI Manager", true);
            wnd.minSize = new Vector2(520, 380);
            wnd.RefreshList();
            wnd.Show();
        }

        private void OnEnable()
        {
            RefreshPrefabs();
        }

        private void OnHierarchyChange()
        {
            Repaint();
        }

        private void OnGUI()
        {
            DrawTopBar();
            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;
                DrawPrefabsList();
            }
        }
        private void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(new GUIContent("Refresh", "Перечитать префабы"), EditorStyles.toolbarButton, GUILayout.Width(80))) RefreshPrefabs();
                _expandAll = GUILayout.Toggle(_expandAll, new GUIContent("Expand All"), EditorStyles.toolbarButton, GUILayout.Width(90));
                GUILayout.Space(6);
                GUILayout.Label("Search:", GUILayout.Width(48));
                _search = GUILayout.TextField(_search ?? string.Empty, EditorStyles.toolbarTextField, GUILayout.Width(220));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Open Squad Panel"), EditorStyles.toolbarButton, GUILayout.Width(120))) AISquadCommandPanel.ShowWindow();
            }
        }

        private void DrawInlineHelp() {}

        private void DrawOverview()
        {
            if (_cache == null || _cache.Count == 0)
            {
                EditorGUILayout.HelpBox("AI на сцене не найдены. Поместите префабы с компонентом Runtime.AI на сцену.", MessageType.Info);
                return;
            }

            int shown = 0;
            foreach (var ai in _cache)
            {
                if (ai == null) continue;
                if (_onlyActive && !ai.gameObject.activeInHierarchy) continue;
                if (!PassesFilter(ai)) continue;

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(new GUIContent(ai.name, "Объект с Runtime.AI"), ai, typeof(Runtime.AI), true);
                        GUILayout.FlexibleSpace();
                        if (_profile != null)
                        {
                            if (GUILayout.Button(new GUIContent("Apply", "Применить выбранный профиль к этому боту"), GUILayout.Width(70))) ApplyProfile(ai);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(new GUIContent($"Faction: {ai.faction?.factionName ?? "None"}", "Фракция бота"));
                        GUILayout.Label(new GUIContent($"HP: {ai.Health:F0}", "Здоровье"));
                        GUILayout.Label(new GUIContent($"Detect: {ai.DetectionRange:F0} | Vision: {ai.VisionRange:F0}@{ai.VisionAngle:F0}°", "Дальность обнаружения и параметры поля зрения"));
                        GUILayout.Label(new GUIContent($"Melee/Ranged: {ai.MeleeAttackRange:F1}/{ai.RangedAttackRange:F1}", "Дистанции атаки ближнего/дальнего боя"));
                    }

#if UNITY_EDITOR
                    if (ai.Actions != null && ai.Actions.Length > 0)
                    {
                        EditorGUILayout.LabelField("Actions:");
                        using (new EditorGUI.IndentLevelScope())
                        {
                            for (int i = 0; i < ai.Actions.Length; i++)
                            {
                                var act = ai.Actions[i];
                                EditorGUILayout.ObjectField($"[{i}]", act, typeof(Black_Orbit.Scripts.AI.ScriptableObjects.Actions.UtilityAction), false);
                            }
                        }
                    }
#endif
                }

                shown++;
            }

            if (shown == 0)
            {
                EditorGUILayout.HelpBox("Ничего не подходит под фильтры. Попробуйте очистить Filter или отключить Only Active.", MessageType.None);
            }
        }

        private void DrawAIPrefabUI(AIBlock ai)
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

        private void DrawSquadPrefabUI(PrefabEntry p)
        {
            // Загружаем содержимое префаба для отображения Squad-полей и членов
            var loaded = LoadSquad(p.path);
            if (loaded.members == null || loaded.members.Count == 0)
            {
                EditorGUILayout.HelpBox("В префабе сквада не найдены члены с Runtime.AI.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Squad", EditorStyles.boldLabel);
            loaded.squad.squadName = EditorGUILayout.TextField("Name", loaded.squad.squadName);
            loaded.squad.searchRadius = EditorGUILayout.FloatField("Search Radius", loaded.squad.searchRadius);
            loaded.squad.coordinationInterval = EditorGUILayout.FloatField("Coordination Interval", loaded.squad.coordinationInterval);
            loaded.squad.minMembersForCoordination = EditorGUILayout.IntField("Min Members", loaded.squad.minMembersForCoordination);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Members", EditorStyles.boldLabel);
            for (int i = 0; i < loaded.members.Count; i++)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField($"[{i}] {loaded.members[i].name}", EditorStyles.boldLabel);
                    DrawAIPrefabUI(loaded.members[i]);
                }
            }

            // Сохраняем изменения обратно в Entry (снапшот)
            // Упростим: сохраним только первый член в p.ai как референс для Apply, а ApplySquad будет пересобирать всю структуру из loaded
            p.ai = loaded.members[0];
            // Кнопка Apply на карточке уже вызовет ApplyPrefabSnapshot, который распишет все
        }

        private void ShowNotify(string msg) => ShowNotification(new GUIContent(msg));

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
                var entry = new PrefabEntry { path = path, asset = asset, foldout = false };
                // определяем тип
                var contents = PrefabUtility.LoadPrefabContents(path);
                if (contents != null)
                {
                    var squad = contents.GetComponentInChildren<Runtime.AISquad>(true);
                    entry.isSquad = squad != null;
                    if (!entry.isSquad)
                    {
                        var ai = contents.GetComponentInChildren<Runtime.AI>(true);
                        if (ai != null)
                        {
                            entry.ai = ToAIBlock(ai);
                        }
                    }
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                _prefabs.Add(entry);
            }
#endif
        }

        // =====================
        // BATCH TAB
        // =====================
        private class DiffRow
        {
            public Runtime.AI ai;
            public bool isPrefab;
            public string prefabPath;
            public Object displayObj; // what to show in UI (ai GO or prefab asset)
            public bool selected;
            public float curHP, newHP;
            public float curDetect, newDetect;
            public float curVisRange, newVisRange;
            public float curVisAngle, newVisAngle;
            public float curMelee, newMelee;
            public float curRanged, newRanged;
            public float curSearchInt, newSearchInt;
        }

        private List<DiffRow> _batchRows = new List<DiffRow>(128);

        private void DrawBatchTab()
        {
            EditorGUILayout.LabelField("Batch Apply (Preview)", EditorStyles.boldLabel);
            if (_profile == null && !(_batchUseDonor && _donor.has))
            {
                EditorGUILayout.HelpBox("Нет источника параметров. Либо выберите Profile сверху, либо укажите Donor на вкладке Prefab и включите Use Donor Prefab.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope("box"))
            {
                _batchSource = (BatchSource)EditorGUILayout.EnumPopup(new GUIContent("Source", "Откуда брать список AI: выделение или вся сцена"), _batchSource, GUILayout.Width(260));
                if (_batchSource == BatchSource.Scene)
                {
                    _factionFilter = EditorGUILayout.TextField(new GUIContent("Faction contains", "Подстрока в названии фракции"), _factionFilter);
                    _tagFilter = EditorGUILayout.TextField(new GUIContent("Tag", "Совпадение с gameObject.tag"), _tagFilter);
                }
                _batchUseDonor = EditorGUILayout.ToggleLeft(new GUIContent("Use Donor Prefab", "Взять значения для 'после' из донор-префаба (вкладка Prefab) вместо профиля"), _batchUseDonor, GUILayout.Width(160));
                if (GUILayout.Button(new GUIContent("Build Preview", "Перестроить список и посчитать diff"), GUILayout.Width(140)))
                {
                    BuildBatchList();
                }
            }

            if (_batchRows.Count == 0)
            {
                EditorGUILayout.HelpBox("Список пуст. Нажмите Build Preview, чтобы собрать цели для применения.", MessageType.None);
                return;
            }

            // Header
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Apply", GUILayout.Width(44));
                GUILayout.Label("AI/Prefab", GUILayout.Width(180));
                GUILayout.Label("HP", GUILayout.Width(90));
                GUILayout.Label("Detect", GUILayout.Width(90));
                GUILayout.Label("VisionR", GUILayout.Width(90));
                GUILayout.Label("VisionA", GUILayout.Width(90));
                GUILayout.Label("Melee", GUILayout.Width(90));
                GUILayout.Label("Ranged", GUILayout.Width(90));
                GUILayout.Label("SearchInt", GUILayout.Width(90));
            }

            int selectedCount = 0;
            foreach (var row in _batchRows)
            {
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    bool sel = EditorGUILayout.Toggle(row.selected, GUILayout.Width(44));
                    if (sel != row.selected)
                    {
                        row.selected = sel;
                        _batchSelection[GetRowKey(row)] = sel;
                    }
                    if (row.isPrefab)
                        EditorGUILayout.ObjectField(row.displayObj, typeof(GameObject), false, GUILayout.Width(180));
                    else
                        EditorGUILayout.ObjectField(row.ai, typeof(Runtime.AI), true, GUILayout.Width(180));
                    DrawDiffCell(row.curHP, row.newHP);
                    DrawDiffCell(row.curDetect, row.newDetect);
                    DrawDiffCell(row.curVisRange, row.newVisRange);
                    DrawDiffCell(row.curVisAngle, row.newVisAngle);
                    DrawDiffCell(row.curMelee, row.newMelee);
                    DrawDiffCell(row.curRanged, row.newRanged);
                    DrawDiffCell(row.curSearchInt, row.newSearchInt);
                }
                if (row.selected) selectedCount++;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All", GUILayout.Width(100)))
                {
                    foreach (var r in _batchRows) { r.selected = true; _batchSelection[GetRowKey(r)] = true; }
                }
                if (GUILayout.Button("Select None", GUILayout.Width(100)))
                {
                    foreach (var r in _batchRows) { r.selected = false; _batchSelection[GetRowKey(r)] = false; }
                }
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(selectedCount == 0))
                {
                    if (GUILayout.Button(new GUIContent($"Apply ({selectedCount})", "Применить профиль к выбранным строкам"), GUILayout.Width(140)))
                    {
                        int applied = 0;
                        foreach (var r in _batchRows)
                        {
                            if (!r.selected) continue;
                            if (r.isPrefab)
                            {
#if UNITY_EDITOR
                                if (string.IsNullOrEmpty(r.prefabPath)) continue;
                                var contents = PrefabUtility.LoadPrefabContents(r.prefabPath);
                                if (contents != null)
                                {
                                    var ai = contents.GetComponentInChildren<Runtime.AI>(true);
                                    if (ai != null)
                                    {
                                        if (_batchUseDonor && _donor.has) CopyDonorToAI(ai);
                                        else _profile.ApplyTo(ai, applyActions: true, applyDebug: true);
                                        PrefabUtility.SaveAsPrefabAsset(contents, r.prefabPath);
                                        applied++;
                                    }
                                    PrefabUtility.UnloadPrefabContents(contents);
                                }
#endif
                            }
                            else
                            {
                                if (r.ai == null) continue;
                                Undo.RecordObject(r.ai, "Batch Apply AI Profile");
                                if (_batchUseDonor && _donor.has) CopyDonorToAI(r.ai);
                                else _profile.ApplyTo(r.ai, applyActions: true, applyDebug: true);
                                EditorUtility.SetDirty(r.ai);
                                applied++;
                            }
                        }
                        ShowNotify($"Applied to {applied} AI");
                        BuildBatchList(); // обновить after значения
                    }
                }
            }
        }

        private void DrawDiffCell(float before, float after)
        {
            Color old = GUI.color;
            if (Mathf.Approximately(before, after)) GUI.color = Color.gray;
            else GUI.color = (after > before ? new Color(0.65f, 0.9f, 0.7f) : new Color(0.95f, 0.7f, 0.7f));
            GUILayout.Label($"{before:F1} → {after:F1}", GUILayout.Width(90));
            GUI.color = old;
        }

        private int GetRowKey(DiffRow row)
        {
            if (row == null) return 0;
            if (row.isPrefab && row.displayObj != null) return row.displayObj.GetInstanceID();
            if (row.ai != null) return row.ai.GetInstanceID();
            return 0;
        }

        private void BuildBatchList()
        {
            _batchRows.Clear();

            var targets = new List<Runtime.AI>();
            if (_batchSource == BatchSource.Selected)
            {
                foreach (var go in Selection.gameObjects)
                {
                    if (go == null) continue;
                    var ai = go.GetComponent<Runtime.AI>();
                    if (ai != null) targets.Add(ai);
                }
            }
            else if (_batchSource == BatchSource.Scene)
            {
                foreach (var ai in _cache)
                {
                    if (ai == null) continue;
                    if (!string.IsNullOrEmpty(_tagFilter) && ai.gameObject.tag != _tagFilter) continue;
                    if (!string.IsNullOrEmpty(_factionFilter))
                    {
                        var fac = ai.faction != null ? ai.faction.factionName : null;
                        if (string.IsNullOrEmpty(fac) || !fac.ToLowerInvariant().Contains(_factionFilter.ToLowerInvariant())) continue;
                    }
                    targets.Add(ai);
                }
            }
            else // Prefabs
            {
#if UNITY_EDITOR
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_FOLDER });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefabAsset == null) continue;
                    var contents = PrefabUtility.LoadPrefabContents(path);
                    if (contents == null) continue;
                    var ai = contents.GetComponentInChildren<Runtime.AI>(true);
                    if (ai == null)
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                        continue;
                    }
                    // Build row from prefab values
                    var row = new DiffRow
                    {
                        ai = null,
                        isPrefab = true,
                        prefabPath = path,
                        displayObj = prefabAsset,
                        curHP = ai.Health,
                        curDetect = ai.DetectionRange,
                        curVisRange = ai.VisionRange,
                        curVisAngle = ai.VisionAngle,
                        curMelee = ai.MeleeAttackRange,
                        curRanged = ai.RangedAttackRange,
                        curSearchInt = ai.TargetSearchInterval,
                        newHP = _profile.initialHP,
                        newDetect = _profile.detectionRange,
                        newVisRange = _profile.visionRange,
                        newVisAngle = _profile.visionAngle,
                        newMelee = _profile.meleeAttackRange,
                        newRanged = _profile.rangedAttackRange,
                        newSearchInt = _profile.targetSearchInterval
                    };
                    bool prevSel = false;
                    _batchSelection.TryGetValue(prefabAsset.GetInstanceID(), out prevSel);
                    row.selected = prevSel;
                    _batchRows.Add(row);
                    PrefabUtility.UnloadPrefabContents(contents);
                }
#endif
                // Prefab path route ends here. Bail out early to avoid duplicating rows below.
                return;
            }

            foreach (var ai in targets)
            {
                var row = new DiffRow { ai = ai, isPrefab = false, displayObj = ai != null ? (Object)ai.gameObject : null };
                // current
                row.curHP = ai.Health;
                row.curDetect = ai.DetectionRange;
                row.curVisRange = ai.VisionRange;
                row.curVisAngle = ai.VisionAngle;
                row.curMelee = ai.MeleeAttackRange;
                row.curRanged = ai.RangedAttackRange;
                row.curSearchInt = ai.TargetSearchInterval;
                // new (from donor or profile)
                if (_batchUseDonor && _donor.has)
                {
                    row.newHP = _donor.hp;
                    row.newDetect = _donor.detect;
                    row.newVisRange = _donor.visRange;
                    row.newVisAngle = _donor.visAngle;
                    row.newMelee = _donor.melee;
                    row.newRanged = _donor.ranged;
                    row.newSearchInt = _donor.searchInt;
                }
                else if (_profile != null)
                {
                    row.newHP = _profile.initialHP;
                    row.newDetect = _profile.detectionRange;
                    row.newVisRange = _profile.visionRange;
                    row.newVisAngle = _profile.visionAngle;
                    row.newMelee = _profile.meleeAttackRange;
                    row.newRanged = _profile.rangedAttackRange;
                    row.newSearchInt = _profile.targetSearchInterval;
                }
                else
                {
                    // Fallback: если источника нет, чтобы не падать — показываем без изменений
                    row.newHP = row.curHP;
                    row.newDetect = row.curDetect;
                    row.newVisRange = row.curVisRange;
                    row.newVisAngle = row.curVisAngle;
                    row.newMelee = row.curMelee;
                    row.newRanged = row.curRanged;
                    row.newSearchInt = row.curSearchInt;
                }

                // selection state
                bool prevSel = false;
                _batchSelection.TryGetValue(ai.GetInstanceID(), out prevSel);
                row.selected = prevSel;

                _batchRows.Add(row);
            }
        }

        // =====================
        // PROFILES TAB
        // =====================
        private void DrawProfilesTab()
        {
            if (_profiles == null || _profiles.Count == 0)
            {
                RefreshProfiles();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
                {
                    EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
                    if (GUILayout.Button(new GUIContent("Create", "Создать новый профиль AIProfile"), GUILayout.Height(22))) CreateProfile();
                    using (new EditorGUI.DisabledScope(_selectedProfileIndex < 0 || _selectedProfileIndex >= _profiles.Count))
                    {
                        if (GUILayout.Button(new GUIContent("Duplicate", "Создать копию выбранного профиля"), GUILayout.Height(22))) DuplicateProfile();
                        if (GUILayout.Button(new GUIContent("Delete", "Удалить выбранный профиль"), GUILayout.Height(22))) DeleteProfile();
                    }
                    EditorGUILayout.Space(6);

                    // List
                    for (int i = 0; i < _profiles.Count; i++)
                    {
                        var pr = _profiles[i];
                        if (pr == null) continue;
                        using (new EditorGUILayout.HorizontalScope("box"))
                        {
                            bool selected = i == _selectedProfileIndex;
                            if (GUILayout.Toggle(selected, new GUIContent(pr.name, "Профиль AI"), "Button"))
                                _selectedProfileIndex = i;
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Apply / Assign", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Шаги: 1) выберите профиль; 2) задайте фильтры (по желанию); 3) нажмите одну из кнопок применения ниже.", MessageType.None);

                    // Current profile selection
                    var currentProfile = (_selectedProfileIndex >= 0 && _selectedProfileIndex < _profiles.Count) ? _profiles[_selectedProfileIndex] : null;
                    EditorGUI.BeginChangeCheck();
                    currentProfile = (AIProfile)EditorGUILayout.ObjectField("Selected Profile", currentProfile, typeof(AIProfile), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _selectedProfileIndex = _profiles.IndexOf(currentProfile);
                        if (_selectedProfileIndex < 0 && currentProfile != null)
                        {
                            _profiles.Add(currentProfile);
                            _selectedProfileIndex = _profiles.Count - 1;
                        }
                    }

                    EditorGUILayout.Space(6);
                    _factionFilter = EditorGUILayout.TextField(new GUIContent("Faction filter (contains)", "Подстрока в названии фракции"), _factionFilter);
                    _tagFilter = EditorGUILayout.TextField(new GUIContent("Tag filter", "Совпадение с gameObject.tag"), _tagFilter);

                    using (new EditorGUI.DisabledScope(currentProfile == null))
                    {
                        if (GUILayout.Button(new GUIContent("Assign Binder To Selected + Set Profile", "Добавить AIProfileBinder на выделенные и записать выбранный профиль"), GUILayout.Height(24)))
                            AssignBinderToSelection(currentProfile);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(new GUIContent("Apply To Selected", "Применить профиль к выделенным ботам"), GUILayout.Height(24)))
                                ApplyProfileToSelection(currentProfile);
                            if (GUILayout.Button(new GUIContent("Apply To All (Scene)", "Применить профиль ко всем ботам на текущей сцене"), GUILayout.Height(24)))
                                ApplyProfileToAll(currentProfile);
                        }

                        if (GUILayout.Button(new GUIContent("Apply To Filtered (Scene)", "Применить профиль только к ботам, подходящим под фильтры"), GUILayout.Height(24)))
                            ApplyProfileToFiltered(currentProfile, _factionFilter, _tagFilter);
                    }
                }
            }
        }

        private void RefreshProfiles()
        {
#if UNITY_EDITOR
            _profiles.Clear();
            var guids = AssetDatabase.FindAssets("t:" + nameof(AIProfile));
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var p = AssetDatabase.LoadAssetAtPath<AIProfile>(path);
                if (p != null) _profiles.Add(p);
            }
            _profiles = _profiles.OrderBy(p => p.name).ToList();
            _selectedProfileIndex = _profiles.Count > 0 ? 0 : -1;
#endif
        }

        private void CreateProfile()
        {
#if UNITY_EDITOR
            string basePath = "Assets/Black Orbit/GameData/AI/Profiles";
            if (!AssetDatabase.IsValidFolder("Assets/Black Orbit/GameData")) AssetDatabase.CreateFolder("Assets/Black Orbit", "GameData");
            if (!AssetDatabase.IsValidFolder("Assets/Black Orbit/GameData/AI")) AssetDatabase.CreateFolder("Assets/Black Orbit/GameData", "AI");
            if (!AssetDatabase.IsValidFolder(basePath)) AssetDatabase.CreateFolder("Assets/Black Orbit/GameData/AI", "Profiles");
            string path = AssetDatabase.GenerateUniqueAssetPath(basePath + "/AIProfile.asset");
            var p = CreateInstance<AIProfile>();
            AssetDatabase.CreateAsset(p, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshProfiles();
#endif
        }

        private void DuplicateProfile()
        {
#if UNITY_EDITOR
            if (_selectedProfileIndex < 0 || _selectedProfileIndex >= _profiles.Count) return;
            var src = _profiles[_selectedProfileIndex];
            string path = AssetDatabase.GetAssetPath(src);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(path.Replace(".asset", " Copy.asset"));
            AssetDatabase.CopyAsset(path, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshProfiles();
#endif
        }

        private void DeleteProfile()
        {
#if UNITY_EDITOR
            if (_selectedProfileIndex < 0 || _selectedProfileIndex >= _profiles.Count) return;
            var src = _profiles[_selectedProfileIndex];
            string path = AssetDatabase.GetAssetPath(src);
            if (EditorUtility.DisplayDialog("Delete Profile", $"Delete {src.name}?", "Yes", "No"))
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RefreshProfiles();
            }
#endif
        }

        private void AssignBinderToSelection(AIProfile pr)
        {
            var objs = Selection.gameObjects;
            int set = 0; int added = 0;
            foreach (var go in objs)
            {
                if (go == null) continue;
                var ai = go.GetComponent<Runtime.AI>();
                if (ai == null) continue;
                var binder = go.GetComponent<Black_Orbit.Scripts.AI.Runtime.Core.AIProfileBinder>();
                if (binder == null)
                {
                    Undo.AddComponent<Black_Orbit.Scripts.AI.Runtime.Core.AIProfileBinder>(go);
                    binder = go.GetComponent<Black_Orbit.Scripts.AI.Runtime.Core.AIProfileBinder>();
                    added++;
                }
                Undo.RecordObject(binder, "Set AI Profile on Binder");
                binder.profile = pr;
                EditorUtility.SetDirty(binder);
                set++;
            }
            ShowNotify($"Binder set on {set} (added {added})");
        }

        private void ApplyProfileToSelection(AIProfile pr)
        {
            var objs = Selection.gameObjects;
            int count = 0;
            foreach (var go in objs)
            {
                if (go == null) continue;
                var ai = go.GetComponent<Runtime.AI>();
                if (ai == null) continue;
                Undo.RecordObject(ai, "Apply AI Profile");
                pr.ApplyTo(ai, applyActions: true, applyDebug: true);
                EditorUtility.SetDirty(ai);
                count++;
            }
            ShowNotify($"Applied to {count} selected AI");
        }

        private void ApplyProfileToAll(AIProfile pr)
        {
            int count = 0;
            foreach (var ai in _cache)
            {
                if (ai == null) continue;
                Undo.RecordObject(ai, "Apply AI Profile");
                pr.ApplyTo(ai, applyActions: true, applyDebug: true);
                EditorUtility.SetDirty(ai);
                count++;
            }
            ShowNotify($"Applied to {count} AI in scene");
        }

        private void ApplyProfileToFiltered(AIProfile pr, string factionContains, string tag)
        {
            string f = string.IsNullOrEmpty(factionContains) ? null : factionContains.ToLowerInvariant();
            int count = 0;
            foreach (var ai in _cache)
            {
                if (ai == null) continue;
                if (!string.IsNullOrEmpty(tag) && (ai.gameObject.tag != tag)) continue;
                if (!string.IsNullOrEmpty(f))
                {
                    var fac = ai.faction != null ? ai.faction.factionName : null;
                    if (string.IsNullOrEmpty(fac) || !fac.ToLowerInvariant().Contains(f)) continue;
                }
                Undo.RecordObject(ai, "Apply AI Profile");
                pr.ApplyTo(ai, applyActions: true, applyDebug: true);
                EditorUtility.SetDirty(ai);
                count++;
            }
            ShowNotify($"Applied to {count} filtered AI");
        }

        private void AddBinderToSelection()
        {
            var objs = Selection.gameObjects;
            int added = 0;
            foreach (var go in objs)
            {
                if (go == null) continue;
                var ai = go.GetComponent<Runtime.AI>();
                if (ai == null) continue;
                var binder = go.GetComponent<Black_Orbit.Scripts.AI.Runtime.Core.AIProfileBinder>();
                if (binder == null)
                {
                    Undo.AddComponent<Black_Orbit.Scripts.AI.Runtime.Core.AIProfileBinder>(go);
                    binder = go.GetComponent<Black_Orbit.Scripts.AI.Runtime.Core.AIProfileBinder>();
                    added++;
                }
                if (binder != null && _profile != null)
                {
                    Undo.RecordObject(binder, "Set AI Profile on Binder");
                    binder.profile = _profile;
                    EditorUtility.SetDirty(binder);
                }
            }
            if (added > 0) ShowNotify($"Added binder to {added} object(s)");
        }

        private void EmitNoiseFromSceneCam() {}

        private void RefreshList()
        {
#if UNITY_2022_2_OR_NEWER
            var all = Object.FindObjectsByType<Runtime.AI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var all = Object.FindObjectsOfType<Runtime.AI>();
#endif
            _cache.Clear();
            _cache.AddRange(all);
        }

        private void ShowNotify(string msg)
        {
            ShowNotification(new GUIContent(msg));
        }
    }
}
