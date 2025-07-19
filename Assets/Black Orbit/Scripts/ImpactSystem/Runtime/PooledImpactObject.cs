using Black_Orbit.Scripts.Core.Pooling;
using UnityEngine;

namespace Black_Orbit.Scripts.ImpactSystem.Runtime
{
    public class PooledImpactObject : MonoBehaviour
    {
        private ObjectPool<PooledImpactObject> _pool;

        public void Initialize(ObjectPool<PooledImpactObject> pool)
        {
            _pool = pool;
        }

        public void ReturnToPool(float delay = 5f)
        {
            StartCoroutine(ReturnAfterDelay(delay));
        }

        private System.Collections.IEnumerator ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _pool.ReturnToPool(this);
        }
    }
}
