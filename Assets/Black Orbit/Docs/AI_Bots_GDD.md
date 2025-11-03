# Руководство геймдизайнера: Создание и настройка ботов AI

Это краткое руководство объясняет, как быстро создать бота на сцене, подключить профили поведения и отладить поведение. Всё без программирования.

## Быстрый старт (2–3 клика)

1. **Откройте окно сборки бота**: меню `Black Orbit/AI/Prefab Builder`.
2. **Укажите модель (Root GameObject)** — поле "Модель (Root)".
3. (Опционально) **Укажите AI Profile** — поле "AI Profile".
4. Нажмите кнопку **"Создать Бота на Сцене"**.
5. (Опционально) Нажмите **"Создать/Добавить Шедулер"**, чтобы добавить централизованный планировщик `AIUpdateScheduler`.

Бот будет создан с:
- `Rigidbody` (Unity 6; движение через `Rigidbody.linearVelocity`),
- `NavMeshAgent` (только планирование пути),
- `AIMovementNavMeshMotor` (физическое перемещение по пути NavMesh),
- `AIController` + `AIProfileLoader` (подгружает профиль),
- (Опционально) `AICombat`, `AIGrenadeThrower`.

## Компоненты и их назначение

- **`AIController`** (`Assets/Black Orbit/Scripts/AI/Runtime/Controller/AIController.cs`)
  - Принимает решения по доменам (Movement, Combat, и т.д.).
  - Параметр `tickRate` — период логики.
  - Сглаживание решений для адекватного поведения:
    - `switchHysteresis` — насколько новый скор должен превосходить текущий, чтобы переключиться.
    - `minActionDuration` — минимальная длительность активного действия.
    - `cooldownOnSwitch` — кулдаун на предыдущее действие после переключения.

- **`AIProfileLoader`** (`Assets/Black Orbit/Scripts/AI/Runtime/Controller/AIProfileLoader.cs`)
  - Читает `AIProfile` и регистрирует экшены в доменах.
  - Ожидает наличие `IMovementMotor` (т.е. `AIMovementNavMeshMotor`).

- **`AIMovementNavMeshMotor`** (`Assets/Black Orbit/Scripts/AI/Runtime/Movement/AIMovementNavMeshMotor.cs`)
  - Навигация по `NavMesh`, перемещение — через `Rigidbody.linearVelocity` (Unity 6 best practice).
  - Важные параметры: `maxSpeed`, `acceleration`, `stopDistance`, `rotateToVelocity`.

- **`AIUpdateScheduler`** (`Assets/Black Orbit/Scripts/AI/Runtime/Controller/AIUpdateScheduler.cs`)
  - Централизованный планировщик тиков для всех `AIController` в сцене.
  - Параметры производительности: `maxAgentsPerFrame`, `timeBudgetMs`, `staggerTicks`, `paused`.

## Сборка бота вручную (если не использовать окно)

1. На корневой `GameObject` добавьте: `Rigidbody`, `NavMeshAgent` (выключить `updatePosition`, `updateRotation`).
2. Добавьте `AIMovementNavMeshMotor` и настройте `maxSpeed`, `acceleration`, `stopDistance`.
3. Добавьте `AIController` и `AIProfileLoader`. В `AIProfileLoader.profile` назначьте нужный `AIProfile`.
4. (Опционально) Добавьте `AICombat`, `AIGrenadeThrower`.
5. В сцену добавьте один `AIUpdateScheduler` (можно через кнопку в `Prefab Builder`).

## Настройка поведения (AI Profile)

- `AIProfile` содержит разделы Movement/Combat/Tactics — включайте нужные экшены, настраивайте веса/пороговые значения.
- Для движения бот потребует `IMovementMotor` (ставится автоматически окном сборки).
- Типовые рекомендации по весам:
  - Movement/Explore: невысокий базовый вес, включать при отсутствии других приоритетов.
  - Movement/Pursue: базовый вес + зависимость от `DistanceToTarget`/`Visibility` (см. Utility Curves).
  - Combat/Shoot: высокий базовый вес при видимом таргете и достаточных патронах.
  - Combat/Reload: включается при малом количестве патронов.
  - Tactics/Retreat: включается при низком здоровье или под огнем (см. профиль).

## Отладка в Editor и Runtime

- Окно: `Black Orbit/AI/Utility Debug View` (`UtilityDebugWindow.cs`)
  - Секция `Scheduler`: видно количество обработанных ботов, время апдейта в мс, графики производительности; можно ставить `Paused`, настроить `Stagger`, `MaxAgents/Frame`, `Time Budget`.
  - Список агентов: видно `tickRate` и `ETA` до следующего тика.
  - Детали агента: фильтры по домену/экшену/минимальному `score`, кнопки `Copy Top Actions` и `Dump Log to Console`.

- Runtime Overlay: компонент `UtilityRuntimeOverlay` (в сцене как отдельный `GameObject`)
  - Клавиша `F9` — показать/скрыть оверлей.
  - Показывает домены/действия, логи, и состояние шедулера (`ETA`, `Processed`, `ms`, `Paused`).

## Рекомендации по тюнингу

- **Стабильность поведения**: регулируйте `switchHysteresis` (0.1–0.2), `minActionDuration` (0.5–1.0с), `cooldownOnSwitch` (0.5–1.5с) в `AIController`.
- **Скорость мышления**: `tickRate` (0.2–0.5с) — ниже = чаще, но дороже для CPU.
- **Производительность**: в `AIUpdateScheduler` включите `staggerTicks`, и настройте `maxAgentsPerFrame` (20–60) и `timeBudgetMs` (1.5–4.0).
- **Навигация**: убедитесь, что `NavMesh` построен; корректно выставлены `NavMeshAgent.radius/height`.
- **Движение (Unity 6)**: используем `Rigidbody.linearVelocity` — не меняйте `Rigidbody.velocity` напрямую.

## Частые проблемы и решения

- Бот не двигается:
  - Нет `NavMesh` или `NavMeshAgent` выключает позицию/ротацию неверно. Должно быть: `updatePosition=false`, `updateRotation=false`.
  - Нет `AIMovementNavMeshMotor` или отсутствует цель в действиях движения.

- Профиль не применился:
  - `AIProfileLoader.profile` не задан или профиль отключен.

- Слишком часто переключается между действиями:
  - Увеличьте `minActionDuration` и/или `switchHysteresis`. Проверьте веса в профиле.

- Просадки FPS с толпой ботов:
  - Добавьте на сцену один `AIUpdateScheduler`. Включите `staggerTicks`, уменьшите `maxAgentsPerFrame` и/или `timeBudgetMs`.

## Где лежат скрипты

- Контроллер: `Assets/Black Orbit/Scripts/AI/Runtime/Controller/AIController.cs`
- Лоадер профиля: `Assets/Black Orbit/Scripts/AI/Runtime/Controller/AIProfileLoader.cs`
- Планировщик: `Assets/Black Orbit/Scripts/AI/Runtime/Controller/AIUpdateScheduler.cs`
- Движение: `Assets/Black Orbit/Scripts/AI/Runtime/Movement/AIMovementNavMeshMotor.cs`
- Окно сборки: `Assets/Black Orbit/Scripts/AI/Editor/AIPrefabBuilderWindow.cs`
- Debug Editor: `Assets/Black Orbit/Scripts/AI/Debug/Editor/UtilityDebugWindow.cs`
- Debug Runtime: `Assets/Black Orbit/Scripts/AI/Debug/Runtime/UtilityRuntimeOverlay.cs`

---
Если нужно создать пресеты профилей для разных типов ботов (штурмовик/разведчик/снайпер), дайте знать — подготовим библиотеку `AIProfile` с примерами весов и кривых.
