using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
    public class CompAbilityEffect_WyvernFireBurner : CompAbilityEffect
    {
        public new CompProperties_AbilityWyvernFireBurner Props
        {
            get
            {
                return (CompProperties_AbilityWyvernFireBurner)this.props;
            }
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (!ModsConfig.AnomalyActive || ThingDefOf.IncineratorSpray == null)
            {
                yield break;
            }
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo a, LocalTargetInfo _)
                {
                    Vector3 drawPos = this.parent.pawn.DrawPos;
                    IntVec3 intVec = drawPos.Yto0().ToIntVec3();
                    Map map = this.parent.pawn.Map;
                    IncineratorSpray incineratorSpray = GenSpawn.Spawn(ThingDefOf.IncineratorSpray, intVec, map, WipeMode.Vanish) as IncineratorSpray;
                    if (incineratorSpray == null)
                    {
                        return;
                    }
                    int numStreams = this.Props.numStreams;
                    Vector3 normalized = (a.CenterVector3 - drawPos).normalized;
                    Func<IntVec3, bool> losValidator = (IntVec3 c) => c.CanBeSeenOverFast(map);

                    for (int i = 0; i < numStreams; i++)
                    {
                        float angle = Rand.Range(-this.Props.coneSizeDegrees, this.Props.coneSizeDegrees);
                        Vector3 vector = normalized.RotatedBy(angle);
                        Vector3 vect = drawPos + vector * (this.Props.range + Rand.Value * this.Props.rangeNoise);
                        IntVec3 start = intVec;
                        IntVec3 end = vect.ToIntVec3();
                        IntVec3 intVec2 = GenSight.LastPointOnLineOfSight(start, end, losValidator, true);
                        if (!intVec2.IsValid)
                        {
                            intVec2 = vect.ToIntVec3();
                        }
                        float num = Vector3.Distance(intVec2.ToVector3(), drawPos);
                        float num2 = Mathf.Clamp01(num / this.Props.sizeReductionDistanceThreshold);
                        if (Vector3.Dot((intVec2.ToVector3() - drawPos).normalized, vector) > 0.5f)
                        {
                            ThingDef moteDef = this.Props.moteDef ?? ThingDefOf.Mote_IncineratorBurst;
                            MoteDualAttached mote = MoteMaker.MakeInteractionOverlay(moteDef, new TargetInfo(intVec, map, false), new TargetInfo(intVec2, map, false));
                            incineratorSpray.Add(new IncineratorProjectileMotion
                            {
                                mote = mote,
                                targetDest = a.Cell,
                                worldSource = drawPos + vector * this.Props.barrelOffsetDistance,
                                worldTarget = intVec2.ToVector3(),
                                moveVector = vector,
                                startScale = Rand.Range(0.8f, 1.2f) * num2,
                                endScale = (1f + Rand.Range(0.1f, 0.4f)) * num2,
                                lifespanTicks = Mathf.FloorToInt(num * 5f) + Rand.Range(-this.Props.lifespanNoise, this.Props.lifespanNoise)
                            });
                            if (this.Props.effecterDef != null)
                            {
                                map.effecterMaintainer.AddEffecterToMaintain(this.Props.effecterDef.Spawn(intVec2, map, 1f), intVec2, 15);
                            }
                        }
                    }
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }
    }
}
