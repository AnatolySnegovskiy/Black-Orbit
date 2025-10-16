using System;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    // Стремление исследовать: выше, когда цель не видна
    public class ExploreNeed : IConsideration
    {
        private readonly UtilityCurveSet _curves;
        public string Name => nameof(ExploreNeed);
        private readonly float _minWhenVisible;

        public ExploreNeed(UtilityCurveSet curves, float minWhenVisible = 0.1f)
        {
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
            _minWhenVisible = Mathf.Clamp01(minWhenVisible);
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            float vis = bb.GetOrDefault(BlackboardKeys.TargetVisibility, 0f);
            float raw = vis <= 0.05f ? 1f : Mathf.Max(_minWhenVisible, 1f - Mathf.Clamp01(vis));
            return _curves.Evaluate(_curves.ExploreNeedCurve, raw);
        }
    }
}
