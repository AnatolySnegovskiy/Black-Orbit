using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Combat;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Combat
{
    public class ThrowGrenadeAction : AIAction
    {
        private readonly Transform _agent;
        private readonly AIGrenadeThrower _thrower;
        private readonly float _minRange;
        private readonly float _maxRange;
        private readonly float _cooldown;
        private float _nextReadyTime;

        public ThrowGrenadeAction(
            Transform agent,
            AIGrenadeThrower thrower,
            float baseWeight = 0.5f,
            float minRange = 6f,
            float maxRange = 20f,
            float cooldown = 6f)
            : base(DomainId.Combat, ExecutionType.Exclusive, baseWeight)
        {
            _agent = agent;
            _thrower = thrower;
            _minRange = Mathf.Max(0.5f, Mathf.Min(minRange, maxRange));
            _maxRange = Mathf.Max(_minRange + 0.1f, maxRange);
            _cooldown = Mathf.Max(0.1f, cooldown);
        }

        public override System.Collections.Generic.IEnumerable<IConsideration> GetConsiderations()
        {
            // Видимость цели и нахождение цели в диапазоне броска
            yield return new Visibility();
            yield return new GrenadeRange(_agent, _minRange, _maxRange);
        }

        public override bool CanStart(Blackboard.Blackboard bb)
        {
            if (_thrower == null) return false;
            if (Time.time < _nextReadyTime) return false;
            return true;
        }

        protected override void OnStart(Blackboard.Blackboard bb)
        {
            var target = bb.GetOrDefault(BlackboardKeys.TargetPosition, _agent.position);
            bool ok = _thrower.ThrowAt(target);
            _nextReadyTime = Time.time + _cooldown;
            // Завершаем сразу, т.к. действие дискретное
            Stop(bb);
        }

        protected override void OnTick(Blackboard.Blackboard bb, float dt) { }

        protected override void OnStop(Blackboard.Blackboard bb) { }
    }
}
