using Black_Orbit.Scripts.WeaponSystem.Base;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime
{
    /// <summary>
    /// Управляет оружием AI, интегрируясь с WeaponSystem.
    /// Автоматически инициализирует оружие и предоставляет доступ к IWeapon для AI-экшенов.
    /// </summary>
    [RequireComponent(typeof(AI))]
    public class AIWeaponHandler : MonoBehaviour
    {
        [Header("Настройки оружия")]
        [Tooltip("ScriptableObject с данными оружия")]
        public WeaponScriptableObject weaponData;
        
        [Tooltip("Точка выстрела (muzzle point). Если не задана, будет создана автоматически")]
        public Transform muzzlePoint;
        
        [Tooltip("Автоматически создать StandardWeapon при старте")]
        public bool autoInitialize = true;

        [Header("Перезарядка")]
        [Tooltip("Автоматически перезаряжать, если магазин пуст или ниже порога")] public bool autoReload = true;
        [Tooltip("Порог низкого боезапаса, при котором инициируется перезарядка")] public int reloadOnLowAmmoThreshold = 0;
        [Tooltip("Интервал проверки боезапаса (сек)")] public float reloadCheckInterval = 0.2f;
        private float _reloadCheckTimer;

        private IWeapon _weapon;
        
        /// <summary>Текущее оружие AI</summary>
        public IWeapon Weapon => _weapon;

        void Start()
        {
            if (autoInitialize && weaponData != null)
            {
                InitializeWeapon();
            }
        }

        void Update()
        {
            if (!autoReload || _weapon == null) return;
            _reloadCheckTimer -= Time.deltaTime;
            if (_reloadCheckTimer > 0f) return;
            _reloadCheckTimer = reloadCheckInterval;

            if (_weapon.IsReloading) return;

            if (_weapon is IWeaponAmmoInfo ammoInfo)
            {
                if (ammoInfo.CurrentAmmo <= reloadOnLowAmmoThreshold)
                {
                    _weapon.Reload();
                }
            }
        }

        /// <summary>
        /// Инициализирует оружие AI
        /// </summary>
        public void InitializeWeapon()
        {
            if (weaponData == null)
            {
                Debug.LogWarning($"[AIWeaponHandler] Weapon data не назначен на {gameObject.name}");
                return;
            }

            // Создаём muzzle point если не задан
            if (muzzlePoint == null)
            {
                GameObject muzzleObj = new GameObject("MuzzlePoint");
                muzzleObj.transform.SetParent(transform);
                muzzleObj.transform.localPosition = Vector3.forward * 0.5f + Vector3.up * 1.5f; // Перед AI на уровне груди
                muzzleObj.transform.localRotation = Quaternion.identity;
                muzzlePoint = muzzleObj.transform;
            }

            // Ищем существующий компонент оружия или создаём новый
            _weapon = GetComponent<IWeapon>();
            if (_weapon == null)
            {
                // Создаём StandardWeapon по умолчанию
                var weaponComponent = gameObject.AddComponent<WeaponSystem.Runtime.StandardWeapon>();
                _weapon = weaponComponent;
            }

            // Инициализируем оружие
            _weapon.Initialize(weaponData, muzzlePoint);
            Debug.Log($"[AIWeaponHandler] Оружие {weaponData.name} инициализировано на {gameObject.name}");
        }

        /// <summary>
        /// Попытка выстрелить из оружия
        /// </summary>
        public void TryFire()
        {
            if (_weapon == null) return;
            if (_weapon.IsReloading) return;

            // Если магазин пуст/низкий боезапас — перезаряжаем
            if (_weapon is IWeaponAmmoInfo ammoInfo)
            {
                if (ammoInfo.CurrentAmmo <= 0)
                {
                    if (autoReload)
                        _weapon.Reload();
                    return;
                }
            }

            _weapon.TryFire();
        }

        /// <summary>
        /// Перезарядка оружия
        /// </summary>
        public void Reload()
        {
            _weapon?.Reload();
        }

        /// <summary>
        /// Отпускает спуск (для автоматического/заряжаемого оружия)
        /// </summary>
        public void ReleaseTrigger()
        {
            _weapon?.ReleaseTrigger();
        }

        /// <summary>
        /// Проверка, перезаряжается ли оружие
        /// </summary>
        public bool IsReloading => _weapon?.IsReloading ?? false;
    }
}
