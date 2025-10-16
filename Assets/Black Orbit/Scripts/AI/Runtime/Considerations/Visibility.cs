using System;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class Visibility : IConsideration
    {
        private readonly UtilityCurveSet _curves;
        public string Name => nameof(Visibility);

        public Visibility(UtilityCurveSet curves)
        {
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }

        public float Evaluate(Blackboard.Blackboard bb)
        {
            var v = bb.GetOrDefault(BlackboardKeys.TargetVisibility, 0f);
            return _curves.Evaluate(_curves.VisibilityCurve, v);
        }
    }
}
