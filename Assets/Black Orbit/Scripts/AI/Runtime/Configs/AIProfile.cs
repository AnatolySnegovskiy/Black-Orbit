using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Runtime.Configs
{
    [CreateAssetMenu(fileName = "AIProfile", menuName = "Black Orbit/AI/AI Profile", order = 0)]
    public class AIProfile : ScriptableObject
    {
        [Header("Profile Metadata")] public string profileName = "Default";
        [Tooltip("Версия профиля для миграций")] public int version = 1;

        [Header("Movement Domain")] public MovementDomainConfig Movement = new();
        [Header("Combat Domain")] public CombatDomainConfig Combat = new();
        // В будущем: Combat, Communication, Tactics, Squad
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
}
