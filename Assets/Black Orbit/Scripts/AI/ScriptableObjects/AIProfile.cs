using UnityEngine;
using Black_Orbit.Scripts.AI.ScriptableObjects.Actions;

namespace Black_Orbit.Scripts.AI.ScriptableObjects
{
    /// <summary>
    /// Профиль настроек AI для дизайнеров. Хранит параметры движения, восприятия, боя,
    /// отладочные флаги и набор действий. Может применяться к существующим Runtime.AI.
    /// </summary>
    [CreateAssetMenu(menuName = "Black Orbit/AI/AI Profile", fileName = "AIProfile")]
    public class AIProfile : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float rotationSpeed = 5f;

        [Header("Perception")]
        public float detectionRange = 20f;
        [Range(0f, 180f)] public float visionAngle = 120f;
        public float visionRange = 30f;
        public LayerMask obstacleMask = Physics.DefaultRaycastLayers;
        public LayerMask coverMask = Physics.DefaultRaycastLayers;
        public float targetSearchInterval = 0.8f;

        [Header("Combat Ranges")]
        public float meleeAttackRange = 2f;
        public float rangedAttackRange = 25f;

        [Header("Health & Misc")] 
        [Tooltip("Начальное здоровье, выставляется при применении профиля")] public float initialHP = 100f;

        [Header("Debug & Gizmos")] 
        public bool showGizmosVision = true;
        public bool showGizmosDetection = true;
        public bool showGizmosAttackRanges = true;
        public bool showGizmosLastSeen = true;
        public bool showGizmosNavTarget = true;
        public bool showGizmosInfoLabel = true;

        [Header("Utility Actions (Set)")]
        public UtilityAction[] actions;

        /// <summary>
        /// Применить профиль к указанному экземпляру Runtime.AI.
        /// </summary>
        public void ApplyTo(Black_Orbit.Scripts.AI.Runtime.AI ai, bool applyActions = true, bool applyDebug = true)
        {
            if (ai == null) return;

            // Параметры ядра через InitializeParameters
            ai.InitializeParameters(
                moveSpd: moveSpeed,
                rotSpd: rotationSpeed,
                detectRange: detectionRange,
                meleeRange: meleeAttackRange,
                rangedRange: rangedAttackRange,
                visAngle: visionAngle,
                visRange: visionRange,
                hp: initialHP,
                targetSearchInt: targetSearchInterval
            );

            // Маски восприятия/укрытий и интервал поиска цели
            // Эти поля находятся в AI как сериализованные, поэтому прямая установка допустима
            // (через публичные свойства/поля, если доступны)
            // Обновление Search Interval продублируем явно
            ai.TargetSearchInterval = targetSearchInterval;

            // Прямой доступ к маскам: через публичные свойства нет сеттера, поэтому используем SerializedObject, если в редакторе.
#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(ai);
            var obstacle = so.FindProperty("obstacleMask");
            var cover = so.FindProperty("coverMask");
            if (obstacle != null) obstacle.intValue = obstacleMask.value;
            if (cover != null) cover.intValue = coverMask.value;
            so.ApplyModifiedPropertiesWithoutUndo();
#else
            // В рантайме прямого сеттера нет, маски остаются как есть на объекте
#endif

            // Debug-флаги
            if (applyDebug)
            {
#if UNITY_EDITOR
                var soDbg = new UnityEditor.SerializedObject(ai);
                TrySetBool(soDbg, "showGizmosVision", showGizmosVision);
                TrySetBool(soDbg, "showGizmosDetection", showGizmosDetection);
                TrySetBool(soDbg, "showGizmosAttackRanges", showGizmosAttackRanges);
                TrySetBool(soDbg, "showGizmosLastSeen", showGizmosLastSeen);
                TrySetBool(soDbg, "showGizmosNavTarget", showGizmosNavTarget);
                TrySetBool(soDbg, "showGizmosInfoLabel", showGizmosInfoLabel);
                soDbg.ApplyModifiedPropertiesWithoutUndo();
#else
                // В рантайме флаги оставить как есть
#endif
            }

            // Набор действий
            if (applyActions && actions != null && actions.Length > 0)
            {
                ai.SetActions(actions);
            }
        }

#if UNITY_EDITOR
        private static void TrySetBool(UnityEditor.SerializedObject so, string propName, bool value)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.boolValue = value;
        }
#endif
    }
}
