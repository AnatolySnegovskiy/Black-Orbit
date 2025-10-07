using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime;
using Black_Orbit.Scripts.AI.Runtime.Cover;

namespace Black_Orbit.Scripts.AI.Runtime.Debugging
{
    /// <summary>
    /// Визуализирует выбор укрытия для указанного AI: точку кандидата, нормаль препятствия, LOS до цели.
    /// Повесьте на AI, чтобы видеть подсказки в Scene/Game View.
    /// </summary>
    [ExecuteAlways]
    public class CoverDebugGizmos : MonoBehaviour
    {
        public AI ai;
        public float searchRadius = 8f;
        public int samples = 24;
        public Color okColor = new Color(0.2f, 1f, 0.4f, 0.8f);
        public Color badColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        private void Reset()
        {
            if (ai == null) ai = GetComponent<AI>();
        }

        void OnDrawGizmos()
        {
            if (ai == null) return;
            var target = ai.AttackTarget;
            if (target == null) return;

            if (CoverService.FindBestCover(ai, target.position, searchRadius, samples, out var best))
            {
                bool blocked = Physics.Linecast(target.position + Vector3.up * 1.6f, best + Vector3.up * 1.6f, ai.ObstacleMask | ai.CoverMask);
                Gizmos.color = blocked ? okColor : badColor;
                Gizmos.DrawSphere(best, 0.2f);
                Gizmos.DrawLine(ai.transform.position, best);
                Gizmos.DrawLine(best, target.position);
            }
        }
    }
}
