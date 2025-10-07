using Black_Orbit.Scripts.AI.Runtime.Blackboard;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class IsRetreating : IConsideration
    {
        public string Name => nameof(IsRetreating);
        public float Evaluate(Blackboard.Blackboard bb)
        {
            return bb.GetOrDefault(BlackboardKeys.SelfRetreating, false) ? 1f : 0f;
        }
    }
}
