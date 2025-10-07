using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Runtime.Perception
{
    /// <summary>
    /// Calculates line-of-sight and updates blackboard with perception signals.
    /// </summary>
    public class AIPerception
    {
        public void Update(AI ai, AIBlackboard bb)
        {
            bool hasLOS = false;
            if (bb.AttackTarget == null)
            {
                // No target: accumulate time since seen
                bb.TimeSinceLastSeen = Mathf.Min(float.MaxValue, bb.TimeSinceLastSeen + Time.deltaTime);
                bb.HasLineOfSight = false;
                return;
            }

            Vector3 toTarget = bb.AttackTarget.position - ai.transform.position;
            float dist = toTarget.magnitude;

            // Визуальное обнаружение опирается только на visionRange
            if (dist <= ai.VisionRange)
            {
                Vector3 dir = toTarget.normalized;
                float angle = Vector3.Angle(ai.transform.forward, dir);

                // FOV gate
                if (angle <= ai.VisionAngle * 0.5f)
                {
                    // Обзор не заблокирован препятствиями
                    if (!Physics.Raycast(ai.transform.position + Vector3.up * 1.6f, dir, out RaycastHit _, dist, ai.ObstacleMask))
                    {
                        hasLOS = true;
                    }
                }
            }

            bb.HasLineOfSight = hasLOS;
            // Target validity (active & alive)
            bool targetValid = bb.AttackTarget != null && bb.AttackTarget.gameObject.activeInHierarchy;
            if (targetValid)
            {
                var th = bb.AttackTarget.GetComponent<Black_Orbit.Scripts.Core.Runtime.Health>();
                if (th != null && th.IsDead) targetValid = false;
            }
            if (hasLOS)
            {
                bb.LastSeenTargetPos = bb.AttackTarget.position;
                bb.NavTargetPos = bb.AttackTarget.position;
                bb.TimeSinceLastSeen = 0f;
            }
            else
            {
                bb.TimeSinceLastSeen = Mathf.Min(float.MaxValue, bb.TimeSinceLastSeen + Time.deltaTime);
                // При потере LOS: держим навточку только если последняя известная позиция в радиусе удержания поиска (detectionRange)
                bool hasValidLastSeen = !float.IsPositiveInfinity(bb.LastSeenTargetPos.x);
                if (hasValidLastSeen)
                {
                    float distToLastSeen = Vector3.Distance(ai.transform.position, bb.LastSeenTargetPos);
                    if (distToLastSeen <= ai.DetectionRange)
                    {
                        bb.NavTargetPos = bb.LastSeenTargetPos;
                    }
                    else
                    {
                        // Вышли за пределы радиуса удержания — бросаем поиск
                        bb.NavTargetPos = Vector3.positiveInfinity;
                    }
                }
                else
                {
                    bb.NavTargetPos = Vector3.positiveInfinity;
                }
            }
        }
    }
}
