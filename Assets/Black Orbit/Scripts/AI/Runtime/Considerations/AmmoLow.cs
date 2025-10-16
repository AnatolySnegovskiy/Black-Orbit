using System;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    // Возвращает высокий вес, когда патронов мало
    public class AmmoLow : IConsideration
    {
        private readonly UtilityCurveSet _curves;
        private readonly int _lowThreshold;
        private readonly int _highThreshold;
        public string Name => nameof(AmmoLow);

        public AmmoLow(UtilityCurveSet curves, int lowThreshold = 5, int highThreshold = 20)
        {
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
            _lowThreshold = Mathf.Max(0, lowThreshold);
            _highThreshold = Mathf.Max(_lowThreshold + 1, highThreshold);
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            int ammo = bb.GetOrDefault(BlackboardKeys.SelfAmmo, 0);
            if (ammo <= _lowThreshold) return 1f;
            if (ammo >= _highThreshold) return 0f;
            float t = (ammo - _lowThreshold) / (float)(_highThreshold - _lowThreshold);
            float raw = 1f - Mathf.Clamp01(t);
            return _curves.Evaluate(_curves.AmmoLowCurve, raw);
        }
    }
}
