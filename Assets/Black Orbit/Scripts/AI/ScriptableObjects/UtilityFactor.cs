using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects
{
    [System.Serializable]
    public class UtilityFactor
    {
        public string name;
        [Range(0f, 1f)] public float weight = 1f; // важность фактора
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        // Берем входные данные (0..1) и переводим в значение полезности
        public float Evaluate(float input)
        {
            return curve.Evaluate(Mathf.Clamp01(input)) * weight;
        }
    }
}
