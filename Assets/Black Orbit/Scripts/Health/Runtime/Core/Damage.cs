using System;
using UnityEngine;

namespace Black_Orbit.Scripts.Health.Runtime.Core
{
    public enum DamageType
    {
        Generic = 0,
        Bullet = 1,
        Explosion = 2,
        Melee = 3,
        Fire = 4,
        Poison = 5,
    }

    [Serializable]
    public struct DamageInfo
    {
        public float amount;
        public DamageType type;
        public Vector3 point;
        public Vector3 normal;
        public Component source; // кто нанес

        public DamageInfo(float amount, DamageType type, Vector3 point, Vector3 normal, Component source)
        {
            this.amount = amount;
            this.type = type;
            this.point = point;
            this.normal = normal;
            this.source = source;
        }
    }
}
