using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Blackboard;
using Black_Orbit.Scripts.AI.Runtime.Controller;
using Black_Orbit.Scripts.AI.Runtime.Combat;
using Black_Orbit.Scripts.WeaponSystem.Base;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;

namespace Black_Orbit.Scripts.WeaponSystem.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AIController))]
    [RequireComponent(typeof(AICombat))]
    public class AIWeaponHandler : MonoBehaviour
    {
        [Header("Weapon Setup")]
        [SerializeField] private WeaponScriptableObject weaponData;
        [SerializeField] private Transform weaponParent;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;
        [SerializeField] private bool alignHandsOnStart = true;

        private AIController _controller;
        private AICombat _combat;
        private GameObject _weaponInstance;
        private WeaponHandler _weaponHandler;
        private BaseWeapon _baseWeapon;

        private void Awake()
        {
            _controller = GetComponent<AIController>();
            _combat = GetComponent<AICombat>();
        }

        private void Start()
        {
            if (weaponData == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[AIWeaponHandler] Weapon data is not assigned.");
#endif
                UpdateBlackboardAmmo(0, 0);
                return;
            }

            SpawnWeapon();
        }

        private void OnDestroy()
        {
            if (_baseWeapon != null)
            {
                _baseWeapon.AmmoChanged -= OnAmmoChanged;
            }
        }

        private void SpawnWeapon()
        {
            if (weaponData.weaponPrefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[AIWeaponHandler] Weapon prefab is missing in weapon data.");
#endif
                UpdateBlackboardAmmo(0, 0);
                return;
            }

            var parent = weaponParent != null ? weaponParent : transform;
            _weaponInstance = Instantiate(weaponData.weaponPrefab, parent);

            _weaponHandler = _weaponInstance.GetComponent<WeaponHandler>();
            if (_weaponHandler == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[AIWeaponHandler] Weapon prefab must contain a WeaponHandler component.");
#endif
                UpdateBlackboardAmmo(0, 0);
                return;
            }

            if (alignHandsOnStart)
            {
                PositionHands();
            }

            var muzzle = _weaponHandler.MuzzleHolder;
            if (muzzle == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[AIWeaponHandler] Weapon prefab requires a muzzle holder.");
#endif
                UpdateBlackboardAmmo(0, 0);
                return;
            }

            var weapon = _weaponInstance.GetComponent<BaseWeapon>();
            if (weapon == null)
            {
                weapon = _weaponInstance.AddComponent<StandardWeapon>();
            }

            weapon.Initialize(weaponData, muzzle);
            _combat.AssignWeapon(weapon);

            _baseWeapon = weapon;
            _baseWeapon.AmmoChanged += OnAmmoChanged;
            OnAmmoChanged(_baseWeapon.CurrentAmmo, _baseWeapon.MagazineSize);
        }

        private void PositionHands()
        {
            if (_weaponHandler == null) return;

            if (rightHand != null && _weaponHandler.RightHandHolder != null)
            {
                rightHand.position = _weaponHandler.RightHandHolder.position;
                rightHand.rotation = _weaponHandler.RightHandHolder.rotation;
            }

            if (leftHand != null && _weaponHandler.LeftHandHolder != null)
            {
                leftHand.position = _weaponHandler.LeftHandHolder.position;
                leftHand.rotation = _weaponHandler.LeftHandHolder.rotation;
            }
        }

        private void OnAmmoChanged(int current, int magazine)
        {
            UpdateBlackboardAmmo(current, magazine);
        }

        private void UpdateBlackboardAmmo(int current, int magazine)
        {
            if (_controller?.Blackboard == null) return;

            int ammoValue = magazine > 0 ? Mathf.Clamp(current, 0, magazine) : Mathf.Max(current, 0);
            _controller.Blackboard.Set(BlackboardKeys.SelfAmmo, ammoValue);
        }
    }
}
