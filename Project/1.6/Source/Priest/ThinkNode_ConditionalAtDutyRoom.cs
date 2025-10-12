using Verse;
using Verse.AI;

namespace NewRatkin
{
    public class ThinkNode_ConditionalAtDutyRoom : ThinkNode_Conditional
    {
		protected override bool Satisfied(Pawn pawn)
		{
			if (pawn.mindState.duty != null)
			{
				return pawn.GetRoom() == pawn.mindState.duty.focus.Cell.GetRoom(pawn.Map);
			}

			return false;
		}
	}
}
