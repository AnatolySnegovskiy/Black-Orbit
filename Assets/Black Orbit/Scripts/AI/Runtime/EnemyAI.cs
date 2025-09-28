using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime
{
    public class EnemyAI : MonoBehaviour
    {
        public Transform player;
        public Rigidbody rb;
        public float moveSpeed = 3f;
        public float detectionRange = 10f;
        public float attackRange = 2f;
        [Range(0f, 100f)] public float health = 100f;

        public UtilityAction[] actions;
        private UtilityAction _currentAction;

        public float HealthNormalized => health / 100f;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            SelectAndExecuteAction();
        }

        void SelectAndExecuteAction()
        {
            float bestScore = -1f;
            UtilityAction bestAction = null;

            foreach (var action in actions)
            {
                float score = action.Evaluate(this);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAction = action;
                }
            }

            if (bestAction != null)
            {
                if (_currentAction != bestAction)
                {
                    _currentAction = bestAction;
                    Debug.Log($"🤖 Выбрано действие: {_currentAction.name} (Score={bestScore:F2})");
                }
                _currentAction.Execute(this);
            }
        }

        public void Stop()
        {
            rb.linearVelocity = Vector3.zero;
        }
    }
}
