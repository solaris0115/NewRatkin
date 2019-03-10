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

        public float desiredBuilderFraction = 0.5f;

        public List<Blueprint> blueprints = new List<Blueprint>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defendCenter, "defendCenter", default(IntVec3), false);
            Scribe_Values.Look(ref baseRadius, "baseRadius", 16f, false);
            Scribe_Values.Look(ref desiredBuilderFraction, "desiredBuilderFraction", 0.5f, false);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                blueprints.RemoveAll((Blueprint blue) => blue.Destroyed);
            }
            Scribe_Collections.Look(ref blueprints, "blueprints", LookMode.Reference, new object[0]);
        }
    }

    public class LordToil_DefendBomb : LordToil
    {
        public Dictionary<Pawn, DutyDef> rememberedDuties = new Dictionary<Pawn, DutyDef>();
        private static readonly FloatRange BuilderCountFraction = new FloatRange(0.25f, 0.4f);

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

        public LordToil_DefendBomb()
        {

        }
        public LordToil_DefendBomb(IntVec3 defendCenter)
        {
            data = new LordToilData_DefendBomb();
            Data.defendCenter = defendCenter;
        }
        public override IntVec3 FlagLoc
        {
            get
            {
                return Data.defendCenter;
            }
        }
        public override void Init()
        {
            base.Init();
            LordToilData_DefendBomb data = Data;
            data.baseRadius = Mathf.InverseLerp(14f, 25f, lord.ownedPawns.Count / 50f);
            data.baseRadius = Mathf.Clamp(data.baseRadius, 14f, 25f);

            Thing bomb = ThingMaker.MakeThing(RatkinBuildingDefOf.RK_EmpBomb);
            bomb.SetFaction(Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia));
            MinifiedThing minified = bomb.MakeMinified();
            IntVec3 intVec3 = CellFinder.RandomClosewalkCellNear(Data.defendCenter, Map, 1);            
            GenSpawn.Spawn(minified, intVec3, Map);

            GenConstruct.PlaceBlueprintForInstall(minified, CellFinder.RandomClosewalkCellNear(intVec3, Map, 5), Map,Rot4.North, Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia));
            data.desiredBuilderFraction = BuilderCountFraction.RandomInRange;
        }
        public override void UpdateAllDuties()
        {
            LordToilData_DefendBomb data = Data;
            if (lord.ticksInToil < 450)
            {
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    SetAsDefender(lord.ownedPawns[i]);
                }
            }
            else
            {
                int bombPlanerCount = 0;
                rememberedDuties.Clear();
                for (int j = 0; j < lord.ownedPawns.Count; j++)
                {
                    Pawn bombPlaner = lord.ownedPawns[j];
                    if (bombPlaner.mindState.duty.def == DutyDefOf.Build)
                    {
                        rememberedDuties.Add(bombPlaner, DutyDefOf.Build);
                        SetAsBuilder(bombPlaner);
                        bombPlanerCount++;
                        break;
                    }
                }
                Log.Message("bombPlanerCount: " + bombPlanerCount);
                if (bombPlanerCount<1)
                {
                    Log.Message("bombPlanerCount<0 = true");
                    Pawn bombPlaner;
                    if ((from pa in lord.ownedPawns
                            where !rememberedDuties.ContainsKey(pa) && CanBeBuilder(pa)
                            select pa).TryRandomElement(out bombPlaner))
                    {
                        rememberedDuties.Add(bombPlaner, DutyDefOf.Build);
                        SetAsBuilder(bombPlaner);
                        bombPlanerCount++;
                    }
                }
                for (int l = 0; l < lord.ownedPawns.Count; l++)
                {
                    Pawn defender = lord.ownedPawns[l];
                    if (!rememberedDuties.ContainsKey(defender))
                    {
                        SetAsDefender(defender);
                        rememberedDuties.Add(defender, DutyDefOf.Defend);
                    }
                }
                if (bombPlanerCount == 0)
                {
                    lord.ReceiveMemo("NoBuilders");
                    return;
                }
            }
        }
        public override void LordToilTick()
        {
            base.LordToilTick();
            if (lord.ticksInToil == 450)
            {
                lord.CurLordToil.UpdateAllDuties();
            }
            if (lord.ticksInToil > 450 && lord.ticksInToil % 500 == 0)
            {
                UpdateAllDuties();
            }
        }
        public override void Cleanup()
        {
            LordToilData_DefendBomb data = Data;
            data.blueprints.RemoveAll((Blueprint blue) => blue.Destroyed);
            for (int i = 0; i < data.blueprints.Count; i++)
            {
                data.blueprints[i].Destroy(DestroyMode.Cancel);
            }
            foreach (Frame frame in Frames.ToList())
            {
                frame.Destroy(DestroyMode.Cancel);
            }
        }

        private void SetAsDefender(Pawn p)
        {
            LordToilData_DefendBomb data = Data;
            p.mindState.duty = new PawnDuty(DutyDefOf.Defend, data.defendCenter, -1f);
            p.mindState.duty.radius = data.baseRadius;
        }

        private bool CanBeBuilder(Pawn p)
        {
            return !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Construction) && !p.story.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter);
        }

        private void SetAsBuilder(Pawn p)
        {
            LordToilData_DefendBomb data = Data;
            p.mindState.duty = new PawnDuty(DutyDefOf.Build, data.defendCenter, -1f);
            p.mindState.duty.radius = data.baseRadius; 
            p.skills.GetSkill(SkillDefOf.Construction).EnsureMinLevelWithMargin(4);
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
    }
    
}
