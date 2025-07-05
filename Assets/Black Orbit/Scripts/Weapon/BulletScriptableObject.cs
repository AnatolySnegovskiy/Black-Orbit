using UnityEngine;

namespace Black_Orbit.Scripts.Weapon
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
        
        [Tooltip("Визуальный эффект шлейфа за пулей (если null - шлейфа не будет)")]
        public TrailRenderer bulletTrail;

        [Header("Explosion Settings")]
        [Tooltip("Радиус взрыва пули (если 0 - нет взрывного эффекта)")]
        public float bulletExplosionRadius;
        
        [Tooltip("Сила взрывной волны (если применимо)")]
        public float bulletExplosionForce;
    }
}
