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

        // === Debug/Introspection ===
        public class ActionScore
        {
            public string name;
            public float score;
            public DomainId domain;
            public ExecutionType exec;
        }

        public class DecisionFrame
        {
            public float time;
            public Dictionary<DomainId, ActionScore> winners = new();
        }

        public Dictionary<DomainId, List<ActionScore>> LastScores { get; private set; } = new();
        public System.Action<DecisionFrame> OnDecision; // подпишется Behavior Log/Debug UI

        private readonly Queue<DecisionFrame> _behaviorLog = new();
        [SerializeField] private int behaviorLogCapacity = 64;

        private static readonly List<AIController> _registry = new();
        public static IReadOnlyList<AIController> Registry => _registry;

        private void Awake()
        {
            Blackboard = new Blackboard.Blackboard();
            EnsureDomains();
        }

        private void OnEnable()
        {
            _loop = StartCoroutine(Loop());
            if (!_registry.Contains(this)) _registry.Add(this);
        }

        private void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            foreach (var d in _domains.Values) d.Deactivate(Blackboard);
            _registry.Remove(this);
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
            LastScores.Clear();
            foreach (var kv in _domains)
            {
                var domain = kv.Value;
                // Собираем оценки всех действий домена
                var scores = new List<ActionScore>();
                foreach (var a in domain.Actions)
                {
                    var s = a.ComputeUtility(Blackboard);
                    scores.Add(new ActionScore { name = a.Name, score = s, domain = domain.Id, exec = a.Execution });
                }
                LastScores[domain.Id] = scores;

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

            // 4) Снапшот победителей для Debug/Log
            var frame = new DecisionFrame { time = Time.time };
            foreach (var kv in _domains)
            {
                var domain = kv.Value;
                var active = domain.GetActive();
                if (active == null) continue;
                frame.winners[domain.Id] = new ActionScore
                {
                    name = active.Name,
                    score = 0f, // детальный скор активного можно найти в LastScores
                    domain = domain.Id,
                    exec = active.Execution
                };
            }
            OnDecision?.Invoke(frame);
            _behaviorLog.Enqueue(frame);
            while (_behaviorLog.Count > behaviorLogCapacity) _behaviorLog.Dequeue();
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

        // Доступ к Behavior Log для Debug UI
        public IEnumerable<DecisionFrame> GetBehaviorLog() => _behaviorLog;
    }
}
