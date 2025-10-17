using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using System;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Movement;
using Black_Orbit.Scripts.AI.Runtime.Utility;
using Random = UnityEngine.Random;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Movement
{
    public class ExploreAreaAction : AIAction
    {
        private readonly Transform _agent;
        private readonly AIMovementMotor _motor;
        private readonly float _radius;
        private readonly UtilityCurveSet _curves;
        private Vector3 _target;

        public ExploreAreaAction(Transform agent, AIMovementMotor motor, UtilityCurveSet curves, float baseWeight = 0.6f, float radius = 25f)
            : base(DomainId.Movement, ExecutionType.Parallel, baseWeight)
        {
            _agent = agent;
            _motor = motor;
            _radius = Mathf.Max(5f, radius);
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
            AddConsideration(new ExploreNeed(_curves));
        }

        public override bool CanStart(Blackboard.Blackboard bb)
        {
            return _motor != null;
        }

        protected override void OnStart(Blackboard.Blackboard bb)
        {
            // Если задана точка исследования — идем к ней, иначе выбираем случайную в радиусе
            var explicitPoint = bb.GetOrDefault(BlackboardKeys.SelfExplorePoint, Vector3.positiveInfinity);
            if (explicitPoint.x.IsFinite())
            {
                _target = explicitPoint;
            }
            else
            {
                var rand = Random.insideUnitSphere; rand.y = 0f;
                _target = _agent.position + rand.normalized * _radius;
            }
        }

        protected override void OnTick(Blackboard.Blackboard bb, float dt)
        {
            bool arrived = _motor.MoveTowards(_target, dt);
            if (arrived)
            {
                // Сменим цель для продолжения исследования
                var rand = Random.insideUnitSphere; rand.y = 0f;
                _target = _agent.position + rand.normalized * _radius;
            }
        }

        protected override void OnStop(Blackboard.Blackboard bb)
        {
            _motor.StopImmediate();
        }
    }

    internal static class FloatExt
    {
        public static bool IsFinite(this float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
