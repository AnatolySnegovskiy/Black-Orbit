using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.Faction.Runtime;

namespace Black_Orbit.Scripts.AI.Runtime.Perception
{
    [RequireComponent(typeof(FactionMember))]
    public class FactionPerception : MonoBehaviour
    {
        [Header("Update")]
        [Range(0.05f, 2f)] public float tickRate = 0.2f;

        [Header("Vision")]
        public float sightRadius = 60f;
        [Range(1f, 180f)] public float fovAngle = 90f;
        public LayerMask losObstacles = Physics.DefaultRaycastLayers;
        public Transform eyes; // опционально: точка зрения

        private Controller.AIController _ai;
        private FactionMember _self;
        private float _timer;
        private readonly List<FactionMember> _cache = new();

        private void Awake()
        {
            _ai = GetComponent<Controller.AIController>();
            _self = GetComponent<FactionMember>();
            if (eyes == null) eyes = transform;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= tickRate)
            {
                _timer = 0f;
                Sense();
            }
        }

        private void Sense()
        {
            if (_ai == null || _ai.Blackboard == null || _self == null || _self.faction == null)
                return;

            _cache.Clear();
            var enemies = FactionManager.Instance.FindEnemiesOfFaction(_self.faction);
            if (enemies == null || enemies.Count == 0)
            {
                _ai.Blackboard.Set(BlackboardKeys.TargetVisibility, 0f);
                return;
            }

            Transform best = null;
            float bestScore = 0f;
            Vector3 eyePos = eyes.position;
            Vector3 fwd = eyes.forward;

            foreach (var fm in enemies)
            {
                if (fm == null) continue;
                Transform t = fm.transform;
                Vector3 to = t.position - eyePos; to.y = 0f;
                float dist = to.magnitude;
                if (dist > sightRadius || dist < 0.01f) continue;

                Vector3 dir = to / dist;
                float angle = Vector3.Angle(fwd, dir);
                if (angle > fovAngle * 0.5f) continue;

                // Line of sight check
                if (Physics.Raycast(eyePos, (t.position - eyePos).normalized, out var hit, sightRadius, losObstacles, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform != t && hit.transform.root != t)
                    {
                        // Закрыт препятствием
                        continue;
                    }
                }

                // Видимость как функция расстояния и угла
                float angleFactor = Mathf.Clamp01(1f - (angle / (fovAngle * 0.5f)));
                float distFactor = Mathf.Clamp01(1f - (dist / sightRadius));
                float visibility = Mathf.Clamp01(0.5f * angleFactor + 0.5f * distFactor);

                if (visibility > bestScore)
                {
                    bestScore = visibility;
                    best = t;
                }
            }

            if (best != null)
            {
                _ai.Blackboard.Set(BlackboardKeys.TargetPosition, best.position);
                _ai.Blackboard.Set(BlackboardKeys.TargetVisibility, bestScore);
            }
            else
            {
                _ai.Blackboard.Set(BlackboardKeys.TargetVisibility, 0f);
            }
        }
    }
}
