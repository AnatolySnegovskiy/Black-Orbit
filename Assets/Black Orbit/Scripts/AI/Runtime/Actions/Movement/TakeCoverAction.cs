using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using System;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Movement;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Movement
{
    public class TakeCoverAction : AIAction
    {
        private readonly Transform _agent;
        private readonly AIMovementMotor _motor;
        private readonly float _searchRadius;
        private readonly float _minDistanceToTarget;
        private readonly UtilityCurveSet _curves;
        private Vector3 _coverPoint;

        public TakeCoverAction(Transform agent, AIMovementMotor motor, UtilityCurveSet curves, float baseWeight = 0.85f, float searchRadius = 25f, float minDistanceToTarget = 6f)
            : base(DomainId.Movement, ExecutionType.Parallel, baseWeight)
        {
            _agent = agent;
            _motor = motor;
            _searchRadius = Mathf.Max(3f, searchRadius);
            _minDistanceToTarget = Mathf.Max(1f, minDistanceToTarget);
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }

        public override System.Collections.Generic.IEnumerable<IConsideration> GetConsiderations()
        {
            // Приоритет выше, если есть укрытия поблизости и цель видима (угроза)
            yield return new CoverAvailable(_curves, _agent, _searchRadius);
            yield return new Visibility(_curves);
        }

        public override bool CanStart(Blackboard.Blackboard bb) => _motor != null;

        protected override void OnStart(Blackboard.Blackboard bb)
        {
            var covers = bb.GetOrDefault(BlackboardKeys.EnvironmentCoverPoints, null);
            var target = bb.GetOrDefault(BlackboardKeys.TargetPosition, _agent.position);
            _coverPoint = FindBestCover(_agent.position, target, covers);
            bb.Set(BlackboardKeys.SelfInCover, false);
        }

        protected override void OnTick(Blackboard.Blackboard bb, float dt)
        {
            bool arrived = _motor.MoveTowards(_coverPoint, dt);
            if (arrived)
            {
                // Считаем, что в укрытии
                bb.Set(BlackboardKeys.SelfInCover, true);
            }
        }

        protected override void OnStop(Blackboard.Blackboard bb)
        {
            _motor.StopImmediate();
            bb.Set(BlackboardKeys.SelfInCover, false);
        }

        private Vector3 FindBestCover(in Vector3 selfPos, in Vector3 targetPos, List<Vector3> covers)
        {
            if (covers == null || covers.Count == 0) return selfPos;

            float bestScore = float.NegativeInfinity;
            Vector3 best = selfPos;
            Vector3 toTarget = targetPos - selfPos; toTarget.y = 0f;

            for (int i = 0; i < covers.Count; i++)
            {
                Vector3 c = covers[i];
                float distSelf = Vector3.Distance(selfPos, c);
                if (distSelf > _searchRadius) continue;

                float distToTarget = Vector3.Distance(targetPos, c);
                // Предпочитать точки, увеличивающие дистанцию до цели и близкие к нам
                float score = (distToTarget >= _minDistanceToTarget ? 1f : distToTarget / _minDistanceToTarget);
                score += 1f - Mathf.Clamp01(distSelf / _searchRadius);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }
    }
}
