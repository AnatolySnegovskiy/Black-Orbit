using System;
using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Actions.Tactics
{
    // Тактическое решение: при низком здоровье выставляет флаг отступления и выбирает точку RetreatPoint
    public class RetreatDecisionAction : AIAction
    {
        private readonly Transform _agent;
        private readonly float _retreatDistance;
        private readonly bool _preferCover;
        private readonly UtilityCurveSet _curves;
        private readonly float _criticalHealth;
        private readonly float _maxHealth;

        public RetreatDecisionAction(Transform agent, UtilityCurveSet curves, float baseWeight, float retreatDistance, bool preferCover, float criticalHealth, float maxHealth)
            : base(DomainId.Tactics, ExecutionType.Overlay, baseWeight)
        {
            _agent = agent;
            _retreatDistance = Mathf.Max(1f, retreatDistance);
            _preferCover = preferCover;
            _curves = curves ?? throw new ArgumentNullException(nameof(curves));
            _criticalHealth = Mathf.Clamp01(criticalHealth);
            _maxHealth = Mathf.Clamp01(maxHealth);
        }

        public override IEnumerable<IConsideration> GetConsiderations()
        {
            // Основной драйвер — LowHealth
            yield return new LowHealth(_curves, _criticalHealth, _maxHealth);
        }

        protected override void OnStart(Blackboard.Blackboard bb)
        {
            // Вычисляем цель отступления: вектор от цели к нам, на дистанцию _retreatDistance
            var selfPos = _agent.position;
            var targetPos = bb.GetOrDefault(BlackboardKeys.TargetPosition, selfPos - _agent.forward * 3f);
            Vector3 awayDir = (selfPos - targetPos); awayDir.y = 0f;
            if (awayDir.sqrMagnitude < 0.0001f) awayDir = -_agent.forward;
            awayDir.Normalize();
            Vector3 candidate = selfPos + awayDir * _retreatDistance;

            // Если нужно, попробуем найти ближайшее укрытие к этой точке
            if (_preferCover)
            {
                var covers = bb.GetOrDefault(BlackboardKeys.EnvironmentCoverPoints, null);
                if (covers != null && covers.Count > 0)
                {
                    candidate = PickClosest(covers, candidate);
                }
            }

            bb.Set(BlackboardKeys.SelfRetreatPoint, candidate);
            bb.Set(BlackboardKeys.SelfRetreating, true);

            // Решение принято — действие дискретное
            Stop(bb);
        }

        protected override void OnTick(Blackboard.Blackboard bb, float dt) { }
        protected override void OnStop(Blackboard.Blackboard bb) { }

        private Vector3 PickClosest(List<Vector3> points, Vector3 refPos)
        {
            float best = float.PositiveInfinity;
            Vector3 bestPt = refPos;
            foreach (var p in points)
            {
                float d = Vector3.SqrMagnitude(p - refPos);
                if (d < best) { best = d; bestPt = p; }
            }
            return bestPt;
        }
    }
}
