using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class AIMovementMotor : MonoBehaviour
    {
        [Header("Движение")] public float maxSpeed = 5f;
        public float acceleration = 20f;
        public float stopDistance = 0.2f;

        [Header("Ротация")]
        [Tooltip("Поворачивать корпус по направлению текущей скорости (по горизонту)")]
        public bool rotateToVelocity = true;
        [Tooltip("Скорость поворота (град/сек)")]
        public float rotationSpeed = 540f;
        [Tooltip("Игнорировать тангаж/крен, вращать только по Y")]
        public bool yawOnly = true;

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

        private void Update()
        {
            if (!rotateToVelocity) return;
            Vector3 v = _rb != null ? _rb.linearVelocity : Vector3.zero;
            v.y = 0f; // горизонтальная составляющая
            if (v.sqrMagnitude < 0.0004f) return; // слишком медленно чтобы вращать

            Quaternion targetRot = Quaternion.LookRotation(v.normalized, Vector3.up);
            if (yawOnly)
            {
                var e = targetRot.eulerAngles;
                targetRot = Quaternion.Euler(0f, e.y, 0f);
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
}
