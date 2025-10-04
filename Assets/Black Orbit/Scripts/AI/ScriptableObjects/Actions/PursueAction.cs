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
        [Header("Параметры преследования")]
        [Tooltip("Желаемая дистанция до цели (метры). Если игрок дальше — преследуем")]
        public float desiredRange = 4f;
        
        [Tooltip("Интервал пересчёта пути (секунды)")]
        public float repathInterval = 0.2f;
        
        private float _repathTimer;

        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.Target == null) return new[] { 0f, 0f };
            // Входы для Utility-системы:
            // [0] = цель вне желаемой дистанции (0..1, чем дальше, тем выше)
            // [1] = есть прямая видимость (1.0 если видим, 0.0 если нет)
            float dist = Vector3.Distance(ai.transform.position, ai.Target.position);
            float outOfRange = Mathf.Clamp01((dist - desiredRange) / (ai.VisionRange));
            float hasLOS = ai.hasLineOfSight ? 1f : 0f;
            return new[] { outOfRange, hasLOS };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.Target == null) return;
            
            // Двигаемся к цели
            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                ai.MoveTo(ai.Target.position);
                _repathTimer = repathInterval;
            }
            ai.LookAt(ai.Target.position);
        }
    }
}
