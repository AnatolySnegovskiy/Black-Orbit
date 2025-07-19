using System.Collections.Generic;
using Black_Orbit.Scripts.Core.Base;
using Black_Orbit.Scripts.Core.Pooling;
using UnityEngine;
using Black_Orbit.Scripts.WeaponSystem.ScriptableObjects;

namespace Black_Orbit.Scripts.WeaponSystem.Runtime
{
    public class BulletPoolManager : MonoBehaviour, IGameSystem
    {
        public static BulletPoolManager Instance { get; private set; }

        private readonly Dictionary<BulletScriptableObject, ObjectPool<PooledBullet>> _pools = new();

        public void Initialize()
        {
            if (Instance != null)
            {
                Debug.LogError("[ImpactManager] Initialized twice!");
                return;
            }

            Instance = this;
            Debug.Log("[ImpactManager] Initialized.");
            // Остальная логика старта
        }

        void Init(BulletScriptableObject bulletData, int desiredCount, out ObjectPool<PooledBullet> pool)
        {
            var prefab = bulletData.bulletPrefab.GetComponent<PooledBullet>();
            if (prefab == null)
            {
                Debug.LogError($"Bullet prefab {bulletData.name} не содержит компонент PooledBullet");
                pool = null;
                return;
            }
            
            prefab.BulletData = bulletData;
            pool = new ObjectPool<PooledBullet>(prefab, desiredCount, transform);
            _pools.Add(bulletData, pool);
        }
        
        public PooledBullet GetBullet(BulletScriptableObject bulletData, int desiredCount = 10)
        {
            if (!_pools.TryGetValue(bulletData, out var pool))
            {
                Init(bulletData, desiredCount, out pool);
            }
            
            PooledBullet bullet = pool.Get();
            bullet.BulletData = bulletData;
            return bullet;
        }

        public void ReturnBullet(BulletScriptableObject bulletData, PooledBullet bullet)
        {
            if (_pools.TryGetValue(bulletData, out var pool))
            {
                pool.ReturnToPool(bullet);
            }
            else
            {
                Debug.LogWarning($"Пул для {bulletData.name} не найден при возврате пули");
                Destroy(bullet.gameObject); // fallback
            }
        }
        
        public void Shutdown()
        {
            foreach (var pool in _pools.Values)
            {
                pool.ClearPool();
            }
            
            _pools.Clear();
            Instance = null;
        }   
    }
}
