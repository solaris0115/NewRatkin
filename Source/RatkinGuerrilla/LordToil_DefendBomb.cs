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
    public class LordToilData_DefendBomb : LordToilData
    {
        public IntVec3 defendCenter;

        public float baseRadius = 16f;

        public float blueprintPoints;

        public float desiredBuilderFraction = 0.5f;

        public List<Blueprint> blueprints = new List<Blueprint>();

        public override void ExposeData()
        {
            Scribe_Values.Look<IntVec3>(ref defendCenter, "defendCenter", default(IntVec3), false);
            Scribe_Values.Look<float>(ref baseRadius, "baseRadius", 16f, false);
            Scribe_Values.Look<float>(ref blueprintPoints, "blueprintPoints", 0f, false);
            Scribe_Values.Look<float>(ref desiredBuilderFraction, "desiredBuilderFraction", 0.5f, false);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                blueprints.RemoveAll((Blueprint blue) => blue.Destroyed);
            }
            Scribe_Collections.Look(ref blueprints, "blueprints", LookMode.Reference, new object[0]);
        }
    }

    public class LordToil_Siege : LordToil
    {
        public Dictionary<Pawn, DutyDef> rememberedDuties = new Dictionary<Pawn, DutyDef>();

        private const float BaseRadiusMin = 5f;

        private const float BaseRadiusMax = 10f;

        private static readonly FloatRange MealCountRangePerRaider = new FloatRange(1f, 3f);

        private const int StartBuildingDelay = 450;

        private static readonly FloatRange BuilderCountFraction = new FloatRange(0.25f, 0.4f);

        private const float FractionLossesToAssault = 0.4f;

        private const int InitalShellsPerCannon = 5;

        private const int ReplenishAtShells = 4;

        private const int ShellReplenishCount = 10;

        private const int ReplenishAtMeals = 5;

        private const int MealReplenishCount = 12;




        public LordToil_Siege(IntVec3 siegeCenter, float blueprintPoints)
        {
            data = new LordToilData_DefendBomb();
            Data.defendCenter = siegeCenter;
            Data.blueprintPoints = blueprintPoints;
        }

        public override IntVec3 FlagLoc
        {
            get
            {
                return this.Data.defendCenter;
            }
        }

        private LordToilData_DefendBomb Data
        {
            get
            {
                return (LordToilData_DefendBomb)data;
            }
        }

        private IEnumerable<Frame> Frames
        {
            get
            {
                LordToilData_DefendBomb data = Data;
                float radSquared = (data.baseRadius + 10f) * (data.baseRadius + 10f);
                List<Thing> framesList = Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame);
                if (framesList.Count == 0)
                {
                    yield break;
                }
                for (int i = 0; i < framesList.Count; i++)
                {
                    Frame frame = (Frame)framesList[i];
                    if (frame.Faction == lord.faction && (frame.Position - data.defendCenter).LengthHorizontalSquared < radSquared)
                    {
                        yield return frame;
                    }
                }
                yield break;
            }
        }

        public override bool ForceHighStoryDanger
        {
            get
            {
                return true;
            }
        }

        public override void Init()
        {
            base.Init();
            LordToilData_DefendBomb data = this.Data;
            data.baseRadius = Mathf.InverseLerp(14f, 25f, (float)this.lord.ownedPawns.Count / 50f);
            data.baseRadius = Mathf.Clamp(data.baseRadius, 14f, 25f);
            List<Thing> list = new List<Thing>();
            foreach (Blueprint_Build blueprint_Build in SiegeBlueprintPlacer.PlaceBlueprints(data.defendCenter, base.Map, this.lord.faction, data.blueprintPoints))
            {
                data.blueprints.Add(blueprint_Build);
                using (List<ThingDefCountClass>.Enumerator enumerator2 = blueprint_Build.MaterialsNeeded().GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        ThingDefCountClass cost = enumerator2.Current;
                        Thing thing = list.FirstOrDefault((Thing t) => t.def == cost.thingDef);
                        if (thing != null)
                        {
                            thing.stackCount += cost.count;
                        }
                        else
                        {
                            Thing thing2 = ThingMaker.MakeThing(cost.thingDef, null);
                            thing2.stackCount = cost.count;
                            list.Add(thing2);
                        }
                    }
                }
                ThingDef thingDef = blueprint_Build.def.entityDefToBuild as ThingDef;
                if (thingDef != null)
                {
                    ThingDef turret = thingDef;
                    bool allowEMP = false;
                    TechLevel techLevel = this.lord.faction.def.techLevel;
                    ThingDef thingDef2 = TurretGunUtility.TryFindRandomShellDef(turret, allowEMP, true, techLevel, false, 250f);
                    if (thingDef2 != null)
                    {
                        Thing thing3 = ThingMaker.MakeThing(thingDef2, null);
                        thing3.stackCount = 5;
                        list.Add(thing3);
                    }
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                list[i].stackCount = Mathf.CeilToInt((float)list[i].stackCount * Rand.Range(1f, 1.2f));
            }
            List<List<Thing>> list2 = new List<List<Thing>>();
            for (int j = 0; j < list.Count; j++)
            {
                while (list[j].stackCount > list[j].def.stackLimit)
                {
                    int num = Mathf.CeilToInt((float)list[j].def.stackLimit * Rand.Range(0.9f, 0.999f));
                    Thing thing4 = ThingMaker.MakeThing(list[j].def, null);
                    thing4.stackCount = num;
                    list[j].stackCount -= num;
                    list.Add(thing4);
                }
            }
            List<Thing> list3 = new List<Thing>();
            for (int k = 0; k < list.Count; k++)
            {
                list3.Add(list[k]);
                if (k % 2 == 1 || k == list.Count - 1)
                {
                    list2.Add(list3);
                    list3 = new List<Thing>();
                }
            }
            List<Thing> list4 = new List<Thing>();
            int num2 = Mathf.RoundToInt(LordToil_Siege.MealCountRangePerRaider.RandomInRange * (float)this.lord.ownedPawns.Count);
            for (int l = 0; l < num2; l++)
            {
                Thing item = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack, null);
                list4.Add(item);
            }
            list2.Add(list4);
            DropPodUtility.DropThingGroupsNear(data.defendCenter, base.Map, list2, 110, false, false, true);
            data.desiredBuilderFraction = LordToil_Siege.BuilderCountFraction.RandomInRange;
        }

        public override void UpdateAllDuties()
        {
            LordToilData_DefendBomb data = this.Data;
            if (this.lord.ticksInToil < 450)
            {
                for (int i = 0; i < this.lord.ownedPawns.Count; i++)
                {
                    this.SetAsDefender(this.lord.ownedPawns[i]);
                }
            }
            else
            {
                this.rememberedDuties.Clear();
                int num = Mathf.RoundToInt((float)this.lord.ownedPawns.Count * data.desiredBuilderFraction);
                if (num <= 0)
                {
                    num = 1;
                }
                int num2 = (from b in base.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                            where b.def.hasInteractionCell && b.Faction == this.lord.faction && b.Position.InHorDistOf(this.FlagLoc, data.baseRadius)
                            select b).Count<Thing>();
                if (num < num2)
                {
                    num = num2;
                }
                int num3 = 0;
                for (int j = 0; j < this.lord.ownedPawns.Count; j++)
                {
                    Pawn pawn = this.lord.ownedPawns[j];
                    if (pawn.mindState.duty.def == DutyDefOf.Build)
                    {
                        this.rememberedDuties.Add(pawn, DutyDefOf.Build);
                        this.SetAsBuilder(pawn);
                        num3++;
                    }
                }
                int num4 = num - num3;
                for (int k = 0; k < num4; k++)
                {
                    Pawn pawn2;
                    if ((from pa in this.lord.ownedPawns
                         where !this.rememberedDuties.ContainsKey(pa) && this.CanBeBuilder(pa)
                         select pa).TryRandomElement(out pawn2))
                    {
                        this.rememberedDuties.Add(pawn2, DutyDefOf.Build);
                        this.SetAsBuilder(pawn2);
                        num3++;
                    }
                }
                for (int l = 0; l < this.lord.ownedPawns.Count; l++)
                {
                    Pawn pawn3 = this.lord.ownedPawns[l];
                    if (!this.rememberedDuties.ContainsKey(pawn3))
                    {
                        this.SetAsDefender(pawn3);
                        this.rememberedDuties.Add(pawn3, DutyDefOf.Defend);
                    }
                }
                if (num3 == 0)
                {
                    this.lord.ReceiveMemo("NoBuilders");
                    return;
                }
            }
        }

        public override void Notify_PawnLost(Pawn victim, PawnLostCondition cond)
        {
            this.UpdateAllDuties();
            base.Notify_PawnLost(victim, cond);
        }

        public override void Notify_ConstructionFailed(Pawn pawn, Frame frame, Blueprint_Build newBlueprint)
        {
            base.Notify_ConstructionFailed(pawn, frame, newBlueprint);
            if (frame.Faction == this.lord.faction && newBlueprint != null)
            {
                this.Data.blueprints.Add(newBlueprint);
            }
        }

        private bool CanBeBuilder(Pawn p)
        {
            return !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction) && !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter);
        }

        private void SetAsBuilder(Pawn p)
        {
            LordToilData_DefendBomb data = this.Data;
            p.mindState.duty = new PawnDuty(DutyDefOf.Build, data.defendCenter, -1f);
            p.mindState.duty.radius = data.baseRadius;
            int minLevel = Mathf.Max(ThingDefOf.Sandbags.constructionSkillPrerequisite, ThingDefOf.Turret_Mortar.constructionSkillPrerequisite);
            p.skills.GetSkill(SkillDefOf.Construction).EnsureMinLevelWithMargin(minLevel);
            p.workSettings.EnableAndInitialize();
            List<WorkTypeDef> allDefsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                WorkTypeDef workTypeDef = allDefsListForReading[i];
                if (workTypeDef == WorkTypeDefOf.Construction)
                {
                    p.workSettings.SetPriority(workTypeDef, 1);
                }
                else
                {
                    p.workSettings.Disable(workTypeDef);
                }
            }
        }

        private void SetAsDefender(Pawn p)
        {
            LordToilData_DefendBomb data = this.Data;
            p.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.defendCenter, -1f);
            p.mindState.duty.radius = data.baseRadius;
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            LordToilData_DefendBomb data = this.Data;
            if (this.lord.ticksInToil == 450)
            {
                this.lord.CurLordToil.UpdateAllDuties();
            }
            if (this.lord.ticksInToil > 450 && this.lord.ticksInToil % 500 == 0)
            {
                this.UpdateAllDuties();
            }
            if (Find.TickManager.TicksGame % 500 == 0)
            {
                if (!(from frame in this.Frames
                      where !frame.Destroyed
                      select frame).Any<Frame>())
                {
                    if (!(from blue in data.blueprints
                          where !blue.Destroyed
                          select blue).Any<Blueprint>() && !base.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Any((Thing b) => b.Faction == this.lord.faction && b.def.building.buildingTags.Contains("Artillery")))
                    {
                        this.lord.ReceiveMemo("NoArtillery");
                        return;
                    }
                }
                int num = GenRadial.NumCellsInRadius(20f);
                int num2 = 0;
                int num3 = 0;
                for (int i = 0; i < num; i++)
                {
                    IntVec3 c = data.defendCenter + GenRadial.RadialPattern[i];
                    if (c.InBounds(base.Map))
                    {
                        List<Thing> thingList = c.GetThingList(base.Map);
                        for (int j = 0; j < thingList.Count; j++)
                        {
                            if (thingList[j].def.IsShell)
                            {
                                num2 += thingList[j].stackCount;
                            }
                            if (thingList[j].def == ThingDefOf.MealSurvivalPack)
                            {
                                num3 += thingList[j].stackCount;
                            }
                        }
                    }
                }
                if (num2 < 4)
                {
                    ThingDef turret_Mortar = ThingDefOf.Turret_Mortar;
                    bool allowEMP = false;
                    TechLevel techLevel = this.lord.faction.def.techLevel;
                    ThingDef thingDef = TurretGunUtility.TryFindRandomShellDef(turret_Mortar, allowEMP, true, techLevel, false, 250f);
                    if (thingDef != null)
                    {
                        this.DropSupplies(thingDef, 10);
                    }
                }
                if (num3 < 5)
                {
                    this.DropSupplies(ThingDefOf.MealSurvivalPack, 12);
                }
            }
        }

        private void DropSupplies(ThingDef thingDef, int count)
        {
            List<Thing> list = new List<Thing>();
            Thing thing = ThingMaker.MakeThing(thingDef, null);
            thing.stackCount = count;
            list.Add(thing);
            DropPodUtility.DropThingsNear(this.Data.defendCenter, base.Map, list, 110, false, false, true);
        }

        public override void Cleanup()
        {
            LordToilData_DefendBomb data = this.Data;
            data.blueprints.RemoveAll((Blueprint blue) => blue.Destroyed);
            for (int i = 0; i < data.blueprints.Count; i++)
            {
                data.blueprints[i].Destroy(DestroyMode.Cancel);
            }
            foreach (Frame frame in this.Frames.ToList<Frame>())
            {
                frame.Destroy(DestroyMode.Cancel);
            }
        }
    }
}
