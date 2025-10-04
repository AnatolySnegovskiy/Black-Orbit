using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;
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
    public class AI : MonoBehaviour
    {
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

        [Header("Ссылки")]
        [SerializeField]
        [Tooltip("Текущая цель для AI (автоматически находится по фракциям или назначается вручную)")]
        private Transform target;
        
        /// <summary>Публичный доступ к текущей цели</summary>
        public Transform Target
        {
            get => target;
            set => target = value;
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
        [Tooltip("Текущее здоровье (0-100)")]
        [Range(0f, 100f)] private float health = 100f;
        
        /// <summary>Публичный доступ к здоровью</summary>
        public float Health
        {
            get => health;
            set => health = Mathf.Clamp(value, 0f, 100f);
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
        
        /// <summary>Устанавливает действия (для Editor)</summary>
        public void SetActions(UtilityAction[] newActions) => actions = newActions;
        
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
            health = hp;
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
        public float HealthNormalized => health / 100f;

        // === Чёрная доска (Blackboard) — данные для принятия решений ===
        [HideInInspector] public Vector3 lastSeenTargetPos = Vector3.positiveInfinity; // Инициализируем невалидным значением
        [HideInInspector] public float timeSinceLastSeen = float.MaxValue; // Никогда не видел
        [HideInInspector] public bool hasLineOfSight;
        
        // === Система приказов от squad ===
        private UtilityAction _orderedAction;
        private float _orderDuration;
        private float _orderTimer;
        
        private float _targetSearchTimer;
        private static Dictionary<FactionData, List<AI>> _factionMembers = new Dictionary<FactionData, List<AI>>();

        // === Обратная совместимость ===
        /// <summary>Алиас для target (для совместимости со старыми экшенами)</summary>
        public Transform Player
        {
            get => target;
            set => target = value;
        }
        
        /// <summary>Алиас для lastSeenTargetPos (для совместимости)</summary>
        public Vector3 LastSeenPlayerPos
        {
            get => lastSeenTargetPos;
            set => lastSeenTargetPos = value;
        }

        /// <summary>Алиас для rangedAttackRange (для совместимости со старыми экшенами)</summary>
        [System.Obsolete("Используйте meleeAttackRange или rangedAttackRange вместо attackRange")]
        public float attackRange
        {
            get => rangedAttackRange;
            set => rangedAttackRange = value;
        }

        void OnEnable()
        {
            // Получаем ссылку на FactionMember
            _factionMember = GetComponent<FactionMember>();
            
            // Регистрируем себя в списке фракции
            if (faction != null)
            {
                if (!_factionMembers.ContainsKey(faction))
                {
                    _factionMembers[faction] = new List<AI>();
                }
                _factionMembers[faction].Add(this);
            }
        }

        void OnDisable()
        {
            // Удаляем себя из списка фракции
            if (faction != null && _factionMembers.ContainsKey(faction))
            {
                _factionMembers[faction].Remove(this);
            }
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
        }

        void Update()
        {
            UpdateTargetSearch();
            UpdatePerception();
            UpdateRotation();
            SelectAndExecuteAction();
        }

        /// <summary>
        /// Обновляет поворот AI к цели
        /// </summary>
        void UpdateRotation()
        {
            if (target == null) return;

            // Направление к цели
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // Игнорируем вертикальную составляющую

            if (direction.sqrMagnitude > 0.01f)
            {
                // Плавный поворот к цели
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        /// <summary>
        /// Периодически ищет новую цель среди враждебных фракций
        /// </summary>
        void UpdateTargetSearch()
        {
            _targetSearchTimer -= Time.deltaTime;
            if (_targetSearchTimer <= 0f)
            {
                _targetSearchTimer = targetSearchInterval;
                
                // Если текущая цель мертва или null, ищем новую
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    target = FindNearestHostile();
                }
            }
        }

        /// <summary>
        /// Находит ближайшего врага из враждебных фракций
        /// Ищет как AI, так и FactionMember компоненты
        /// </summary>
        Transform FindNearestHostile()
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;

            if (faction == null) return null;
            
            // Получаем враждебные фракции
            var hostileFactions = FactionManager.Instance.GetHostileFactions(faction);
            
            // Поиск среди AI
            foreach (var hostileFaction in hostileFactions)
            {
                if (!_factionMembers.ContainsKey(hostileFaction)) continue;

                foreach (var hostile in _factionMembers[hostileFaction])
                {
                    if (hostile == null || !hostile.gameObject.activeInHierarchy) continue;
                    
                    float dist = Vector3.Distance(transform.position, hostile.transform.position);
                    if (dist < nearestDist && dist <= detectionRange)
                    {
                        nearestDist = dist;
                        nearest = hostile.transform;
                    }
                }
            }
            
            // Поиск среди FactionMember (игроки, NPC без AI)
            var factionMembers = FindObjectsOfType<FactionMember>();
            foreach (var member in factionMembers)
            {
                if (member == null || !member.gameObject.activeInHierarchy) continue;
                if (member.faction == null || !faction.IsHostile(member.faction)) continue;
                
                float dist = Vector3.Distance(transform.position, member.transform.position);
                if (dist < nearestDist && dist <= detectionRange)
                {
                    nearestDist = dist;
                    nearest = member.transform;
                }
            }

            return nearest;
        }

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
            List<AI> allies = new List<AI>();
            if (faction == null) return allies;
            
            var alliedFactions = FactionManager.Instance.GetAlliedFactions(faction);
            
            // Проверяем свою фракцию
            if (_factionMembers.ContainsKey(faction))
            {
                foreach (var ally in _factionMembers[faction])
                {
                    if (ally == this || ally == null) continue;
                    if (Vector3.Distance(transform.position, ally.transform.position) <= range)
                    {
                        allies.Add(ally);
                    }
                }
            }
            
            // Проверяем союзные фракции
            foreach (var alliedFaction in alliedFactions)
            {
                if (!_factionMembers.ContainsKey(alliedFaction)) continue;
                
                foreach (var ally in _factionMembers[alliedFaction])
                {
                    if (ally == null) continue;
                    if (Vector3.Distance(transform.position, ally.transform.position) <= range)
                    {
                        allies.Add(ally);
                    }
                }
            }
            
            return allies;
        }

        /// <summary>
        /// Выбирает действие с наивысшей utility-оценкой и выполняет его
        /// </summary>
        void SelectAndExecuteAction()
        {
            // Проверяем, есть ли приказ от squad
            if (_orderedAction != null)
            {
                _orderTimer -= Time.deltaTime;
                if (_orderTimer > 0f)
                {
                    // Выполняем приказанное действие
                    if (_currentAction != _orderedAction)
                    {
                        _currentAction = _orderedAction;
                        Debug.Log($"🎖️ [{faction}] Выполняю приказ: {_currentAction.name}");
                    }
                    _currentAction.Execute(this);
                    return;
                }
                else
                {
                    // Приказ истёк, возвращаемся к обычному поведению
                    _orderedAction = null;
                }
            }

            float bestScore = -1f;
            UtilityAction bestAction = null;

            // Оцениваем все доступные действия
            foreach (var action in actions)
            {
                float score = action.Evaluate(this);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAction = action;
                }
            }

            // Выполняем лучшее действие
            if (bestAction != null)
            {
                if (_currentAction != bestAction)
                {
                    _currentAction = bestAction;
                    Debug.Log($"🤖 [{faction}] Выбрано действие: {_currentAction.name} (Score={bestScore:F2})");
                }
                _currentAction.Execute(this);
            }
        }

        /// <summary>
        /// Приказывает AI выполнить конкретное действие (вызывается из AISquad)
        /// </summary>
        /// <param name="actionName">Название действия (например, "Pursue", "Flank")</param>
        /// <param name="duration">Длительность приказа в секундах (по умолчанию 3 сек)</param>
        public void OrderAction(string actionName, float duration = 3f)
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

        /// <summary>
        /// Отменяет текущий приказ и возвращается к обычному поведению
        /// </summary>
        public void CancelOrder()
        {
            _orderedAction = null;
            _orderTimer = 0f;
        }

        /// <summary>
        /// Проверяет, выполняет ли AI приказ в данный момент
        /// </summary>
        public bool IsFollowingOrder => _orderedAction != null && _orderTimer > 0f;

        /// <summary>
        /// Останавливает движение
        /// </summary>
        public void Stop()
        {
            if (agent != null)
            {
                agent.ResetPath();
            }
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Перемещает к целевой точке через NavMeshAgent или Rigidbody
        /// </summary>
        public void MoveTo(Vector3 targetPos)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(targetPos);
            }
            else if (rb != null)
            {
                Vector3 dir = (targetPos - transform.position).normalized;
                rb.MovePosition(rb.position + dir * (moveSpeed * Time.deltaTime));
            }
        }

        /// <summary>
        /// Проверяет, достиг ли целевой точки
        /// </summary>
        public bool IsAtDestination(float tolerance = 0.5f)
        {
            if (agent != null && agent.hasPath)
            {
                return !float.IsPositiveInfinity(agent.remainingDistance) && agent.remainingDistance <= tolerance;
            }
            return Vector3.Distance(transform.position, lastSeenTargetPos) <= tolerance;
        }

        /// <summary>
        /// Плавно поворачивает к целевой точке
        /// </summary>
        public void LookAt(Vector3 point, float turnSpeed = 10f)
        {
            Vector3 dir = (point - transform.position);
            dir.y = 0f;
            
            // Проверяем что вектор не нулевой
            if (dir.sqrMagnitude > 0.001f)
            {
                // Нормализуем перед передачей в LookRotation
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
            hasLineOfSight = false;
            if (target == null) { timeSinceLastSeen += Time.deltaTime; return; }

            Vector3 toTarget = target.position - transform.position;
            float dist = toTarget.magnitude;
            
            // Проверяем дистанцию
            if (dist <= Mathf.Max(visionRange, detectionRange))
            {
                Vector3 dir = toTarget.normalized;
                float angle = Vector3.Angle(transform.forward, dir);
                
                // Проверяем угол обзора
                if (angle <= visionAngle * 0.5f)
                {
                    // Проверяем препятствия через Raycast
                    if (!Physics.Raycast(transform.position + Vector3.up * 1.6f, dir, out RaycastHit _, dist, obstacleMask))
                    {
                        hasLineOfSight = true;
                    }
                }
            }

            // Обновляем чёрную доску
            if (hasLineOfSight)
            {
                lastSeenTargetPos = target.position;
                timeSinceLastSeen = 0f;
            }
            else
            {
                timeSinceLastSeen += Time.deltaTime;
            }
        }

        /// <summary>
        /// Отрисовка конуса зрения и других debug-элементов в Scene View
        /// </summary>
        void OnDrawGizmosSelected()
        {
            // Цвет зависит от состояния
            Color visionColor = hasLineOfSight ? Color.red : Color.yellow;
            visionColor.a = 0.3f;

            // === КОНУС ЗРЕНИЯ ===
            Vector3 origin = transform.position + Vector3.up * 1.6f; // Уровень глаз
            
            // Рисуем дугу конуса зрения
            float halfAngle = visionAngle * 0.5f;
            int segments = 20;
            Vector3 prevPoint = origin;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = -halfAngle + (visionAngle * i / segments);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                Vector3 point = origin + direction * visionRange;
                
                if (i > 0)
                {
                    // Рисуем треугольник конуса
                    Gizmos.color = visionColor;
                    Gizmos.DrawLine(origin, point);
                    Gizmos.DrawLine(prevPoint, point);
                }
                
                prevPoint = point;
            }
            
            // Линия от origin к первой точке (замыкаем конус)
            Vector3 leftBoundary = Quaternion.Euler(0, -halfAngle, 0) * transform.forward * visionRange;
            Gizmos.DrawLine(origin, origin + leftBoundary);
            
            // === ГРАНИЦЫ КОНУСА (жирные линии) ===
            Gizmos.color = hasLineOfSight ? Color.red : Color.yellow;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;
            
            Debug.DrawRay(origin, leftDir * visionRange, Gizmos.color);
            Debug.DrawRay(origin, rightDir * visionRange, Gizmos.color);
            
            // === ДАЛЬНОСТЬ ОБНАРУЖЕНИЯ (окружность) ===
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // === ДАЛЬНОСТЬ БЛИЖНЕЙ АТАКИ (окружность) ===
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
            
            // === ДАЛЬНОСТЬ ДАЛЬНЕЙ АТАКИ (окружность) ===
            Gizmos.color = new Color(1f, 0.5f, 0f); // Оранжевый
            Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
            
            // === ПОСЛЕДНЯЯ ИЗВЕСТНАЯ ПОЗИЦИЯ ЦЕЛИ ===
            if (lastSeenTargetPos != Vector3.positiveInfinity && lastSeenTargetPos != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastSeenTargetPos, 0.5f);
                Gizmos.DrawLine(transform.position + Vector3.up, lastSeenTargetPos + Vector3.up);
            }
            
            // === ЛУЧ К ЦЕЛИ (если видим) ===
            if (hasLineOfSight && target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, target.position + Vector3.up);
                
                // Индикатор на цели
                Gizmos.DrawWireSphere(target.position + Vector3.up, 0.3f);
            }
            
            // === НАПРАВЛЕНИЕ ВЗГЛЯДА ===
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, transform.forward * 2f);
            
            // === ТЕКСТ С ИНФОРМАЦИЕЙ ===
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f,
                $"{(faction != null ? faction.factionName : "No Faction")}\n" +
                $"HP: {health:F0}\n" +
                $"LOS: {(hasLineOfSight ? "✓" : "✗")}\n" +
                $"Time: {timeSinceLastSeen:F1}s\n" +
                $"Melee: {meleeAttackRange:F0}m | Range: {rangedAttackRange:F0}m\n" +
                $"Action: {(_currentAction != null ? _currentAction.name : "None")}",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = hasLineOfSight ? Color.red : Color.white },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                }
            );
            #endif
        }
    }
}
