using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Преследование: бот активно движется к игроку, сокращая дистанцию.
    /// Активируется когда игрок виден, но находится дальше желаемой дистанции атаки.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/Pursue", fileName = "Pursue")]
    public class PursueAction : UtilityAction
    {
        public override ActionChannel Channel => ActionChannel.Movement;
        [Header("Параметры преследования")]
        [Tooltip("Желаемая дистанция до цели (метры). Если игрок дальше — преследуем")]
        public float desiredRange = 4f;
        
        [Tooltip("Интервал пересчёта пути (секунды)")]
        public float repathInterval = 0.2f;
        
        private float _repathTimer;

        public override float[] GetInputs(Runtime.AI ai)
        {
            // Входы для Utility-системы:
            // [0] = цель вне желаемой дистанции (0..1, чем дальше, тем выше)
            // [1] = есть прямая видимость (1.0 если видим, 0.0 если нет)
            Vector3 targetPos;
            if (ai.AttackTarget != null)
                targetPos = ai.AttackTarget.position;
            else
                targetPos = ai.NavTargetPos;

            float dist = Vector3.Distance(ai.transform.position, targetPos);
            float outOfRange = Mathf.Clamp01((dist - desiredRange) / Mathf.Max(1f, ai.VisionRange));
            float hasLOS = ai.hasLineOfSight ? 1f : 0f;
            return new[] { outOfRange, hasLOS };
        }

        public override void Execute(Runtime.AI ai)
        {
            // Выбираем точку: при LOS двигаемся к живой цели, иначе — к навточке
            Vector3 dest;
            if (ai.AttackTarget != null && ai.hasLineOfSight)
                dest = ai.AttackTarget.position;
            else
                dest = ai.NavTargetPos;

            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                ai.MoveTo(dest);
                _repathTimer = repathInterval;
            }
            ai.LookAt(dest);
        }
    }
}
