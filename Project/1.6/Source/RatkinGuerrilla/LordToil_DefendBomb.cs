using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using UnityEngine;
using Verse.AI;
namespace NewRatkin
{
    public class LordToilData_DefendBomb : LordToilData
    {
        public IntVec3 defendCenter;
        public float eventPoint = 0;

        public float baseRadius = 16f;

        public float desiredBuilderFraction = 0.5f;
        
        public Thing minifiedEmpBomb;
        public Thing EmpBomb;
        public bool empIsSpawned = true;
        public bool minifiedEmpBombDestoryed = false;
        public Blueprint blueprint;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defendCenter, "defendCenter", default(IntVec3), false);
            Scribe_Values.Look(ref baseRadius, "baseRadius", 16f, false);
            Scribe_Values.Look(ref desiredBuilderFraction, "desiredBuilderFraction", 0.5f, false);
            Scribe_References.Look(ref blueprint, "blueprint");
            Scribe_Values.Look(ref eventPoint, "eventPoint");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (minifiedEmpBomb != null && minifiedEmpBomb.Destroyed)
                {
                    minifiedEmpBombDestoryed = true;
                    if (EmpBomb != null && EmpBomb.Spawned)
                    {
                        Scribe_References.Look(ref EmpBomb, "EmpBomb");
                    }
                    else
                    {
                        empIsSpawned = false;
                    }
                }
                else
                {
                    Scribe_References.Look(ref minifiedEmpBomb, "minifiedEmpBomb");
                    Scribe_References.Look(ref EmpBomb, "EmpBomb");
                }
            }
            Scribe_Values.Look(ref empIsSpawned, "empDestroyed");
            Scribe_Values.Look(ref minifiedEmpBombDestoryed, "minifiedEmpBombDestoryed");
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                if (!minifiedEmpBombDestoryed)
                {
                    Scribe_References.Look(ref minifiedEmpBomb, "minifiedEmpBomb");
                    Scribe_References.Look(ref EmpBomb, "EmpBomb");
                }
                if (!empIsSpawned)
                {
                    Scribe_References.Look(ref EmpBomb, "EmpBomb");
                }
            }
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
        public LordToil_DefendBomb(IntVec3 defendCenter,float eventPoint)
        {
            data = new LordToilData_DefendBomb();
            Data.defendCenter = defendCenter;
            Data.eventPoint = eventPoint;
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
            Current.Game.GetComponent<GameComponent_EMPCheck>().lord = lord;
            LordToilData_DefendBomb data = Data;
            data.baseRadius = Mathf.InverseLerp(14f, 25f, lord.ownedPawns.Count / 50f);
            data.baseRadius = Mathf.Clamp(data.baseRadius, 14f, 25f);

            Thing bomb = ThingMaker.MakeThing(RatkinBuildingDefOf.RK_EmpBomb);           
            Faction ratkiniaFaction = Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia);
            data.EmpBomb = bomb;
            bomb.SetFaction(ratkiniaFaction);
            if (bomb.TryGetComp<Comp_Emp>()!=null)
            {
                bomb.TryGetComp<Comp_Emp>().eventPoint = Data.eventPoint;
            }
            MinifiedThing minified = bomb.MakeMinified();
            data.minifiedEmpBomb = minified;
            IntVec3 intVec3 = CellFinder.RandomClosewalkCellNear(Data.defendCenter, Map, 1);
            GenSpawn.Spawn(minified, intVec3, Map);
            IntVec3 bluePrintPosition = CellFinder.RandomClosewalkCellNear(intVec3, Map, 5);
            Blueprint b =  GenConstruct.PlaceBlueprintForInstall(minified, bluePrintPosition, Map,Rot4.North, Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia));
            data.blueprint= b;
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
                if (bombPlanerCount<1)
                {
                    Pawn bombPlaner;
                    if ((from pawnB in lord.ownedPawns
                            where !rememberedDuties.ContainsKey(pawnB) && CanBeBuilder(pawnB)
                            select pawnB).TryRandomElement(out bombPlaner))
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
            if (Find.TickManager.TicksGame % 500 == 0)
            {
                LordToilData_DefendBomb data = Data;
                if(data.EmpBomb != null && data.EmpBomb.Destroyed ||(data.minifiedEmpBomb!=null && data.minifiedEmpBomb.Destroyed && !data.EmpBomb.Spawned) )
                {
                    lord.ReceiveMemo("NoBomb");
                    return;
                }
                if(!data.empIsSpawned && data.minifiedEmpBombDestoryed)
                {
                    lord.ReceiveMemo("NoBomb");
                    return;
                }
            }
        }
        public override void Cleanup()
        {
            LordToilData_DefendBomb data = Data;
            if(data.blueprint!=null && !data.blueprint.Destroyed)
            {
                data.blueprint.Destroy(DestroyMode.Cancel);
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
            return !p.WorkTypeIsDisabled(WorkTypeDefOf.Construction) && !p.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter);
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
