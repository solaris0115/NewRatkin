using RimWorld;
using System.Collections.Generic;
using Verse;

namespace NewRatkin
{
    [StaticConstructorOnStartup]
    public static class RatkinTunnelUtility
    {
        public static List<PawnKindDef> spawnableElitePawnKinds = new List<PawnKindDef>();
        public static List<ThingDef> filthTypes = new List<ThingDef>();

        static RatkinTunnelUtility()
        {
            filthTypes.Clear();
            filthTypes.Add(ThingDefOf.Filth_Dirt);
            filthTypes.Add(ThingDefOf.Filth_Dirt);
            filthTypes.Add(ThingDefOf.Filth_Dirt);
            filthTypes.Add(ThingDefOf.Filth_RubbleRock);
            spawnableElitePawnKinds.Add(RatkinPawnKindDefOf.RatkinEliteSoldier);
            spawnableElitePawnKinds.Add(RatkinPawnKindDefOf.RatkinDemonMan);
        }

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

        public static void Notify_TunnelDespawned(Building_GuerrillaTunnel tunnel, Map map)
        {
            int num = GenRadial.NumCellsInRadius(2f);
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = tunnel.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j].Faction == Faction.OfInsects && !AnyTunnelPreventsClaiming(thingList[j]))
                        {
                            thingList[j].SetFaction(null, null);
                        }
                    }
                }
            }
        }
    }
}
