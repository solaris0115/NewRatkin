using System;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반 방어 전환 시, 리더 또는 캐러반 중심 위치를 방어 지점으로 설정.
	/// </summary>
	public class TransitionAction_SetWanderingCaravanDefendPoint : TransitionAction
	{
		public override void DoAction(Transition trans)
		{
			if (!(trans.target is LordToil_DefendPoint toil))
				return;

			LordJob_WanderingCaravan job = toil.lord?.LordJob as LordJob_WanderingCaravan;
			if (job?.leader != null && job.leader.Spawned)
			{
				toil.SetDefendPoint(job.leader.Position);
				return;
			}

			var spawned = toil.lord.ownedPawns.Where(p => p != null && p.Spawned).ToList();
			if (spawned.Count > 0)
				toil.SetDefendPoint(spawned.RandomElement().Position);
		}
	}
}
