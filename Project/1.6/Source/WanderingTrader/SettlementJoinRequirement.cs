using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 유랑민 정착지 합류 조건의 기반 추상 클래스.
	/// 인스턴스별로 생성되어 풀에 저장되며 세이브/로드 시 영속.
	/// desc/descShort는 Def XML의 번역 키에서 생성 시점에 번역되어 저장됨.
	/// </summary>
	public abstract class SettlementJoinRequirement : IExposable
	{
		private const string TranslateKeyPrefix = "RK_JoinReq_";

		protected string desc;
		protected string descShort;

		/// <summary>분위기 멘트 (랜덤 택1). line1.</summary>
		public string Desc => desc ?? "";
		/// <summary>세부 필요사항. line2.</summary>
		public string DescShort => descShort ?? "";

		public abstract bool IsMet(Map map);

		/// <summary>합류 제안 UI용 세부 수치. 기본은 DescShort 반환.</summary>
		public virtual string GetRequirementDetails() => DescShort;

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref desc, "desc", "");
			Scribe_Values.Look(ref descShort, "descShort", "");
			if (Scribe.mode == LoadSaveMode.PostLoadInit && !string.IsNullOrEmpty(desc) && desc.StartsWith(TranslateKeyPrefix))
				desc = desc.Translate().RawText;
			if (Scribe.mode == LoadSaveMode.PostLoadInit && !string.IsNullOrEmpty(descShort) && descShort.StartsWith(TranslateKeyPrefix))
				descShort = descShort.Translate().RawText;
		}

		/// <summary>PawnKindDef 전용 조건 생성. extension 있으면 conditions 택1, 없으면 무조건 영입.</summary>
		public static SettlementJoinRequirement GenerateForPawnKind(PawnKindDef kind, IncidentDefExtension_WanderingCaravan ext = null)
		{
			var joinExt = kind?.GetModExtension<PawnKindDefExtension_WanderingCaravanJoin>();
			if (joinExt == null)
				return new SettlementJoinRequirementAlwaysMet();

			if (joinExt.conditions == null || joinExt.conditions.Count == 0)
				return new SettlementJoinRequirementAlwaysMet();

			int totalWeight = joinExt.conditions.Sum(c => c != null ? Math.Max(0, c.weight) : 0);
			if (totalWeight <= 0)
				return new SettlementJoinRequirementAlwaysMet();

			int roll = Rand.RangeInclusive(1, totalWeight);
			JoinConditionBase opt = null;
			foreach (var c in joinExt.conditions)
			{
				if (c == null) continue;
				int w = Math.Max(0, c.weight);
				if (roll <= w) { opt = c; break; }
				roll -= w;
			}
			if (opt == null)
				return new SettlementJoinRequirementAlwaysMet();

			return opt.CreateRequirement();
		}
	}

	/// <summary>조건 없음. 항상 영입 가능.</summary>
	public class SettlementJoinRequirementAlwaysMet : SettlementJoinRequirement
	{
		public SettlementJoinRequirementAlwaysMet() : this(null, null) { }

		public SettlementJoinRequirementAlwaysMet(string descShortOverride = null, string descOverride = null)
		{
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate().RawText : "";
		}

		public override bool IsMet(Map map) => true;
	}

	/// <summary>부상 환자 수 조건 (hp 손실된 식민지원 N명 이상)</summary>
	public class InjuredPatientCountRequirement : SettlementJoinRequirement
	{
		private int requiredCount;

		public InjuredPatientCountRequirement() { }

		public InjuredPatientCountRequirement(int count, string descShortOverride = null, string descOverride = null)
		{
			requiredCount = count;
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate(count).RawText : "";
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
		}

		public override bool IsMet(Map map)
		{
			if (map == null) return false;
			int injured = 0;
			foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
			{
				if (p != null && !p.Dead && p.health?.summaryHealth != null)
				{
					if (p.health.summaryHealth.SummaryHealthPercent < 1f)
						injured++;
				}
			}
			return injured >= requiredCount;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref requiredCount, "requiredCount", 0);
		}
	}

	/// <summary>약품 수량 조건 (허브+산업 의약품 합계 N 이상)</summary>
	public class MedicineQuantityRequirement : SettlementJoinRequirement
	{
		private int requiredCount;

		public MedicineQuantityRequirement() { }

		public MedicineQuantityRequirement(int count, string descShortOverride = null, string descOverride = null)
		{
			requiredCount = count;
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate(count).RawText : "";
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
		}

		public override bool IsMet(Map map)
		{
			if (map == null) return false;
			int total = 0;
			foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine))
				total += t.stackCount;
			return total >= requiredCount;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref requiredCount, "requiredCount", 0);
		}
	}

	/// <summary>정착지 부(WealthTotal) 조건</summary>
	public class ColonyWealthJoinRequirement : SettlementJoinRequirement
	{
		private int requiredWealth;

		public ColonyWealthJoinRequirement() { }

		public ColonyWealthJoinRequirement(int wealth, string descShortOverride = null, string descOverride = null)
		{
			requiredWealth = wealth;
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate(wealth).RawText : "";
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
		}

		public override bool IsMet(Map map)
		{
			if (map == null || map.wealthWatcher == null) return false;
			return map.wealthWatcher.WealthTotal >= requiredWealth;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref requiredWealth, "requiredWealth", 0);
		}
	}

	/// <summary>정착지 식민지원 중 지정 백스토리 보유자가 없어야 하는 조건 (귀족 등 피하고 싶을 때)</summary>
	public class ColonistBackstoryJoinRequirement : SettlementJoinRequirement
	{
		private List<string> backstoryDefNames;

		public ColonistBackstoryJoinRequirement() { }

		public ColonistBackstoryJoinRequirement(List<string> defNames, string descShortOverride = null, string descOverride = null)
		{
			backstoryDefNames = defNames ?? new List<string>();
			string summary = BuildBackstorySummary();
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate(summary).RawText : "";
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
		}

		private string BuildBackstorySummary()
		{
			if (backstoryDefNames == null || backstoryDefNames.Count == 0) return "";
			var labels = new List<string>();
			foreach (string d in backstoryDefNames)
			{
				var bd = DefDatabase<BackstoryDef>.GetNamedSilentFail(d);
				labels.Add(bd != null ? ((TaggedString)bd.TitleFor(Gender.Male)).RawText : d);
			}
			return string.Join(", ", labels);
		}

		public override bool IsMet(Map map)
		{
			if (map == null || backstoryDefNames == null || backstoryDefNames.Count == 0) return true;
			foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
			{
				if (p == null || p.Dead || p.story == null) continue;
				var child = p.story.Childhood;
				var adult = p.story.Adulthood;
				if (child != null && backstoryDefNames.Contains(child.defName)) return false;
				if (adult != null && backstoryDefNames.Contains(adult.defName)) return false;
			}
			return true;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref backstoryDefNames, "backstoryDefNames", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && backstoryDefNames == null)
				backstoryDefNames = new List<string>();
		}
	}

	/// <summary>특정 물건/건물 보유 수량 조건. mode=OR이면 하나라도 충족, AND면 전부 충족 시.</summary>
	public class ThingQuantityJoinRequirement : SettlementJoinRequirement
	{
		private List<ThingDefCountClass> thingCounts;
		private List<ThingCategoryCountEntry> categoryCounts;
		private bool modeOr;

		public ThingQuantityJoinRequirement() { }

		public ThingQuantityJoinRequirement(List<ThingDefCountClass> counts, List<ThingCategoryCountPair> categoryPairs, string mode, string descShortOverride = null, string descOverride = null)
		{
			thingCounts = counts ?? new List<ThingDefCountClass>();
			categoryCounts = new List<ThingCategoryCountEntry>();
			foreach (var p in categoryPairs ?? new List<ThingCategoryCountPair>())
			{
				if (string.IsNullOrEmpty(p?.group)) continue;
				if (System.Enum.TryParse<ThingRequestGroup>(p.group, true, out var grp))
					categoryCounts.Add(new ThingCategoryCountEntry { group = grp, requiredCount = Rand.RangeInclusive(p.countRange.min, p.countRange.max) });
			}
			modeOr = string.IsNullOrEmpty(mode) || mode.ToUpperInvariant() == "OR";
			string summary = BuildItemSummary(modeOr);
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate(summary).RawText : "";
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
		}

		private string BuildItemSummary(bool orMode)
		{
			var parts = new List<string>();
			foreach (var tc in thingCounts ?? new List<ThingDefCountClass>())
			{
				if (tc?.thingDef != null)
					parts.Add(tc.thingDef.label + " x" + tc.count);
			}
			foreach (var cc in categoryCounts ?? new List<ThingCategoryCountEntry>())
				parts.Add(cc.group.ToString() + " x" + cc.requiredCount);
			string sep = orMode ? "RK_JoinReq_ItemSeparatorOr".Translate().RawText : ", ";
			return string.Join(sep, parts);
		}

		public override bool IsMet(Map map)
		{
			if (map == null) return false;
			foreach (var tc in thingCounts ?? new List<ThingDefCountClass>())
			{
				if (tc?.thingDef == null) continue;
				int total = CountThingsOnMapForDef(map, tc.thingDef);
				bool met = total >= tc.count;
				if (modeOr && met) return true;
				if (!modeOr && !met) return false;
			}
			foreach (var cc in categoryCounts ?? new List<ThingCategoryCountEntry>())
			{
				int total = CountThingsInGroupForColony(map, cc.group);
				bool met = total >= cc.requiredCount;
				if (modeOr && met) return true;
				if (!modeOr && !met) return false;
			}
			return !modeOr && (thingCounts?.Count ?? 0) == 0 && (categoryCounts?.Count ?? 0) == 0 ? false : !modeOr;
		}

		/// <summary>맵+창고+정착민 소유 물건만 카운트. 캐러반/방문자 소유 제외.</summary>
		private static int CountThingsOnMapForDef(Map map, ThingDef def)
		{
			int total = 0;
			foreach (Thing t in map.listerThings.ThingsOfDef(def))
			{
				if (IsColonyOwned(map, t)) total += t.stackCount;
			}
			if (map.haulDestinationManager != null)
			{
				foreach (var hs in map.haulDestinationManager.AllHaulSourcesListForReading)
				{
					if (hs == null || hs is Pawn) continue;
					foreach (Thing t in hs.GetDirectlyHeldThings())
					{
						if (t?.def == def) total += t.stackCount;
					}
				}
			}
			foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
			{
				if (p == null || p.Dead) continue;
				if (p.inventory != null)
					foreach (Thing t in p.inventory.innerContainer)
						if (t?.def == def) total += t.stackCount;
				if (p.equipment != null)
					foreach (ThingWithComps eq in p.equipment.AllEquipmentListForReading)
						if (eq?.def == def) total += eq.stackCount;
			}
			return total;
		}

		/// <summary>맵+창고+정착민 소유 물건만 카운트. 캐러반/방문자 소유 제외.</summary>
		private static int CountThingsInGroupForColony(Map map, ThingRequestGroup group)
		{
			int total = 0;
			var tmp = new List<Thing>();
			map.listerThings.GetAllThings(tmp, group, null, true);
			foreach (Thing t in tmp)
			{
				if (t == null) continue;
				if (IsHeldByNonColonistPawn(map, t)) continue;
				total += t.stackCount;
			}
			foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
			{
				if (p == null || p.Dead) continue;
				if (p.inventory != null)
					foreach (Thing t in p.inventory.innerContainer)
						if (t != null && group.Includes(t.def)) total += t.stackCount;
				if (p.equipment != null)
					foreach (ThingWithComps eq in p.equipment.AllEquipmentListForReading)
						if (eq != null && group.Includes(eq.def)) total += eq.stackCount;
			}
			return total;
		}

		private static bool IsColonyOwned(Map map, Thing t)
		{
			if (t == null) return false;
			return !IsHeldByNonColonistPawn(map, t);
		}

		private static bool IsHeldByNonColonistPawn(Map map, Thing t)
		{
			for (IThingHolder h = t.ParentHolder; h != null; h = h.ParentHolder)
			{
				Pawn p = null;
				if (h is Pawn_InventoryTracker inv) p = inv.pawn;
				else if (h is Pawn_EquipmentTracker eq) p = eq.pawn;
				else if (h is Pawn pawn) p = pawn;
				if (p != null)
					return !map.mapPawns.FreeColonistsSpawned.Contains(p);
			}
			return false;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref thingCounts, "thingCounts", LookMode.Deep);
			Scribe_Collections.Look(ref categoryCounts, "categoryCounts", LookMode.Deep);
			Scribe_Values.Look(ref modeOr, "modeOr", true);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && thingCounts == null)
				thingCounts = new List<ThingDefCountClass>();
			if (Scribe.mode == LoadSaveMode.PostLoadInit && categoryCounts == null)
				categoryCounts = new List<ThingCategoryCountEntry>();
		}
	}

	internal class ThingCategoryCountEntry : IExposable
	{
		public ThingRequestGroup group;
		public int requiredCount;

		public void ExposeData()
		{
			Scribe_Values.Look(ref group, "group");
			Scribe_Values.Look(ref requiredCount, "requiredCount", 0);
		}
	}

	/// <summary>스킬 멘토 다중 조건. AND=한 pawn이 전부 충족, OR=아무 pawn이 하나라도 충족.</summary>
	public class SkillMentorMultiJoinRequirement : SettlementJoinRequirement
	{
		private List<SkillLevelEntry> skillEntries;
		private bool modeOr;

		public SkillMentorMultiJoinRequirement() { }

		public SkillMentorMultiJoinRequirement(List<SkillLevelRangePair> pairs, string mode, string descShortOverride = null, string descOverride = null)
		{
			skillEntries = new List<SkillLevelEntry>();
			foreach (var p in pairs ?? new List<SkillLevelRangePair>())
			{
				if (p?.skillDef == null) continue;
				skillEntries.Add(new SkillLevelEntry { skillDef = p.skillDef, requiredLevel = Rand.RangeInclusive(p.levelRange.min, p.levelRange.max) });
			}
			modeOr = !string.IsNullOrEmpty(mode) && mode.ToUpperInvariant() == "OR";
			string summary = BuildSkillSummary();
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate(summary).RawText : "";
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
		}

		private string BuildSkillSummary()
		{
			if (skillEntries == null || skillEntries.Count == 0) return "";
			return string.Join(", ", skillEntries
				.Where(e => e.skillDef != null)
				.Select(e => e.skillDef.label + " " + e.requiredLevel + "+"));
		}

		public override bool IsMet(Map map)
		{
			if (map == null || skillEntries == null || skillEntries.Count == 0) return false;
			if (modeOr)
			{
				foreach (var entry in skillEntries)
				{
					foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
					{
						if (p == null || p.Dead || p.skills == null) continue;
						var sk = p.skills.GetSkill(entry.skillDef);
						if (sk != null && !sk.TotallyDisabled && sk.Level >= entry.requiredLevel)
							return true;
					}
				}
				return false;
			}
			else
			{
				foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
				{
					if (p == null || p.Dead || p.skills == null) continue;
					bool allMet = true;
					foreach (var entry in skillEntries)
					{
						var sk = p.skills.GetSkill(entry.skillDef);
						if (sk == null || sk.TotallyDisabled || sk.Level < entry.requiredLevel)
						{ allMet = false; break; }
					}
					if (allMet) return true;
				}
				return false;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref skillEntries, "skillEntries", LookMode.Deep);
			Scribe_Values.Look(ref modeOr, "modeOr", false);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && skillEntries == null)
				skillEntries = new List<SkillLevelEntry>();
		}
	}

	/// <summary>정착지 식민지원 중 제국 작위 최고 랭크가 maxTitle 이하여야 하는 조건. (귀족/높은 작위 있는 곳엔 안 감)</summary>
	public class ColonistRoyalTitleJoinRequirement : SettlementJoinRequirement
	{
		private RoyalTitleDef maxTitle;
		private int maxSeniority;

		public ColonistRoyalTitleJoinRequirement() { }

		public ColonistRoyalTitleJoinRequirement(RoyalTitleDef title, string descShortOverride = null, string descOverride = null)
		{
			maxTitle = title;
			maxSeniority = title != null ? title.seniority : 0;
			string label = title != null ? title.GetLabelCapForBothGenders() : "";
			descShort = !string.IsNullOrEmpty(descShortOverride) ? descShortOverride.Translate(label).RawText : "";
			desc = !string.IsNullOrEmpty(descOverride) ? descOverride.Translate().RawText : "";
		}

		public override bool IsMet(Map map)
		{
			if (map == null) return true;
			var empire = Faction.OfEmpire;
			if (empire == null || !ModsConfig.RoyaltyActive) return true;

			int highestSeniority = 0;
			foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
			{
				if (p == null || p.Dead || p.royalty == null) continue;
				int sen = p.GetCurrentTitleSeniorityIn(empire);
				if (sen > highestSeniority) highestSeniority = sen;
			}
			return highestSeniority <= maxSeniority;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref maxTitle, "maxTitle");
			Scribe_Values.Look(ref maxSeniority, "maxSeniority", 0);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && maxTitle != null)
				maxSeniority = maxTitle.seniority;
		}
	}

	internal class SkillLevelEntry : IExposable
	{
		public SkillDef skillDef;
		public int requiredLevel;

		public void ExposeData()
		{
			Scribe_Defs.Look(ref skillDef, "skillDef");
			Scribe_Values.Look(ref requiredLevel, "requiredLevel", 0);
		}
	}

}
