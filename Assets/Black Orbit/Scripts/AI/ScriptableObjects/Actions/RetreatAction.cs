using Black_Orbit.Scripts.AI.Runtime;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    [CreateAssetMenu(menuName = "AI/Actions/Retreat")]
    public class RetreatAction : UtilityAction
    {
        public override float[] GetInputs(EnemyAI enemy)
        {
            if (enemy.player == null) return new float[] { 0f, 0f };

            float lowHealth = 1f - enemy.HealthNormalized; // меньше хп → выше приоритет
            float nearEnemy = 1f - Mathf.Clamp01(
                Vector3.Distance(enemy.transform.position, enemy.player.position) / enemy.detectionRange
            ); // ближе игрок → выше шанс отступить

            return new float[] { lowHealth, nearEnemy };
        }

        public override void Execute(EnemyAI enemy)
        {
            Debug.Log("🏃 Моб отступает!");
            if (enemy.player != null)
            {
                Vector3 dir = (enemy.transform.position - enemy.player.position).normalized;
                enemy.rb.MovePosition(enemy.rb.position + dir * enemy.moveSpeed * Time.deltaTime);
            }
        }
    }
}
