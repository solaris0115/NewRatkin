using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using System.Text;
using System.Reflection;
using RimWorld;
using Harmony;
using UnityEngine;
using Verse.AI;
namespace NewRatkin
{
    //침투
    public class IncidentWorker_RatkinGuerrillaTunner : IncidentWorker
    {
        private const float tunnelPoints = 220f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 intVec;
            return base.CanFireNowSub(parms) && Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia).HostileTo(Faction.OfPlayer) && (RatkinTunnelUtility.TotalSpawnedTunnelCount(map) < 2) && RatkinTunnelCellFinder.FindPowerPlantNearCell(out intVec, map);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Thing t = SpawnTunnels(map);
            if (t != null)
            {
                SendStandardLetter(t, null, new string[0]);
                //Find.TickManager.slower.SignalForceNormalSpeedShort();
                return true;
            }
            return false;
        }
        private Thing SpawnTunnels(Map map)
        {
            IntVec3 loc;
            if (!RatkinTunnelCellFinder.FindPowerPlantNearCell(out loc, map))
            {
                return null;
            }
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(RatkinBuildingDefOf.RK_GuerrillaTunnelSpawner, null), loc, map, WipeMode.FullRefund);
            return thing;
        }
    }
}
