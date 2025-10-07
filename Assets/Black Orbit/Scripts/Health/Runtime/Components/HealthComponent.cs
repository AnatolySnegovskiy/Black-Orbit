using System;
using UnityEngine;
using Black_Orbit.Scripts.Health.Runtime.Core;
using Black_Orbit.Scripts.Health.Runtime.Configs;
using Black_Orbit.Scripts.WeaponSystem.Base;

namespace Black_Orbit.Scripts.Health.Runtime.Components
{
    [DisallowMultipleComponent]
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [Header("Profile")]
        public HealthProfile profile;

        [Header("Runtime State")]
        [SerializeField, Min(0f)] private float currentHealth;
        [SerializeField] private bool isDead;

        [Header("Events (Unity)")]
        public UnityEngine.Events.UnityEvent<float, float> onHealthChanged; // (current, max)
        public UnityEngine.Events.UnityEvent<DamageInfo> onDamaged;
        public UnityEngine.Events.UnityEvent<float> onHealed;
        public UnityEngine.Events.UnityEvent onDeath;

        public float Current => currentHealth;
        public float Max => profile != null ? Mathf.Max(1f, profile.maxHealth) : 100f;
        public bool IsDead => isDead;

        private float _nextRegenTime;

        private void Awake()
        {
            if (profile == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[HealthComponent] {name}: Profile is null. Using defaults.");
#endif
            }
        }

        private void OnEnable()
        {
            var start = profile != null ? Mathf.Clamp(profile.startHealth, 0f, Max) : Max;
            currentHealth = start;
            isDead = false;
            onHealthChanged?.Invoke(currentHealth, Max);
        }

        private void Update()
        {
            if (isDead) return;
            if (profile == null) return;

            if (profile.regenPerSecond > 0f && Time.time >= _nextRegenTime)
            {
                float heal = profile.regenPerSecond * Time.deltaTime;
                InternalHeal(heal, invokeEvent: true);
            }
        }

        public void ApplyDamage(DamageInfo dmg)
        {
            if (isDead) return;
            float amount = Mathf.Max(0f, dmg.amount);

            if (profile != null && profile.globalDamageReduction > 0f)
            {
                amount *= (1f - Mathf.Clamp01(profile.globalDamageReduction));
            }

            if (amount <= 0f) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            _nextRegenTime = Time.time + (profile != null ? Mathf.Max(0f, profile.regenDelay) : 0f);

            onDamaged?.Invoke(dmg);
            onHealthChanged?.Invoke(currentHealth, Max);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || isDead) return;
            InternalHeal(amount, invokeEvent: true);
        }

        public void Kill()
        {
            if (isDead) return;
            currentHealth = 0f;
            onHealthChanged?.Invoke(currentHealth, Max);
            Die();
        }

        private void InternalHeal(float amount, bool invokeEvent)
        {
            if (profile != null && !profile.allowOverheal)
                currentHealth = Mathf.Min(Max, currentHealth + amount);
            else
                currentHealth = currentHealth + amount;

            if (invokeEvent) onHealed?.Invoke(amount);
            onHealthChanged?.Invoke(currentHealth, Max);
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            onDeath?.Invoke();

            if (profile != null && profile.destroyOnDeath)
            {
                Destroy(gameObject, Mathf.Max(0f, profile.destroyDelay));
            }
        }

        // Совместимость с оружейной системой (IDamageable.ApplyDamage(int))
        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || isDead) return;
            var info = new DamageInfo(amount, DamageType.Generic, transform.position, Vector3.up, this);
            ApplyDamage(info);
        }
    }
}
