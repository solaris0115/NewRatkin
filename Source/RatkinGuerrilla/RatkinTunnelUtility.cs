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
}
