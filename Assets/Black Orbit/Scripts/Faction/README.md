# Система Фракций (Faction System)

## Структура

```
FactionSystem/
├─ ScriptableObjects/
│  └─ FactionData.cs          - ScriptableObject фракции
├─ Runtime/
│  ├─ FactionMember.cs        - Компонент для объектов
│  └─ FactionManager.cs       - Singleton менеджер
└─ README.md                  - Документация
```

## Обзор

Полноценная система фракций на основе ScriptableObject с гибкими отношениями от -1 (лютый враг) до 1 (лучший друг).

## Ключевые особенности

✅ **ScriptableObject фракции** — создавайте неограниченное количество фракций  
✅ **Гибкие отношения** — значения от -1 до 1 вместо жёстких категорий  
✅ **Централизованное управление** — FactionManager для всех фракций  
✅ **Динамические изменения** — меняйте отношения в runtime  
✅ **Визуальная настройка** — цвет, иконка, описание для каждой фракции  

---

## Быстрый старт

### Шаг 1: Создайте фракции

1. **Create → Faction System → Faction**
2. Назовите: "Player", "Enemy", "Wildlife" и т.д.
3. Настройте параметры:
   - Faction Name
   - Description
   - Faction Color
   - Icon (опционально)

### Шаг 2: Настройте отношения

В Inspector фракции "Enemy":
```
Relationships:
├─ [0]
│   ├─ Faction: Player
│   └─ Relationship Value: -1.0 (лютый враг)
└─ [1]
    ├─ Faction: Wildlife
    └─ Relationship Value: -0.5 (враг)
```

### Шаг 3: Используйте на объектах

```csharp
// На игроке
player.AddComponent<FactionMember>().faction = playerFactionData;

// На враге (AI)
enemy.GetComponent<AI>().faction = enemyFactionData;
```

---

## Шкала отношений

```
-1.0 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 1.0
 │                    │                    │                    │
Лютый враг        Нейтрал            Друг            Лучший друг
```

### Пороговые значения:

| Значение | Описание | Поведение |
|----------|----------|-----------|
| **<= -0.8** | Лютый враг | Атакует всегда, без пощады |
| **-0.8 ... -0.5** | Враг | Атакует при виде |
| **-0.5 ... -0.3** | Недружелюбный | Может атаковать |
| **-0.3 ... 0.3** | Нейтральный | Игнорирует |
| **0.3 ... 0.5** | Дружелюбный | Не атакует |
| **0.5 ... 0.8** | Друг | Может помочь |
| **>= 0.8** | Лучший друг | Всегда помогает |

### Методы проверки:

```csharp
// Враждебный (< -0.3)
bool isHostile = faction.IsHostile(otherFaction);

// Союзный (> 0.3)
bool isAllied = faction.IsAllied(otherFaction);

// Нейтральный (-0.3 <= x <= 0.3)
bool isNeutral = faction.IsNeutral(otherFaction);
```

---

## Создание фракций

### Через Unity Editor:

1. **Create → Faction System → Faction**
2. Настройте параметры:

```
Player Faction:
├─ Faction Name: "Player"
├─ Description: "Игрок и его союзники"
├─ Faction Color: Blue
├─ Icon: [Player Icon]
└─ Relationships:
    ├─ Enemy: -1.0
    ├─ Wildlife: 0.0
    └─ Guards: 0.8
```

### Через код:

```csharp
// Создание через ScriptableObject.CreateInstance
var playerFaction = ScriptableObject.CreateInstance<FactionData>();
playerFaction.factionName = "Player";
playerFaction.factionColor = Color.blue;

// Настройка отношений
playerFaction.SetRelationship(enemyFaction, -1f); // Враги
playerFaction.SetRelationship(guardsFaction, 0.8f); // Друзья
```

---

## FactionData API

### Получение отношения:

```csharp
// Получить значение отношения
float rel = faction.GetRelationship(otherFaction);
// Returns: -1.0 ... 1.0

// Получить текстовое описание
string desc = faction.GetRelationshipDescription(otherFaction);
// Returns: "Лютый враг", "Друг", "Нейтральный" и т.д.
```

### Изменение отношений (репутации):

```csharp
// Установить отношение
faction.SetRelationship(otherFaction, -0.5f);

// Изменить на delta
faction.ModifyRelationship(otherFaction, +0.1f); // Улучшить
faction.ModifyRelationship(otherFaction, -0.2f); // Ухудшить

// Удобные алиасы для репутации:
faction.IncreaseReputation(otherFaction, 0.1f); // Улучшить репутацию
faction.DecreaseReputation(otherFaction, 0.2f); // Ухудшить репутацию
```

### Проверки:

```csharp
// Враждебный?
if (faction.IsHostile(otherFaction)) { /* атаковать */ }

// Союзный?
if (faction.IsAllied(otherFaction)) { /* помочь */ }

// Нейтральный?
if (faction.IsNeutral(otherFaction)) { /* игнорировать */ }
```

---

## FactionMember Component

Легкий компонент для объектов с фракцией.

### Использование:

```csharp
// Добавить на игрока
var fm = player.AddComponent<FactionMember>();
fm.faction = playerFactionData;

// Проверить отношение
float rel = fm.GetRelationship(enemyFactionData);
bool isHostile = fm.IsHostile(enemy.GetComponent<FactionMember>());
```

### API:

```csharp
// Получить отношение
float GetRelationship(FactionData otherFaction);
float GetRelationship(FactionMember other);

// Проверки
bool IsHostile(FactionData otherFaction);
bool IsHostile(FactionMember other);
bool IsAllied(FactionData otherFaction);
bool IsAllied(FactionMember other);
bool IsNeutral(FactionData otherFaction);
bool IsNeutral(FactionMember other);

// Описание
string GetRelationshipDescription(FactionData otherFaction);
```

---

## FactionManager

Singleton для управления всеми фракциями.

### Настройка:

1. Создайте GameObject "FactionManager"
2. Add Component → Faction Manager
3. Добавьте все фракции в список "All Factions"
4. Включите "Auto Sync Relationships" для двусторонних отношений

### API:

```csharp
// Получить фракцию по имени
FactionData faction = FactionManager.Instance.GetFaction("Player");

// Получить списки
List<FactionData> enemies = FactionManager.Instance.GetHostileFactions(playerFaction);
List<FactionData> allies = FactionManager.Instance.GetAlliedFactions(playerFaction);

// Изменить отношения
FactionManager.Instance.SetRelationship(faction1, faction2, -0.8f);
FactionManager.Instance.MakeEnemies(faction1, faction2); // -1.0
FactionManager.Instance.MakeAllies(faction1, faction2);  // 1.0
FactionManager.Instance.MakeNeutral(faction1, faction2); // 0.0

// Найти объекты на сцене
List<FactionMember> enemies = FactionManager.Instance.FindEnemiesOfFaction(playerFaction);
List<FactionMember> allies = FactionManager.Instance.FindAlliesOfFaction(playerFaction);
```

---

## Примеры использования

### Пример 1: Базовая настройка

```csharp
// Создайте 3 фракции: Player, Enemy, Wildlife

// В Player.asset:
Relationships:
- Enemy: -1.0 (лютый враг)
- Wildlife: 0.0 (нейтрал)

// В Enemy.asset:
Relationships:
- Player: -1.0 (лютый враг)
- Wildlife: -0.5 (враг)

// В Wildlife.asset:
Relationships:
- Player: 0.0 (нейтрал)
- Enemy: -0.5 (враг)
```

### Пример 2: Система репутации (квесты)

```csharp
public class QuestManager : MonoBehaviour
{
    public FactionData playerFaction;
    public FactionData guardsFaction;
    
    void OnQuestCompleted()
    {
        // Игрок помог стражникам - улучшаем репутацию
        playerFaction.IncreaseReputation(guardsFaction, 0.3f);
        
        Debug.Log($"Отношение: {playerFaction.GetRelationshipDescription(guardsFaction)}");
        // "Дружелюбный" или "Друг"
    }
    
    void OnPlayerBetray()
    {
        // Игрок предал стражников - резко ухудшаем
        playerFaction.DecreaseReputation(guardsFaction, 1.0f);
        
        Debug.Log("Стража теперь ваш враг!");
    }
    
    void OnKillGuard()
    {
        // Убийство стражника - небольшое ухудшение
        playerFaction.DecreaseReputation(guardsFaction, 0.1f);
    }
}
```

### Пример 3: Сложная репутационная система

```csharp
public class ReputationSystem : MonoBehaviour
{
    public FactionData playerFaction;
    
    public void OnKillEnemy(FactionData enemyFaction)
    {
        // Убийство врага улучшает отношения с их врагами
        var enemiesOfEnemy = FactionManager.Instance.GetHostileFactions(enemyFaction);
        
        foreach (var faction in enemiesOfEnemy)
        {
            playerFaction.IncreaseReputation(faction, 0.05f);
            Debug.Log($"{faction.factionName}: {playerFaction.GetRelationship(faction):F2}");
        }
    }
    
    public void OnKillCivilian(FactionData civilianFaction)
    {
        // Убийство мирного ухудшает отношения со всеми
        var allFactions = FactionManager.Instance.allFactions;
        
        foreach (var faction in allFactions)
        {
            if (faction == playerFaction) continue;
            playerFaction.DecreaseReputation(faction, 0.1f);
        }
    }
    
    public void OnHelpFaction(FactionData helpedFaction)
    {
        // Помощь фракции улучшает отношения
        playerFaction.IncreaseReputation(helpedFaction, 0.2f);
        
        // И немного улучшает с их союзниками
        var allies = FactionManager.Instance.GetAlliedFactions(helpedFaction);
        foreach (var ally in allies)
        {
            playerFaction.IncreaseReputation(ally, 0.05f);
        }
    }
}
```

---

## Интеграция с AI

AI автоматически использует новую систему фракций:

```csharp
// AI.cs автоматически:
// 1. Ищет враждебные фракции через FactionManager
// 2. Проверяет отношения через FactionData.IsHostile()
// 3. Находит союзников через FactionData.IsAllied()

// Ничего настраивать не нужно!
```

---

## Миграция со старой системы

### Было (enum Faction):
```csharp
public Faction faction = Faction.Enemy;
public List<Faction> hostileFactions = new List<Faction> { Faction.Player };
```

### Стало (ScriptableObject):
```csharp
public FactionData faction; // Ссылка на Enemy.asset
// hostileFactions больше не нужно - настраивается в FactionData
```

### Шаги миграции:

1. Создайте FactionData для каждой старой фракции
2. Настройте отношения в FactionData
3. Замените ссылки на enum на ссылки на ScriptableObject
4. Удалите старые поля hostileFactions/alliedFactions

---

## Частые вопросы

### Q: Сколько фракций можно создать?
**A:** Неограниченное количество! Создавайте сколько нужно через Create → Faction System → Faction.

### Q: Как сделать асимметричные отношения?
**A:** Отключите "Auto Sync Relationships" в FactionManager и настройте вручную.

### Q: Можно ли изменять отношения в runtime?
**A:** Да! Используйте `SetRelationship()` или `ModifyRelationship()`.

### Q: Как сделать временное перемирие?
**A:**
```csharp
float oldRel = faction1.GetRelationship(faction2);
faction1.SetRelationship(faction2, 0f); // Нейтрал
// Через время:
faction1.SetRelationship(faction2, oldRel); // Вернуть
```

### Q: Где хранятся фракции?
**A:** Рекомендуется: `Assets/Black Orbit/GameData/Factions/`

---

## Итоги

✅ **ScriptableObject** — неограниченное количество фракций  
✅ **Гибкие отношения** — от -1 до 1 вместо категорий  
✅ **Централизованное управление** — FactionManager  
✅ **Динамические изменения** — квесты, репутация, события  
✅ **Легкая интеграция** — AI использует автоматически  

**Система фракций готова к использованию! 🎯**

---

**Версия:** 3.0  
**Дата:** 04.10.2025
