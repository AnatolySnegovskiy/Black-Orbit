using UnityEngine;
using UnityEngine.AI;

namespace Black_Orbit.Scripts.AI.Runtime.Cover
{
    /// <summary>
    /// Простой поиск укрытий по маске: находим точки на поверхности коллайдеров, проверяем закрытость от цели.
    /// v1: сэмплируем окружность вокруг AI и ищем точки, где Ray от цели до точки перекрывается препятствием.
    /// </summary>
    public static class CoverService
    {
        /// <summary>
        /// Ищет лучшую точку укрытия вокруг AI относительно позиции цели.
        /// </summary>
        /// <param name="ai">Искатель укрытия</param>
        /// <param name="targetPos">Позиция угрозы (цели)</param>
        /// <param name="radius">Радиус поиска</param>
        /// <param name="samples">Количество сэмплов окружности</param>
        /// <returns>true, если найдена точка; out bestPoint</returns>
        public static bool FindBestCover(Black_Orbit.Scripts.AI.Runtime.AI ai, Vector3 targetPos, float radius, int samples, out Vector3 bestPoint)
        {
            bestPoint = Vector3.positiveInfinity;
            if (ai == null || ai.gameObject == null) return false;
            if (radius <= 0f) radius = 5f;
            if (samples < 8) samples = 8;

            var origin = ai.transform.position;
            var up = Vector3.up;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < samples; i++)
            {
                float angle = (360f * i) / samples;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                // Ищем препятствие между целью и этой дугой окружности: луч от цели в направлении на сэмпл
                Vector3 dirFromThreat = (origin + dir * radius - targetPos).normalized;
                float maxDist = Vector3.Distance(targetPos, origin) + radius;
                if (Physics.Raycast(targetPos + up * 1.6f, dirFromThreat, out var blockHit, maxDist, ai.ObstacleMask | ai.CoverMask))
                {
                    // Ставим точку за препятствием по нормали
                    Vector3 coverBase = blockHit.point + blockHit.normal * 1.0f;
                    // Добавляем небольшое латеральное смещение вдоль касательной, выбирая сторону ближе к AI
                    Vector3 threatDir = (targetPos - blockHit.point).normalized;
                    Vector3 tangent = Vector3.Cross(up, threatDir).normalized; // левая/правая относительно угрозы
                    float sideSign = Mathf.Sign(Vector3.Dot((origin - blockHit.point).normalized, tangent));
                    Vector3 lateral = tangent * sideSign * 0.6f;
                    Vector3 candidate = coverBase + lateral;

                    // Привязываем к NavMesh
                    if (NavMesh.SamplePosition(candidate, out var navHit, 1.5f, NavMesh.AllAreas))
                    {
                        candidate = navHit.position;
                    }
                    else
                    {
                        // Попытка опустить на землю
                        if (Physics.Raycast(candidate + up * 2f, Vector3.down, out var groundHit, 5f, ~0))
                            candidate = groundHit.point;
                    }

                    // Оценка: точка между угрозой и AI, близко к AI, с хорошей нормалью
                    float facing = Vector3.Dot((origin - candidate).normalized, (targetPos - candidate).normalized);
                    float distToAI = Vector3.Distance(origin, candidate);
                    float distNorm = Mathf.Clamp01(1f - distToAI / radius);
                    float normAlign = Mathf.Clamp01(Vector3.Dot(blockHit.normal, (candidate - blockHit.point).normalized) * 0.5f + 0.5f);
                    float score = facing * 1.0f + distNorm * 0.5f + normAlign * 0.25f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPoint = candidate;
                    }
                }
            }

            return bestPoint != Vector3.positiveInfinity;
        }
    }
}
