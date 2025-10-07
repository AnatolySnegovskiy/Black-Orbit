using UnityEngine;

namespace Black_Orbit.Scripts.AI.Runtime.Utility
{
    public static class UtilityCurvesRegistry
    {
        public static AnimationCurve DistanceToTargetCurve;
        public static AnimationCurve VisibilityCurve;
        public static AnimationCurve LowHealthCurve;
        public static AnimationCurve AmmoLowCurve;
        public static AnimationCurve HasAmmoCurve;
        public static AnimationCurve CoverAvailableCurve;
        public static AnimationCurve ExploreNeedCurve;
        public static AnimationCurve GrenadeRangeCurve;

        // Безопасная выборка: если кривая не задана — возвращаем t как есть (линейно)
        public static float Eval(AnimationCurve curve, float t)
        {
            t = Mathf.Clamp01(t);
            return curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
        }
    }
}
