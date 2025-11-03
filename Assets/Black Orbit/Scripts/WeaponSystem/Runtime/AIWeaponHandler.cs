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
        [Header("Настройка оружия")]
        [SerializeField] private WeaponScriptableObject weaponData;
        [Tooltip("Куда будет смонтирован префаб оружия. По умолчанию — на правую руку, если найдена.")]
        [SerializeField] private Transform weaponParent;
        [Tooltip("Трансформ правой руки модели (если пусто, попытаемся найти через Animator Humanoid")]
        [SerializeField] private Transform rightHand;
        [Tooltip("Трансформ левой руки модели (если пусто, попытаемся найти через Animator Humanoid")]
        [SerializeField] private Transform leftHand;
        [Tooltip("Автоматически совместить точки хвата рук с держателями оружия при старте")]
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

        /// <summary>
        /// Конфигурирование из внешнего редакторского кода (Builder).
        /// Любые null параметры будут авторазрешены (например, руки через Animator Humanoid).
        /// </summary>
        public void SetConfig(WeaponScriptableObject data, Transform parent = null, Transform right = null, Transform left = null, bool? alignHands = null)
        {
            weaponData = data;
            weaponParent = parent;
            rightHand = right;
            leftHand = left;
            if (alignHands.HasValue) alignHandsOnStart = alignHands.Value;

            // Автонатяжка указателей
            AutoResolveBonesIfNeeded();
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

            AutoResolveBonesIfNeeded();

            var parent = weaponParent != null ? weaponParent : (rightHand != null ? rightHand : transform);
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

        private void AutoResolveBonesIfNeeded()
        {
            // 1) Ищем специальную пустышку "WeaponHundler" как родителя оружия
            if (weaponParent == null)
            {
                var hub = FindTransformDeep(transform, "WeaponHundler");
                if (hub != null)
                {
                    weaponParent = hub;
                    // Попробуем найти точки рук внутри хаба
                    if (rightHand == null)
                        rightHand = FindTransformDeep(hub, "Right arm point");
                    if (leftHand == null)
                        leftHand = FindTransformDeep(hub, "Left arm point");
                }
            }

            // 2) Если не нашли через пустышку — пробуем искать по всему объекту
            if (rightHand == null)
                rightHand = FindTransformDeep(transform, "Right arm point");
            if (leftHand == null)
                leftHand = FindTransformDeep(transform, "Left arm point");

            // 3) Фоллбек: берём кости из Animator Humanoid
            if ((rightHand == null || leftHand == null))
            {
                var animator = GetComponentInChildren<Animator>();
                if (animator != null && animator.isHuman)
                {
                    if (rightHand == null)
                        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    if (leftHand == null)
                        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                }
            }

            // 4) Если родитель не назначен — по умолчанию правая рука либо сам объект
            if (weaponParent == null)
            {
                weaponParent = rightHand != null ? rightHand : transform;
            }
        }

        private static Transform FindTransformDeep(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var found = FindTransformDeep(child, name);
                if (found != null) return found;
            }
            return null;
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
