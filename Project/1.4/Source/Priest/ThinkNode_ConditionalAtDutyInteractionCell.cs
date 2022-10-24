using Verse;
using Verse.AI;

namespace NewRatkin
{
    public class ThinkNode_ConditionalAtDutyInteractionCell : ThinkNode_Conditional
	{
		protected override bool Satisfied(Pawn pawn)
		{
			if (pawn.mindState.duty?.focus.HasThing ?? false)
			{
				return pawn.Position == pawn.mindState.duty.focus.Thing.InteractionCell;
			}

			return false;
		}
	}
}
