using UnityEngine;

namespace Black_Orbit.Scripts.WeaponSystem.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject для настройки всех параметров пули
    /// Позволяет создавать различные типы пуль через меню Unity
    /// </summary>
    [CreateAssetMenu(fileName = "New Bullet", menuName = "Black Orbit/Weapon/New Bullet")]
    public class BulletScriptableObject : ScriptableObject
    {
        [Header("Basic Settings")]
        [Tooltip("Префаб пули, который будет инстанциирован при выстреле")]
        public GameObject bulletPrefab;
        
        [Tooltip("Скорость движения пули (единиц в секунду)")]
        public float bulletSpeed;
        
        [Tooltip("Время жизни пули в секундах перед автоматическим уничтожением")]
        public float bulletLifeTime;
        
        [Tooltip("Урон, наносимый пулей при попадании")]
        public int bulletDamage;

        [Header("Visual Effects")]
        [Tooltip("Разброс пуль при выстреле (в градусах)")]
        public float bulletSpread;
        
        [Tooltip("Масштаб пули (размер)")]
        public float bulletSize;

        [Header("Explosion Settings")]
        [Tooltip("Радиус взрыва пули (если 0 - нет взрывного эффекта)")]
        public float bulletExplosionRadius;
        
        [Tooltip("Сила взрывной волны (если применимо)")]
        public float bulletExplosionForce;
        
        [Header("Ricochet Settings")]
        
        [Tooltip("Базовый шанс рикошета при столкновении (0–1)")]
        [Range(0f, 1f)]
        public float ricochetChance = 0.3f;
        
        [Tooltip("Коэффициент уменьшения шанса рикошета после каждого отскока (0–1)")]
        [Range(0f, 1f)]
        public float ricochetChanceFalloff = 0.5f;

        [Tooltip("Минимальный угол между пулей и поверхностью, при котором возможен рикошет (в градусах)")]
        [Range(0f, 90f)]
        public float minRicochetAngle = 20f;

        [Tooltip("Множитель урона после каждого рикошета (например, 0.7 = 70%)")]
        [Range(0f, 1f)]
        public float ricochetDamageMultiplier = 0.7f;
    }
}
