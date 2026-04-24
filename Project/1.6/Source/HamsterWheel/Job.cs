using System.Collections.Generic;
using UnityEngine;
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
            Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            // GotoThing 완료 후 즉시 동쪽을 바라보도록 설정
            gotoToil.AddFinishAction(delegate ()
            {
                Pawn actor = gotoToil.actor;
                // 동쪽 방향으로 설정 (X축 양의 방향 = 동쪽)
                actor.rotationTracker.Face(actor.DrawPos + new Vector3(1f, 0f, 0f));
            });
            yield return gotoToil;

            Toil work = new Toil();
            work.handlingFacing = true; // Toil이 방향을 제어하도록 설정
            work.initAction = delegate ()
            {
                Pawn actor = work.actor;
                Building building = (Building)actor.CurJob.targetA.Thing;
                CompPowerPlantHamsterWheel comp = building.GetComp<CompPowerPlantHamsterWheel>();
                comp.StartTurnning(actor.GetStatValue(StatDefOf.MoveSpeed, true), actor);
                // 챗바퀴를 사용하는 동안 항상 동쪽을 바라보도록 설정
                actor.rotationTracker.Face(actor.DrawPos + new Vector3(1f, 0f, 0f));
            };
            work.tickAction = delegate ()
            {
                Pawn actor = work.actor;
                // 매 틱마다 동쪽을 바라보도록 강제 (다른 시스템이 방향을 바꾸는 것을 방지)
                actor.rotationTracker.Face(actor.DrawPos + new Vector3(1f, 0f, 0f));
                
                // 무작위 방향으로 jitter 효과
                if(Current.Game.tickManager.TicksGame % 10 ==0)
                {
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