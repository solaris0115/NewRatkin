using System;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반 퇴장. 트레이더처럼 리더를 따라 한곳으로 모여서 퇴장.
	/// </summary>
	public class LordToil_ExitMapWanderingCaravan : LordToil
	{
		public override bool AllowSatisfyLongNeeds => false;
		public override bool AllowSelfTend => false;

		private Pawn Leader
		{
			get
			{
				LordJob_WanderingCaravan job = lord?.LordJob as LordJob_WanderingCaravan;
				return job?.leader;
			}
		}

		public override void UpdateAllDuties()
		{
			Pawn leader = Leader;
			// 리더가 ownedPawns에 없으면(ExitedMap 직후 RemovePawn됐으나 아직 DeSpawn 전) fallback
			if (leader == null || !leader.Spawned || !lord.ownedPawns.Contains(leader))
			{
				// 리더 없으면 전원 ExitMapBest로 개별 퇴장
				for (int i = 0; i < lord.ownedPawns.Count; i++)
				{
					Pawn p = lord.ownedPawns[i];
					if (p.Spawned)
					{
						p.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
						p.mindState.duty.locomotion = LocomotionUrgency.Jog;
					}
				}
				return;
			}

			// 리더: 맵 끝으로 이동 (트레이더와 동일)
			leader.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBestAndDefendSelf);
			leader.mindState.duty.radius = 18f;
			leader.mindState.duty.locomotion = LocomotionUrgency.Jog;

			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn p = lord.ownedPawns[i];
				if (p == leader || !p.Spawned) continue;

				// 호위: 리더 에스코트
				if (p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard)
				{
					p.mindState.duty = new PawnDuty(DutyDefOf.Escort, leader, 26f);
					continue;
				}

				// 유랑민, 짐꾼(동물): 리더 따라가기
				if (WanderingCaravanUtility.IsSettlerPoolKind(p.kindDef)
					|| p.RaceProps.Animal)
				{
					p.mindState.duty = new PawnDuty(DutyDefOf.Follow, leader, 5f);
					continue;
				}

				// 기타: 리더 에스코트
				p.mindState.duty = new PawnDuty(DutyDefOf.Escort, leader, 14f);
			}
		}
	}
}
