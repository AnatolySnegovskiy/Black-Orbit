using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class Visibility : IConsideration
    {
        public string Name => nameof(Visibility);
        public float Evaluate(Blackboard.Blackboard bb)
        {
            var v = bb.GetOrDefault(BlackboardKeys.TargetVisibility, 0f);
            return UtilityCurvesRegistry.Eval(UtilityCurvesRegistry.VisibilityCurve, v);
        }
    }
}
