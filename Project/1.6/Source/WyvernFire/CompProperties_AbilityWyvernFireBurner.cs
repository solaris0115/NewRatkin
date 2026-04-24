using Verse;
using RimWorld;

namespace NewRatkin
{
	public class CompProperties_AbilityWyvernFireBurner : CompProperties_AbilityEffect
	{
		public ThingDef moteDef;

		public int numStreams;

		public float coneSizeDegrees;

		public float range;

		public float rangeNoise;

		public float barrelOffsetDistance;

		public int lifespanNoise;

		public float sizeReductionDistanceThreshold;

		public EffecterDef effecterDef;

		public CompProperties_AbilityWyvernFireBurner()
		{
			this.compClass = typeof(CompAbilityEffect_WyvernFireBurner);
		}
	}
}
