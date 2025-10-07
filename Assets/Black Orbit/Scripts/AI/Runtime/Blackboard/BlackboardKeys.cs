using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Core;

namespace Black_Orbit.Scripts.AI.Runtime.Blackboard
{
    public static class BlackboardKeys
    {
        // Target
        public static readonly BlackboardKey<Vector3> TargetPosition = new("Target.Position");
        public static readonly BlackboardKey<float> TargetVisibility = new("Target.Visibility");

        // Self
        public static readonly BlackboardKey<float> SelfHealth = new("Self.Health");
        public static readonly BlackboardKey<int> SelfAmmo = new("Self.Ammo");
        public static readonly BlackboardKey<bool> SelfInCover = new("Self.InCover");
        public static readonly BlackboardKey<Vector3> SelfExplorePoint = new("Self.ExplorePoint");
        public static readonly BlackboardKey<bool> SelfRetreating = new("Self.Retreating");
        public static readonly BlackboardKey<Vector3> SelfRetreatPoint = new("Self.RetreatPoint");

        // Squad
        public static readonly BlackboardKey<SquadOrder> SquadOrderKey = new("Squad.Orders");

        // Environment
        // Для упрощения: здесь могли бы быть списки, но сериализация в BB не требуется
        public static readonly BlackboardKey<System.Collections.Generic.List<Vector3>> EnvironmentCoverPoints = new("Environment.CoverPoints");
    }
}
