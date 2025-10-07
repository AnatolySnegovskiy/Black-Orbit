using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class CoverAvailable : IConsideration
    {
        private readonly Transform _agent;
        private readonly float _searchRadius;
        public string Name => nameof(CoverAvailable);

        public CoverAvailable(Transform agent, float searchRadius = 20f)
        {
            _agent = agent;
            _searchRadius = Mathf.Max(1f, searchRadius);
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            var covers = bb.GetOrDefault(BlackboardKeys.EnvironmentCoverPoints, null);
            if (covers == null || covers.Count == 0) return 0f;

            float best = float.MaxValue;
            Vector3 pos = _agent.position;
            for (int i = 0; i < covers.Count; i++)
            {
                float d = Vector3.Distance(pos, covers[i]);
                if (d < best) best = d;
            }

            if (best == float.MaxValue) return 0f;
            return Mathf.Clamp01(1f - Mathf.Clamp01(best / _searchRadius));
        }
    }
}
