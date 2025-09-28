using Black_Orbit.Scripts.AI.Runtime;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    [CreateAssetMenu(menuName = "AI/Actions/Attack")]
    public class AttackAction : UtilityAction
    {
        public override float[] GetInputs(EnemyAI enemy)
        {
            if (!enemy.player) return new float[] { 0f, 0f };

            float distNorm = 1f - Mathf.Clamp01(
                Vector3.Distance(enemy.transform.position, enemy.player.position) / enemy.attackRange
            );

            float healthNorm = enemy.HealthNormalized; // чем больше здоровье, тем активнее атакует

            return new[] { distNorm, healthNorm };
        }

        public override void Execute(EnemyAI enemy)
        {
            Debug.Log("⚔️ Моб атакует!");
            enemy.Stop();
        }
    }
}
