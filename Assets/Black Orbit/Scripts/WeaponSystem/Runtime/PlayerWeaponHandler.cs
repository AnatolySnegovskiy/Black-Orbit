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
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;
        private IWeapon _weapon;
        private InputActionMap _currentMap;
        private WeaponHandler _weaponHandler;
        private void Awake()
        {
            _currentMap = GetComponent<PlayerInput>().currentActionMap;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Start()
        {
            if (weaponData == null)
            {
#if UNITY_EDITOR
                Debug.LogError("WeaponData не назначен");
#endif
                return;
            }

            GameObject weaponGo = Instantiate(weaponData.weaponPrefab, transform);
            _weaponHandler = weaponGo.GetComponent<WeaponHandler>();
            HandPositioning(_weaponHandler);
       
            var muzzle = _weaponHandler.MuzzleHolder;
            if (muzzle == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Weapon prefab должен содержать дочерний объект с компонентом Muzzle");
#endif
                return;
            }

            _weapon = weaponGo.AddComponent<StandardWeapon>();
            _weapon.Initialize(weaponData, muzzle);

            _currentMap["Reload"].performed += OnReload;
            _currentMap["Fire"].performed += OnFireStart;
            _currentMap["Fire"].canceled += OnFireStop;
        }

        private void HandPositioning(WeaponHandler handler)
        {
            leftHand.position = handler.LeftHandHolder.position;
            leftHand.rotation = handler.LeftHandHolder.rotation;
            rightHand.position = handler.RightHandHolder.position;
            rightHand.rotation = handler.RightHandHolder.rotation;
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
