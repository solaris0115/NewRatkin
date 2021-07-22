using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewRatkin
{
    public class LordToil_JoinPrayerService : LordToil
    {
        Pawn organizer;
        IntVec3 spot;

        public LordToil_JoinPrayerService(Pawn organizer, IntVec3 spot)
        {
            this.organizer = organizer;
            this.spot = spot;
        }

        public override ThinkTreeDutyHook VoluntaryJoinDutyHookFor(Pawn p)
        {
            return RatkinDutyDefOf.RK_JoinPrayerService.hook;
        }

		public override void UpdateAllDuties()
		{
			foreach (var pawn in lord.ownedPawns)
			{
				pawn.mindState.duty = new PawnDuty(RatkinDutyDefOf.RK_JoinPrayerService, spot);
			}
		}
	}
}
