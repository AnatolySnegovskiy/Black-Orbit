using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Runtime.Configs
{
    [CreateAssetMenu(fileName = "AIProfile", menuName = "Black Orbit/AI/AI Profile", order = 0)]
    public class AIProfile : ScriptableObject
    {
        [Header("Profile Metadata")] public string profileName = "Default";
        [Tooltip("Версия профиля для миграций")] public int version = 1;

        [Header("Utility Curves")] public UtilityCurvesConfig Curves = new();

        [Header("Movement Domain")] public MovementDomainConfig Movement = new();
        [Header("Combat Domain")] public CombatDomainConfig Combat = new();
        [Header("Tactics Domain")] public TacticsDomainConfig Tactics = new();
        // В будущем: Combat, Communication, Tactics, Squad
    }

    [System.Serializable]
    public class UtilityCurvesConfig
    {
        [Tooltip("DistanceToTarget: вход t=0..1 (0 близко, 1 далеко)")]
        public AnimationCurve distanceToTarget = AnimationCurve.Linear(0, 1, 1, 0);

        [Tooltip("Visibility: вход t=0..1 (0 невидимо, 1 отлично видно)")]
        public AnimationCurve visibility = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("LowHealth: вход t=0..1 (0 нет эффекта, 1 критически низко)")]
        public AnimationCurve lowHealth = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("AmmoLow: вход t=0..1 (0 нормально, 1 мало патронов)")]
        public AnimationCurve ammoLow = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("HasAmmo: вход t=0..1 (0 нет, 1 есть патроны)")]
        public AnimationCurve hasAmmo = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("CoverAvailable: вход t=0..1 (0 нет укрытия рядом, 1 есть близкое укрытие)")]
        public AnimationCurve coverAvailable = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("ExploreNeed: вход t=0..1 (0 низкая потребность, 1 высокая)")]
        public AnimationCurve exploreNeed = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("GrenadeRange: вход t=0..1 (0 вне диапазона, 1 оптимальная дистанция)")]
        public AnimationCurve grenadeRange = AnimationCurve.Linear(0, 0, 1, 1);
    }

    [System.Serializable]
    public class MovementDomainConfig
    {
        public bool enabled = true;

        public ExploreSettings Explore = new();
        public PursueSettings Pursue = new();
        public FlankSettings Flank = new();
        public TakeCoverSettings TakeCover = new();
    }

    [System.Serializable]
    public class ExploreSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 0.6f;
        public float radius = 30f;
    }

    [System.Serializable]
    public class PursueSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 0.9f;
        public float maxDistance = 60f;
    }

    [System.Serializable]
    public class FlankSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 0.8f;
        public float flankDistance = 12f;
        [Tooltip("Мультипликатор при приказе FlankLeft/Right")] [Range(0.1f,3f)] public float orderBoost = 1.25f;
    }

    [System.Serializable]
    public class TakeCoverSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 0.85f;
        public float searchRadius = 25f;
        public float minDistanceToTarget = 6f;
    }

    [System.Serializable]
    public class CombatDomainConfig
    {
        public bool enabled = true;

        public ShootSettings Shoot = new();
        public ReloadSettings Reload = new();
        public ThrowGrenadeSettings ThrowGrenade = new();
    }

    [System.Serializable]
    public class ShootSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 1.0f;
        [Tooltip("Мультипликатор веса стрельбы при отступлении (0..1)")]
        [Range(0f,1f)] public float retreatPenalty = 0.5f;
        [Tooltip("Мультипликатор при приказе Suppress")] [Range(0.1f,3f)] public float suppressBoost = 1.2f;
    }

    [System.Serializable]
    public class ReloadSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 0.9f;
        public int lowThreshold = 5;
        public int highThreshold = 20;
    }

    [System.Serializable]
    public class ThrowGrenadeSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 0.5f;
        public float minRange = 6f;
        public float maxRange = 20f;
        public float cooldown = 6f;
    }

    [System.Serializable]
    public class TacticsDomainConfig
    {
        public bool enabled = true;

        public RetreatDecisionSettings RetreatDecision = new();
        public RetreatMoveSettings RetreatMove = new();
    }

    [System.Serializable]
    public class RetreatDecisionSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 1.0f;
        [Tooltip("Порог низкого здоровья (Self.Health <= critical -> 1.0)")]
        [Range(0f,1f)] public float critical = 0.3f;
        [Tooltip("Зона спада эффекта LowHealth (Self.Health >= max -> 0.0)")]
        [Range(0f,1f)] public float max = 0.7f;
        [Tooltip("Дистанция отступления от цели, м")] public float retreatDistance = 15f;
        [Tooltip("Предпочитать укрытия при выборе точки")] public bool preferCover = true;
    }

    [System.Serializable]
    public class RetreatMoveSettings
    {
        public bool enabled = true;
        [Range(0f, 2f)] public float baseWeight = 1.1f;
    }
}
