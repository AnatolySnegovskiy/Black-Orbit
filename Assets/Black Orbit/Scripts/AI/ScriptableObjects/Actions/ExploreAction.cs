using UnityEngine;
using UnityEngine.AI;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Исследование окружения: бот случайно перемещается по уровню, изучая территорию.
    /// Активируется когда нет контакта с игроком или игрок давно не был замечен.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/Explore", fileName = "Explore")]
    public class ExploreAction : UtilityAction
    {
        [Header("Параметры исследования")]
        [Tooltip("Радиус случайного перемещения от текущей позиции (метры)")]
        public float radius = 15f;
        
        [Tooltip("Интервал выбора новой точки для исследования (секунды)")]
        public float repathInterval = 1.2f;
        
        private float _repathTimer;

        public override float[] GetInputs(Runtime.AI ai)
        {
            // Входы для Utility-системы:
            // [0] = бот в режиме ожидания (1.0 если цели нет или давно не видели, 0.0 если есть контакт)
            // [1] = уровень здоровья (0..1)
            float idle = (ai.Target == null || (!ai.hasLineOfSight && ai.timeSinceLastSeen > 4f)) ? 1f : 0f;
            float health = ai.HealthNormalized;
            return new[] { idle, health };
        }

        public override void Execute(Runtime.AI ai)
        {
            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                // Выбираем случайную точку в радиусе
                Vector3 random = ai.transform.position + Random.insideUnitSphere * radius;
                random.y = ai.transform.position.y;
                if (NavMesh.SamplePosition(random, out var hit, 3f, NavMesh.AllAreas))
                {
                    ai.MoveTo(hit.position);
                }
                _repathTimer = repathInterval;
            }
        }
    }
}
