using System;
using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Configs;

namespace Black_Orbit.Scripts.AI.Runtime.Utility
{
    [Serializable]
    public sealed class UtilityCurveSet
    {
        public AnimationCurve DistanceToTargetCurve { get; private set; }
        public AnimationCurve VisibilityCurve { get; private set; }
        public AnimationCurve LowHealthCurve { get; private set; }
        public AnimationCurve AmmoLowCurve { get; private set; }
        public AnimationCurve HasAmmoCurve { get; private set; }
        public AnimationCurve CoverAvailableCurve { get; private set; }
        public AnimationCurve ExploreNeedCurve { get; private set; }
        public AnimationCurve GrenadeRangeCurve { get; private set; }

        public UtilityCurveSet()
        {
            Apply(new UtilityCurvesConfig());
        }

        public void Apply(UtilityCurvesConfig config)
        {
            if (config == null)
            {
                config = new UtilityCurvesConfig();
            }

            DistanceToTargetCurve = Clone(config.distanceToTarget);
            VisibilityCurve = Clone(config.visibility);
            LowHealthCurve = Clone(config.lowHealth);
            AmmoLowCurve = Clone(config.ammoLow);
            HasAmmoCurve = Clone(config.hasAmmo);
            CoverAvailableCurve = Clone(config.coverAvailable);
            ExploreNeedCurve = Clone(config.exploreNeed);
            GrenadeRangeCurve = Clone(config.grenadeRange);
        }

        public float Evaluate(AnimationCurve curve, float t)
        {
            t = Mathf.Clamp01(t);
            return curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
        }

        private static AnimationCurve Clone(AnimationCurve source)
        {
            if (source == null) return null;
            var clone = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return clone;
        }
    }
}
