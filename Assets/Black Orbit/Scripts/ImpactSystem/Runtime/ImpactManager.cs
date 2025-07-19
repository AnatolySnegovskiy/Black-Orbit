using System.Collections.Generic;
using Black_Orbit.Scripts.Core.Base;
using Black_Orbit.Scripts.Core.Pooling;
using Black_Orbit.Scripts.ImpactSystem.ScriptableObjects;
using UnityEngine;

namespace Black_Orbit.Scripts.ImpactSystem.Runtime
{
    public class ImpactManager : MonoBehaviour, IGameSystem
    {
        public static ImpactManager Instance { get; private set; }

        [SerializeField] private ImpactSurfaceDatabase surfaceDatabase;
        [SerializeField] private int initialPoolSize = 10;

        private readonly Dictionary<GameObject, ObjectPool<PooledImpactObject>> _effectPools = new();
        private readonly Dictionary<GameObject, ObjectPool<PooledImpactObject>> _decalPools = new();

        public ImpactSurfaceDatabase SurfaceDatabase => surfaceDatabase;

        public void Initialize()
        {
            if (Instance != null) return;
            Instance = this;

            if (surfaceDatabase == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[ImpactManager] SurfaceDatabase не назначен!");
#endif
            }

#if UNITY_EDITOR
            Debug.Log($"[ImpactManager] Инициализирован, записей: {surfaceDatabase?.GetAllEntries().Count}");
#endif
        }

        public void HandleImpact(Vector3 position, Vector3 normal, int surfaceID, float scale = 1f)
        {
            var surfaceEntry = surfaceDatabase.GetSurfaceEntry(surfaceID);

            if (surfaceEntry.impactEffectPrefab != null)
                SpawnFromPool(surfaceEntry.impactEffectPrefab, position, normal, scale, _effectPools);

            if (surfaceEntry.decalPrefab != null)
                SpawnFromPool(surfaceEntry.decalPrefab, position, normal, scale, _decalPools);

            if (surfaceEntry.impactSound != null)
                AudioSource.PlayClipAtPoint(surfaceEntry.impactSound, position);
        }

        private void SpawnFromPool(GameObject prefab, Vector3 position, Vector3 normal, float scale, Dictionary<GameObject, ObjectPool<PooledImpactObject>> poolDict)
        {
            if (!poolDict.TryGetValue(prefab, out var pool))
            {
                var impactComponent = prefab.GetComponent<PooledImpactObject>();
                if (impactComponent == null)
                {
#if UNITY_EDITOR
                    Debug.LogError($"Префаб {prefab.name} должен содержать компонент PooledImpactObject");
#endif
                    return;
                }

                pool = new ObjectPool<PooledImpactObject>(impactComponent, initialPoolSize, transform);
                poolDict[prefab] = pool;
            }

            var instance = pool.Get();
            instance.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
            instance.transform.localScale = Vector3.one * scale;
            instance.Initialize(pool);
            instance.ReturnToPool(); // Вернётся в пул через 5 секунд по умолчанию
        }
        
        public void Shutdown()
        {
            foreach (var pool in _effectPools.Values)
            {
                pool.ClearPool();
            }
            
            foreach (var pool in _decalPools.Values)
            {
                pool.ClearPool();
            }
            
            _effectPools.Clear();
            _decalPools.Clear();
            Instance = null;
        }
    }
}
