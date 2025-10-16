using System;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    // Возвращает высокий вес, когда цель в диапазоне [min,max]
    public class GrenadeRange : IConsideration
    {
        private readonly UtilityCurveSet _curves;
        private readonly Transform _agent;
        private readonly float _min;
        private readonly float _max;
        public string Name => nameof(GrenadeRange);

        public GrenadeRange(UtilityCurveSet curves, Transform agent, float min = 6f, float max = 20f)
        {
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
            _agent = agent;
            _min = Mathf.Max(0f, Mathf.Min(min, max));
            _max = Mathf.Max(_min + 0.1f, max);
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            var target = bb.GetOrDefault(BlackboardKeys.TargetPosition, _agent.position);
            float d = Vector3.Distance(_agent.position, target);
            if (d <= _min) return 0f;
            if (d >= _max) return 0f;
            // Треугольная функция: максимум в середине диапазона
            float mid = (_min + _max) * 0.5f;
            float t = 1f - Mathf.Abs(d - mid) / (mid - _min);
            float raw = Mathf.Clamp01(t);
            return _curves.Evaluate(_curves.GrenadeRangeCurve, raw);
        }
    }
}
