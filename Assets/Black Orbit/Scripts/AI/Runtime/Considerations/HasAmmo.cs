using Black_Orbit.Scripts.AI.Runtime.Blackboard;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class HasAmmo : IConsideration
    {
        public string Name => nameof(HasAmmo);
        public float Evaluate(Blackboard.Blackboard bb)
        {
            int ammo = bb.GetOrDefault(BlackboardKeys.SelfAmmo, 0);
            return ammo > 0 ? 1f : 0f;
        }
    }
}
