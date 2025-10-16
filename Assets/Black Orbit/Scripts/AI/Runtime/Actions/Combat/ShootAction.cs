using System;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Combat;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Combat
{
    public class ShootAction : AIAction
    {
        private readonly Transform _agent;
        private readonly AICombat _combat;
        private readonly float _retreatPenalty;
        private readonly float _suppressBoost;
        private readonly UtilityCurveSet _curves;

        public ShootAction(Transform agent, AICombat combat, UtilityCurveSet curves, float baseWeight = 1.0f, float retreatPenalty = 0.5f, float suppressBoost = 1.0f)
            : base(DomainId.Combat, ExecutionType.Parallel, baseWeight)
        {
            _agent = agent;
            _combat = combat;
            _retreatPenalty = Mathf.Clamp01(retreatPenalty);
            _suppressBoost = Mathf.Max(0.1f, suppressBoost);
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }

        public override System.Collections.Generic.IEnumerable<IConsideration> GetConsiderations()
        {
            yield return new HasAmmo(_curves);
            yield return new Visibility(_curves);
        }

        public override bool CanStart(Blackboard.Blackboard bb)
        {
            return _combat != null && _combat.Weapon != null && !_combat.Weapon.IsReloading;
        }

        public override float ComputeUtility(Blackboard.Blackboard bb)
        {
            float u = base.ComputeUtility(bb);
            if (bb.GetOrDefault(BlackboardKeys.SelfRetreating, false))
            {
                u *= _retreatPenalty;
            }
            var order = bb.GetOrDefault(BlackboardKeys.SquadOrderKey, SquadOrder.None);
            if (order == SquadOrder.Suppress)
            {
                u *= _suppressBoost;
            }
            return u;
        }

        protected override void OnStart(Blackboard.Blackboard bb)
        {
            // Ничего
        }

        protected override void OnTick(Blackboard.Blackboard bb, float dt)
        {
            if (_combat?.Weapon == null) return;
            _combat.Weapon.TryFire();
        }

        protected override void OnStop(Blackboard.Blackboard bb)
        {
            _combat?.Weapon?.ReleaseTrigger();
        }
    }
}
