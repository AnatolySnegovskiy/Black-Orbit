# Генератор префабов AI

## Обзор

Автоматическое создание готовых префабов AI с предустановленными настройками. Одиночные боты и squad'ы одним кликом!

## Использование

### Открыть генератор:
**Tools → AI → Generate Prefabs**

### Создать префаб:
Просто нажмите на нужную кнопку — префаб создастся автоматически в `Assets/Black Orbit/GameData/AI/Prefabs/`

---

## Одиночные боты

### 🎯 Тактический стрелок (Tactical Shooter)
**Описание:** Сбалансированный боец с тактическим поведением

**Характеристики:**
- Health: 100
- Move Speed: 3.5 м/с
- Vision Angle: 120°
- Vision Range: 18м
- Detection Range: 12м

**Действия:**
- Patrol, Explore, SearchLastKnown
- Pursue, Flank, TakeCover
- RangedAttack, SuppressionFire

**Использование:** Универсальный враг для любых ситуаций

---

### ⚡ Агрессивный боец (Aggressive Fighter)
**Описание:** Быстрый и агрессивный боец

**Характеристики:**
- Health: 120
- Move Speed: 5 м/с
- Vision Angle: 140°
- Vision Range: 20м
- Detection Range: 15м

**Действия:**
- Pursue, RangedAttack, MeleeAttack
- Flank, SearchLastKnown

**Использование:** Для интенсивных боёв, агрессивные атаки

---

### 🎯 Снайпер (Sniper)
**Описание:** Дальнобойный боец с узким обзором

**Характеристики:**
- Health: 80
- Move Speed: 2.5 м/с
- Vision Angle: 90°
- Vision Range: 30м
- Detection Range: 25м

**Действия:**
- Patrol, TakeCover, RangedAttack
- SuppressionFire, Retreat

**Особенности:**
- RangedAttack.preferredRange = 25м
- RangedAttack.fireCooldown = 1.5 сек

**Использование:** Дальняя поддержка, позиционная игра

---

### ⚔️ Берсерк (Melee Berserker)
**Описание:** Ближний боец с высоким здоровьем

**Характеристики:**
- Health: 150
- Move Speed: 6 м/с
- Vision Angle: 160°
- Vision Range: 15м
- Detection Range: 12м

**Действия:**
- Pursue, MeleeAttack, Flank

**Использование:** Ближний бой, агрессивные атаки

---

### 🚶 Патрульный (Scout/Patrol)
**Описание:** Разведчик с фокусом на патрулирование

**Характеристики:**
- Health: 80
- Move Speed: 3 м/с
- Vision Angle: 90°
- Vision Range: 15м
- Detection Range: 10м

**Действия:**
- Patrol, Explore, SearchLastKnown
- Pursue, Retreat

**Использование:** Патрулирование уровня, разведка

---

## Squad'ы (Отряды)

### 👥 Тактический отряд (Tactical Squad)
**Состав:** 3 бойца

**Члены:**
1. **Rusher** — штурмовик
   - Действия: Pursue, RangedAttack, Flank
   
2. **Flanker** — фланкёр
   - Действия: Flank, RangedAttack, TakeCover
   
3. **Suppressor** — подавление
   - Действия: RangedAttack, SuppressionFire, TakeCover

**Настройки squad:**
- Search Radius: 20м
- Coordination Interval: 0.5 сек
- Min Members: 2

**Использование:** Универсальный тактический отряд

---

### 🔫 Штурмовой отряд (Assault Squad)
**Состав:** 4 бойца

**Члены:**
4x **Assaulter** — штурмовики
- Действия: Pursue, RangedAttack, Flank, MeleeAttack
- Health: 120
- Move Speed: 4.5 м/с

**Настройки squad:**
- Search Radius: 25м
- Coordination Interval: 0.3 сек (быстрая реакция)
- Min Members: 2

**Использование:** Агрессивные атаки, численное превосходство

---

### 🎯 Снайперская команда (Sniper Team)
**Состав:** 3 бойца

**Члены:**
1. **Sniper_1** — снайпер
   - Действия: RangedAttack, TakeCover, SuppressionFire
   - Vision Range: 30м
   - Vision Angle: 90°
   
2. **Sniper_2** — снайпер
   - Аналогично Sniper_1
   
3. **Spotter** — наблюдатель/прикрытие
   - Действия: Patrol, SearchLastKnown, RangedAttack, TakeCover

**Настройки squad:**
- Search Radius: 30м
- Coordination Interval: 1 сек
- Min Members: 2

**Использование:** Дальняя поддержка, позиционная игра

---

## Кастомный префаб

### Создание своего префаба:

1. Введите имя в поле "Имя"
2. Нажмите "Создать базовый AI"
3. Откройте префаб и настройте параметры

**Базовые настройки:**
- Faction: Enemy
- Health: 100
- Move Speed: 3.5 м/с
- Vision: 120° / 15м
- Действия: Patrol, Pursue, RangedAttack

---

## Структура префаба

### Одиночный бот:
```
BotName (GameObject)
├─ AI (Component)
├─ NavMeshAgent (Component)
├─ Rigidbody (Component)
├─ CapsuleCollider (Component)
├─ AIWeaponHandler (Component) [опционально]
└─ Visual (GameObject)
    └─ Capsule (MeshRenderer)
```

### Squad:
```
SquadName (GameObject)
├─ AISquad (Component)
├─ Member_1 (GameObject)
│   ├─ AI (Component, squad = parent)
│   ├─ NavMeshAgent
│   ├─ Rigidbody
│   ├─ AIWeaponHandler
│   └─ Visual
├─ Member_2 (GameObject)
│   └─ ...
└─ Member_3 (GameObject)
    └─ ...
```

---

## Требования

### Перед созданием префабов:

1. **Создайте действия:**
   - Tools → AI → Generate Action Assets
   - Или создайте вручную в `GameData/AI/Actions/`

2. **Настройте слои:**
   - Player, Enemy, Cover, Obstacle

3. **Запеките NavMesh:**
   - Window → AI → Navigation → Bake

### Если действия не найдены:

Генератор выдаст предупреждение:
```
⚠️ Действие не найдено: Assets/.../Patrol.asset
Сначала создайте действия через Tools → AI → Generate Action Assets
```

Решение: создайте действия через генератор действий.

---

## Настройка после создания

### Что можно изменить:

1. **Визуализацию:**
   - Замените капсулу на свою модель
   - Добавьте анимации

2. **Параметры AI:**
   - Health, Speed, Vision
   - Добавьте/удалите действия

3. **Оружие:**
   - Назначьте Weapon Data в AIWeaponHandler
   - Настройте Muzzle Point

4. **Squad:**
   - Измените количество членов
   - Настройте роли вручную

---

## Использование префабов

### На сцене:

1. Перетащите префаб на сцену
2. Убедитесь, что он стоит на NavMesh
3. Настройте Patrol Points (опционально)
4. Назначьте Weapon Data (для стрельбы)
5. Запустите игру!

### Для squad:

1. Перетащите squad префаб на сцену
2. Члены отряда уже настроены и привязаны
3. Squad автоматически начнёт координацию
4. Можно изменить позиции членов

---

## Советы

### Для разнообразия:

- Создайте несколько вариантов одного типа
- Измените параметры (health, speed)
- Комбинируйте разные действия

### Для баланса:

- **Легкие враги:** Scout, Sniper (80 HP)
- **Средние враги:** Tactical Shooter (100 HP)
- **Тяжелые враги:** Aggressive Fighter (120 HP), Berserker (150 HP)

### Для тактики:

- Комбинируйте типы: снайпер + штурмовики
- Используйте squad'ы для координации
- Добавляйте patrol points для маршрутов

---

## Частые вопросы

### Q: Префабы не создаются?
**A:** Убедитесь, что папка `GameData/AI/Prefabs/` существует или генератор создаст её автоматически.

### Q: Действия не загружаются?
**A:** Сначала создайте действия через Tools → AI → Generate Action Assets.

### Q: Как изменить модель?
**A:** Откройте префаб, удалите Visual/Capsule, добавьте свою модель как дочерний объект.

### Q: Squad не работает?
**A:** Убедитесь, что все члены имеют компонент AI и привязаны к squad.members.

### Q: Можно ли создать свой тип?
**A:** Да, используйте "Создать базовый AI" и настройте параметры вручную.

---

## Итоги

✅ **5 типов одиночных ботов** — от разведчика до берсерка  
✅ **3 типа squad'ов** — тактический, штурмовой, снайперский  
✅ **Кастомный генератор** — создайте свой тип  
✅ **Автоматическая настройка** — всё готово из коробки  
✅ **Легко модифицировать** — измените любые параметры  

**Создавайте армии AI одним кликом! 🎯**

---

**Версия:** 2.1  
**Дата:** 04.10.2025
