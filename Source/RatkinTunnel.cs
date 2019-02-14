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
    public class Hive : ThingWithComps
    {
        public bool active = true;

        public int nextPawnSpawnTick = -1;

        private List<Pawn> spawnedPawns = new List<Pawn>();

        public bool caveColony;

        public bool canSpawnPawns = true;

        public const int PawnSpawnRadius = 2;

        public const float MaxSpawnedPawnsPoints = 500f;

        public const float InitialPawnsPoints = 200f;

        private static readonly FloatRange PawnSpawnIntervalDays = new FloatRange(0.85f, 1.15f);

        public static List<PawnKindDef> spawnablePawnKinds = new List<PawnKindDef>();

        public static readonly string MemoAttackedByEnemy = "HiveAttacked";

        public static readonly string MemoDeSpawned = "HiveDeSpawned";

        public static readonly string MemoBurnedBadly = "HiveBurnedBadly";

        public static readonly string MemoDestroyedNonRoofCollapse = "HiveDestroyedNonRoofCollapse";

        private Lord Lord
        {
            get
            {
                Predicate<Pawn> hasDefendHiveLord = delegate (Pawn x)
                {
                    Lord lord = x.GetLord();
                    return lord != null && lord.LordJob is LordJob_DefendAndExpandHive;
                };
                Pawn foundPawn = this.spawnedPawns.Find(hasDefendHiveLord);
                if (base.Spawned)
                {
                    if (foundPawn == null)
                    {
                        RegionTraverser.BreadthFirstTraverse(this.GetRegion(RegionType.Set_Passable), (Region from, Region to) => true, delegate (Region r)
                        {
                            List<Thing> list = r.ListerThings.ThingsOfDef(ThingDefOf.Hive);
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (list[i] != this)
                                {
                                    if (list[i].Faction == this.Faction)
                                    {
                                        foundPawn = ((Hive)list[i]).spawnedPawns.Find(hasDefendHiveLord);
                                        if (foundPawn != null)
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            return false;
                        }, 20, RegionType.Set_Passable);
                    }
                    if (foundPawn != null)
                    {
                        return foundPawn.GetLord();
                    }
                }
                return null;
            }
        }

        private float SpawnedPawnsPoints
        {
            get
            {
                this.FilterOutUnspawnedPawns();
                float num = 0f;
                for (int i = 0; i < this.spawnedPawns.Count; i++)
                {
                    num += this.spawnedPawns[i].kindDef.combatPower;
                }
                return num;
            }
        }

        public static void ResetStaticData()
        {
            Hive.spawnablePawnKinds.Clear();
            Hive.spawnablePawnKinds.Add(PawnKindDefOf.Megascarab);
            Hive.spawnablePawnKinds.Add(PawnKindDefOf.Spelopede);
            Hive.spawnablePawnKinds.Add(PawnKindDefOf.Megaspider);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (base.Faction == null)
            {
                this.SetFaction(Faction.OfInsects, null);
            }
            if (!respawningAfterLoad && this.active)
            {
                this.SpawnInitialPawns();
            }
        }

        private void SpawnInitialPawns()
        {
            this.SpawnPawnsUntilPoints(200f);
            this.CalculateNextPawnSpawnTick();
        }

        public void SpawnPawnsUntilPoints(float points)
        {
            int num = 0;
            while (this.SpawnedPawnsPoints < points)
            {
                num++;
                if (num > 1000)
                {
                    Log.Error("Too many iterations.", false);
                    break;
                }
                Pawn pawn;
                if (!this.TrySpawnPawn(out pawn))
                {
                    break;
                }
            }
            this.CalculateNextPawnSpawnTick();
        }

        public override void Tick()
        {
            base.Tick();
            if (base.Spawned)
            {
                this.FilterOutUnspawnedPawns();
                if (!this.active && !base.Position.Fogged(base.Map))
                {
                    this.Activate();
                }
                if (this.active && Find.TickManager.TicksGame >= this.nextPawnSpawnTick)
                {
                    if (this.SpawnedPawnsPoints < 500f)
                    {
                        Pawn pawn;
                        bool flag = this.TrySpawnPawn(out pawn);
                        if (flag && pawn.caller != null)
                        {
                            pawn.caller.DoCall();
                        }
                    }
                    this.CalculateNextPawnSpawnTick();
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = base.Map;
            base.DeSpawn(mode);
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                lords[i].ReceiveMemo(Hive.MemoDeSpawned);
            }
            HiveUtility.Notify_HiveDespawned(this, map);
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (dinfo.Def.ExternalViolenceFor(this) && dinfo.Instigator != null && dinfo.Instigator.Faction != null)
            {
                Lord lord = this.Lord;
                if (lord != null)
                {
                    lord.ReceiveMemo(Hive.MemoAttackedByEnemy);
                }
            }
            if (dinfo.Def == DamageDefOf.Flame && (float)this.HitPoints < (float)base.MaxHitPoints * 0.3f)
            {
                Lord lord2 = this.Lord;
                if (lord2 != null)
                {
                    lord2.ReceiveMemo(Hive.MemoBurnedBadly);
                }
            }
            base.PostApplyDamage(dinfo, totalDamageDealt);
        }

        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            if (base.Spawned && (dinfo == null || dinfo.Value.Category != DamageInfo.SourceCategory.Collapse))
            {
                List<Lord> lords = base.Map.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    lords[i].ReceiveMemo(Hive.MemoDestroyedNonRoofCollapse);
                }
            }
            base.Kill(dinfo, exactCulprit);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.active, "active", false, false);
            Scribe_Values.Look<int>(ref this.nextPawnSpawnTick, "nextPawnSpawnTick", 0, false);
            Scribe_Collections.Look<Pawn>(ref this.spawnedPawns, "spawnedPawns", LookMode.Reference, new object[0]);
            Scribe_Values.Look<bool>(ref this.caveColony, "caveColony", false, false);
            Scribe_Values.Look<bool>(ref this.canSpawnPawns, "canSpawnPawns", true, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.spawnedPawns.RemoveAll((Pawn x) => x == null);
            }
        }

        private void Activate()
        {
            this.active = true;
            this.SpawnInitialPawns();
            this.CalculateNextPawnSpawnTick();
            CompSpawnerHives comp = base.GetComp<CompSpawnerHives>();
            if (comp != null)
            {
                comp.CalculateNextHiveSpawnTick();
            }
        }

        private void CalculateNextPawnSpawnTick()
        {
            float num = GenMath.LerpDouble(0f, 5f, 1f, 0.5f, (float)this.spawnedPawns.Count);
            this.nextPawnSpawnTick = Find.TickManager.TicksGame + (int)(Hive.PawnSpawnIntervalDays.RandomInRange * 60000f / (num * Find.Storyteller.difficulty.enemyReproductionRateFactor));
        }

        private void FilterOutUnspawnedPawns()
        {
            for (int i = this.spawnedPawns.Count - 1; i >= 0; i--)
            {
                if (!this.spawnedPawns[i].Spawned)
                {
                    this.spawnedPawns.RemoveAt(i);
                }
            }
        }

        private bool TrySpawnPawn(out Pawn pawn)
        {
            if (!this.canSpawnPawns)
            {
                pawn = null;
                return false;
            }
            float curPoints = this.SpawnedPawnsPoints;
            IEnumerable<PawnKindDef> source = from x in Hive.spawnablePawnKinds
                                              where curPoints + x.combatPower <= 500f
                                              select x;
            PawnKindDef kindDef;
            if (!source.TryRandomElement(out kindDef))
            {
                pawn = null;
                return false;
            }
            pawn = PawnGenerator.GeneratePawn(kindDef, base.Faction);
            this.spawnedPawns.Add(pawn);
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(base.Position, base.Map, 2, null), base.Map, WipeMode.Vanish);
            Lord lord = this.Lord;
            if (lord == null)
            {
                lord = this.CreateNewLord();
            }
            lord.AddPawn(pawn);
            SoundDefOf.Hive_Spawn.PlayOneShot(this);
            return true;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Spawn pawn",
                    icon = TexCommand.ReleaseAnimals,
                    action = delegate ()
                    {
                        Pawn pawn;
                        this.TrySpawnPawn(out pawn);
                    }
                };
            }
            yield break;
        }

        public override bool PreventPlayerSellingThingsNearby(out string reason)
        {
            if (this.spawnedPawns.Count > 0)
            {
                if (this.spawnedPawns.Any((Pawn p) => !p.Downed))
                {
                    reason = this.def.label;
                    return true;
                }
            }
            reason = null;
            return false;
        }

        private Lord CreateNewLord()
        {
            return LordMaker.MakeNewLord(base.Faction, new LordJob_DefendAndExpandHive(!this.caveColony), base.Map, null);
        }
    }

    public class LordJob_DefendAndExpandHive : LordJob
    {
        private bool aggressive;

        public LordJob_DefendAndExpandHive()
        {
        }

        public LordJob_DefendAndExpandHive(bool aggressive)
        {
            this.aggressive = aggressive;
        }

        public override bool CanBlockHostileVisitors
        {
            get
            {
                return false;
            }
        }

        public override bool AddFleeToil
        {
            get
            {
                return false;
            }
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_DefendAndExpandHive lordToil_DefendAndExpandHive = new LordToil_DefendAndExpandHive();
            lordToil_DefendAndExpandHive.distToHiveToAttack = 10f;
            stateGraph.StartingToil = lordToil_DefendAndExpandHive;
            LordToil_DefendHiveAggressively lordToil_DefendHiveAggressively = new LordToil_DefendHiveAggressively();
            lordToil_DefendHiveAggressively.distToHiveToAttack = 40f;
            stateGraph.AddToil(lordToil_DefendHiveAggressively);
            LordToil_AssaultColony lordToil_AssaultColony = new LordToil_AssaultColony(false);
            stateGraph.AddToil(lordToil_AssaultColony);
            Transition transition = new Transition(lordToil_DefendAndExpandHive, (!this.aggressive) ? lordToil_DefendHiveAggressively : lordToil_AssaultColony, false, true);
            transition.AddTrigger(new Trigger_PawnHarmed(0.5f, true, null));
            transition.AddTrigger(new Trigger_PawnLostViolently(false));
            transition.AddTrigger(new Trigger_Memo(Hive.MemoAttackedByEnemy));
            transition.AddTrigger(new Trigger_Memo(Hive.MemoBurnedBadly));
            transition.AddTrigger(new Trigger_Memo(Hive.MemoDestroyedNonRoofCollapse));
            transition.AddTrigger(new Trigger_Memo(HediffGiver_Heat.MemoPawnBurnedByAir));
            transition.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(transition, false);
            Transition transition2 = new Transition(lordToil_DefendAndExpandHive, lordToil_AssaultColony, false, true);
            Transition transition3 = transition2;
            float chance = 0.5f;
            Faction parentFaction = base.Map.ParentFaction;
            transition3.AddTrigger(new Trigger_PawnHarmed(chance, false, parentFaction));
            transition2.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(transition2, false);
            Transition transition4 = new Transition(lordToil_DefendHiveAggressively, lordToil_AssaultColony, false, true);
            Transition transition5 = transition4;
            chance = 0.5f;
            parentFaction = base.Map.ParentFaction;
            transition5.AddTrigger(new Trigger_PawnHarmed(chance, false, parentFaction));
            transition4.AddPostAction(new TransitionAction_EndAllJobs());
            stateGraph.AddTransition(transition4, false);
            Transition transition6 = new Transition(lordToil_DefendAndExpandHive, lordToil_DefendAndExpandHive, true, true);
            transition6.AddTrigger(new Trigger_Memo(Hive.MemoDeSpawned));
            stateGraph.AddTransition(transition6, false);
            Transition transition7 = new Transition(lordToil_DefendHiveAggressively, lordToil_DefendHiveAggressively, true, true);
            transition7.AddTrigger(new Trigger_Memo(Hive.MemoDeSpawned));
            stateGraph.AddTransition(transition7, false);
            Transition transition8 = new Transition(lordToil_AssaultColony, lordToil_DefendAndExpandHive, false, true);
            transition8.AddSource(lordToil_DefendHiveAggressively);
            transition8.AddTrigger(new Trigger_TicksPassedWithoutHarmOrMemos(1200, new string[]
            {
                Hive.MemoAttackedByEnemy,
                Hive.MemoBurnedBadly,
                Hive.MemoDestroyedNonRoofCollapse,
                Hive.MemoDeSpawned,
                HediffGiver_Heat.MemoPawnBurnedByAir
            }));
            transition8.AddPostAction(new TransitionAction_EndAttackBuildingJobs());
            stateGraph.AddTransition(transition8, false);
            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.aggressive, "aggressive", false, false);
        }
    }
}
