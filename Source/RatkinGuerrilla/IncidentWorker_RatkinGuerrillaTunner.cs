﻿using System;
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
            IntVec3 intVec;
            //Log.Message("totalCount:" + (base.CanFireNowSub(parms) && (RatkinTunnelUtility.TotalSpawnedTunnelCount(map) < 2) && RatkinTunnelCellFinder.TryFindCell(out intVec, map)));
            return base.CanFireNowSub(parms) && (RatkinTunnelUtility.TotalSpawnedTunnelCount(map) < 2) && RatkinTunnelCellFinder.TryFindCell(out intVec,map);
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Thing t = SpawnTunnels(map);
            SendStandardLetter(t, null, new string[0]);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }
        private Thing SpawnTunnels(Map map)
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
