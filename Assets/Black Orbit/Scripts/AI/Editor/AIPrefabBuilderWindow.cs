#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using Black_Orbit.Scripts.AI.Runtime.Controller;
using Black_Orbit.Scripts.AI.Runtime.Configs;
using Black_Orbit.Scripts.AI.Runtime.Movement;
using Black_Orbit.Scripts.WeaponSystem.Runtime;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;
using Black_Orbit.Scripts.Faction.Runtime;
using Black_Orbit.Scripts.Faction.ScriptableObjects;
using Black_Orbit.Scripts.AI.Runtime.Perception;
using Black_Orbit.Scripts.Health.Runtime.Components;
using Black_Orbit.Scripts.Health.Runtime.Configs;

namespace Black_Orbit.Scripts.AI.Editor
{
    public class AIPrefabBuilderWindow : EditorWindow
    {
        [MenuItem("Black Orbit/AI/Prefab Builder")] 
        public static void ShowWindow()
        {
            var wnd = GetWindow<AIPrefabBuilderWindow>(true, "AI Prefab Builder");
            wnd.minSize = new Vector2(380, 320);
            wnd.Show();
        }

        [Header("Модель (Root)")]
        [Tooltip("Префаб модели или объект сцены, который станет визуалом бота. Если указать префаб, он будет инстанциирован как дочерний к новому корневому объекту бота.")]
        public GameObject modelRoot;

        [Header("AI Profile (опционально)")]
        [Tooltip("Профиль поведения, который будет назначен компоненту AIProfileLoader.")]
        public AIProfile profile;

        [Header("Опции компонентов")]
        [Tooltip("Добавить компоненты боевой системы, если они используются в профиле/проекте.")]
        public bool addCombatComponents = false;

        [Tooltip("Автоматически добавить централизованный шедулер AIUpdateScheduler в сцену, если отсутствует.")]
        public bool ensureScheduler = true;

        [Header("Оружие (для AI)")]
        [Tooltip("Добавить и настроить AIWeaponHandler на боте.")]
        public bool addAIWeaponHandler = true;
        [Tooltip("Данные оружия для AI (ScriptableObject)")]
        public WeaponScriptableObject aiWeaponData;
        [Tooltip("Совмещать точки хвата рук с держателями оружия при старте")]
        public bool alignHandsOnStart = true;

        [Header("Фракция (IFF)")]
        [Tooltip("Добавить компонент FactionMember и назначить фракцию боту.")]
        public bool addFaction = true;
        [Tooltip("Фракция бота (ScriptableObject)")]
        public FactionData factionData;
        [Tooltip("Добавить восприятие врагов по фракциям (видимость/цель в Blackboard)")]
        public bool addFactionPerception = true;

        [Header("Здоровье")]
        [Tooltip("Добавить компонент HealthComponent на бота")] public bool addHealthComponent = true;
        [Tooltip("Профиль здоровья (ScriptableObject)")] public HealthProfile healthProfile;

        private SerializedObject _so;

        private void OnEnable()
        {
            _so = new SerializedObject(this);
        }

        private void OnGUI()
        {
            _so.Update();

            EditorGUILayout.LabelField("Сборка бота", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Укажите модель (Root) и при необходимости профиль. Нажмите 'Создать Бота на Сцене'.", MessageType.Info);

            EditorGUILayout.PropertyField(_so.FindProperty(nameof(modelRoot)), new GUIContent("Модель (Root)"));
            EditorGUILayout.PropertyField(_so.FindProperty(nameof(profile)), new GUIContent("AI Profile"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Опции", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_so.FindProperty(nameof(addCombatComponents)), new GUIContent("Добавить боевые компоненты"));
            EditorGUILayout.PropertyField(_so.FindProperty(nameof(ensureScheduler)), new GUIContent("Создать/Добавить Шедулер"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Оружие (AI)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_so.FindProperty(nameof(addAIWeaponHandler)), new GUIContent("Добавить AIWeaponHandler"));
            using (new EditorGUI.DisabledScope(!_so.FindProperty(nameof(addAIWeaponHandler)).boolValue))
            {
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(aiWeaponData)), new GUIContent("Weapon Data"));
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(alignHandsOnStart)), new GUIContent("Совмещать руки при старте"));
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Фракция (IFF)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_so.FindProperty(nameof(addFaction)), new GUIContent("Добавить FactionMember"));
            using (new EditorGUI.DisabledScope(!_so.FindProperty(nameof(addFaction)).boolValue))
            {
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(factionData)), new GUIContent("Faction Data"));
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(addFactionPerception)), new GUIContent("Добавить FactionPerception"));
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Здоровье", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_so.FindProperty(nameof(addHealthComponent)), new GUIContent("Добавить HealthComponent"));
            using (new EditorGUI.DisabledScope(!_so.FindProperty(nameof(addHealthComponent)).boolValue))
            {
                EditorGUILayout.PropertyField(_so.FindProperty(nameof(healthProfile)), new GUIContent("Health Profile"));
            }

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(modelRoot == null))
            {
                if (GUILayout.Button(new GUIContent("Создать Бота на Сцене", "Создать нового бота с необходимыми компонентами"), GUILayout.Height(32)))
                {
                    CreateBot();
                }
            }

            using (new EditorGUI.DisabledScope(!ensureScheduler))
            {
                if (GUILayout.Button(new GUIContent("Создать/Добавить Шедулер", "Добавить AIUpdateScheduler в сцену, если его нет")))
                {
                    EnsureSchedulerInScene();
                }
            }

            _so.ApplyModifiedProperties();
        }

        private void CreateBot()
        {
            // Определяем целевой объект: если передан префаб-ассет — инстанциируем его;
            // если объект сцены — дополняем его напрямую.
            GameObject target;
            if (PrefabUtility.GetPrefabAssetType(modelRoot) != PrefabAssetType.NotAPrefab)
            {
                target = (GameObject)PrefabUtility.InstantiatePrefab(modelRoot);
                Undo.RegisterCreatedObjectUndo(target, "Instantiate AI Bot Prefab");
                target.name = modelRoot.name; // чтобы не добавлять (Clone)
            }
            else
            {
                target = modelRoot;
                Undo.RegisterCompleteObjectUndo(target, "Augment AI Bot Object");
            }

            Selection.activeGameObject = target;

            // Rigidbody (создаём только если отсутствует)
            var rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = Undo.AddComponent<Rigidbody>(target);
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // NavMeshAgent (только планирование пути)
            var agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = Undo.AddComponent<NavMeshAgent>(target);
            }
            agent.updatePosition = false;
            agent.updateRotation = false;

            // Движок перемещения (имеется в проекте AIMovementMotor)
            var motor = target.GetComponent<AIMovementMotor>();
            if (motor == null)
            {
                motor = Undo.AddComponent<AIMovementMotor>(target);
            }
            // Оставим поля по умолчанию, т.к. у проекта есть собственные дефолты; при необходимости пользователь настроит в инспекторе

            // Контроллер AI
            var controller = target.GetComponent<AIController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<AIController>(target);
            }

            // Лоадер профиля AI
            var loader = target.GetComponent<AIProfileLoader>();
            if (loader == null)
            {
                loader = Undo.AddComponent<AIProfileLoader>(target);
            }
            if (profile != null)
            {
                loader.profile = profile;
                EditorUtility.SetDirty(loader);
            }

            // Опционально добавить боевые компоненты (если они есть в проекте)
            if (addCombatComponents)
            {
                // Попробуем добавить по типам, если классы доступны в сборке проекта
                TryAddComponentByTypeName(target, "Black_Orbit.Scripts.AI.Runtime.Combat.AICombat");
                TryAddComponentByTypeName(target, "Black_Orbit.Scripts.AI.Runtime.Combat.AIGrenadeThrower");
            }

            // AI Weapon Handler
            if (addAIWeaponHandler)
            {
                var handler = target.GetComponent<AIWeaponHandler>();
                if (handler == null)
                    handler = Undo.AddComponent<AIWeaponHandler>(target);
                var combat = target.GetComponent<Black_Orbit.Scripts.AI.Runtime.Combat.AICombat>();
                if (combat == null)
                {
                    combat = Undo.AddComponent<Black_Orbit.Scripts.AI.Runtime.Combat.AICombat>(target);
                }
                handler.SetConfig(aiWeaponData, null, null, null, alignHandsOnStart);
                EditorUtility.SetDirty(handler);
            }

            // Health
            if (addHealthComponent)
            {
                var hc = target.GetComponent<HealthComponent>();
                if (hc == null) hc = Undo.AddComponent<HealthComponent>(target);
                if (healthProfile != null)
                {
                    hc.profile = healthProfile;
                    EditorUtility.SetDirty(hc);
                }
            }

            // Фракция (IFF)
            // 1) По флагу addFaction — проставляем конкретную фракцию
            if (addFaction)
            {
                var fm = target.GetComponent<FactionMember>();
                if (fm == null) fm = Undo.AddComponent<FactionMember>(target);
                if (factionData != null)
                {
                    fm.faction = factionData;
                    EditorUtility.SetDirty(fm);
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Фракция не назначена",
                        "Вы включили добавление FactionMember, но не выбрали Faction Data. Бот не сможет определять врагов/союзников, а зрение не будет работать.",
                        "OK");
                }
            }

            // 2) По флагу addFactionPerception — гарантируем наличие FactionMember и добавляем FactionPerception
            if (addFactionPerception)
            {
                var fmEnsure = target.GetComponent<FactionMember>();
                if (fmEnsure == null) fmEnsure = Undo.AddComponent<FactionMember>(target); // пустой, если фракция не выбрана

                // Добавляем напрямую, без рефлексии по строке
                var fp = target.GetComponent<FactionPerception>();
                if (fp == null) fp = Undo.AddComponent<FactionPerception>(target);
                if (fp != null)
                {
                    fp.debugDraw = true; // включим гизмо по умолчанию
                    EditorUtility.SetDirty(fp);
                }
            }

            // Убедимся, что в сцене есть шедулер
            if (ensureScheduler)
            {
                // Переводим контроллер на внешний шедулер
                controller.externalScheduler = true;
                EditorUtility.SetDirty(controller);
                EnsureSchedulerInScene();
            }

            // Готово
            var note = ensureScheduler ? "Бот создан/обновлён. Включён внешний шедулер (AIUpdateScheduler)." : "Бот создан/обновлён.";
            ShowNotification(new GUIContent(note));
        }

        private static void EnsureSchedulerInScene()
        {
            // Прямая ссылка на тип (надежнее, чем рефлексия по строке)
            var existing = Object.FindObjectOfType<AIUpdateScheduler>();
            if (existing == null)
            {
                var go = new GameObject("AIUpdateScheduler");
                Undo.RegisterCreatedObjectUndo(go, "Create AIUpdateScheduler");
                go.AddComponent<AIUpdateScheduler>();
                EditorUtility.SetDirty(go);
            }
            else
            {
                EditorGUIUtility.PingObject(existing);
            }
        }

        private static void TryAddComponentByTypeName(GameObject target, string qualifiedTypeName)
        {
            var t = System.Type.GetType(qualifiedTypeName);
            if (t == null) return;
            if (target.GetComponent(t) != null) return;
            Undo.AddComponent(target, t);
        }
    }
}
#endif
