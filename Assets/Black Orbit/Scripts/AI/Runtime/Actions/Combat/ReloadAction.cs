using System;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Combat;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Combat
{
    public class ReloadAction : AIAction
    {
        private readonly AICombat _combat;
        private readonly int _lowThreshold;
        private readonly int _highThreshold;
        private bool _requested;
        private readonly UtilityCurveSet _curves;

        public ReloadAction(AICombat combat, UtilityCurveSet curves, float baseWeight = 0.9f, int lowThreshold = 5, int highThreshold = 20)
            : base(DomainId.Combat, ExecutionType.Overlay, baseWeight)
        {
            _combat = combat;
            _lowThreshold = Mathf.Max(0, lowThreshold);
            _highThreshold = Mathf.Max(_lowThreshold + 1, highThreshold);
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }

        public override System.Collections.Generic.IEnumerable<IConsideration> GetConsiderations()
        {
            yield return new AmmoLow(_curves, _lowThreshold, _highThreshold);
        }

        public override bool CanStart(Blackboard.Blackboard bb)
        {
            return _combat != null && _combat.Weapon != null && !_combat.Weapon.IsReloading;
        }

        protected override void OnStart(Blackboard.Blackboard bb)
        {
            _requested = true;
            _combat?.Weapon?.Reload();
        }

        protected override void OnTick(Blackboard.Blackboard bb, float dt)
        {
            if (_combat?.Weapon == null) return;
            // Ждем завершения перезарядки и снимаем экшен
            if (_requested && _combat.Weapon.IsReloading == false)
            {
                // Завершено
                Stop(bb);
            }
        }

        protected override void OnStop(Blackboard.Blackboard bb)
        {
            _requested = false;
        }
    }
}
