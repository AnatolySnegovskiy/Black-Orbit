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

        [MenuItem("Black Orbit/AI/AI Profile Designer")]
        public static void Open()
        {
            GetWindow<AIProfileDesignerWindow>("AI Profile Designer");
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
                EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
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
                    if (GUILayout.Button("New", GUILayout.Height(22f)))
                    {
                        CreateProfileAsset();
                    }

                    EditorGUI.BeginDisabledGroup(_selectedProfile == null);
                    if (GUILayout.Button("Clone", GUILayout.Height(22f)))
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
                    EditorGUILayout.HelpBox("Select or create an AIProfile asset to edit.", MessageType.Info);
                    return;
                }

                _serializedProfile.Update();

                _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);

                EditorGUILayout.LabelField("Profile Metadata", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_serializedProfile.FindProperty("profileName"));
                EditorGUILayout.PropertyField(_serializedProfile.FindProperty("version"));
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
            _curvesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_curvesFoldout, "Utility Curves");
            if (_curvesFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var curvesProp = _serializedProfile.FindProperty("Curves");
                    DrawCurve(curvesProp.FindPropertyRelative("distanceToTarget"), "Distance To Target");
                    DrawCurve(curvesProp.FindPropertyRelative("visibility"), "Visibility");
                    DrawCurve(curvesProp.FindPropertyRelative("lowHealth"), "Low Health");
                    DrawCurve(curvesProp.FindPropertyRelative("ammoLow"), "Ammo Low");
                    DrawCurve(curvesProp.FindPropertyRelative("hasAmmo"), "Has Ammo");
                    DrawCurve(curvesProp.FindPropertyRelative("coverAvailable"), "Cover Available");
                    DrawCurve(curvesProp.FindPropertyRelative("exploreNeed"), "Explore Need");
                    DrawCurve(curvesProp.FindPropertyRelative("grenadeRange"), "Grenade Range");
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawCurve(SerializedProperty property, string label)
        {
            EditorGUILayout.Space(2f);
            var curve = property.animationCurveValue;
            curve = EditorGUILayout.CurveField(new GUIContent(label), curve, Color.cyan, new Rect(0f, 0f, 1f, 1f), GUILayout.Height(60f));
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
            _movementFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_movementFoldout, "Movement Domain");
            if (_movementFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(movementProp.FindPropertyRelative("enabled"));
                    EditorGUILayout.Space(2f);

                    if (movementProp.FindPropertyRelative("enabled").boolValue)
                    {
                        DrawMovementAction(movementProp.FindPropertyRelative("Explore"), "Explore", "radius");
                        DrawMovementAction(movementProp.FindPropertyRelative("Pursue"), "Pursue", "maxDistance");
                        DrawMovementAction(movementProp.FindPropertyRelative("Flank"), "Flank", "flankDistance", "orderBoost");
                        DrawMovementAction(movementProp.FindPropertyRelative("TakeCover"), "Take Cover", "searchRadius", "minDistanceToTarget");
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
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("enabled"));
                using (new EditorGUI.DisabledScope(!prop.FindPropertyRelative("enabled").boolValue))
                {
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("baseWeight"));
                    foreach (var field in additionalFields)
                    {
                        var child = prop.FindPropertyRelative(field);
                        if (child != null)
                        {
                            EditorGUILayout.PropertyField(child);
                        }
                    }
                }
            }
        }

        private void DrawCombatSection()
        {
            var combatProp = _serializedProfile.FindProperty("Combat");
            _combatFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_combatFoldout, "Combat Domain");
            if (_combatFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(combatProp.FindPropertyRelative("enabled"));
                    EditorGUILayout.Space(2f);

                    if (combatProp.FindPropertyRelative("enabled").boolValue)
                    {
                        DrawCombatAction(combatProp.FindPropertyRelative("Shoot"), "Shoot", "retreatPenalty", "suppressBoost");
                        DrawCombatAction(combatProp.FindPropertyRelative("Reload"), "Reload", "lowThreshold", "highThreshold");
                        DrawCombatAction(combatProp.FindPropertyRelative("ThrowGrenade"), "Throw Grenade", "minRange", "maxRange", "cooldown");
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
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("enabled"));
                using (new EditorGUI.DisabledScope(!prop.FindPropertyRelative("enabled").boolValue))
                {
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("baseWeight"));
                    foreach (var field in additionalFields)
                    {
                        var child = prop.FindPropertyRelative(field);
                        if (child != null)
                        {
                            EditorGUILayout.PropertyField(child);
                        }
                    }
                }
            }
        }

        private void DrawTacticsSection()
        {
            var tacticsProp = _serializedProfile.FindProperty("Tactics");
            _tacticsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_tacticsFoldout, "Tactics Domain");
            if (_tacticsFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(tacticsProp.FindPropertyRelative("enabled"));
                    EditorGUILayout.Space(2f);

                    if (tacticsProp.FindPropertyRelative("enabled").boolValue)
                    {
                        DrawTacticsAction(tacticsProp.FindPropertyRelative("RetreatDecision"), "Retreat Decision", "critical", "max", "retreatDistance", "preferCover");
                        DrawTacticsAction(tacticsProp.FindPropertyRelative("RetreatMove"), "Retreat Move");
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
                EditorGUILayout.PropertyField(prop.FindPropertyRelative("enabled"));
                using (new EditorGUI.DisabledScope(!prop.FindPropertyRelative("enabled").boolValue))
                {
                    EditorGUILayout.PropertyField(prop.FindPropertyRelative("baseWeight"));
                    foreach (var field in additionalFields)
                    {
                        var child = prop.FindPropertyRelative(field);
                        if (child != null)
                        {
                            EditorGUILayout.PropertyField(child);
                        }
                    }
                }
            }
        }

        private void DrawActionWeightsPreview(DomainId domain)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Live Weights", EditorStyles.boldLabel);
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
                            EditorGUILayout.LabelField($"{action.Name}", $"Base {action.BaseWeight:0.00}");
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
                    EditorGUILayout.LabelField("No actions available.", EditorStyles.miniLabel);
                }
            }
        }

        private IEnumerable<(string actionName, string summary)> GetAssetActionSummaries(DomainId domain)
        {
            switch (domain)
            {
                case DomainId.Movement:
                    yield return ("Explore", FormatSummary(_selectedProfile.Movement.Explore.enabled, _selectedProfile.Movement.Explore.baseWeight));
                    yield return ("Pursue", FormatSummary(_selectedProfile.Movement.Pursue.enabled, _selectedProfile.Movement.Pursue.baseWeight));
                    yield return ("Flank", FormatSummary(_selectedProfile.Movement.Flank.enabled, _selectedProfile.Movement.Flank.baseWeight));
                    yield return ("Take Cover", FormatSummary(_selectedProfile.Movement.TakeCover.enabled, _selectedProfile.Movement.TakeCover.baseWeight));
                    break;
                case DomainId.Combat:
                    yield return ("Shoot", FormatSummary(_selectedProfile.Combat.Shoot.enabled, _selectedProfile.Combat.Shoot.baseWeight));
                    yield return ("Reload", FormatSummary(_selectedProfile.Combat.Reload.enabled, _selectedProfile.Combat.Reload.baseWeight));
                    yield return ("Throw Grenade", FormatSummary(_selectedProfile.Combat.ThrowGrenade.enabled, _selectedProfile.Combat.ThrowGrenade.baseWeight));
                    break;
                case DomainId.Tactics:
                    yield return ("Retreat Decision", FormatSummary(_selectedProfile.Tactics.RetreatDecision.enabled, _selectedProfile.Tactics.RetreatDecision.baseWeight));
                    yield return ("Retreat Move", FormatSummary(_selectedProfile.Tactics.RetreatMove.enabled, _selectedProfile.Tactics.RetreatMove.baseWeight));
                    break;
            }
        }

        private static string FormatSummary(bool enabled, float weight)
        {
            return enabled ? $"Enabled • Base {weight:0.00}" : "Disabled";
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
            EditorGUILayout.LabelField("Scene Integration", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selectedProfile == null)
                {
                    EditorGUILayout.HelpBox("Select a profile to assign.", MessageType.Info);
                    return;
                }

                var selectedLoaders = GetLoadersFromSelection().ToList();
                EditorGUILayout.LabelField("Selected Loaders", selectedLoaders.Count.ToString());
                if (GUILayout.Button("Assign to Selected Loaders"))
                {
                    AssignProfileToLoaders(selectedLoaders);
                }

                if (GUILayout.Button("Assign to All Scene Loaders"))
                {
                    AssignProfileToLoaders(FindSceneLoaders(includeInactive: true));
                }

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Push Runtime Changes From Selected Loader"))
                    {
                        var runtimeLoader = selectedLoaders.FirstOrDefault();
                        if (runtimeLoader != null)
                        {
                            PushRuntimeChanges(runtimeLoader);
                        }
                        else
                        {
                            ShowNotification(new GUIContent("No AIProfileLoader selected."));
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
                ShowNotification(new GUIContent($"Assigned to {count} loader(s)."));
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
            ShowNotification(new GUIContent("Runtime values pushed to asset."));
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
            var path = EditorUtility.SaveFilePanelInProject("Create AI Profile", "NewAIProfile", "asset", "Choose location for the new AI Profile");
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
            var newPath = EditorUtility.SaveFilePanelInProject("Clone AI Profile", source.name + " Copy", "asset", "Choose location for the cloned profile", directory);
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
