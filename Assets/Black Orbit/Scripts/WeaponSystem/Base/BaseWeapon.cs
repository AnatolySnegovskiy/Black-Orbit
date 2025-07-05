using Black_Orbit.Scripts.WeaponSystem.Runtime;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;

namespace Black_Orbit.Scripts.WeaponSystem.Base
{
    using UnityEngine;
    using System.Collections;
    using static UnityEngine.Quaternion;
    
    public abstract class BaseWeapon : MonoBehaviour, IWeapon
    {
        protected WeaponScriptableObject data;
        protected float lastFireTime;
        protected int currentAmmo;
        protected bool isReloading;
        
        private Transform muzzle;
        
        public bool IsReloading => isReloading;
        
        public virtual void Initialize(WeaponScriptableObject weaponData, Transform muzzlePoint)
        {
            data = weaponData;
            muzzle = muzzlePoint;
            currentAmmo = data.magazineSize > 0 ? data.magazineSize : int.MaxValue;
        }

        public abstract void TryFire();
        public abstract void ReleaseTrigger();

        public virtual void Reload()
        {
            if (isReloading || data.magazineSize <= 0) return;
            isReloading = true;
            StartCoroutine(ReloadRoutine());
        }

        protected virtual IEnumerator ReloadRoutine()
        {
            yield return new WaitForSeconds(data.reloadTime);
            currentAmmo = data.magazineSize;
            isReloading = false;
        }

        protected bool CanFire()
        {
            return Time.time - lastFireTime >= 60f / data.fireRate && !isReloading && currentAmmo > 0;
        }

        protected void ConsumeAmmo()
        {
            if (data.magazineSize > 0) currentAmmo--;
        }
        
        protected void FireBullet(float multiplier)
        {
            var bullet = BulletPoolManager.Instance.GetBullet(data.bulletType, data.magazineSize > 0 ? data.magazineSize : 10);

            // Случайный разброс
            var direction = Euler(
                Random.Range(-data.spreadAngle, data.spreadAngle),
                Random.Range(-data.spreadAngle, data.spreadAngle),
                0f
            ) * muzzle.forward;

            bullet.Launch(transform.TransformPoint(muzzle.localPosition), direction, multiplier);
        }
    }
}
