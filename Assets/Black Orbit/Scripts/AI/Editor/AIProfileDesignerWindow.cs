using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Actions.Combat;
using Black_Orbit.Scripts.AI.Runtime.Actions.Movement;
using Black_Orbit.Scripts.AI.Runtime.Actions.Tactics;
using Black_Orbit.Scripts.AI.Runtime.Configs;
using Black_Orbit.Scripts.AI.Runtime.Controller;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Domains;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Editor
{
    public class AIProfileDesignerWindow : EditorWindow
    {
        private readonly List<AIProfile> _profiles = new();
        private SerializedObject _serializedProfile;
        private AIProfile _selectedProfile;
        private Vector2 _profilesScroll;
        private Vector2 _detailsScroll;
        private string _search = string.Empty;

        private bool _curvesFoldout = true;
        private bool _movementFoldout = true;
        private bool _combatFoldout = true;
        private bool _tacticsFoldout = true;

        private GUIContent _refreshIcon;

        [MenuItem("Black Orbit/AI/Дизайнер профилей AI")]
        public static void Open()
        {
            GetWindow<AIProfileDesignerWindow>("Дизайнер профилей AI");
        }

        private void OnEnable()
        {
            _refreshIcon = EditorGUIUtility.IconContent("Refresh");
            RefreshProfiles();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawProfileList();
                DrawProfileDetails();
            }
        }

        private void DrawProfileList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f)))
            {
                EditorGUILayout.LabelField("Профили", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var searchStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? GUI.skin.textField;
                    var cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton") ?? GUI.skin.button;
                    _search = EditorGUILayout.TextField(_search, searchStyle);
                    if (GUILayout.Button(GUIContent.none, cancelStyle, GUILayout.Width(18f)))
                    {
                        _search = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Создать", "Создать новый профиль AI (ScriptableObject)"), GUILayout.Height(22f)))
                    {
                        CreateProfileAsset();
                    }

                    EditorGUI.BeginDisabledGroup(_selectedProfile == null);
                    if (GUILayout.Button(new GUIContent("Клонировать", "Создать копию выбранного профиля"), GUILayout.Height(22f)))
                    {
                        CloneProfileAsset(_selectedProfile);
                    }
                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button(_refreshIcon, GUILayout.Width(30f), GUILayout.Height(22f)))
                    {
                        RefreshProfiles();
                    }
                }

                _profilesScroll = EditorGUILayout.BeginScrollView(_profilesScroll, GUI.skin.box);
                for (int i = 0; i < _profiles.Count; i++)
                {
                    var profile = _profiles[i];
                    if (profile == null) continue;

                    if (!string.IsNullOrEmpty(_search) && !profile.profileName.ToLowerInvariant().Contains(_search.ToLowerInvariant()))
                        continue;

                    bool isSelected = profile == _selectedProfile;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Toggle(isSelected, profile.profileName, "Button"))
                        {
                            if (!isSelected)
                            {
                                SelectProfile(profile);
                            }
                        }

                        GUILayout.Label(EditorGUIUtility.ObjectContent(profile, typeof(AIProfile)).image,
                            GUILayout.Width(20f), GUILayout.Height(20f));
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawProfileDetails()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_selectedProfile == null || _serializedProfile == null)
                {
                    EditorGUILayout.HelpBox("Выберите или создайте AIProfile для редактирования.", MessageType.Info);
                    return;
                }

                _serializedProfile.Update();

                _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);

                EditorGUILayout.LabelField("Метаданные профиля", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    _serializedProfile.FindProperty("profileName"),
                    new GUIContent("Имя профиля", "Отображаемое имя профиля в инструментах редактора."));
                EditorGUILayout.PropertyField(
                    _serializedProfile.FindProperty("version"),
                    new GUIContent("Версия", "Произвольная версия/тег для отслеживания изменений."));
                EditorGUILayout.Space(6f);

                DrawUtilityCurvesSection();
                EditorGUILayout.Space(4f);

                DrawMovementSection();
                EditorGUILayout.Space(4f);

                DrawCombatSection();
                EditorGUILayout.Space(4f);

                DrawTacticsSection();
                EditorGUILayout.Space(8f);

                DrawSceneIntegration();

                EditorGUILayout.EndScrollView();

                _serializedProfile.ApplyModifiedProperties();
            }
        }

        private void DrawUtilityCurvesSection()
        {
            _curvesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_curvesFoldout, "Кривые полезности");
            if (_curvesFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var curvesProp = _serializedProfile.FindProperty("Curves");
                    DrawCurve(curvesProp.FindPropertyRelative("distanceToTarget"), new GUIContent("Дистанция до цели", "Нормализация расстояния 0..1. 0 = далеко, 1 = близко (в зависимости от формы кривой)."));
                    DrawCurve(curvesProp.FindPropertyRelative("visibility"), new GUIContent("Видимость цели", "Вероятность/степень видимости цели."));
                    DrawCurve(curvesProp.FindPropertyRelative("lowHealth"), new GUIContent("Низкое здоровье", "Чем ниже здоровье, тем выше значение."));
                    DrawCurve(curvesProp.FindPropertyRelative("ammoLow"), new GUIContent("Мало патронов", "Повышается при малом количестве боеприпасов."));
                    DrawCurve(curvesProp.FindPropertyRelative("hasAmmo"), new GUIContent("Есть патроны", "Отражает достаточность боезапаса."));
                    DrawCurve(curvesProp.FindPropertyRelative("coverAvailable"), new GUIContent("Доступно укрытие", "Оценивает наличие укрытий поблизости (см. CoverAvailable)."));
                    DrawCurve(curvesProp.FindPropertyRelative("exploreNeed"), new GUIContent("Необходимость разведки", "Степень потребности исследовать территорию."));
                    DrawCurve(curvesProp.FindPropertyRelative("grenadeRange"), new GUIContent("Дальность гранаты", "Подходит ли дистанция для броска гранаты."));
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCurve(SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.Space(2f);
            var curve = property.animationCurveValue;
            curve = EditorGUILayout.CurveField(label, curve, Color.cyan, new Rect(0f, 0f, 1f, 1f), GUILayout.Height(60f));
            property.animationCurveValue = curve;

            if (curve != null && curve.keys.Length > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawCurveSample(curve, 0f);
                    DrawCurveSample(curve, 0.25f);
                    DrawCurveSample(curve, 0.5f);
                    DrawCurveSample(curve, 0.75f);
                    DrawCurveSample(curve, 1f);
                }
            }
        }

        private static void DrawCurveSample(AnimationCurve curve, float t)
        {
            var value = curve.Evaluate(t);
            EditorGUILayout.LabelField($"t={t:0.00}: {value:0.00}", GUILayout.Width(80f));
        }

        private void DrawMovementSection()
        {
            var movementProp = _serializedProfile.FindProperty("Movement");
            _movementFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_movementFoldout, "Домен Движения");
            if (_movementFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(movementProp.FindPropertyRelative("enabled"), new GUIContent("Включено", "Активировать/деактивировать домен движения."));
                    EditorGUILayout.Space(2f);

                    if (movementProp.FindPropertyRelative("enabled").boolValue)
                    {
                        DrawMovementAction(movementProp.FindPropertyRelative("Explore"), "Разведка (Explore)", "radius");
                        DrawMovementAction(movementProp.FindPropertyRelative("Pursue"), "Преследование (Pursue)", "maxDistance");
                        DrawMovementAction(movementProp.FindPropertyRelative("Flank"), "Окружение (Flank)", "flankDistance", "orderBoost");
                        DrawMovementAction(movementProp.FindPropertyRelative("TakeCover"), "Занять укрытие (Take Cover)", "searchRadius", "minDistanceToTarget");
                    }

                    DrawActionWeightsPreview(DomainId.Movement);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawMovementAction(SerializedProperty prop, string label, params string[] additionalFields)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("enabled"), new GUIContent("Включено", "Активность данного действия."));
                using (new EditorGUI.DisabledScope(!prop.FindPropertyRelative("enabled").boolValue))
                {
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("baseWeight"), new GUIContent("Базовый вес", "Стартовая важность действия до учёта кривых и условий."));
                    foreach (var field in additionalFields)
                    {
                        var child = prop.FindPropertyRelative(field);
                        if (child != null)
                        {
                            EditorGUILayout.PropertyField(child, new GUIContent(ObjectNames.NicifyVariableName(field)));
                        }
                    }
                }
            }
        }

        private void DrawCombatSection()
        {
            var combatProp = _serializedProfile.FindProperty("Combat");
            _combatFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_combatFoldout, "Домен Боя");
            if (_combatFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(combatProp.FindPropertyRelative("enabled"), new GUIContent("Включено", "Активировать/деактивировать домен боя."));
                    EditorGUILayout.Space(2f);

                    if (combatProp.FindPropertyRelative("enabled").boolValue)
                    {
                        DrawCombatAction(combatProp.FindPropertyRelative("Shoot"), "Стрельба (Shoot)", "retreatPenalty", "suppressBoost");
                        DrawCombatAction(combatProp.FindPropertyRelative("Reload"), "Перезарядка (Reload)", "lowThreshold", "highThreshold");
                        DrawCombatAction(combatProp.FindPropertyRelative("ThrowGrenade"), "Граната (Throw Grenade)", "minRange", "maxRange", "cooldown");
                    }

                    DrawActionWeightsPreview(DomainId.Combat);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCombatAction(SerializedProperty prop, string label, params string[] additionalFields)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("enabled"), new GUIContent("Включено", "Активность данного действия."));
                using (new EditorGUI.DisabledScope(!prop.FindPropertyRelative("enabled").boolValue))
                {
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("baseWeight"), new GUIContent("Базовый вес", "Стартовая важность действия до учёта кривых и условий."));
                    foreach (var field in additionalFields)
                    {
                        var child = prop.FindPropertyRelative(field);
                        if (child != null)
                        {
                            EditorGUILayout.PropertyField(child, new GUIContent(ObjectNames.NicifyVariableName(field)));
                        }
                    }
                }
            }
        }

        private void DrawTacticsSection()
        {
            var tacticsProp = _serializedProfile.FindProperty("Tactics");
            _tacticsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_tacticsFoldout, "Домен Тактики");
            if (_tacticsFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(tacticsProp.FindPropertyRelative("enabled"), new GUIContent("Включено", "Активировать/деактивировать домен тактики."));
                    EditorGUILayout.Space(2f);

                    if (tacticsProp.FindPropertyRelative("enabled").boolValue)
                    {
                        DrawTacticsAction(tacticsProp.FindPropertyRelative("RetreatDecision"), "Решение об отступлении", "critical", "max", "retreatDistance", "preferCover");
                        DrawTacticsAction(tacticsProp.FindPropertyRelative("RetreatMove"), "Отступление (движение)");
                    }

                    DrawActionWeightsPreview(DomainId.Tactics);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTacticsAction(SerializedProperty prop, string label, params string[] additionalFields)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("enabled"), new GUIContent("Включено", "Активность данного действия."));
                using (new EditorGUI.DisabledScope(!prop.FindPropertyRelative("enabled").boolValue))
                {
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("baseWeight"), new GUIContent("Базовый вес", "Стартовая важность действия до учёта кривых и условий."));
                    foreach (var field in additionalFields)
                    {
                        var child = prop.FindPropertyRelative(field);
                        if (child != null)
                        {
                            EditorGUILayout.PropertyField(child, new GUIContent(ObjectNames.NicifyVariableName(field)));
                        }
                    }
                }
            }
        }

        private void DrawActionWeightsPreview(DomainId domain)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Предпросмотр весов", EditorStyles.boldLabel);
                bool hasData = false;
                if (Application.isPlaying)
                {
                    foreach (var loader in GetRuntimeLoadersForSelectedProfile())
                    {
                        var controller = loader.GetComponent<AIController>();
                        if (controller == null) continue;
                        var aiDomain = controller.GetDomain(domain);
                        if (aiDomain == null) continue;

                        EditorGUILayout.LabelField(loader.name, EditorStyles.miniBoldLabel);
                        foreach (var action in aiDomain.Actions)
                        {
                            EditorGUILayout.LabelField($"{action.Name}", $"База {action.BaseWeight:0.00}");
                            hasData = true;
                        }
                        hasData = true;
                    }
                }
                else
                {
                    var summaries = GetAssetActionSummaries(domain);
                    foreach (var summary in summaries)
                    {
                        EditorGUILayout.LabelField(summary.actionName, summary.summary);
                        hasData = true;
                    }
                }

                if (!hasData)
                {
                    EditorGUILayout.LabelField("Нет доступных действий.", EditorStyles.miniLabel);
                }
            }
        }

        private IEnumerable<(string actionName, string summary)> GetAssetActionSummaries(DomainId domain)
        {
            switch (domain)
            {
                case DomainId.Movement:
                    yield return ("Разведка (Explore)", FormatSummary(_selectedProfile.Movement.Explore.enabled, _selectedProfile.Movement.Explore.baseWeight));
                    yield return ("Преследование (Pursue)", FormatSummary(_selectedProfile.Movement.Pursue.enabled, _selectedProfile.Movement.Pursue.baseWeight));
                    yield return ("Окружение (Flank)", FormatSummary(_selectedProfile.Movement.Flank.enabled, _selectedProfile.Movement.Flank.baseWeight));
                    yield return ("Занять укрытие (Take Cover)", FormatSummary(_selectedProfile.Movement.TakeCover.enabled, _selectedProfile.Movement.TakeCover.baseWeight));
                    break;
                case DomainId.Combat:
                    yield return ("Стрельба (Shoot)", FormatSummary(_selectedProfile.Combat.Shoot.enabled, _selectedProfile.Combat.Shoot.baseWeight));
                    yield return ("Перезарядка (Reload)", FormatSummary(_selectedProfile.Combat.Reload.enabled, _selectedProfile.Combat.Reload.baseWeight));
                    yield return ("Граната (Throw Grenade)", FormatSummary(_selectedProfile.Combat.ThrowGrenade.enabled, _selectedProfile.Combat.ThrowGrenade.baseWeight));
                    break;
                case DomainId.Tactics:
                    yield return ("Решение об отступлении", FormatSummary(_selectedProfile.Tactics.RetreatDecision.enabled, _selectedProfile.Tactics.RetreatDecision.baseWeight));
                    yield return ("Отступление (движение)", FormatSummary(_selectedProfile.Tactics.RetreatMove.enabled, _selectedProfile.Tactics.RetreatMove.baseWeight));
                    break;
            }
        }

        private static string FormatSummary(bool enabled, float weight)
        {
            return enabled ? $"Включено • База {weight:0.00}" : "Выключено";
        }

        private IEnumerable<AIProfileLoader> GetRuntimeLoadersForSelectedProfile()
        {
            if (!Application.isPlaying || _selectedProfile == null) yield break;

            foreach (var loader in FindSceneLoaders(includeInactive: false))
            {
                if (loader.profile == _selectedProfile)
                {
                    yield return loader;
                }
            }
        }

        private void DrawSceneIntegration()
        {
            EditorGUILayout.LabelField("Интеграция со сценой", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selectedProfile == null)
                {
                    EditorGUILayout.HelpBox("Выберите профиль для назначения.", MessageType.Info);
                    return;
                }

                var selectedLoaders = GetLoadersFromSelection().ToList();
                EditorGUILayout.LabelField("Выбранные загрузчики (AIProfileLoader)", selectedLoaders.Count.ToString());
                if (GUILayout.Button(new GUIContent("Назначить выбранным", "Назначить текущий профиль всем выбранным AIProfileLoader в сцене.")))
                {
                    AssignProfileToLoaders(selectedLoaders);
                }

                if (GUILayout.Button(new GUIContent("Назначить всем в сцене", "Назначить текущий профиль всем AIProfileLoader в сцене.")))
                {
                    AssignProfileToLoaders(FindSceneLoaders(includeInactive: true));
                }

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button(new GUIContent("Применить Runtime значения из выбранного", "Считать актуальные кривые/веса с выбранного агента (игровая сцена) в профиль-ассет.")))
                    {
                        var runtimeLoader = selectedLoaders.FirstOrDefault();
                        if (runtimeLoader != null)
                        {
                            PushRuntimeChanges(runtimeLoader);
                        }
                        else
                        {
                            ShowNotification(new GUIContent("AIProfileLoader не выбран."));
                        }
                    }
                }
            }
        }

        private IEnumerable<AIProfileLoader> GetLoadersFromSelection()
        {
            var seen = new HashSet<AIProfileLoader>();
            foreach (var go in Selection.gameObjects)
            {
                foreach (var loader in go.GetComponentsInChildren<AIProfileLoader>(true))
                {
                    if (seen.Add(loader))
                    {
                        yield return loader;
                    }
                }
            }
        }

        private void AssignProfileToLoaders(IEnumerable<AIProfileLoader> loaders)
        {
            if (_selectedProfile == null) return;

            int count = 0;
            var seen = new HashSet<AIProfileLoader>();
            foreach (var loader in loaders)
            {
                if (loader == null) continue;
                if (!seen.Add(loader)) continue;
                Undo.RecordObject(loader, "Assign AI Profile");
                loader.profile = _selectedProfile;
                EditorUtility.SetDirty(loader);
                count++;
            }

            if (count > 0)
            {
                ShowNotification(new GUIContent($"Назначено загрузчикам: {count}."));
            }
        }

        private static IEnumerable<AIProfileLoader> FindSceneLoaders(bool includeInactive)
        {
#if UNITY_2020_1_OR_NEWER
            return FindObjectsOfType<AIProfileLoader>(includeInactive);
#else
            return Resources.FindObjectsOfTypeAll<AIProfileLoader>()
                .Where(loader => loader != null && loader.gameObject.scene.IsValid() && (includeInactive || loader.gameObject.activeInHierarchy));
#endif
        }

        private void PushRuntimeChanges(AIProfileLoader loader)
        {
            if (_selectedProfile == null || loader == null) return;

            var controller = loader.GetComponent<AIController>();
            if (controller == null)
            {
                ShowNotification(new GUIContent("Selected loader has no AIController."));
                return;
            }

            Undo.RecordObject(_selectedProfile, "Apply Runtime Curves");

            var curves = controller.UtilityCurves;
            if (curves != null)
            {
                _selectedProfile.Curves.distanceToTarget = CloneCurve(curves.DistanceToTargetCurve);
                _selectedProfile.Curves.visibility = CloneCurve(curves.VisibilityCurve);
                _selectedProfile.Curves.lowHealth = CloneCurve(curves.LowHealthCurve);
                _selectedProfile.Curves.ammoLow = CloneCurve(curves.AmmoLowCurve);
                _selectedProfile.Curves.hasAmmo = CloneCurve(curves.HasAmmoCurve);
                _selectedProfile.Curves.coverAvailable = CloneCurve(curves.CoverAvailableCurve);
                _selectedProfile.Curves.exploreNeed = CloneCurve(curves.ExploreNeedCurve);
                _selectedProfile.Curves.grenadeRange = CloneCurve(curves.GrenadeRangeCurve);
            }

            if (controller.GetDomain(DomainId.Movement) is { } movementDomain)
            {
                ApplyWeightsFromDomain(movementDomain, DomainId.Movement);
            }
            if (controller.GetDomain(DomainId.Combat) is { } combatDomain)
            {
                ApplyWeightsFromDomain(combatDomain, DomainId.Combat);
            }
            if (controller.GetDomain(DomainId.Tactics) is { } tacticsDomain)
            {
                ApplyWeightsFromDomain(tacticsDomain, DomainId.Tactics);
            }

            EditorUtility.SetDirty(_selectedProfile);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Значения из рантайма сохранены в ассет профиля."));
        }

        private void ApplyWeightsFromDomain(AIDomain domain, DomainId id)
        {
            switch (id)
            {
                case DomainId.Movement:
                    ApplyMovementWeights(domain);
                    break;
                case DomainId.Combat:
                    ApplyCombatWeights(domain);
                    break;
                case DomainId.Tactics:
                    ApplyTacticsWeights(domain);
                    break;
            }
        }

        private void ApplyMovementWeights(AIDomain domain)
        {
            var actions = domain.Actions;
            _selectedProfile.Movement.Explore.enabled = false;
            _selectedProfile.Movement.Pursue.enabled = false;
            _selectedProfile.Movement.Flank.enabled = false;
            _selectedProfile.Movement.TakeCover.enabled = false;

            foreach (var action in actions)
            {
                switch (action)
                {
                    case ExploreAreaAction:
                        _selectedProfile.Movement.Explore.enabled = true;
                        _selectedProfile.Movement.Explore.baseWeight = action.BaseWeight;
                        break;
                    case PursueTargetAction:
                        _selectedProfile.Movement.Pursue.enabled = true;
                        _selectedProfile.Movement.Pursue.baseWeight = action.BaseWeight;
                        break;
                    case FlankEnemyAction:
                        _selectedProfile.Movement.Flank.enabled = true;
                        _selectedProfile.Movement.Flank.baseWeight = action.BaseWeight;
                        break;
                    case TakeCoverAction:
                        _selectedProfile.Movement.TakeCover.enabled = true;
                        _selectedProfile.Movement.TakeCover.baseWeight = action.BaseWeight;
                        break;
                    case RetreatMoveAction:
                        _selectedProfile.Tactics.RetreatMove.enabled = true;
                        _selectedProfile.Tactics.RetreatMove.baseWeight = action.BaseWeight;
                        break;
                }
            }
        }

        private void ApplyCombatWeights(AIDomain domain)
        {
            var actions = domain.Actions;
            _selectedProfile.Combat.Shoot.enabled = false;
            _selectedProfile.Combat.Reload.enabled = false;
            _selectedProfile.Combat.ThrowGrenade.enabled = false;

            foreach (var action in actions)
            {
                switch (action)
                {
                    case ShootAction:
                        _selectedProfile.Combat.Shoot.enabled = true;
                        _selectedProfile.Combat.Shoot.baseWeight = action.BaseWeight;
                        break;
                    case ReloadAction:
                        _selectedProfile.Combat.Reload.enabled = true;
                        _selectedProfile.Combat.Reload.baseWeight = action.BaseWeight;
                        break;
                    case ThrowGrenadeAction:
                        _selectedProfile.Combat.ThrowGrenade.enabled = true;
                        _selectedProfile.Combat.ThrowGrenade.baseWeight = action.BaseWeight;
                        break;
                }
            }
        }

        private void ApplyTacticsWeights(AIDomain domain)
        {
            var actions = domain.Actions;
            _selectedProfile.Tactics.RetreatDecision.enabled = false;
            _selectedProfile.Tactics.RetreatMove.enabled = false;

            foreach (var action in actions)
            {
                switch (action)
                {
                    case RetreatDecisionAction:
                        _selectedProfile.Tactics.RetreatDecision.enabled = true;
                        _selectedProfile.Tactics.RetreatDecision.baseWeight = action.BaseWeight;
                        break;
                    case RetreatMoveAction:
                        _selectedProfile.Tactics.RetreatMove.enabled = true;
                        _selectedProfile.Tactics.RetreatMove.baseWeight = action.BaseWeight;
                        break;
                }
            }
        }

        private static AnimationCurve CloneCurve(AnimationCurve curve)
        {
            if (curve == null) return null;
            return new AnimationCurve(curve.keys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode
            };
        }

        private void SelectProfile(AIProfile profile)
        {
            _selectedProfile = profile;
            _serializedProfile = profile != null ? new SerializedObject(profile) : null;
            Repaint();
        }

        private void RefreshProfiles()
        {
            _profiles.Clear();
            var guids = AssetDatabase.FindAssets("t:AIProfile");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AIProfile>(path);
                if (asset != null)
                {
                    _profiles.Add(asset);
                }
            }

            _profiles.Sort((a, b) => string.Compare(a.profileName, b.profileName, StringComparison.Ordinal));

            if (_selectedProfile == null && _profiles.Count > 0)
            {
                SelectProfile(_profiles[0]);
            }
            else if (_selectedProfile != null && !_profiles.Contains(_selectedProfile))
            {
                SelectProfile(null);
            }
        }

        private void CreateProfileAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("Создать AI Profile", "NewAIProfile", "asset", "Выберите путь для нового AI Profile");
            if (string.IsNullOrEmpty(path)) return;

            var profile = ScriptableObject.CreateInstance<AIProfile>();
            profile.profileName = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshProfiles();
            SelectProfile(profile);
        }

        private void CloneProfileAsset(AIProfile source)
        {
            if (source == null) return;

            var sourcePath = AssetDatabase.GetAssetPath(source);
            var directory = System.IO.Path.GetDirectoryName(sourcePath);
            var newPath = EditorUtility.SaveFilePanelInProject("Клонировать AI Profile", source.name + " Copy", "asset", "Выберите путь для копии профиля", directory);
            if (string.IsNullOrEmpty(newPath)) return;

            var clone = Instantiate(source);
            clone.profileName = System.IO.Path.GetFileNameWithoutExtension(newPath);
            AssetDatabase.CreateAsset(clone, newPath);
            EditorUtility.CopySerialized(source, clone);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshProfiles();
            SelectProfile(clone);
        }
    }
}
