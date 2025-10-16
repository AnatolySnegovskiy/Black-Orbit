using System;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Considerations
{
    public class HasAmmo : IConsideration
    {
        private readonly UtilityCurveSet _curves;
        public string Name => nameof(HasAmmo);
        public HasAmmo(UtilityCurveSet curves)
        {
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }
        public float Evaluate(Blackboard.Blackboard bb)
        {
            int ammo = bb.GetOrDefault(BlackboardKeys.SelfAmmo, 0);
            float raw = ammo > 0 ? 1f : 0f;
            return _curves.Evaluate(_curves.HasAmmoCurve, raw);
        }
    }
}
