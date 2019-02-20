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
            /*
             * 5개 미만, 적절한 공간이 있으면 격발
             */
            Map map = (Map)parms.target;
            IntVec3 intVec;
            return base.CanFireNowSub(parms) && RatkinTunnelUtility.TotalSpawnedTunnelCount(map) < 2 && RatkinTunnelCellFinder.TryFindCell(out intVec, map);
        }
        /// <summary>
        /// 지금 이 이벤트 실행을 시도해봄.
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int tunnelCount = 0;//Mathf.Max(GenMath.RoundRandom(parms.points / tunnelPoints), 1);
            Thing t = SpawnTunnels(tunnelCount, map);
            SendStandardLetter(t, null, new string[0]);
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
            if (!RatkinTunnelCellFinder.TryFindCell(out loc, map))
            {
                return null;
            }
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(RatkinBuildingDefOf.RK_GuerrillaTunnel, null), loc, map, WipeMode.FullRefund);
            return thing;
        }
    }
}
