using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Combat
{
    public class AIGrenadeThrower : MonoBehaviour
    {
        [Header("Grenade")]
        public GameObject grenadePrefab;
        public float throwForce = 14f;
        public float upBias = 0.2f;

        public bool ThrowAt(Vector3 target)
        {
            if (grenadePrefab == null) return false;
            var go = Instantiate(grenadePrefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();

            Vector3 to = target - transform.position;
            to.y = 0f;
            Vector3 dir = (to.normalized + Vector3.up * upBias).normalized;
            rb.linearVelocity = dir * throwForce;
            return true;
        }
    }
}
