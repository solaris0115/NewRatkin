using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 유랑단 캐러반 인시던트. hidden faction(RK_Faction_Caravan) 사용.
	/// 명부 기반 영속 인물 + 매년 충원. LordJob_WanderingCaravan 사용.
	/// </summary>
	public class IncidentWorker_RatkinWanderingTrader : IncidentWorker
	{
		/// <summary>재방문 시 호위 보충 최소 수. 포인트 기반 가드는 별도.</summary>
		private const int MinGuardCountOnRefill = 2;

		private IncidentDefExtension_WanderingCaravan Ext => def.GetModExtension<IncidentDefExtension_WanderingCaravan>();

		protected override bool CanFireNowSub(IncidentParms parms)
		{
			if (!base.CanFireNowSub(parms))
				return false;
			Map map = (Map)parms.target;
			if (map == null) return false;
			foreach (GameCondition cond in map.GameConditionManager.ActiveConditions)
			{
				if (cond.def.preventNeutralVisitors)
					return false;
			}
			// 공격 패널티 기간 중에는 방문 차단
			GameComponent_WanderingCaravan comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
			if (comp != null && comp.IsAttackPenaltyActive)
				return false;
			// 유랑단이 이미 어딘가에 존재하면 트리거 안 함 (다중 맵, 디버그 강제 호출 대비)
			if (WanderingCaravanUtility.HasWanderingCaravanActiveAnywhere())
				return false;
			return true;
		}

		/// <summary>
		/// 맵에 유랑단 캐러반이 이미 존재하는지 확인.
		/// LordJob_WanderingCaravan 보유 Lord 또는 RK_Faction_Caravan 소속 유랑단 멤버(리더/호위/유랑민)가 맵에 있으면 true.
		/// 플레이어가 영입한 pawn은 Faction.OfPlayer로 변경되므로 제외됨.
		/// </summary>
		private static bool HasWanderingCaravanOnMap(Map map)
		{
			if (map == null) return false;
			FactionDef caravanFaction = RatkinFactionDefOf.RK_Faction_Caravan;
			if (caravanFaction == null) return false;

			// LordJob_WanderingCaravan 보유 Lord가 맵에 있는지
			foreach (Lord lord in map.lordManager.lords)
			{
				if (lord?.LordJob is LordJob_WanderingCaravan)
					return true;
			}

			// RK_Faction_Caravan 소속 유랑단 멤버(리더/호위/유랑민)가 맵에 스폰되어 있는지
			foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
			{
				if (p == null || p.DestroyedOrNull() || p.Dead) continue;
				if (p.Faction?.def != caravanFaction) continue;
				if (p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanLeader
					|| p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard
					|| WanderingCaravanUtility.IsSettlerPoolKind(p.kindDef))
					return true;
			}
			return false;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			// 디버그 수동 호출 등 CanFireNow를 우회하는 경우 대비: 캐러반이 이미 어딘가에 있으면 경고 후 중단
			if (WanderingCaravanUtility.HasWanderingCaravanActiveAnywhere())
			{
				Messages.Message("RK_WanderingCaravan_AlreadySpawned".Translate(), MessageTypeDefOf.NeutralEvent, false);
				return false;
			}
			// 패널티 만료 후 첫 방문: 공격 플래그 리셋 + 팩션 관계 중립 복구 (로스터는 NotifyCaravanAttacked에서 이미 클리어됨)
			GameComponent_WanderingCaravan comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
			if (comp != null && comp.WasAttackedByPlayer)
			{
				comp.ResetAttackedFlag();
				Faction caravanFaction = Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.RK_Faction_Caravan);
				if (caravanFaction != null && caravanFaction.HostileTo(Faction.OfPlayer))
					caravanFaction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false, null, null);
			}
			var ext = Ext ?? new IncidentDefExtension_WanderingCaravan();
			int maxRoster = ext.maxRosterCount;
			IntRange yearlyRecruit = ext.yearlyRecruitRange;

			Faction faction = Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.RK_Faction_Caravan);
			if (faction == null)
			{
				FactionGeneratorParms fgParms = new FactionGeneratorParms(
					RatkinFactionDefOf.RK_Faction_Caravan,
					default(IdeoGenerationParms),
					true);
				faction = FactionGenerator.NewGeneratedFaction(fgParms);
				Find.FactionManager.Add(faction);
			}
			faction.factionHostileOnHarmByPlayer = true;

			TraderKindDef traderKind = DefDatabase<TraderKindDef>.GetNamed("RK_TraderKind_WanderingTrader", false);
			if (traderKind == null)
				return false;

			if (!parms.spawnCenter.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, map, CellFinder.EdgeRoadChance_Neutral, false, null))
				return false;

			if (comp == null)
				return false;

			Pawn leader;
			List<Pawn> guards;
			List<Pawn> salePawns;

			if (comp.IsRosterEmpty)
			{
				// 첫 방문: 전원 신규 생성, 유랑민은 풀에 추가 후 로스터 선택
				leader = CreateLeader(faction, map.Tile, traderKind);
				if (leader == null) return false;

				guards = CreateGuardsWithPoints(faction, map.Tile, leader);
				var initialSettlers = CreateSettlers(faction, map.Tile, ext.initialSettlerCount);
				foreach (Pawn p in initialSettlers)
					comp.AddToPool(p, SettlementJoinRequirement.GenerateForPawnKind(p.kindDef, ext));
				salePawns = comp.SelectRosterFromPool(maxRoster);
			}
			else
			{
				// 재방문: 사망/만료 정리, 풀 충원, 풀에서 로스터 선택 (이벤트마다 충원, 시간 통제는 minRefireDays 등으로 처리)
				comp.CleanupDeadPawns();
				comp.RemoveExpiredFromPool(ext.expireAfterAppearances);
				comp.RefillPool(map, faction, yearlyRecruit.RandomInRange, ext.maxPoolSize, ext);
				if (comp.PoolCount >= maxRoster)
					comp.RefillPool(map, faction, ext.overflowRecruitCount, ext.maxPoolSize, ext);
				comp.SelectRosterFromPool(maxRoster);
				comp.TakePawnsForSpawn(map, out leader, out guards, out salePawns);

				if (leader == null || leader.DestroyedOrNull() || leader.Dead)
				{
					leader = CreateLeader(faction, map.Tile, traderKind);
					if (leader == null) return false;
				}
				else
				{
					leader.mindState.wantsToTradeWithColony = true;
					PawnComponentsUtility.AddAndRemoveDynamicComponents(leader, true);
					leader.trader.traderKind = traderKind;
				}
				CleanupAndRefillGuards(guards, faction, map.Tile);
			}

			// 거래 물품 생성 (바닐라 TraderStock과 동일)
			ThingSetMakerParams stockParms = default(ThingSetMakerParams);
			stockParms.traderDef = traderKind;
			stockParms.tile = new PlanetTile?(map.Tile);
			stockParms.makingFaction = faction;
			List<Thing> wares = new List<Thing>();
			foreach (Thing thing in ThingSetMakerDefOf.TraderStock.root.Generate(stockParms))
			{
				Pawn stockPawn = thing as Pawn;
				if (stockPawn != null)
				{
					if (stockPawn.Faction != faction)
						stockPawn.SetFaction(faction, null);
					salePawns.Add(stockPawn);
				}
				else
				{
					wares.Add(thing);
				}
			}
			// 짐꾼 동물: 바닐라와 동일하게 물품 수에 따라 ceil(wares/8), 최소 1
			List<Pawn> packAnimals = CreatePackAnimals(faction, map.Tile, wares.Count);
			DistributeWaresToCarriers(wares, packAnimals, leader);
			PawnInventoryGenerator.GiveRandomFood(leader);
			List<Pawn> allPawns = new List<Pawn> { leader };
			allPawns.AddRange(guards);
			allPawns.AddRange(salePawns);
			allPawns.AddRange(packAnimals);

			foreach (Pawn p in allPawns)
			{
				IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5, null);
				GenSpawn.Spawn(p, loc, map, WipeMode.Vanish);
				if (p.needs?.food != null)
					p.needs.food.CurLevel = p.needs.food.MaxLevel;
			}

			IntVec3 chillSpot;
			if (!RCellFinder.TryFindRandomSpotJustOutsideColony(leader.Position, map, leader, out chillSpot, c =>
			{
				foreach (Pawn p in allPawns)
				{
					if (!p.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
						return false;
				}
				return true;
			}))
			{
				return false;
			}

			comp.UpdateLastVisitYear(map.Tile);

			LordJob_WanderingCaravan lordJob = new LordJob_WanderingCaravan(leader, chillSpot, faction);
			LordMaker.MakeNewLord(faction, lordJob, map, allPawns);

			TaggedString label = "RK_WanderingCaravan_ArrivalLetterLabel".Translate();
			TaggedString text = "RK_WanderingCaravan_ArrivalLetter".Translate(faction.NameColored);
			PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(allPawns, ref label, ref text,
				"LetterRelatedPawnsNeutralGroup".Translate(Faction.OfPlayer.def.pawnsPlural), true, true);
			SendStandardLetter(label, text, LetterDefOf.PositiveEvent, parms, leader, Array.Empty<NamedArgument>());

			return true;
		}

		private static Pawn CreateLeader(Faction faction, int tile, TraderKindDef traderKind)
		{
			Pawn leader = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
				RatkinPawnKindDefOf.RK_PawnKind_CaravanLeader, faction, PawnGenerationContext.NonPlayer, tile,
				false, false, false, true, false, 1f, true, true, true, true, true,
				false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
				null, null, null, null, null, null, null, null, false, false, false, false,
				null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
				false, false, false, -1, 0, false));
			if (leader == null) return null;

			leader.mindState.wantsToTradeWithColony = true;
			PawnComponentsUtility.AddAndRemoveDynamicComponents(leader, true);
			leader.trader.traderKind = traderKind;
			return leader;
		}

		/// <summary>
		/// 바닐라 TraderCaravanArrival과 동일: TraderCaravanUtility.GenerateGuardPoints() 기반,
		/// 팩션 Trader PawnGroupMaker의 guards로 포인트 기반 생성.
		/// </summary>
		private static List<Pawn> CreateGuardsWithPoints(Faction faction, int tile, Pawn leader)
		{
			var guards = new List<Pawn>();
			var groupMaker = faction?.def?.pawnGroupMakers?.FirstOrDefault(g => g.kindDef == PawnGroupKindDefOf.Trader);
			if (groupMaker == null || groupMaker.guards.NullOrEmpty())
			{
				// 폴백: RK_PawnKind_CaravanGuard 2~4명
				int count = Rand.RangeInclusive(2, 4);
				for (int i = 0; i < count; i++)
				{
					var g = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
						RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard, faction, PawnGenerationContext.NonPlayer, tile,
						false, false, false, true, true, 1f, true, true, true, true, true,
						false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
						null, null, null, null, null, null, null, null, false, false, false, false,
						null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
						false, false, false, -1, 0, false));
					if (g != null) guards.Add(g);
				}
				return guards;
			}
			float points = TraderCaravanUtility.GenerateGuardPoints();
			if (leader != null)
				points -= leader.kindDef.combatPower;
			if (points <= 0f) return guards;
			var parms = new PawnGroupMakerParms
			{
				groupKind = PawnGroupKindDefOf.Trader,
				tile = tile,
				faction = faction,
				points = points
			};
			foreach (var opt in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(points, groupMaker.guards, parms))
			{
				var kind = opt.Option.kind;
				var xenotype = opt.Xenotype;
				var req = new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, tile,
					false, false, false, true, true, 1f, true, true, true, true, true,
					false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
					null, null, null, null, null, null, null, null, false, false, false, false,
					null, null, xenotype, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
					false, false, false, -1, 0, false);
				var guard = PawnGenerator.GeneratePawn(req);
				if (guard != null) guards.Add(guard);
			}
			return guards;
		}

		private static List<Pawn> CreateSettlers(Faction faction, int tile, int count)
		{
			var list = new List<Pawn>();
			for (int i = 0; i < count; i++)
			{
				PawnKindDef kind = WanderingCaravanUtility.RandomSettlerKind();
				Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
					kind, faction, PawnGenerationContext.NonPlayer, tile,
					false, false, false, true, kind.isFighter, 1f, true, true, true, true, true,
					false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
					null, null, null, null, null, null, null, null, false, false, false, false,
					null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
					false, false, false, -1, 0, false));
				if (pawn != null) list.Add(pawn);
			}
			return list;
		}

		/// <summary>
		/// 바닐라 PawnGroupKindWorker_Trader.GenerateCarriers와 동일: ceil(waresCount/8), 최소 1.
		/// </summary>
		private static List<Pawn> CreatePackAnimals(Faction faction, int tile, int waresCount)
		{
			var list = new List<Pawn>();
			int count = Mathf.Max(1, Mathf.CeilToInt((float)waresCount / 8f));
			for (int i = 0; i < count; i++)
			{
				Pawn animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
					RatkinPawnKindDefOf.Ratkin_KingHamster, faction, PawnGenerationContext.NonPlayer, tile,
					false, false, false, true, false, 1f, true, true, true, true, true,
					false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
					null, null, null, null, null, null, null, null, false, false, false, false,
					null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
					false, false, false, -1, 0, false));
				if (animal != null)
				{
					animal.training.Train(RimWorld.TrainableDefOf.Obedience, null, true);
					list.Add(animal);
				}
			}
			return list;
		}

		/// <summary>
		/// 바닐라 PawnGroupKindWorker_Trader.GenerateCarriers() 방식 참조.
		/// 짐꾼 동물(packAnimal)에게 우선 분배하고, 남은 물품은 리더에게 넣는다.
		/// </summary>
		private static void DistributeWaresToCarriers(List<Thing> wares, List<Pawn> packAnimals, Pawn leader)
		{
			if (wares.Count == 0) return;

			if (packAnimals.Count == 0)
			{
				foreach (Thing thing in wares)
				{
					if (!leader.inventory.innerContainer.TryAdd(thing, true))
						thing.Destroy(DestroyMode.Vanish);
				}
				return;
			}

			// 라운드 로빈으로 각 짐꾼에게 순서대로 분배
			for (int i = 0; i < wares.Count; i++)
			{
				Pawn carrier = packAnimals[i % packAnimals.Count];
				if (!carrier.inventory.innerContainer.TryAdd(wares[i], true))
				{
					// 짐꾼이 못 받으면 리더에게
					if (!leader.inventory.innerContainer.TryAdd(wares[i], true))
						wares[i].Destroy(DestroyMode.Vanish);
				}
			}
		}

		private static void CleanupAndRefillGuards(List<Pawn> guards, Faction faction, int tile)
		{
			guards.RemoveAll(p => p == null || p.DestroyedOrNull() || p.Dead);
			int need = MinGuardCountOnRefill - guards.Count;
			if (need <= 0) return;
			// 포인트 기반 보충 (250~400으로 1~2명 수준)
			float refillPoints = Rand.Range(250f, 400f);
			var groupMaker = faction?.def?.pawnGroupMakers?.FirstOrDefault(g => g.kindDef == PawnGroupKindDefOf.Trader);
			if (groupMaker != null && !groupMaker.guards.NullOrEmpty())
			{
				var parms = new PawnGroupMakerParms
				{
					groupKind = PawnGroupKindDefOf.Trader,
					tile = tile,
					faction = faction,
					points = refillPoints
				};
				foreach (var opt in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(refillPoints, groupMaker.guards, parms))
				{
					var kind = opt.Option.kind;
					var xenotype = opt.Xenotype;
					var req = new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, tile,
						false, false, false, true, true, 1f, true, true, true, true, true,
						false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
						null, null, null, null, null, null, null, null, false, false, false, false,
						null, null, xenotype, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
						false, false, false, -1, 0, false);
					var guard = PawnGenerator.GeneratePawn(req);
					if (guard != null) guards.Add(guard);
				}
			}
			else
			{
				for (int i = 0; i < need; i++)
				{
					var g = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
						RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard, faction, PawnGenerationContext.NonPlayer, tile,
						false, false, false, true, true, 1f, true, true, true, true, true,
						false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
						null, null, null, null, null, null, null, null, false, false, false, false,
						null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
						false, false, false, -1, 0, false));
					if (g != null) guards.Add(g);
				}
			}
		}
	}
}
