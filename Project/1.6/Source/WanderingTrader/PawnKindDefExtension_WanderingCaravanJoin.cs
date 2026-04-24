using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// PawnKindDef별 유랑민 합류 조건 정의. XML에서 택1로 사용.
	/// 각 서브클래스는 고유한 필드·로직을 가질 수 있음.
	/// </summary>
	public abstract class JoinConditionBase
	{
		/// <summary>분위기 멘트 목록. 생성 시 랜덤 택1. XML에서 li로 복수 입력. (Desc1, Desc2 등)</summary>
		public List<string> desc;
		/// <summary>조건 설명(세부 필요사항). 번역 키. {0} 등 placeholder 사용 가능.</summary>
		public string descShort;
		/// <summary>선택 가중치. 미지정 시 1. 전체 합산 후 랜덤 정수로 택1.</summary>
		public int weight = 1;

		/// <summary>desc 목록에서 랜덤으로 하나 선택. 비어있으면 null.</summary>
		protected string PickDesc()
		{
			if (desc == null || desc.Count == 0) return null;
			return desc.RandomElement();
		}

		/// <summary>이 조건에서 SettlementJoinRequirement 생성. Pawn마다 랜덤 값 적용.</summary>
		public abstract SettlementJoinRequirement CreateRequirement();
	}

	/// <summary>부상 환자 수 조건 (countRange에서 랜덤)</summary>
	public class JoinCondition_InjuredPatientCount : JoinConditionBase
	{
		public IntRange countRange = new IntRange(3, 5);

		public override SettlementJoinRequirement CreateRequirement()
		{
			int value = Rand.RangeInclusive(countRange.min, countRange.max);
			return new InjuredPatientCountRequirement(value, descShort, PickDesc());
		}
	}

	/// <summary>약품 수량 조건 (countRange에서 랜덤)</summary>
	public class JoinCondition_MedicineQuantity : JoinConditionBase
	{
		public IntRange countRange = new IntRange(10, 20);

		public override SettlementJoinRequirement CreateRequirement()
		{
			int value = Rand.RangeInclusive(countRange.min, countRange.max);
			return new MedicineQuantityRequirement(value, descShort, PickDesc());
		}
	}

	/// <summary>정착지 부(WealthTotal) 조건 (countRange에서 랜덤)</summary>
	public class JoinCondition_ColonyWealth : JoinConditionBase
	{
		public IntRange countRange = new IntRange(50000, 150000);

		public override SettlementJoinRequirement CreateRequirement()
		{
			int value = Rand.RangeInclusive(countRange.min, countRange.max);
			return new ColonyWealthJoinRequirement(value, descShort, PickDesc());
		}
	}

	/// <summary>조건 없음. 확률적으로 선택되면 무조건 합류.</summary>
	public class JoinCondition_AlwaysMet : JoinConditionBase
	{
		public override SettlementJoinRequirement CreateRequirement()
		{
			return new SettlementJoinRequirementAlwaysMet(descShort, PickDesc());
		}
	}

	/// <summary>정착지 식민지원 중 backstoryList에 해당하는 백스토리 보유자가 없어야 하는 조건</summary>
	public class JoinCondition_ColonistBackstory : JoinConditionBase
	{
		public List<string> backstoryList;

		public override SettlementJoinRequirement CreateRequirement()
		{
			return new ColonistBackstoryJoinRequirement(backstoryList ?? new List<string>(), descShort, PickDesc());
		}
	}

	/// <summary>특정 물건/건물 보유 수량 조건. mode=OR이면 하나라도 충족 시, AND면 전부 충족 시.</summary>
	public class JoinCondition_ThingQuantity : JoinConditionBase
	{
		/// <summary>OR: 하나라도 충족 시, AND: 전부 충족 시</summary>
		public string mode = "OR";
		/// <summary>ThingDef:count 목록.</summary>
		public List<ThingDefCountClass> thingCounts;
		/// <summary>ThingRequestGroup:countRange 목록. Weapon, Apparel 등.</summary>
		public List<ThingCategoryCountPair> thingCategoryCounts;

		public override SettlementJoinRequirement CreateRequirement()
		{
			return new ThingQuantityJoinRequirement(thingCounts ?? new List<ThingDefCountClass>(), thingCategoryCounts ?? new List<ThingCategoryCountPair>(), mode, descShort, PickDesc());
		}
	}

	/// <summary>ThingRequestGroup + countRange. XML: group=Weapon/Apparel 등, countRange=3~6</summary>
	public class ThingCategoryCountPair
	{
		public string group = "Weapon";
		public IntRange countRange = new IntRange(1, 5);
	}

	/// <summary>스킬 멘토 조건. AND=한 pawn이 전부 충족, OR=아무 pawn이 하나라도 충족.</summary>
	public class JoinCondition_SkillMentorMulti : JoinConditionBase
	{
		/// <summary>AND: 한 pawn이 전부 충족, OR: 아무 pawn이 하나라도 충족</summary>
		public string mode = "AND";
		/// <summary>SkillDef:levelRange. Melee, Shooting 등.</summary>
		public List<SkillLevelRangePair> skillList;

		public override SettlementJoinRequirement CreateRequirement()
		{
			return new SkillMentorMultiJoinRequirement(skillList ?? new List<SkillLevelRangePair>(), mode, descShort, PickDesc());
		}
	}

	/// <summary>SkillDef + levelRange. XML: skillDef=Melee, levelRange=5~8</summary>
	public class SkillLevelRangePair
	{
		public SkillDef skillDef;
		public IntRange levelRange = new IntRange(5, 8);
	}

	/// <summary>정착지 식민지원 중 제국 작위 최고 랭크가 maxTitle 이하여야 하는 조건. (하인 등: 높은 작위 있는 곳엔 안 감)</summary>
	public class JoinCondition_ServantRoyalTitle : JoinConditionBase
	{
		/// <summary>이 작위 이하여야 합류. 예: Acolyte = 수련사 이하.</summary>
		public RoyalTitleDef maxTitle;

		public override SettlementJoinRequirement CreateRequirement()
		{
			return new ColonistRoyalTitleJoinRequirement(maxTitle, descShort, PickDesc());
		}
	}

	/// <summary>
	/// PawnKindDef별 유랑민 합류 조건 오버라이드.
	/// conditions가 비어있으면 무조건 영입 가능.
	/// </summary>
	public class PawnKindDefExtension_WanderingCaravanJoin : DefModExtension
	{
		/// <summary>택1 조건 목록. 비어있으면 조건 없음(항상 영입 가능).</summary>
		public List<JoinConditionBase> conditions;
	}
}
