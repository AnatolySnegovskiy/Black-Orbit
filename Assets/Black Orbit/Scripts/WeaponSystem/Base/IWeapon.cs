using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;
using UnityEngine;

namespace Black_Orbit.Scripts.WeaponSystem.Base
{
    public interface IWeapon
    {
        void Initialize(WeaponScriptableObject weaponData, Transform muzzlePoint);
        void TryFire();
        void ReleaseTrigger(); // для Charged/Automatic
        void Reload();
        bool IsReloading { get; }
    }
}
