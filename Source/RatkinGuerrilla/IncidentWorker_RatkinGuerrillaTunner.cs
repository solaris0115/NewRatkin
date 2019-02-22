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
            /*
             * 5개 미만, 적절한 공간이 있으면 격발
             */
            Map map = (Map)parms.target;
            Zone zone = map.zoneManager.AllZones.FindAll((Zone z) => z is Zone_Stockpile).RandomElement<Zone>();
            if(zone !=null)
            {
                Log.Message("StockPile" + zone.Position);
                Log.Message("Room " + map.regionGrid.GetValidRegionAt(zone.Position).Room.ID);

                Log.Message("---------FoodSource---------");
                foreach (Thing t in map.regionGrid.GetValidRegionAt(zone.Position).ListerThings.ThingsInGroup(ThingRequestGroup.Everything))
                {
                    Log.Message(t.Label);
                }
                Log.Message("---------FoodSourceNotPlantOrTree---------");
                foreach (Thing t in map.regionGrid.GetValidRegionAt(zone.Position).ListerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
                {
                    Log.Message(t.Label);
                }
            }
            return base.CanFireNowSub(parms) && RatkinTunnelUtility.TotalSpawnedTunnelCount(map) < 2 /*셀 여부 확인*/;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int tunnelCount = 0;//Mathf.Max(GenMath.RoundRandom(parms.points / tunnelPoints), 1);
            Thing t = SpawnTunnels(tunnelCount, map);
            SendStandardLetter(t, null, new string[0]);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }
        private Thing SpawnTunnels(int tunnelCount, Map map)
        {
            IntVec3 loc;
            if (!RatkinTunnelCellFinder.TryFindCell(out loc, map))
            {
                return null;
            }
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(RatkinBuildingDefOf.RK_GuerrillaTunnel, null), loc, map, WipeMode.FullRefund);
            return thing;
        }
    }
}
