using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Тип канала действия — позволяет исполнять несколько действий параллельно (например, движение + стрельба).
    /// </summary>
    public enum ActionChannel
    {
        Movement,
        Combat,
        Support
    }

    /// <summary>
    /// Базовый класс для всех действий AI с Utility-системой.
    /// Каждое действие оценивает свою полезность на основе факторов и выполняется, если имеет наивысший приоритет.
    /// Поддерживает каналы для одновременного исполнения (движение/бой/поддержка).
    /// </summary>
    public abstract class UtilityAction : ScriptableObject
    {
        [Header("Факторы полезности")]
        [Tooltip("Список факторов для оценки приоритета этого действия. Каждый фактор имеет вес и кривую отклика")]
        public UtilityFactor[] factors;

        /// <summary>
        /// Канал действия. По умолчанию — движение.
        /// Переопределяется в боевых экшенах (стрельба/удар) как Combat.
        /// </summary>
        public virtual ActionChannel Channel => ActionChannel.Movement;

        /// <summary>
        /// Возвращает входные значения (0..1) для каждого фактора на основе текущего состояния AI
        /// </summary>
        public abstract float[] GetInputs(Runtime.AI ai);

        /// <summary>
        /// Оценивает полезность действия (0..1) на основе всех факторов
        /// </summary>
        public float Evaluate(Runtime.AI ai)
        {
            float[] inputs = GetInputs(ai);
            float total = 0f;

            // Суммируем взвешенные оценки всех факторов
            for (int i = 0; i < factors.Length && i < inputs.Length; i++)
            {
                total += factors[i].Evaluate(inputs[i]);
            }

            // Нормализуем результат (среднее значение)
            return Mathf.Clamp01(total / Mathf.Max(1, factors.Length));
        }

        /// <summary>
        /// Выполняет действие (вызывается каждый кадр, пока действие активно)
        /// </summary>
        public abstract void Execute(Runtime.AI ai);
    }
}
