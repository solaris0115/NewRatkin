using System.Collections.Generic;
using Verse;
using RimWorld;
namespace NewRatkin
{
    public static class RatkinTunnelCellFinder
    {
        public static bool FindFoodStockpile(out IntVec3 cell, Map map)
        {
            List<IntVec3> candidate = new List<IntVec3>();
            int zoneCount;
            foreach (Zone_Stockpile zone in map.zoneManager.AllZones.FindAll((Zone z) => z is Zone_Stockpile))
            {
                zoneCount = 0;
                
                foreach (Region region in RegionAndRoomQuery.RoomAt(zone.Position, map).Regions)
                {
                    zoneCount += region.ListerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree).Count;
                    if (zoneCount > 5)
                    {
                        foreach(Region r in RegionAndRoomQuery.RoomAt(zone.Position,map).Regions)
                        {
                            int wallIndex = r.extentsClose.maxZ + 1;
                            int maxZ = r.extentsClose.maxZ;

                            for (int index = r.extentsClose.minX; index <= r.extentsClose.maxX; index++)
                            {
                                IntVec3 wallPosition = new IntVec3(index, 0, wallIndex);
                                Thing wall = map.thingGrid.ThingAt(wallPosition, ThingCategory.Building);
                                Thing passingPoint = map.thingGrid.ThingAt(new IntVec3(index, 0, maxZ), ThingCategory.Building);

                                if (wall != null && wall.def.fillPercent >= 1 && wall.def.graphicData.linkType == LinkDrawerType.CornerFiller && (passingPoint == null || passingPoint.def.passability != Traversability.Impassable))
                                {
                                    candidate.Add(wallPosition);
                                }
                            }
                        }
                        break;
                    }
                }
            }
            if(candidate.Count>0)
            {
                cell = candidate.RandomElement();
                return true;
            }
            else
            {
                cell = new IntVec3();
                return false;
            }
        }

        public static bool FindPowerPlantNearCell(out IntVec3 cell, Map map)
        {
            List<IntVec3> candidate = new List<IntVec3>();

            foreach(PowerNet net in map.powerNetManager.AllNetsListForReading)
            {
                foreach (CompPowerTrader t in net.powerComps)
                {
                    IntVec3 temp;
                    if(CellFinder.TryFindRandomCellNear(t.parent.Position, map, 3,(IntVec3 vec)=> !vec.UsesOutdoorTemperature(map) && vec.Standable(map), out temp))
                    {
                        candidate.Add(temp);
                    }
                }
            }
            if(candidate.Count>0)
            {
                cell = candidate.RandomElement();
                return true;
            }
            cell = new IntVec3();
            return false;
        }
    }
}
