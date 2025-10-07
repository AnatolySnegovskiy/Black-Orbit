using System;

namespace Black_Orbit.Scripts.AI.Runtime.Core
{
    [Flags]
    public enum ExecutionType
    {
        Exclusive = 1 << 0,
        Parallel = 1 << 1,
        Overlay = 1 << 2,
        Background = 1 << 3,
    }

    public enum DomainId
    {
        Movement = 0,
        Combat = 1,
        Communication = 2,
        Tactics = 3,
        Squad = 4,
    }

    public enum SquadOrder
    {
        None = 0,
        FlankLeft,
        FlankRight,
        Suppress,
        HoldPosition,
        Regroup
    }
}
