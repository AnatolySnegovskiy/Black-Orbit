# Руководство по поведению AI на основе отношений

## Градации отношений и поведение

### Таблица поведения

| Отношение | Описание | Поведение AI |
|-----------|----------|--------------|
| **<= -0.8** | Лютый враг | Атакует всегда, без пощады. Максимальная агрессия |
| **-0.8 ... -0.5** | Враг | Атакует при виде. Высокая агрессия |
| **-0.5 ... -0.3** | Недружелюбный | Может атаковать при провокации. Настороженность |
| **-0.3 ... 0.3** | Нейтральный | Игнорирует. Нет агрессии |
| **0.3 ... 0.5** | Дружелюбный | Не атакует. Может помочь при просьбе |
| **0.5 ... 0.8** | Друг | Может помочь (70% шанс). Защищает |
| **>= 0.8** | Лучший друг | Всегда помогает. Активная защита |

---

## API для проверки поведения

### Базовые проверки

```csharp
// Враждебный? (< -0.3)
bool isHostile = ai.IsHostile(otherAI);

// Союзный? (> 0.3)
bool isAllied = ai.IsAllied(otherAI);

// Получить точное отношение
float relationship = ai.GetRelationship(otherAI);
// Returns: -1.0 ... 1.0
```

### Расширенные проверки (новые методы)

```csharp
// Должен ли атаковать?
bool shouldAttack = ai.ShouldAttack(otherAI);
// true если relationship <= -0.5

// Должен ли помогать?
bool shouldHelp = ai.ShouldHelp(otherAI);
// true если relationship >= 0.8
// 70% шанс если >= 0.5

// Уровень агрессии (0 ... 1)
float aggression = ai.GetAggressionLevel(otherAI);
// -1.0 (лютый враг) -> 1.0 (максимальная агрессия)
// -0.5 (враг) -> 0.5 (средняя агрессия)
// 0.0 (нейтрал) -> 0.0 (нет агрессии)
```

---

## Примеры использования

### Пример 1: Условная атака

```csharp
public class CombatBehavior : MonoBehaviour
{
    public AI ai;
    
    void Update()
    {
        if (ai.target == null) return;
        
        var targetAI = ai.target.GetComponent<AI>();
        if (targetAI == null) return;
        
        float relationship = ai.GetRelationship(targetAI);
        
        if (relationship <= -0.8f)
        {
            // Лютый враг - атакуем без пощады
            AttackAggressively();
        }
        else if (relationship <= -0.5f)
        {
            // Враг - обычная атака
            AttackNormally();
        }
        else if (relationship <= -0.3f)
        {
            // Недружелюбный - только если провокация
            if (WasAttackedRecently())
            {
                AttackDefensively();
            }
        }
        else
        {
            // Нейтральный или дружелюбный - не атакуем
            StopAttacking();
        }
    }
}
```

### Пример 2: Система помощи

```csharp
public class AllySupport : MonoBehaviour
{
    public AI ai;
    public float supportRange = 15f;
    
    void Update()
    {
        // Ищем союзников в опасности
        var allies = ai.GetAlliesInRange(supportRange);
        
        foreach (var ally in allies)
        {
            if (ally.health < ally.maxHealth * 0.3f) // Союзник ранен
            {
                float relationship = ai.GetRelationship(ally);
                
                if (relationship >= 0.8f)
                {
                    // Лучший друг - всегда помогаем
                    HelpAlly(ally);
                }
                else if (relationship >= 0.5f)
                {
                    // Друг - помогаем с вероятностью
                    if (ai.ShouldHelp(ally))
                    {
                        HelpAlly(ally);
                    }
                }
            }
        }
    }
    
    void HelpAlly(AI ally)
    {
        // Логика помощи: прикрытие, лечение, подавляющий огонь
        Debug.Log($"Помогаю {ally.name}!");
    }
}
```

### Пример 3: Динамическая агрессия

```csharp
public class AdaptiveCombat : MonoBehaviour
{
    public AI ai;
    public float baseFireRate = 1f;
    
    void Update()
    {
        if (ai.target == null) return;
        
        var targetAI = ai.target.GetComponent<AI>();
        if (targetAI == null) return;
        
        // Получаем уровень агрессии
        float aggression = ai.GetAggressionLevel(targetAI);
        
        // Адаптируем поведение
        float fireRate = baseFireRate * (1f + aggression); // До 2x скорости
        float accuracy = 0.5f + (aggression * 0.5f);       // До 100% точности
        float retreatThreshold = 0.3f - (aggression * 0.2f); // Меньше отступает
        
        // Применяем
        SetFireRate(fireRate);
        SetAccuracy(accuracy);
        SetRetreatThreshold(retreatThreshold);
    }
}
```

### Пример 4: Squad координация с учётом отношений

```csharp
// AISquad автоматически проверяет отношения:
void CoordinateSquad()
{
    // Проверяем, что цель враждебна
    float relationship = member.GetRelationship(targetAI);
    
    if (relationship >= -0.3f)
    {
        // Цель не враждебна - не координируем атаку
        return;
    }
    
    // Цель враждебна - координируем
    AssignRoles();
    OrderActions();
}
```

---

## Рекомендации по балансу

### Для врагов:

```csharp
// Обычные враги
playerFaction.SetRelationship(enemyFaction, -0.6f); // Враг

// Элитные враги
playerFaction.SetRelationship(bossFaction, -1.0f); // Лютый враг

// Бандиты (могут стать нейтральными)
playerFaction.SetRelationship(banditFaction, -0.4f); // Недружелюбный
```

### Для союзников:

```csharp
// Случайные союзники
playerFaction.SetRelationship(merchantFaction, 0.4f); // Дружелюбный

// Постоянные союзники
playerFaction.SetRelationship(guardsFaction, 0.7f); // Друг

// Близкие друзья
playerFaction.SetRelationship(companionFaction, 0.9f); // Лучший друг
```

### Прогрессия репутации:

```csharp
// Малые действия
faction.IncreaseReputation(other, 0.05f); // +5%
faction.DecreaseReputation(other, 0.05f); // -5%

// Средние действия (квесты)
faction.IncreaseReputation(other, 0.2f);  // +20%
faction.DecreaseReputation(other, 0.2f);  // -20%

// Крупные действия (предательство, спасение)
faction.IncreaseReputation(other, 0.5f);  // +50%
faction.DecreaseReputation(other, 0.5f);  // -50%
```

---

## Частые сценарии

### Сценарий 1: От врага к союзнику

```csharp
// Начало: Враги
playerFaction.SetRelationship(guardsFaction, -0.6f);

// Квест 1: Помощь (+0.3)
playerFaction.IncreaseReputation(guardsFaction, 0.3f);
// Теперь: -0.3 (Недружелюбный)

// Квест 2: Спасение (+0.5)
playerFaction.IncreaseReputation(guardsFaction, 0.5f);
// Теперь: 0.2 (Нейтральный)

// Квест 3: Героизм (+0.4)
playerFaction.IncreaseReputation(guardsFaction, 0.4f);
// Теперь: 0.6 (Друг)
```

### Сценарий 2: Провокация нейтрального

```csharp
// Начало: Нейтральный
playerFaction.SetRelationship(wildlifeFaction, 0.0f);

// Игрок атаковал зверя
playerFaction.DecreaseReputation(wildlifeFaction, 0.2f);
// Теперь: -0.2 (всё ещё Нейтральный, но близко к Недружелюбному)

// Игрок убил зверя
playerFaction.DecreaseReputation(wildlifeFaction, 0.3f);
// Теперь: -0.5 (Враг - звери атакуют!)
```

### Сценарий 3: Предательство

```csharp
// Начало: Друг
playerFaction.SetRelationship(alliedFaction, 0.7f);

// Игрок предал
playerFaction.DecreaseReputation(alliedFaction, 1.5f);
// Теперь: -0.8 (Лютый враг - атакуют без пощады!)
```

---

## Итоги

✅ **7 градаций отношений** — от лютого врага до лучшего друга  
✅ **Гибкое поведение** — AI адаптируется к отношениям  
✅ **Простое API** — `ShouldAttack()`, `ShouldHelp()`, `GetAggressionLevel()`  
✅ **Автоматическая интеграция** — Squad учитывает отношения  
✅ **Плавная прогрессия** — репутация меняется постепенно  

**Система готова для создания сложных социальных взаимодействий! 🎯**

---

**Версия:** 3.0  
**Дата:** 04.10.2025
