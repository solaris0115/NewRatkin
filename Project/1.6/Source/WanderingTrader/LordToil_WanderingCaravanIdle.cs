using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반 대기 상태. 리더 우클릭 시 ExtraFloatMenuOptions로 대화하기/돌려보내기 제공.
	/// </summary>
	public class LordToil_WanderingCaravanIdle : LordToil_DefendPoint
	{
		public LordToil_WanderingCaravanIdle(IntVec3 chillSpot) : base(chillSpot, null, null)
		{
		}

		/// <summary>바닐라 LordToil_DefendTraderCaravan과 동일: 대기 중 수면·장기 욕구 충족 비활성.</summary>
		public override bool AllowSatisfyLongNeeds => false;

		public override float? CustomWakeThreshold => 0.5f;

		public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn clickedPawn, Pawn forPawn)
		{
			return GetFloatMenuOptionsForLeader(lord, clickedPawn, forPawn);
		}

		/// <summary>
		/// 리더 우클릭 시 대화하기/돌려보내기 옵션. Idle·Travel 공통 사용.
		/// </summary>
		internal static IEnumerable<FloatMenuOption> GetFloatMenuOptionsForLeader(Lord lord, Pawn clickedPawn, Pawn forPawn)
		{
			LordJob_WanderingCaravan job = lord?.LordJob as LordJob_WanderingCaravan;
			if (job == null || job.leader != clickedPawn)
				yield break;

			if (forPawn == null || !forPawn.IsColonist)
				yield break;

			// 대화하기: pawn이 리더에게 직접 이동 후 대화 (trader와 동일)
			// Social 비활성(협상 불가) 팅은 거래창에서 TradePriceImprovement 오류 발생 → 대화 불가
			bool canNegotiate = !RimWorld.StatDefOf.TradePriceImprovement.Worker.IsDisabledFor(forPawn);

			Action talkAction = () =>
			{
				Job talkJob = JobMaker.MakeJob(RatkinJobDefOf.RK_Job_TalkToCaravanLeader, clickedPawn);
				talkJob.playerForced = true;
				forPawn.jobs.TryTakeOrderedJob(talkJob, new JobTag?(JobTag.Misc), false);
			};

			if (!canNegotiate)
			{
				yield return new FloatMenuOption("RK_WanderingCaravan_TalkToLeader".Translate() + ": " + "Incapable".Translate().CapitalizeFirst(),
					null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
			}
			else if (!forPawn.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
			{
				yield return new FloatMenuOption("RK_WanderingCaravan_TalkToLeader".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
					null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
			}
			else
			{
				yield return FloatMenuUtility.DecoratePrioritizedTask(
					new FloatMenuOption("RK_WanderingCaravan_TalkToLeader".Translate(), talkAction,
						MenuOptionPriority.InitiateSocial, null, clickedPawn, 0f, null, null, true, 0),
					forPawn, clickedPawn, "ReservedBy", null);
			}

			// 돌려보내기: pawn이 리더에게 직접 이동 후 요청 (대화하기와 동일)
			Action dismissAction = () =>
			{
				Job dismissJob = JobMaker.MakeJob(RatkinJobDefOf.RK_Job_DismissCaravanLeader, clickedPawn);
				dismissJob.playerForced = true;
				forPawn.jobs.TryTakeOrderedJob(dismissJob, new JobTag?(JobTag.Misc), false);
			};

			if (!forPawn.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
			{
				yield return new FloatMenuOption("RK_WanderingCaravan_DismissCaravan".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
					null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
			}
			else
			{
				yield return FloatMenuUtility.DecoratePrioritizedTask(
					new FloatMenuOption("RK_WanderingCaravan_DismissCaravan".Translate(), dismissAction,
						MenuOptionPriority.InitiateSocial, null, clickedPawn, 0f, null, null, true, 0),
					forPawn, clickedPawn, "ReservedBy", null);
			}
		}

		internal static Dialog_NodeTree CreateMainDialog(Pawn leader, Pawn colonist)
		{
			LordJob_WanderingCaravan job = leader?.GetLord()?.LordJob as LordJob_WanderingCaravan;
			List<Pawn> settlers = job != null ? GetSettlersFromLord(job.lord) : new List<Pawn>();

			DiaNode root = new DiaNode("RK_WanderingCaravan_LeaderGreeting".Translate(leader.NameShortColored));

			// 1. 물자 거래
			DiaOption tradeOpt = new DiaOption("RK_WanderingCaravan_TradeGoods".Translate());
			tradeOpt.action = () => Find.WindowStack.Add(new RimWorld.Dialog_Trade(colonist, leader, false));
			tradeOpt.resolveTree = true;
			root.options.Add(tradeOpt);

			// 2. 물건 의뢰
			DiaOption commissionOpt = new DiaOption("RK_WanderingCaravan_Commission".Translate());
			commissionOpt.action = () => Find.WindowStack.Add(new Dialog_CaravanCommission(leader));
			commissionOpt.resolveTree = true;
			root.options.Add(commissionOpt);

			// 3. 정착 제안하기
			if (settlers.Count > 0)
			{
				DiaOption settleOpt = new DiaOption("RK_WanderingCaravan_SettleProposal".Translate());
				settleOpt.action = () => Find.WindowStack.Add(new Dialog_CaravanSettlers(leader, settlers));
				settleOpt.resolveTree = true;
				root.options.Add(settleOpt);
			}

			// 4. 대화 마치기
			DiaOption closeOpt = new DiaOption("RK_WanderingCaravan_EndDialog".Translate());
			closeOpt.resolveTree = true;
			root.options.Add(closeOpt);

			return new Dialog_NodeTree(root, true, false, "RK_WanderingCaravan_LeaderTitle".Translate());
		}

		private static List<Pawn> GetSettlersFromLord(Lord lord)
		{
			var result = new List<Pawn>();
			if (lord?.ownedPawns == null) return result;

			var comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
			foreach (Pawn p in lord.ownedPawns)
			{
				if (p == null || !p.Spawned || p.Dead) continue;
				if (!WanderingCaravanUtility.IsSettlerPoolKind(p.kindDef)) continue;
				if (comp != null && comp.GetRequirement(p) == null) continue;
				result.Add(p);
			}
			return result;
		}
	}
}
