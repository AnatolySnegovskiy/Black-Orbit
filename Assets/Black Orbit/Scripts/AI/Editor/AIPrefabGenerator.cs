using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;
using Black_Orbit.Scripts.Faction.ScriptableObjects;
using Black_Orbit.Scripts.Faction.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using AIComponent = Black_Orbit.Scripts.AI.Runtime.AI;
using AISquadComponent = Black_Orbit.Scripts.AI.Runtime.AISquad;
using AIWeaponHandlerComponent = Black_Orbit.Scripts.AI.Runtime.AIWeaponHandler;

namespace Black_Orbit.Scripts.AI.Editor
{
    /// <summary>
    /// Генератор готовых префабов AI с предустановленными настройками.
    /// Создаёт одиночных ботов и squad'ы одним кликом.
    /// </summary>
    public class AIPrefabGenerator : EditorWindow
    {
        private const string OUTPUT_PATH = "Assets/Black Orbit/Prefabs/AI/Prefabs";
        private const string ACTIONS_PATH = "Assets/Black Orbit/GameData/AI/Actions";
        private const string FACTIONS_PATH = "Assets/Black Orbit/GameData/Factions";
        private const string SETTINGS_ASSET_PATH = "Assets/Black Orbit/GameData/AI/Settings/AISettings.asset";
        
        private Vector2 scrollPos;
        private string customName = "CustomAI";
        
        // Фракции
        private static FactionData _selectedFaction;
        private static FactionData[] _allFactions;
        private static string[] _factionNames;
        private static int _selectedFactionIndex = 0;

        [MenuItem("Tools/AI/Generate Prefabs")]
        public static void ShowWindow()
        {
            var window = GetWindow<AIPrefabGenerator>("AI Prefab Generator");
            window.LoadAllFactions();
        }

        void OnEnable()
        {
            LoadAllFactions();
        }

        void LoadAllFactions()
        {
            if (!System.IO.Directory.Exists(FACTIONS_PATH))
            {
                _allFactions = new FactionData[0];
                _factionNames = new string[] { "Нет фракций" };
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:FactionData", new[] { FACTIONS_PATH });
            _allFactions = new FactionData[guids.Length];
            _factionNames = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _allFactions[i] = AssetDatabase.LoadAssetAtPath<FactionData>(path);
                _factionNames[i] = _allFactions[i] != null ? _allFactions[i].factionName : "Unknown";
            }

            // Выбираем Enemy по умолчанию если есть
            for (int i = 0; i < _allFactions.Length; i++)
            {
                if (_allFactions[i] != null && _allFactions[i].factionName == "Enemy")
                {
                    _selectedFactionIndex = i;
                    _selectedFaction = _allFactions[i];
                    break;
                }
            }

            if (_selectedFaction == null && _allFactions.Length > 0)
            {
                _selectedFactionIndex = 0;
                _selectedFaction = _allFactions[0];
            }
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("AI Prefab Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            string currentPath = _selectedFaction != null 
                ? $"{OUTPUT_PATH}/{_selectedFaction.factionName}/" 
                : $"{OUTPUT_PATH}/NoFaction/";
            GUILayout.Label($"Путь создания: {currentPath}", EditorStyles.helpBox);
            GUILayout.Space(10);

            // === ВЫБОР ФРАКЦИИ ===
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Фракция для префабов:", EditorStyles.boldLabel, GUILayout.Width(150));
            
            if (_allFactions != null && _allFactions.Length > 0)
            {
                int newIndex = EditorGUILayout.Popup(_selectedFactionIndex, _factionNames);
                if (newIndex != _selectedFactionIndex)
                {
                    _selectedFactionIndex = newIndex;
                    _selectedFaction = _allFactions[_selectedFactionIndex];
                }

                // Цветной индикатор
                if (_selectedFaction != null)
                {
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = _selectedFaction.factionColor;
                    GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                    GUI.backgroundColor = oldColor;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Фракции не найдены! Создайте через Tools → Faction → Create Default Factions", MessageType.Warning);
            }

            if (GUILayout.Button("Обновить", GUILayout.Width(80)))
            {
                LoadAllFactions();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            // === ОДИНОЧНЫЕ БОТЫ ===
            GUILayout.Label("Одиночные боты:", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            if (GUILayout.Button("🎯 Тактический стрелок (Tactical Shooter)", GUILayout.Height(35)))
                CreateTacticalShooter();
            
            if (GUILayout.Button("⚡ Агрессивный боец (Aggressive Fighter)", GUILayout.Height(35)))
                CreateAggressiveFighter();
            
            if (GUILayout.Button("🎯 Снайпер (Sniper)", GUILayout.Height(35)))
                CreateSniper();
            
            if (GUILayout.Button("⚔️ Берсерк (Melee Berserker)", GUILayout.Height(35)))
                CreateBerserker();
            
            if (GUILayout.Button("🚶 Патрульный (Scout/Patrol)", GUILayout.Height(35)))
                CreateScout();

            GUILayout.Space(15);

            // === SQUAD'Ы ===
            GUILayout.Label("Squad'ы (отряды):", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            if (GUILayout.Button("👥 Тактический отряд (3 бойца)", GUILayout.Height(35)))
                CreateTacticalSquad();
            
            if (GUILayout.Button("🔫 Штурмовой отряд (4 бойца)", GUILayout.Height(35)))
                CreateAssaultSquad();
            
            if (GUILayout.Button("🎯 Снайперская команда (2 снайпера + 1 прикрытие)", GUILayout.Height(35)))
                CreateSniperTeam();

            GUILayout.Space(15);

            // === КАСТОМНЫЙ ===
            GUILayout.Label("Кастомный префаб:", EditorStyles.boldLabel);
            customName = EditorGUILayout.TextField("Имя:", customName);
            if (GUILayout.Button("Создать базовый AI", GUILayout.Height(30)))
                CreateBasicAI(customName);

            GUILayout.Space(20);
            
            if (GUILayout.Button("📁 Открыть папку с префабами", GUILayout.Height(30)))
            {
                EditorUtility.RevealInFinder(OUTPUT_PATH);
            }

            EditorGUILayout.EndScrollView();
        }

        // ============================================
        // ОДИНОЧНЫЕ БОТЫ
        // ============================================

        static void CreateTacticalShooter()
        {
            var prefab = CreateBasePrefab("TacticalShooter");
            var ai = prefab.GetComponent<AIComponent>();
            
            // Настройки
            ai.InitializeParameters(
                moveSpd: 3.6f,
                rotSpd: 5f,
                detectRange: 25f,
                meleeRange: 2f,
                rangedRange: 30f,
                visAngle: 130f,
                visRange: 35f,
                hp: 100f,
                targetSearchInt: 0.8f
            );
            
            // Действия
            ai.SetActions(LoadActions(new[]
            {
                "Patrol", "Explore", "SearchLastKnown",
                "Pursue", "Flank", "TakeCover",
                "RangedAttack", "PeekAndShoot", "SuppressionFire"
            }));
            TryAssignAISettings(ai);
            
            // Оружие
            var weaponHandler = prefab.AddComponent<AIWeaponHandlerComponent>();
            weaponHandler.autoInitialize = true;
            weaponHandler.autoReload = true;
            weaponHandler.reloadOnLowAmmoThreshold = 2; // тактик перезаряжается заранее
            weaponHandler.reloadCheckInterval = 0.2f;
            
            SavePrefab(prefab, "TacticalShooter");
            Debug.Log($"✅ Создан: Тактический стрелок (Фракция: {ai.faction?.factionName ?? "None"})");
        }

        static void CreateAggressiveFighter()
        {
            var prefab = CreateBasePrefab("AggressiveFighter");
            var ai = prefab.GetComponent<AIComponent>();
            
            ai.InitializeParameters(
                moveSpd: 5.2f,
                rotSpd: 7.5f,
                detectRange: 30f,
                meleeRange: 3f,
                rangedRange: 35f,
                visAngle: 140f,
                visRange: 40f,
                hp: 120f,
                targetSearchInt: 0.4f
            );
            
            ai.SetActions(LoadActions(new[]
            {
                "Pursue", "RangedAttack", "MeleeAttack",
                "Flank", "TakeCover", "PeekAndShoot", "SearchLastKnown"
            }));
            TryAssignAISettings(ai);
            
            var weaponHandler = prefab.AddComponent<AIWeaponHandlerComponent>();
            weaponHandler.autoInitialize = true;
            weaponHandler.autoReload = true;
            weaponHandler.reloadOnLowAmmoThreshold = 1; // агрессивный — терпит до минимума
            weaponHandler.reloadCheckInterval = 0.15f;
            
            SavePrefab(prefab, "AggressiveFighter");
            Debug.Log($"✅ Создан: Агрессивный боец (Фракция: {ai.faction?.factionName ?? "None"})");
        }

        static void CreateSniper()
        {
            var prefab = CreateBasePrefab("Sniper");
            var ai = prefab.GetComponent<AIComponent>();
            
            ai.InitializeParameters(
                moveSpd: 2.5f,
                rotSpd: 3f,
                detectRange: 120f,
                meleeRange: 1f,
                rangedRange: 180f,
                visAngle: 95f,
                visRange: 180f,
                hp: 80f,
                targetSearchInt: 1.2f
            );
            
            ai.SetActions(LoadActions(new[]
            {
                "Patrol", "TakeCover", "RangedAttack",
                "PeekAndShoot", "SuppressionFire", "Retreat"
            }));
            TryAssignAISettings(ai);
            
            var weaponHandler = prefab.AddComponent<AIWeaponHandlerComponent>();
            weaponHandler.autoInitialize = true;
            weaponHandler.autoReload = true;
            weaponHandler.reloadOnLowAmmoThreshold = 5; // снайпер — перезаряжается заранее
            weaponHandler.reloadCheckInterval = 0.3f;
            
            // Настройка RangedAttack для снайпера (если есть)
            var rangedAttack = LoadAction("RangedAttack") as RangedAttackAction;
            if (rangedAttack != null)
            {
                rangedAttack.preferredRange = 25f;
                rangedAttack.fireCooldown = 1.5f;
            }
            
            SavePrefab(prefab, "Sniper");
            Debug.Log($"✅ Создан: Снайпер (Фракция: {ai.faction?.factionName ?? "None"})");
        }

        static void CreateBerserker()
        {
            var prefab = CreateBasePrefab("Berserker");
            var ai = prefab.GetComponent<AIComponent>();
            
            ai.InitializeParameters(
                moveSpd: 6f,
                rotSpd: 8f,
                detectRange: 20f,
                meleeRange: 5f,
                rangedRange: 12f,
                visAngle: 160f,
                visRange: 25f,
                hp: 150f,
                targetSearchInt: 0.3f
            );
            
            ai.SetActions(LoadActions(new[]
            {
                "Pursue", "MeleeAttack", "Flank"
            }));
            TryAssignAISettings(ai);
            
            SavePrefab(prefab, "Berserker");
            Debug.Log($"✅ Создан: Берсерк (Фракция: {ai.faction?.factionName ?? "None"})");
        }

        static void CreateScout()
        {
            var prefab = CreateBasePrefab("Scout");
            var ai = prefab.GetComponent<AIComponent>();
            
            ai.InitializeParameters(
                moveSpd: 3.2f,
                rotSpd: 4.2f,
                detectRange: 20f,
                meleeRange: 2f,
                rangedRange: 25f,
                visAngle: 100f,
                visRange: 25f,
                hp: 80f,
                targetSearchInt: 0.9f
            );
            
            ai.SetActions(LoadActions(new[]
            {
                "Patrol", "Explore", "SearchLastKnown", 
                "Pursue", "Retreat"
            }));
            TryAssignAISettings(ai);
            
            SavePrefab(prefab, "Scout");
            Debug.Log($"✅ Создан: Патрульный (Фракция: {ai.faction?.factionName ?? "None"})");
        }

        // ============================================
        // SQUAD'Ы
        // ============================================

        static void CreateTacticalSquad()
        {
            var squad = CreateSquadPrefab("TacticalSquad", 3);
            var squadComponent = squad.GetComponent<AISquadComponent>();
            
            squadComponent.squadName = "Tactical Squad";
            squadComponent.searchRadius = 20f;
            squadComponent.coordinationInterval = 0.5f;
            squadComponent.minMembersForCoordination = 2;
            
            // Создаём 3 членов отряда
            var member1 = CreateSquadMember(squad.transform, "Rusher", 0);
            ConfigureSquadMember(member1, "Pursue", "RangedAttack", "Flank", "TakeCover", "PeekAndShoot");
            
            var member2 = CreateSquadMember(squad.transform, "Flanker", 1);
            ConfigureSquadMember(member2, "Flank", "RangedAttack", "TakeCover", "PeekAndShoot");
            
            var member3 = CreateSquadMember(squad.transform, "Suppressor", 2);
            ConfigureSquadMember(member3, "RangedAttack", "SuppressionFire", "TakeCover", "PeekAndShoot");
            
            // Добавляем в squad
            squadComponent.members.Add(member1.GetComponent<AIComponent>());
            squadComponent.members.Add(member2.GetComponent<AIComponent>());
            squadComponent.members.Add(member3.GetComponent<AIComponent>());
            
            SavePrefab(squad, "TacticalSquad");
            var firstMember = squad.GetComponentInChildren<AIComponent>();
            Debug.Log($"✅ Создан: Тактический отряд (3 бойца, Фракция: {firstMember?.faction?.factionName ?? "None"})");
        }

        static void CreateAssaultSquad()
        {
            var squad = CreateSquadPrefab("AssaultSquad", 4);
            var squadComponent = squad.GetComponent<AISquadComponent>();
            
            squadComponent.squadName = "Assault Squad";
            squadComponent.searchRadius = 25f;
            squadComponent.coordinationInterval = 0.5f;
            squadComponent.minMembersForCoordination = 2;
            
            // 2 штурмовика (ближний бой + преследование)
            for (int i = 0; i < 2; i++)
            {
                var member = CreateSquadMember(squad.transform, $"Assaulter_{i + 1}", i);
                ConfigureSquadMember(member, "Pursue", "MeleeAttack", "RangedAttack", "TakeCover", "PeekAndShoot");
                
                var ai = member.GetComponent<AIComponent>();
                ai.InitializeParameters(
                    moveSpd: 5f,
                    rotSpd: 7f,
                    detectRange: 15f,
                    meleeRange: 3f,
                    rangedRange: 25f,
                    visAngle: 140f,
                    visRange: 20f,
                    hp: 130f,
                    targetSearchInt: 0.5f
                );
                
                squadComponent.members.Add(ai);
            }
            
            // 2 стрелка (дальний бой + фланг)
            for (int i = 0; i < 2; i++)
            {
                var member = CreateSquadMember(squad.transform, $"Rifleman_{i + 1}", i + 2);
                ConfigureSquadMember(member, "RangedAttack", "Flank", "TakeCover", "PeekAndShoot");
                
                var ai = member.GetComponent<AIComponent>();
                ai.InitializeParameters(
                    moveSpd: 4f,
                    rotSpd: 5f,
                    detectRange: 20f,
                    meleeRange: 2f,
                    rangedRange: 35f,
                    visAngle: 120f,
                    visRange: 25f,
                    hp: 100f,
                    targetSearchInt: 1f
                );
                
                squadComponent.members.Add(ai);
            }
            
            // Получаем данные ДО сохранения (SavePrefab уничтожает объект)
            var firstMember = squad.GetComponentInChildren<AIComponent>();
            var factionName = firstMember?.faction?.factionName ?? "None";
            
            SavePrefab(squad, "AssaultSquad");
            Debug.Log($"✅ Создан: Штурмовой отряд (4 бойца, Фракция: {factionName})");
        }

        static void CreateSniperTeam()
        {
            var squad = CreateSquadPrefab("SniperTeam", 3);
            var squadComponent = squad.GetComponent<AISquadComponent>();
            
            squadComponent.squadName = "Sniper Team";
            squadComponent.searchRadius = 30f;
            squadComponent.coordinationInterval = 1f;
            squadComponent.minMembersForCoordination = 2;
            
            // 2 снайпера
            for (int i = 0; i < 2; i++)
            {
                var sniper = CreateSquadMember(squad.transform, $"Sniper_{i + 1}", i);
                ConfigureSquadMember(sniper, "RangedAttack", "TakeCover", "PeekAndShoot", "SuppressionFire");
                
                var ai = sniper.GetComponent<AIComponent>();
                ai.InitializeParameters(
                    moveSpd: 2.5f,
                    rotSpd: 3f,
                    detectRange: 50f,
                    meleeRange: 1f,
                    rangedRange: 100f,
                    visAngle: 90f,
                    visRange: 100f,
                    hp: 80f,
                    targetSearchInt: 2f
                );
                
                squadComponent.members.Add(ai);
            }
            
            // 1 прикрытие
            var spotter = CreateSquadMember(squad.transform, "Spotter", 2);
            ConfigureSquadMember(spotter, "Patrol", "SearchLastKnown", "RangedAttack", "TakeCover", "PeekAndShoot");
            squadComponent.members.Add(spotter.GetComponent<AIComponent>());
            
            // Получаем данные ДО сохранения (SavePrefab уничтожает объект)
            var firstMember = squad.GetComponentInChildren<AIComponent>();
            var factionName = firstMember?.faction?.factionName ?? "None";
            
            SavePrefab(squad, "SniperTeam");
            Debug.Log($"✅ Создан: Снайперская команда (3 бойца, Фракция: {factionName})");
        }

        // ============================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ============================================

        static GameObject CreateBasePrefab(string name)
        {
            var go = new GameObject(name);
            
            // Основные компоненты
            var factionMember = go.AddComponent<FactionMember>();
            var ai = go.AddComponent<AIComponent>(); // RequireComponent автоматически добавит NavMeshAgent, Rigidbody и FactionMember
            
            // Устанавливаем фракцию
            factionMember.faction = LoadFaction("Enemy");
            
            // Получаем автоматически добавленные компоненты
            var agent = go.GetComponent<NavMeshAgent>();
            var rb = go.GetComponent<Rigidbody>();
            
            // Настройка NavMeshAgent
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            
            // Настройка Rigidbody
            rb.mass = 70f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // Визуализация (капсула)
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Visual";
            capsule.transform.SetParent(go.transform);
            capsule.transform.localPosition = Vector3.up;
            capsule.transform.localRotation = Quaternion.identity;
            capsule.transform.localScale = Vector3.one;
            
            // Удаляем коллайдер с визуализации
            DestroyImmediate(capsule.GetComponent<Collider>());
            
            // Добавляем коллайдер на корень
            var collider = go.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up;
            collider.radius = 0.5f;
            collider.height = 2f;
            
            return go;
        }

        static GameObject CreateBasicAI(string name)
        {
            var prefab = CreateBasePrefab(name);
            var ai = prefab.GetComponent<AIComponent>();
            
            ai.InitializeParameters(
                moveSpd: 3.5f,
                rotSpd: 5f,
                detectRange: 20f,
                meleeRange: 2f,
                rangedRange: 25f,
                visAngle: 120f,
                visRange: 30f,
                hp: 100f,
                targetSearchInt: 0.8f
            );
            
            ai.SetActions(LoadActions(new[] { "Patrol", "Pursue", "RangedAttack", "TakeCover", "PeekAndShoot" }));
            TryAssignAISettings(ai);
            
            // Оружие базового AI
            var weaponHandler = prefab.AddComponent<AIWeaponHandlerComponent>();
            weaponHandler.autoInitialize = true;
            weaponHandler.autoReload = true;
            weaponHandler.reloadOnLowAmmoThreshold = 2;
            weaponHandler.reloadCheckInterval = 0.2f;
            
            SavePrefab(prefab, name);
            Debug.Log($"✅ Создан: {name} (Фракция: {ai.faction?.factionName ?? "None"})");
            return prefab;
        }

        static GameObject CreateSquadPrefab(string name, int memberCount)
        {
            var go = new GameObject(name);
            var squad = go.AddComponent<AISquadComponent>();
            
            squad.autoFindMembers = false; // Члены добавляются вручную
            
            return go;
        }

        static GameObject CreateSquadMember(Transform parent, string name, int index)
        {
            var member = CreateBasePrefab(name);
            member.transform.SetParent(parent);
            
            // Расположение в линию
            member.transform.localPosition = new Vector3(index * 2f, 0, 0);
            member.transform.localRotation = Quaternion.identity;
            
            var ai = member.GetComponent<AIComponent>();
            ai.faction = LoadFaction("Enemy");
            ai.InitializeParameters(
                moveSpd: 3.5f,
                rotSpd: 5f,
                detectRange: 20f,
                meleeRange: 2f,
                rangedRange: 25f,
                visAngle: 120f,
                visRange: 30f,
                hp: 100f,
                targetSearchInt: 0.8f
            );
            
            // Добавляем оружие
            var weaponHandler = member.AddComponent<AIWeaponHandlerComponent>();
            weaponHandler.autoInitialize = true;
            weaponHandler.autoReload = true;
            weaponHandler.reloadOnLowAmmoThreshold = 2;
            weaponHandler.reloadCheckInterval = 0.2f;
            
            return member;
        }

        static void ConfigureSquadMember(GameObject member, params string[] actionNames)
        {
            var ai = member.GetComponent<AIComponent>();
            ai.SetActions(LoadActions(actionNames));
            TryAssignAISettings(ai);
        }

        static UtilityAction[] LoadActions(string[] actionNames)
        {
            var actions = new System.Collections.Generic.List<UtilityAction>();
            
            foreach (var actionName in actionNames)
            {
                var action = LoadAction(actionName);
                if (action != null)
                {
                    actions.Add(action);
                }
            }
            
            return actions.ToArray();
        }

        static UtilityAction LoadAction(string actionName)
        {
            string path = $"{ACTIONS_PATH}/{actionName}.asset";
            var action = AssetDatabase.LoadAssetAtPath<UtilityAction>(path);
            
            if (action == null)
            {
                Debug.LogWarning($"⚠️ Действие не найдено: {path}. Сначала создайте действия через Tools → AI → Generate Action Assets");
            }
            
            return action;
        }

        static void TryAssignAISettings(AIComponent ai)
        {
            if (ai == null) return;
            var settings = AssetDatabase.LoadAssetAtPath<Black_Orbit.Scripts.AI.Runtime.Core.AISettings>(SETTINGS_ASSET_PATH);
            if (settings == null) return;
            var so = new SerializedObject(ai);
            var prop = so.FindProperty("settings");
            if (prop != null)
            {
                prop.objectReferenceValue = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        
        static FactionData LoadFaction(string factionName)
        {
            // Используем выбранную фракцию из UI
            if (_selectedFaction != null)
            {
                return _selectedFaction;
            }
            
            // Fallback: пытаемся загрузить по имени
            string path = $"{FACTIONS_PATH}/{factionName}.asset";
            var faction = AssetDatabase.LoadAssetAtPath<FactionData>(path);
            
            if (faction == null)
            {
                Debug.LogWarning($"⚠️ Фракция не найдена: {path}. Выберите фракцию в UI или создайте через Tools → Faction → Create Default Factions");
            }
            
            return faction;
        }

        static void SavePrefab(GameObject go, string name)
        {
            // Определяем путь с учётом фракции
            string factionFolder = _selectedFaction != null ? _selectedFaction.factionName : "NoFaction";
            string fullPath = $"{OUTPUT_PATH}/{factionFolder}";

            // Создаём базовую папку если не существует
            if (!AssetDatabase.IsValidFolder(OUTPUT_PATH))
            {
                string parentPath = "Assets/Black Orbit/Prefabs/AI";
                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    AssetDatabase.CreateFolder("Assets/Black Orbit/Prefabs", "AI");
                }
                AssetDatabase.CreateFolder(parentPath, "Prefabs");
            }

            // Создаём папку фракции если не существует
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(OUTPUT_PATH, factionFolder);
            }

            string path = $"{fullPath}/{name}.prefab";

            // Проверяем, существует ли уже префаб
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Префаб существует",
                    $"{name}.prefab уже существует. Перезаписать?\n{path}",
                    "Да",
                    "Нет");
                if (!overwrite)
                {
                    // Отклонено: удаляем временный объект и выходим
                    Object.DestroyImmediate(go);
                    return;
                }
                // Удаляем старый, как в генераторе Action Asset
                AssetDatabase.DeleteAsset(path);
            }

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"💾 Сохранён: {path}");
        }
    }
}
