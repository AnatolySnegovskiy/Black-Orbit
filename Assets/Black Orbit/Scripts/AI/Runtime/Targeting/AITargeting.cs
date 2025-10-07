using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.Faction.Runtime;

namespace Black_Orbit.Scripts.AI.Runtime.Targeting
{
    /// <summary>
    /// Handles periodic target acquisition based on faction relationships.
    /// Keeps its own timer and updates AI.Target (and thus blackboard via AI setter).
    /// </summary>
    public class AITargeting
    {
        private float _timer;

        public void Update(AI ai, AIBlackboard bb)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = Mathf.Max(0.01f, ai.TargetSearchInterval);

            bool targetInvalid = false;
            if (bb.Target == null)
            {
                targetInvalid = true;
            }
            else
            {
                if (!bb.Target.gameObject.activeInHierarchy)
                    targetInvalid = true;
                else
                {
                    var health = bb.Target.GetComponent<Black_Orbit.Scripts.Core.Runtime.Health>();
                    if (health != null && health.IsDead)
                        targetInvalid = true;
                }
                // Если цель есть, но прямой видимости нет — сбрасываем таргет (требование дизайна)
                if (!bb.HasLineOfSight)
                {
                    targetInvalid = true;
                }
            }

            if (targetInvalid)
            {
                var nearest = FindNearestHostile(ai);
                if (nearest != null)
                {
                    ai.Target = nearest; // setter синхронизирует blackboard
                }
                else
                {
                    // Целей нет — явно очищаем, чтобы боевые экшены не продолжали работать
                    ai.Target = null;
                }
            }
        }

        private Transform FindNearestHostile(AI ai)
        {
            if (ai.faction == null) return null;

            Transform nearest = null;
            float nearestDist = float.MaxValue;

            // Поиск среди AI
            var allAi = Object.FindObjectsOfType<AI>();
            foreach (var other in allAi)
            {
                if (other == null || other == ai || other.faction == null) continue;
                if (!ai.faction.IsHostile(other.faction)) continue;
                float dist = Vector3.Distance(ai.transform.position, other.transform.position);
                if (dist < nearestDist && dist <= ai.DetectionRange)
                {
                    nearestDist = dist;
                    nearest = other.transform;
                }
            }

            // Поиск среди FactionMember (игроки/NPC без AI)
            var members = Object.FindObjectsOfType<FactionMember>();
            foreach (var member in members)
            {
                if (member == null || member.transform == ai.transform) continue;
                if (member.faction == null || !ai.faction.IsHostile(member.faction)) continue;
                float dist = Vector3.Distance(ai.transform.position, member.transform.position);
                if (dist < nearestDist && dist <= ai.DetectionRange)
                {
                    nearestDist = dist;
                    nearest = member.transform;
                }
            }

            return nearest;
        }
    }
}
