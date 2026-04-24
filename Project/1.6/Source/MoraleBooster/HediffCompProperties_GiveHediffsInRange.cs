using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 시전자 Social 스킬이 minLevel 이상일 때 적용할 버프 Hediff (더 높은 minLevel이 우선).
	/// </summary>
	public class MoraleBoosterSocialSkillTier
	{
		public int minLevel;

		public HediffDef hediff;
	}

	public class HediffCompProperties_GiveHediffsInRange : HediffCompProperties
	{
		public float range;

		public TargetingParameters targetingParameters;

		/// <summary>단일 버프만 쓸 때. socialSkillHediffTiers가 비어 있으면 이 값 사용.</summary>
		public HediffDef hediff;

		/// <summary>비어 있지 않으면 시전자의 Social 레벨로 hediff 선택.</summary>
		public List<MoraleBoosterSocialSkillTier> socialSkillHediffTiers;

		public ThingDef mote;

		public bool hideMoteWhenNotDrafted;

		public float initialSeverity = 1f;

		public bool onlyPawnsInSameFaction = true;

		public HediffCompProperties_GiveHediffsInRange()
		{
			this.compClass = typeof(HediffComp_GiveHediffsInRange);
		}
	}
}

