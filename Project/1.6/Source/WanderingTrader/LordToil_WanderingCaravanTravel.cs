using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반 이동 상태. LordToil_Travel 확장으로 이동 중에도 대화하기/돌려보내기 FloatMenu 제공.
	/// </summary>
	public class LordToil_WanderingCaravanTravel : LordToil_Travel
	{
		public LordToil_WanderingCaravanTravel(IntVec3 dest) : base(dest)
		{
		}

		public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn clickedPawn, Pawn forPawn)
		{
			return LordToil_WanderingCaravanIdle.GetFloatMenuOptionsForLeader(lord, clickedPawn, forPawn);
		}
	}
}
