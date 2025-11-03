using System.Collections.Generic;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Controller
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Black Orbit/AI/Controller/AI Update Scheduler")]
    public class AIUpdateScheduler : MonoBehaviour
    {
        [Header("Общие настройки")]
        [Tooltip("Остановить тики всех агентов.")]
        public bool paused = false;

        [Tooltip("Ограничение количества тиков AI за кадр.")]
        [Min(1)] public int maxAgentsPerFrame = 32;

        [Tooltip("Ограничение времени на тики в миллисекундах за кадр. 0 — без ограничения.")]
        [Min(0f)] public float timeBudgetMs = 0f;

        [Header("Распределение тиков")]
        [Tooltip("Равномерно распределять тики агентов по кадрам (round-robin). Рекомендуется включить.")]
        public bool staggerTicks = true;

        // Планы тиков по агентам (время следующего тика по Time.time)
        private readonly Dictionary<AIController, float> _nextTickTime = new();
        private int _rrIndex; // round-robin индекс

        private void OnEnable()
        {
            RebuildRegistrySnapshot();
        }

        private void Update()
        {
            if (paused) return;

            // Удаляем отсутствующих агентов
            PruneMissingAgents();

            var registry = AIController.Registry;
            if (registry == null || registry.Count == 0) return;

            int processed = 0;
            double startTicks = Time.realtimeSinceStartupAsDouble;

            // Стартовый индекс для round-robin
            int count = registry.Count;
            if (_rrIndex >= count) _rrIndex = 0;
            int startIndex = _rrIndex;

            // Проходим по всем агентам максимум один раз за кадр
            for (int step = 0; step < count; step++)
            {
                if (maxAgentsPerFrame > 0 && processed >= maxAgentsPerFrame) break;
                if (timeBudgetMs > 0f)
                {
                    double elapsedMs = (Time.realtimeSinceStartupAsDouble - startTicks) * 1000.0;
                    if (elapsedMs >= timeBudgetMs) break;
                }

                int idx = staggerTicks ? (startIndex + step) % count : step;
                var ctrl = registry[idx];
                if (ctrl == null || !ctrl.isActiveAndEnabled) continue;

                // Убеждаемся, что агент присутствует в словаре
                if (!_nextTickTime.TryGetValue(ctrl, out float next))
                {
                    _nextTickTime[ctrl] = Time.time + Mathf.Max(0.01f, ctrl.tickRate);
                    continue;
                }

                if (Time.time >= next)
                {
                    float dt = Mathf.Max(0.01f, ctrl.tickRate);
                    ctrl.TickStep(dt);
                    _nextTickTime[ctrl] = Time.time + ctrl.tickRate;
                    processed++;
                }
            }

            // Сдвигаем round-robin указатель
            if (staggerTicks && count > 0)
            {
                _rrIndex = (startIndex + 1) % count;
            }
        }

        private void RebuildRegistrySnapshot()
        {
            _nextTickTime.Clear();
            var registry = AIController.Registry;
            if (registry == null) return;
            for (int i = 0; i < registry.Count; i++)
            {
                var ctrl = registry[i];
                if (ctrl != null && ctrl.isActiveAndEnabled)
                {
                    _nextTickTime[ctrl] = Time.time + Mathf.Max(0.01f, ctrl.tickRate);
                }
            }
        }

        private void PruneMissingAgents()
        {
            // Удаляем контроллеры, которых больше нет
            var toRemove = ListPool<AIController>.Get();
            foreach (var kv in _nextTickTime)
            {
                var ctrl = kv.Key;
                if (ctrl == null || !ctrl.isActiveAndEnabled)
                    toRemove.Add(ctrl);
            }
            for (int i = 0; i < toRemove.Count; i++) _nextTickTime.Remove(toRemove[i]);
            ListPool<AIController>.Release(toRemove);
        }
    }

    // Простой пул списков, чтобы избежать аллокаций в редакторе/рантайме
    internal static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>();
        public static List<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new List<T>(16);
        }
        public static void Release(List<T> list)
        {
            list.Clear();
            Pool.Push(list);
        }
    }
}
