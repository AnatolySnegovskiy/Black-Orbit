# История изменений AI системы

## Версия 3.1 - Suppression/Audio/Cover v2/Orders (05.10.2025)

### ✨ Новые возможности

- **Подавление (Suppression)**
  - Поле `AIBlackboard.SuppressionLevel` с плавным затуханием
  - Генерация подавления из урона: `AI` подписывается на `Health.OnDamaged`
  - Влияние на поведение:
    - `TakeCoverAction` повышает приоритет под огнём
    - `PeekAndShootAction` динамически сокращает `peek` и увеличивает `hide`

- **Слух (Audio v1)**
  - `AI.EmitNoise(position, level, radius)` рассылает события шума
  - `AIBlackboard.HeardNoisePos/NoiseLevel` с затуханием
  - При отсутствии цели `NavTargetPos` берётся из источника шума

- **Укрытия v2**
  - `CoverService`: выбор точки «за» препятствием по нормали с латеральным смещением
  - Привязка к `NavMesh` и валидация LOS-блокировки
  - `TakeCoverAction`: кэш точки укрытия с проверкой валидности

- **Приказы отряда**
  - `AISquad.ReportSuppression(source, level)` — распределение подавления внутри отряда
  - `AISquad.OrderFlank(aroundPos, duration, maxFlankers)` — фланговый приказ
  - `AISquad.OrderSuppressAt(pos, duration, suppressors, flankers)` — комбинированный приказ «подавление + фланг»
  - При высоком среднем подавлении отряд увеличивает долю фланкёров

- **PeekAndShootAction**
  - Добавлено новое действие с фазами peek/hide и учётом подавления

- **Генераторы**
  - `AIActionAssetGenerator`: генерация `PeekAndShoot`
  - `AIPrefabGenerator`: добавлены `TakeCover` и `PeekAndShoot` в пресеты; автоназначение `AISettings.asset`

- **Документация**
  - Обновлены: `README_RU.md`, `QUICK_SETUP_RU.md`, `SQUAD_SYSTEM_RU.md`, `README_ACTIONS.md`, `README_PREFABS.md`

---

## Версия 3.0 - Faction System Overhaul (04.10.2025)

### 🎯 Полностью переработанная система фракций

#### Новая архитектура
- ✅ **FactionData (ScriptableObject)** — фракции как ассеты, неограниченное количество
- ✅ **Гибкие отношения** — значения от -1 (лютый враг) до 1 (лучший друг)
- ✅ **Централизованный FactionManager** — управление всеми фракциями
- ✅ **FactionMember** — легкий компонент для любых объектов
- ✅ **Визуальная настройка** — цвет, иконка, описание для каждой фракции

#### Удалено из старой системы
- ❌ Enum Faction (Player, Enemy, Wildlife, Neutral)
- ❌ Поля hostileFactions и alliedFactions в AI
- ❌ Старый FactionManager с жёсткими категориями
- ❌ Старый FactionMember с массивом hostileFactions

#### Преимущества новой системы
- ✅ **Неограниченное количество фракций** — создавайте сколько нужно
- ✅ **Гибкость** — 11 уровней отношений вместо 3 категорий
- ✅ **Динамичность** — плавное изменение отношений в runtime
- ✅ **Репутация** — система квестов, предательств, союзов
- ✅ **Простота** — настройка в Inspector, не в коде

### 📝 Новые файлы

**FactionSystem/** (новая папка, архитектура как WeaponSystem)
- `ScriptableObjects/FactionData.cs` — ScriptableObject фракции
- `Runtime/FactionMember.cs` — компонент для объектов с фракцией
- `Runtime/FactionManager.cs` — Singleton менеджер фракций
- `README.md` — полная документация системы

### 🔧 Изменённые файлы

**AI/Runtime:**
- `AI.cs` — использует FactionData вместо enum Faction
- Удалены поля hostileFactions и alliedFactions
- Обновлены методы поиска целей и проверки отношений
- Обновлены using для FactionSystem.ScriptableObjects и FactionSystem.Runtime
- **Новые методы:** `GetRelationship()`, `ShouldAttack()`, `ShouldHelp()`, `GetAggressionLevel()`
- Поддержка 7 градаций отношений для тонкого поведения

**AI/Runtime:**
- `AISquad.cs` — проверяет отношения перед координацией атаки
- Не координирует атаку на нейтральные/дружественные цели

**AI/Editor:**
- `AIPrefabGenerator.cs` — загружает FactionData из ассетов

**FactionSystem/ScriptableObjects:**
- `FactionData.cs` — добавлены методы `IncreaseReputation()` и `DecreaseReputation()`

### ❌ Удалённые файлы

**AI/Runtime:** (старые файлы фракций удалены)
- `FactionManager.cs` — перенесён в FactionSystem/Runtime/
- `FactionMember.cs` — перенесён в FactionSystem/Runtime/

**AI/Enums:**
- `Faction.cs` — заменён на ScriptableObject FactionData

### 🔄 Миграция

**Было:**
```csharp
public Faction faction = Faction.Enemy;
public List<Faction> hostileFactions = new List<Faction> { Faction.Player };
```

**Стало:**
```csharp
public FactionData faction; // Ссылка на Enemy.asset
// Отношения настраиваются в FactionData
```

---

## Версия 2.1 - Squad System Release (04.10.2025)

### ✨ Новые возможности

#### Генератор готовых действий
- ✅ **AIActionAssetGenerator** — Editor-инструмент для автоматического создания действий
- ✅ **Одним кликом** — все 10 действий с настроенными факторами
- ✅ **Предустановленные параметры** — оптимальные значения для каждого действия
- ✅ **Готовые кривые** — Linear, S-Curve, Convex, Bell
- ✅ **Путь:** Tools → AI → Generate Action Assets

#### Генератор префабов AI
- ✅ **AIPrefabGenerator** — Editor-инструмент для создания готовых префабов
- ✅ **5 типов одиночных ботов** — Tactical Shooter, Aggressive Fighter, Sniper, Berserker, Scout
- ✅ **3 типа squad'ов** — Tactical Squad (3), Assault Squad (4), Sniper Team (3)
- ✅ **Кастомный генератор** — создание базового AI с произвольным именем
- ✅ **Полная настройка** — AI, NavMeshAgent, Rigidbody, Collider, Visual
- ✅ **Путь:** Tools → AI → Generate Prefabs

#### Система Squad (Отрядов)
- ✅ **AISquad** — компонент для управления группой AI
- ✅ **Автоматическое назначение ролей** — Rusher, Flanker, Suppressor, Defender
- ✅ **Координация атак** — члены отряда работают вместе
- ✅ **Система приказов** — squad может переопределить Utility AI
- ✅ **Общая цель отряда** — автоматическое отслеживание цели
- ✅ **Гибкое API** — ручное и автоматическое управление

#### Новые компоненты
- ✅ **FactionManager** — централизованная система управления отношениями между фракциями
- ✅ **FactionMember** — легкий компонент фракции для игрока и NPC без AI
- ✅ Автоматический поиск FactionMember в FindNearestHostile()
- ✅ Singleton с DontDestroyOnLoad
- ✅ Кэширование отношений для производительности
- ✅ Динамическое изменение отношений в runtime

#### Новые действия
- ✅ **SuppressionFireAction** — подавляющий огонь по последней позиции

#### Улучшения AI.cs
- ✅ Добавлено поле `squad` для привязки к отряду
- ✅ Метод `OrderAction()` — получение приказов от squad
- ✅ Метод `CancelOrder()` — отмена приказа
- ✅ Свойство `IsFollowingOrder` — проверка выполнения приказа
- ✅ Приоритет приказов над Utility AI
- ✅ **Debug-визуализация** — конус зрения, дальности, цель, информация в Scene View
- ✅ **Интеграция с FactionManager** — убраны поля hostileFactions и alliedFactions
- ✅ Отношения теперь управляются централизованно через FactionManager

#### Документация
- ✅ **SQUAD_SYSTEM_RU.md** — полное руководство по системе squad (15+ страниц)
- ✅ Примеры использования и API
- ✅ Советы по реализации поведения как в F.E.A.R.

### 🎯 Возможности уровня F.E.A.R.

С добавлением squad системы AI теперь может:
- **Координировать атаки** — один подавляет, другие фланкуют
- **Назначать роли** — автоматическое распределение задач
- **Работать командой** — синхронизация действий
- **Подавляющий огонь** — стрельба по последней позиции
- **Тактическое преимущество** — использование численного превосходства

### 📝 Новые файлы

**Runtime:**
- `AISquad.cs` — система управления отрядом
- `FactionManager.cs` — централизованная система фракций
- `FactionMember.cs` — компонент фракции для игрока/NPC

**Editor:**
- `AIActionAssetGenerator.cs` — генератор готовых действий
- `AIPrefabGenerator.cs` — генератор префабов AI

**ScriptableObjects/Actions:**
- `SuppressionFireAction.cs` — подавляющий огонь

**Документация:**
- `SQUAD_SYSTEM_RU.md` — руководство по squad системе
- `README_ACTIONS.md` — описание готовых действий
- `README_PREFABS.md` — описание генератора префабов
- `FACTION_SYSTEM.md` — система фракций (обновлено)
- `FACTION_MANAGER.md` — централизованное управление фракциями
- `DEBUG_VISUALIZATION.md` — визуализация и отладка

### 🔧 Изменённые файлы

**Runtime:**
- `AI.cs` — добавлена поддержка squad и приказов

---

## Версия 2.0.1 - Bugfix Release (04.10.2025)

### 🐛 Исправленные ошибки

#### RangedAttackAction.cs
- **Исправлено:** Неправильный namespace `Runtime.AIWeaponHandler` → `AIWeaponHandler`
- **Причина:** Компилятор не мог найти класс из-за неполного пути
- **Решение:** Используется корректный namespace, так как `using Black_Orbit.Scripts.AI.Runtime` уже импортирован

#### RetreatAction.cs
- **Исправлено:** Прямое использование `ai.rb.MovePosition()` без проверки на null
- **Причина:** Вызывало `NullReferenceException` если у AI нет Rigidbody
- **Решение:** Заменено на универсальный метод `ai.MoveTo()`, который работает с NavMeshAgent и Rigidbody
- **Добавлено:** Проверка `if (ai.target == null) return;` для безопасности
- **Улучшено:** Отступление теперь рассчитывает позицию на 3м от текущей позиции

#### README_RU.md
- **Исправлено:** Обрыв текста в строке 341 (`{{ ... }}`)
- **Добавлено:** Раздел "Для снайпера" с рекомендациями по настройке
- **Улучшено:** Описание RetreatAction с информацией о совместимости

#### WEAPON_INTEGRATION_RU.md
- **Исправлено:** Устаревшие ссылки на `EnemyWeaponHandler` → `AIWeaponHandler`
- **Обновлено:** 4 упоминания в документации для соответствия актуальному коду

### 📝 Обновлённые файлы

**Runtime:**
- `RetreatAction.cs` — исправлена логика отступления

**ScriptableObjects/Actions:**
- `RangedAttackAction.cs` — исправлен namespace

**Документация:**
- `README_RU.md` — исправлен обрыв текста, добавлен раздел для снайпера
- `WEAPON_INTEGRATION_RU.md` — обновлены названия компонентов
- `CHANGELOG_RU.md` — добавлена информация об исправлениях

---

## Версия 2.0 - Production Release (04.10.2025)

### ✨ Новые возможности

#### Система восприятия
- ✅ Угол обзора (Vision Angle) с проверкой через `Vector3.Angle`
- ✅ Дальность зрения (Vision Range)
- ✅ Проверка препятствий через `Physics.Raycast` с `obstacleMask`
- ✅ Line of Sight (LOS) система
- ✅ Чёрная доска (Blackboard) для хранения последней позиции игрока

#### Навигация
- ✅ Полная поддержка `NavMeshAgent`
- ✅ Fallback на `Rigidbody` для простых сцен
- ✅ Утилиты: `MoveTo()`, `Stop()`, `LookAt()`, `IsAtDestination()`

#### Новые действия (Actions)
- ✅ **PatrolAction** — патрулирование по точкам или случайное блуждание
- ✅ **ExploreAction** — исследование окружения
- ✅ **SearchLastKnownAction** — поиск на последней известной позиции игрока
- ✅ **PursueAction** — преследование игрока
- ✅ **FlankAction** — фланговый манёвр (обход сбоку/с тыла)
- ✅ **TakeCoverAction** — поиск укрытия с проверкой блокировки LOS
- ✅ **RangedAttackAction** — дальняя атака с поддержанием дистанции
- ✅ **MeleeAttackAction** — ближняя атака

#### Интеграция с WeaponSystem
- ✅ **AIWeaponHandler** — компонент для управления оружием AI
- ✅ Автоматическая инициализация `StandardWeapon`
- ✅ Поддержка всех типов оружия (Automatic, SemiAuto, Burst, Charged)
- ✅ Автоматическое создание Muzzle Point
- ✅ Интеграция с `RangedAttackAction`

#### Документация на русском
- ✅ **README_RU.md** — полное руководство (20+ страниц)
- ✅ **QUICK_SETUP_RU.md** — быстрая настройка за 5 минут
- ✅ **WEAPON_INTEGRATION_RU.md** — интеграция с WeaponSystem
- ✅ Все параметры с `[Tooltip]` на русском
- ✅ Все комментарии в коде на русском
- ✅ XML-документация для всех публичных методов

### 🔧 Улучшения

#### EnemyAI.cs
- Добавлены Header-секции для группировки параметров
- Все публичные методы с XML-комментариями
- Улучшена система восприятия с учётом угла обзора
- Добавлена чёрная доска для принятия решений

#### UtilityAction.cs
- Подробные комментарии для каждого метода
- Tooltip для массива факторов
- Улучшенная нормализация оценок

#### UtilityFactor.cs
- Tooltip для всех параметров
- Описание типов кривых в документации

### 📝 Обновлённые файлы

**Runtime:**
- `AI.cs` — расширен с восприятием и навигацией
- `AIWeaponHandler.cs` — новый компонент

**ScriptableObjects/Actions:**
- `UtilityAction.cs` — обновлён с комментариями
- `AttackAction.cs` — помечен как Legacy
- `RetreatAction.cs` — обновлён с комментариями
- `PatrolAction.cs` — новый
- `ExploreAction.cs` — новый
- `SearchLastKnownAction.cs` — новый
- `PursueAction.cs` — новый
- `FlankAction.cs` — новый
- `TakeCoverAction.cs` — новый
- `RangedAttackAction.cs` — новый с интеграцией WeaponSystem
- `MeleeAttackAction.cs` — новый

**ScriptableObjects:**
- `UtilityFactor.cs` — обновлён с Tooltip

**Документация:**
- `README_RU.md` — новый
- `QUICK_SETUP_RU.md` — новый
- `WEAPON_INTEGRATION_RU.md` — новый
- `CHANGELOG_RU.md` — новый (этот файл)

---

## Версия 1.0 - Initial Release

### Базовая функциональность
- ✅ Utility AI система
- ✅ `EnemyAI` компонент
- ✅ `UtilityAction` базовый класс
- ✅ `UtilityFactor` для оценки полезности
- ✅ `AttackAction` — базовая атака
- ✅ `RetreatAction` — отступление

---

## Миграция с версии 1.0 на 2.0

### Что нужно сделать:

1. **Обновить EnemyAI:**
   - Добавить `NavMeshAgent` на врага
   - Настроить новые параметры восприятия
   - Назначить `obstacleMask` и `coverMask`

2. **Создать новые действия:**
   - Create → AI/Actions → выбрать нужные
   - Настроить факторы по рекомендациям из `QUICK_SETUP_RU.md`

3. **Добавить AIWeaponHandler (для дальней атаки):**
   - Add Component → AI Weapon Handler
   - Назначить Weapon Data

4. **Запечь NavMesh:**
   - Window → AI → Navigation → Bake

### Обратная совместимость:

- ✅ Старые `AttackAction` и `RetreatAction` работают
- ✅ Можно использовать без `NavMeshAgent` (через Rigidbody)
- ✅ Можно использовать без `AIWeaponHandler` (будет Debug.Log)

---

## Планы на будущее

### Версия 2.1 (планируется)
- [ ] Система групповой тактики (координация между врагами)
- [ ] Динамическое укрытие (создание временных укрытий)
- [ ] Система коммуникации (враги предупреждают друг друга)
- [ ] Улучшенный поиск пути с учётом опасных зон

### Версия 2.2 (планируется)
- [ ] Система эмоций/состояний (страх, агрессия, паника)
- [ ] Адаптивная сложность (враги учатся на действиях игрока)
- [ ] Система патрулирования с запоминанием маршрутов
- [ ] Интеграция с системой звуков (реакция на шум)

### Версия 3.0 (планируется)
- [ ] Machine Learning интеграция (ML-Agents)
- [ ] Процедурная генерация поведения
- [ ] Визуальный редактор Utility AI
- [ ] Профилирование и оптимизация производительности

---

## Известные ограничения

### Текущие ограничения:
- Один враг может использовать только одно оружие одновременно
- `TakeCoverAction` требует правильно настроенные слои укрытий
- `FlankAction` выбирает сторону случайно (нет анализа позиции игрока)
- Нет системы перезарядки с укрытием (враг не прячется во время reload)

### Workarounds:
- Для смены оружия используйте `AIWeaponHandler.InitializeWeapon()`
- Для укрытий создайте отдельный слой `Cover` и назначьте его объектам
- Для умного фланга можно расширить `FlankAction.Execute()`
- Для перезарядки с укрытием создайте отдельный `ReloadAction`

---

## Благодарности

Спасибо за использование системы AI для Black Orbit! 🚀

Если нашли баг или есть предложения — создайте Issue или Pull Request.

**Версия:** 2.0  
**Дата:** 04.10.2025  
**Автор:** Cascade AI Assistant
