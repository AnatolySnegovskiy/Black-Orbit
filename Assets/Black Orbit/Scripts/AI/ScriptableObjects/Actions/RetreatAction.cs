using Black_Orbit.Scripts.AI.Runtime;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Отступление: бот убегает от игрока, когда здоровье низкое и враг близко.
    /// Простое отступление без поиска укрытия (для укрытия используйте TakeCoverAction).
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/Retreat", fileName = "Retreat")]
    public class RetreatAction : UtilityAction
    {
        public override ActionChannel Channel => ActionChannel.Movement;
        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.Target == null) return new float[] { 0f, 0f };

            // Входы для Utility-системы:
            // [0] = низкое здоровье (0..1, чем меньше HP, тем выше приоритет)
            // [1] = цель близко (0..1, чем ближе цель, тем выше шанс отступить)
            float lowHealth = 1f - ai.HealthNormalized;
            float nearTarget = 1f - Mathf.Clamp01(
                Vector3.Distance(ai.transform.position, ai.Target.position) / ai.DetectionRange
            );

            return new float[] { lowHealth, nearTarget };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.Target == null) return;
            
            // Убегаем в противоположную от цели сторону
            Vector3 dir = (ai.transform.position - ai.Target.position).normalized;
            Vector3 retreatPos = ai.transform.position + dir * 3f;
            ai.MoveTo(retreatPos);
            
            Debug.Log("🏃 Отступление!");
        }
    }
}
