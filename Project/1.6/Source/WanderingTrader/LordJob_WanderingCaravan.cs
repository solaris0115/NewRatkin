using System;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 유랑단 캐러반 전용 LordJob.
	/// 리더 중심 상호작용, Travel → Idle → Exit 흐름.
	/// </summary>
	public class LordJob_WanderingCaravan : LordJob
	{
		public Pawn leader;
		private IntVec3 chillSpot;
		private Faction faction;

		public LordJob_WanderingCaravan() { }

		public LordJob_WanderingCaravan(Pawn leader, IntVec3 chillSpot, Faction faction)
		{
			this.leader = leader;
			this.chillSpot = chillSpot;
			this.faction = faction;
		}

		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph();

			LordToil_WanderingCaravanTravel travel = new LordToil_WanderingCaravanTravel(chillSpot);
			stateGraph.StartingToil = travel; // setter가 이미 lordToils에 추가함. AddToil(travel) 중복 시 오류

			LordToil_WanderingCaravanIdle idle = new LordToil_WanderingCaravanIdle(chillSpot);
			stateGraph.AddToil(idle);

			LordToil_WanderingCaravanDefend defend = new LordToil_WanderingCaravanDefend();
			stateGraph.AddToil(defend);

			// 트레이더처럼 리더 따라 한곳으로 모여 퇴장
			LordToil_ExitMapWanderingCaravan exitCaravan = new LordToil_ExitMapWanderingCaravan();
			stateGraph.AddToil(exitCaravan);

			LordToil_ExitMap exitMap = new LordToil_ExitMap(LocomotionUrgency.None, false, false);
			stateGraph.AddToil(exitMap);

			LordToil_ExitMapAndDefendSelf exitDefend = new LordToil_ExitMapAndDefendSelf();
			stateGraph.AddToil(exitDefend);

			// TravelArrived → Idle
			Transition toIdle = new Transition(travel, idle, false, true);
			toIdle.AddTrigger(new Trigger_Memo("TravelArrived"));
			stateGraph.AddTransition(toIdle, false);

			// Idle: TicksPassed → Exit (리더 따라 한곳으로 퇴장)
			Transition toExitTime = new Transition(idle, exitCaravan, false, true);
			toExitTime.AddTrigger(new Trigger_TicksPassed(
				(Prefs.DevMode && DebugSettings.instantVisitorsGift) ? 0 : Rand.Range(27000, 45000)));
			toExitTime.AddPreAction(new TransitionAction_Custom(() => SaveCaravanToWorldPawns()));
			toExitTime.AddPreAction(new TransitionAction_Message("MessageTraderCaravanLeaving".Translate(faction.Name), null, 1f));
			toExitTime.AddPostAction(new TransitionAction_WakeAll());
			stateGraph.AddTransition(toExitTime, false);

			// Idle: CaravanDismissed → Exit (리더 따라 한곳으로 퇴장)
			Transition toExitDismissed = new Transition(idle, exitCaravan, false, true);
			toExitDismissed.AddTrigger(new Trigger_Memo("CaravanDismissed"));
			toExitDismissed.AddPreAction(new TransitionAction_Message("MessageTraderCaravanDismissed".Translate(faction.Name), null, 1f));
			toExitDismissed.AddPostAction(new TransitionAction_WakeAll());
			toExitDismissed.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(toExitDismissed, false);

			// Travel: CaravanDismissed → Exit (진입 중에도 돌려보내기 가능)
			Transition toExitDismissedFromTravel = new Transition(travel, exitCaravan, false, true);
			toExitDismissedFromTravel.AddTrigger(new Trigger_Memo("CaravanDismissed"));
			toExitDismissedFromTravel.AddPreAction(new TransitionAction_Message("MessageTraderCaravanDismissed".Translate(faction.Name), null, 1f));
			toExitDismissedFromTravel.AddPostAction(new TransitionAction_WakeAll());
			toExitDismissedFromTravel.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(toExitDismissedFromTravel, false);

			// Idle/Travel: BecamePlayerEnemy → ExitDefend (플레이어 적대 시 즉시 퇴각)
			Transition toExitEnemy = new Transition(idle, exitDefend, false, true);
			toExitEnemy.AddSource(travel);
			toExitEnemy.AddSource(defend);
			toExitEnemy.AddPreAction(new TransitionAction_Custom(() => MarkAttackedIfPlayerHostile()));
			toExitEnemy.AddTrigger(new Trigger_BecamePlayerEnemy());
			toExitEnemy.AddPostAction(new TransitionAction_WakeAll());
			toExitEnemy.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(toExitEnemy, false);

			// Idle/Travel: PawnHarmed → Defend (raid처럼 전투, 피해 누적 시 별도 전이로 퇴각)
			Transition toDefend = new Transition(idle, defend, false, true);
			toDefend.AddSource(travel);
			toDefend.AddPreAction(new TransitionAction_SetWanderingCaravanDefendPoint());
			toDefend.AddTrigger(new Trigger_PawnHarmed(1f, false, null, null, null));
			toDefend.AddPostAction(new TransitionAction_WakeAll());
			toDefend.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(toDefend, false);

			// Defend: 피해 20% 누적 → ExitDefend (퇴각)
			Transition defendToExit = new Transition(defend, exitDefend, false, true);
			defendToExit.AddPreAction(new TransitionAction_Custom(() => MarkAttackedIfPlayerHostile()));
			defendToExit.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
			defendToExit.AddPostAction(new TransitionAction_WakeAll());
			defendToExit.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(defendToExit, false);

			// Defend: 1200틱(약 20초) 무해 → Idle로 복귀 (Travel 중이었으면 Idle에서 chillSpot 근처 대기)
			Transition defendToIdle = new Transition(defend, idle, false, true);
			defendToIdle.AddTrigger(new Trigger_TicksPassedWithoutHarm(1200));
			stateGraph.AddTransition(defendToIdle, false);

			// Idle/Travel: DangerousTemperatures → Exit (리더 따라 한곳으로 퇴장)
			Transition toExitTemp = new Transition(idle, exitCaravan, false, true);
			toExitTemp.AddSource(travel);
			toExitTemp.AddSource(defend);
			toExitTemp.AddPreAction(new TransitionAction_Custom(() => SaveCaravanToWorldPawns()));
			toExitTemp.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name), null, 1f));
			toExitTemp.AddPostAction(new TransitionAction_EndAllJobs());
			toExitTemp.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
			stateGraph.AddTransition(toExitTemp, false);

			// ExitCaravan: PawnLost 시 self-transition으로 UpdateAllDuties 재호출 (리더 퇴장 시 나머지 pawn duty 갱신)
			Transition exitCaravanSelfUpdate = new Transition(exitCaravan, exitCaravan, true, true);
			exitCaravanSelfUpdate.canMoveToSameState = true;
			exitCaravanSelfUpdate.AddTrigger(new Trigger_PawnLost(PawnLostCondition.Undefined, null));
			stateGraph.AddTransition(exitCaravanSelfUpdate, false);

			// ExitCaravan: 60000틱(약 16시간) 경과 시 개별 퇴장으로 폴백 (트레이더와 동일)
			Transition exitCaravanToIndividual = new Transition(exitCaravan, exitMap, false, true);
			exitCaravanToIndividual.AddTrigger(new Trigger_TicksPassed(60000));
			exitCaravanToIndividual.AddPostAction(new TransitionAction_WakeAll());
			stateGraph.AddTransition(exitCaravanToIndividual, false);

			return stateGraph;
		}

		private void SaveCaravanToWorldPawns()
		{
			Current.Game.GetComponent<GameComponent_WanderingCaravan>()?.OnCaravanExited(lord);
		}

		private void MarkAttackedIfPlayerHostile()
		{
			if (faction != null && faction.HostileTo(Faction.OfPlayer))
			{
				GameComponent_WanderingCaravan comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
				comp?.NotifyCaravanAttacked();
				Messages.Message("RK_WanderingCaravan_AttackedPenalty".Translate(), MessageTypeDefOf.NegativeEvent, false);
			}
		}

		public override void Notify_PawnLost(Pawn p, PawnLostCondition cond)
		{
			base.Notify_PawnLost(p, cond);
			// 돌려보내기로 맵 끝 이동 후 퇴장 시, 각 pawn이 ExitMap할 때마다 roster에 추가 (WorldPawns KeepForever는 Pawn_ExitMap_Patch에서 처리)
			if (cond == PawnLostCondition.ExitedMap)
			{
				Current.Game.GetComponent<GameComponent_WanderingCaravan>()?.OnCaravanPawnExitedMap(p);
			}
			// BecamePlayerEnemy 등으로 팩션이 적대로 바뀐 뒤, 맵에 유랑단 맴버가 모두 사라지면 다시 중립(팩션 관계만; 공격 패널티는 유지)
			WanderingCaravanUtility.TryResetCaravanFactionToNeutralIfCleared();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref leader, "leader");
			Scribe_Values.Look(ref chillSpot, "chillSpot");
			Scribe_References.Look(ref faction, "faction");
		}
	}
}
