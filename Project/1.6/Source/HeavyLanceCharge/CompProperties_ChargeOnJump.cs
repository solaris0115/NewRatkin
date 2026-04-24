using Verse;

namespace NewRatkin
{
	public class CompProperties_ChargeOnJump : RimWorld.CompProperties_AbilityEffect
	{
		public ThingDef pawnFlyerDef;
		public HediffDef exhaustionHediffDef;
		public int exhaustionDurationTicks;
		public HediffDef focusHediffDef;
		public int focusDurationTicks;
		public HediffDef momentumHediffDef;
		public int momentumDurationTicks = 600;
		public bool onlyHostilePawns = true;

		public CompProperties_ChargeOnJump()
		{
			compClass = typeof(CompAbilityEffect_ChargeOnJump);
		}
	}
}
