using Black_Orbit.Scripts.AI.Runtime;
using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Подавляющий огонь: бот стреляет по последней известной позиции игрока,
    /// даже если не видит его. Используется для координации с союзниками.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/SuppressionFire", fileName = "SuppressionFire")]
    public class SuppressionFireAction : UtilityAction
    {
        [Header("Параметры подавления")]
        [Tooltip("Максимальное время с последнего обнаружения для подавления (секунды)")]
        public float maxTimeSinceSeen = 3f;
        
        [Tooltip("Кулдаун между выстрелами (секунды)")]
        public float fireCooldown = 0.4f;
        
        [Tooltip("Минимальная дистанция для подавления (метры)")]
        public float minRange = 5f;
        
        private float _cooldown;

        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.Target == null) return new[] { 0f, 0f, 0f };
            
            // Входы для Utility-системы:
            // [0] = недавно видели цель, но сейчас нет LOS (0..1)
            // [1] = на подходящей дистанции (0..1)
            // [2] = есть союзники рядом (1.0 если есть, 0.0 если нет)
            float recentlySeen = ai.timeSinceLastSeen > 0.1f && ai.timeSinceLastSeen < maxTimeSinceSeen ? 1f : 0f;
            float noLOS = ai.hasLineOfSight ? 0f : 1f;
            float combined = recentlySeen * noLOS;
            
            float dist = Vector3.Distance(ai.transform.position, ai.lastSeenTargetPos);
            float inRange = dist > minRange ? 1f : 0f;
            
            // Проверяем наличие союзников
            float hasAllies = 0f;
            if (ai.Squad != null && ai.Squad.AliveCount > 1)
            {
                hasAllies = 1f;
            }
            
            return new[] { combined, inRange, hasAllies };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.Target == null || ai.lastSeenTargetPos == Vector3.zero) return;
            
            // Смотрим на последнюю известную позицию
            ai.LookAt(ai.lastSeenTargetPos);
            ai.Stop(); // Стоим на месте
            
            // Стреляем по кулдауну
            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f)
            {
                // Используем AIWeaponHandler для стрельбы
                if (ai.TryGetComponent<AIWeaponHandler>(out var weaponHandler))
                {
                    weaponHandler.TryFire();
                    Debug.Log($"🔥 [{ai.faction}] Подавляющий огонь по последней позиции!");
                }
                else if (ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var weapon))
                {
                    weapon.TryFire();
                }
                else
                {
                    Debug.Log("🔥 Подавляющий огонь (оружие не найдено)");
                }
                _cooldown = fireCooldown;
            }
        }
    }
}
