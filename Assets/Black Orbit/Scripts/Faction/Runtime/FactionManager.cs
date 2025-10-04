using Black_Orbit.Scripts.Faction.ScriptableObjects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Black_Orbit.Scripts.Faction.Runtime
{
    /// <summary>
    /// Singleton менеджер для управления всеми фракциями в игре.
    /// Хранит список всех фракций и предоставляет утилиты для работы с ними.
    /// </summary>
    public class FactionManager : MonoBehaviour
    {
        private static FactionManager _instance;
        public static FactionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FactionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("FactionManager");
                        _instance = go.AddComponent<FactionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Все фракции в игре")]
        [Tooltip("Список всех доступных фракций. Добавьте сюда все созданные FactionData")]
        public List<FactionData> allFactions = new List<FactionData>();

        [Header("Настройки")]
        [Tooltip("Автоматически синхронизировать отношения (если A враг B, то B враг A)")]
        public bool autoSyncRelationships = true;

        [Tooltip("Показывать логи отладки")]
        public bool debugMode = false;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoSyncRelationships)
            {
                SyncAllRelationships();
            }

            if (debugMode)
            {
                Debug.Log($"[FactionManager] Инициализирован с {allFactions.Count} фракциями");
            }
        }

        /// <summary>
        /// Синхронизирует отношения между всеми фракциями (двусторонние)
        /// </summary>
        [ContextMenu("Sync All Relationships")]
        public void SyncAllRelationships()
        {
            foreach (var faction in allFactions)
            {
                if (faction == null) continue;

                foreach (var rel in faction.relationships)
                {
                    if (rel.faction == null) continue;

                    // Устанавливаем обратное отношение
                    rel.faction.SetRelationship(faction, rel.relationshipValue);
                }
            }

            if (debugMode)
                Debug.Log("[FactionManager] Отношения синхронизированы");
        }

        /// <summary>
        /// Получает фракцию по имени
        /// </summary>
        public FactionData GetFaction(string factionName)
        {
            return allFactions.FirstOrDefault(f => f != null && f.factionName == factionName);
        }

        /// <summary>
        /// Получает все враждебные фракции для указанной фракции
        /// </summary>
        public List<FactionData> GetHostileFactions(FactionData faction)
        {
            if (faction == null) return new List<FactionData>();

            return allFactions.Where(f => f != null && faction.IsHostile(f)).ToList();
        }

        /// <summary>
        /// Получает все союзные фракции для указанной фракции
        /// </summary>
        public List<FactionData> GetAlliedFactions(FactionData faction)
        {
            if (faction == null) return new List<FactionData>();

            return allFactions.Where(f => f != null && faction.IsAllied(f)).ToList();
        }

        /// <summary>
        /// Получает все нейтральные фракции для указанной фракции
        /// </summary>
        public List<FactionData> GetNeutralFactions(FactionData faction)
        {
            if (faction == null) return new List<FactionData>();

            return allFactions.Where(f => f != null && f != faction && faction.IsNeutral(f)).ToList();
        }

        /// <summary>
        /// Устанавливает отношение между двумя фракциями (двустороннее)
        /// </summary>
        public void SetRelationship(FactionData faction1, FactionData faction2, float value)
        {
            if (faction1 == null || faction2 == null) return;

            faction1.SetRelationship(faction2, value);
            
            if (autoSyncRelationships)
            {
                faction2.SetRelationship(faction1, value);
            }

            if (debugMode)
            {
                Debug.Log($"[FactionManager] {faction1.factionName} <-> {faction2.factionName} = {value:F2} ({faction1.GetRelationshipDescription(faction2)})");
            }
        }

        /// <summary>
        /// Изменяет отношение между двумя фракциями на указанное значение
        /// </summary>
        public void ModifyRelationship(FactionData faction1, FactionData faction2, float delta)
        {
            if (faction1 == null || faction2 == null) return;

            float current = faction1.GetRelationship(faction2);
            SetRelationship(faction1, faction2, current + delta);
        }

        /// <summary>
        /// Делает две фракции врагами (отношение = -1)
        /// </summary>
        public void MakeEnemies(FactionData faction1, FactionData faction2)
        {
            SetRelationship(faction1, faction2, -1f);
        }

        /// <summary>
        /// Делает две фракции союзниками (отношение = 1)
        /// </summary>
        public void MakeAllies(FactionData faction1, FactionData faction2)
        {
            SetRelationship(faction1, faction2, 1f);
        }

        /// <summary>
        /// Делает две фракции нейтральными (отношение = 0)
        /// </summary>
        public void MakeNeutral(FactionData faction1, FactionData faction2)
        {
            SetRelationship(faction1, faction2, 0f);
        }

        /// <summary>
        /// Выводит все отношения всех фракций в консоль
        /// </summary>
        [ContextMenu("Print All Relationships")]
        public void PrintAllRelationships()
        {
            Debug.Log("=== Faction Relationships ===");
            
            foreach (var faction in allFactions)
            {
                if (faction == null) continue;

                Debug.Log($"\n{faction.factionName}:");
                foreach (var rel in faction.relationships)
                {
                    if (rel.faction == null) continue;
                    Debug.Log($"  -> {rel.faction.factionName}: {rel.relationshipValue:F2} ({faction.GetRelationshipDescription(rel.faction)})");
                }
            }
            
            Debug.Log($"\nTotal factions: {allFactions.Count}");
        }

        /// <summary>
        /// Находит всех членов указанной фракции на сцене
        /// </summary>
        public List<FactionMember> FindMembersOfFaction(FactionData faction)
        {
            if (faction == null) return new List<FactionMember>();

            var allMembers = FindObjectsOfType<FactionMember>();
            return allMembers.Where(m => m.faction == faction).ToList();
        }

        /// <summary>
        /// Находит всех врагов указанной фракции на сцене
        /// </summary>
        public List<FactionMember> FindEnemiesOfFaction(FactionData faction)
        {
            if (faction == null) return new List<FactionMember>();

            var allMembers = FindObjectsOfType<FactionMember>();
            return allMembers.Where(m => m.faction != null && faction.IsHostile(m.faction)).ToList();
        }

        /// <summary>
        /// Находит всех союзников указанной фракции на сцене
        /// </summary>
        public List<FactionMember> FindAlliesOfFaction(FactionData faction)
        {
            if (faction == null) return new List<FactionMember>();

            var allMembers = FindObjectsOfType<FactionMember>();
            return allMembers.Where(m => m.faction != null && faction.IsAllied(m.faction)).ToList();
        }
    }
}
