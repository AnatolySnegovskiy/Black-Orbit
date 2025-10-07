using System;
using Black_Orbit.Scripts.WeaponSystem.Base;
using UnityEngine;

namespace Black_Orbit.Scripts.Core.Runtime
{
    /// <summary>
    /// Reusable Health component for any object (player, AI, props).
    /// Implements IDamageable so existing weapon/projectile code can damage it.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private bool invulnerable = false;
        [SerializeField] private bool destroyOnDeath = false;
        [Tooltip("Optional object to disable on death (if not destroying). Defaults to this GameObject.")]
        [SerializeField] private GameObject disableOnDeath;

        /// <summary>Raised when damage is applied. Args: amount, newHealth.</summary>
        public event Action<int, int> OnDamaged;
        /// <summary>Raised when healed. Args: amount, newHealth.</summary>
        public event Action<int, int> OnHealed;
        /// <summary>Raised once upon death.</summary>
        public event Action OnDied;
        /// <summary>Raised upon revive.</summary>
        public event Action OnRevived;

        public int MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = Mathf.Max(1, value);
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            }
        }

        public int CurrentHealth
        {
            get => currentHealth;
            private set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
        }

        public float Normalized => MaxHealth <= 0 ? 0f : (float)CurrentHealth / MaxHealth;
        public bool IsDead => CurrentHealth <= 0;
        public bool Invulnerable
        {
            get => invulnerable;
            set => invulnerable = value;
        }

        void Reset()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            if (disableOnDeath == null) disableOnDeath = gameObject;
        }

        void Awake()
        {
            if (disableOnDeath == null) disableOnDeath = gameObject;
            // Clamp on load
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        /// <summary>
        /// Apply integer damage. Negative amounts are treated as 0.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            if (Invulnerable || IsDead) return;

            int newHealth = Mathf.Max(0, CurrentHealth - amount);
            CurrentHealth = newHealth;
            OnDamaged?.Invoke(amount, CurrentHealth);

            if (IsDead)
            {
                Die();
            }
        }

        /// <summary>
        /// Heals the object by amount.
        /// </summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            if (IsDead) return; // no heal on dead by default

            int newHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            int applied = newHealth - CurrentHealth;
            CurrentHealth = newHealth;
            if (applied > 0)
                OnHealed?.Invoke(applied, CurrentHealth);
        }

        /// <summary>
        /// Directly sets current health without triggering damage/heal events.
        /// Useful for initialization and syncing values from external systems.
        /// If value <= 0, will set to 0 and trigger death flow.
        /// </summary>
        public void SetHealth(int value)
        {
            int clamped = Mathf.Clamp(value, 0, MaxHealth);
            bool wasAlive = !IsDead;
            CurrentHealth = clamped;
            if (wasAlive && IsDead)
            {
                Die();
            }
        }

        /// <summary>
        /// Instantly kills the object.
        /// </summary>
        public void Kill()
        {
            if (IsDead) return;
            CurrentHealth = 0;
            Die();
        }

        /// <summary>
        /// Revives the object to specified health (default MaxHealth).
        /// Re-enables disabled object if needed.
        /// </summary>
        public void Revive(int toHealth = -1)
        {
            int reviveTo = toHealth < 0 ? MaxHealth : Mathf.Clamp(toHealth, 1, MaxHealth);
            CurrentHealth = reviveTo;
            if (disableOnDeath != null && !disableOnDeath.activeSelf)
                disableOnDeath.SetActive(true);
            OnRevived?.Invoke();
        }

        private void Die()
        {
            // Fire event first so listeners can react before destruction/disable
            OnDied?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else if (disableOnDeath != null)
            {
                disableOnDeath.SetActive(false);
            }
        }
    }
}
