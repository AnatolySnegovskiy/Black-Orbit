using UnityEngine;
using UnityEngine.InputSystem;
using Black_Orbit.Scripts.WeaponSystem.Base;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;

namespace Black_Orbit.Scripts.WeaponSystem.Runtime
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerWeaponHandler : MonoBehaviour
    {
        [SerializeField] private WeaponScriptableObject weaponData;

        private IWeapon _weapon;
        private InputActionMap _currentMap;

        private void Awake()
        {
            _currentMap = GetComponent<PlayerInput>().currentActionMap;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Start()
        {
            if (weaponData == null)
            {
                Debug.LogError("WeaponData не назначен");
                return;
            }

            GameObject weaponGo = Instantiate(weaponData.weaponPrefab, transform);
            var muzzle = weaponGo.GetComponentInChildren<Muzzle>();
            if (muzzle == null)
            {
                Debug.LogError("Weapon prefab должен содержать дочерний объект с компонентом Muzzle");
                return;
            }

            _weapon = weaponGo.AddComponent<StandardWeapon>();
            _weapon.Initialize(weaponData, muzzle.transform);

            _currentMap["Reload"].performed += OnReload;
            _currentMap["Fire"].performed += OnFireStart;
            _currentMap["Fire"].canceled += OnFireStop;
        }

        private void OnDestroy()
        {
            if (_currentMap == null) return;

            _currentMap["Reload"].performed -= OnReload;
            _currentMap["Fire"].performed -= OnFireStart;
            _currentMap["Fire"].canceled -= OnFireStop;
        }

        private void OnReload(InputAction.CallbackContext ctx) => _weapon.Reload();
        private void OnFireStart(InputAction.CallbackContext ctx) => _weapon.TryFire();
        private void OnFireStop(InputAction.CallbackContext ctx) => _weapon.ReleaseTrigger();
    }
}
