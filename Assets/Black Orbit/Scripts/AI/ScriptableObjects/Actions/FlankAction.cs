using UnityEngine;
using UnityEngine.AI;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Фланговый манёвр: бот пытается обойти игрока сбоку или с тыла для тактического преимущества.
    /// Активируется когда есть прямая видимость и бот имеет достаточно здоровья для агрессивных действий.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/Flank", fileName = "Flank")]
    public class FlankAction : UtilityAction
    {
        public override ActionChannel Channel => ActionChannel.Movement;
        [Header("Параметры флангового манёвра")]
        [Tooltip("Дистанция бокового смещения от игрока (метры)")]
        public float flankDistance = 5f;
        
        [Tooltip("Интервал пересчёта пути (секунды)")]
        public float repathInterval = 0.5f;
        
        [Tooltip("Минимальный угол отклонения от прямого направления (градусы)")]
        public float minAngle = 60f;
        
        private float _repathTimer;

        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.Target == null) return new[] { 0f, 0f };
            // Входы для Utility-системы:
            // [0] = есть прямая видимость (1.0 если видим, 0.0 если нет)
            // [1] = уровень здоровья (0..1, чем больше HP, тем выше приоритет фланга)
            float los = ai.hasLineOfSight ? 1f : 0f;
            float health = ai.HealthNormalized;
            return new[] { los, health };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.Target == null) return;

            Vector3 toTarget = (ai.Target.position - ai.transform.position).normalized;
            // Выбираем левый или правый фланг случайно
            Vector3 side = (Random.value > 0.5f) ? Vector3.Cross(Vector3.up, toTarget) : Vector3.Cross(toTarget, Vector3.up);
            // Целевая точка: сбоку от цели с небольшим смещением назад
            Vector3 targetPos = ai.Target.position + side.normalized * flankDistance - toTarget * 1.0f;

            if (NavMesh.SamplePosition(targetPos, out var hit, 2f, NavMesh.AllAreas))
            {
                _repathTimer -= Time.deltaTime;
                if (_repathTimer <= 0f)
                {
                    ai.MoveTo(hit.position);
                    _repathTimer = repathInterval;
                }
                ai.LookAt(ai.Target.position);
            }
        }
    }
}
