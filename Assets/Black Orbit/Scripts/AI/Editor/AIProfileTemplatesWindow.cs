#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using Black_Orbit.Scripts.AI.Runtime.Configs;

namespace Black_Orbit.Scripts.AI.Editor
{
    public class AIProfileTemplatesWindow : EditorWindow
    {
        private const string DefaultSavePath = "Assets/Black Orbit/GameData/AI";

        [MenuItem("Black Orbit/AI/Билдер профилей AI")] 
        public static void Open()
        {
            var w = GetWindow<AIProfileTemplatesWindow>(true, "Билдер профилей AI");
            w.minSize = new Vector2(420, 420);
            w.Show();
        }

        [Header("Путь сохранения")] public string savePath = DefaultSavePath;

        private SerializedObject _so;
        private void OnEnable()
        {
            _so = new SerializedObject(this);
        }

        private void OnGUI()
        {
            _so.Update();
            EditorGUILayout.LabelField("Создание базовых шаблонов AIProfile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Профили будут сохранены как ScriptableObject в указанную папку.", MessageType.Info);
            EditorGUILayout.PropertyField(_so.FindProperty(nameof(savePath)), new GUIContent("Папка сохранения"));
            if (GUILayout.Button("Открыть папку в Project"))
            {
                EnsureDir();
                var obj = AssetDatabase.LoadAssetAtPath<Object>(savePath);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Шаблоны", EditorStyles.boldLabel);
            if (GUILayout.Button("Пехотинец (Агрессивный)")) CreateAggressiveGrunt();
            if (GUILayout.Button("Пехотинец (Оборонительный)")) CreateDefensiveGrunt();
            if (GUILayout.Button("Разведчик (Scout)")) CreateScout();
            if (GUILayout.Button("Снайпер (Sniper)")) CreateSniper();
            if (GUILayout.Button("Тяжёлый (Heavy)")) CreateHeavy();
            if (GUILayout.Button("Тестовый (Dummy)")) CreateDummy();

            _so.ApplyModifiedProperties();
        }

        private void EnsureDir()
        {
            if (!AssetDatabase.IsValidFolder(savePath))
            {
                var root = "Assets/Black Orbit";
                if (!AssetDatabase.IsValidFolder(root)) AssetDatabase.CreateFolder("Assets", "Black Orbit");
                var gameData = "Assets/Black Orbit/GameData";
                if (!AssetDatabase.IsValidFolder(gameData)) AssetDatabase.CreateFolder(root, "GameData");
                var ai = "Assets/Black Orbit/GameData/AI";
                if (!AssetDatabase.IsValidFolder(ai)) AssetDatabase.CreateFolder(gameData, "AI");
            }
        }

        private static T CreateAsset<T>(string pathWithName) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, pathWithName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            return asset;
        }

        private string Unique(string fileName)
        {
            EnsureDir();
            var basePath = savePath.EndsWith("/") ? savePath : savePath + "/";
            var path = AssetDatabase.GenerateUniqueAssetPath(basePath + fileName + ".asset");
            return path;
        }

        // === Templates ===
        private void CreateAggressiveGrunt()
        {
            var path = Unique("AIProfile_Grunt_Aggressive");
            var p = CreateAsset<AIProfile>(path);
            p.profileName = "Grunt_Aggressive";
            p.version = 1;

            // Curves
            p.Curves.visibility = AnimationCurve.EaseInOut(0, 0, 1, 1);
            p.Curves.distanceToTarget = AnimationCurve.EaseInOut(0, 1, 1, 0.2f);
            p.Curves.hasAmmo = AnimationCurve.Linear(0, 0, 1, 1);
            p.Curves.ammoLow = AnimationCurve.Linear(0, 0, 1, 1);

            // Movement
            p.Movement.Explore.enabled = true;  p.Movement.Explore.baseWeight = 0.4f; p.Movement.Explore.radius = 25f;
            p.Movement.Pursue.enabled = true;   p.Movement.Pursue.baseWeight = 1.2f; p.Movement.Pursue.maxDistance = 80f;
            p.Movement.Flank.enabled = true;    p.Movement.Flank.baseWeight = 1.0f; p.Movement.Flank.flankDistance = 10f;
            p.Movement.TakeCover.enabled = true;p.Movement.TakeCover.baseWeight = 0.6f; p.Movement.TakeCover.searchRadius = 18f;

            // Combat
            p.Combat.Shoot.enabled = true;      p.Combat.Shoot.baseWeight = 1.3f; p.Combat.Shoot.retreatPenalty = 0.4f; p.Combat.Shoot.suppressBoost = 1.3f;
            p.Combat.Reload.enabled = true;     p.Combat.Reload.baseWeight = 0.8f; p.Combat.Reload.lowThreshold = 6; p.Combat.Reload.highThreshold = 18;
            p.Combat.ThrowGrenade.enabled = true; p.Combat.ThrowGrenade.baseWeight = 0.6f; p.Combat.ThrowGrenade.minRange = 8f; p.Combat.ThrowGrenade.maxRange = 22f;

            EditorUtility.SetDirty(p);
        }

        private void CreateDefensiveGrunt()
        {
            var path = Unique("AIProfile_Grunt_Defensive");
            var p = CreateAsset<AIProfile>(path);
            p.profileName = "Grunt_Defensive";
            p.version = 1;

            p.Curves.visibility = AnimationCurve.Linear(0, 0, 1, 1);
            p.Curves.distanceToTarget = AnimationCurve.EaseInOut(0, 1, 1, 0.1f);

            p.Movement.Explore.enabled = true;  p.Movement.Explore.baseWeight = 0.3f; p.Movement.Explore.radius = 20f;
            p.Movement.Pursue.enabled = true;   p.Movement.Pursue.baseWeight = 0.8f; p.Movement.Pursue.maxDistance = 50f;
            p.Movement.Flank.enabled = false;
            p.Movement.TakeCover.enabled = true;p.Movement.TakeCover.baseWeight = 1.2f; p.Movement.TakeCover.searchRadius = 28f; p.Movement.TakeCover.minDistanceToTarget = 8f;

            p.Combat.Shoot.enabled = true;      p.Combat.Shoot.baseWeight = 1.0f; p.Combat.Shoot.retreatPenalty = 0.6f; p.Combat.Shoot.suppressBoost = 1.1f;
            p.Combat.Reload.enabled = true;     p.Combat.Reload.baseWeight = 1.0f; p.Combat.Reload.lowThreshold = 8; p.Combat.Reload.highThreshold = 24;
            p.Combat.ThrowGrenade.enabled = false;

            EditorUtility.SetDirty(p);
        }

        private void CreateScout()
        {
            var path = Unique("AIProfile_Scout");
            var p = CreateAsset<AIProfile>(path);
            p.profileName = "Scout";
            p.version = 1;

            p.Curves.visibility = AnimationCurve.EaseInOut(0, 0, 1, 1);
            p.Curves.exploreNeed = AnimationCurve.EaseInOut(0, 0.2f, 1, 1);

            p.Movement.Explore.enabled = true;  p.Movement.Explore.baseWeight = 1.2f; p.Movement.Explore.radius = 40f;
            p.Movement.Pursue.enabled = true;   p.Movement.Pursue.baseWeight = 0.9f; p.Movement.Pursue.maxDistance = 90f;
            p.Movement.Flank.enabled = true;    p.Movement.Flank.baseWeight = 0.9f; p.Movement.Flank.flankDistance = 16f;
            p.Movement.TakeCover.enabled = true;p.Movement.TakeCover.baseWeight = 0.5f; p.Movement.TakeCover.searchRadius = 20f;

            p.Combat.Shoot.enabled = true;      p.Combat.Shoot.baseWeight = 0.9f;
            p.Combat.Reload.enabled = true;     p.Combat.Reload.baseWeight = 0.9f;
            p.Combat.ThrowGrenade.enabled = false;

            EditorUtility.SetDirty(p);
        }

        private void CreateSniper()
        {
            var path = Unique("AIProfile_Sniper");
            var p = CreateAsset<AIProfile>(path);
            p.profileName = "Sniper";
            p.version = 1;

            p.Curves.visibility = AnimationCurve.EaseInOut(0, 0, 1, 1);
            p.Curves.distanceToTarget = AnimationCurve.Linear(0, 0.2f, 1, 1);

            p.Movement.Explore.enabled = false;
            p.Movement.Pursue.enabled = true;   p.Movement.Pursue.baseWeight = 0.6f; p.Movement.Pursue.maxDistance = 120f;
            p.Movement.Flank.enabled = false;
            p.Movement.TakeCover.enabled = true;p.Movement.TakeCover.baseWeight = 1.2f; p.Movement.TakeCover.searchRadius = 35f; p.Movement.TakeCover.minDistanceToTarget = 20f;

            p.Combat.Shoot.enabled = true;      p.Combat.Shoot.baseWeight = 1.4f;
            p.Combat.Reload.enabled = true;     p.Combat.Reload.baseWeight = 0.8f;
            p.Combat.ThrowGrenade.enabled = false;

            EditorUtility.SetDirty(p);
        }

        private void CreateHeavy()
        {
            var path = Unique("AIProfile_Heavy");
            var p = CreateAsset<AIProfile>(path);
            p.profileName = "Heavy";
            p.version = 1;

            p.Curves.visibility = AnimationCurve.Linear(0, 0, 1, 1);
            p.Curves.lowHealth = AnimationCurve.Linear(0, 0, 1, 1);

            p.Movement.Explore.enabled = false;
            p.Movement.Pursue.enabled = true;   p.Movement.Pursue.baseWeight = 0.7f; p.Movement.Pursue.maxDistance = 60f;
            p.Movement.Flank.enabled = false;
            p.Movement.TakeCover.enabled = true;p.Movement.TakeCover.baseWeight = 0.7f; p.Movement.TakeCover.searchRadius = 18f;

            p.Combat.Shoot.enabled = true;      p.Combat.Shoot.baseWeight = 1.5f; p.Combat.Shoot.suppressBoost = 1.5f;
            p.Combat.Reload.enabled = true;     p.Combat.Reload.baseWeight = 0.7f; p.Combat.Reload.lowThreshold = 12; p.Combat.Reload.highThreshold = 30;
            p.Combat.ThrowGrenade.enabled = true; p.Combat.ThrowGrenade.baseWeight = 0.4f; p.Combat.ThrowGrenade.minRange = 10f; p.Combat.ThrowGrenade.maxRange = 18f;

            EditorUtility.SetDirty(p);
        }

        private void CreateDummy()
        {
            var path = Unique("AIProfile_Dummy");
            var p = CreateAsset<AIProfile>(path);
            p.profileName = "Dummy";
            p.version = 1;

            p.Movement.enabled = false;
            p.Combat.enabled = false;
            p.Tactics.enabled = false;

            EditorUtility.SetDirty(p);
        }
    }
}
#endif
