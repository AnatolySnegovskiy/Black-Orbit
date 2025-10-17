using UnityEngine;
using Black_Orbit.Scripts.WeaponSystem.Base;

namespace Black_Orbit.Scripts.AI.Runtime.Combat
{
    // Обертка для доступа к IWeapon на агенте
    public class AICombat : MonoBehaviour
    {
        public MonoBehaviour weaponBehaviour; // компонент, реализующий IWeapon
        private IWeapon _weapon;

        public IWeapon Weapon => _weapon;

        private void Awake()
        {
            if (weaponBehaviour != null)
                AssignWeapon(weaponBehaviour as IWeapon);
            if (_weapon == null)
                AssignWeapon(GetComponentInChildren<IWeapon>());
        }

        public void AssignWeapon(IWeapon weapon)
        {
            _weapon = weapon;
            if (weapon is MonoBehaviour behaviour)
            {
                weaponBehaviour = behaviour;
            }
        }
    }
}
