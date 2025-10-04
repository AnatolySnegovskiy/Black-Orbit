using Black_Orbit.Scripts.AI.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Поиск укрытия: бот ищет ближайший объект-укрытие и прячется за ним от игрока.
    /// Активируется при низком здоровье, когда игрок близко и есть угроза.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/TakeCover", fileName = "TakeCover")]
    public class TakeCoverAction : UtilityAction
    {
        [Header("Параметры поиска укрытия")]
        [Tooltip("Радиус поиска укрытий вокруг бота (метры)")]
        public float searchRadius = 12f;
        
        [Tooltip("Интервал пересчёта пути к укрытию (секунды)")]
        public float repathInterval = 0.4f;
        
        [Tooltip("Кулдаун стрельбы из укрытия (секунды)")]
        public float fireCooldown = 1.2f;
        
        [Tooltip("Дистанция до укрытия, при которой считаем что достигли (метры)")]
        public float coverReachedDistance = 1.5f;
        
        private float _repathTimer;
        private float _fireCooldown;

        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.Target == null) return new[] { 0f, 0f, 0f, 0f };
            // Входы для Utility-системы:
            // [0] = низкое здоровье (0..1, чем меньше HP, тем выше)
            // [1] = под угрозой (1.0 если цель видит нас, 0.0 если нет)
            // [2] = цель близко (0..1, чем ближе, тем выше)
            // [3] = есть укрытие рядом (1.0 если есть, 0.0 если нет)
            float lowHealth = 1f - ai.HealthNormalized;
            float underThreat = ai.hasLineOfSight ? 1f : 0f;
            float closeToTarget = 1f - Mathf.Clamp01(Vector3.Distance(ai.transform.position, ai.Target.position) / ai.DetectionRange);
            
            // Проверяем наличие укрытия рядом
            Vector3? cover = FindBestCover(ai);
            float hasCover = cover.HasValue ? 1f : 0f;
            
            return new[] { lowHealth, underThreat, closeToTarget, hasCover };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.Target == null) return;
            
            // Ищем лучшее укрытие
            Vector3? cover = FindBestCover(ai);
            if (cover.HasValue)
            {
                float distToCover = Vector3.Distance(ai.transform.position, cover.Value);
                
                // Если еще не достигли укрытия - движемся к нему
                if (distToCover > coverReachedDistance)
                {
                    _repathTimer -= Time.deltaTime;
                    if (_repathTimer <= 0f)
                    {
                        ai.MoveTo(cover.Value);
                        _repathTimer = repathInterval;
                    }
                }
                else
                {
                    // Достигли укрытия - останавливаемся и стреляем
                    ai.Stop();
                    
                    // Стрельба из укрытия
                    _fireCooldown -= Time.deltaTime;
                    if (ai.hasLineOfSight && _fireCooldown <= 0f)
                    {
                        if (ai.TryGetComponent<AIWeaponHandler>(out var weaponHandler))
                        {
                            weaponHandler.TryFire();
                            _fireCooldown = fireCooldown;
                        }
                    }
                }
                
                ai.LookAt(ai.Target.position);
            }
        }

        /// <summary>
        /// Находит лучшее укрытие: объект, который блокирует линию видимости до игрока
        /// </summary>
        private Vector3? FindBestCover(Runtime.AI ai)
        {
            Collider[] hits = Physics.OverlapSphere(ai.transform.position, searchRadius, ai.CoverMask);
            Vector3 bestPos = Vector3.zero;
            float bestScore = -1f;
            
            foreach (var h in hits)
            {
                Vector3 dirToTarget = (ai.Target.position - h.transform.position).normalized;
                // Позиция за укрытием относительно цели
                Vector3 candidate = h.transform.position - dirToTarget * 1.5f;
                if (!NavMesh.SamplePosition(candidate, out var navHit, 1.5f, NavMesh.AllAreas))
                    continue;

                // Проверяем, что укрытие блокирует луч от кандидата до цели
                if (Physics.Raycast(candidate + Vector3.up * 1.6f, (ai.Target.position - candidate).normalized,
                    out RaycastHit rayHit, Mathf.Infinity))
                {
                    // Первое препятствие должно быть укрытием, а не целью
                    if (rayHit.collider != null && rayHit.collider.transform != ai.Target)
                    {
                        float distToTarget = Vector3.Distance(candidate, ai.Target.position);
                        float distFromAI = Vector3.Distance(candidate, ai.transform.position);
                        // Предпочитаем укрытия дальше от цели, но не слишком далеко от нас
                        float score = distToTarget - 0.5f * distFromAI;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestPos = navHit.position;
                        }
                    }
                }
            }
            return bestScore > 0f ? bestPos : (Vector3?)null;
        }
    }
}
