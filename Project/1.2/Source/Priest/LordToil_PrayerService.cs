using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewRatkin
{
    public class LordToil_PrayerService : LordToil
	{
		Building pulpit;
		Pawn organizer;
		IntVec3 spot;

		int tickCounter;

		public CellRect spectatorRect;

		public LordToil_PrayerService(Building pulpit, Pawn organizer, IntVec3 spot, CellRect spectatorRect)
		{
			this.pulpit = pulpit;
			this.organizer = organizer;
			this.spot = spot;

			this.spectatorRect = spectatorRect;
		}

		public override ThinkTreeDutyHook VoluntaryJoinDutyHookFor(Pawn p)
		{
			if (p == organizer)
			{
				return RatkinDutyDefOf.RK_OrganizePrayerService.hook;
			}

			return RatkinDutyDefOf.RK_SpectatePrayerService.hook;
		}

        public override void Init()
        {
            base.Init();

			tickCounter = GenTicks.TicksGame + Rand.Range(ConstPriest.PrayerServiceEffectDelayTickMin, ConstPriest.PrayerServiceEffectDelayTickMax);
		}

        public override void LordToilTick()
        {
			if (GenTicks.TicksGame > tickCounter)
            {
				foreach (var pawn in lord.ownedPawns)
                {
					pawn.needs.mood.thoughts.memories.TryGainMemory(RatkinThoughtDefOf.RK_AttendPrayerMeetingMood);
                }

				tickCounter = GenTicks.TicksGame + Rand.Range(ConstPriest.PrayerServiceEffectDelayTickMin, ConstPriest.PrayerServiceEffectDelayTickMax);
            }
        }

        public override void UpdateAllDuties()
		{
			foreach (var pawn in lord.ownedPawns)
            {
				if (pawn == organizer)
                {
					pawn.mindState.duty = new PawnDuty(RatkinDutyDefOf.RK_OrganizePrayerService, pulpit);
					continue;
                }

				pawn.mindState.duty = new PawnDuty(RatkinDutyDefOf.RK_SpectatePrayerService)
				{
					spectateRect = spectatorRect,
					spectateRectAllowedSides = SpectateRectSide.All,
				};
            }
		}
	}
}
