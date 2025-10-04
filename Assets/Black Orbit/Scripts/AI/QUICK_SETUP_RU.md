# Быстрая настройка AI - Шпаргалка

## 0. Автоматическая генерация (НОВОЕ!)

### Вариант A: Готовый префаб (самый быстрый!)
1. **Tools → AI → Generate Prefabs**
2. Выберите тип бота (Tactical Shooter, Sniper, Squad и т.д.)
3. Префаб создан в `GameData/AI/Prefabs/`!
4. Перетащите на сцену → Готово! ⚡

### Вариант B: Только действия
1. **Tools → AI → Generate Action Assets**
2. Нажмите **"Создать все действия"**
3. Действия созданы в `GameData/AI/Actions/`
4. Настройте AI вручную (см. раздел 1)

**Или создайте вручную:**
- Create → AI/Actions → выбрать нужное
- Настроить факторы по таблице ниже

---

## 1. Настройка AI (на префабе)

```
AI Component:
├─ Faction: Enemy (или Player/Wildlife)
├─ Hostile Factions: [Player] (список враждебных фракций)
├─ Allied Factions: [] (список союзных фракций)
├─ Target: [автоматически находится по фракциям]
├─ Move Speed: 3-5
├─ Detection Range: 10-15
├─ Attack Range: 2-3
├─ Vision Angle: 110-140
├─ Vision Range: 15-20 (≥ Detection Range)
├─ Obstacle Mask: [Obstacle] (БЕЗ Player!)
├─ Cover Mask: [Cover]
├─ Health: 100
├─ Patrol Points: [опционально]
└─ Actions: [массив ScriptableObject'ов]
```

## 2. Рекомендуемые факторы для действий

### 🚶 PatrolAction
```
[0] notSeeingPlayer
    Weight: 0.7
    Curve: Linear

[1] timeSinceSeen
    Weight: 0.3
    Curve: S-образная (0,0 → 0.2,0.1 → 0.8,0.9 → 1,1)
```

### 🔍 ExploreAction
```
[0] idle
    Weight: 0.7
    Curve: Linear

[1] health
    Weight: 0.3
    Curve: Linear
```

### 🔎 SearchLastKnownAction
```
[0] seenRecently
    Weight: 0.7
    Curve: Выпуклая (0,0 → 0.3,0.8 → 1,1)

[1] noLOS
    Weight: 0.3
    Curve: Linear
```

### 🏃 PursueAction
```
[0] outOfRange
    Weight: 0.6
    Curve: Linear

[1] hasLOS
    Weight: 0.4
    Curve: Linear
```

### 🔄 FlankAction
```
[0] hasLOS
    Weight: 0.6
    Curve: Linear

[1] health
    Weight: 0.4
    Curve: Linear
```

### 🛡️ TakeCoverAction
```
[0] lowHealth
    Weight: 0.5
    Curve: Выпуклая (0,0 → 0.2,0.6 → 0.5,0.9 → 1,1)

[1] underThreat
    Weight: 0.3
    Curve: Linear

[2] closeToPlayer
    Weight: 0.2
    Curve: Linear
```

### 🔫 RangedAttackAction
```
[0] inPreferred
    Weight: 0.5
    Curve: Колокол (0,0 → 0.5,1 → 1,0)

[1] hasLOS
    Weight: 0.3
    Curve: Linear

[2] health
    Weight: 0.2
    Curve: Linear
```

### ⚔️ MeleeAttackAction
```
[0] close
    Weight: 0.7
    Curve: Выпуклая (0,0 → 0.3,0.7 → 1,1)

[1] hasLOS
    Weight: 0.3
    Curve: Linear
```

### 🏃‍♂️ RetreatAction
```
[0] lowHealth
    Weight: 0.6
    Curve: Выпуклая (0,0 → 0.3,0.8 → 1,1)

[1] nearEnemy
    Weight: 0.4
    Curve: Linear
```

---

## 3. Типы кривых (AnimationCurve)

### Linear (Линейная)
```
Точки: (0, 0) → (1, 1)
Использование: прямая зависимость
```

### S-образная (Sigmoid)
```
Точки: (0, 0) → (0.2, 0.1) → (0.8, 0.9) → (1, 1)
Использование: пороговое поведение (медленно → резко → медленно)
```

### Выпуклая (Exponential)
```
Точки: (0, 0) → (0.3, 0.7) → (1, 1)
Использование: быстрый рост в начале (критические ситуации)
```

### Колокол (Bell)
```
Точки: (0, 0) → (0.5, 1) → (1, 0)
Использование: оптимум в середине (идеальная дистанция)
```

### Вогнутая (Logarithmic)
```
Точки: (0, 0) → (0.7, 0.3) → (1, 1)
Использование: медленный рост (постепенное усиление)
```

---

## 4. Чек-лист перед запуском

- [ ] NavMesh запечён (Window → AI → Navigation → Bake)
- [ ] Враг стоит на NavMesh (синяя зона в Scene View)
- [ ] NavMeshAgent добавлен на врага
- [ ] Слой игрока НЕ в Obstacle Mask
- [ ] Укрытия на слое Cover
- [ ] Все ScriptableObject действия созданы
- [ ] Факторы настроены для каждого действия
- [ ] Действия добавлены в массив Actions на AI
- [ ] Фракции настроены (Faction, Hostile Factions, Allied Factions)
- [ ] **AIWeaponHandler добавлен (для дальней атаки)**
- [ ] **Weapon Data назначен в AIWeaponHandler**

---

## 5. Быстрые пресеты

### Агрессивный боец
```
Действия: Pursue, MeleeAttack, RangedAttack, Flank
Веса атак: 0.7-0.8
Vision Angle: 140°
Vision Range: 20м
```

### Тактический стрелок
```
Действия: Patrol, Pursue, RangedAttack, TakeCover, Flank
Веса TakeCover: 0.6-0.7
Vision Angle: 120°
Vision Range: 18м
```

### Разведчик/Патруль
```
Действия: Patrol, Explore, SearchLastKnown, Pursue, Retreat
Веса Patrol/Explore: 0.7-0.8
Vision Angle: 90°
Vision Range: 15м
```

### Берсерк (ближний бой)
```
Действия: Pursue, MeleeAttack, Flank
Веса MeleeAttack: 0.8-0.9
Vision Angle: 160°
Vision Range: 12м
Move Speed: 6-8
```

---

## 6. Отладка

### Включить логи выбора действий
Уже включено в `AI.cs`:
```csharp
Debug.Log($"🤖 [{faction}] Выбрано действие: {_currentAction.name} (Score={bestScore:F2})");
```

### Визуализация обзора (добавьте в AI)
```csharp
void OnDrawGizmosSelected()
{
    // Дальность обзора
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, visionRange);
    
    // Угол обзора
    Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward * visionRange;
    Vector3 rightBoundary = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward * visionRange;
    Gizmos.color = Color.cyan;
    Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
    Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    
    // Последняя позиция игрока
    if (lastSeenPlayerPos != Vector3.zero)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastSeenPlayerPos, 0.5f);
    }
}
```

---

## 7. Частые проблемы

| Проблема | Решение |
|----------|---------|
| Бот не видит игрока | Убрать Player из Obstacle Mask |
| Бот не двигается | Проверить NavMesh Bake |
| Бот не находит укрытие | Добавить Collider на укрытия, проверить Cover Mask |
| Бот застревает в одном действии | Перебалансировать веса факторов |
| Бот слишком агрессивный | Увеличить веса TakeCover/Retreat |
| Бот слишком пассивный | Увеличить веса атакующих действий |

---

## 8. Порядок создания (3 минуты с генератором!)

### С автогенератором (рекомендуется):

1. **Создать все действия** (10 сек)
   - Tools → AI → Generate Action Assets → "Создать все действия"
   
2. **Настроить AI** (1 мин)
   - Заполнить поля из раздела 1
   - Настроить фракции (Faction, Hostile Factions)
   - Перетащить `.asset` файлы из `GameData/AI/Actions/` в Actions
   
3. **Настроить оружие** (30 сек)
   - Добавить компонент `AIWeaponHandler`
   - Назначить `Weapon Data` (WeaponScriptableObject)
   
4. **Запечь NavMesh** (30 сек)
   - Window → AI → Navigation → Bake
   
5. **Тест** (30 сек)
   - Запустить игру и проверить логи

**Готово! 🎉**

### Без генератора (старый способ):

1. **Создать ScriptableObjects** (1 мин)
   - Create → AI/Actions → выбрать все нужные
   
2. **Настроить факторы** (2 мин)
   - Скопировать значения из раздела 2 этой шпаргалки
   
3. Далее шаги 2-5 как выше

---

## 9. Интеграция с WeaponSystem

### Настройка стрельбы AI:

```
AIWeaponHandler Component:
├─ Weapon Data: [ваш WeaponScriptableObject]
├─ Muzzle Point: [опционально, создастся автоматически]
└─ Auto Initialize: ✓
```

`RangedAttackAction` автоматически найдёт `AIWeaponHandler` и будет стрелять через `WeaponSystem`!

### Что происходит под капотом:
1. `AIWeaponHandler` создаёт `StandardWeapon` при старте
2. Инициализирует его вашими данными оружия
3. `RangedAttackAction` вызывает `weaponHandler.TryFire()`
4. Система оружия обрабатывает стрельбу, разброс, боеприпасы, звуки

### Без AIWeaponHandler:
Если не добавите `AIWeaponHandler`, AI будет логировать "🔫 Дальняя атака (оружие не найдено)" вместо стрельбы.
