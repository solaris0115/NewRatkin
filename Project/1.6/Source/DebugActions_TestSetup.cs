using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using LudeonTK;

namespace NewRatkin
{
    public static class DebugActions_TestSetup
    {
        private static ModContentPack GetRatkinMod()
        {
            return LoadedModManager.RunningModsListForReading
                .FirstOrDefault(m => m.assemblies?.loadedAssemblies?.Contains(typeof(DebugActions).Assembly) == true);
        }

        /// <summary>
        /// [Test1] 모든 랫킨 의복 생성 + 생산 설비 배치 + 레시피 추가 + 무기 생성
        /// </summary>
        [DebugAction("Ratkin", "[Test1] Spawn Apparel + Workbenches + Weapons",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 990)]
        private static void TestButton1_SpawnAll()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            ModContentPack ratkinMod = GetRatkinMod();
            if (ratkinMod == null)
            {
                Messages.Message("랫킨 모드를 찾을 수 없습니다.", MessageTypeDefOf.RejectInput);
                return;
            }

            const int startX = 5;
            const int startZ = 5;
            const int itemSpacing = 2;
            const int buildingRowHeight = 6;
            int maxX = map.Size.x - 5;

            int curX = startX;
            int curZ = startZ;

            // --- 1. 모든 랫킨 의복 아이템 생성 ---
            var allApparel = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.modContentPack == ratkinMod && d.IsApparel)
                .OrderBy(d => d.defName)
                .ToList();

            foreach (ThingDef def in allApparel)
            {
                if (curX + itemSpacing > maxX)
                {
                    curX = startX;
                    curZ += itemSpacing;
                }
                IntVec3 pos = new IntVec3(curX, 0, curZ);
                if (pos.InBounds(map))
                {
                    Thing item = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                    GenSpawn.Spawn(item, pos, map, Rot4.North, WipeMode.Vanish);
                }
                curX += itemSpacing;
            }

            // --- 2. 모든 랫킨 생산 설비 배치 ---
            curX = startX;
            curZ += buildingRowHeight;

            var allWorkbenches = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.modContentPack == ratkinMod
                    && d.thingClass != null
                    && typeof(Building_WorkTable).IsAssignableFrom(d.thingClass))
                .OrderBy(d => d.defName)
                .ToList();

            List<Building_WorkTable> spawnedTables = new List<Building_WorkTable>();
            foreach (ThingDef def in allWorkbenches)
            {
                int footX = def.size.x + 2;
                if (curX + footX > maxX)
                {
                    curX = startX;
                    curZ += buildingRowHeight;
                }
                IntVec3 pos = new IntVec3(curX, 0, curZ);
                if (pos.InBounds(map))
                {
                    Building_WorkTable table = (Building_WorkTable)ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                    GenSpawn.Spawn(table, pos, map, Rot4.South, WipeMode.Vanish);
                    spawnedTables.Add(table);
                }
                curX += footX;
            }

            // --- 3. 생산 설비마다 랫킨 모드 레시피 전부 추가 ---
            foreach (Building_WorkTable table in spawnedTables)
            {
                List<RecipeDef> recipes = table.def.AllRecipes
                    .Where(r => r.modContentPack == ratkinMod)
                    .ToList();
                foreach (RecipeDef recipe in recipes)
                {
                    table.BillStack.AddBill(recipe.MakeNewBill());
                }
                // Log.Message($"[Test1] {table.def.defName}: 레시피 {recipes.Count}개 추가됨");
            }

            // --- 4. 모든 랫킨 무기 아이템 생성 ---
            curX = startX;
            curZ += buildingRowHeight;

            var allWeapons = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.modContentPack == ratkinMod && d.IsWeapon)
                .OrderBy(d => d.defName)
                .ToList();

            foreach (ThingDef def in allWeapons)
            {
                if (curX + itemSpacing > maxX)
                {
                    curX = startX;
                    curZ += itemSpacing;
                }
                IntVec3 pos = new IntVec3(curX, 0, curZ);
                if (pos.InBounds(map))
                {
                    Thing item = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
                    GenSpawn.Spawn(item, pos, map, Rot4.North, WipeMode.Vanish);
                }
                curX += itemSpacing;
            }

            Messages.Message(
                $"[Test1] Apparel {allApparel.Count}개, 생산설비 {allWorkbenches.Count}개 (레시피 추가됨), 무기 {allWeapons.Count}개 생성 완료",
                MessageTypeDefOf.TaskCompletion);
        }

        /// <summary>
        /// [Test2] 랫킨 모드에 추가된 모든 인시던트 발동
        /// </summary>
        [DebugAction("Ratkin", "[Test2] Fire All Ratkin Incidents",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 989)]
        private static void TestButton2_FireAllIncidents()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            ModContentPack ratkinMod = GetRatkinMod();
            if (ratkinMod == null)
            {
                Messages.Message("랫킨 모드를 찾을 수 없습니다.", MessageTypeDefOf.RejectInput);
                return;
            }

            var ratkinIncidents = DefDatabase<IncidentDef>.AllDefs
                .Where(d => d.modContentPack == ratkinMod)
                .OrderBy(d => d.defName)
                .ToList();

            // Log.Message($"[Test2] 발동 시도할 랫킨 인시던트: {ratkinIncidents.Count}개");

            int fired = 0;
            int skipped = 0;

            foreach (IncidentDef incident in ratkinIncidents)
            {
                try
                {
                    IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
                    parms.target = map;
                    if (parms.points < 1000f)
                        parms.points = 1000f;

                    if (incident.Worker.CanFireNow(parms))
                    {
                        incident.Worker.TryExecute(parms);
                        // Log.Message($"[Test2] 발동 성공: {incident.defName}");
                        fired++;
                    }
                    else
                    {
                        RatkinLimitedLog.Warning(RatkinLogKeys.DebugTestSetup_CanFireNowSkipped, $"[Test2] CanFireNow=false, 건너뜀: {incident.defName}");
                        skipped++;
                    }
                }
                catch (Exception e)
                {
                    RatkinLimitedLog.Error(RatkinLogKeys.DebugTestSetup_IncidentException, $"[Test2] 예외 발생 - {incident.defName}: {e.Message}");
                    skipped++;
                }
            }

            Messages.Message(
                $"[Test2] 랫킨 인시던트 {fired}개 발동 / {skipped}개 건너뜀 (로그 확인)",
                MessageTypeDefOf.TaskCompletion);
        }
    }
}
