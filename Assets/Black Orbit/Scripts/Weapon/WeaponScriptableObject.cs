using UnityEngine;

namespace Black_Orbit.Scripts.Weapon
{
    /// <summary>
    /// ScriptableObject для настройки параметров оружия
    /// Позволяет создавать разные типы оружия через меню Unity
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Black Orbit/Weapon/New Weapon")]
    public class WeaponScriptableObject : ScriptableObject
    {
        [Header("Base Settings")]
        [Tooltip("Название оружия (для отображения в UI)")]
        public string weaponName;
        
        [Tooltip("Префаб оружия (визуальная модель)")]
        public GameObject weaponPrefab;
        
        [Tooltip("Иконка оружия для UI")]
        public Sprite weaponIcon;
        
        [Tooltip("Тип оружия (автоматическое, полуавтоматическое, заряжаемое)")]
        public WeaponType weaponType;

        [Header("Firing Parameters")]
        [Tooltip("Скорострельность (выстрелов в минуту)")]
        public float fireRate;
        
        [Tooltip("Задержка перед первым выстрелом")]
        public float initialFireDelay;
        
        [Tooltip("Разброс при стрельбе (в градусах)")]
        public float spreadAngle;
        
        [Tooltip("Количество пуль за выстрел (для дробовиков)")]
        public int bulletsPerShot = 1;
        
        [Tooltip("Максимальный размер обоймы (0 если неограниченно)")]
        public int magazineSize;
        
        [Tooltip("Время перезарядки в секундах")]
        public float reloadTime;

        [Header("Ammo & Projectile")]
        [Tooltip("Тип используемых патронов/пуль")]
        public BulletScriptableObject bulletType;
        
        [Tooltip("Начальный боезапас (0 если неограниченно)")]
        public int initialAmmo;
        
        [Tooltip("Эффект дульного вспышки при выстреле")]
        public ParticleSystem muzzleFlashEffect;

        [Header("Recoil & Camera")]
        [Tooltip("Отдача: смещение камеры назад при выстреле")]
        public float recoilKickback;
        
        [Tooltip("Отдача: подброс камеры вверх при выстреле")]
        public float recoilRise;
        
        [Tooltip("Время восстановления после отдачи")]
        public float recoilRecoveryTime;

        [Header("Audio")]
        [Tooltip("Звук выстрела")]
        public AudioClip fireSound;
        
        [Tooltip("Звук перезарядки")]
        public AudioClip reloadSound;
        
        [Tooltip("Громкость звуков (0-1)")]
        [Range(0, 1)] public float soundVolume = 0.7f;

        [Header("Animation")]
        [Tooltip("Анимация стрельбы (если есть)")]
        public AnimationClip fireAnimation;
        
        [Tooltip("Анимация перезарядки (если есть)")]
        public AnimationClip reloadAnimation;
    }

    public enum WeaponType
    {
        Automatic,     // Автоматическая стрельба при зажатии
        SemiAuto,     // По одному выстрелу на нажатие
        Charged,      // Заряжаемый выстрел
        Burst         // Очередью по 2-3 выстрела
    }
}