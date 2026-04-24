using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// RK Equipment Spawn 디버그 도구 설정 UI. 품질 선택, 아이템 체크박스, Spawn 버튼.
    /// </summary>
    public class Dialog_RKSpawnEquipmentConfig : Window_Dev
    {
        private RKSpawnEquipmentConfig config;
        private List<ThingDef> eligibleDefs;
        private Vector2 scrollPosition;
        private float scrollViewHeight;

        private const float SpawnButtonHeight = 36f;
        private const float QualityRowHeight = 28f;
        private const float ItemRowHeight = 24f;
        private const float ContentMargin = 18f;

        public Dialog_RKSpawnEquipmentConfig()
        {
            optionalTitle = "RK Equipment Spawn";
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            config = RKSpawnEquipmentConfig.Load();
            eligibleDefs = BuildEligibleDefList();
        }

        public override Vector2 InitialSize => new Vector2(560f, 640f);

        private static List<ThingDef> BuildEligibleDefList()
        {
            ModContentPack ratkinMod = LoadedModManager.RunningModsListForReading
                .FirstOrDefault(mod => mod.assemblies?.loadedAssemblies?.Contains(typeof(DebugActions).Assembly) == true);

            if (ratkinMod == null)
            {
                return new List<ThingDef>();
            }

            return DefDatabase<ThingDef>.AllDefs
                .Where(def => def.modContentPack == ratkinMod
                    && def.defName.StartsWith("RK_")
                    && (def.IsApparel || def.IsWeapon))
                .OrderBy(def => def.IsApparel ? 0 : 1)
                .ThenBy(def => def.defName)
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            // Spawn 버튼
            Rect spawnRect = new Rect(0f, y, inRect.width - ContentMargin, SpawnButtonHeight);
            if (Widgets.ButtonText(spawnRect, "Spawn"))
            {
                Close(true);
                StartSpawnTool();
            }
            y += SpawnButtonHeight + 8f;

            // 품질 행
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, y, 60f, QualityRowHeight), "품질:");
            float qualityX = 70f;
            int qualityCount = QualityUtility.AllQualityCategories.Count;
            float qualityWidth = qualityCount > 0 ? (inRect.width - qualityX - ContentMargin) / qualityCount : 80f;
            foreach (QualityCategory qc in QualityUtility.AllQualityCategories)
            {
                Rect qcRect = new Rect(qualityX, y, qualityWidth - 4f, QualityRowHeight);
                if (Widgets.RadioButtonLabeled(qcRect, qc.GetLabelShort(), config.quality == qc, false))
                {
                    config.quality = qc;
                    config.Write();
                }
                qualityX += qualityWidth;
            }
            y += QualityRowHeight + 12f;

            // 아이템 목록 헤더
            Widgets.Label(new Rect(0f, y, inRect.width, 22f), "아이템 목록");
            y += 24f;

            // 아이템 체크박스 (스크롤)
            Rect scrollOutRect = new Rect(0f, y, inRect.width, inRect.height - y - ContentMargin);
            Rect scrollViewRect = new Rect(0f, 0f, scrollOutRect.width - 20f, scrollViewHeight);
            Widgets.BeginScrollView(scrollOutRect, ref scrollPosition, scrollViewRect, true);

            float itemY = 0f;
            foreach (ThingDef def in eligibleDefs)
            {
                if (def == null) continue;

                bool enabled = config.enabledDefNames.Contains(def.defName);
                bool wasEnabled = enabled;
                Rect rowRect = new Rect(0f, itemY, scrollViewRect.width, ItemRowHeight);
                Widgets.CheckboxLabeled(rowRect, def.LabelCap, ref enabled, false, null, null, false, true);
                if (enabled != wasEnabled)
                {
                    if (enabled)
                    {
                        if (!config.enabledDefNames.Contains(def.defName))
                            config.enabledDefNames.Add(def.defName);
                    }
                    else
                    {
                        config.enabledDefNames.Remove(def.defName);
                    }
                    config.Write();
                }
                itemY += ItemRowHeight;
            }

            scrollViewHeight = itemY;
            Widgets.EndScrollView();
        }

        private void StartSpawnTool()
        {
            if (Find.CurrentMap == null)
            {
                Messages.Message("맵을 찾을 수 없습니다.", MessageTypeDefOf.RejectInput);
                return;
            }

            DebugTools.curTool = new DebugTool("Spawn RK Equipment...", () =>
            {
                IntVec3 cell = UI.MouseCell();
                DoSpawnAt(cell);
            }, (Action)null);
        }

        private void DoSpawnAt(IntVec3 spawnCenter)
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            int colonistCount = map.mapPawns.FreeColonistsSpawned.Count;
            if (colonistCount == 0)
            {
                Messages.Message("정착지에 식민지 주민이 없습니다.", MessageTypeDefOf.RejectInput);
                return;
            }

            var defsToSpawn = config.enabledDefNames
                .Select(defName => DefDatabase<ThingDef>.GetNamedSilentFail(defName))
                .Where(def => def != null)
                .ToList();

            if (defsToSpawn.Count == 0)
            {
                Messages.Message("선택된 아이템이 없습니다.", MessageTypeDefOf.RejectInput);
                return;
            }

            int totalSpawned = 0;
            int gridX = 0;
            int gridZ = 0;

            foreach (ThingDef def in defsToSpawn)
            {
                for (int i = 0; i < colonistCount; i++)
                {
                    ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
                    Thing thing = ThingMaker.MakeThing(def, stuff);

                    CompQuality compQuality = thing.TryGetComp<CompQuality>();
                    if (compQuality != null)
                    {
                        compQuality.SetQuality(config.quality, new ArtGenerationContext?(ArtGenerationContext.Outsider));
                    }

                    IntVec3 cell = new IntVec3(spawnCenter.x + gridX, 0, spawnCenter.z + gridZ);
                    if (GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near))
                    {
                        totalSpawned++;
                    }
                    else
                    {
                        thing.Destroy();
                    }

                    gridX++;
                    if (gridX > 10)
                    {
                        gridX = 0;
                        gridZ++;
                    }
                }
            }

            Messages.Message($"장비 {totalSpawned}개 생성 (인원 {colonistCount}명 × {defsToSpawn.Count}종, 품질 {config.quality.GetLabel()})", MessageTypeDefOf.TaskCompletion);
        }
    }
}
