using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Considerations;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Actions
{
    public abstract class AIAction
    {
        public virtual string Name => GetType().Name;
        public DomainId Domain => domainId;
        public ExecutionType Execution => executionType;
        public float BaseWeight => baseWeight;
        public bool IsActive { get; private set; }

        protected readonly DomainId domainId;
        protected readonly ExecutionType executionType;
        protected readonly float baseWeight;

        protected AIAction(DomainId domain, ExecutionType exec, float baseWeight = 1f)
        {
            this.domainId = domain;
            this.executionType = exec;
            this.baseWeight = baseWeight;
        }

        public virtual IEnumerable<IConsideration> GetConsiderations() { yield break; }

        public virtual float ComputeUtility(Blackboard.Blackboard bb)
        {
            var vals = new List<float>();
            foreach (var c in GetConsiderations())
                vals.Add(Mathf.Clamp01(c.Evaluate(bb)));
            float combined = UtilityEvaluator.CombineMultiplyCompensate(vals);
            return Mathf.Clamp01(combined * Mathf.Max(0f, BaseWeight));
        }

        public virtual bool CanStart(Blackboard.Blackboard bb) => true;
        public void Start(Blackboard.Blackboard bb)
        {
            if (IsActive) return;
            IsActive = true;
            OnStart(bb);
        }
        public void Tick(Blackboard.Blackboard bb, float dt)
        {
            if (!IsActive) return;
            OnTick(bb, dt);
        }
        public void Stop(Blackboard.Blackboard bb)
        {
            if (!IsActive) return;
            IsActive = false;
            OnStop(bb);
        }

        protected abstract void OnStart(Blackboard.Blackboard bb);
        protected abstract void OnTick(Blackboard.Blackboard bb, float dt);
        protected abstract void OnStop(Blackboard.Blackboard bb);
    }
}
