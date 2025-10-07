# Система Squad (Отрядов) для AI

## Обзор

Система squad позволяет AI координировать свои действия в группе, как в F.E.A.R. Отряд автоматически назначает роли членам и приказывает им выполнять конкретные действия для тактического преимущества.

## Ключевые возможности

✅ **Автоматическое назначение ролей** — Rusher, Flanker, Suppressor, Defender  
✅ **Координация атак** — члены отряда работают вместе  
✅ **Приказы действий** — squad может переопределить Utility AI  
✅ **Общая цель** — отряд отслеживает общую цель  
✅ **Гибкая настройка** — можно управлять вручную или автоматически  

---

## Быстрая настройка

### Шаг 1: Создать объект Squad

1. Создайте пустой GameObject на сцене
2. Назовите его "EnemySquad" или "Squad Alpha"
3. Добавьте компонент **AISquad**

### Шаг 2: Настроить параметры

```
AISquad Component:
├─ Squad Name: "Squad Alpha"
├─ Members: [] (заполнится автоматически или вручную)
├─ Leader: null (выбирается автоматически)
├─ Auto Find Members: ✓
├─ Search Radius: 20м
├─ Coordination Interval: 0.5 сек
└─ Min Members For Coordination: 2
```

### Шаг 3: Добавить AI в отряд

**Автоматически:**
- Включите `Auto Find Members`
- Все AI в радиусе `Search Radius` будут добавлены автоматически

**Вручную:**
- Перетащите AI в массив `Members`
- Или используйте код: `squad.AddMember(ai);`

### Шаг 4: Готово!

Отряд автоматически начнёт координировать действия членов.

---

## Роли в отряде

### 🏃 Rusher (Штурмовик)
- **Задача:** Агрессивно атаковать цель
- **Действие:** Pursue → сближается с целью
- **Назначается:** Ближайшему к цели

### 🔄 Flanker (Фланкёр)
- **Задача:** Обходить цель сбоку/с тыла
- **Действие:** Flank → обходной манёвр
- **Назначается:** Среднему по дистанции

### 🔫 Suppressor (Подавление)
- **Задача:** Стрелять с дистанции, прикрывать союзников
- **Действие:** RangedAttack или SuppressionFire
- **Назначается:** Дальнему от цели

### 🛡️ Defender (Защитник)
- **Задача:** Держать позицию, защищать укрытие
- **Действие:** TakeCover → прячется за укрытием
- **Назначается:** Вручную через код

---

## Как это работает

### Автоматическая координация

Каждые `coordinationInterval` секунд (по умолчанию 0.5 сек):

1. **Обновление цели** — находит общую цель отряда
2. **Назначение ролей** — сортирует членов по дистанции до цели
3. **Отдача приказов** — каждому члену приказывается действие на основе роли

```csharp
// Пример автоматической координации
void CoordinateSquad()
{
    // Ближайший штурмует
    AssignRole(closest, SquadRole.Rusher);
    closest.OrderAction("Pursue");
    
    // Средний фланкует
    AssignRole(middle, SquadRole.Flanker);
    middle.OrderAction("Flank");
    
    // Дальний подавляет огнем
    AssignRole(farthest, SquadRole.Suppressor);
    farthest.OrderAction("RangedAttack");
}
```

### Подавление и новые приказы (НОВОЕ)

#### Распределение подавления внутри отряда
```csharp
// Кто-то получает урон → сообщает в отряд
squad.ReportSuppression(sourceAI, level: 0.3f);
// Остальные члены получают часть подавления, повышая склонность к укрытию
```

#### Фланговый приказ
```csharp
// Выдать приказ фланга вокруг позиции цели
squad.OrderFlank(targetPos, duration: 3f, maxFlankers: 2);
```

#### Комбинированный приказ: подавление + фланг
```csharp
// Часть бойцов подавляет указанную позицию, часть флангует
squad.OrderSuppressAt(targetPos, duration: 4f, suppressors: 2, flankers: 1);
```

#### Автоповедение при высоком подавлении
- В координации, если среднее подавление отряда высокое, часть ролей конвертируется в Flanker.

### Система приказов

Когда squad отдаёт приказ через `ai.OrderAction()`:
- AI **игнорирует** Utility AI на время приказа
- Выполняет **только приказанное действие**
- После истечения времени возвращается к обычному поведению

```csharp
// Приказ на 5 секунд
ai.OrderAction("Flank", duration: 5f);

// Отменить приказ досрочно
ai.CancelOrder();

// Проверить, выполняет ли приказ
if (ai.IsFollowingOrder)
{
    Debug.Log("AI выполняет приказ");
}
```

---

## API для управления squad

### Управление членами

```csharp
// Добавить члена
squad.AddMember(ai);

// Удалить члена
squad.RemoveMember(ai);

// Получить количество живых
int alive = squad.AliveCount;
```

### Приказы отряду

```csharp
// Приказать всему отряду
squad.OrderSquad("TakeCover", duration: 5f);

// Приказать конкретному члену
ai.OrderAction("Pursue", duration: 3f);

// Отменить приказ
ai.CancelOrder();
```

### Получение информации

```csharp
// Получить роль члена
SquadRole role = squad.GetRole(ai);

// Получить всех с определённой ролью
List<AI> flankers = squad.GetMembersByRole(SquadRole.Flanker);

// Проверить видимость цели
bool canSee = squad.CanAnyoneSeeTarget();

// Получить цель отряда
Transform target = squad.SquadTarget;

// Последняя известная позиция цели
Vector3 lastPos = squad.LastKnownTargetPos;
```

---

## Примеры использования

### Пример 1: Координированная атака

```csharp
// В вашем коде (например, при обнаружении игрока)
void OnPlayerDetected()
{
    // Отряд автоматически назначит роли и начнёт атаку
    // Ничего делать не нужно — всё работает автоматически!
}
```

### Пример 2: Ручное управление

```csharp
public class CustomSquadController : MonoBehaviour
{
    public AISquad squad;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Приказать всем атаковать
            squad.OrderSquad("Pursue", 10f);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Приказать всем отступить
            squad.OrderSquad("Retreat", 5f);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Приказать всем укрыться
            squad.OrderSquad("TakeCover", 8f);
        }
    }
}
```

### Пример 3: Динамическое назначение ролей

```csharp
public class AdvancedSquadTactics : MonoBehaviour
{
    public AISquad squad;
    
    void AssignCustomRoles()
    {
        var members = squad.members;
        
        // Снайпер — дальше всех
        var sniper = members.OrderByDescending(m => 
            Vector3.Distance(m.transform.position, squad.SquadTarget.position)
        ).First();
        sniper.OrderAction("RangedAttack", 999f); // Бесконечно
        
        // Штурмовики — ближайшие 2
        var rushers = members.OrderBy(m => 
            Vector3.Distance(m.transform.position, squad.SquadTarget.position)
        ).Take(2);
        foreach (var rusher in rushers)
        {
            rusher.OrderAction("Pursue", 10f);
        }
        
        // Остальные — фланкуют
        var flankers = members.Except(rushers).Except(new[] { sniper });
        foreach (var flanker in flankers)
        {
            flanker.OrderAction("Flank", 10f);
        }
    }
}
```

---

## Новое действие: SuppressionFire

Добавлено специальное действие для подавляющего огня:

### Описание
Бот стреляет по **последней известной позиции** игрока, даже если не видит его. Идеально для координации с союзниками.

### Параметры
- `maxTimeSinceSeen` — 3 сек (максимальное время с последнего обнаружения)
- `fireCooldown` — 0.4 сек (интервал между выстрелами)
- `minRange` — 5м (минимальная дистанция)

### Факторы
- [0] недавно видели цель, но сейчас нет LOS (0..1)
- [1] на подходящей дистанции (0..1)
- [2] есть союзники рядом (1.0 если есть)

### Настройка
1. Create → AI/Actions → SuppressionFire
2. Настройте факторы:
   - Factor 0: Weight 0.5, Linear curve
   - Factor 1: Weight 0.3, Linear curve
   - Factor 2: Weight 0.2, Linear curve
3. Добавьте в массив Actions на AI

---

## Советы по использованию

### Для реализма как в F.E.A.R.:

1. **Используйте минимум 3 AI в отряде**
   - Один подавляет, другие фланкуют
   
2. **Добавьте SuppressionFire в действия**
   - Создаёт иллюзию тактического мышления
   
3. **Настройте короткий coordinationInterval**
   - 0.3-0.5 сек для быстрой реакции
   
4. **Комбинируйте с голосовыми репликами**
   - "Обходи справа!" при назначении Flanker
   - "Подавляй огнем!" при назначении Suppressor

### Оптимизация:

- Увеличьте `coordinationInterval` до 1-2 сек для больших отрядов
- Используйте `minMembersForCoordination = 2` чтобы одиночные AI не тратили ресурсы
- Отключите `autoFindMembers` если члены отряда известны заранее

---

## Отладка

### Визуализация в Scene View

При выборе AISquad в иерархии:
- **Синие линии** — связи между членами отряда
- **Синяя сфера** — радиус поиска членов

### Логи в консоли

```
[AISquad] Squad Alpha: найдено 3 членов отряда
[AISquad] Enemy_01 назначен роль: Rusher
[AISquad] Enemy_02 назначен роль: Flanker
[AISquad] Enemy_03 назначен роль: Suppressor
📋 [Enemy] Получен приказ: Pursue на 3 сек
🎖️ [Enemy] Выполняю приказ: Pursue
```

### Проверка состояния

```csharp
// В Update или через Inspector
Debug.Log($"Alive: {squad.AliveCount}");
Debug.Log($"Target: {squad.SquadTarget?.name}");
Debug.Log($"Role: {squad.GetRole(ai)}");
Debug.Log($"Following order: {ai.IsFollowingOrder}");
```

---

## Расширение системы

### Добавление своей роли

```csharp
// В AISquad.cs добавьте в enum SquadRole
public enum SquadRole
{
    None,
    Rusher,
    Flanker,
    Suppressor,
    Defender,
    Sniper,      // Ваша роль
    Medic        // Ваша роль
}

// В GiveOrderBasedOnRole добавьте обработку
case SquadRole.Sniper:
    member.OrderAction("RangedAttack");
    // Можно добавить дополнительную логику
    break;
```

### Создание кастомной координации

```csharp
public class MyCustomSquad : AISquad
{
    protected override void CoordinateSquad()
    {
        // Ваша логика координации
        // Например, анализ позиции игрока и выбор лучшей тактики
    }
}
```

---

## Частые вопросы

### Q: Как отключить автоматическую координацию?
**A:** Установите `coordinationInterval = 999f` или `minMembersForCoordination = 999`.

### Q: Можно ли иметь несколько squad на сцене?
**A:** Да, каждый squad работает независимо. Создайте несколько объектов AISquad.

### Q: Как сделать, чтобы AI не выполнял приказы?
**A:** Не назначайте `squad` в компоненте AI, или вызовите `ai.CancelOrder()`.

### Q: Приказы не работают?
**A:** Проверьте, что название действия в `OrderAction("Pursue")` совпадает с именем ScriptableObject (например, "Pursue", "Flank").

### Q: Как сделать постоянный приказ?
**A:** Используйте большую длительность: `ai.OrderAction("Defend", 9999f)`.

---

## Итоги

✅ **Система squad готова к использованию**  
✅ **Автоматическая координация работает из коробки**  
✅ **Гибкое API для ручного управления**  
✅ **Новое действие SuppressionFire для тактики**  

**Теперь ваши AI работают как команда, как в F.E.A.R.! 🎯**

---

**Версия:** 2.1  
**Дата:** 04.10.2025  
**Автор:** Cascade AI Assistant
