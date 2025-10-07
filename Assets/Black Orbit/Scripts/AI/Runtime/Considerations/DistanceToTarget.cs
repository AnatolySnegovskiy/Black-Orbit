using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class DistanceToTarget : IConsideration
    {
        private readonly Transform _agent;
        private readonly float _maxDistance;
        private readonly bool _invert; // если true: ближе -> выше
        public string Name => nameof(DistanceToTarget);

        public DistanceToTarget(Transform agent, float maxDistance, bool invert = false)
        {
            _agent = agent;
            _maxDistance = Mathf.Max(1f, maxDistance);
            _invert = invert;
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            var targetPos = bb.GetOrDefault(BlackboardKeys.TargetPosition, _agent.position);
            float dist = Vector3.Distance(_agent.position, targetPos);
            float t = Mathf.Clamp01(dist / _maxDistance); // 0 близко, 1 далеко
            if (_invert) t = 1f - t; // опциональная инверсия до кривой
            return UtilityCurvesRegistry.Eval(UtilityCurvesRegistry.DistanceToTargetCurve, t);
        }
    }
}
