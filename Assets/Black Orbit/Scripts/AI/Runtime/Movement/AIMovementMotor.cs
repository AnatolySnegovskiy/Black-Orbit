using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class AIMovementMotor : MonoBehaviour
    {
        [Header("Movement Settings")] public float maxSpeed = 5f;
        public float acceleration = 20f;
        public float stopDistance = 0.2f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public bool MoveTowards(Vector3 target, float dt)
        {
            Vector3 to = target - transform.position;
            float dist = to.magnitude;
            if (dist <= stopDistance)
            {
                // Тормозим
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, Mathf.Clamp01(dt * acceleration));
                return true;
            }

            Vector3 dir = to / Mathf.Max(dist, 0.0001f);
            Vector3 desiredVel = dir * maxSpeed;
            // Плавно выходим на нужную скорость
            _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, desiredVel, acceleration * dt);
            return false;
        }

        public void StopImmediate()
        {
            _rb.linearVelocity = Vector3.zero;
        }
    }
}
