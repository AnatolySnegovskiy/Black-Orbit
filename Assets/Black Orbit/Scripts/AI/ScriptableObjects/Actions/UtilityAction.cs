using Black_Orbit.Scripts.AI.Runtime;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    public abstract class UtilityAction : ScriptableObject
    {
        [Header("Utility Factors")]
        public UtilityFactor[] factors;

        // каждый Action сам определяет входные значения для своих факторов
        public abstract float[] GetInputs(EnemyAI enemy);

        public float Evaluate(EnemyAI enemy)
        {
            float[] inputs = GetInputs(enemy);
            float total = 0f;

            for (int i = 0; i < factors.Length && i < inputs.Length; i++)
            {
                total += factors[i].Evaluate(inputs[i]);
            }

            
            return Mathf.Clamp01(total / Mathf.Max(1, factors.Length));
        }

        public abstract void Execute(EnemyAI enemy);
    }
}
