using Black_Orbit.Scripts.AI.Runtime.Blackboard;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class Visibility : IConsideration
    {
        public string Name => nameof(Visibility);
        public float Evaluate(Blackboard.Blackboard bb)
        {
            return bb.GetOrDefault(BlackboardKeys.TargetVisibility, 0f);
        }
    }
}
