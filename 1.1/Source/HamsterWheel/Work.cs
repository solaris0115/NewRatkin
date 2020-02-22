using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace NewRatkin
{
    public class WorkGiver_HamsterWheel : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForDef(RK_ThingDefOf.RK_HamsterWheelGenerator);
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(RK_ThingDefOf.RK_HamsterWheelGenerator).Cast<Thing>();
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                if (allBuildingsColonist[i].def == RK_ThingDefOf.RK_HamsterWheelGenerator)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building building = t as Building;
            bool flag = building == null;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                if (building.IsForbidden(pawn))
                {
                    result = false;
                }
                else
                {
                    LocalTargetInfo target = building;
                    if (!pawn.CanReserve(target, 1, -1, null, forced))
                    {
                        result = false;
                    }
                    else
                    {
                        CompPowerPlantHamsterWheel compHW = building.TryGetComp<CompPowerPlantHamsterWheel>();
                        result = (compHW.CanUseNow && /*compHW.user == null &&*/ !building.Position.IsInPrisonCell(pawn.Map) && !building.IsBurning());
                    }
                }
            }
            return result;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return new Job(RK_JobDefOf.RK_Job_HamsterWheel, t, 1500, true);
        }
    }
}
