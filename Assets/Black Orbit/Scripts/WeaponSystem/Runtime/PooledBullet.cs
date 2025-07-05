using Black_Orbit.Scripts.WeaponSystem.Base;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;
using UnityEngine;

namespace Black_Orbit.Scripts.WeaponSystem.Runtime
{
    [RequireComponent(typeof(Rigidbody))]
    public class PooledBullet : MonoBehaviour
    {
        private Rigidbody _rb;
        private BulletScriptableObject _data;
        private float _multiplier = 1f;
        private BulletPoolManager _pool = BulletPoolManager.Instance;
        private static readonly Collider[] Overlap = new Collider[32];
        internal BulletScriptableObject BulletData { set => _data = value; }

        private Vector3 _localScale;
        private int _ricochetCount;

        private void Awake()
        {
            _rb = transform.GetComponent<Rigidbody>();
            _localScale = transform.localScale;
        }

        public void Launch(Vector3 position, Vector3 direction, float multiplier)
        {
            _ricochetCount = 0; // Сброс при запуске
            _multiplier = multiplier;
            transform.position = position;

            // ✅ Применяем bulletSpread
            direction = Quaternion.Euler(
                Random.Range(-_data.bulletSpread, _data.bulletSpread),
                Random.Range(-_data.bulletSpread, _data.bulletSpread),
                0f
            ) * direction;

            transform.rotation = Quaternion.LookRotation(direction);
            transform.localScale = _localScale * _data.bulletSize;
            _rb.linearVelocity = direction * _data.bulletSpeed;

            CancelInvoke();
            Invoke(nameof(ReturnToPool), _data.bulletLifeTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            var contact = collision.contacts[0];

            ApplyDirectHitDamage(collision.collider);
            ApplyExplosionDamage();

            if (!TryRicochetOrReturn(contact.normal))
            {
                ReturnToPool();
            }
        }
        
        private void ApplyDirectHitDamage(Collider collisionCollider)
        {
            var hit = collisionCollider.GetComponent<IDamageable>();
            if (hit != null)
                hit.ApplyDamage((int)(_data.bulletDamage * _multiplier));
        }
        
        private void ApplyExplosionDamage()
        {
            if (_data.bulletExplosionRadius <= 0) return;

            int count = Physics.OverlapSphereNonAlloc(transform.position, _data.bulletExplosionRadius, Overlap);
            for (int i = 0; i < count; i++)
            {
                var c = Overlap[i];
                var target = c.GetComponent<IDamageable>();
                if (target != null)
                    target.ApplyDamage((int)(_data.bulletDamage * _multiplier));

                if (c.attachedRigidbody != null)
                    c.attachedRigidbody.AddExplosionForce(_data.bulletExplosionForce, transform.position, _data.bulletExplosionRadius);
            }
        }

        private bool TryRicochetOrReturn(Vector3 hitNormal)
        {
            if (_ricochetCount >= _data.maxRicochets)
                return false;

            float angle = Vector3.Angle(-_rb.linearVelocity.normalized, hitNormal);
            bool isRicochetAngleValid = angle >= _data.minRicochetAngle;
            bool chancePassed = Random.value <= _data.ricochetChance;

            if (isRicochetAngleValid && chancePassed)
            {
                _ricochetCount++;
                _multiplier *= _data.ricochetDamageMultiplier;

                Vector3 reflectDir = GetRicochetDirection(_rb.linearVelocity.normalized, hitNormal);
                _rb.linearVelocity = reflectDir * _data.bulletSpeed / 2f;
                transform.rotation = Quaternion.LookRotation(reflectDir);
                return true;
            }

            return false;
        }
        
        private Vector3 GetRicochetDirection(Vector3 incoming, Vector3 normal, float angleRandomness = 10f)
        {
            // Базовое отражение
            Vector3 reflect = Vector3.Reflect(incoming, normal);

            // Добавим случайный поворот вокруг нормали (для разнообразия)
            Quaternion deviation = Quaternion.AngleAxis(Random.Range(-angleRandomness, angleRandomness), normal);
            reflect = deviation * reflect;

            // Ограничим вертикальность
            reflect.y = Mathf.Clamp(reflect.y, -0.2f, 0.5f);
            reflect.Normalize();

            return reflect;
        }
        
        private void ReturnToPool()
        {
            _rb.linearVelocity = Vector3.zero;
            _pool.ReturnBullet(_data, this);
        }
    }
}
