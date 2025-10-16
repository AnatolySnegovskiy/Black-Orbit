using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using System;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Movement;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Movement
{
    public class PursueTargetAction : AIAction
    {
        private readonly Transform _agent;
        private readonly AIMovementMotor _motor;
        private readonly float _maxDistance;
        private readonly UtilityCurveSet _curves;

        public PursueTargetAction(Transform agent, AIMovementMotor motor, UtilityCurveSet curves, float baseWeight = 0.9f, float maxDistance = 60f)
            : base(DomainId.Movement, ExecutionType.Parallel, baseWeight)
        {
            _agent = agent;
            _motor = motor;
            _maxDistance = Mathf.Max(5f, maxDistance);
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }

        public override System.Collections.Generic.IEnumerable<IConsideration> GetConsiderations()
        {
            // Чем дальше цель и чем лучше видимость — тем выше приоритет преследования
            yield return new DistanceToTarget(_curves, _agent, _maxDistance, invert: false);
            yield return new Visibility(_curves);
        }

        public override bool CanStart(Blackboard.Blackboard bb)
        {
            return _motor != null;
        }

        protected override void OnStart(Blackboard.Blackboard bb) { }

        protected override void OnTick(Blackboard.Blackboard bb, float dt)
        {
            var target = bb.GetOrDefault(BlackboardKeys.TargetPosition, _agent.position);
            _motor.MoveTowards(target, dt);
        }

        protected override void OnStop(Blackboard.Blackboard bb)
        {
            _motor.StopImmediate();
        }
    }
}
