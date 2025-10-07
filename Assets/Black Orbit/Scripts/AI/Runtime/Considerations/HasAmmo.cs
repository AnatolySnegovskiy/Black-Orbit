using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class HasAmmo : IConsideration
    {
        public string Name => nameof(HasAmmo);
        public float Evaluate(Blackboard.Blackboard bb)
        {
            int ammo = bb.GetOrDefault(BlackboardKeys.SelfAmmo, 0);
            float raw = ammo > 0 ? 1f : 0f;
            return UtilityCurvesRegistry.Eval(UtilityCurvesRegistry.HasAmmoCurve, raw);
        }
    }
}
