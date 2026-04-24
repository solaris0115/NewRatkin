using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using NewRatkin.AlienRaceCompat;
using RimWorld;
using Verse;

namespace NewRatkin
{
    public static class DebugActions
    {
        /// <summary>
        /// 랫킨 모드 출처 ThingDef 중, Apparel 또는 Equipable이며, 판매 불가(sellable 아님) 아이템 목록을 로그에 출력
        /// </summary>
        [DebugAction("Mods", "List Non-Sellable RK Equipment", 
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 1000)]
        private static void ListNonSellableRatkinEquipment()
        {
            ModContentPack ratkinMod = LoadedModManager.RunningModsListForReading
                .FirstOrDefault(mod => mod.assemblies?.loadedAssemblies?.Contains(typeof(DebugActions).Assembly) == true);

            if (ratkinMod == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_RatkinModMissing, "랫킨 모드 ModContentPack을 찾을 수 없습니다.");
                Messages.Message("랫킨 모드 출처를 찾을 수 없음", MessageTypeDefOf.RejectInput);
                return;
            }

            var nonSellable = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.modContentPack == ratkinMod
                    && (def.IsApparel || def.IsWeapon)
                    && !def.tradeability.TraderCanSell())
                .OrderBy(def => def.IsApparel ? 0 : 1)
                .ThenBy(def => def.defName)
                .ToList();

            // Log.Message("=== 랫킨 장비 중 판매 불가(Not Sellable) 목록 ===");
            // Log.Message($"  [출처: {ratkinMod.Name} | Apparel 또는 Equipable(Weapon)]");
            if (nonSellable.Count == 0)
            {
                // Log.Message("(없음)");
            }
            else
            {
                // 장비별 상세 Log.Message 비활성화 (이전: foreach nonSellable …)
            }
            // Log.Message("==========================================");

            Messages.Message($"판매 불가 랫킨 장비 {nonSellable.Count}개 - 로그 확인", MessageTypeDefOf.NeutralEvent);
        }

        [DebugAction("Ratkin", "All Apparel Test", 
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 999)]
        private static void AllApparelTest()
        {
            if (Find.CurrentMap == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoMapAllApparel, "No current map found.");
                return;
            }

            if (DefDatabase<PawnKindDef>.GetNamedSilentFail("RatkinColonist") == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_RatkinColonistMissing, "RatkinColonist PawnKindDef not found.");
                return;
            }

            List<ThingDef> allApparelDefs = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.IsApparel && def.apparel != null)
                .OrderBy(def => def.defName)
                .ToList();

            if (allApparelDefs.Count == 0)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoRKApparel, "No apparel ThingDefs found.");
                return;
            }

            Map map = Find.CurrentMap;
            const int originX = 10;
            const int originZ = 10;
            IntVec3 origin = new IntVec3(originX, 0, originZ);
            if (!origin.InBounds(map))
            {
                Messages.Message($"시작 칸이 맵 밖입니다. ({originX},{originZ}, 맵 {map.Size.x}x{map.Size.z})", MessageTypeDefOf.RejectInput);
                return;
            }

            List<DebugMenuOption> options = new List<DebugMenuOption>();
            Rot4[] dirs = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
            foreach (Rot4 d in dirs)
            {
                Rot4 dirCaptured = d;
                options.Add(new DebugMenuOption(dirCaptured.ToStringHuman(), DebugMenuOptionMode.Action, delegate()
                {
                    AllApparelTestRun(allApparelDefs, dirCaptured, 20f, DevelopmentalStage.Adult, false);
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, "All Apparel Test — 젠 방향"));
        }

        [DebugAction("Ratkin", "All Apparel Test (8yo, child apparel only)",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 998,
            requiresBiotech = true)]
        private static void AllApparelTestEightYearChild()
        {
            if (Find.CurrentMap == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoMapAllApparel, "No current map found.");
                return;
            }

            if (DefDatabase<PawnKindDef>.GetNamedSilentFail("RatkinColonist") == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_RatkinColonistMissing, "RatkinColonist PawnKindDef not found.");
                return;
            }

            List<ThingDef> childApparelDefs = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.IsApparel && def.apparel != null
                    && def.apparel.developmentalStageFilter.Has(DevelopmentalStage.Child))
                .OrderBy(def => def.defName)
                .ToList();

            if (childApparelDefs.Count == 0)
            {
                Messages.Message("developmentalStageFilter에 Child가 포함된 의류 Def가 없습니다.", MessageTypeDefOf.RejectInput);
                return;
            }

            Map map = Find.CurrentMap;
            const int originX = 10;
            const int originZ = 10;
            IntVec3 origin = new IntVec3(originX, 0, originZ);
            if (!origin.InBounds(map))
            {
                Messages.Message($"시작 칸이 맵 밖입니다. ({originX},{originZ}, 맵 {map.Size.x}x{map.Size.z})", MessageTypeDefOf.RejectInput);
                return;
            }

            List<DebugMenuOption> options = new List<DebugMenuOption>();
            Rot4[] dirs = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
            foreach (Rot4 d in dirs)
            {
                Rot4 dirCaptured = d;
                options.Add(new DebugMenuOption(dirCaptured.ToStringHuman(), DebugMenuOptionMode.Action, delegate()
                {
                    AllApparelTestRun(childApparelDefs, dirCaptured, 8f, DevelopmentalStage.Child, true);
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, "All Apparel Test (8세·어린이 장비) — 젠 방향"));
        }

        private static int AllApparelTest_CountSlotsAlongPrimary(IntVec3 origin, Rot4 genDir, Map map)
        {
            IntVec3 step = genDir.FacingCell;
            int count = 0;
            for (IntVec3 c = origin; c.InBounds(map); c += step)
            {
                count++;
            }
            return count;
        }

        private static IntVec3 AllApparelTest_CellForSlot(IntVec3 origin, Rot4 genDir, int rowLength, int slot)
        {
            int row = slot / rowLength;
            int col = slot % rowLength;
            return origin + genDir.FacingCell * col + genDir.RighthandCell * row;
        }

        private static PawnGenerationRequest AllApparelTest_MakeRequest(PawnKindDef ratkinKind, XenotypeDef ratkinXenotype, float fixedAge, DevelopmentalStage developmentalStages)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: ratkinKind,
                faction: Faction.OfPlayer,
                forceGenerateNewPawn: true,
                allowFood: false,
                allowAddictions: false,
                relationWithExtraPawnChanceFactor: 0f,
                fixedBiologicalAge: fixedAge,
                fixedChronologicalAge: fixedAge,
                developmentalStages: developmentalStages
            );
            if (ratkinXenotype != null && ModsConfig.BiotechActive)
            {
                request.ForcedXenotype = ratkinXenotype;
            }

            return request;
        }

        private static void AllApparelTestRun(List<ThingDef> allApparelDefs, Rot4 genDir, float fixedAge, DevelopmentalStage developmentalStages, bool childApparelListOnly)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoMapAllApparel, "No current map found.");
                return;
            }

            PawnKindDef ratkinKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("RatkinColonist");
            if (ratkinKind == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_RatkinColonistMissing, "RatkinColonist PawnKindDef not found.");
                return;
            }

            XenotypeDef ratkinXenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail("RK_XenoType_Ratkin");

            const int originX = 10;
            const int originZ = 10;
            IntVec3 origin = new IntVec3(originX, 0, originZ);
            if (!origin.InBounds(map))
            {
                Messages.Message($"시작 칸이 맵 밖입니다. ({originX},{originZ})", MessageTypeDefOf.RejectInput);
                return;
            }

            int rowLength = AllApparelTest_CountSlotsAlongPrimary(origin, genDir, map);
            if (rowLength <= 0)
            {
                Messages.Message("선택한 방향으로 맵 안에 한 칸도 없습니다.", MessageTypeDefOf.RejectInput);
                return;
            }

            PawnGenerationRequest probeRequest = AllApparelTest_MakeRequest(ratkinKind, ratkinXenotype, fixedAge, developmentalStages);
            Pawn probePawn = PawnGenerator.GeneratePawn(probeRequest);
            List<Pawn> spawnedPawns = new List<Pawn>();
            int slot = 0;
            int excludedNotWearable = 0;
            int excludedNoStandableCell = 0;

            foreach (ThingDef apparelDef in allApparelDefs)
            {
                if (!ApparelUtility.HasPartsToWear(probePawn, apparelDef) || !apparelDef.apparel.PawnCanWear(probePawn, false))
                {
                    excludedNotWearable++;
                    continue;
                }

                IntVec3 spawnPos = AllApparelTest_CellForSlot(origin, genDir, rowLength, slot);
                slot++;
                if (!spawnPos.InBounds(map))
                {
                    Messages.Message($"배치 영역이 맵을 벗어났습니다. ({spawnedPawns.Count}명까지 배치, 방향 {genDir.ToStringHuman()})", MessageTypeDefOf.RejectInput);
                    break;
                }

                if (!spawnPos.Standable(map))
                {
                    IntVec3 near = CellFinder.RandomSpawnCellForPawnNear(spawnPos, map, 4);
                    if (!near.InBounds(map) || !near.Standable(map))
                    {
                        excludedNoStandableCell++;
                        continue;
                    }
                    spawnPos = near;
                }

                PawnGenerationRequest request = AllApparelTest_MakeRequest(ratkinKind, ratkinXenotype, fixedAge, developmentalStages);
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(pawn, spawnPos, map);
                if (pawn.Faction != Faction.OfPlayer)
                {
                    pawn.SetFaction(Faction.OfPlayer);
                }

                if (pawn.apparel != null)
                {
                    List<Apparel> wornApparel = pawn.apparel.WornApparel.ToList();
                    foreach (Apparel app in wornApparel)
                    {
                        pawn.apparel.Remove(app);
                        app.Destroy();
                    }
                }

                Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef, GenStuff.DefaultStuffFor(apparelDef));
                pawn.apparel.Wear(apparel, false);
                spawnedPawns.Add(pawn);
            }

            probePawn.Destroy();

            foreach (Pawn pawn in spawnedPawns)
            {
                if (pawn.drafter != null)
                {
                    pawn.drafter.Drafted = true;
                }
            }

            string modeLabel = childApparelListOnly
                ? $"8세 어린이·어린이 장비 Def만 ({allApparelDefs.Count}종)"
                : $"성인·전체 의류 후보 ({allApparelDefs.Count}종)";
            Messages.Message($"랫킨 의류 착용 테스트: {spawnedPawns.Count}명, {modeLabel}, 젠 {genDir.ToStringHuman()} (착용 불가 {excludedNotWearable}, 배치 실패 {excludedNoStandableCell}). ({originX},{originZ}), 한 줄 {rowLength}칸.", MessageTypeDefOf.TaskCompletion);
        }

        private static void ClearMapExceptTerrain()
        {
            Map map = Find.CurrentMap;
            
            // 1. Remove overhead mountain (thick roof) first - items will fall down
            int removedRoofs = 0;
            foreach (IntVec3 cell in map.AllCells)
            {
                RoofDef roof = map.roofGrid.RoofAt(cell);
                if (roof != null && roof.isThickRoof)
                {
                    map.roofGrid.SetRoof(cell, null);
                    removedRoofs++;
                }
            }
            
            if (removedRoofs > 0)
            {
                // Log.Message($"Removed {removedRoofs} overhead mountain roofs.");
            }

            // 2. Remove all pawns
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned.ToList();
            foreach (Pawn pawn in pawns)
            {
                pawn.Destroy();
            }

            // 3. Remove all things (buildings, items, plants, etc.) - after roofs are removed
            List<Thing> things = map.listerThings.AllThings.ToList();
            foreach (Thing thing in things)
            {
                if (!thing.Destroyed && thing.def.category != ThingCategory.Mote)
                {
                    thing.Destroy();
                }
            }

            // 4. Remove all plants
            var plants = map.listerThings.ThingsInGroup(ThingRequestGroup.Plant).ToList();
            foreach (Thing plant in plants)
            {
                plant.Destroy();
            }

            Messages.Message("Map cleared (terrain preserved).", MessageTypeDefOf.TaskCompletion);
        }

        /// <summary>
        /// DefDatabase의 <c>AlienRace.RaceSettings</c> 인스턴스(들)와 <c>Utilities.UniversalBodyAddons</c> 캐시를 Player.log에 출력합니다.
        /// </summary>
        // allowedGameStates에 Entry를 비트로 넣으면 IsAllowedInCurrentGameState에서
        // 맵 플레이 중 flag가 항상 false가 되어 메뉴에 안 나온다(바닐라 DebugActionAttribute 로직).
        [DebugAction("Ratkin", "Log RaceSettings (HAR) state",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 993)]
        private static void LogRaceSettingsHarState()
        {
            RaceSettingsDebugDumper.DumpToPlayerLog();
            Messages.Message("RaceSettings(HAR) 덤프를 Player.log에 출력했습니다.", MessageTypeDefOf.TaskCompletion);
        }

        [DebugAction("Ratkin", "Fill All Needs", 
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 998)]
        private static void FillAllNeeds()
        {
            if (Find.CurrentMap == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoMapFillNeeds, "No current map found.");
                return;
            }

            // 맵의 모든 플레이어 소속 pawn 가져오기 (식민지 주민, 죄수, 노예 포함)
            List<Pawn> allColonyPawns = new List<Pawn>();
            allColonyPawns.AddRange(Find.CurrentMap.mapPawns.FreeColonistsSpawned);
            allColonyPawns.AddRange(Find.CurrentMap.mapPawns.PrisonersOfColonySpawned);
            allColonyPawns.AddRange(Find.CurrentMap.mapPawns.SlavesOfColonySpawned);

            if (allColonyPawns.Count == 0)
            {
                Messages.Message("No pawns found to fill needs.", MessageTypeDefOf.RejectInput);
                return;
            }

            int filledCount = 0;
            foreach (Pawn pawn in allColonyPawns)
            {
                if (pawn.needs == null)
                    continue;

                // 모든 need를 최대치로 채우기
                foreach (Need need in pawn.needs.AllNeeds)
                {
                    if (need != null)
                    {
                        need.CurLevel = need.MaxLevel;
                    }
                }

                filledCount++;
            }

            Messages.Message($"Filled all needs for {filledCount} pawn(s).", MessageTypeDefOf.TaskCompletion);
        }

        /// <summary>
        /// RK Equipment Spawn 설정 UI 열기. 품질/아이템 선택 후 맵에서 스폰 위치 지정.
        /// </summary>
        [DebugAction("Ratkin", "Spawn RK Equipment Per Colonist",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 997)]
        private static void SpawnRKEquipmentPerColonist()
        {
            Find.WindowStack.Add(new Dialog_RKSpawnEquipmentConfig());
        }

        [DebugAction("Ratkin", "Remove Body Part", 
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMapForPawns,
            displayPriority = 997)]
        private static void RemoveBodyPart(Pawn p)
        {
            if (p == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_RemoveBodyPawnNull, "RemoveBodyPart: Pawn is null.");
                return;
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(Options_RemovePart(p), null));
        }

        private static List<DebugMenuOption> Options_RemovePart(Pawn p)
        {
            if (p == null)
            {
                throw new System.ArgumentNullException("p");
            }

            List<DebugMenuOption> list = new List<DebugMenuOption>();
            
            // 현재 존재하는 body part 목록 가져오기 (이미 제거된 것은 제외)
            foreach (BodyPartRecord localPart2 in p.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null))
            {
                BodyPartRecord localPart = localPart2;
                list.Add(new DebugMenuOption(localPart.LabelCap, DebugMenuOptionMode.Action, delegate()
                {
                    // Hediff_MissingPart 생성 및 추가
                    Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, p, null);
                    hediff_MissingPart.Part = localPart;
                    hediff_MissingPart.IsFresh = false;
                    hediff_MissingPart.lastInjury = HediffDefOf.Cut; // 기본 부상 타입 설정
                    
                    p.health.AddHediff(hediff_MissingPart, localPart, null, null);
                    
                    Messages.Message($"Removed {localPart.LabelCap} from {p.LabelShort}.", MessageTypeDefOf.NeutralEvent);
                }));
            }
            
            return list;
        }

        [DebugAction("Ratkin", "Create Baby From Parents (1000x)", 
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 996,
            requiresBiotech = true)]
        private static void CreateBabyFromParents1000x()
        {
            if (Find.CurrentMap == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoMapBaby, "No current map found.");
                return;
            }

            DebugTool tool = null;
            Pawn parent1 = null;
            tool = new DebugTool("First parent...", delegate()
            {
                parent1 = PawnAt(UI.MouseCell());
                if (parent1 != null)
                {
                    DebugTools.curTool = new DebugTool("Second parent...", delegate()
                    {
                        Pawn parent2 = PawnAt(UI.MouseCell());
                        if (parent2 != null)
                        {
                            // 결과 카운트 딕셔너리
                            Dictionary<int, int> resultCounts = new Dictionary<int, int>();
                            
                            // 1000회 반복 실행
                            for (int cycle = 1; cycle <= 1000; cycle++)
                            {
                                // 생성 전 아이 수 확인
                                int beforeCount = CountChildrenOnMap();
                                
                                // 아이 생성
                                PregnancyUtility.ApplyBirthOutcome(
                                    RitualOutcomeEffectDefOf.ChildBirth.BestOutcome, 
                                    1f, 
                                    null, 
                                    null, 
                                    parent1, 
                                    parent2, 
                                    parent2, 
                                    null, 
                                    null, 
                                    null, 
                                    false);
                                
                                // 생성 후 아이 수 확인
                                int afterCount = CountChildrenOnMap();
                                int createdCount = afterCount - beforeCount;
                                
                                // 결과 카운트
                                if (!resultCounts.ContainsKey(createdCount))
                                {
                                    resultCounts[createdCount] = 0;
                                }
                                resultCounts[createdCount]++;
                            }
                            
                            // 결과 요약 출력 (일반 로그 비활성화)
                            // Log.Message("=== Create Baby Test Results (1000 trials) ===");
                            // foreach (var kvp in resultCounts.OrderBy(x => x.Key))
                            // {
                            //     double percentage = (kvp.Value / 1000.0) * 100.0;
                            //     Log.Message($"{kvp.Key}명: {kvp.Value}회 ({percentage:F2}%)");
                            // }
                            
                            Messages.Message("Created babies 1000 times. Check logs for probability summary.", MessageTypeDefOf.TaskCompletion);
                        }
                        DebugTools.curTool = tool;
                    }, (Action)null);
                }
            }, (Action)null);
            DebugTools.curTool = tool;
        }

        private static Pawn PawnAt(IntVec3 c)
        {
            foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(c))
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }
            return null;
        }

        private static int CountChildrenOnMap()
        {
            int count = 0;
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (pawn.ageTracker != null && 
                    (pawn.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Newborn ||
                     pawn.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Baby ||
                     pawn.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Child))
                {
                    count++;
                }
            }
            return count;
        }

        [DebugAction("Ratkin", "Spawn All PawnKinds Test (25 each)", 
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            displayPriority = 995)]
        private static void SpawnAllPawnKindsTest()
        {
            if (Find.CurrentMap == null)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoMapSpawnKinds, "No current map found.");
                return;
            }

            Map map = Find.CurrentMap;

            // 맵 사이즈 체크
            if (map.Size.x < 225 || map.Size.z < 225)
            {
                Messages.Message($"Map size ({map.Size.x}x{map.Size.z}) is too small. Minimum required: 225x225", MessageTypeDefOf.RejectInput);
                RatkinLimitedLog.Warning(RatkinLogKeys.DebugActions_MapTooSmall, $"Map size ({map.Size.x}x{map.Size.z}) is too small. Minimum required: 225x225");
                return;
            }

            // 1. Clear all things on map except terrain
            ClearMapExceptTerrain();

            // 2. Get all Ratkin PawnKindDefs (exclude abstract ones by checking defName is not null)
            var ratkinPawnKinds = DefDatabase<PawnKindDef>.AllDefs
                .Where(def => def.race != null && def.race.defName == "Ratkin" && def.defName != null)
                .OrderBy(def => def.defName)
                .ToList();

            if (ratkinPawnKinds.Count == 0)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_NoRatkinPawnKind, "No Ratkin PawnKindDef found.");
                return;
            }

            // 3. Get Ratkin XenotypeDef
            XenotypeDef ratkinXenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail("RK_XenoType_Ratkin");

            // 4. 배치 설정
            const int startX = 25;
            const int startZ = 25;
            const int spacing = 25; // 간격 25칸
            const int margin = 5; // 여백 5칸
            const int pawnsPerKind = 25; // 각 pawnkind당 50명

            // 한 줄에 배치 가능한 최대 수 계산 (시작 위치 25, 끝 여백 5)
            int maxPawnsPerRow = (map.Size.x - startX - margin) / spacing;

            if (maxPawnsPerRow <= 0)
            {
                RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_CannotFitPawnsRow, $"Cannot fit any pawns in a row. Map width: {map.Size.x}, Start: {startX}, Margin: {margin}, Spacing: {spacing}");
                return;
            }

            int totalSpawned = 0;
            List<Pawn> spawnedPawns = new List<Pawn>();

            // 전역 위치 변수 - 각 pawnkind가 연속된 위치에 배치되도록 함
            int currentX = startX;
            int currentZ = startZ;

            foreach (PawnKindDef pawnKind in ratkinPawnKinds)
            {
                int spawnedForThisKind = 0;

                // 각 pawnkind당 50명 생성
                for (int i = 0; i < pawnsPerKind; i++)
                {
                    try
                    {
                        // 현재 줄에 더 이상 배치할 수 없으면 다음 줄로
                        if (currentX + spacing > map.Size.x - margin)
                        {
                            currentX = startX;
                            currentZ += spacing;
                        }

                        IntVec3 spawnPos = new IntVec3(currentX, 0, currentZ);

                        // 유효한 스폰 위치 찾기
                        if (!spawnPos.InBounds(map) || !spawnPos.Standable(map))
                        {
                            spawnPos = CellFinder.RandomSpawnCellForPawnNear(spawnPos, map, 10);
                            if (!spawnPos.InBounds(map))
                            {
                                RatkinLimitedLog.Warning(RatkinLogKeys.DebugActions_SpawnPositionInvalid, $"Could not find valid spawn position for {pawnKind.defName} at ({currentX}, {currentZ})");
                                break; // 이 pawnkind는 더 이상 생성 불가
                            }
                        }

                        // Pawn 생성
                        PawnGenerationRequest request = new PawnGenerationRequest(
                            kind: pawnKind,
                            faction: Faction.OfPlayer,
                            forceGenerateNewPawn: true,
                            allowFood: false,
                            allowAddictions: false,
                            relationWithExtraPawnChanceFactor: 0f
                        );

                        // Set Ratkin xenotype if available
                        if (ratkinXenotype != null && ModsConfig.BiotechActive)
                        {
                            request.ForcedXenotype = ratkinXenotype;
                        }

                        Pawn pawn = PawnGenerator.GeneratePawn(request);

                        // Spawn pawn
                        GenSpawn.Spawn(pawn, spawnPos, map);

                        // Make colonist
                        if (pawn.Faction != Faction.OfPlayer)
                        {
                            pawn.SetFaction(Faction.OfPlayer);
                        }

                        spawnedPawns.Add(pawn);
                        spawnedForThisKind++;
                        totalSpawned++;
                    }
                    catch (Exception ex)
                    {
                        RatkinLimitedLog.Error(RatkinLogKeys.DebugActions_SpawnPawnFailed, $"Failed to spawn pawn {i + 1} of {pawnKind.defName}: {ex.Message}");
                        // Continue to next pawn
                    }
                    finally
                    {
                        // 예외 발생 여부와 관계없이 다음 위치로 이동
                        currentX += spacing;
                    }
                }

                // Log.Message($"Spawned {spawnedForThisKind} pawns of {pawnKind.defName}");
            }

            // Draft all spawned pawns
            foreach (Pawn pawn in spawnedPawns)
            {
                if (pawn.drafter != null)
                {
                    pawn.drafter.Drafted = true;
                }
            }

            Messages.Message($"Spawned {totalSpawned} Ratkin pawns ({ratkinPawnKinds.Count} kinds, 50 each).", MessageTypeDefOf.TaskCompletion);
        }
    }
}

