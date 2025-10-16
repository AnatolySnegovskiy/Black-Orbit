using System;
using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class CoverAvailable : IConsideration
    {
        private readonly UtilityCurveSet _curves;
        private readonly Transform _agent;
        private readonly float _searchRadius;
        public string Name => nameof(CoverAvailable);

        public CoverAvailable(UtilityCurveSet curves, Transform agent, float searchRadius = 20f)
        {
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
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
            float raw = Mathf.Clamp01(1f - Mathf.Clamp01(best / _searchRadius));
            return _curves.Evaluate(_curves.CoverAvailableCurve, raw);
        }
    }
}
