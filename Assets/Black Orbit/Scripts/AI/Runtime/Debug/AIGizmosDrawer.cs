using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime;

namespace Black_Orbit.Scripts.AI.Runtime.Debugging
{
    /// <summary>
    /// Helper to draw AI gizmos in Scene view.
    /// </summary>
    public static class AIGizmosDrawer
    {
        public static void Draw(AI ai)
        {
            if (ai == null) return;

            // Local cached values
            var transform = ai.transform;
            var origin = transform.position + Vector3.up * 1.6f; // eye level
            bool hasLineOfSight = ai.hasLineOfSight;
            float visionAngle = ai.VisionAngle;
            float visionRange = ai.VisionRange;
            float detectionRange = ai.DetectionRange;
            float meleeRange = ai.MeleeAttackRange;
            float rangedRange = ai.RangedAttackRange;
            var lastSeen = ai.lastSeenTargetPos;
            var target = ai.Target;

            if (ai.showGizmosVision)
            {
                // Vision cone color & arc
                Color visionColor = hasLineOfSight ? new Color(1f, 0.2f, 0.2f, 0.15f) : new Color(1f, 1f, 0f, 0.12f);
                float halfAngle = visionAngle * 0.5f;
                int segments = 32;
                Vector3 prevPoint = Vector3.zero;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = -halfAngle + (visionAngle * i / segments);
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                    Vector3 point = origin + direction * visionRange;
                    if (i > 0)
                    {
                        Gizmos.color = visionColor;
                        Gizmos.DrawLine(origin, point);
                        Gizmos.DrawLine(prevPoint, point);
                    }
                    prevPoint = point;
                }
                Gizmos.color = hasLineOfSight ? Color.red : Color.yellow;
                Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0, -halfAngle, 0) * transform.forward) * visionRange);
                Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0,  halfAngle, 0) * transform.forward) * visionRange);
            }

            // === Ranges ===
            if (ai.showGizmosVision)
            {
                // Vision range ring
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.8f);
                Gizmos.DrawWireSphere(transform.position, visionRange);
            }

            if (ai.showGizmosDetection)
            {
                // Detection (retention) range ring
                Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
                Gizmos.DrawWireSphere(transform.position, detectionRange);
            }

            if (ai.showGizmosAttackRanges)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f); // melee
                Gizmos.DrawWireSphere(transform.position, meleeRange);

                Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f); // ranged
                Gizmos.DrawWireSphere(transform.position, rangedRange);
            }

            // === Last known target pos ===
            if (ai.showGizmosLastSeen && lastSeen != Vector3.positiveInfinity && lastSeen != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastSeen, 0.5f);
                Gizmos.DrawLine(transform.position + Vector3.up, lastSeen + Vector3.up);
            }

            // === Current navigation target (NavTargetPos) ===
            var nav = ai.NavTargetPos;
            if (ai.showGizmosNavTarget && nav != Vector3.positiveInfinity && nav != Vector3.zero)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
                Gizmos.DrawSphere(nav, 0.15f);
            }

            // === Line to target if visible ===
            if (hasLineOfSight && target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, target.position + Vector3.up);
                Gizmos.DrawWireSphere(target.position + Vector3.up, 0.3f);
            }

            // === Look direction ===
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, transform.forward * 2f);

            // === Info label with current actions ===
            #if UNITY_EDITOR
            if (ai.showGizmosInfoLabel)
            {
                string movement = ai.CurrentMovementAction ? ai.CurrentMovementAction.name : "-";
                string combat = ai.CurrentCombatAction ? ai.CurrentCombatAction.name : "-";
                var labelPos = transform.position + Vector3.up * 2.6f;

                string text =
                    $"{(ai.faction != null ? ai.faction.factionName : "No Faction")}\n" +
                    $"HP: {ai.Health:F0} | LOS: {(hasLineOfSight ? "✓" : "✗")} | LastSeen: {ai.timeSinceLastSeen:F1}s\n" +
                    $"Move: {movement}   |   Combat: {combat}";

                var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.UpperCenter
                };
                // simple backdrop
                UnityEditor.Handles.BeginGUI();
                var size = style.CalcSize(new GUIContent(text));
                var screenPos = UnityEditor.HandleUtility.WorldToGUIPoint(labelPos);
                var rect = new Rect(screenPos.x - size.x/2f - 6, screenPos.y - size.y - 6, size.x + 12, size.y + 8);
                UnityEditor.EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.6f));
                GUI.Label(new Rect(rect.x + 6, rect.y + 4, size.x, size.y), text, style);
                UnityEditor.Handles.EndGUI();
            }
            #endif
        }
    }
}
