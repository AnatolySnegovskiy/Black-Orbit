using Black_Orbit.Scripts.AI.Runtime;
using Black_Orbit.Scripts.AI.Runtime.Cover;
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
        public override ActionChannel Channel => ActionChannel.Movement;
        [Header("Параметры поиска укрытия")]
        [Tooltip("Радиус поиска укрытий вокруг бота (метры)")]
        public float searchRadius = 12f;
        [Tooltip("Количество сэмплов по окружности при поиске укрытия")]
        public int samples = 24;
        
        [Tooltip("Интервал пересчёта пути к укрытию (секунды)")]
        public float repathInterval = 0.4f;
        
        [Tooltip("Дистанция до укрытия, при которой считаем что достигли (метры)")]
        public float coverReachedDistance = 1.5f;

        [Tooltip("Предпочитать укрытие, когда нас видят (повышает оценку экшена при LOS)")]
        public bool preferWhenUnderThreat = true;
        
        [Header("Кэш укрытия")]
        [Tooltip("Время удержания выбранной точки укрытия (сек), чтобы не прыгать между ближайшими")]
        public float coverCacheDuration = 1.5f;
        
        private float _repathTimer;
        private Vector3 _cachedCover = Vector3.positiveInfinity;
        private float _coverCacheTimer;

        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.AttackTarget == null && float.IsPositiveInfinity(ai.NavTargetPos.x))
                return new[] { 0f, 0f, 0f, 0f };
            // Входы для Utility-системы:
            // [0] = низкое здоровье (0..1, чем меньше HP, тем выше)
            // [1] = под угрозой (1.0 если цель видит нас, 0.0 если нет)
            // [2] = цель близко (0..1, чем ближе, тем выше)
            // [3] = есть укрытие рядом (1.0 если есть, 0.0 если нет)
            float lowHealth = 1f - ai.HealthNormalized;
            float underThreat = ai.hasLineOfSight ? 1f : 0f;
            Vector3 refPos = ai.AttackTarget != null ? ai.AttackTarget.position : ai.NavTargetPos;
            float closeToTarget = 1f - Mathf.Clamp01(Vector3.Distance(ai.transform.position, refPos) / Mathf.Max(1f, ai.DetectionRange));
            
            // Проверяем наличие укрытия рядом
            Vector3 coverPoint;
            bool hasCover = CoverService.FindBestCover(ai, refPos, searchRadius, samples, out coverPoint);
            float hasCoverF = hasCover ? 1f : 0f;
            
            // Если предпочитаем при угрозе — немного бустим вход при LOS
            if (preferWhenUnderThreat && underThreat > 0.5f)
                closeToTarget = Mathf.Clamp01(closeToTarget + 0.15f);

            // Подавление повышает приоритет укрытия
            if (ai.SuppressionLevel > 0f)
            {
                float sup = ai.SuppressionLevel;
                closeToTarget = Mathf.Clamp01(closeToTarget + 0.25f * sup);
                hasCoverF = Mathf.Clamp01(hasCoverF + 0.25f * sup);
            }

            return new[] { lowHealth, underThreat, closeToTarget, hasCoverF };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.AttackTarget == null && float.IsPositiveInfinity(ai.NavTargetPos.x)) return;
            
            // Ищем лучшее укрытие
            Vector3 refPos = ai.AttackTarget != null ? ai.AttackTarget.position : ai.NavTargetPos;
            // Обновляем таймер кэша
            if (_coverCacheTimer > 0f) _coverCacheTimer -= Time.deltaTime;

            bool haveCover = false;
            Vector3 cover = _cachedCover;

            // Если есть актуальный кэш и он ещё валиден — используем его
            if (_coverCacheTimer > 0f && !float.IsPositiveInfinity(_cachedCover.x) && IsCoverValid(ai, refPos, _cachedCover))
            {
                haveCover = true;
                cover = _cachedCover;
            }
            else if (CoverService.FindBestCover(ai, refPos, searchRadius, samples, out cover))
            {
                // Сохраняем новый кэш
                _cachedCover = cover;
                _coverCacheTimer = Mathf.Max(0.2f, coverCacheDuration);
                haveCover = true;
            }

            if (haveCover)
            {
                float distToCover = Vector3.Distance(ai.transform.position, cover);
                
                // Если еще не достигли укрытия - движемся к нему
                if (distToCover > coverReachedDistance)
                {
                    _repathTimer -= Time.deltaTime;
                    if (_repathTimer <= 0f)
                    {
                        ai.MoveTo(cover);
                        _repathTimer = repathInterval;
                    }
                }
                else
                {
                    // Достигли укрытия — останавливаемся и занимаем позицию (стрельба вынесена в PeekAndShootAction)
                    ai.Stop();
                }
                // Смотрим в сторону цели/направления угрозы
                ai.LookAt(refPos);
            }
        }

        private bool IsCoverValid(Runtime.AI ai, Vector3 threatPos, Vector3 coverPoint)
        {
            // Проверяем, что укрытие по-прежнему блокирует прямую видимость от угрозы
            Vector3 toCandidate = coverPoint - threatPos;
            float dist = toCandidate.magnitude;
            if (dist < 0.2f) return false;
            Vector3 rayDir = toCandidate.normalized;
            return Physics.Raycast(threatPos + Vector3.up * 1.6f, rayDir, dist, ai.ObstacleMask | ai.CoverMask);
        }
    }
}
