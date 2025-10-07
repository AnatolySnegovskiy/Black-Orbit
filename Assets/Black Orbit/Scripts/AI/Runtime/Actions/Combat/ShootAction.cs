using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Combat;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Combat
{
    public class ShootAction : AIAction
    {
        private readonly Transform _agent;
        private readonly AICombat _combat;

        public ShootAction(Transform agent, AICombat combat, float baseWeight = 1.0f)
            : base(DomainId.Combat, ExecutionType.Parallel, baseWeight)
        {
            _agent = agent;
            _combat = combat;
        }

        public override System.Collections.Generic.IEnumerable<IConsideration> GetConsiderations()
        {
            yield return new HasAmmo();
            yield return new Visibility();
        }

        public override bool CanStart(Blackboard.Blackboard bb)
        {
            return _combat != null && _combat.Weapon != null && !_combat.Weapon.IsReloading;
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
