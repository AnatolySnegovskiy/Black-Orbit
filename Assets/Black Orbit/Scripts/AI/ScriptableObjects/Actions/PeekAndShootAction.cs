using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime;

namespace Black_Orbit.Scripts.AI.ScriptableObjects.Actions
{
    /// <summary>
    /// Короткие «высовывания» из укрытия и огонь. Работает в паре с TakeCoverAction.
    /// Во время peek бот смотрит на цель и стреляет, во время hide — отпускает спуск.
    /// </summary>
    [CreateAssetMenu(menuName = "AI/Actions/PeekAndShoot", fileName = "PeekAndShoot")]
    public class PeekAndShootAction : UtilityAction
    {
        public override ActionChannel Channel => ActionChannel.Combat;

        [Header("Параметры peek/hide")]
        [Tooltip("Длительность высовывания (сек)")]
        public float peekDuration = 0.8f;
        [Tooltip("Длительность укрытия (сек)")]
        public float hideDuration = 1.2f;
        [Tooltip("Скорость поворота во время peek")]
        public float peekTurnSpeed = 10f;

        private float _timer;
        private bool _isPeeking;

        public override float[] GetInputs(Runtime.AI ai)
        {
            // [0] = есть цель (1/0), [1] = нет LOS (требуется peek), [2] = здоровье (0..1)
            float hasTarget = (ai.AttackTarget != null) ? 1f : 0f;
            float needPeek = (ai.AttackTarget != null && !ai.hasLineOfSight) ? 1f : 0f;
            float health = ai.HealthNormalized;
            return new[] { hasTarget, needPeek, health };
        }

        public override void Execute(Runtime.AI ai)
        {
            // Нет валидной цели — ничего не делаем
            if (ai.AttackTarget == null || !ai.IsEngagementAllowed())
            {
                Release(ai);
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _isPeeking = !_isPeeking;
                // Подавление влияет на тайминги: меньше peek, больше hide
                float sup = ai.SuppressionLevel;
                float curPeek = Mathf.Lerp(peekDuration, peekDuration * 0.6f, sup);
                float curHide = Mathf.Lerp(hideDuration, hideDuration * 1.6f, sup);
                _timer = _isPeeking ? Mathf.Max(0.1f, curPeek) : Mathf.Max(0.1f, curHide);
            }

            if (_isPeeking)
            {
                // Фаза peek: смотрим на цель и пытаемся стрелять
                ai.LookAt(ai.AttackTarget.position, peekTurnSpeed);
                if (!CheckFriendlyFire(ai))
                {
                    if (ai.TryGetComponent<AIWeaponHandler>(out var handler)) handler.TryFire();
                    else if (ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var weapon)) weapon.TryFire();
                }
                else
                {
                    Release(ai);
                }
            }
            else
            {
                // Фаза hide: отпускаем спуск
                Release(ai);
            }
        }

        private void Release(Runtime.AI ai)
        {
            if (ai.TryGetComponent<AIWeaponHandler>(out var handler)) handler.ReleaseTrigger();
            else if (ai.TryGetComponent<WeaponSystem.Base.IWeapon>(out var weapon)) weapon.ReleaseTrigger();
        }

        private bool CheckFriendlyFire(Runtime.AI ai)
        {
            if (ai.AttackTarget == null) return true;
            Vector3 origin = ai.transform.position + Vector3.up * 1.6f;
            Vector3 dir = (ai.AttackTarget.position - origin).normalized;
            float dist = Vector3.Distance(origin, ai.AttackTarget.position);
            var hits = Physics.RaycastAll(origin, dir, dist);
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                var t = hit.collider.transform;
                if (t == ai.transform || t == ai.AttackTarget) continue;
                var hitAI = t.GetComponentInParent<Runtime.AI>();
                var hitFaction = t.GetComponentInParent<Black_Orbit.Scripts.Faction.Runtime.FactionMember>();
                if (hitAI != null && ai.faction != null && hitAI.faction != null)
                {
                    if (ai.faction.GetRelationship(hitAI.faction) >= -0.3f) return true; // союз/нейтрал
                }
                else if (hitFaction != null && ai.faction != null && hitFaction.faction != null)
                {
                    if (ai.faction.GetRelationship(hitFaction.faction) >= -0.3f) return true;
                }
            }
            return false;
        }
    }
}
