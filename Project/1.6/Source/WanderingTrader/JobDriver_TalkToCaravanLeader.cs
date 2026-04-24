using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewRatkin
{
	/// <summary>
	/// 정착민이 유랑단 리더에게 이동 후 대화창을 띄움. (trader와 동일하게 직접 만나야 대화 가능)
	/// </summary>
	public class JobDriver_TalkToCaravanLeader : JobDriver
	{
		private Pawn Leader => (Pawn)job.GetTarget(TargetIndex.A).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Leader, job, 1, -1, null, errorOnFailed, false);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOn(() =>
			{
				LordJob_WanderingCaravan lj = Leader?.GetLord()?.LordJob as LordJob_WanderingCaravan;
				return lj == null || lj.leader != Leader;
			});

			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);

			Toil openDialog = ToilMaker.MakeToil("MakeNewToils");
			openDialog.initAction = () =>
			{
				if (Leader != null && Leader.Spawned && !Leader.Dead)
				{
					Dialog_NodeTree dialog = LordToil_WanderingCaravanIdle.CreateMainDialog(Leader, pawn);
					Find.WindowStack.Add(dialog);
				}
			};
			yield return openDialog;
		}
	}
}
