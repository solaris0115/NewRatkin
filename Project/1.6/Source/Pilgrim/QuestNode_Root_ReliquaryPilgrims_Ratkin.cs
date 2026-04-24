using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace NewRatkin
{
	// 랫킨 전용 필그림 퀘스트 노드
	public class QuestNode_Root_ReliquaryPilgrims_Ratkin : RimWorld.QuestGen.QuestNode_Root_ReliquaryPilgrims
	{
		// RunInt를 오버라이드하여 랫킨 필그림 사용
		protected override void RunInt()
		{
			if (!ModLister.CheckIdeology("Reliquary pilgrims"))
			{
				return;
			}
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Map map = QuestGen_Get.GetMap(false, null, false);
			// PilgrimCount는 private이므로 직접 정의
			int randomInRange = new IntRange(1, 4).RandomInRange;
			if (map == null)
			{
				return;
			}
			
			// 랫킨 필그림 팩션과 PawnKind 선택
			FactionDef factionDef;
			PawnKindDef kindDef;
			GetFactionAndPawnKind_Ratkin(out factionDef, out kindDef);
			
			Precept_Relic precept_Relic;
			Building building;
			Thing var;
			TryFindReliquaryWithRelic_Ratkin(map, out precept_Relic, out building, out var);
			List<FactionRelation> list = new List<FactionRelation>();
			foreach (Faction faction2 in Find.FactionManager.AllFactionsListForReading)
			{
				if (!faction2.def.PermanentlyHostileTo(factionDef))
				{
					list.Add(new FactionRelation
					{
						other = faction2,
						kind = FactionRelationKind.Neutral
					});
				}
			}
			Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(factionDef, list, true);
			faction.temporary = true;
			Find.FactionManager.Add(faction);
			List<Pawn> list2 = new List<Pawn>();
			int num = 0;
			bool var2 = false;
			if (Find.Storyteller.difficulty.ChildrenAllowed)
			{
				List<ValueTuple<int, float>> list3 = new List<ValueTuple<int, float>>();
				list3.Add(new ValueTuple<int, float>(0, 0.25f));
				list3.Add(new ValueTuple<int, float>(Rand.Range(1, Mathf.Max(1, randomInRange / 2)), 0.5f));
				list3.Add(new ValueTuple<int, float>(randomInRange, 0.25f));
				ValueTuple<int, float> valueTuple;
				list3.TryRandomElementByWeight((ValueTuple<int, float> p) => p.Item2, out valueTuple);
				num = valueTuple.Item1;
				var2 = (randomInRange == num);
			}
			slate.Set<int>("childCount", num, false);
			slate.Set<bool>("allChildren", var2, false);
			for (int i = 0; i < randomInRange; i++)
			{
				DevelopmentalStage developmentalStages = (i >= randomInRange - num) ? DevelopmentalStage.Child : DevelopmentalStage.Adult;
				Pawn pawn = quest.GeneratePawn(kindDef, faction, true, null, 0f, true, null, 0f, 0f, false, true, developmentalStages, false);
				pawn.ideo.SetIdeo(precept_Relic.ideo);
				list2.Add(pawn);
			}
			quest.SetFactionHidden(faction, false, null);
			quest.PawnsArrive(list2, null, map.Parent, null, false, null, "[pilgrimsArrivedLetterLabel]", "[pilgrimsArrivedLetterText]", null, null, false, false, true);
			string text = QuestGen.GenerateNewSignal("VenerationCompleted", true);
			string text2 = QuestGenUtility.HardcodedSignalWithQuestID("relicThing.Despawned");
			QuestPart_Venerate questPart_Venerate = new QuestPart_Venerate();
			questPart_Venerate.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
			questPart_Venerate.inSignalForceExit = text2;
			questPart_Venerate.pawns.AddRange(list2);
			questPart_Venerate.target = building;
			questPart_Venerate.venerateDurationTicks = new IntRange(5000, 10000).RandomInRange;
			questPart_Venerate.faction = faction;
			questPart_Venerate.mapParent = map.Parent;
			questPart_Venerate.outSignalVenerationCompleted = text;
			quest.AddPart(questPart_Venerate);
			Quest quest2 = quest;
			string message = "[pilgrimsLeavingMessage]";
			MessageTypeDef neutralEvent = MessageTypeDefOf.NeutralEvent;
			bool getLookTargetsFromSignal = false;
			RulePack rules = null;
			string inSignal = text;
			quest2.Message(message, neutralEvent, getLookTargetsFromSignal, rules, list2, inSignal);
			string text3 = QuestGen.GenerateNewSignal("AllLeftMap", true);
			QuestPart_PassAll questPart_PassAll = new QuestPart_PassAll();
			questPart_PassAll.outSignal = text3;
			quest.AddPart(questPart_PassAll);
			slate.Set<List<Pawn>>("pawns", list2, false);
			slate.Set<Faction>("faction", faction, false);
			foreach (Pawn pawn2 in list2)
			{
				string text4 = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(pawn2.ThingID);
				QuestUtility.AddQuestTag(pawn2, text4);
				string item = QuestGenUtility.HardcodedSignalWithQuestID(text4 + ".LeftMap");
				questPart_PassAll.inSignals.Add(item);
			}
			ThingSetMakerParams parms = default(ThingSetMakerParams);
			parms.totalMarketValueRange = new FloatRange?(new FloatRange(1000f, 2000f));
			parms.qualityGenerator = new QualityGenerator?(QualityGenerator.Reward);
			parms.makingFaction = faction;
			parms.countRange = new IntRange?(new IntRange(1, 1));
			List<Thing> list4 = ThingSetMakerDefOf.Reward_ReliquaryPilgrims.root.Generate(parms);
			QuestPart_DelayedRewardDropPods delayedReward = new QuestPart_DelayedRewardDropPods();
			delayedReward.inSignal = text3;
			delayedReward.faction = faction;
			delayedReward.giver = list2[0];
			delayedReward.rewards.AddRange(list4);
			delayedReward.delayTicks = new IntRange(300000, 600000).RandomInRange;
			delayedReward.chance = 0.5f;
			QuestGen.AddTextRequest("root", delegate(string x)
			{
				delayedReward.customLetterText = x;
			}, QuestGenUtility.MergeRules(null, "[delayedRewardLetterText]", "root"));
			quest.Message("[pilgrimsLeftMessage]", MessageTypeDefOf.NeutralEvent, false, null, null, text3);
			quest.AddPart(delayedReward);
			string item2 = QuestGenUtility.HardcodedSignalWithQuestID("pawns.Arrested");
			string item3 = QuestGenUtility.HardcodedSignalWithQuestID("pawns.Killed");
			quest.AnySignal(new List<string>
			{
				item2,
				item3
			}, delegate
			{
				quest.RecordHistoryEvent(HistoryEventDefOf.CharityRefused_Pilgrims_Betrayed, null, QuestPart.SignalListenMode.Always);
				QuestPart_FactionRelationChange questPart_FactionRelationChange = new QuestPart_FactionRelationChange();
				questPart_FactionRelationChange.faction = faction;
				questPart_FactionRelationChange.relationKind = FactionRelationKind.Hostile;
				questPart_FactionRelationChange.canSendHostilityLetter = false;
				questPart_FactionRelationChange.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
				quest.AddPart(questPart_FactionRelationChange);
			}, null, QuestPart.SignalListenMode.OngoingOnly);
			string text5 = QuestGen.GenerateNewSignal("RelicInvalidated", true);
			QuestPart_PassAnyActivable questPart_PassAnyActivable = new QuestPart_PassAnyActivable();
			questPart_PassAnyActivable.inSignalEnable = QuestGen.slate.Get<string>("inSignal", null, false);
			questPart_PassAnyActivable.inSignals.Add(text2);
			questPart_PassAnyActivable.inSignals.Add(QuestGenUtility.HardcodedSignalWithQuestID("reliquary.Destroyed"));
			questPart_PassAnyActivable.outSignalsCompleted.Add(text5);
			questPart_PassAnyActivable.inSignalDisable = text;
			quest.AddPart(questPart_PassAnyActivable);
			quest.RecordHistoryEvent(HistoryEventDefOf.CharityRefused_Pilgrims, text2, QuestPart.SignalListenMode.OngoingOnly);
			QuestPart_Choice questPart_Choice = quest.RewardChoice(null, null);
			QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();
			choice.rewards.Add(new Reward_PossibleFutureReward());
			if (Faction.OfPlayer.ideos.FluidIdeo != null)
			{
				choice.rewards.Add(new Reward_DevelopmentPoints(quest));
			}
			questPart_Choice.choices.Add(choice);
			quest.End(QuestEndOutcome.Fail, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, true, false);
			quest.End(QuestEndOutcome.Fail, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("map.MapRemoved"), QuestPart.SignalListenMode.OngoingOnly, true, false);
			quest.End(QuestEndOutcome.Fail, 0, null, QuestGenUtility.HardcodedSignalWithQuestID("faction.BecameHostileToPlayer"), QuestPart.SignalListenMode.OngoingOnly, true, false);
			quest.End(QuestEndOutcome.Success, 0, null, text3, QuestPart.SignalListenMode.OngoingOnly, false, false);
			slate.Set<Map>("map", map, false);
			slate.Set<Precept_Relic>("relic", precept_Relic, false);
			slate.Set<Thing>("relicThing", var, false);
			slate.Set<Building>("reliquary", building, false);
			slate.Set<int>("pilgrimCount", randomInRange, false);
			slate.Set<string>("rewards", GenLabel.ThingsLabel(list4, "  - "), false);
			slate.Set<float>("rewardsMarketValue", TradeUtility.TotalMarketValue(list4), false);
			slate.Set<string>("venerateDate", GenDate.DateFullStringAt((long)GenDate.TickGameToAbs(quest.acceptanceTick), Find.WorldGrid.LongLatOf(map.Tile)), false);
			slate.Set<bool>("pilgrimFaction", faction.def == RatkinFactionDefOf.RK_Faction_Pilgrims, false);
		}

		// 랫킨 필그림 팩션과 PawnKind 선택
		private void GetFactionAndPawnKind_Ratkin(out FactionDef factionDef, out PawnKindDef pawnKind)
		{
			// 랫킨 필그림 팩션과 PawnKind 선택
			// 33% 확률: 기본 필그림
			// 33% 확률: 사제 필그림
			// 34% 확률: 귀족 필그림
			float rand = Verse.Rand.Value;
			if (rand < 0.33f)
			{
				factionDef = RatkinFactionDefOf.RK_Faction_Pilgrims;
				pawnKind = RatkinPawnKindDefOf.RK_PawnKind_Pilgrim;
				return;
			}
			else if (rand < 0.66f)
			{
				factionDef = RatkinFactionDefOf.RK_Faction_Pilgrims;
				pawnKind = RatkinPawnKindDefOf.RK_PawnKind_Priest;
				return;
			}
			else
			{
				factionDef = RatkinFactionDefOf.Rakinia;
				pawnKind = RatkinPawnKindDefOf.RK_PawnKind_NoblePilgrim;
				return;
			}
		}

		// 성물함 찾기 메서드 (원본에서 복사)
		private static bool TryFindReliquaryWithRelic_Ratkin(Map map, out Precept_Relic relic, out Building reliquary, out Thing relicThing)
		{
			foreach (Thing thing in map.listerThings.ThingsOfDef(ThingDefOf.Reliquary).InRandomOrder(null))
			{
				CompThingContainer compThingContainer = thing.TryGetComp<CompThingContainer>();
				if (compThingContainer != null)
				{
					foreach (Thing thing2 in ((IEnumerable<Thing>)compThingContainer.GetDirectlyHeldThings()))
					{
						Precept_Relic precept_Relic = thing2.StyleSourcePrecept as Precept_Relic;
						if (precept_Relic != null)
						{
							reliquary = (Building)thing;
							relic = precept_Relic;
							relicThing = thing2;
							return true;
						}
					}
				}
			}
			reliquary = null;
			relic = null;
			relicThing = null;
			return false;
		}

		// TestRunInt 오버라이드
		protected override bool TestRunInt(Slate slate)
		{
			Map map = QuestGen_Get.GetMap(false, null, false);
			Precept_Relic precept_Relic;
			Building building;
			Thing thing;
			return map != null && TryFindReliquaryWithRelic_Ratkin(map, out precept_Relic, out building, out thing);
		}
	}
}

