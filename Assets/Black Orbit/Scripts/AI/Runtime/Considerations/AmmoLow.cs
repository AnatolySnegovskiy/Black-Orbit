using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    // Возвращает высокий вес, когда патронов мало
    public class AmmoLow : IConsideration
    {
        private readonly int _lowThreshold;
        private readonly int _highThreshold;
        public string Name => nameof(AmmoLow);

        public AmmoLow(int lowThreshold = 5, int highThreshold = 20)
        {
            _lowThreshold = Mathf.Max(0, lowThreshold);
            _highThreshold = Mathf.Max(_lowThreshold + 1, highThreshold);
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            int ammo = bb.GetOrDefault(BlackboardKeys.SelfAmmo, 0);
            if (ammo <= _lowThreshold) return 1f;
            if (ammo >= _highThreshold) return 0f;
            float t = (ammo - _lowThreshold) / (float)(_highThreshold - _lowThreshold);
            return 1f - Mathf.Clamp01(t);
        }
    }
}
