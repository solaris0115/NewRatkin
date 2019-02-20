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
        private static List<RatkinTunnelCellFinder.LocationCandidate> locationCandidates = new List<RatkinTunnelCellFinder.LocationCandidate>();

        private static Dictionary<Region, float> regionsDistanceToUnroofed = new Dictionary<Region, float>();

        private static ByteGrid closedAreaSize;

        private static ByteGrid distToColonyBuilding;

        private const float MinRequiredScore = 7.5f;

        private const float MinMountainousnessScore = 0.17f;

        private const int MountainousnessScoreRadialPatternIdx = 700;

        private const int MountainousnessScoreRadialPatternSkip = 10;

        private const float MountainousnessScorePerRock = 1f;

        private const float MountainousnessScorePerThickRoof = 0.5f;

        private const float MinCellTempToSpawnHive = -17f;

        private const float MaxDistanceToColonyBuilding = 30f;

        private static List<Pair<IntVec3, float>> tmpCachedInfestationChanceCellColors;

        private static HashSet<Region> tempUnroofedRegions = new HashSet<Region>();

        private static List<IntVec3> tmpColonyBuildingsLocs = new List<IntVec3>();

        private static List<KeyValuePair<IntVec3, float>> tmpDistanceResult = new List<KeyValuePair<IntVec3, float>>();

        public static bool TryFindCell(out IntVec3 cell, Map map)
        {





            RatkinTunnelCellFinder.CalculateLocationCandidates(map);
            RatkinTunnelCellFinder.LocationCandidate locationCandidate;
            if (!RatkinTunnelCellFinder.locationCandidates.TryRandomElementByWeight((RatkinTunnelCellFinder.LocationCandidate x) => x.score, out locationCandidate))
            {
                cell = IntVec3.Invalid;
                return false;
            }
            cell = CellFinder.FindNoWipeSpawnLocNear(locationCandidate.cell, map, ThingDefOf.Hive, Rot4.North, 2, (IntVec3 x) => RatkinTunnelCellFinder.GetScoreAt(x, map) > 0f && x.GetFirstThing(map, ThingDefOf.Hive) == null && x.GetFirstThing(map, ThingDefOf.TunnelHiveSpawner) == null);
            return true;
        }

        private static float GetScoreAt(IntVec3 cell, Map map)
        {
            if ((float)RatkinTunnelCellFinder.distToColonyBuilding[cell] > 30f)
            {
                return 0f;
            }
            if (!cell.Walkable(map))
            {
                return 0f;
            }
            if (cell.Fogged(map))
            {
                return 0f;
            }
            if (RatkinTunnelCellFinder.CellHasBlockingThings(cell, map))
            {
                return 0f;
            }
            if (!cell.Roofed(map) || !cell.GetRoof(map).isThickRoof)
            {
                return 0f;
            }
            Region region = cell.GetRegion(map, RegionType.Set_Passable);
            if (region == null)
            {
                return 0f;
            }
            if (RatkinTunnelCellFinder.closedAreaSize[cell] < 2)
            {
                return 0f;
            }
            float temperature = cell.GetTemperature(map);
            if (temperature < -17f)
            {
                return 0f;
            }
            float mountainousnessScoreAt = RatkinTunnelCellFinder.GetMountainousnessScoreAt(cell, map);
            if (mountainousnessScoreAt < 0.17f)
            {
                return 0f;
            }
            int num = RatkinTunnelCellFinder.StraightLineDistToUnroofed(cell, map);
            float num2;
            if (!RatkinTunnelCellFinder.regionsDistanceToUnroofed.TryGetValue(region, out num2))
            {
                num2 = (float)num * 1.15f;
            }
            else
            {
                num2 = Mathf.Min(num2, (float)num * 4f);
            }
            num2 = Mathf.Pow(num2, 1.55f);
            float num3 = Mathf.InverseLerp(0f, 12f, (float)num);
            float num4 = Mathf.Lerp(1f, 0.18f, map.glowGrid.GameGlowAt(cell, false));
            float num5 = 1f - Mathf.Clamp(RatkinTunnelCellFinder.DistToBlocker(cell, map) / 11f, 0f, 0.6f);
            float num6 = Mathf.InverseLerp(-17f, -7f, temperature);
            float num7 = num2 * num3 * num5 * mountainousnessScoreAt * num4 * num6;
            num7 = Mathf.Pow(num7, 1.2f);
            if (num7 < 7.5f)
            {
                return 0f;
            }
            return num7;
        }

        public static void DebugDraw()
        {
            if (DebugViewSettings.drawInfestationChance)
            {
                if (RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors == null)
                {
                    RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors = new List<Pair<IntVec3, float>>();
                }
                if (Time.frameCount % 8 == 0)
                {
                    RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors.Clear();
                    Map currentMap = Find.CurrentMap;
                    CellRect cellRect = Find.CameraDriver.CurrentViewRect;
                    cellRect.ClipInsideMap(currentMap);
                    cellRect = cellRect.ExpandedBy(1);
                    RatkinTunnelCellFinder.CalculateTraversalDistancesToUnroofed(currentMap);
                    RatkinTunnelCellFinder.CalculateClosedAreaSizeGrid(currentMap);
                    RatkinTunnelCellFinder.CalculateDistanceToColonyBuildingGrid(currentMap);
                    float num = 0.001f;
                    for (int i = 0; i < currentMap.Size.z; i++)
                    {
                        for (int j = 0; j < currentMap.Size.x; j++)
                        {
                            IntVec3 cell = new IntVec3(j, 0, i);
                            float scoreAt = RatkinTunnelCellFinder.GetScoreAt(cell, currentMap);
                            if (scoreAt > num)
                            {
                                num = scoreAt;
                            }
                        }
                    }
                    for (int k = 0; k < currentMap.Size.z; k++)
                    {
                        for (int l = 0; l < currentMap.Size.x; l++)
                        {
                            IntVec3 intVec = new IntVec3(l, 0, k);
                            if (cellRect.Contains(intVec))
                            {
                                float scoreAt2 = RatkinTunnelCellFinder.GetScoreAt(intVec, currentMap);
                                if (scoreAt2 > 7.5f)
                                {
                                    float second = GenMath.LerpDouble(7.5f, num, 0f, 1f, scoreAt2);
                                    RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors.Add(new Pair<IntVec3, float>(intVec, second));
                                }
                            }
                        }
                    }
                }
                for (int m = 0; m < RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors.Count; m++)
                {
                    IntVec3 first = RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors[m].First;
                    float second2 = RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors[m].Second;
                    CellRenderer.RenderCell(first, SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 1f, second2), false));
                }
            }
            else
            {
                RatkinTunnelCellFinder.tmpCachedInfestationChanceCellColors = null;
            }
        }

        private static void CalculateLocationCandidates(Map map)
        {
            RatkinTunnelCellFinder.locationCandidates.Clear();
            RatkinTunnelCellFinder.CalculateTraversalDistancesToUnroofed(map);
            RatkinTunnelCellFinder.CalculateClosedAreaSizeGrid(map);
            RatkinTunnelCellFinder.CalculateDistanceToColonyBuildingGrid(map);
            for (int i = 0; i < map.Size.z; i++)
            {
                for (int j = 0; j < map.Size.x; j++)
                {
                    IntVec3 cell = new IntVec3(j, 0, i);
                    float scoreAt = RatkinTunnelCellFinder.GetScoreAt(cell, map);
                    if (scoreAt > 0f)
                    {
                        RatkinTunnelCellFinder.locationCandidates.Add(new RatkinTunnelCellFinder.LocationCandidate(cell, scoreAt));
                    }
                }
            }
        }

        private static bool CellHasBlockingThings(IntVec3 cell, Map map)
        {
            List<Thing> thingList = cell.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Pawn || thingList[i] is Hive || thingList[i] is TunnelHiveSpawner)
                {
                    return true;
                }
                bool flag = thingList[i].def.category == ThingCategory.Building && thingList[i].def.passability == Traversability.Impassable;
                if (flag && GenSpawn.SpawningWipes(ThingDefOf.Hive, thingList[i].def))
                {
                    return true;
                }
            }
            return false;
        }

        private static int StraightLineDistToUnroofed(IntVec3 cell, Map map)
        {
            int num = int.MaxValue;
            int i = 0;
            while (i < 4)
            {
                Rot4 rot = new Rot4(i);
                IntVec3 facingCell = rot.FacingCell;
                int num2 = 0;
                int num3;
                for (; ; )
                {
                    IntVec3 intVec = cell + facingCell * num2;
                    if (!intVec.InBounds(map))
                    {
                        goto Block_1;
                    }
                    num3 = num2;
                    if (RatkinTunnelCellFinder.NoRoofAroundAndWalkable(intVec, map))
                    {
                        break;
                    }
                    num2++;
                }
                IL_6F:
                if (num3 < num)
                {
                    num = num3;
                }
                i++;
                continue;
                Block_1:
                num3 = int.MaxValue;
                goto IL_6F;
            }
            if (num == 2147483647)
            {
                return map.Size.x;
            }
            return num;
        }

        private static float DistToBlocker(IntVec3 cell, Map map)
        {
            int num = int.MinValue;
            int num2 = int.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Rot4 rot = new Rot4(i);
                IntVec3 facingCell = rot.FacingCell;
                int num3 = 0;
                int num4;
                for (; ; )
                {
                    IntVec3 c = cell + facingCell * num3;
                    num4 = num3;
                    if (!c.InBounds(map) || !c.Walkable(map))
                    {
                        break;
                    }
                    num3++;
                }
                if (num4 > num)
                {
                    num2 = num;
                    num = num4;
                }
                else if (num4 > num2)
                {
                    num2 = num4;
                }
            }
            return (float)Mathf.Min(num, num2);
        }

        private static bool NoRoofAroundAndWalkable(IntVec3 cell, Map map)
        {
            if (!cell.Walkable(map))
            {
                return false;
            }
            if (cell.Roofed(map))
            {
                return false;
            }
            for (int i = 0; i < 4; i++)
            {
                Rot4 rot = new Rot4(i);
                IntVec3 c = rot.FacingCell + cell;
                if (c.InBounds(map) && c.Roofed(map))
                {
                    return false;
                }
            }
            return true;
        }

        private static float GetMountainousnessScoreAt(IntVec3 cell, Map map)
        {
            float num = 0f;
            int num2 = 0;
            for (int i = 0; i < 700; i += 10)
            {
                IntVec3 c = cell + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    Building edifice = c.GetEdifice(map);
                    if (edifice != null && edifice.def.category == ThingCategory.Building && edifice.def.building.isNaturalRock)
                    {
                        num += 1f;
                    }
                    else if (c.Roofed(map) && c.GetRoof(map).isThickRoof)
                    {
                        num += 0.5f;
                    }
                    num2++;
                }
            }
            return num / (float)num2;
        }

        private static void CalculateTraversalDistancesToUnroofed(Map map)
        {
            RatkinTunnelCellFinder.tempUnroofedRegions.Clear();
            for (int i = 0; i < map.Size.z; i++)
            {
                for (int j = 0; j < map.Size.x; j++)
                {
                    IntVec3 intVec = new IntVec3(j, 0, i);
                    Region region = intVec.GetRegion(map, RegionType.Set_Passable);
                    if (region != null && RatkinTunnelCellFinder.NoRoofAroundAndWalkable(intVec, map))
                    {
                        RatkinTunnelCellFinder.tempUnroofedRegions.Add(region);
                    }
                }
            }
            Dijkstra<Region>.Run(RatkinTunnelCellFinder.tempUnroofedRegions, (Region x) => x.Neighbors, (Region a, Region b) => Mathf.Sqrt((float)a.extentsClose.CenterCell.DistanceToSquared(b.extentsClose.CenterCell)), RatkinTunnelCellFinder.regionsDistanceToUnroofed, null);
            RatkinTunnelCellFinder.tempUnroofedRegions.Clear();
        }

        private static void CalculateClosedAreaSizeGrid(Map map)
        {
            if (RatkinTunnelCellFinder.closedAreaSize == null)
            {
                RatkinTunnelCellFinder.closedAreaSize = new ByteGrid(map);
            }
            else
            {
                RatkinTunnelCellFinder.closedAreaSize.ClearAndResizeTo(map);
            }
            for (int i = 0; i < map.Size.z; i++)
            {
                for (int j = 0; j < map.Size.x; j++)
                {
                    IntVec3 intVec = new IntVec3(j, 0, i);
                    if (RatkinTunnelCellFinder.closedAreaSize[j, i] == 0 && !intVec.Impassable(map))
                    {
                        int area = 0;
                        map.floodFiller.FloodFill(intVec, (IntVec3 c) => !c.Impassable(map), delegate (IntVec3 c)
                        {
                            area++;
                        }, int.MaxValue, false, null);
                        area = Mathf.Min(area, 255);
                        map.floodFiller.FloodFill(intVec, (IntVec3 c) => !c.Impassable(map), delegate (IntVec3 c)
                        {
                            RatkinTunnelCellFinder.closedAreaSize[c] = (byte)area;
                        }, int.MaxValue, false, null);
                    }
                }
            }
        }

        private static void CalculateDistanceToColonyBuildingGrid(Map map)
        {
            if (RatkinTunnelCellFinder.distToColonyBuilding == null)
            {
                RatkinTunnelCellFinder.distToColonyBuilding = new ByteGrid(map);
            }
            else if (!RatkinTunnelCellFinder.distToColonyBuilding.MapSizeMatches(map))
            {
                RatkinTunnelCellFinder.distToColonyBuilding.ClearAndResizeTo(map);
            }
            RatkinTunnelCellFinder.distToColonyBuilding.Clear(byte.MaxValue);
            RatkinTunnelCellFinder.tmpColonyBuildingsLocs.Clear();
            List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                RatkinTunnelCellFinder.tmpColonyBuildingsLocs.Add(allBuildingsColonist[i].Position);
            }
            Dijkstra<IntVec3>.Run(RatkinTunnelCellFinder.tmpColonyBuildingsLocs, (IntVec3 x) => DijkstraUtility.AdjacentCellsNeighborsGetter(x, map), delegate (IntVec3 a, IntVec3 b)
            {
                if (a.x == b.x || a.z == b.z)
                {
                    return 1f;
                }
                return 1.41421354f;
            }, RatkinTunnelCellFinder.tmpDistanceResult, null);
            for (int j = 0; j < RatkinTunnelCellFinder.tmpDistanceResult.Count; j++)
            {
                RatkinTunnelCellFinder.distToColonyBuilding[RatkinTunnelCellFinder.tmpDistanceResult[j].Key] = (byte)Mathf.Min(RatkinTunnelCellFinder.tmpDistanceResult[j].Value, 254.999f);
            }
        }

        private struct LocationCandidate
        {
            public IntVec3 cell;

            public float score;

            public LocationCandidate(IntVec3 cell, float score)
            {
                this.cell = cell;
                this.score = score;
            }
        }
    }
}
