using UnityEngine;
using UnityEngine.AI;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Патрулирование: бот обходит точки патруля или блуждает случайно, если точек нет.
    /// Активируется когда игрок не виден и давно не был замечен.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/Patrol", fileName = "Patrol")]
    public class PatrolAction : UtilityAction
    {
        public override ActionChannel Channel => ActionChannel.Movement;
        [Header("Параметры патрулирования")]
        [Tooltip("Радиус достижения точки патруля (метры)")]
        public float waypointTolerance = 0.6f;
        
        [Tooltip("Радиус случайного блуждания, если точки патруля не заданы (метры)")]
        public float wanderRadius = 10f;
        
        [Tooltip("Интервал пересчёта пути (секунды)")]
        public float repathInterval = 1.0f;

        private float _repathTimer;

        public override float[] GetInputs(Runtime.AI ai)
        {
            // Входы для Utility-системы:
            // [0] = не видит цель (1.0 если нет LOS, 0.0 если есть)
            // [1] = время с последнего обнаружения (0..1, чем больше времени прошло, тем выше)
            float notSeeingTarget = ai.hasLineOfSight ? 0f : 1f;
            float timeSinceSeenNorm = Mathf.Clamp01(ai.timeSinceLastSeen / 5f);
            return new[] { notSeeingTarget, timeSinceSeenNorm };
        }

        public override void Execute(Runtime.AI ai)
        {
            _repathTimer -= Time.deltaTime;

            if (ai.PatrolPoints != null && ai.PatrolPoints.Length > 0)
            {
                // Патруль по заданным точкам
                var target = ai.PatrolPoints[ai.patrolIndex].position;
                if (_repathTimer <= 0f)
                {
                    ai.MoveTo(target);
                    _repathTimer = repathInterval;
                }
                ai.LookAt(target);
                if (Vector3.Distance(ai.transform.position, target) <= waypointTolerance)
                {
                    ai.patrolIndex = (ai.patrolIndex + 1) % ai.PatrolPoints.Length;
                }
            }
            else
            {
                // Случайное блуждание по NavMesh
                if (_repathTimer <= 0f)
                {
                    Vector3 random = ai.transform.position + Random.insideUnitSphere * wanderRadius;
                    random.y = ai.transform.position.y;
                    if (NavMesh.SamplePosition(random, out var hit, 2f, NavMesh.AllAreas))
                    {
                        ai.MoveTo(hit.position);
                    }
                    _repathTimer = repathInterval;
                }
            }
        }
    }
}
