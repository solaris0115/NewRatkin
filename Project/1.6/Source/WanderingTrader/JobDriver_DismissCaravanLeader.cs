using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewRatkin
{
	/// <summary>
	/// 정착민이 유랑단 리더에게 직접 이동 후 돌려보내기 요청. (대화하기와 동일하게 직접 만나야 가능)
	/// </summary>
	public class JobDriver_DismissCaravanLeader : JobDriver
	{
		private Pawn Leader => (Pawn)job.GetTarget(TargetIndex.A).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			// Travel 중 리더가 이동할 때 예약 충돌 가능 → ignoreOtherReservations로 우선 예약
			return pawn.Reserve(Leader, job, 1, -1, null, errorOnFailed, true);
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

			Toil dismissToil = ToilMaker.MakeToil("MakeNewToils");
			dismissToil.initAction = () =>
			{
				Lord lord = Leader?.GetLord();
				if (lord != null && lord.LordJob is LordJob_WanderingCaravan)
					lord.ReceiveMemo("CaravanDismissed");
			};
			yield return dismissToil;
		}
	}
}
