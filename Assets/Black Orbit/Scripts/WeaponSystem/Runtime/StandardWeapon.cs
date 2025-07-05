using System;
using System.Collections;
using Black_Orbit.Scripts.WeaponSystem.Base;
using Black_Orbit.Scripts.WeaponSystem.Enums;
using UnityEngine;

namespace Black_Orbit.Scripts.WeaponSystem.Runtime
{
    public class StandardWeapon : BaseWeapon
    {
        private bool _triggerHeld;
        private Coroutine _burstRoutine;
        private Coroutine _chargeRoutine;

        public override void TryFire()
        {
            switch (data.weaponType)
            {
                case WeaponType.Automatic:
                    _triggerHeld = true;
                    StartCoroutine(AutomaticFire());
                    break;
                case WeaponType.SemiAuto:
                    FireOnce();
                    break;
                case WeaponType.Burst:
                    if (_burstRoutine == null)
                        _burstRoutine = StartCoroutine(BurstFire());
                    break;
                case WeaponType.Charged:
                    if (_chargeRoutine == null)
                        _chargeRoutine = StartCoroutine(ChargeFire());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void ReleaseTrigger()
        {
            _triggerHeld = false;
        }

        private IEnumerator AutomaticFire()
        {
            while (_triggerHeld)
            {
                FireOnce();
                yield return new WaitForSeconds(60f / data.fireRate);
            }
        }

        private IEnumerator BurstFire()
        {
            for (int i = 0; i < data.burstCount; i++)
            {
                FireOnce();
                yield return new WaitForSeconds(data.burstDelay);
            }
            _burstRoutine = null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator ChargeFire()
        {
            float chargeTime = 0f;
            while (_triggerHeld && chargeTime < data.maxChargeTime)
            {
                chargeTime += Time.deltaTime;
                yield return null;
            }

            float multiplier = Mathf.Lerp(1f, data.maxChargeMultiplier, chargeTime / data.maxChargeTime);
            FireOnce(multiplier);
            _chargeRoutine = null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void FireOnce(float damageMultiplier = 1f)
        {
            if (!CanFire()) return;

            lastFireTime = Time.time;
            ConsumeAmmo();

            for (int i = 0; i < data.bulletsPerShot; i++)
            {
                FireBullet(damageMultiplier);
            }

            if (data.fireSound)
                AudioSource.PlayClipAtPoint(data.fireSound, transform.position, data.soundVolume);
        }
    }

}
