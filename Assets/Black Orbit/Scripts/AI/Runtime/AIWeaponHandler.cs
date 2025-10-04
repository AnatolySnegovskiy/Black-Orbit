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
            if (_weapon != null && !_weapon.IsReloading)
            {
                _weapon.TryFire();
            }
        }

        /// <summary>
        /// Перезарядка оружия
        /// </summary>
        public void Reload()
        {
            _weapon?.Reload();
        }

        /// <summary>
        /// Проверка, перезаряжается ли оружие
        /// </summary>
        public bool IsReloading => _weapon?.IsReloading ?? false;
    }
}
