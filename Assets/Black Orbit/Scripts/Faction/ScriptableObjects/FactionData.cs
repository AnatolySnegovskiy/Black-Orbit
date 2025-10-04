using System.Collections.Generic;
using UnityEngine;

namespace Black_Orbit.Scripts.Faction.ScriptableObjects
{
    /// <summary>
    /// ScriptableObject представляющий фракцию.
    /// Создавайте новые фракции через Create → Faction System → Faction.
    /// </summary>
    [CreateAssetMenu(fileName = "New Faction", menuName = "Faction System/Faction", order = 1)]
    public class FactionData : ScriptableObject
    {
        [Header("Основная информация")]
        [Tooltip("Уникальное имя фракции")]
        public string factionName = "New Faction";
        
        [Tooltip("Описание фракции")]
        [TextArea(3, 5)]
        public string description = "";
        
        [Tooltip("Цвет фракции (для UI и визуализации)")]
        public Color factionColor = Color.white;
        
        [Tooltip("Иконка фракции (опционально)")]
        public Sprite icon;

        [Header("Отношения с другими фракциями")]
        [Tooltip("Список отношений с другими фракциями. Значение от -1 (враг) до 1 (друг)")]
        public List<FactionRelationship> relationships = new List<FactionRelationship>();

        /// <summary>
        /// Получает отношение к указанной фракции
        /// </summary>
        /// <param name="otherFaction">Другая фракция</param>
        /// <returns>Значение от -1 (враг) до 1 (друг), 0 если не найдено</returns>
        public float GetRelationship(FactionData otherFaction)
        {
            if (otherFaction == null) return 0f;
            if (otherFaction == this) return 1f; // Сама с собой - друг

            foreach (var rel in relationships)
            {
                if (rel.faction == otherFaction)
                    return rel.relationshipValue;
            }

            return 0f; // Нейтральные по умолчанию
        }

        /// <summary>
        /// Устанавливает отношение к указанной фракции
        /// </summary>
        public void SetRelationship(FactionData otherFaction, float value)
        {
            if (otherFaction == null || otherFaction == this) return;

            value = Mathf.Clamp(value, -1f, 1f);

            // Ищем существующее отношение
            foreach (var rel in relationships)
            {
                if (rel.faction == otherFaction)
                {
                    rel.relationshipValue = value;
                    return;
                }
            }

            // Добавляем новое
            relationships.Add(new FactionRelationship
            {
                faction = otherFaction,
                relationshipValue = value
            });
        }

        /// <summary>
        /// Проверяет, является ли фракция враждебной (отношение < -0.3)
        /// </summary>
        public bool IsHostile(FactionData otherFaction)
        {
            return GetRelationship(otherFaction) < -0.3f;
        }

        /// <summary>
        /// Проверяет, является ли фракция союзной (отношение > 0.3)
        /// </summary>
        public bool IsAllied(FactionData otherFaction)
        {
            return GetRelationship(otherFaction) > 0.3f;
        }

        /// <summary>
        /// Проверяет, является ли фракция нейтральной (-0.3 <= отношение <= 0.3)
        /// </summary>
        public bool IsNeutral(FactionData otherFaction)
        {
            float rel = GetRelationship(otherFaction);
            return rel >= -0.3f && rel <= 0.3f;
        }

        /// <summary>
        /// Получает текстовое описание отношения
        /// </summary>
        public string GetRelationshipDescription(FactionData otherFaction)
        {
            float rel = GetRelationship(otherFaction);

            if (rel <= -0.8f) return "Лютый враг";
            if (rel <= -0.5f) return "Враг";
            if (rel <= -0.3f) return "Недружелюбный";
            if (rel < 0.3f) return "Нейтральный";
            if (rel < 0.5f) return "Дружелюбный";
            if (rel < 0.8f) return "Друг";
            return "Лучший друг";
        }

        /// <summary>
        /// Изменяет отношение на указанное значение (например, +0.1 или -0.2)
        /// </summary>
        public void ModifyRelationship(FactionData otherFaction, float delta)
        {
            float current = GetRelationship(otherFaction);
            SetRelationship(otherFaction, current + delta);
        }

        /// <summary>
        /// Улучшает репутацию с фракцией (алиас для ModifyRelationship с положительным значением)
        /// </summary>
        public void IncreaseReputation(FactionData otherFaction, float amount)
        {
            ModifyRelationship(otherFaction, Mathf.Abs(amount));
        }

        /// <summary>
        /// Ухудшает репутацию с фракцией (алиас для ModifyRelationship с отрицательным значением)
        /// </summary>
        public void DecreaseReputation(FactionData otherFaction, float amount)
        {
            ModifyRelationship(otherFaction, -Mathf.Abs(amount));
        }
    }

    /// <summary>
    /// Отношение к одной фракции
    /// </summary>
    [System.Serializable]
    public class FactionRelationship
    {
        [Tooltip("Фракция")]
        public FactionData faction;
        
        [Tooltip("Отношение: -1 (лютый враг) ... 0 (нейтрал) ... 1 (лучший друг)")]
        [Range(-1f, 1f)]
        public float relationshipValue = 0f;
    }
}
