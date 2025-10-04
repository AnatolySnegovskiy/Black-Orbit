using Black_Orbit.Scripts.Faction.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Black_Orbit.Scripts.Faction.Editor
{
    /// <summary>
    /// Шаблоны фракций для быстрого создания стандартных наборов.
    /// </summary>
    public class FactionTemplates : EditorWindow
    {
        private const string FACTIONS_PATH = "Assets/Black Orbit/GameData/Factions";

        [MenuItem("Tools/Faction/Create Default Factions")]
        public static void CreateDefaultFactions()
        {
            if (EditorUtility.DisplayDialog("Создать стандартные фракции?",
                "Будут созданы: Player, Enemy, Wildlife, Guards, Bandits, Civilians\n\nПродолжить?",
                "Да", "Нет"))
            {
                CreateStandardFactionSet();
            }
        }

        static void CreateStandardFactionSet()
        {
            // Создаём фракции
            var player = CreateFaction("Player", "Игрок и его союзники", new Color(0.2f, 0.5f, 1f));
            var enemy = CreateFaction("Enemy", "Основные враги игрока", new Color(1f, 0.2f, 0.2f));
            var wildlife = CreateFaction("Wildlife", "Дикие животные", new Color(0.6f, 0.4f, 0.2f));
            var guards = CreateFaction("Guards", "Городская стража", new Color(0.3f, 0.3f, 0.8f));
            var bandits = CreateFaction("Bandits", "Бандиты и разбойники", new Color(0.5f, 0.2f, 0.2f));
            var civilians = CreateFaction("Civilians", "Мирные жители", new Color(0.7f, 0.7f, 0.7f));

            // Настраиваем отношения
            
            // Player
            player.SetRelationship(enemy, -1.0f);      // Лютый враг
            player.SetRelationship(wildlife, 0.0f);    // Нейтрал
            player.SetRelationship(guards, 0.5f);      // Друг
            player.SetRelationship(bandits, -0.6f);    // Враг
            player.SetRelationship(civilians, 0.3f);   // Дружелюбный

            // Enemy
            enemy.SetRelationship(player, -1.0f);      // Лютый враг
            enemy.SetRelationship(wildlife, -0.5f);    // Враг
            enemy.SetRelationship(guards, -0.8f);      // Лютый враг
            enemy.SetRelationship(bandits, 0.2f);      // Нейтрал/Дружелюбный
            enemy.SetRelationship(civilians, -0.3f);   // Недружелюбный

            // Wildlife
            wildlife.SetRelationship(player, 0.0f);    // Нейтрал
            wildlife.SetRelationship(enemy, -0.5f);    // Враг
            wildlife.SetRelationship(guards, 0.0f);    // Нейтрал
            wildlife.SetRelationship(bandits, 0.0f);   // Нейтрал
            wildlife.SetRelationship(civilians, 0.0f); // Нейтрал

            // Guards
            guards.SetRelationship(player, 0.5f);      // Друг
            guards.SetRelationship(enemy, -0.8f);      // Лютый враг
            guards.SetRelationship(wildlife, 0.0f);    // Нейтрал
            guards.SetRelationship(bandits, -0.7f);    // Враг
            guards.SetRelationship(civilians, 0.8f);   // Лучший друг

            // Bandits
            bandits.SetRelationship(player, -0.6f);    // Враг
            bandits.SetRelationship(enemy, 0.2f);      // Нейтрал/Дружелюбный
            bandits.SetRelationship(wildlife, 0.0f);   // Нейтрал
            bandits.SetRelationship(guards, -0.7f);    // Враг
            bandits.SetRelationship(civilians, -0.4f); // Недружелюбный

            // Civilians
            civilians.SetRelationship(player, 0.3f);   // Дружелюбный
            civilians.SetRelationship(enemy, -0.3f);   // Недружелюбный
            civilians.SetRelationship(wildlife, 0.0f); // Нейтрал
            civilians.SetRelationship(guards, 0.8f);   // Лучший друг
            civilians.SetRelationship(bandits, -0.4f); // Недружелюбный

            // Сохраняем
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(enemy);
            EditorUtility.SetDirty(wildlife);
            EditorUtility.SetDirty(guards);
            EditorUtility.SetDirty(bandits);
            EditorUtility.SetDirty(civilians);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ Созданы стандартные фракции:");
            Debug.Log("  - Player (синий)");
            Debug.Log("  - Enemy (красный)");
            Debug.Log("  - Wildlife (коричневый)");
            Debug.Log("  - Guards (тёмно-синий)");
            Debug.Log("  - Bandits (тёмно-красный)");
            Debug.Log("  - Civilians (серый)");

            EditorUtility.DisplayDialog("Готово!", 
                "Стандартные фракции созданы в:\n" + FACTIONS_PATH, "OK");
        }

        static FactionData CreateFaction(string name, string description, Color color)
        {
            var faction = ScriptableObject.CreateInstance<FactionData>();
            faction.factionName = name;
            faction.description = description;
            faction.factionColor = color;

            string path = $"{FACTIONS_PATH}/{name}.asset";

            // Проверяем существование
            if (System.IO.File.Exists(path))
            {
                Debug.LogWarning($"⚠️ Фракция {name} уже существует, пропускаем");
                return AssetDatabase.LoadAssetAtPath<FactionData>(path);
            }

            // Создаём папку если не существует
            if (!AssetDatabase.IsValidFolder(FACTIONS_PATH))
            {
                string parentPath = "Assets/Black Orbit/GameData";
                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    AssetDatabase.CreateFolder("Assets/Black Orbit", "GameData");
                }
                AssetDatabase.CreateFolder(parentPath, "Factions");
            }

            AssetDatabase.CreateAsset(faction, path);
            return faction;
        }
    }
}
