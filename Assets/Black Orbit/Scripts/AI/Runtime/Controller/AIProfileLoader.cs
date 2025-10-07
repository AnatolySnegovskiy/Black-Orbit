using UnityEngine;
using Black_Orbit.Scripts.AI.Runtime.Configs;
using Black_Orbit.Scripts.AI.Runtime.Core;
using Black_Orbit.Scripts.AI.Runtime.Controller;
using Black_Orbit.Scripts.AI.Runtime.Domains;
using Black_Orbit.Scripts.AI.Runtime.Movement;
using Black_Orbit.Scripts.AI.Runtime.Actions.Movement;
using Black_Orbit.Scripts.AI.Runtime.Combat;
using Black_Orbit.Scripts.AI.Runtime.Actions.Combat;
using Black_Orbit.Scripts.AI.Runtime.Actions.Tactics;
using Black_Orbit.Scripts.AI.Runtime.Utility;

namespace Black_Orbit.Scripts.AI.Runtime.Controller
{
    [RequireComponent(typeof(AIController))]
    public class AIProfileLoader : MonoBehaviour
    {
        [Header("Profile")]
        public AIProfile profile;

        private void Start()
        {
            if (profile == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning("[AIProfileLoader] Profile is null. Skipping.");
#endif
                return;
            }

            var ctrl = GetComponent<AIController>();

            // Initialize Utility Curves registry from profile
            if (profile.Curves != null)
            {
                UtilityCurvesRegistry.DistanceToTargetCurve = profile.Curves.distanceToTarget;
                UtilityCurvesRegistry.VisibilityCurve = profile.Curves.visibility;
                UtilityCurvesRegistry.LowHealthCurve = profile.Curves.lowHealth;
                UtilityCurvesRegistry.AmmoLowCurve = profile.Curves.ammoLow;
                UtilityCurvesRegistry.HasAmmoCurve = profile.Curves.hasAmmo;
                UtilityCurvesRegistry.CoverAvailableCurve = profile.Curves.coverAvailable;
                UtilityCurvesRegistry.ExploreNeedCurve = profile.Curves.exploreNeed;
                UtilityCurvesRegistry.GrenadeRangeCurve = profile.Curves.grenadeRange;
            }

            // Movement domain
            if (profile.Movement != null && profile.Movement.enabled)
            {
                ctrl.AddDomainIfMissing(DomainId.Movement);
                var domain = ctrl.GetDomain(DomainId.Movement);

                var motor = GetComponent<AIMovementMotor>();
                if (motor == null)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogWarning("[AIProfileLoader] AIMovementMotor is missing for Movement domain.");
#endif
                }
                else
                {
                    if (profile.Movement.Explore.enabled)
                    {
                        domain.AddAction(new ExploreAreaAction(transform, motor,
                            profile.Movement.Explore.baseWeight,
                            profile.Movement.Explore.radius));
                    }

                    if (profile.Movement.Pursue.enabled)
                    {
                        domain.AddAction(new PursueTargetAction(transform, motor,
                            profile.Movement.Pursue.baseWeight,
                            profile.Movement.Pursue.maxDistance));
                    }

                    if (profile.Movement.Flank.enabled)
                    {
                        domain.AddAction(new FlankEnemyAction(transform, motor,
                            profile.Movement.Flank.baseWeight,
                            profile.Movement.Flank.flankDistance,
                            profile.Movement.Flank.orderBoost));
                    }

                    if (profile.Movement.TakeCover.enabled)
                    {
                        domain.AddAction(new TakeCoverAction(transform, motor,
                            profile.Movement.TakeCover.baseWeight,
                            profile.Movement.TakeCover.searchRadius,
                            profile.Movement.TakeCover.minDistanceToTarget));
                    }
                }
            }

            // Combat domain
            if (profile.Combat != null && profile.Combat.enabled)
            {
                ctrl.AddDomainIfMissing(DomainId.Combat);
                var domain = ctrl.GetDomain(DomainId.Combat);

                var combat = GetComponent<AICombat>();
                var thrower = GetComponent<AIGrenadeThrower>();

                if (profile.Combat.Shoot.enabled)
                {
                    if (combat == null)
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogWarning("[AIProfileLoader] AICombat is missing for Shoot action.");
#endif
                    }
                    else
                    {
                        domain.AddAction(new ShootAction(transform, combat,
                            profile.Combat.Shoot.baseWeight,
                            profile.Combat.Shoot.retreatPenalty,
                            profile.Combat.Shoot.suppressBoost));
                    }
                }

                if (profile.Combat.Reload.enabled)
                {
                    if (combat == null)
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogWarning("[AIProfileLoader] AICombat is missing for Reload action.");
#endif
                    }
                    else
                    {
                        domain.AddAction(new ReloadAction(combat,
                            profile.Combat.Reload.baseWeight,
                            profile.Combat.Reload.lowThreshold,
                            profile.Combat.Reload.highThreshold));
                    }
                }

                if (profile.Combat.ThrowGrenade.enabled)
                {
                    if (thrower == null)
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogWarning("[AIProfileLoader] AIGrenadeThrower is missing for ThrowGrenade action.");
#endif
                    }
                    else
                    {
                        domain.AddAction(new ThrowGrenadeAction(transform, thrower,
                            profile.Combat.ThrowGrenade.baseWeight,
                            profile.Combat.ThrowGrenade.minRange,
                            profile.Combat.ThrowGrenade.maxRange,
                            profile.Combat.ThrowGrenade.cooldown));
                    }
                }
            }

            // Tactics domain
            if (profile.Tactics != null && profile.Tactics.enabled)
            {
                ctrl.AddDomainIfMissing(DomainId.Tactics);
                var domain = ctrl.GetDomain(DomainId.Tactics);

                if (profile.Tactics.RetreatDecision.enabled)
                {
                    domain.AddAction(new RetreatDecisionAction(transform,
                        profile.Tactics.RetreatDecision.baseWeight,
                        profile.Tactics.RetreatDecision.retreatDistance,
                        profile.Tactics.RetreatDecision.preferCover));
                }

                // RetreatMove регистрируется в Movement домене, т.к. это движение
                if (profile.Tactics.RetreatMove.enabled)
                {
                    var motor = GetComponent<AIMovementMotor>();
                    if (motor == null)
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogWarning("[AIProfileLoader] AIMovementMotor is missing for RetreatMove action.");
#endif
                    }
                    else
                    {
                        ctrl.AddDomainIfMissing(DomainId.Movement);
                        var moveDomain = ctrl.GetDomain(DomainId.Movement);
                        moveDomain.AddAction(new Black_Orbit.Scripts.AI.Runtime.Actions.Movement.RetreatMoveAction(transform, motor,
                            profile.Tactics.RetreatMove.baseWeight));
                    }
                }
            }
        }
    }
}
