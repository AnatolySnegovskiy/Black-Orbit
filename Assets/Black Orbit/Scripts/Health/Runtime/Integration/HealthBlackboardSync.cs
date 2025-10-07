using UnityEngine;
using Black_Orbit.Scripts.Health.Runtime.Components;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;

namespace Black_Orbit.Scripts.Health.Runtime.Integration
{
    [RequireComponent(typeof(HealthComponent))]
    public class HealthBlackboardSync : MonoBehaviour
    {
        private HealthComponent _health;
        private AI.Runtime.Controller.AIController _ai;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _ai = GetComponent<AI.Runtime.Controller.AIController>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.onHealthChanged.AddListener(OnHealthChanged);
                // Инициировать запись
                OnHealthChanged(_health.Current, _health.Max);
            }
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.onHealthChanged.RemoveListener(OnHealthChanged);
        }

        private void OnHealthChanged(float current, float max)
        {
            if (_ai?.Blackboard == null) return;
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            _ai.Blackboard.Set(BlackboardKeys.SelfHealth, ratio);
        }
    }
}
