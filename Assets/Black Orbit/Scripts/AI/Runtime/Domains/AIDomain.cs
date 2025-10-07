using System.Collections.Generic;
using Black_Orbit.Scripts.AI.Runtime.Actions;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Runtime.Domains
{
    public class AIDomain
    {
        public DomainId Id { get; }
        public IReadOnlyList<AIAction> Actions => _actions;
        private readonly List<AIAction> _actions = new();
        private AIAction _active;

        public AIDomain(DomainId id)
        {
            Id = id;
        }

        public void AddAction(AIAction action)
        {
            if (action != null && action.Domain == Id)
                _actions.Add(action);
        }

        public AIAction EvaluateBest(Blackboard.Blackboard bb)
        {
            AIAction best = null;
            float bestScore = 0f;
            foreach (var a in _actions)
            {
                var score = a.ComputeUtility(bb);
                if (score > bestScore && a.CanStart(bb))
                {
                    bestScore = score;
                    best = a;
                }
            }
            return best;
        }

        public void TickActive(Blackboard.Blackboard bb, float dt)
        {
            _active?.Tick(bb, dt);
        }

        // Управление активностью внутри домена (одна активная в домене)
        public void Activate(Blackboard.Blackboard bb, AIAction action)
        {
            if (_active == action) return;
            _active?.Stop(bb);
            _active = action;
            _active?.Start(bb);
        }

        public void Deactivate(Blackboard.Blackboard bb)
        {
            _active?.Stop(bb);
            _active = null;
        }

        public AIAction GetActive() => _active;
    }
}
