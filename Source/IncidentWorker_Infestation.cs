using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Harmony;
using UnityEngine;
using Verse.AI;
using Verse;
using Verse.Sound;


namespace NewRatkin
{
    /*
     * 랫킨굴 이벤트
     * 종류
     * 1. 물건을 훔쳐가는 랫킨굴 (우호 이벤트)(고유 히든 세력)
     * 2. 전투조 침입 (적대 이벤트)
     * 
     * 
     * 
     * 생성 위치
     * 1. 플레이어의 집안 [공통]
     * 2. 창고 아니면 음식창고에서 젠되도록 [도난이벤트] ! 눈치 못채도록 지연성 처리하도록
     * 3. 
     */
     /*
    [StaticConstructorOnStartup]
    public static class CustomRaidPatch
    {
        private static readonly Type patchType = typeof(CustomRaidPatch);
        static CustomRaidPatch()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("com.NewRatkin.rimworld.mod");
            harmonyInstance.Patch(AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker"),  new HarmonyMethod(patchType, "TryExecuteWorkerPrefix", null));
        }
        public static void TryExecuteWorkerPrefix()
        {
            Log.Message("123");
        }
    }


    public class IncidentWorker_RatkinHive : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 intVec;
            return base.CanFireNowSub(parms) && HiveUtility.TotalSpawnedHivesCount(map) < 30 && InfestationCellFinder.TryFindCell(out intVec, map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int hiveCount = Mathf.Max(GenMath.RoundRandom(parms.points / 220f), 1);
            Thing t = SpawnTunnels(hiveCount, map);
            base.SendStandardLetter(t, null, new string[0]);
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            return true;
        }

        private Thing SpawnTunnels(int hiveCount, Map map)
        {
            IntVec3 loc;
            if (!InfestationCellFinder.TryFindCell(out loc, map))
            {
                return null;
            }
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.TunnelHiveSpawner, null), loc, map, WipeMode.FullRefund);
            for (int i = 0; i < hiveCount - 1; i++)
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

    public class IncidentWorker_RaidRatkin : IncidentWorker_Raid
    {
        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return base.FactionCanBeGroupSource(f, map, desperate) && f.HostileTo(Faction.OfPlayer) && (desperate || (float)GenDate.DaysPassed >= f.def.earliestRaidDays);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!base.TryExecuteWorker(parms))
            {
                return false;
            }
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            Find.StoryWatcher.statsRecord.numRaidsEnemy++;
            return true;
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (parms.faction != null)
            {
                return true;
            }
            float num = parms.points;
            if (num <= 0f)
            {
                num = 999999f;
            }
            return PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, false), true, true, true, true) || PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, true), true, true, true, true);
        }

        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            if (parms.points <= 0f)
            {
                Log.Error("RaidEnemy is resolving raid points. They should always be set before initiating the incident.", false);
                parms.points = StorytellerUtility.DefaultThreatPointsNow(parms.target);
            }
        }

        protected override void ResolveRaidStrategy(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            if (parms.raidStrategy != null)
            {
                return;
            }
            Map map = (Map)parms.target;
            if (!(from d in DefDatabase<RaidStrategyDef>.AllDefs
                  where d.Worker.CanUseWith(parms, groupKind) && (parms.raidArrivalMode != null || (d.arriveModes != null && d.arriveModes.Any((PawnsArrivalModeDef x) => x.Worker.CanUseWith(parms))))
                  select d).TryRandomElementByWeight((RaidStrategyDef d) => d.Worker.SelectionWeight(map, parms.points), out parms.raidStrategy))
            {
                Log.Error(string.Concat(new object[]
                {
                    "No raid stategy for ",
                    parms.faction,
                    " with points ",
                    parms.points,
                    ", groupKind=",
                    groupKind,
                    "\nparms=",
                    parms
                }), false);
                if (!Prefs.DevMode)
                {
                    parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                }
            }
        }

        protected override string GetLetterLabel(IncidentParms parms)
        {
            return parms.raidStrategy.letterLabelEnemy;
        }

        protected override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
        {
            string text = string.Format(parms.raidArrivalMode.textEnemy, parms.faction.def.pawnsPlural, parms.faction.Name);
            text += "\n\n";
            text += parms.raidStrategy.arrivalTextEnemy;
            Pawn pawn = pawns.Find((Pawn x) => x.Faction.leader == x);
            if (pawn != null)
            {
                text += "\n\n";
                text += "EnemyRaidLeaderPresent".Translate(pawn.Faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER"));
            }
            return text;
        }

        protected override LetterDef GetLetterDef()
        {
            return LetterDefOf.ThreatBig;
        }

        protected override string GetRelatedPawnsInfoLetterText(IncidentParms parms)
        {
            return "LetterRelatedPawnsRaidEnemy".Translate(Faction.OfPlayer.def.pawnsPlural, parms.faction.def.pawnsPlural);
        }
    }*/
}
