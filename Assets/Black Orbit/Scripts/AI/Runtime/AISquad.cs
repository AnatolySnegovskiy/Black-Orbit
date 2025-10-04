
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
                _squadTarget = targetCounts.OrderByDescending(kvp => kvp.Value).First().Key;
                _lastKnownTargetPos = _squadTarget.position;
            }
        }

        /// <summary>
        /// Координирует действия отряда: назначает роли и приказывает действия
        /// </summary>
        void CoordinateSquad()
        {
            if (_squadTarget == null) return;

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

            // Отдаём приказы на основе ролей
            foreach (var kvp in _assignedRoles)
            {
                GiveOrderBasedOnRole(kvp.Key, kvp.Value);
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
