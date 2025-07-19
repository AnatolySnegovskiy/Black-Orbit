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

    private readonly Dictionary<BulletScriptableObject, ObjectPool<BulletProjectile>> _pools = new();

        public void Initialize()
        {
            if (Instance != null)
            {
#if UNITY_EDITOR
                Debug.LogError("[BulletPoolManager] Initialized twice!");
#endif
                return;
            }

            Instance = this;
#if UNITY_EDITOR
            Debug.Log("[BulletPoolManager] Initialized.");
#endif
            // Остальная логика старта
        }

    void Init(BulletScriptableObject bulletData, int desiredCount, out ObjectPool<BulletProjectile> pool)
        {
        var prefab = bulletData.bulletPrefab.GetComponent<BulletProjectile>();
            if (prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"Bullet prefab {bulletData.name} не содержит компонент BulletProjectile");
#endif
                pool = null;
                return;
            }
            
            prefab.BulletData = bulletData;
        pool = new ObjectPool<BulletProjectile>(prefab, desiredCount, transform);
            _pools.Add(bulletData, pool);
        }
        
    public BulletProjectile GetBullet(BulletScriptableObject bulletData, int desiredCount = 10)
        {
            if (!_pools.TryGetValue(bulletData, out var pool))
            {
                Init(bulletData, desiredCount, out pool);
            }
            
        BulletProjectile bullet = pool.Get();
            bullet.BulletData = bulletData;
            return bullet;
        }

    public void ReturnBullet(BulletScriptableObject bulletData, BulletProjectile bullet)
        {
            if (_pools.TryGetValue(bulletData, out var pool))
            {
                pool.ReturnToPool(bullet);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Пул для {bulletData.name} не найден при возврате пули");
#endif
                Destroy(bullet.gameObject); // если пула нет, уничтожаем пулю
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
