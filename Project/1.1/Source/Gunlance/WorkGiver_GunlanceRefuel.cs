using System;
using Verse;
using Verse.AI;
using RimWorld;

namespace NewRatkin
{/*
	public class WorkGiver_GunlanceRefuel : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest
		{
			get
			{
				return ThingRequest.ForDef(ThingDefOf.)
			}
		}

		public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.Touch;
			}
		}

		public virtual JobDef JobStandard
		{
			get
			{
				return JobDefOf.Refuel;
			}
		}

		public virtual JobDef JobAtomic
		{
			get
			{
				return JobDefOf.RefuelAtomic;
			}
		}

		public virtual bool CanRefuelThing(Thing t)
		{
			return !(t is Building_Turret);
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return this.CanRefuelThing(t) && RefuelWorkGiverUtility.CanRefuel(pawn, t, forced);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return RefuelWorkGiverUtility.RefuelJob(pawn, t, forced, this.JobStandard, this.JobAtomic);
		}
	}*/
}
