using UnityEngine;
using UnityEngine.AI;

namespace Black_Orbit.Scripts.AI.Runtime.Movement
{
    /// <summary>
    /// Encapsulates movement/rotation for AI via NavMeshAgent or Rigidbody.
    /// </summary>
    public class AIMovement
    {
        private readonly AI _ai;
        private readonly NavMeshAgent _agent;
        private readonly Rigidbody _rb;

        public AIMovement(AI ai, NavMeshAgent agent, Rigidbody rb)
        {
            _ai = ai;
            _agent = agent;
            _rb = rb;
        }

        public void Stop()
        {
            if (_agent != null)
            {
                _agent.ResetPath();
            }
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
            }
        }

        public void MoveTo(Vector3 targetPos)
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.SetDestination(targetPos);
            }
            else if (_rb != null)
            {
                Vector3 dir = (targetPos - _rb.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir = dir.normalized;
                    _rb.MovePosition(_rb.position + dir * (_ai.MoveSpeed * Time.deltaTime));
                }
            }
        }

        public bool IsAtDestination(Vector3 dest, float tolerance = 0.5f)
        {
            if (_agent != null && _agent.hasPath)
            {
                return !float.IsPositiveInfinity(_agent.remainingDistance) && _agent.remainingDistance <= tolerance;
            }
            return Vector3.Distance(_ai.transform.position, dest) <= tolerance;
        }

        public void LookAt(Vector3 point, float turnSpeed)
        {
            Vector3 dir = (point - _ai.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(dir);
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }
        }

        public void UpdateRotation(Transform target)
        {
            if (target == null) return;
            Vector3 direction = (target.position - _ai.transform.position);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                direction.Normalize();
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _ai.transform.rotation = Quaternion.Slerp(_ai.transform.rotation, targetRotation, Time.deltaTime * _ai.RotationSpeed);
            }
        }
    }
}
