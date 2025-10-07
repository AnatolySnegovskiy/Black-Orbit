using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using System.Collections.Generic;
using Black_Orbit.Scripts.AI.Runtime.Controller;

namespace Black_Orbit.Scripts.AI.Runtime.Squad
{
    public class SquadController : MonoBehaviour
    {
        // Разрешаем несколько сквадов на сцене (локальные группы по 4 бота и т.п.)

        [Header("Orders")]
        [SerializeField] private SquadOrder currentOrder = SquadOrder.None;
        [Tooltip("Периодическая рассылка приказа всем зарегистрированным агентам (сек)")]
        public float broadcastInterval = 1.0f;
        private float _timer;

        [Header("Members (runtime)")]
        [SerializeField] private List<AIController> _members = new();
        [Tooltip("Автоматически собирать AIController из дочерних объектов сквада")]
        public bool autoCollectFromChildren = true;

        private void Awake()
        {
            // Локальный контроллер, не singleton. Живёт в сцене/префабе отряда.
            if (autoCollectFromChildren)
                RefreshMembersFromChildren();
        }

        private void OnValidate()
        {
            if (autoCollectFromChildren)
                RefreshMembersFromChildren();
        }

        // Пример: широковещательный приказ (упрощенно)
        public void BroadcastOrder(SquadOrder order)
        {
            // В дальнейшем: хранение списка агентов, модификация их Blackboard
            // Здесь только лог
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[SquadController] Order: {order}");
#endif
            for (int i = _members.Count - 1; i >= 0; i--)
            {
                var ai = _members[i];
                if (ai == null) { _members.RemoveAt(i); continue; }
                if (ai.Blackboard == null) continue;
                ai.Blackboard.Set(BlackboardKeys.SquadOrderKey, order);
            }
        }

        public void SetOrder(SquadOrder order)
        {
            currentOrder = order;
            BroadcastOrder(currentOrder);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= broadcastInterval)
            {
                _timer = 0f;
                if (currentOrder != SquadOrder.None)
                    BroadcastOrder(currentOrder);
            }
        }

        [System.Obsolete("Ручная регистрация больше не требуется при autoCollectFromChildren. Оставлено для совместимости." )]
        public void Register(AIController ai)
        {
            if (ai == null) return;
            if (!_members.Contains(ai))
            {
                _members.Add(ai);
                if (currentOrder != SquadOrder.None && ai.Blackboard != null)
                    ai.Blackboard.Set(BlackboardKeys.SquadOrderKey, currentOrder);
            }
        }

        [System.Obsolete("Ручная регистрация больше не требуется при autoCollectFromChildren. Оставлено для совместимости." )]
        public void Unregister(AIController ai)
        {
            if (ai == null) return;
            _members.Remove(ai);
        }

        [ContextMenu("Refresh Members From Children")]
        public void RefreshMembersFromChildren()
        {
            _members.Clear();
            GetComponentsInChildren(true, _tmp);
            foreach (var ai in _tmp)
            {
                if (ai != null && ai != null && ai != (AIController)null)
                    _members.Add(ai);
            }
            _tmp.Clear();
        }

        private static readonly List<AIController> _tmp = new();

        private void OnDrawGizmosSelected()
        {
            // Визуализация центра сквада
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
