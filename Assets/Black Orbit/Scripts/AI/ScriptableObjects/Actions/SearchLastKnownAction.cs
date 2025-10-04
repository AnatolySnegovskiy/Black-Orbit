using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Поиск на последней известной позиции: бот идёт туда, где последний раз видел игрока, и сканирует окружение.
    /// Активируется когда игрок недавно был замечен, но сейчас нет прямой видимости.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/SearchLastKnown", fileName = "SearchLastKnown")]
    public class SearchLastKnownAction : UtilityAction
    {
        [Header("Параметры поиска")]
        [Tooltip("Радиус достижения последней позиции игрока (метры)")]
        public float tolerance = 0.7f;
        
        [Tooltip("Время сканирования окружения на месте (секунды)")]
        public float scanTime = 3f;
        
        private float _scanTimer;

        public override float[] GetInputs(Runtime.AI ai)
        {
            // Входы для Utility-системы:
            // [0] = цель была замечена недавно (1.0 сразу после потери, 0.0 через 5+ сек)
            // [1] = нет прямой видимости (1.0 если нет LOS, 0.0 если есть)
            float seenRecently = 1f - Mathf.Clamp01(ai.timeSinceLastSeen / 5f);
            float noLOS = ai.hasLineOfSight ? 0f : 1f;
            return new[] { seenRecently, noLOS };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.timeSinceLastSeen < 0.1f)
            {
                // Только что видел цель, ничего не делаем
                return;
            }

            // Идём к последней известной позиции
            ai.MoveTo(ai.lastSeenTargetPos);
            ai.LookAt(ai.lastSeenTargetPos);

            if (Vector3.Distance(ai.transform.position, ai.lastSeenTargetPos) <= tolerance)
            {
                _scanTimer += Time.deltaTime;
                // Медленно вращаемся, сканируя окружение
                ai.transform.Rotate(Vector3.up, 60f * Time.deltaTime);
                if (_scanTimer >= scanTime)
                {
                    _scanTimer = 0f; // Позволяем другим действиям взять управление
                }
            }
            else
            {
                _scanTimer = 0f;
            }
        }
    }
}
