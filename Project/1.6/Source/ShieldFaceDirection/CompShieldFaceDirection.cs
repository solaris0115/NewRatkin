using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace NewRatkin
{
    public class CompShieldFaceDirection : ThingComp
    {
        private int lastFaceCommandTick = -9999;

        public CompProperties_ShieldFaceDirection Props => (CompProperties_ShieldFaceDirection)props;

        private Pawn PawnOwner
        {
            get
            {
                if (parent is Apparel apparel)
                {
                    return apparel.Wearer;
                }
                return null;
            }
        }

        public void NotifyFaceCommandUsed()
        {
            lastFaceCommandTick = GenTicks.TicksGame;
        }

        public int GetCooldownTicksLeft()
        {
            int cooldownTicks = Props?.cooldownTicks ?? 300;
            int elapsed = GenTicks.TicksGame - lastFaceCommandTick;
            return UnityEngine.Mathf.Max(0, cooldownTicks - elapsed);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastFaceCommandTick, "lastFaceCommandTick", -9999);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (var gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }
            Pawn pawn = PawnOwner;
            if (pawn == null)
            {
                yield break;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            if (!Find.Selector.SelectedPawns.Contains(pawn))
            {
                yield break;
            }
            if (!IsFirstSelectedWithThisComp())
            {
                yield break;
            }
            foreach (var gizmo in GetFaceDirectionGizmos())
            {
                yield return gizmo;
            }
        }

        private bool IsFirstSelectedWithThisComp()
        {
            Pawn owner = PawnOwner;
            if (owner == null) return false;
            foreach (Pawn p in Find.Selector.SelectedPawns)
            {
                if (p == null) continue;
                if (GetCompFromPawn(p) != null)
                {
                    return p == owner;
                }
            }
            return false;
        }

        private static CompShieldFaceDirection GetCompFromPawn(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null) return null;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.GetComp<CompShieldFaceDirection>();
                if (comp != null) return comp;
            }
            return null;
        }

        private static IEnumerable<Pawn> GetSelectedPawnsWithShieldFaceDirection()
        {
            foreach (Pawn p in Find.Selector.SelectedPawns)
            {
                if (p != null && GetCompFromPawn(p) != null)
                {
                    yield return p;
                }
            }
        }

        private IEnumerable<Gizmo> GetFaceDirectionGizmos()
        {
            int cooldownTicks = Props?.cooldownTicks ?? 300;
            var cmd = new Command_Target
            {
                defaultLabel = "RK_ShieldFaceDirection".Translate(),
                defaultDesc = "RK_ShieldFaceDirectionDesc".Translate(),
                icon = TexCommand.AttackMelee,
                groupable = true,
                groupKey = 0x5F4E3D2C,
                targetingParams = new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetPawns = true,
                    canTargetBuildings = true
                },
                action = target =>
                {
                    if (target.Cell.IsValid && !target.HasThing)
                    {
                        var pawns = GetSelectedPawnsWithShieldFaceDirection().ToList();
                        if (pawns.All(p => p.Position == target.Cell))
                        {
                            return;
                        }
                    }
                    foreach (Pawn p in GetSelectedPawnsWithShieldFaceDirection())
                    {
                        if (p.Position == target.Cell && !target.HasThing) continue;
                        var job = JobMaker.MakeJob(RatkinJobDefOf.RK_Job_ShieldFaceDirection);
                        job.targetA = target;
                        p.jobs.TryTakeOrderedJob(job);
                        GetCompFromPawn(p)?.NotifyFaceCommandUsed();
                    }
                }
            };
            var candidates = GetSelectedPawnsWithShieldFaceDirection().ToList();
            bool anyDrafted = candidates.Any(p => p.Drafted);
            if (!anyDrafted)
            {
                yield break; // 소집 시에만 커맨드 표시
            }
            bool anyDeadDowned = candidates.Any(p => p.Dead || p.Downed);
            bool anyBusy = candidates.Any(p => p.stances.FullBodyBusy);
            int maxCooldownLeft = candidates
                .Select(p => GetCompFromPawn(p))
                .Where(c => c != null)
                .Select(c => c.GetCooldownTicksLeft())
                .DefaultIfEmpty(0)
                .Max();

            if (anyDeadDowned)
            {
                cmd.Disable("RK_ShieldFaceDirection_InvalidState".Translate());
            }
            else if (anyBusy)
            {
                cmd.Disable("RK_ShieldFaceDirection_Busy".Translate());
            }
            else if (maxCooldownLeft > 0)
            {
                cmd.Disable("RK_ShieldFaceDirection_Cooldown".Translate(maxCooldownLeft.ToStringSecondsFromTicks()));
            }
            yield return cmd;
        }
    }
}
