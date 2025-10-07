using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    // Стремление исследовать: выше, когда цель не видна
    public class ExploreNeed : IConsideration
    {
        public string Name => nameof(ExploreNeed);
        private readonly float _minWhenVisible;

        public ExploreNeed(float minWhenVisible = 0.1f)
        {
            _minWhenVisible = Mathf.Clamp01(minWhenVisible);
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            float vis = bb.GetOrDefault(BlackboardKeys.TargetVisibility, 0f);
            if (vis <= 0.05f) return 1f; // цель не видна
            return _minWhenVisible; // немного желания искать
        }
    }
}
