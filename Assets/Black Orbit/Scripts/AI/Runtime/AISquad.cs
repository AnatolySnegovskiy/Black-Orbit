
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime
{
    /// <summary>
    /// Управляет группой AI, координируя их действия для тактического преимущества.
    /// Позволяет назначать роли, приказывать конкретные действия и синхронизировать атаки.
    /// </summary>
    public class AISquad : MonoBehaviour
    {
        [Header("Настройки отряда")]
        [Tooltip("Название отряда (для отладки)")]
        public string squadName = "Squad Alpha";
        
        [Tooltip("Члены отряда (заполняется автоматически или вручную)")]
        public List<AI> members = new List<AI>();
        
        [Tooltip("Лидер отряда (если null, выбирается автоматически)")]
        public AI leader;
        
        [Tooltip("Автоматически находить членов отряда при старте")]
        public bool autoFindMembers = true;
        
        [Tooltip("Радиус поиска членов отряда (метры)")]
        public float searchRadius = 20f;

        [Header("Координация")]
        [Tooltip("Интервал обновления координации (секунды)")]
        public float coordinationInterval = 0.5f;
        
        [Tooltip("Минимальное количество членов для координированной атаки")]
        public int minMembersForCoordination = 2;

        [Header("Логика цели отряда")]
        [Tooltip("Минимальное число голосов за одну цель, чтобы принять её как SquadTarget. Если голосов меньше — используем усреднённую последнюю позицию.")]
        public int minVotesForTarget = 2;

        [Tooltip("Если нет явной цели, двигаться к усреднённой последней позиции цели.")]
        public bool useLastKnownWhenNoTarget = true;

        [Tooltip("Длительность приказа SearchLastKnown для членов при координации без явной цели (секунды)")]
        public float searchOrderDuration = 3f;

        // Внутреннее состояние
        private float _coordinationTimer;
        private Transform _squadTarget;
        private Vector3 _lastKnownTargetPos;
        private Dictionary<AI, SquadRole> _assignedRoles = new Dictionary<AI, SquadRole>();

        /// <summary>Текущая цель отряда</summary>
        public Transform SquadTarget => _squadTarget;
        
        /// <summary>Последняя известная позиция цели</summary>
        public Vector3 LastKnownTargetPos => _lastKnownTargetPos;
        
        /// <summary>Количество живых членов отряда</summary>
        public int AliveCount => members.Count(m => m != null && m.gameObject.activeInHierarchy);

        /// <summary>
        /// Репорт подавления от одного члена отряда — распределяет подавление по остальным (эффект общей настороженности)
        /// </summary>
        public void ReportSuppression(AI source, float level)
        {
            if (members == null || members.Count == 0) return;
            float spread = Mathf.Clamp01(level) * 0.5f; // распределяем половину уровня
            foreach (var m in members)
            {
                if (m == null || !m.gameObject.activeInHierarchy || m == source) continue;
                m.AddSuppression(spread);
            }
        }

        /// <summary>
        /// Отдать комбинированный приказ: подавление позиции и фланг.
        /// suppressors и flankers — целевые количества, если людей меньше, берём по возможности.
        /// </summary>
        public void OrderSuppressAt(Vector3 pos, float duration = 3f, int suppressors = 2, int flankers = 1)
        {
            if (members == null || members.Count == 0) return;
            var alive = members.Where(m => m != null && m.gameObject.activeInHierarchy).ToList();
            if (alive.Count == 0) return;

            // Выберем подавляющих — с наилучшей «линией на цель» (минимальный угол к pos)
            var dirTo = alive.Select(m => new {
                M = m,
                Score = Vector3.Dot((pos - m.transform.position).normalized, m.transform.forward)
            }).OrderByDescending(x => x.Score).ToList();

            var chosenSuppress = dirTo.Take(Mathf.Min(suppressors, dirTo.Count)).Select(x => x.M).ToList();

            // Остальных ранжируем по латерали для фланга
            var rest = alive.Except(chosenSuppress).ToList();
            Vector3 toPosNorm(AI m) => (pos - m.transform.position).normalized;
            var flankChosen = rest
                .Select(m => new { M = m, Score = Mathf.Abs(Vector3.Dot(m.transform.right, toPosNorm(m))) })
                .OrderByDescending(x => x.Score)
                .Take(Mathf.Min(flankers, rest.Count))
                .Select(x => x.M)
                .ToList();

            // Отдаём приказы
            foreach (var s in chosenSuppress)
            {
                s.OrderAction("RangedAttack", duration);
            }
            foreach (var f in flankChosen)
            {
                f.OrderAction("Flank", duration);
            }
        }

        /// <summary>
        /// Прикажи части отряда выполнить Flank на указанную длительность (по умолчанию 2 бойца)
        /// </summary>
        public void OrderFlank(Vector3 aroundPos, float duration = 3f, int maxFlankers = 2)
        {
            if (members == null || members.Count == 0) return;
            var alive = members.Where(m => m != null && m.gameObject.activeInHierarchy).ToList();
            if (alive.Count == 0) return;

            // Выберем кандидатов по наибольшей латерали относительно направления на точку
            Vector3 toPosNorm(AI m) => (aroundPos - m.transform.position).normalized;
            var scored = alive.Select(m => new {
                M = m,
                Score = Mathf.Abs(Vector3.Dot(m.transform.right, toPosNorm(m))) // чем больше латеральная составляющая, тем лучше фланкёр
            }).OrderByDescending(x => x.Score).Take(Mathf.Max(1, maxFlankers));

            foreach (var s in scored)
            {
                s.M.OrderAction("Flank", duration);
            }
        }

        void Start()
        {
            if (autoFindMembers)
            {
                FindSquadMembers();
            }
            
            if (leader == null && members.Count > 0)
            {
                leader = members[0];
            }
            
            // Регистрируем всех членов в отряде
            foreach (var member in members)
            {
                if (member != null)
                {
                    member.Squad = this;
                }
            }
        }

        void Update()
        {
            UpdateSquadTarget();
            
            _coordinationTimer -= Time.deltaTime;
            if (_coordinationTimer <= 0f && AliveCount >= minMembersForCoordination)
            {
                CoordinateSquad();
                _coordinationTimer = coordinationInterval;
            }
        }

        /// <summary>
        /// Автоматически находит AI в радиусе и добавляет в отряд
        /// </summary>
        void FindSquadMembers()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);
            foreach (var hit in hits)
            {
                var ai = hit.GetComponent<AI>();
                if (ai != null && !members.Contains(ai))
                {
                    AddMember(ai);
                }
            }
            Debug.Log($"[AISquad] {squadName}: найдено {members.Count} членов отряда");
        }

        /// <summary>
        /// Добавляет AI в отряд
        /// </summary>
        public void AddMember(AI ai)
        {
            if (!members.Contains(ai))
            {
                members.Add(ai);
                ai.Squad = this;
                Debug.Log($"[AISquad] {ai.name} присоединился к {squadName}");
            }
        }

        /// <summary>
        /// Удаляет AI из отряда
        /// </summary>
        public void RemoveMember(AI ai)
        {
            if (members.Contains(ai))
            {
                members.Remove(ai);
                ai.Squad = null;
                _assignedRoles.Remove(ai);
                
                if (leader == ai && members.Count > 0)
                {
                    leader = members[0]; // Новый лидер
                }
            }
        }

        /// <summary>
        /// Обновляет цель отряда на основе целей членов
        /// </summary>
        void UpdateSquadTarget()
        {
            // Находим общую цель (цель большинства членов)
            var targetCounts = new Dictionary<Transform, int>();
            foreach (var member in members)
            {
                if (member != null && member.Target != null)
                {
                    if (!targetCounts.ContainsKey(member.Target))
                        targetCounts[member.Target] = 0;
                    targetCounts[member.Target]++;
                }
            }

            if (targetCounts.Count > 0)
            {
                var top = targetCounts.OrderByDescending(kvp => kvp.Value).First();
                if (top.Value >= Mathf.Max(1, minVotesForTarget))
                {
                    _squadTarget = top.Key;
                    _lastKnownTargetPos = _squadTarget.position;
                }
                else
                {
                    _squadTarget = null;
                }
            }
            else
            {
                // Нет явной цели: агрегируем последнюю известную позицию по членам
                Vector3 sum = Vector3.zero;
                int count = 0;
                foreach (var member in members)
                {
                    if (member == null) continue;
                    var lp = member.lastSeenTargetPos;
                    if (!float.IsPositiveInfinity(lp.x) && lp != Vector3.zero)
                    {
                        sum += lp;
                        count++;
                    }
                }
                _squadTarget = null;
                _lastKnownTargetPos = count > 0 ? sum / count : Vector3.positiveInfinity;
            }
        }

        /// <summary>
        /// Координирует действия отряда: назначает роли и приказывает действия
        /// </summary>
        void CoordinateSquad()
        {
            // Если явной цели нет, но есть последняя известная позиция — координируем поиск
            if (_squadTarget == null)
            {
                if (!useLastKnownWhenNoTarget || _lastKnownTargetPos == Vector3.positiveInfinity) return;
                // Задаём всем навигационную цель и приказываем поиск последней позиции
                foreach (var m in members)
                {
                    if (m == null || !m.gameObject.activeInHierarchy) continue;
                    m.NavTargetPos = _lastKnownTargetPos;
                    m.OrderAction("SearchLastKnown", searchOrderDuration > 0f ? searchOrderDuration : coordinationInterval);
                }
                return;
            }

            // Очищаем старые роли
            _assignedRoles.Clear();

            var alivemembers = members.Where(m => m != null && m.gameObject.activeInHierarchy).ToList();
            if (alivemembers.Count < minMembersForCoordination) return;
            
            // Проверяем, что цель враждебна для отряда
            if (alivemembers.Count > 0 && alivemembers[0] != null)
            {
                var targetAI = _squadTarget.GetComponent<AI>();
                if (targetAI != null)
                {
                    float relationship = alivemembers[0].GetRelationship(targetAI);
                    
                    // Если цель не враждебна (>= -0.3), не координируем атаку
                    if (relationship >= -0.3f)
                    {
                        return;
                    }
                }
            }

            // Сортируем по дистанции до цели
            var sorted = alivemembers.OrderBy(m => Vector3.Distance(m.transform.position, _squadTarget.position)).ToList();

            // Назначаем роли
            if (sorted.Count >= 3)
            {
                // 3+ членов: фланкёры, подавление, штурм
                AssignRole(sorted[0], SquadRole.Rusher);      // Ближайший штурмует
                AssignRole(sorted[1], SquadRole.Flanker);     // Средний фланкует
                AssignRole(sorted[2], SquadRole.Suppressor);  // Дальний подавляет
                
                for (int i = 3; i < sorted.Count; i++)
                {
                    AssignRole(sorted[i], i % 2 == 0 ? SquadRole.Flanker : SquadRole.Suppressor);
                }
            }
            else if (sorted.Count == 2)
            {
                // 2 члена: один штурмует, другой прикрывает
                AssignRole(sorted[0], SquadRole.Rusher);
                AssignRole(sorted[1], SquadRole.Suppressor);
            }

            // Если отряд под сильным подавлением — усиливаем долю фланкёров
            float avgSuppression = 0f;
            if (sorted.Count > 0)
            {
                avgSuppression = sorted.Average(m => m.SuppressionLevel);
            }
            if (avgSuppression >= 0.5f)
            {
                // Выберем до 2 бойцов не-фланкёров и сделаем их фланкёрами
                int converted = 0;
                foreach (var m in sorted)
                {
                    if (converted >= 2) break;
                    if (!_assignedRoles.TryGetValue(m, out var role) || role == SquadRole.Flanker) continue;
                    _assignedRoles[m] = SquadRole.Flanker;
                    converted++;
                }
            }

            // Отдаём приказы на основе ролей
            foreach (var kvp in _assignedRoles)
            {
                GiveOrderBasedOnRole(kvp.Key, kvp.Value);
            }

            // Дополнительно: при очень высоком подавлении делаем комбинированный приказ «подавление+фланг» вокруг цели
            if (_squadTarget != null)
            {
                // Используем уже собранный список alivemembers и/или рассчитанный выше avgSuppression
                float avgSuppressionFinal = 0f;
                if (alivemembers != null && alivemembers.Count > 0)
                    avgSuppressionFinal = alivemembers.Average(m => m.SuppressionLevel);
                if (avgSuppressionFinal >= 0.75f)
                {
                    OrderSuppressAt(_squadTarget.position, coordinationInterval * 2f, suppressors: 2, flankers: 1);
                }
            }
        }

        /// <summary>
        /// Назначает роль члену отряда
        /// </summary>
        void AssignRole(AI member, SquadRole role)
        {
            _assignedRoles[member] = role;
            Debug.Log($"[AISquad] {member.name} назначен роль: {role}");
        }

        /// <summary>
        /// Отдаёт приказ члену отряда на основе его роли
        /// </summary>
        void GiveOrderBasedOnRole(AI member, SquadRole role)
        {
            switch (role)
            {
                case SquadRole.Rusher:
                    // Агрессивное преследование
                    member.OrderAction("Pursue");
                    break;
                    
                case SquadRole.Flanker:
                    // Фланговый манёвр
                    member.OrderAction("Flank");
                    break;
                    
                case SquadRole.Suppressor:
                    // Дальняя атака с дистанции
                    member.OrderAction("RangedAttack");
                    break;
                    
                case SquadRole.Defender:
                    // Защита позиции
                    member.OrderAction("TakeCover");
                    break;
            }
        }

        /// <summary>
        /// Приказывает всему отряду выполнить конкретное действие
        /// </summary>
        public void OrderSquad(string actionName, float duration = 3f)
        {
            foreach (var member in members)
            {
                if (member != null && member.gameObject.activeInHierarchy)
                {
                    member.OrderAction(actionName, duration);
                }
            }
            Debug.Log($"[AISquad] {squadName} получил приказ: {actionName}");
        }

        /// <summary>
        /// Получает роль конкретного члена отряда
        /// </summary>
        public SquadRole GetRole(AI member)
        {
            return _assignedRoles.ContainsKey(member) ? _assignedRoles[member] : SquadRole.None;
        }

        /// <summary>
        /// Проверяет, может ли член отряда видеть цель
        /// </summary>
        public bool CanAnyoneSeeTarget()
        {
            return members.Any(m => m != null && m.hasLineOfSight);
        }

        /// <summary>
        /// Получает всех членов отряда с определённой ролью
        /// </summary>
        public List<AI> GetMembersByRole(SquadRole role)
        {
            return _assignedRoles.Where(kvp => kvp.Value == role).Select(kvp => kvp.Key).ToList();
        }

        void OnDrawGizmosSelected()
        {
            if (members == null || members.Count == 0) return;

            // Рисуем связи между членами отряда
            Gizmos.color = Color.cyan;
            foreach (var member in members)
            {
                if (member != null)
                {
                    if (leader != null)
                    {
                        Gizmos.DrawLine(leader.transform.position, member.transform.position);
                    }
                }
            }

            // Рисуем радиус отряда
            if (leader != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(leader.transform.position, searchRadius);
            }

            // Рисуем последнюю известную позицию цели отряда
            if (_lastKnownTargetPos != Vector3.positiveInfinity && _lastKnownTargetPos != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(_lastKnownTargetPos, 0.2f);
            }
        }
    }

    /// <summary>
    /// Роли членов отряда для координации
    /// </summary>
    public enum SquadRole
    {
        None,        // Нет роли
        Rusher,      // Штурмовик (агрессивно атакует)
        Flanker,     // Фланкёр (обходит сбоку)
        Suppressor,  // Подавление (стреляет с дистанции)
        Defender     // Защитник (держит позицию)
    }
}
