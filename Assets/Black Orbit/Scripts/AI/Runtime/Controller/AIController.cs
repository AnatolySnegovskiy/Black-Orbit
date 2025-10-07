using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Domains;
using Black_Orbit.Scripts.AI.Runtime.Actions;

namespace Black_Orbit.Scripts.AI.Runtime.Controller
{
    public class AIController : MonoBehaviour
    {
        [Range(0.05f, 2f)] public float tickRate = 0.25f;

        public Blackboard.Blackboard Blackboard { get; private set; }

        private readonly Dictionary<DomainId, AIDomain> _domains = new();

        private Coroutine _loop;

        private void Awake()
        {
            Blackboard = new Blackboard.Blackboard();
            EnsureDomains();
        }

        private void OnEnable()
        {
            _loop = StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            foreach (var d in _domains.Values) d.Deactivate(Blackboard);
        }

        private IEnumerator Loop()
        {
            var wait = new WaitForSeconds(tickRate);
            while (enabled)
            {
                Tick(tickRate);
                yield return wait;
            }
        }

        private void Tick(float dt)
        {
            // 1) Оценка лучших действий по доменам
            var winners = new List<AIAction>();
            foreach (var kv in _domains)
            {
                var domain = kv.Value;
                var best = domain.EvaluateBest(Blackboard);
                if (best != null) winners.Add(best);
            }

            // 2) Разрешение конфликтов по ExecutionType
            bool hasExclusive = winners.Exists(a => (a.Execution & ExecutionType.Exclusive) != 0);

            foreach (var kv in _domains)
            {
                var domain = kv.Value;
                var best = domain.EvaluateBest(Blackboard);
                if (best == null)
                {
                    domain.Deactivate(Blackboard);
                    continue;
                }

                if (hasExclusive && (best.Execution & ExecutionType.Exclusive) == 0)
                {
                    // Если есть Exclusive, все не-Exclusive снимаются
                    domain.Deactivate(Blackboard);
                }
                else
                {
                    domain.Activate(Blackboard, best);
                }
            }

            // 3) Тик активных действий
            foreach (var d in _domains.Values)
            {
                d.TickActive(Blackboard, dt);
            }
        }

        private void EnsureDomains()
        {
            AddDomainIfMissing(DomainId.Movement);
            AddDomainIfMissing(DomainId.Combat);
            AddDomainIfMissing(DomainId.Communication);
            AddDomainIfMissing(DomainId.Tactics);
            AddDomainIfMissing(DomainId.Squad);
        }

        public AIDomain GetDomain(DomainId id) => _domains.TryGetValue(id, out var d) ? d : null;
        public void AddDomainIfMissing(DomainId id)
        {
            if (_domains.ContainsKey(id)) return;
            _domains[id] = new AIDomain(id);
        }
    }
}
