using Black_Orbit.Scripts.AI.Runtime;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Дальняя атака: бот держит оптимальную дистанцию и стреляет по игроку из дальнобойного оружия.
    /// Активируется когда игрок в зоне видимости на средней дистанции.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/RangedAttack", fileName = "RangedAttack")]
    public class RangedAttackAction : UtilityAction
    {
        public override ActionChannel Channel => ActionChannel.Combat;
        [Header("Параметры дальней атаки")]
        [Tooltip("Предпочтительная дистанция для стрельбы (метры)")]
        public float preferredRange = 12f;
        
        [Tooltip("Минимальная дистанция — если игрок ближе, отступаем (метры)")]
        public float minRange = 4f;
        
        [Tooltip("Кулдаун между выстрелами (секунды)")]
        public float fireCooldown = 0.6f;
        
        private float _cooldown; // не используем для авто-огня, пусть ROF контролирует оружие

        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.Target == null) return new[] { 0f, 0f, 0f, 0f };
            // Входы для Utility-системы:
            // [0] = находимся на предпочтительной дистанции (0..1, пик на preferredRange)
            // [1] = есть прямая видимость (1.0 если видим, 0.0 если нет)
            // [2] = уровень здоровья (0..1)
            // [3] = линия огня чиста (1.0 если чисто, 0.0 если союзник на пути)
            float dist = Vector3.Distance(ai.transform.position, ai.Target.position);
            float inPreferred = 1f - Mathf.Clamp01(Mathf.Abs(dist - preferredRange) / preferredRange);
            float los = ai.hasLineOfSight ? 1f : 0f;
            float health = ai.HealthNormalized;
            float clearShot = CheckFriendlyFire(ai) ? 0f : 1f; // 0 если союзник на пути
            return new[] { inPreferred, los, health, clearShot };
        }

        public override void Execute(Runtime.AI ai)
        {
            // Попробуем получить оружие заранее для отпускания спуска при любых отказах
            ai.TryGetComponent<AIWeaponHandler>(out var handler);
            ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var weaponIface);

            if (ai.Target == null || !ai.Target.gameObject.activeInHierarchy)
            {
                handler?.ReleaseTrigger();
                weaponIface?.ReleaseTrigger();
                return;
            }
            // Если у цели есть здоровье и оно на нуле — не стреляем
            var targetHealth = ai.Target.GetComponent<Black_Orbit.Scripts.Core.Runtime.Health>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                handler?.ReleaseTrigger();
                weaponIface?.ReleaseTrigger();
                return;
            }
            if (!ai.hasLineOfSight)
            {
                handler?.ReleaseTrigger();
                weaponIface?.ReleaseTrigger();
                return; // не видим цель — не стреляем
            }

            // Боевой канал: не управляем перемещением, только прицел и стрельба
            ai.LookAt(ai.Target.position);

            // Проверка дружественного огня (дополнительная защита)
            if (CheckFriendlyFire(ai))
            {
                handler?.ReleaseTrigger();
                weaponIface?.ReleaseTrigger();
                return;
            }

            // Держим "спуск" активным — для авто оружия будет непрерывный огонь, ROF контролирует само оружие
            if (handler != null)
            {
                handler.TryFire();
            }
            else if (weaponIface != null)
            {
                weaponIface.TryFire();
            }
            else
            {
                Debug.Log("🔫 Дальняя атака (оружие не найдено)");
            }
        }
        
        /// <summary>
        /// Проверяет есть ли союзник или нейтрал на линии огня
        /// </summary>
        bool CheckFriendlyFire(Runtime.AI ai)
        {
            if (ai.Target == null) return true;
            
            Vector3 origin = ai.transform.position + Vector3.up * 1.6f; // Уровень оружия
            Vector3 direction = (ai.Target.position - origin).normalized;
            float distance = Vector3.Distance(origin, ai.Target.position);
            
            // Raycast до цели
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);
            
            foreach (var hit in hits)
            {
                // Пропускаем себя и цель
                if (hit.collider.transform == ai.transform || hit.collider.transform == ai.Target)
                    continue;
                
                // Проверяем есть ли AI или FactionMember на пути
                var hitAI = hit.collider.GetComponentInParent<Runtime.AI>();
                var hitFactionMember = hit.collider.GetComponentInParent<Black_Orbit.Scripts.Faction.Runtime.FactionMember>();
                
                if (hitAI != null)
                {
                    // Проверяем отношения с этим AI
                    if (ai.faction != null && hitAI.faction != null)
                    {
                        float relationship = ai.faction.GetRelationship(hitAI.faction);
                        
                        // Если союзник (>= 0.3) или нейтрал (>= -0.3) - не стреляем
                        if (relationship >= -0.3f)
                        {
                            return true; // Дружественный огонь!
                        }
                    }
                }
                else if (hitFactionMember != null)
                {
                    // Проверяем отношения с FactionMember (игрок, NPC)
                    if (ai.faction != null && hitFactionMember.faction != null)
                    {
                        float relationship = ai.faction.GetRelationship(hitFactionMember.faction);
                        
                        // Если союзник или нейтрал - не стреляем
                        if (relationship >= -0.3f)
                        {
                            return true; // Дружественный огонь!
                        }
                    }
                }
            }
            
            return false; // Линия огня чиста
        }
    }
}
