using System;
using RimWorld;
using Verse;

namespace NewRatkin
{
	public class CompProperties_AbilityBurnerCustom : CompProperties_AbilityEffect
	{
		public ThingDef moteDef;

		public int numStreams;

		public float coneSizeDegrees;

		public float range;

		public float rangeNoise;

		public float barrelOffsetDistance;

		public float spawnOffsetDistance = 0f; // pawn으로부터의 시작 위치 오프셋 (양수: 앞쪽, 음수: 뒤쪽)

		public int lifespanNoise;

		public float lifespanMultiplier = 5f; // 거리 기반 lifespan 계산 배율 (거리 * 이 값)

		public int? lifespanTicks; // 고정 lifespan 틱 수 (지정 시 거리 기반 계산 무시)

		public float sizeReductionDistanceThreshold;

		public EffecterDef effecterDef;

		public CompProperties_AbilityBurnerCustom()
		{
			this.compClass = typeof(CompAbilityEffect_BurnerCustom);
		}
	}
}
