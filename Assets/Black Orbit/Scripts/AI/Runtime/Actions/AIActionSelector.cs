using UnityEngine;
using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime;

namespace Black_Orbit.Scripts.AI.Runtime.Actions
{
    public class AIActionSelector
    {
        private UtilityAction _currentMovement;
        private UtilityAction _currentCombat;
        private UtilityAction _orderedAction;
        private float _orderTimer;

        public UtilityAction CurrentAction => _currentMovement != null ? _currentMovement : _currentCombat; // для обратной совместимости
        public UtilityAction CurrentMovement => _currentMovement;
        public UtilityAction CurrentCombat => _currentCombat;
        public bool IsFollowingOrder => _orderedAction != null && _orderTimer > 0f;

        public void SelectAndExecute(AI ai, AIBlackboard bb, UtilityAction[] actions, bool verboseLogs = false)
        {
            if (actions == null || actions.Length == 0) return;

            // Жёсткие пререквизиты боя: если цели нет/неактивна/мертва или нет LOS — сбрасываем боевой экшен и отпускаем спуск
            bool targetDead = false;
            if (ai.Target != null)
            {
                var th = ai.Target.GetComponent<Black_Orbit.Scripts.Core.Runtime.Health>();
                targetDead = th != null && th.IsDead;
            }
            if (ai.Target == null || !ai.Target.gameObject.activeInHierarchy || targetDead || !ai.hasLineOfSight)
            {
                if (_currentCombat != null)
                {
                    _currentCombat = null;
                    if (ai.TryGetComponent<AIWeaponHandler>(out var h0)) h0.ReleaseTrigger();
                    else if (ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var w0)) w0.ReleaseTrigger();
                    #if UNITY_EDITOR
                    if (verboseLogs) Debug.Log($"🔕 [{ai.faction}] Combat сброшен: нет цели/LOS");
                    #endif
                }
            }

            // Приказ активен: фиксируем конкретный экшен на его канале
            if (IsFollowingOrder)
            {
                _orderTimer -= Time.deltaTime;
                if (_orderTimer > 0f)
                {
                    if (_orderedAction.Channel == ActionChannel.Movement)
                    {
                        if (_currentMovement != _orderedAction)
                        {
                            _currentMovement = _orderedAction;
                            #if UNITY_EDITOR
                            if (verboseLogs) Debug.Log($"🎖️ [{ai.faction}] Приказ(движение): {_currentMovement.name}");
                            #endif
                        }
                        _currentMovement.Execute(ai);
                    }
                    else if (_orderedAction.Channel == ActionChannel.Combat)
                    {
                        // Если нет цели/LOS — не исполняем боевой приказ, отпускаем спуск и ждём
                        bool dead2 = false;
                        if (ai.Target != null)
                        {
                            var t2 = ai.Target.GetComponent<Black_Orbit.Scripts.Core.Runtime.Health>();
                            dead2 = t2 != null && t2.IsDead;
                        }
                        if (ai.Target == null || !ai.Target.gameObject.activeInHierarchy || dead2 || !ai.hasLineOfSight)
                        {
                            if (_currentCombat != null)
                            {
                                if (ai.TryGetComponent<AIWeaponHandler>(out var h1)) h1.ReleaseTrigger();
                                else if (ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var w1)) w1.ReleaseTrigger();
                            }
                            _currentCombat = null;
                            return;
                        }
                        if (_currentCombat != _orderedAction)
                        {
                            _currentCombat = _orderedAction;
                            #if UNITY_EDITOR
                            if (verboseLogs) Debug.Log($"🎖️ [{ai.faction}] Приказ(бой): {_currentCombat.name}");
                            #endif
                        }
                        _currentCombat.Execute(ai);
                    }
                }
                else
                {
                    _orderedAction = null;
                }
            }

            // Обычный выбор по двум каналам
            float bestMoveScore = -1f;
            UtilityAction bestMove = null;
            float bestCombatScore = -1f;
            UtilityAction bestCombat = null;

            foreach (var action in actions)
            {
                if (action == null) continue;
                float score = action.Evaluate(ai);
                if (action.Channel == ActionChannel.Combat)
                {
                    // Пререквизиты боя: должна быть валидная (живая) цель и LOS
                    bool dead = false;
                    if (ai.Target != null)
                    {
                        var t = ai.Target.GetComponent<Black_Orbit.Scripts.Core.Runtime.Health>();
                        dead = t != null && t.IsDead;
                    }
                    if (ai.Target == null || !ai.Target.gameObject.activeInHierarchy || dead || !ai.hasLineOfSight)
                    {
                        continue; // не рассматриваем боевые экшены
                    }
                    if (score > bestCombatScore)
                    {
                        bestCombatScore = score;
                        bestCombat = action;
                    }
                }
                else // Movement/Support
                {
                    if (score > bestMoveScore)
                    {
                        bestMoveScore = score;
                        bestMove = action;
                    }
                }
            }

            if (bestMove != null && _orderedAction?.Channel != ActionChannel.Movement)
            {
                if (_currentMovement != bestMove)
                {
                    _currentMovement = bestMove;
                    #if UNITY_EDITOR
                    if (verboseLogs) Debug.Log($"🚶 [{ai.faction}] Движение: {_currentMovement.name} (Score={bestMoveScore:F2})");
                    #endif
                }
                _currentMovement.Execute(ai);
            }
            else if (_orderedAction?.Channel != ActionChannel.Movement)
            {
                // Ничего не выбрано по движению — очистим текущее
                _currentMovement = null;
            }

            if (bestCombat != null && _orderedAction?.Channel != ActionChannel.Combat)
            {
                if (_currentCombat != bestCombat)
                {
                    _currentCombat = bestCombat;
                    #if UNITY_EDITOR
                    if (verboseLogs) Debug.Log($"🔫 [{ai.faction}] Бой: {_currentCombat.name} (Score={bestCombatScore:F2})");
                    #endif
                }
                _currentCombat.Execute(ai);
            }
            else if (_orderedAction?.Channel != ActionChannel.Combat)
            {
                // Ничего не выбрано по бою — очистим и отпустим спуск оружия
                _currentCombat = null;
                if (ai.TryGetComponent<AIWeaponHandler>(out var handler))
                {
                    handler.ReleaseTrigger();
                }
                else if (ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var weapon))
                {
                    weapon.ReleaseTrigger();
                }
            }
        }

        public void OrderAction(AI ai, string actionName, float duration, UtilityAction[] actions, bool verboseLogs = false)
        {
            if (actions == null) return;
            UtilityAction found = null;
            foreach (var a in actions)
            {
                if (a != null && a.name.Contains(actionName)) { found = a; break; }
            }
            if (found != null)
            {
                _orderedAction = found;
                _orderTimer = duration;
                #if UNITY_EDITOR
                if (verboseLogs) Debug.Log($"📋 [{ai.faction}] Приказ: {actionName} (канал: {found.Channel}) на {duration} сек");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                if (verboseLogs) Debug.LogWarning($"[AI] Действие '{actionName}' не найдено в списке actions!");
                #endif
            }
        }

        public void CancelOrder()
        {
            _orderedAction = null;
            _orderTimer = 0f;
        }
    }
}
