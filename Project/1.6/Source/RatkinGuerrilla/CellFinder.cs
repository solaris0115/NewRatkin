using System.Collections.Generic;
using Verse;
using RimWorld;

namespace NewRatkin
{
    public static class RatkinTunnelCellFinder
    {
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
