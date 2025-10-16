using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Movement;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Movement
{
    // Движение к точке Self.RetreatPoint, пока флаг Self.Retreating включен
    public class RetreatMoveAction : AIAction
    {
        private readonly AIMovementMotor _motor;
        private readonly Transform _agent;

        public RetreatMoveAction(Transform agent, AIMovementMotor motor, float baseWeight = 1.1f)
            : base(DomainId.Movement, ExecutionType.Parallel, baseWeight)
        {
            _motor = motor; _agent = agent;
            AddConsideration(new IsRetreating());
        }

        public override bool CanStart(Blackboard.Blackboard bb) => _motor != null;

        protected override void OnStart(Blackboard.Blackboard bb) { }

        protected override void OnTick(Blackboard.Blackboard bb, float dt)
        {
            var point = bb.GetOrDefault(BlackboardKeys.SelfRetreatPoint, _agent.position);
            bool arrived = _motor.MoveTowards(point, dt);
            if (arrived)
            {
                // Прибыли в точку — снимаем флаг отступления
                bb.Set(BlackboardKeys.SelfRetreating, false);
            }
        }

        protected override void OnStop(Blackboard.Blackboard bb)
        {
            // Ничего
        }
    }
}
