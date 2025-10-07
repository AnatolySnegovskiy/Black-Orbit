using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Movement;
using Black_Orbit.Scripts.AI.Runtime.Perception;
using Black_Orbit.Scripts.AI.Runtime.Targeting;
using Black_Orbit.Scripts.AI.Runtime.Actions;
using Black_Orbit.Scripts.Core.Runtime;
using Black_Orbit.Scripts.Faction.ScriptableObjects;
using Black_Orbit.Scripts.Faction.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Black_Orbit.Scripts.AI.Runtime
{
    /// <summary>
    /// Основной компонент AI с Utility-системой принятия решений.
    /// Управляет восприятием, навигацией и выбором действий на основе utility-оценок.
    /// Поддерживает систему фракций для определения союзников и врагов.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(FactionMember))]
    [RequireComponent(typeof(Health))]
    public class AI : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<AI> s_all = new System.Collections.Generic.List<AI>();
        // === Modularized services ===
        private AIBlackboard _bb;
        private AIMovement _movement;
        private AIPerception _perception;
        private AITargeting _targeting;
        private AIActionSelector _selector;
        // Фракция теперь берётся из FactionMember компонента
        private FactionMember _factionMember;

        /// <summary>Фракция этого AI (из FactionMember)</summary>
        public FactionData faction
        {
            get => _factionMember != null ? _factionMember.faction : null;
            set
            {
                if (_factionMember == null)
                    _factionMember = GetComponent<FactionMember>();
                if (_factionMember != null)
                    _factionMember.faction = value;
            }
        }

        private void OnHealthDamaged(int amount, int newHealth)
        {
            if (amount <= 0) return;
            // Перевод части урона в подавление (коэф. под тюнинг)
            int maxHp = healthComponent != null ? healthComponent.MaxHealth : 100;
            float suppression = Mathf.Clamp01(amount / (float)Mathf.Max(1, maxHp)) * 0.8f;
            suppression = Mathf.Max(0.05f, suppression);
            AddSuppression(suppression);
            if (Squad != null)
            {
                Squad.ReportSuppression(this, suppression * 0.5f);
            }
        }

        /// <summary>
        /// Сообщить этому AI о шуме (позиция и уровень 0..1)
        /// </summary>
        public void HearNoise(Vector3 position, float level)
        {
            if (_bb == null) return;
            _bb.HeardNoisePos = position;
            _bb.NoiseLevel = Mathf.Clamp01(Mathf.Max(level, _bb.NoiseLevel * 0.8f));
        }

        /// <summary>
        /// Глобальная рассылка шума всем AI (с затуханием по расстоянию)
        /// </summary>
        public static void EmitNoise(Vector3 position, float level, float maxRadius = 25f)
        {
            if (s_all == null || s_all.Count == 0) return;
            foreach (var ai in s_all)
            {
                if (ai == null || !ai.gameObject.activeInHierarchy) continue;
                float dist = Vector3.Distance(ai.transform.position, position);
                if (dist > maxRadius) continue;
                float att = Mathf.Clamp01(1f - dist / Mathf.Max(0.01f, maxRadius));
                ai.HearNoise(position, Mathf.Clamp01(level * att));
            }
        }

        /// <summary>
        /// Возвращает true, если можно вести огонь: есть цель, она активна, жива и есть LOS
        /// </summary>
        public bool IsEngagementAllowed()
        {
            if (Target == null || !Target.gameObject.activeInHierarchy) return false;
            var h = Target.GetComponent<Health>();
            if (h != null && h.IsDead) return false;
            return hasLineOfSight;
        }

        [Header("Ссылки")]
        [SerializeField]
        [Tooltip("Цель для атаки/прицеливания")] private Transform attackTarget;
        
        /// <summary>Цель для атаки/прицеливания</summary>
        public Transform AttackTarget
        {
            get => attackTarget;
            set
            {
                attackTarget = value;
                if (_bb != null) _bb.AttackTarget = attackTarget;
            }
        }

        /// <summary>Навигационная цель (точка), куда движемся при отсутствии LOS/для поиска</summary>
        public Vector3 NavTargetPos
        {
            get => _bb != null ? _bb.NavTargetPos : lastSeenTargetPos;
            set
            {
                if (_bb != null) _bb.NavTargetPos = value;
                lastSeenTargetPos = value;
            }
        }

        /// <summary>Алиас для обратной совместимости: Target == AttackTarget</summary>
        public Transform Target
        {
            get => AttackTarget;
            set => AttackTarget = value;
        }
        
        [SerializeField]
        [Tooltip("Rigidbody для физического движения (опционально, если нет NavMeshAgent)")]
        private Rigidbody rb;
        
        [SerializeField]
        [Tooltip("NavMeshAgent для навигации по уровню (рекомендуется)")]
        private NavMeshAgent agent;
        
        /// <summary>Публичный доступ к NavMeshAgent</summary>
        public NavMeshAgent Agent => agent;
        
        /// <summary>Публичный доступ к Rigidbody</summary>
        public Rigidbody Rb => rb;

        [Header("Параметры движения")]
        [SerializeField]
        [Tooltip("Скорость движения (м/с) — используется если нет NavMeshAgent")]
        private float moveSpeed = 3f;
        
        [SerializeField]
        [Tooltip("Скорость поворота к цели (множитель). Выше = быстрее поворот")]
        private float rotationSpeed = 5f;
        
        /// <summary>Публичный доступ к скорости движения</summary>
        public float MoveSpeed => moveSpeed;
        
        /// <summary>Публичный доступ к скорости поворота</summary>
        public float RotationSpeed => rotationSpeed;

        [Header("Параметры восприятия")]
        [SerializeField]
        [Tooltip("Дальность обнаружения целей (метры)")]
        private float detectionRange = 10f;
        
        [SerializeField]
        [Tooltip("Угол обзора (градусы, 0-180). 120° = широкий обзор")]
        [Range(0f, 180f)] private float visionAngle = 120f;
        
        [SerializeField]
        [Tooltip("Дальность зрения (метры). Должна быть >= detectionRange")]
        private float visionRange = 15f;
        
        [SerializeField]
        [Tooltip("Слои препятствий, блокирующих обзор")]
        private LayerMask obstacleMask;
        
        [SerializeField]
        [Tooltip("Слои объектов-укрытий для TakeCoverAction")]
        private LayerMask coverMask;
        
        [SerializeField]
        [Tooltip("Интервал поиска новых целей (секунды). 0 = каждый кадр")]
        private float targetSearchInterval = 1f;
        
        /// <summary>Публичный доступ к дальности обнаружения</summary>
        public float DetectionRange => detectionRange;
        
        /// <summary>Публичный доступ к интервалу поиска целей</summary>
        public float TargetSearchInterval
        {
            get => targetSearchInterval;
            set => targetSearchInterval = value;
        }
        
        /// <summary>Публичный доступ к углу обзора</summary>
        public float VisionAngle => visionAngle;
        
        /// <summary>Публичный доступ к дальности зрения</summary>
        public float VisionRange => visionRange;
        
        /// <summary>Публичный доступ к маске препятствий</summary>
        public LayerMask ObstacleMask => obstacleMask;
        
        /// <summary>Публичный доступ к маске укрытий</summary>
        public LayerMask CoverMask => coverMask;

        [Header("Дальности атак")]
        [SerializeField]
        [Tooltip("Дальность ближней атаки (метры). Для берсерков: 3-5м, для обычных: 2м")]
        private float meleeAttackRange = 2f;
        
        [SerializeField]
        [Tooltip("Дальность дальней атаки (метры). Для снайперов: 100-300м, для обычных: 20-30м")]
        private float rangedAttackRange = 20f;
        
        /// <summary>Публичный доступ к дальности ближней атаки</summary>
        public float MeleeAttackRange => meleeAttackRange;
        
        /// <summary>Публичный доступ к дальности дальней атаки</summary>
        public float RangedAttackRange => rangedAttackRange;

        [Header("Здоровье")]
        [SerializeField]
        [Tooltip("Компонент здоровья (Core)")]
        private Health healthComponent;
        
        /// <summary>Публичный доступ к здоровью (значение текущего HP)</summary>
        public float Health
        {
            get => healthComponent != null ? healthComponent.CurrentHealth : 0f;
            set
            {
                if (healthComponent != null)
                {
                    healthComponent.SetHealth(Mathf.RoundToInt(value));
                }
            }
        }

        [Header("Патрулирование")]
        [SerializeField]
        [Tooltip("Точки патруля (опционально). Если не заданы, бот будет блуждать случайно")]
        private Transform[] patrolPoints;
        
        [HideInInspector] public int patrolIndex;
        
        /// <summary>Публичный доступ к точкам патруля</summary>
        public Transform[] PatrolPoints => patrolPoints;

        [Header("Utility AI")]
        [SerializeField]
        [Tooltip("Список доступных действий. Система автоматически выбирает лучшее по utility-оценке")]
        private UtilityAction[] actions;
        
        private UtilityAction _currentAction;
        
        /// <summary>Публичный доступ к действиям</summary>
        public UtilityAction[] Actions => actions;
        
        /// <summary>Публичный доступ к текущему действию</summary>
        public UtilityAction CurrentAction => _currentAction;

        /// <summary>Текущее выбранное действие по каналу Movement (для отладки/гизмо)</summary>
        public UtilityAction CurrentMovementAction => _selector != null ? _selector.CurrentMovement : null;

        /// <summary>Текущее выбранное действие по каналу Combat (для отладки/гизмо)</summary>
        public UtilityAction CurrentCombatAction => _selector != null ? _selector.CurrentCombat : null;
        
        /// <summary>Устанавливает действия (для Editor)</summary>
        public void SetActions(UtilityAction[] newActions) => actions = newActions;

        [Header("Debug & Gizmos")]
        [SerializeField] private AISettings settings;
        [SerializeField] public bool showGizmosVision = true;
        [SerializeField] public bool showGizmosDetection = true;
        [SerializeField] public bool showGizmosAttackRanges = true;
        [SerializeField] public bool showGizmosLastSeen = true;
        [SerializeField] public bool showGizmosNavTarget = true;
        [SerializeField] public bool showGizmosInfoLabel = true;

        [Header("Suppression")]
        [Tooltip("Скорость затухания подавления (в секунду)")]
        [SerializeField] private float suppressionDecayRate = 0.25f;
        /// <summary>Текущий уровень подавления (0..1)</summary>
        public float SuppressionLevel => _bb != null ? _bb.SuppressionLevel : 0f;
        /// <summary>Добавляет подавление (0..1), кумулятивно</summary>
        public void AddSuppression(float amount)
        {
            if (_bb == null) return;
            _bb.SuppressionLevel = Mathf.Clamp01(_bb.SuppressionLevel + Mathf.Max(0f, amount));
        }
        
        /// <summary>Инициализация параметров (для Editor/Prefab Generator)</summary>
        public void InitializeParameters(
            float moveSpd, float rotSpd, float detectRange,
            float meleeRange, float rangedRange, float visAngle, float visRange,
            float hp, float targetSearchInt)
        {
            moveSpeed = moveSpd;
            rotationSpeed = rotSpd;
            detectionRange = detectRange;
            meleeAttackRange = meleeRange;
            rangedAttackRange = rangedRange;
            visionAngle = visAngle;
            visionRange = visRange;
            if (healthComponent == null) healthComponent = GetComponent<Health>();
            if (healthComponent != null)
            {
                healthComponent.OnDamaged -= OnHealthDamaged; // гарантируем единственную подписку
                healthComponent.OnDamaged += OnHealthDamaged;
            }
            if (healthComponent != null)
            {
                // Не меняем MaxHealth здесь, только выставляем текущее
                healthComponent.SetHealth(Mathf.RoundToInt(hp));
            }
            targetSearchInterval = targetSearchInt;
        }

        [Header("Squad")]
        [SerializeField]
        [Tooltip("Отряд, к которому принадлежит этот AI (опционально)")]
        private AISquad squad;
        
        /// <summary>Публичный доступ к отряду</summary>
        public AISquad Squad
        {
            get => squad;
            set => squad = value;
        }

        /// <summary>Нормализованное здоровье (0..1)</summary>
        public float HealthNormalized => healthComponent != null ? healthComponent.Normalized : 0f;

        // === Чёрная доска (Blackboard) — данные для принятия решений ===
        [HideInInspector] public Vector3 lastSeenTargetPos = Vector3.positiveInfinity; // Инициализируем невалидным значением
        [HideInInspector] public float timeSinceLastSeen = float.MaxValue; // Никогда не видел
        [HideInInspector] public bool hasLineOfSight;
        
        // === Система приказов от squad ===
        private UtilityAction _orderedAction;
        private float _orderDuration;
        private float _orderTimer;
        
        private float _targetSearchTimer;

        // === Обратная совместимость ===
        /// <summary>Алиас для AttackTarget (для совместимости со старыми экшенами)</summary>
        public Transform Player
        {
            get => AttackTarget;
            set => AttackTarget = value;
        }
        
        /// <summary>Алиас для lastSeenTargetPos (для совместимости)</summary>
        public Vector3 LastSeenPlayerPos
        {
            get => lastSeenTargetPos;
            set => lastSeenTargetPos = value;
        }

        /// <summary>Алиас для rangedAttackRange (для совместимости со старыми экшенами)</summary>
        public float attackRange
        {
            get => rangedAttackRange;
            set => rangedAttackRange = value;
        }

        void OnEnable()
        {
            // Получаем ссылку на FactionMember
            _factionMember = GetComponent<FactionMember>();
            if (!s_all.Contains(this)) s_all.Add(this);
            // Подписка на урон для подавления
            if (healthComponent == null) healthComponent = GetComponent<Health>();
            if (healthComponent != null)
            {
                healthComponent.OnDamaged -= OnHealthDamaged; // на всякий случай
                healthComponent.OnDamaged += OnHealthDamaged;
            }
        }

        void OnDisable() 
        { 
            s_all.Remove(this);
            if (healthComponent != null) healthComponent.OnDamaged -= OnHealthDamaged;
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.updateRotation = false; // Вращение контролируем вручную через LookAt
                agent.updateUpAxis = false;
            }

            if (healthComponent == null) healthComponent = GetComponent<Health>();

            // Initialize services
            if (_bb == null) _bb = new AIBlackboard();
            _bb.AttackTarget = attackTarget;
            _bb.NavTargetPos = lastSeenTargetPos;
            _bb.HasLineOfSight = hasLineOfSight;
            _bb.LastSeenTargetPos = lastSeenTargetPos;
            _bb.TimeSinceLastSeen = timeSinceLastSeen;

            if (_movement == null) _movement = new AIMovement(this, agent, rb);
            if (_perception == null) _perception = new AIPerception();
            if (_targeting == null) _targeting = new AITargeting();
            if (_selector == null) _selector = new AIActionSelector();

            // Apply settings (gizmo defaults) if provided
            if (settings != null)
            {
                showGizmosVision = settings.showGizmosVision;
                showGizmosDetection = settings.showGizmosDetection;
                showGizmosAttackRanges = settings.showGizmosAttackRanges;
                showGizmosLastSeen = settings.showGizmosLastSeen;
                showGizmosNavTarget = settings.showGizmosNavTarget;
                showGizmosInfoLabel = settings.showGizmosInfoLabel;
            }
        }

        void Update()
        {
            UpdatePerception();
            UpdateTargetSearch();
            UpdateRotation();
            SelectAndExecuteAction();

            // Затухание подавления
            if (_bb != null && _bb.SuppressionLevel > 0f)
            {
                _bb.SuppressionLevel = Mathf.Max(0f, _bb.SuppressionLevel - suppressionDecayRate * Time.deltaTime);
            }

            // Затухание шума и авто-навигация на шум при отсутствии цели/LOS
            if (_bb != null && _bb.NoiseLevel > 0f)
            {
                _bb.NoiseLevel = Mathf.Max(0f, _bb.NoiseLevel - 0.5f * Time.deltaTime);
                if (_bb.NoiseLevel <= 0f) _bb.HeardNoisePos = Vector3.positiveInfinity;
                else
                {
                    // Если нет явной цели, используем шум как навточку
                    if (AttackTarget == null && !float.IsPositiveInfinity(_bb.HeardNoisePos.x))
                    {
                        NavTargetPos = _bb.HeardNoisePos;
                    }
                }
            }
        }

        /// <summary>
        /// Обновляет поворот AI к цели
        /// </summary>
        void UpdateRotation()
        {
            if (_movement != null)
            {
                _movement.UpdateRotation(AttackTarget);
            }
            else
            {
                if (AttackTarget == null)
                {
                    // Поворачиваемся к навигационной цели, если она валидна
                    if (float.IsPositiveInfinity(NavTargetPos.x)) return;
                    Vector3 dirNav = (NavTargetPos - transform.position); dirNav.y = 0;
                    if (dirNav.sqrMagnitude <= 0.01f) return;
                    dirNav.Normalize();
                    Quaternion rotNav = Quaternion.LookRotation(dirNav);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotNav, Time.deltaTime * rotationSpeed);
                    return;
                }
                Vector3 direction = (AttackTarget.position - transform.position);
                direction.y = 0;
                if (direction.sqrMagnitude > 0.01f)
                {
                    direction.Normalize();
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }

        /// <summary>
        /// Периодически ищет новую цель среди враждебных фракций
        /// </summary>
        void UpdateTargetSearch()
        {
            if (_targeting != null && _bb != null)
            {
                _targeting.Update(this, _bb);
            }
            else
            {
                _targetSearchTimer -= Time.deltaTime;
                if (_targetSearchTimer <= 0f)
                {
                    _targetSearchTimer = targetSearchInterval;
                    // Fallback path without targeting service: keep current target or wait until service is available
                }
            }
        }

        // FindNearestHostile() перенесён в AITargeting

        /// <summary>
        /// Проверяет, является ли указанный AI враждебным
        /// </summary>
        public bool IsHostile(AI other)
        {
            if (faction == null || other == null || other.faction == null) return false;
            return faction.IsHostile(other.faction);
        }

        /// <summary>
        /// Проверяет, является ли указанный AI союзником
        /// </summary>
        public bool IsAllied(AI other)
        {
            if (faction == null || other == null || other.faction == null) return false;
            return faction.IsAllied(other.faction);
        }

        /// <summary>
        /// Получает отношение к другому AI (-1 ... 1)
        /// </summary>
        public float GetRelationship(AI other)
        {
            if (faction == null || other == null || other.faction == null) return 0f;
            return faction.GetRelationship(other.faction);
        }

        /// <summary>
        /// Проверяет, должен ли AI атаковать цель на основе отношений
        /// </summary>
        public bool ShouldAttack(AI other)
        {
            if (other == null) return false;
            
            float relationship = GetRelationship(other);
            
            // <= -0.5: Враг или лютый враг - атакует
            if (relationship <= -0.5f) return true;
            
            // -0.5 ... -0.3: Недружелюбный - может атаковать (50% шанс или при провокации)
            if (relationship > -0.5f && relationship < -0.3f)
            {
                // Можно добавить логику провокации или случайности
                return false; // По умолчанию не атакует
            }
            
            return false;
        }

        /// <summary>
        /// Проверяет, должен ли AI помогать цели на основе отношений
        /// </summary>
        public bool ShouldHelp(AI other)
        {
            if (other == null) return false;
            
            float relationship = GetRelationship(other);
            
            // >= 0.8: Лучший друг - всегда помогает
            if (relationship >= 0.8f) return true;
            
            // 0.5 ... 0.8: Друг - может помочь (вероятность)
            if (relationship >= 0.5f && relationship < 0.8f)
            {
                return Random.value > 0.3f; // 70% шанс помочь
            }
            
            return false;
        }

        /// <summary>
        /// Получает агрессивность к цели (0 = игнорирует, 1 = атакует без пощады)
        /// </summary>
        public float GetAggressionLevel(AI other)
        {
            if (other == null) return 0f;
            
            float relationship = GetRelationship(other);
            
            // Конвертируем отношение в агрессивность
            // -1.0 (лютый враг) -> 1.0 (максимальная агрессия)
            // -0.5 (враг) -> 0.5 (средняя агрессия)
            // 0.0 (нейтрал) -> 0.0 (нет агрессии)
            // 1.0 (друг) -> 0.0 (нет агрессии)
            
            if (relationship < 0)
            {
                return Mathf.Clamp01(-relationship); // -1 -> 1, -0.5 -> 0.5
            }
            
            return 0f;
        }

        /// <summary>
        /// Получает список всех союзников в радиусе
        /// </summary>
        public List<AI> GetAlliesInRange(float range)
        {
            // Используем внутренний список агентов, чтобы избежать FindObjectsOfType и аллокаций.
            List<AI> allies = new List<AI>();
            if (faction == null || s_all == null || s_all.Count == 0) return allies;
            Vector3 selfPos = transform.position;
            float rangeSqr = range * range;
            for (int i = 0; i < s_all.Count; i++)
            {
                var other = s_all[i];
                if (other == null || other == this || other.faction == null) continue;
                if (!other.gameObject.activeInHierarchy) continue;
                if (!faction.IsAllied(other.faction)) continue;
                Vector3 to = other.transform.position - selfPos; to.y = 0f;
                if (to.sqrMagnitude <= rangeSqr)
                {
                    allies.Add(other);
                }
            }
            return allies;
        }

        /// <summary>
        /// Выбирает действие с наивысшей utility-оценкой и выполняет его
        /// </summary>
        void SelectAndExecuteAction()
        {
            if (_selector != null && _bb != null)
            {
                _selector.SelectAndExecute(this, _bb, actions);
                // Синхронизируем поле только для обратной совместимости/отладки
                _currentAction = _selector.CurrentAction;

                // Централизованный контроль спуска: если нет боевого экшена или нельзя вести огонь — отпускаем спуск
                bool shouldHoldTrigger = _selector.CurrentCombat != null && IsEngagementAllowed();
                if (!shouldHoldTrigger)
                {
                    if (TryGetComponent<AIWeaponHandler>(out var handler)) handler.ReleaseTrigger();
                    else if (TryGetComponent<WeaponSystem.Base.IWeapon>(out var weapon)) weapon.ReleaseTrigger();
                }
            }
            else
            {
                // Fallback: прежняя логика выбора (упрощённо)
                float bestScore = -1f;
                UtilityAction bestAction = null;
                foreach (var action in actions)
                {
                    float score = action.Evaluate(this);
                    if (score > bestScore) { bestScore = score; bestAction = action; }
                }
                if (bestAction != null)
                {
                    _currentAction = bestAction;
                    _currentAction.Execute(this);
                }
            }
        }

        /// <summary>
        /// Приказывает AI выполнить конкретное действие (вызывается из AISquad)
        /// </summary>
        /// <param name="actionName">Название действия (например, "Pursue", "Flank")</param>
        /// <param name="duration">Длительность приказа в секундах (по умолчанию 3 сек)</param>
        public void OrderAction(string actionName, float duration = 3f)
        {
            if (_selector != null)
            {
                _selector.OrderAction(this, actionName, duration, actions);
            }
            else
            {
                var action = System.Array.Find(actions, a => a.name.Contains(actionName));
                if (action != null)
                {
                    _orderedAction = action;
                    _orderDuration = duration;
                    _orderTimer = duration;
                    Debug.Log($"📋 [{faction}] Получен приказ: {actionName} на {duration} сек");
                }
                else
                {
                    Debug.LogWarning($"[AI] Действие '{actionName}' не найдено в списке actions!");
                }
            }
        }

        /// <summary>
        /// Отменяет текущий приказ и возвращается к обычному поведению
        /// </summary>
        public void CancelOrder()
        {
            if (_selector != null) { _selector.CancelOrder(); return; }
            _orderedAction = null;
            _orderTimer = 0f;
        }

        /// <summary>
        /// Проверяет, выполняет ли AI приказ в данный момент
        /// </summary>
        public bool IsFollowingOrder => _selector != null ? _selector.IsFollowingOrder : (_orderedAction != null && _orderTimer > 0f);

        /// <summary>
        /// Останавливает движение
        /// </summary>
        public void Stop()
        {
            if (_movement != null) { _movement.Stop(); return; }
            if (agent != null) agent.ResetPath();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        /// <summary>
        /// Перемещает к целевой точке через NavMeshAgent или Rigidbody
        /// </summary>
        public void MoveTo(Vector3 targetPos)
        {
            if (_movement != null) { _movement.MoveTo(targetPos); return; }
            if (agent != null && agent.isOnNavMesh) agent.SetDestination(targetPos);
            else if (rb != null)
            {
                Vector3 dir = (targetPos - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir = dir.normalized;
                    rb.MovePosition(rb.position + dir * (moveSpeed * Time.deltaTime));
                }
            }
        }

        /// <summary>
        /// Проверяет, достиг ли целевой точки
        /// </summary>
        public bool IsAtDestination(float tolerance = 0.5f)
        {
            if (_movement != null)
            {
                return _movement.IsAtDestination(NavTargetPos, tolerance);
            }
            if (agent != null && agent.hasPath)
            {
                return !float.IsPositiveInfinity(agent.remainingDistance) && agent.remainingDistance <= tolerance;
            }
            return Vector3.Distance(transform.position, NavTargetPos) <= tolerance;
        }

        /// <summary>
        /// Плавно поворачивает к целевой точке
        /// </summary>
        public void LookAt(Vector3 point, float turnSpeed = 10f)
        {
            if (_movement != null) { _movement.LookAt(point, turnSpeed); return; }
            Vector3 dir = (point - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }
        }

        /// <summary>
        /// Обновляет систему восприятия: проверяет видимость цели с учётом угла обзора и препятствий
        /// </summary>
        void UpdatePerception()
        {
            // Delegate to perception service updating blackboard
            if (_perception != null && _bb != null)
            {
                _perception.Update(this, _bb);
                // Sync legacy fields for backward compatibility
                hasLineOfSight = _bb.HasLineOfSight;
                lastSeenTargetPos = _bb.LastSeenTargetPos;
                timeSinceLastSeen = _bb.TimeSinceLastSeen;
            }
            else
            {
                // Fallback to legacy behavior if service is missing
                hasLineOfSight = false;
                if (AttackTarget == null) { timeSinceLastSeen += Time.deltaTime; return; }

                Vector3 toTarget = AttackTarget.position - transform.position;
                float dist = toTarget.magnitude;
                if (dist <= Mathf.Max(visionRange, detectionRange))
                {
                    Vector3 dir = toTarget.normalized;
                    float angle = Vector3.Angle(transform.forward, dir);
                    if (angle <= visionAngle * 0.5f)
                    {
                        if (!Physics.Raycast(transform.position + Vector3.up * 1.6f, dir, out RaycastHit _, dist, obstacleMask))
                        {
                            hasLineOfSight = true;
                        }
                    }
                }
                if (hasLineOfSight)
                {
                    lastSeenTargetPos = AttackTarget.position;
                    timeSinceLastSeen = 0f;
                }
                else
                {
                    timeSinceLastSeen += Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Отрисовка конуса зрения и других debug-элементов в Scene View
        /// </summary>
        void OnDrawGizmosSelected()
        {
            Debugging.AIGizmosDrawer.Draw(this);
        }
    }
}
