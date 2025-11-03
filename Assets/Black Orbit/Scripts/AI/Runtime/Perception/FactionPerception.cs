using System.Collections.Generic;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.Faction.Runtime;

namespace Black_Orbit.Scripts.AI.Runtime.Perception
{
    [RequireComponent(typeof(FactionMember))]
    public class FactionPerception : MonoBehaviour
    {
        [Header("Обновление")]
        [Range(0.05f, 2f)] public float tickRate = 0.2f;

        [Header("Зрение")]
        [Tooltip("Радиус зрения")] public float sightRadius = 60f;
        [Tooltip("Поле зрения (угол)")] [Range(1f, 180f)] public float fovAngle = 90f;
        [Tooltip("Слои, которые блокируют линию зрения (Raycast)")] public LayerMask losObstacles = Physics.DefaultRaycastLayers;
        [Tooltip("Слои, по которым ищем цель (0 = любые). Оставьте 0, если не нужно ограничивать")] public LayerMask detectableLayers = 0;
        [Tooltip("Порог видимости [0..1], выше которого цель считается видимой")] [Range(0f,1f)] public float visibleThreshold = 0.25f;
        [Tooltip("Время памяти цели (сек). Если цель пропала из вида, сохраняем последнюю позицию")] [Min(0f)] public float targetMemoryTime = 3f;
        [Tooltip("Трансформ глаз. Если не задан, возьмётся transform")] public Transform eyes;

        [Header("Отладка")]
        public bool debugDraw = false;

        private Controller.AIController _ai;
        private FactionMember _self;
        private float _timer;
        private readonly List<FactionMember> _cache = new();

        private Transform _lastTarget;
        private float _lastSeenTime;
        private Vector3 _lastKnownPos;

        private void Awake()
        {
            _ai = GetComponent<Controller.AIController>();
            _self = GetComponent<FactionMember>();
            if (eyes == null)
            {
                var animator = GetComponentInChildren<Animator>();
                if (animator != null && animator.isHuman)
                {
                    var head = animator.GetBoneTransform(HumanBodyBones.Head);
                    if (head != null) eyes = head;
                }
                if (eyes == null) eyes = transform;
            }
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
                ClearTargetIfMemoryExpired();
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
                if (detectableLayers.value != 0 && ((1 << t.gameObject.layer) & detectableLayers.value) == 0)
                    continue;
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
                _ai.Blackboard.Set(BlackboardKeys.TargetTransform, best);
                _ai.Blackboard.Set(BlackboardKeys.TargetPosition, best.position);
                _ai.Blackboard.Set(BlackboardKeys.TargetVisibility, bestScore);
                bool isVisible = bestScore >= visibleThreshold;
                _ai.Blackboard.Set(BlackboardKeys.TargetVisible, isVisible);

                _lastTarget = best;
                _lastKnownPos = best.position;
                if (isVisible) _lastSeenTime = Time.time;
            }
            else
            {
                // Нет видимых целей — используем память
                ClearTargetIfMemoryExpired();
            }
        }

        private void ClearTargetIfMemoryExpired()
        {
            if (_lastTarget != null)
            {
                // Память о цели в течение targetMemoryTime секунд
                float since = Time.time - _lastSeenTime;
                if (since <= targetMemoryTime)
                {
                    // держим последнюю позицию, но цель невидима
                    _ai?.Blackboard?.Set(BlackboardKeys.TargetTransform, _lastTarget);
                    _ai?.Blackboard?.Set(BlackboardKeys.TargetPosition, _lastKnownPos);
                    _ai?.Blackboard?.Set(BlackboardKeys.TargetVisibility, 0f);
                    _ai?.Blackboard?.Set(BlackboardKeys.TargetVisible, false);
                    return;
                }
            }

            // Полный сброс
            _lastTarget = null;
            _ai?.Blackboard?.Set(BlackboardKeys.TargetTransform, null);
            _ai?.Blackboard?.Set(BlackboardKeys.TargetVisibility, 0f);
            _ai?.Blackboard?.Set(BlackboardKeys.TargetVisible, false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!debugDraw && !UnityEditor.Selection.Contains(gameObject)) return;
            var eye = eyes != null ? eyes : transform;
            Vector3 pos = eye.position;
            Vector3 fwd = eye.forward;
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(pos, sightRadius);

            // Draw FOV cone (approx)
            Vector3 left = Quaternion.Euler(0f, -fovAngle * 0.5f, 0f) * fwd;
            Vector3 right = Quaternion.Euler(0f, fovAngle * 0.5f, 0f) * fwd;
            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawLine(pos, pos + left * sightRadius);
            Gizmos.DrawLine(pos, pos + right * sightRadius);
        }
#endif
    }
}
