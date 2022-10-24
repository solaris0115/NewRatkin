using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;

namespace NewRatkin
{
    public class JobDriver_HamsterWheel : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo targetA = this.job.targetA;
            Job job = this.job;
            return pawn.Reserve (targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(()=>
            {
                CompPowerPlantHamsterWheel compHW = job.targetA.Thing.TryGetComp<CompPowerPlantHamsterWheel>();

                return !compHW.CanUseNow;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            Toil work = new Toil();
            work.initAction = delegate ()
            {
                Pawn actor = work.actor;
                Building building = (Building)actor.CurJob.targetA.Thing;
                CompPowerPlantHamsterWheel comp = building.GetComp<CompPowerPlantHamsterWheel>();
                comp.StartTurnning(actor.GetStatValue(StatDefOf.MoveSpeed, true), actor);

            };
            work.tickAction = delegate ()
            {
                if(Current.Game.tickManager.TicksGame % 10 ==0)
                {
                    Pawn actor = work.actor;
                    Traverse.Create(actor.Drawer).Field<JitterHandler>("jitterer").Value.AddOffset(0.07f, Rand.Range(0, 360));
                }
            };
            work.AddFinishAction(delegate ()
            {
                Pawn actor = work.actor;
                Building building = (Building)actor.CurJob.targetA.Thing;
                CompPowerPlantHamsterWheel comp = building.GetComp<CompPowerPlantHamsterWheel>();
                comp.UsingDone();
            });
            work.defaultCompleteMode = ToilCompleteMode.Delay;
            work.defaultDuration = 4000;
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            yield return work;
            yield break;
        }
    }
}