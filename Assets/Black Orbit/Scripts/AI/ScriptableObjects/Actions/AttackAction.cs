using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Базовая атака (устаревший экшен): простая атака при близкой дистанции.
    /// Рекомендуется использовать MeleeAttackAction или RangedAttackAction вместо этого.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/Attack (Legacy)", fileName = "Attack")]
    public class AttackAction : UtilityAction
    {
        public override float[] GetInputs(Runtime.AI ai)
        {
            if (!ai.Target) return new float[] { 0f, 0f };

            // Входы для Utility-системы:
            // [0] = цель в радиусе атаки (0..1, чем ближе, тем выше)
            // [1] = уровень здоровья (0..1, чем больше здоровье, тем активнее атакует)
            float distNorm = 1f - Mathf.Clamp01(
                Vector3.Distance(ai.transform.position, ai.Target.position) / ai.attackRange
            );

            float healthNorm = ai.HealthNormalized;

            return new[] { distNorm, healthNorm };
        }

        public override void Execute(Runtime.AI ai)
        {
            Debug.Log("⚔️ Атака!");
            ai.Stop();
        }
    }
}
