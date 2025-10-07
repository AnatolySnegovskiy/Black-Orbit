using UnityEngine;

namespace Black_Orbit.Scripts.Health.Runtime.Configs
{
    [CreateAssetMenu(fileName = "HealthProfile", menuName = "Black Orbit/Health/Health Profile", order = 0)]
    public class HealthProfile : ScriptableObject
    {
        [Header("Base")]
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Начальное здоровье при спавне")] public float startHealth = 100f;
        [Tooltip("Можно ли лечить сверх максимума")] public bool allowOverheal = false;

        [Header("Regen")]
        [Tooltip("Секундное восстановление здоровья (0 = выключено)")] public float regenPerSecond = 0f;
        [Tooltip("Задержка перед началом регена после урона, сек")] public float regenDelay = 3f;

        [Header("Armor/Resistances")]
        [Range(0f, 1f)] public float globalDamageReduction = 0f; // 0..1 (например, 0.2 = -20% урона)

        [Header("Death")]
        public bool destroyOnDeath = false;
        public float destroyDelay = 2f;
    }
}
