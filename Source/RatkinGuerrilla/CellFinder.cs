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
    public static class RatkinTunnelCellFinder
    {
        public static bool TryFindCell(out IntVec3 cell, Map map)
        {
            foreach(Zone zone in map.zoneManager.AllZones.FindAll((Zone z) => z is Zone_Stockpile))
            {
                zone.
            }
            if (zone != null)
            {
                Log.Message("StockPile" + zone.Position);
                foreach (Region region in RegionAndRoomQuery.RoomAt(zone.Position, map).Regions)
                {
                    Log.Message("region" + region.id + ": " + region.ListerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree).Count);
                }
            }
            cell = 
            return true;
        }
    }
}
