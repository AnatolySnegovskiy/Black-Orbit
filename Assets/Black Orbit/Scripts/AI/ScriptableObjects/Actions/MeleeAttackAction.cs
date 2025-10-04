using UnityEngine;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Ближняя атака: бот сближается с игроком и наносит удары в ближнем бою.
    /// Активируется когда игрок очень близко и есть прямая видимость.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/MeleeAttack", fileName = "MeleeAttack")]
    public class MeleeAttackAction : UtilityAction
    {
        [Header("Параметры ближней атаки")]
        [Tooltip("Дистанция удара — максимальная дальность для нанесения урона (метры)")]
        public float strikeRange = 2.2f;

        [Tooltip("Кулдаун между ударами (секунды)")]
        public float swingCooldown = 0.8f;

        private float _cooldown;

        public override float[] GetInputs(Runtime.AI ai)
        {
            if (ai.Target == null) return new[] { 0f, 0f };
            // Входы для Utility-системы:
            // [0] = цель близко (0..1, чем ближе, тем выше)
            // [1] = есть прямая видимость (1.0 если видим, 0.0 если нет)
            float dist = Vector3.Distance(ai.transform.position, ai.Target.position);
            float close = 1f - Mathf.Clamp01(dist / strikeRange);
            float los = ai.hasLineOfSight ? 1f : 0f;
            return new[] { close, los };
        }

        public override void Execute(Runtime.AI ai)
        {
            if (ai.Target == null) return;
            float dist = Vector3.Distance(ai.transform.position, ai.Target.position);
            
            if (dist > strikeRange * 0.9f)
            {
                // Сближаемся с целью
                ai.MoveTo(ai.Target.position);
            }
            else
            {
                // В радиусе удара — останавливаемся и атакуем
                ai.Stop();
                ai.LookAt(ai.Target.position);
                _cooldown -= Time.deltaTime;
                if (_cooldown <= 0f)
                {
                    // TODO: подключить систему оружения/анимацию удара
                    Debug.Log("⚔️ Ближняя атака");
                    _cooldown = swingCooldown;
                }
            }
        }
    }
}