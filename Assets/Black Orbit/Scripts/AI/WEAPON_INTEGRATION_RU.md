# Интеграция WeaponSystem с AI

## Быстрый старт

### 1. Добавьте компонент на AI

На префабе AI добавьте компонент **`AIWeaponHandler`**:
- Add Component → Scripts → AI → Runtime → AI Weapon Handler

### 2. Настройте параметры

```
AIWeaponHandler:
├─ Weapon Data: [перетащите WeaponScriptableObject]
├─ Muzzle Point: [опционально, создастся автоматически]
└─ Auto Initialize: ✓ (оставьте включённым)

**Weapon Data** — это ваш существующий ScriptableObject оружия (например, `AssaultRifle`, `Pistol` и т.д.)

### 3. Готово!

`RangedAttackAction` автоматически найдёт `AIWeaponHandler` и будет стрелять через `WeaponSystem`!

---

## Интеграция с PeekAndShoot

### ReleaseTrigger
### Архитектура

```
AI
  └─ RangedAttackAction (ScriptableObject)
       └─ Ищет AIWeaponHandler на AI
            └─ AIWeaponHandler
                 └─ Создаёт StandardWeapon
                      └─ Использует ваш WeaponScriptableObject
```

### Последовательность вызовов

1. **При старте игры:**
   ```csharp
   AIWeaponHandler.Start()
   └─ InitializeWeapon()
       ├─ Создаёт MuzzlePoint (если не задан)
       ├─ Добавляет StandardWeapon на AI
       └─ Инициализирует weapon.Initialize(weaponData, muzzlePoint)
   ```

2. **Во время боя:**
   ```csharp
   RangedAttackAction.Execute()
   └─ ai.TryGetComponent<AIWeaponHandler>()
       └─ weaponHandler.TryFire()
           └─ weapon.TryFire() // Ваша WeaponSystem
   ```

---

## Настройка Muzzle Point

### Автоматическое создание (рекомендуется)

Если не назначите `Muzzle Point`, система создаст его автоматически:
- **Позиция:** 0.5м вперёд, 1.5м вверх (уровень груди)
- **Имя:** "MuzzlePoint"

### Ручная настройка

Для точной настройки:
1. Создайте пустой GameObject на враге
2. Назовите его "MuzzlePoint" или "WeaponMuzzle"
3. Расположите в точке, откуда должны вылетать пули (обычно перед оружием)
4. Перетащите в поле `Muzzle Point` компонента `AIWeaponHandler`

---

## Поддерживаемые типы оружия

`AIWeaponHandler` поддерживает все типы оружия из вашей `WeaponSystem`:

- ✅ **Automatic** — автоматическая стрельба
- ✅ **SemiAuto** — одиночные выстрелы
- ✅ **Burst** — очередями
- ✅ **Charged** — с зарядкой

Тип оружия определяется в `WeaponScriptableObject.weaponType`.

---

## Настройка поведения стрельбы

### Через RangedAttackAction

В ScriptableObject `RangedAttackAction` настройте:

```
Fire Cooldown: 0.6 сек
```

Это минимальный интервал между вызовами `TryFire()`. 

**Важно:** Реальная скорострельность определяется параметром `fireRate` в `WeaponScriptableObject`!

### Пример балансировки:

**Быстрая стрельба:**
```
RangedAttackAction.fireCooldown = 0.3
WeaponScriptableObject.fireRate = 600 (выстрелов/мин)
```

**Медленная стрельба:**
```
RangedAttackAction.fireCooldown = 1.0
WeaponScriptableObject.fireRate = 120 (выстрелов/мин)
```

---

## Перезарядка

### Автоматическая перезарядка

`StandardWeapon` автоматически управляет боеприпасами и перезарядкой:
- Когда патроны заканчиваются, оружие перестаёт стрелять
- Перезарядка происходит автоматически через `weapon.Reload()`

### Ручная перезарядка (опционально)

Вы можете добавить логику перезарядки в AI:

```csharp
// В новом экшене или в RangedAttackAction
if (weaponHandler.IsReloading)
{
    // Бот перезаряжается — укрыться или отступить
    enemy.MoveTo(coverPosition);
}
```

---

## Отладка

### Проверка инициализации

При старте игры в консоли должно появиться:
```
[AIWeaponHandler] Оружие AssaultRifle инициализировано на Enemy_01
```

Если видите:
```
[AIWeaponHandler] Weapon data не назначен на Enemy_01
```
→ Назначьте `Weapon Data` в инспекторе

### Проверка стрельбы

Если AI не стреляет:
1. Убедитесь, что `AIWeaponHandler` добавлен
2. Проверьте, что `Weapon Data` назначен
3. Убедитесь, что `RangedAttackAction` в массиве `actions` на `AI`
4. Проверьте факторы `RangedAttackAction` — возможно, приоритет слишком низкий

### Визуализация Muzzle Point

Добавьте Gizmo в `AIWeaponHandler`:
```csharp
void OnDrawGizmosSelected()
{
    if (muzzlePoint != null)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(muzzlePoint.position, 0.1f);
        Gizmos.DrawLine(muzzlePoint.position, muzzlePoint.position + muzzlePoint.forward * 2f);
    }
}
```

---

## Продвинутая настройка

### Разные типы врагов с разным оружием

Создайте несколько префабов врагов:

**Снайпер:**
```
AIWeaponHandler:
  Weapon Data: SniperRifle
RangedAttackAction:
  Preferred Range: 25м
  Fire Cooldown: 1.5 сек
```

**Штурмовик:**
```
AIWeaponHandler:
  Weapon Data: AssaultRifle
RangedAttackAction:
  Preferred Range: 12м
  Fire Cooldown: 0.4 сек
```

**Пулемётчик:**
```
AIWeaponHandler:
  Weapon Data: MachineGun
RangedAttackAction:
  Preferred Range: 15м
  Fire Cooldown: 0.2 сек
```

### Смена оружия в рантайме

```csharp
// Получить AIWeaponHandler
var weaponHandler = ai.GetComponent<AIWeaponHandler>();

// Назначить новое оружие
weaponHandler.weaponData = newWeaponData;

// Переинициализировать
weaponHandler.InitializeWeapon();
```

### Использование своего компонента оружия

Если у вас есть кастомный компонент оружия:

1. Реализуйте интерфейс `IWeapon`:
```csharp
public class MyCustomWeapon : MonoBehaviour, IWeapon
{
    public void Initialize(WeaponScriptableObject weaponData, Transform muzzlePoint) { }
    public void TryFire() { }
    public void ReleaseTrigger() { }
    public void Reload() { }
    public bool IsReloading { get; }
}
```

2. Добавьте компонент на врага
3. `RangedAttackAction` автоматически найдёт его через `TryGetComponent<IWeapon>`

---

## Частые вопросы

### Q: Враг стреляет слишком быстро/медленно?
**A:** Настройте `fireCooldown` в `RangedAttackAction` и `fireRate` в `WeaponScriptableObject`.

### Q: Пули вылетают не из того места?
**A:** Настройте `Muzzle Point` вручную или проверьте автоматическую позицию (0.5м вперёд, 1.5м вверх).

### Q: Враг не перезаряжается?
**A:** `StandardWeapon` управляет перезарядкой автоматически. Проверьте `magazineSize` и `reloadTime` в `WeaponScriptableObject`.

### Q: Можно ли использовать разные оружия для разных действий?
**A:** Да, но потребуется создать несколько `AIWeaponHandler` или расширить логику. Сейчас один враг = одно оружие.

### Q: Как сделать, чтобы враг стрелял очередями?
**A:** Используйте `WeaponType.Burst` в `WeaponScriptableObject` и настройте `burstCount` и `burstDelay`.

---

## Итоги

✅ **Простая интеграция** — добавьте `AIWeaponHandler` и назначьте `Weapon Data`  
✅ **Автоматическая работа** — `RangedAttackAction` сам найдёт и использует оружие  
✅ **Полная совместимость** — использует вашу существующую `WeaponSystem`  
✅ **Гибкость** — поддержка всех типов оружия и кастомных компонентов  

**Враги теперь стреляют как профи! 🎯**
