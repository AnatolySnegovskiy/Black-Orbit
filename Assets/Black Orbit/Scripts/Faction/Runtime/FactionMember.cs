using Black_Orbit.Scripts.Faction.ScriptableObjects;
using UnityEngine;

namespace Black_Orbit.Scripts.Faction.Runtime
{
    /// <summary>
    /// Компонент для объектов, принадлежащих к фракции.
    /// Используйте на игроке, NPC, врагах - на любых объектах которые должны иметь фракцию.
    /// </summary>
    public class FactionMember : MonoBehaviour
    {
        [Header("Фракция")]
        [Tooltip("Фракция этого объекта (ScriptableObject)")]
        public FactionData faction;

        /// <summary>
        /// Получает отношение к другой фракции
        /// </summary>
        /// <returns>Значение от -1 (враг) до 1 (друг)</returns>
        public float GetRelationship(FactionData otherFaction)
        {
            if (faction == null) return 0f;
            return faction.GetRelationship(otherFaction);
        }

        /// <summary>
        /// Получает отношение к другому члену фракции
        /// </summary>
        public float GetRelationship(FactionMember other)
        {
            if (other == null || other.faction == null) return 0f;
            return GetRelationship(other.faction);
        }

        /// <summary>
        /// Проверяет, является ли фракция враждебной (< -0.3)
        /// </summary>
        public bool IsHostile(FactionData otherFaction)
        {
            if (faction == null) return false;
            return faction.IsHostile(otherFaction);
        }

        /// <summary>
        /// Проверяет, является ли другой член враждебным
        /// </summary>
        public bool IsHostile(FactionMember other)
        {
            if (other == null || other.faction == null) return false;
            return IsHostile(other.faction);
        }

        /// <summary>
        /// Проверяет, является ли фракция союзной (> 0.3)
        /// </summary>
        public bool IsAllied(FactionData otherFaction)
        {
            if (faction == null) return false;
            return faction.IsAllied(otherFaction);
        }

        /// <summary>
        /// Проверяет, является ли другой член союзным
        /// </summary>
        public bool IsAllied(FactionMember other)
        {
            if (other == null || other.faction == null) return false;
            return IsAllied(other.faction);
        }

        /// <summary>
        /// Проверяет, является ли фракция нейтральной (-0.3 <= x <= 0.3)
        /// </summary>
        public bool IsNeutral(FactionData otherFaction)
        {
            if (faction == null) return true;
            return faction.IsNeutral(otherFaction);
        }

        /// <summary>
        /// Проверяет, является ли другой член нейтральным
        /// </summary>
        public bool IsNeutral(FactionMember other)
        {
            if (other == null || other.faction == null) return true;
            return IsNeutral(other.faction);
        }

        /// <summary>
        /// Получает текстовое описание отношения
        /// </summary>
        public string GetRelationshipDescription(FactionData otherFaction)
        {
            if (faction == null) return "Нет фракции";
            return faction.GetRelationshipDescription(otherFaction);
        }
    }
}
