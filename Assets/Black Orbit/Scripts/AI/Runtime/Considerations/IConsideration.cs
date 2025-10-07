using Black_Orbit.Scripts.AI.Runtime.Blackboard;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public interface IConsideration
    {
        string Name { get; }
        float Evaluate(Blackboard.Blackboard bb);
    }
}
