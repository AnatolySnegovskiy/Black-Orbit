using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Domains;
using Black_Orbit.Scripts.AI.Runtime.Actions;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Controller
{
    public class AIController : MonoBehaviour
    {
        [Range(0.05f, 2f)] public float tickRate = 0.25f;

        [Header("Планирование (шедулер)")]
        [Tooltip("Если включено, внутренний цикл (корутина) не запускается. Тики будет вызывать внешний AIUpdateScheduler.")]
        public bool externalScheduler = false;

        public Blackboard.Blackboard Blackboard { get; private set; }
        public UtilityCurveSet UtilityCurves { get; private set; }

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
            UtilityCurves = new UtilityCurveSet();
            Blackboard = new Blackboard.Blackboard();
            EnsureDomains();
        }

        private void OnEnable()
        {
            if (!externalScheduler)
            {
                _loop = StartCoroutine(Loop());
            }
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

        // Публичный тик для внешнего шедулера
        public void TickStep(float dt)
        {
            Tick(dt);
        }

        private void Tick(float dt)
        {
            // 1) Оценка лучших действий по доменам
            var winners = new Dictionary<DomainId, AIAction>();
            LastScores.Clear();
            foreach (var kv in _domains)
            {
                var domain = kv.Value;
                var actions = domain.Actions;

                var scores = new List<ActionScore>(actions.Count);
                AIAction best = null;
                float bestScore = 0f;

                foreach (var action in actions)
                {
                    var score = action.ComputeUtility(Blackboard);
                    scores.Add(new ActionScore
                    {
                        name = action.Name,
                        score = score,
                        domain = domain.Id,
                        exec = action.Execution
                    });

                    if (score > bestScore && action.CanStart(Blackboard))
                    {
                        bestScore = score;
                        best = action;
                    }
                }

                scores.Sort((a, b) => b.score.CompareTo(a.score));
                LastScores[domain.Id] = scores;

                if (best != null)
                {
                    winners[domain.Id] = best;
                }
            }

            // 2) Разрешение конфликтов по ExecutionType
            bool hasExclusive = false;
            foreach (var winner in winners.Values)
            {
                if ((winner.Execution & ExecutionType.Exclusive) != 0)
                {
                    hasExclusive = true;
                    break;
                }
            }

            foreach (var kv in _domains)
            {
                var domain = kv.Value;

                if (!winners.TryGetValue(domain.Id, out var best))
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
                float activeScore = 0f;
                if (LastScores.TryGetValue(domain.Id, out var scores))
                {
                    for (int i = 0; i < scores.Count; i++)
                    {
                        if (scores[i].name == active.Name)
                        {
                            activeScore = scores[i].score;
                            break;
                        }
                    }
                }
                frame.winners[domain.Id] = new ActionScore
                {
                    name = active.Name,
                    score = activeScore,
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
