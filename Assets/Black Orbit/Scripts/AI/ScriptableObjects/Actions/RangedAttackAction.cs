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
        [Header("Параметры дальней атаки")]
        [Tooltip("Предпочтительная дистанция для стрельбы (метры)")]
        public float preferredRange = 12f;
        
        [Tooltip("Минимальная дистанция — если игрок ближе, отступаем (метры)")]
        public float minRange = 4f;
        
        [Tooltip("Кулдаун между выстрелами (секунды)")]
        public float fireCooldown = 0.6f;
        
        private float _cooldown;

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
            if (ai.Target == null) return;
            float dist = Vector3.Distance(ai.transform.position, ai.Target.position);

            // Поддерживаем оптимальную дистанцию
            if (dist > preferredRange)
            {
                // Слишком далеко — приближаемся
                ai.MoveTo(ai.Target.position);
            }
            else if (dist < minRange)
            {
                // Слишком близко — отступаем
                Vector3 away = (ai.transform.position - ai.Target.position).normalized;
                ai.MoveTo(ai.transform.position + away * 2f);
            }
            else
            {
                // В оптимальной зоне — стоим и стреляем
                ai.Stop();
            }

            ai.LookAt(ai.Target.position);

            // Стрельба по кулдауну
            _cooldown -= Time.deltaTime;
            if (ai.hasLineOfSight && _cooldown <= 0f)
            {
                // Проверка дружественного огня (дополнительная защита)
                if (CheckFriendlyFire(ai))
                {
                    // Не стреляем, но и не логируем - utility уже должна быть низкой
                    return;
                }
                
                // Используем AIWeaponHandler для стрельбы
                if (ai.TryGetComponent<AIWeaponHandler>(out var weaponHandler))
                {
                    weaponHandler.TryFire();
                }
                else if (ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var weapon))
                {
                    // Fallback: прямое использование IWeapon
                    weapon.TryFire();
                }
                else
                {
                    Debug.Log("🔫 Дальняя атака (оружие не найдено)");
                }
                _cooldown = fireCooldown;
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
