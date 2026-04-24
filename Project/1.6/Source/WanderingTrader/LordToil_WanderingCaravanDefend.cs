using System;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반 전투 방어 Toil. 공격 당했을 때 즉시 도망하지 않고 해당 위치에서 전투.
	/// 피해 누적(Trigger_FractionPawnsLost) 시 ExitMapAndDefendSelf로 전환되어 퇴각.
	/// </summary>
	public class LordToil_WanderingCaravanDefend : LordToil_DefendPoint
	{
		public override bool AllowSatisfyLongNeeds => false;

		public override float? CustomWakeThreshold => 0.5f;

		public LordToil_WanderingCaravanDefend() : base(true)
		{
		}
	}
}
