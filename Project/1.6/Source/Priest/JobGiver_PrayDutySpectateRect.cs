using RimWorld;
using Verse;
using Verse.AI;

namespace NewRatkin
{
    public class JobGiver_PrayDutySpectateRect : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			PawnDuty duty = pawn.mindState.duty;
			if (duty == null)
			{
				return null;
			}
			if ((duty.spectateRectPreferredSide == SpectateRectSide.None || 
				!SpectatorCellFinder.TryFindSpectatorCellFor(pawn, duty.spectateRect, pawn.Map, out IntVec3 cell, duty.spectateRectPreferredSide)) 
				&& !SpectatorCellFinder.TryFindSpectatorCellFor(pawn, duty.spectateRect, pawn.Map, out cell, duty.spectateRectAllowedSides))
			{
				return null;
			}
			IntVec3 centerCell = duty.spectateRect.CenterCell;
			Building edifice = cell.GetEdifice(pawn.Map);
			if (edifice != null && edifice.def.category == ThingCategory.Building && edifice.def.building.isSittable && pawn.CanReserve(edifice))
			{
				return JobMaker.MakeJob(RatkinJobDefOf.RK_Job_SpectatePray, edifice, centerCell);
			}
			return JobMaker.MakeJob(RatkinJobDefOf.RK_Job_SpectatePray, cell, centerCell);
		}
	}
}
