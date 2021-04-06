using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace NewRatkin
{
    public class JobGiver_GotoTravelInteractionCell : ThinkNode_JobGiver
	{
		private LocomotionUrgency locomotionUrgency = LocomotionUrgency.Walk;

		private Danger maxDanger = Danger.Some;

		private int jobMaxDuration = 999999;

		private IntRange WaitTicks = new IntRange(30, 80);

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_GotoTravelInteractionCell obj = (JobGiver_GotoTravelInteractionCell)base.DeepCopy(resolve);
			obj.locomotionUrgency = locomotionUrgency;
			obj.maxDanger = maxDanger;
			obj.jobMaxDuration = jobMaxDuration;
			return obj;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			pawn.mindState.nextMoveOrderIsWait = !pawn.mindState.nextMoveOrderIsWait;

			if (!pawn.mindState.duty.focus.HasThing)
            {
				return null;
            }

			Thing thing = pawn.mindState.duty.focus.Thing;

			IntVec3 cell = thing.InteractionCell;
			if (!pawn.CanReach(cell, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(pawn, maxDanger)))
			{
				return null;
			}
			if (pawn.Position == cell)
			{
				return null;
			}

			Job job2 = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job2.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, locomotionUrgency);
			job2.expiryInterval = jobMaxDuration;
			return job2;
		}
	}
}
