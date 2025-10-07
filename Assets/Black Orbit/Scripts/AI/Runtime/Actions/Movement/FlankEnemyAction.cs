using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Movement;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Movement
{
    public class FlankEnemyAction : AIAction
    {
        private readonly Transform _agent;
        private readonly AIMovementMotor _motor;
        private readonly float _flankDistance;
        private readonly float _orderBoost;
        private Vector3 _goal;

        public FlankEnemyAction(Transform agent, AIMovementMotor motor, float baseWeight = 0.8f, float flankDistance = 10f, float orderBoost = 1.0f)
            : base(DomainId.Movement, ExecutionType.Parallel, baseWeight)
        {
            _agent = agent;
            _motor = motor;
            _flankDistance = Mathf.Max(3f, flankDistance);
            _orderBoost = Mathf.Max(0.1f, orderBoost);
        }

        public override System.Collections.Generic.IEnumerable<IConsideration> GetConsiderations()
        {
            // Лучше фланговать когда цель видна и мы не слишком близко
            yield return new Visibility();
            yield return new DistanceToTarget(_agent, _flankDistance * 3f, invert: false);
        }

        public override bool CanStart(Blackboard.Blackboard bb) => _motor != null;

        public override float ComputeUtility(Blackboard.Blackboard bb)
        {
            float u = base.ComputeUtility(bb);
            var order = bb.GetOrDefault(BlackboardKeys.SquadOrderKey, SquadOrder.None);
            if (order == SquadOrder.FlankLeft || order == SquadOrder.FlankRight)
                u *= _orderBoost;
            return u;
        }

        protected override void OnStart(Blackboard.Blackboard bb)
        {
            var target = bb.GetOrDefault(BlackboardKeys.TargetPosition, _agent.position);
            var order = bb.GetOrDefault(BlackboardKeys.SquadOrderKey, SquadOrder.None);

            Vector3 toTarget = (target - _agent.position);
            toTarget.y = 0f;
            Vector3 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : _agent.forward;

            // Перпендикуляр влево/вправо
            Vector3 left = new Vector3(-dir.z, 0f, dir.x);
            Vector3 right = -left;

            Vector3 side = order == SquadOrder.FlankLeft ? left :
                           order == SquadOrder.FlankRight ? right :
                           (Random.value > 0.5f ? left : right);

            _goal = target + side * _flankDistance;
        }

        protected override void OnTick(Blackboard.Blackboard bb, float dt)
        {
            _motor.MoveTowards(_goal, dt);
        }

        protected override void OnStop(Blackboard.Blackboard bb)
        {
            _motor.StopImmediate();
        }
    }
}
