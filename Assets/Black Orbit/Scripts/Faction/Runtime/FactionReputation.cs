using Black_Orbit.Scripts.Faction.ScriptableObjects;
using System.Collections.Generic;
using UnityEngine;

namespace Black_Orbit.Scripts.Faction.Runtime
{
    /// <summary>
    /// Компонент для отслеживания репутации с фракциями.
    /// Хранит модификаторы отношений которые применяются поверх базовых значений FactionData.
    /// </summary>
    public class FactionReputation : MonoBehaviour
    {
        [Header("Репутация")]
        [Tooltip("Фракция владельца (обычно игрок)")]
        public FactionData ownerFaction;

        [Tooltip("Модификаторы репутации с другими фракциями")]
        public List<ReputationModifier> reputationModifiers = new List<ReputationModifier>();

        [Header("Настройки")]
        [Tooltip("Минимальное значение репутации")]
        public float minReputation = -100f;

        [Tooltip("Максимальное значение репутации")]
        public float maxReputation = 100f;

        [Tooltip("Показывать логи изменения репутации")]
        public bool debugMode = false;

        /// <summary>
        /// Получает модификатор репутации для указанной фракции
        /// </summary>
        public float GetReputationModifier(FactionData faction)
        {
            if (faction == null) return 0f;

            foreach (var mod in reputationModifiers)
            {
                if (mod.faction == faction)
                    return mod.reputationValue;
            }

            return 0f;
        }

        /// <summary>
        /// Получает итоговое отношение с учётом репутации
        /// </summary>
        /// <param name="faction">Фракция для проверки</param>
        /// <returns>Значение от -1 до 1</returns>
        public float GetTotalRelationship(FactionData faction)
        {
            if (ownerFaction == null || faction == null) return 0f;

            // Базовое отношение из FactionData
            float baseRelationship = ownerFaction.GetRelationship(faction);

            // Модификатор репутации
            float reputationMod = GetReputationModifier(faction);

            // Конвертируем репутацию (-100...100) в модификатор отношений (-1...1)
            float normalizedMod = Mathf.Clamp(reputationMod / 100f, -1f, 1f);

            // Итоговое отношение
            float total = Mathf.Clamp(baseRelationship + normalizedMod, -1f, 1f);

            return total;
        }

        /// <summary>
        /// Изменяет репутацию с фракцией
        /// </summary>
        /// <param name="faction">Фракция</param>
        /// <param name="delta">Изменение репутации</param>
        public void ModifyReputation(FactionData faction, float delta)
        {
            if (faction == null) return;

            // Ищем существующий модификатор
            ReputationModifier existing = null;
            foreach (var mod in reputationModifiers)
            {
                if (mod.faction == faction)
                {
                    existing = mod;
                    break;
                }
            }

            if (existing != null)
            {
                // Обновляем существующий
                float oldValue = existing.reputationValue;
                existing.reputationValue = Mathf.Clamp(existing.reputationValue + delta, minReputation, maxReputation);

                if (debugMode)
                {
                    Debug.Log($"[Reputation] {faction.factionName}: {oldValue:F1} → {existing.reputationValue:F1} ({delta:+0.0;-0.0})");
                }
            }
            else
            {
                // Создаём новый
                float newValue = Mathf.Clamp(delta, minReputation, maxReputation);
                reputationModifiers.Add(new ReputationModifier
                {
                    faction = faction,
                    reputationValue = newValue
                });

                if (debugMode)
                {
                    Debug.Log($"[Reputation] {faction.factionName}: 0 → {newValue:F1} ({delta:+0.0;-0.0})");
                }
            }

            // Проверяем изменение статуса отношений
            CheckRelationshipStatusChange(faction);
        }

        /// <summary>
        /// Устанавливает репутацию с фракцией
        /// </summary>
        public void SetReputation(FactionData faction, float value)
        {
            if (faction == null) return;

            value = Mathf.Clamp(value, minReputation, maxReputation);

            // Ищем существующий модификатор
            bool found = false;
            foreach (var mod in reputationModifiers)
            {
                if (mod.faction == faction)
                {
                    mod.reputationValue = value;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                reputationModifiers.Add(new ReputationModifier
                {
                    faction = faction,
                    reputationValue = value
                });
            }

            if (debugMode)
            {
                Debug.Log($"[Reputation] {faction.factionName} установлена на {value:F1}");
            }

            CheckRelationshipStatusChange(faction);
        }

        /// <summary>
        /// Проверяет изменение статуса отношений (враг/нейтрал/друг)
        /// </summary>
        void CheckRelationshipStatusChange(FactionData faction)
        {
            if (!debugMode || ownerFaction == null || faction == null) return;

            float total = GetTotalRelationship(faction);
            string status = GetRelationshipStatus(total);

            Debug.Log($"[Reputation] Отношение с {faction.factionName}: {total:F2} ({status})");
        }

        /// <summary>
        /// Получает статус отношений
        /// </summary>
        string GetRelationshipStatus(float relationship)
        {
            if (relationship <= -0.8f) return "Лютый враг";
            if (relationship <= -0.5f) return "Враг";
            if (relationship <= -0.3f) return "Недружелюбный";
            if (relationship < 0.3f) return "Нейтральный";
            if (relationship < 0.5f) return "Дружелюбный";
            if (relationship < 0.8f) return "Друг";
            return "Лучший друг";
        }

        /// <summary>
        /// Проверяет, является ли фракция враждебной с учётом репутации
        /// </summary>
        public bool IsHostile(FactionData faction)
        {
            return GetTotalRelationship(faction) < -0.3f;
        }

        /// <summary>
        /// Проверяет, является ли фракция союзной с учётом репутации
        /// </summary>
        public bool IsAllied(FactionData faction)
        {
            return GetTotalRelationship(faction) > 0.3f;
        }

        /// <summary>
        /// Проверяет, является ли фракция нейтральной с учётом репутации
        /// </summary>
        public bool IsNeutral(FactionData faction)
        {
            float rel = GetTotalRelationship(faction);
            return rel >= -0.3f && rel <= 0.3f;
        }

        /// <summary>
        /// Получает описание репутации
        /// </summary>
        public string GetReputationDescription(FactionData faction)
        {
            float rep = GetReputationModifier(faction);

            if (rep <= -80f) return "Ненавидят";
            if (rep <= -50f) return "Враждебны";
            if (rep <= -20f) return "Недружелюбны";
            if (rep < 20f) return "Нейтральны";
            if (rep < 50f) return "Дружелюбны";
            if (rep < 80f) return "Уважают";
            return "Почитают";
        }

        /// <summary>
        /// Сбрасывает всю репутацию
        /// </summary>
        [ContextMenu("Reset All Reputation")]
        public void ResetAllReputation()
        {
            reputationModifiers.Clear();
            if (debugMode)
            {
                Debug.Log("[Reputation] Вся репутация сброшена");
            }
        }

        /// <summary>
        /// Выводит всю репутацию в консоль
        /// </summary>
        [ContextMenu("Print All Reputation")]
        public void PrintAllReputation()
        {
            Debug.Log("=== Reputation Status ===");

            foreach (var mod in reputationModifiers)
            {
                if (mod.faction == null) continue;

                float total = GetTotalRelationship(mod.faction);
                string status = GetRelationshipStatus(total);
                string repDesc = GetReputationDescription(mod.faction);

                Debug.Log($"{mod.faction.factionName}:");
                Debug.Log($"  Репутация: {mod.reputationValue:F1} ({repDesc})");
                Debug.Log($"  Итоговое отношение: {total:F2} ({status})");
            }

            Debug.Log($"\nTotal tracked factions: {reputationModifiers.Count}");
        }
    }

    /// <summary>
    /// Модификатор репутации для одной фракции
    /// </summary>
    [System.Serializable]
    public class ReputationModifier
    {
        [Tooltip("Фракция")]
        public FactionData faction;

        [Tooltip("Значение репутации (-100 ... 100)")]
        [Range(-100f, 100f)]
        public float reputationValue = 0f;
    }
}
