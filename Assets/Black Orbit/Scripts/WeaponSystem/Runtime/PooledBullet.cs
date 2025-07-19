using System;
using System.Collections.Generic;
using Black_Orbit.Scripts.WeaponSystem.Base;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Black_Orbit.Scripts.WeaponSystem.Runtime
{
    [RequireComponent(typeof(Rigidbody))]
    public class PooledBullet : MonoBehaviour
    {
        [SerializeField] private GameObject inpactEffect;
        [SerializeField] private LayerMask hitMask = ~0; // по умолчанию всё, кроме Nothing

        private Rigidbody _rb;
        private BulletScriptableObject _data;
        private float _multiplier = 1f;
        private BulletPoolManager _pool = BulletPoolManager.Instance;
        private static readonly Collider[] Overlap = new Collider[32];
        internal BulletScriptableObject BulletData { set => _data = value; }

        private Vector3 _localScale;
        private int _ricochetCount;
        private float _skinWidth = 0.01f;
        private float _radius = 0.025f; // половина диаметра коллайдера
        private bool _hasProcessedHitThisFrame;

        private void Awake()
        {
            _rb = transform.GetComponent<Rigidbody>();
            _localScale = transform.localScale;
        }

        private void FixedUpdate()
        {
            _hasProcessedHitThisFrame = false;

            Vector3 dir = _rb.linearVelocity.normalized;
            float dist = _rb.linearVelocity.magnitude * Time.fixedDeltaTime + _skinWidth;

            if (Physics.SphereCast(transform.position, _radius, dir, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (!_hasProcessedHitThisFrame)
                {
                    HandleHit(hit.collider, hit.point, hit.normal);
                    _hasProcessedHitThisFrame = true;
                }
            }
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

        private List<Vector3> lithit = new List<Vector3>();
        
        private void HandleHit(Collider collider, Vector3 point, Vector3 normal)
        {
            Vector3 bulletVelocity = _rb.linearVelocity;
            float bulletMass = _rb.mass;

            Vector3 impulse = bulletVelocity * bulletMass;
            Rigidbody col = collider.GetComponent<Rigidbody>();
            if (col != null)
            {
                col.AddForceAtPosition(impulse, point, ForceMode.Impulse);
            }


            ApplyDirectHitDamage(collider);
            ApplyExplosionDamage();

            if (!TryRicochetOrReturn(normal, point))
            {
                //ReturnToPool();
            }

            if (inpactEffect)
            {
                GameObject fx = Instantiate(inpactEffect, point, Quaternion.FromToRotation(-normal, Vector3.up));
                fx.transform.SetParent(collider.transform, worldPositionStays: true);
                lithit.Add(point);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasProcessedHitThisFrame) return;
            var contact = collision.contacts[0];
            HandleHit(collision.collider, contact.point, contact.normal);
            _hasProcessedHitThisFrame = true;
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

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _data.bulletExplosionRadius,
                Overlap,
                hitMask, // 💥 учитывать только нужные слои
                QueryTriggerInteraction.Ignore
            );
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

        private bool TryRicochetOrReturn(Vector3 hitNormal, Vector3 hitPoint)
        {
            if (hitNormal == Vector3.zero)
            {
                Debug.LogWarning("[Ricochet] Hit normal is zero — ignoring.");
                return false;
            }
            
            Vector3 incomingDir = _rb.linearVelocity.normalized;
            float angle = Vector3.Angle(-incomingDir, hitNormal);

            Debug.DrawRay(hitPoint, hitNormal, Color.green, 2f);
            Debug.DrawRay(hitPoint, incomingDir, Color.yellow, 2f);
            Debug.Log($"[Ricochet] Hit angle: {angle:F1}° | RicochetChance: {_data.ricochetChance} | RicochetCount: {_ricochetCount}");

            bool isRicochetAngleValid = angle >= _data.minRicochetAngle && angle < 89f;
            float chance = _data.ricochetChance * Mathf.Pow(_data.ricochetChanceFalloff, _ricochetCount);
            bool chancePassed = Random.value <= chance;

            if (isRicochetAngleValid && chancePassed)
            {
                if (angle <= 1f || angle >= 89f)
                    Debug.LogWarning($"[Ricochet] Unnatural hit angle: {angle:F1}° — position: {hitPoint}");
                
                _ricochetCount++;
                _multiplier *= _data.ricochetDamageMultiplier;

                Vector3 reflectDir = GetRicochetDirection(incomingDir, hitNormal, angle);

                float speedLoss = Mathf.Clamp01(1f - (angle / 90f) * 0.5f);
                _rb.linearVelocity = reflectDir * _data.bulletSpeed * speedLoss;
                transform.rotation = Quaternion.LookRotation(reflectDir);

                Debug.DrawRay(hitPoint, reflectDir * 2f, Color.red, 2f);
                Debug.Log($"[Ricochet] Ricochet happened! New direction: {reflectDir}, Speed loss: {speedLoss:F2}");

                return true;
            }

            Debug.Log("[Ricochet] No ricochet. Returning bullet to pool.");
            return false;
        }

        private Vector3 GetRicochetDirection(Vector3 incoming, Vector3 normal, float impactAngle)
        {
            Vector3 reflected = Vector3.Reflect(incoming, normal).normalized;

            float maxDeviation = (impactAngle < 25f) ? 0f : 5f;
            Vector3 tangent = Vector3.Cross(normal, reflected).normalized;

            Quaternion deviation = Quaternion.AngleAxis(Random.Range(-maxDeviation, maxDeviation), tangent);
            Vector3 deviatedReflect = (deviation * reflected).normalized;

            return deviatedReflect;
        }


        private void ReturnToPool()
        {
            _rb.linearVelocity = Vector3.zero;
            _pool.ReturnBullet(_data, this);
        }

        private void OnDrawGizmos()
        {
            if (lithit == null || lithit.Count < 2)
                return;

            Gizmos.color = Color.yellow; // цвет линий

            for (int i = 0; i < lithit.Count - 1; i++)
            {
                Gizmos.DrawLine(lithit[i], lithit[i + 1]);
            }
        }
    }
}
