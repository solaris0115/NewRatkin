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
    [DefOf]
    public static class RatkinBuildingDefOf
    {
        public static ThingDef RK_GuerrillaTunnel;
    }

    public class IncidentWorker_RatkinGuerrillaTunner : IncidentWorker
    {
        private const float tunnelPoints = 220f;

        /// <summary>
        /// 현재 이 이벤트가 발동 가능한지 여부
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 intVec;
            return base.CanFireNowSub(parms) && RatkinTunnelUtility.TotalSpawnedTunnelCount(map) < 5 && InfestationCellFinder.TryFindCell(out intVec, map);
        }
        /// <summary>
        /// 지금 이 이벤트 실행을 시도해봄.
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int tunnelCount = Mathf.Max(GenMath.RoundRandom(parms.points / tunnelPoints), 1);
            Thing t = this.SpawnTunnels(tunnelCount, map);
            base.SendStandardLetter(t, null, new string[0]);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }
        /// <summary>
        /// 터널을 만듦
        /// </summary>
        /// <param name="tunnelCount"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        private Thing SpawnTunnels(int tunnelCount, Map map)
        {
            IntVec3 loc;
            //셀파인더
            if (!InfestationCellFinder.TryFindCell(out loc, map))
            {
                return null;
            }
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(RatkinBuildingDefOf.RK_GuerrillaTunnel, null), loc, map, WipeMode.FullRefund);
            for (int i = 0; i < tunnelCount - 1; i++)
            {
                loc = CompSpawnerHives.FindChildHiveLocation(thing.Position, map, ThingDefOf.Hive, ThingDefOf.Hive.GetCompProperties<CompProperties_SpawnerHives>(), false, true);
                if (loc.IsValid)
                {
                    thing = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.TunnelHiveSpawner, null), loc, map, WipeMode.FullRefund);
                }
            }
            return thing;
        }
    }

    public static class RatkinTunnelUtility
    {
        private const float TunnelPreventsClaimingInRadius = 2f;

        public static int TotalSpawnedTunnelCount(Map map)
        {
            return map.listerThings.ThingsOfDef(RatkinBuildingDefOf.RK_GuerrillaTunnel).Count;
        }

        public static bool AnyTunnelPreventsClaiming(Thing thing)
        {
            if (!thing.Spawned)
            {
                return false;
            }
            int num = GenRadial.NumCellsInRadius(2f);
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = thing.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(thing.Map) && c.GetFirstThing<Thing>(thing.Map) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static void Notify_TunnelDespawned(Hive hive, Map map)
        {
            int num = GenRadial.NumCellsInRadius(2f);
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = hive.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j].Faction == Faction.OfInsects && !RatkinTunnelUtility.AnyTunnelPreventsClaiming(thingList[j]))
                        {
                            thingList[j].SetFaction(null, null);
                        }
                    }
                }
            }
        }
    }

    public class Tunnel_Guerrilla:ThingWithComps
    {

    }
}
