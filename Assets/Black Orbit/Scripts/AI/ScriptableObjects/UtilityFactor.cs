using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects
{
    /// <summary>
    /// Фактор для Utility-системы: преобразует входное значение (0..1) в оценку полезности через кривую и вес.
    /// Используется для настройки приоритетов действий в зависимости от контекста.
    /// </summary>
    [System.Serializable]
    public class UtilityFactor
    {
        [Tooltip("Название фактора (для удобства в инспекторе)")]
        public string name;
        
        [Tooltip("Вес фактора (0-1). Чем выше, тем больше влияние на итоговую оценку")]
        [Range(0f, 1f)] public float weight = 1f;
        
        [Tooltip("Кривая отклика (0..1 → 0..1). Линейная = прямая зависимость, S-образная = пороговая, колокол = оптимум в середине")]
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        /// <summary>
        /// Оценивает входное значение (0..1) и возвращает взвешенную полезность
        /// </summary>
        public float Evaluate(float input)
        {
            return curve.Evaluate(Mathf.Clamp01(input)) * weight;
        }
    }
}
