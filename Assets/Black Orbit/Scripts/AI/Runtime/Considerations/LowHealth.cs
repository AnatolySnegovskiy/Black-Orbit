using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    // Возвращает высокий вес при низком здоровье (Self.Health 0..1)
    public class LowHealth : IConsideration
    {
        private readonly float _critical;
        private readonly float _max;
        public string Name => nameof(LowHealth);

        public LowHealth(float critical = 0.3f, float max = 0.7f)
        {
            _critical = Mathf.Clamp01(Mathf.Min(critical, max));
            _max = Mathf.Clamp01(Mathf.Max(critical, max));
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            float h = bb.GetOrDefault(BlackboardKeys.SelfHealth, 1f);
            float raw;
            if (h <= _critical) raw = 1f;
            else if (h >= _max) raw = 0f;
            else
            {
                float t = (h - _critical) / Mathf.Max(0.0001f, (_max - _critical));
                raw = 1f - Mathf.Clamp01(t);
            }
            return UtilityCurvesRegistry.Eval(UtilityCurvesRegistry.LowHealthCurve, raw);
        }
    }
}
