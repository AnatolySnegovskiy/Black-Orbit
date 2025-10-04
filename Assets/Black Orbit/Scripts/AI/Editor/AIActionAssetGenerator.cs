using Black_Orbit.Scripts.AI.ScriptableObjects;
using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;
using UnityEditor;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Editor
{
    /// <summary>
    /// Генератор готовых ScriptableObject для AI действий с предустановленными факторами.
    /// Создаёт все действия одним кликом в папке GameData/AI/Actions.
    /// </summary>
    public class AIActionAssetGenerator : EditorWindow
    {
        private const string OUTPUT_PATH = "Assets/Black Orbit/GameData/AI/Actions";
        
        [MenuItem("Tools/AI/Generate Action Assets")]
        public static void ShowWindow()
        {
            GetWindow<AIActionAssetGenerator>("AI Action Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("AI Action Asset Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label($"Путь создания: {OUTPUT_PATH}", EditorStyles.helpBox);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Создать все действия", GUILayout.Height(40)))
            {
                GenerateAllActions();
            }
            
            GUILayout.Space(20);
            GUILayout.Label("Или создать по отдельности:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Patrol")) CreatePatrolAction();
            if (GUILayout.Button("Explore")) CreateExploreAction();
            if (GUILayout.Button("SearchLastKnown")) CreateSearchLastKnownAction();
            if (GUILayout.Button("Pursue")) CreatePursueAction();
            if (GUILayout.Button("Flank")) CreateFlankAction();
            if (GUILayout.Button("TakeCover")) CreateTakeCoverAction();
            if (GUILayout.Button("RangedAttack")) CreateRangedAttackAction();
            if (GUILayout.Button("MeleeAttack")) CreateMeleeAttackAction();
            if (GUILayout.Button("Retreat")) CreateRetreatAction();
            if (GUILayout.Button("SuppressionFire")) CreateSuppressionFireAction();
        }

        static void GenerateAllActions()
        {
            // Создаём папку если не существует
            if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
            {
                string parentPath = "Assets/Black Orbit/GameData/AI";
                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    AssetDatabase.CreateFolder("Assets/Black Orbit/GameData", "AI");
                }
                AssetDatabase.CreateFolder(parentPath, "Actions");
            }

            CreatePatrolAction();
            CreateExploreAction();
            CreateSearchLastKnownAction();
            CreatePursueAction();
            CreateFlankAction();
            CreateTakeCoverAction();
            CreateRangedAttackAction();
            CreateMeleeAttackAction();
            CreateRetreatAction();
            CreateSuppressionFireAction();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"✅ Все AI действия созданы в {OUTPUT_PATH}");
            EditorUtility.DisplayDialog("Готово!", $"Все действия созданы в:\n{OUTPUT_PATH}", "OK");
        }

        static void CreatePatrolAction()
        {
            var action = ScriptableObject.CreateInstance<PatrolAction>();
            action.waypointTolerance = 0.6f;
            action.wanderRadius = 10f;
            action.repathInterval = 1.0f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Не видит игрока", 0.7f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("Время с последнего обнаружения", 0.3f, CreateSCurve())
            };
            
            SaveAsset(action, "Patrol");
        }

        static void CreateExploreAction()
        {
            var action = ScriptableObject.CreateInstance<ExploreAction>();
            action.radius = 15f;
            action.repathInterval = 1.2f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Режим ожидания", 0.7f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("Уровень здоровья", 0.3f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "Explore");
        }

        static void CreateSearchLastKnownAction()
        {
            var action = ScriptableObject.CreateInstance<SearchLastKnownAction>();
            action.tolerance = 0.7f;
            action.scanTime = 3f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Видели недавно", 0.7f, CreateConvexCurve()),
                CreateFactor("Нет прямой видимости", 0.3f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "SearchLastKnown");
        }

        static void CreatePursueAction()
        {
            var action = ScriptableObject.CreateInstance<PursueAction>();
            action.desiredRange = 4f;
            action.repathInterval = 0.2f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Вне желаемой дистанции", 0.6f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("Есть прямая видимость", 0.4f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "Pursue");
        }

        static void CreateFlankAction()
        {
            var action = ScriptableObject.CreateInstance<FlankAction>();
            action.flankDistance = 5f;
            action.repathInterval = 0.5f;
            action.minAngle = 60f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Есть прямая видимость", 0.6f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("Уровень здоровья", 0.4f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "Flank");
        }

        static void CreateTakeCoverAction()
        {
            var action = ScriptableObject.CreateInstance<TakeCoverAction>();
            action.searchRadius = 12f;
            action.repathInterval = 0.4f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Низкое здоровье", 0.5f, CreateConvexCurve()),
                CreateFactor("Под угрозой", 0.3f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("Игрок близко", 0.2f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "TakeCover");
        }

        static void CreateRangedAttackAction()
        {
            var action = ScriptableObject.CreateInstance<RangedAttackAction>();
            action.preferredRange = 12f;
            action.minRange = 4f;
            action.fireCooldown = 0.6f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("На предпочтительной дистанции", 0.5f, CreateBellCurve()),
                CreateFactor("Есть прямая видимость", 0.3f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("Уровень здоровья", 0.2f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "RangedAttack");
        }

        static void CreateMeleeAttackAction()
        {
            var action = ScriptableObject.CreateInstance<MeleeAttackAction>();
            action.strikeRange = 2.2f;
            action.swingCooldown = 0.8f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Игрок близко", 0.7f, CreateConvexCurve()),
                CreateFactor("Есть прямая видимость", 0.3f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "MeleeAttack");
        }

        static void CreateRetreatAction()
        {
            var action = ScriptableObject.CreateInstance<RetreatAction>();
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Низкое здоровье", 0.6f, CreateConvexCurve()),
                CreateFactor("Враг близко", 0.4f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "Retreat");
        }

        static void CreateSuppressionFireAction()
        {
            var action = ScriptableObject.CreateInstance<SuppressionFireAction>();
            action.maxTimeSinceSeen = 3f;
            action.fireCooldown = 0.4f;
            action.minRange = 5f;
            
            action.factors = new UtilityFactor[]
            {
                CreateFactor("Недавно видели, нет LOS", 0.5f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("На подходящей дистанции", 0.3f, AnimationCurve.Linear(0, 0, 1, 1)),
                CreateFactor("Есть союзники", 0.2f, AnimationCurve.Linear(0, 0, 1, 1))
            };
            
            SaveAsset(action, "SuppressionFire");
        }

        static UtilityFactor CreateFactor(string name, float weight, AnimationCurve curve)
        {
            var factor = new UtilityFactor
            {
                name = name,
                weight = weight,
                curve = curve
            };
            return factor;
        }

        static AnimationCurve CreateSCurve()
        {
            return new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.2f, 0.1f),
                new Keyframe(0.8f, 0.9f),
                new Keyframe(1, 1)
            );
        }

        static AnimationCurve CreateConvexCurve()
        {
            return new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.3f, 0.7f),
                new Keyframe(1, 1)
            );
        }

        static AnimationCurve CreateBellCurve()
        {
            return new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 1),
                new Keyframe(1, 0)
            );
        }

        static void SaveAsset(ScriptableObject asset, string name)
        {
            string path = $"{OUTPUT_PATH}/{name}.asset";
            
            // Проверяем, существует ли уже
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
            {
                if (!EditorUtility.DisplayDialog("Файл существует", 
                    $"{name}.asset уже существует. Перезаписать?", "Да", "Нет"))
                {
                    return;
                }
                AssetDatabase.DeleteAsset(path);
            }
            
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"✅ Создан: {path}");
        }
    }
}
