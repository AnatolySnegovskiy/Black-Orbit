using Black_Orbit.Scripts.Faction.ScriptableObjects;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Black_Orbit.Scripts.Faction.Editor
{
    /// <summary>
    /// Editor для создания и управления фракциями.
    /// Позволяет создавать фракции, настраивать отношения и визуализировать связи.
    /// </summary>
    public class FactionEditor : EditorWindow
    {
        private const string FACTIONS_PATH = "Assets/Black Orbit/GameData/Factions";
        
        private List<FactionData> _allFactions = new List<FactionData>();
        private Vector2 _scrollPos;
        private FactionData _selectedFaction;
        private string _newFactionName = "New Faction";
        private Color _newFactionColor = Color.white;
        
        // Визуализация
        private bool _showRelationshipMatrix = true;
        private Vector2 _matrixScrollPos;

        [MenuItem("Tools/Faction/Faction Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<FactionEditor>("Faction Editor");
            window.minSize = new Vector2(800, 600);
        }

        void OnEnable()
        {
            LoadAllFactions();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Левая панель - список фракций
            DrawFactionList();
            
            // Правая панель - детали фракции
            DrawFactionDetails();
            
            EditorGUILayout.EndHorizontal();
            
            // Нижняя панель - матрица отношений
            if (_showRelationshipMatrix)
            {
                DrawRelationshipMatrix();
            }
        }

        void DrawFactionList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            
            GUILayout.Label("Фракции", EditorStyles.boldLabel);
            
            // Кнопки управления
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Обновить", GUILayout.Height(25)))
            {
                LoadAllFactions();
            }
            if (GUILayout.Button("Создать", GUILayout.Height(25)))
            {
                ShowCreateFactionDialog();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Список фракций
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            foreach (var faction in _allFactions)
            {
                if (faction == null) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                // Цветной индикатор
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = faction.factionColor;
                GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                GUI.backgroundColor = oldColor;
                
                // Кнопка выбора
                if (GUILayout.Button(faction.factionName, GUILayout.Height(25)))
                {
                    _selectedFaction = faction;
                    Selection.activeObject = faction;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            // Статистика
            EditorGUILayout.HelpBox($"Всего фракций: {_allFactions.Count}", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        void DrawFactionDetails()
        {
            EditorGUILayout.BeginVertical();
            
            if (_selectedFaction == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Выберите фракцию из списка", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.Label($"Фракция: {_selectedFaction.factionName}", EditorStyles.largeLabel);
                GUILayout.Space(10);
                
                // Основная информация
                EditorGUILayout.LabelField("Основная информация", EditorStyles.boldLabel);
                
                _selectedFaction.factionName = EditorGUILayout.TextField("Название:", _selectedFaction.factionName);
                _selectedFaction.factionColor = EditorGUILayout.ColorField("Цвет:", _selectedFaction.factionColor);
                _selectedFaction.icon = (Sprite)EditorGUILayout.ObjectField("Иконка:", _selectedFaction.icon, typeof(Sprite), false);
                
                GUILayout.Label("Описание:");
                _selectedFaction.description = EditorGUILayout.TextArea(_selectedFaction.description, GUILayout.Height(60));
                
                GUILayout.Space(10);
                
                // Отношения
                EditorGUILayout.LabelField("Отношения с другими фракциями", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Добавить отношение", GUILayout.Height(25)))
                {
                    ShowAddRelationshipDialog();
                }
                if (GUILayout.Button("Синхронизировать все", GUILayout.Height(25)))
                {
                    SyncAllRelationships();
                }
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                // Список отношений
                for (int i = 0; i < _selectedFaction.relationships.Count; i++)
                {
                    var rel = _selectedFaction.relationships[i];
                    if (rel.faction == null) continue;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Цветной индикатор
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = rel.faction.factionColor;
                    GUILayout.Box("", GUILayout.Width(15), GUILayout.Height(15));
                    GUI.backgroundColor = oldColor;
                    
                    // Название фракции
                    GUILayout.Label(rel.faction.factionName, GUILayout.Width(120));
                    
                    // Слайдер отношения
                    rel.relationshipValue = EditorGUILayout.Slider(rel.relationshipValue, -1f, 1f);
                    
                    // Описание
                    string desc = _selectedFaction.GetRelationshipDescription(rel.faction);
                    GUILayout.Label(desc, GUILayout.Width(100));
                    
                    // Кнопка удаления
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        _selectedFaction.relationships.RemoveAt(i);
                        break;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                GUILayout.Space(10);
                
                // Кнопки действий
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Сохранить изменения", GUILayout.Height(30)))
                {
                    SaveFaction(_selectedFaction);
                }
                if (GUILayout.Button("Удалить фракцию", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Удалить фракцию?", 
                        $"Вы уверены что хотите удалить {_selectedFaction.factionName}?", "Да", "Нет"))
                    {
                        DeleteFaction(_selectedFaction);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        void DrawRelationshipMatrix()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Матрица отношений", EditorStyles.boldLabel);
            
            if (_allFactions.Count == 0)
            {
                EditorGUILayout.HelpBox("Нет фракций для отображения", MessageType.Info);
                return;
            }
            
            _matrixScrollPos = EditorGUILayout.BeginScrollView(_matrixScrollPos, GUILayout.Height(200));
            
            // Заголовок
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(100)); // Пустая ячейка
            
            foreach (var faction in _allFactions)
            {
                if (faction == null) continue;
                
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = faction.factionColor;
                GUILayout.Box(faction.factionName.Substring(0, Mathf.Min(3, faction.factionName.Length)), 
                    GUILayout.Width(40), GUILayout.Height(20));
                GUI.backgroundColor = oldColor;
            }
            EditorGUILayout.EndHorizontal();
            
            // Строки
            foreach (var faction1 in _allFactions)
            {
                if (faction1 == null) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                // Название фракции
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = faction1.factionColor;
                GUILayout.Box(faction1.factionName, GUILayout.Width(100), GUILayout.Height(20));
                GUI.backgroundColor = oldColor;
                
                // Ячейки отношений
                foreach (var faction2 in _allFactions)
                {
                    if (faction2 == null) continue;
                    
                    float relationship = faction1.GetRelationship(faction2);
                    Color cellColor = GetRelationshipColor(relationship);
                    
                    oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = cellColor;
                    
                    string label = relationship == 1f ? "=" : relationship.ToString("F1");
                    GUILayout.Box(label, GUILayout.Width(40), GUILayout.Height(20));
                    
                    GUI.backgroundColor = oldColor;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            // Легенда
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Легенда:", EditorStyles.boldLabel);
            DrawLegendItem("Лютый враг", GetRelationshipColor(-1f));
            DrawLegendItem("Враг", GetRelationshipColor(-0.6f));
            DrawLegendItem("Нейтрал", GetRelationshipColor(0f));
            DrawLegendItem("Друг", GetRelationshipColor(0.6f));
            DrawLegendItem("Лучший друг", GetRelationshipColor(1f));
            EditorGUILayout.EndHorizontal();
        }

        void DrawLegendItem(string label, Color color)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Box("", GUILayout.Width(15), GUILayout.Height(15));
            GUI.backgroundColor = oldColor;
            GUILayout.Label(label, GUILayout.Width(80));
        }

        Color GetRelationshipColor(float relationship)
        {
            if (relationship == 1f) return Color.gray; // Сама с собой
            if (relationship <= -0.8f) return new Color(0.8f, 0f, 0f); // Тёмно-красный
            if (relationship <= -0.5f) return new Color(1f, 0.3f, 0.3f); // Красный
            if (relationship <= -0.3f) return new Color(1f, 0.7f, 0.3f); // Оранжевый
            if (relationship < 0.3f) return Color.white; // Белый
            if (relationship < 0.5f) return new Color(0.7f, 1f, 0.7f); // Светло-зелёный
            if (relationship < 0.8f) return new Color(0.3f, 1f, 0.3f); // Зелёный
            return new Color(0f, 0.8f, 0f); // Тёмно-зелёный
        }

        void ShowCreateFactionDialog()
        {
            _newFactionName = EditorUtility.DisplayDialog("Создать фракцию", 
                "Введите название новой фракции", "Создать", "Отмена") 
                ? _newFactionName : null;
            
            if (!string.IsNullOrEmpty(_newFactionName))
            {
                CreateFaction(_newFactionName);
            }
        }

        void ShowAddRelationshipDialog()
        {
            if (_selectedFaction == null) return;
            
            GenericMenu menu = new GenericMenu();
            
            foreach (var faction in _allFactions)
            {
                if (faction == null || faction == _selectedFaction) continue;
                
                // Проверяем, нет ли уже отношения
                bool hasRelationship = _selectedFaction.relationships.Any(r => r.faction == faction);
                
                if (!hasRelationship)
                {
                    menu.AddItem(new GUIContent(faction.factionName), false, () => 
                    {
                        _selectedFaction.relationships.Add(new FactionRelationship
                        {
                            faction = faction,
                            relationshipValue = 0f
                        });
                        SaveFaction(_selectedFaction);
                    });
                }
            }
            
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("Нет доступных фракций"));
            }
            
            menu.ShowAsContext();
        }

        void LoadAllFactions()
        {
            _allFactions.Clear();
            
            if (!Directory.Exists(FACTIONS_PATH))
            {
                Directory.CreateDirectory(FACTIONS_PATH);
                AssetDatabase.Refresh();
            }
            
            string[] guids = AssetDatabase.FindAssets("t:FactionData", new[] { FACTIONS_PATH });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FactionData faction = AssetDatabase.LoadAssetAtPath<FactionData>(path);
                if (faction != null)
                {
                    _allFactions.Add(faction);
                }
            }
            
            _allFactions = _allFactions.OrderBy(f => f.factionName).ToList();
        }

        void CreateFaction(string factionName)
        {
            var faction = ScriptableObject.CreateInstance<FactionData>();
            faction.factionName = factionName;
            faction.factionColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            
            string path = $"{FACTIONS_PATH}/{factionName}.asset";
            
            // Проверяем существование
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Ошибка", $"Фракция {factionName} уже существует!", "OK");
                return;
            }
            
            AssetDatabase.CreateAsset(faction, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            LoadAllFactions();
            _selectedFaction = faction;
            
            Debug.Log($"✅ Создана фракция: {factionName}");
        }

        void SaveFaction(FactionData faction)
        {
            EditorUtility.SetDirty(faction);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Фракция {faction.factionName} сохранена");
        }

        void DeleteFaction(FactionData faction)
        {
            string path = AssetDatabase.GetAssetPath(faction);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
            
            LoadAllFactions();
            _selectedFaction = null;
            
            Debug.Log($"✅ Фракция {faction.factionName} удалена");
        }

        void SyncAllRelationships()
        {
            if (_selectedFaction == null) return;
            
            foreach (var rel in _selectedFaction.relationships)
            {
                if (rel.faction == null) continue;
                rel.faction.SetRelationship(_selectedFaction, rel.relationshipValue);
                EditorUtility.SetDirty(rel.faction);
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Отношения синхронизированы для {_selectedFaction.factionName}");
        }
    }
}
