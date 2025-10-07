### 📤 PeekAndShoot
```
Параметры:
- Peek Duration: 0.9 сек
- Hide Duration: 1.2 сек
- Peek Turn Speed: 10

Факторы:
[0] Есть цель (Weight: 0.5, Linear)
[1] Нет LOS (нужно выглянуть) (Weight: 0.3, Linear)
[2] Здоровье (Weight: 0.2, Linear)
```
# Готовые настройки AI Actions

## Автоматическая генерация

Все действия можно создать автоматически с правильно настроенными факторами!

### Способ 1: Через меню Unity (рекомендуется)

1. В Unity Editor откройте меню: **Tools → AI → Generate Action Assets**
2. Нажмите кнопку **"Создать все действия"**
3. Готово! Все действия созданы в `Assets/Black Orbit/GameData/AI/Actions/`

### Способ 2: Создать отдельное действие

В окне генератора можно создать любое действие по отдельности:
- Patrol
- Explore
- SearchLastKnown
- Pursue
- Flank
- TakeCover
- RangedAttack
- MeleeAttack
- Retreat
- SuppressionFire
- PeekAndShoot

---

## Созданные действия

После генерации вы получите следующие файлы:

### 📁 Actions/
- `Patrol.asset` — патрулирование
- `Explore.asset` — исследование
- `SearchLastKnown.asset` — поиск на последней позиции
- `Pursue.asset` — преследование
- `Flank.asset` — фланговый манёвр
- `TakeCover.asset` — поиск укрытия
- `RangedAttack.asset` — дальняя атака
- `MeleeAttack.asset` — ближняя атака
- `Retreat.asset` — отступление
- `SuppressionFire.asset` — подавляющий огонь
- `PeekAndShoot.asset` — выглянуть и выстрелить (peek/hide из укрытия)

---

## Предустановленные настройки

### 🚶 Patrol
```
Параметры:
- Waypoint Tolerance: 0.6м
- Wander Radius: 10м
- Repath Interval: 1.0 сек

Факторы:
[0] Не видит игрока (Weight: 0.7, Linear)
[1] Время с последнего обнаружения (Weight: 0.3, S-Curve)
```

### 🔍 Explore
```
Параметры:
- Radius: 15м
- Repath Interval: 1.2 сек

Факторы:
[0] Режим ожидания (Weight: 0.7, Linear)
[1] Уровень здоровья (Weight: 0.3, Linear)
```

### 🔎 SearchLastKnown
```
Параметры:
- Tolerance: 0.7м
- Scan Time: 3 сек

Факторы:
[0] Видели недавно (Weight: 0.7, Convex)
[1] Нет прямой видимости (Weight: 0.3, Linear)
```

### 🏃 Pursue
```
Параметры:
- Desired Range: 4м
- Repath Interval: 0.2 сек

Факторы:
[0] Вне желаемой дистанции (Weight: 0.6, Linear)
[1] Есть прямая видимость (Weight: 0.4, Linear)
```

### 🔄 Flank
```
Параметры:
- Flank Distance: 5м
- Repath Interval: 0.5 сек
- Min Angle: 60°

Факторы:
[0] Есть прямая видимость (Weight: 0.6, Linear)
[1] Уровень здоровья (Weight: 0.4, Linear)
```

### 🛡️ TakeCover
```
Параметры:
- Search Radius: 12м
- Repath Interval: 0.4 сек

Факторы:
[0] Низкое здоровье (Weight: 0.5, Convex)
[1] Под угрозой (Weight: 0.3, Linear)
[2] Игрок близко (Weight: 0.2, Linear)
```

### 🔫 RangedAttack
```
Параметры:
- Preferred Range: 12м
- Min Range: 4м
- Fire Cooldown: 0.6 сек

Факторы:
[0] На предпочтительной дистанции (Weight: 0.5, Bell)
[1] Есть прямая видимость (Weight: 0.3, Linear)
[2] Уровень здоровья (Weight: 0.2, Linear)
```

### ⚔️ MeleeAttack
```
Параметры:
- Strike Range: 2.2м
- Swing Cooldown: 0.8 сек

Факторы:
[0] Игрок близко (Weight: 0.7, Convex)
[1] Есть прямая видимость (Weight: 0.3, Linear)
```

### 🏃‍♂️ Retreat
```
Факторы:
[0] Низкое здоровье (Weight: 0.6, Convex)
[1] Враг близко (Weight: 0.4, Linear)
```

### 🔥 SuppressionFire
```
Параметры:
- Max Time Since Seen: 3 сек
- Fire Cooldown: 0.4 сек
- Min Range: 5м

Факторы:
[0] Недавно видели, нет LOS (Weight: 0.5, Linear)
[1] На подходящей дистанции (Weight: 0.3, Linear)
[2] Есть союзники (Weight: 0.2, Linear)
```

---

## Использование

### Назначение на AI:

1. Выберите префаб AI
2. В компоненте **AI** → **Actions**
3. Установите размер массива (например, 8)
4. Перетащите нужные `.asset` файлы из `GameData/AI/Actions/`

### Рекомендуемый набор для врага:

```
Actions (Size: 9)
├─ [0] Patrol
├─ [1] Explore
├─ [2] SearchLastKnown
├─ [3] Pursue
├─ [4] Flank
├─ [5] TakeCover
├─ [6] RangedAttack
├─ [7] PeekAndShoot
└─ [8] SuppressionFire
```

### Для ближнего боя:

```
Actions (Size: 6)
├─ [0] Patrol
├─ [1] Explore
├─ [2] Pursue
├─ [3] Flank
├─ [4] MeleeAttack
└─ [5] Retreat
```

---

## Типы кривых

### Linear (Линейная)
Прямая зависимость от 0 до 1

### S-Curve (S-образная)
Медленный старт → резкий рост → медленное завершение  
Используется для порогового поведения

### Convex (Выпуклая)
Быстрый рост в начале → замедление  
Используется для критических ситуаций (низкое HP)

### Bell (Колокол)
Пик в середине (0.5), спад по краям  
Используется для оптимальной дистанции

---

## Настройка

Все созданные действия можно дополнительно настроить:

1. Откройте `.asset` файл в Inspector
2. Измените параметры действия
3. Настройте веса факторов (0-1)
4. Отредактируйте кривые (двойной клик на curve)

---

## Пересоздание

Если нужно пересоздать действия с дефолтными настройками:

1. **Tools → AI → Generate Action Assets**
2. Нажмите на конкретное действие
3. Подтвердите перезапись

---

## Частые вопросы

### Q: Где находятся созданные файлы?
**A:** `Assets/Black Orbit/GameData/AI/Actions/`

### Q: Можно ли изменить настройки после создания?
**A:** Да, просто откройте `.asset` файл в Inspector и измените параметры.

### Q: Что делать, если генератор не появляется в меню?
**A:** Убедитесь, что `AIActionAssetGenerator.cs` находится в папке `Editor/`.

### Q: Нужно ли пересоздавать при обновлении скриптов?
**A:** Нет, `.asset` файлы сохраняют свои настройки независимо от кода.

---

**Готово! Теперь у вас есть все настроенные действия для AI! 🎯**
