using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;

namespace NewRatkin
{
    public class Building_GuerrillaTunnel : ThingWithComps
    {
        public float eventPoint = 0;
        public bool active = true;

        public int nextPawnSpawnTick = -1;

        private List<Pawn> spawnedPawns = new List<Pawn>();

        public bool caveColony;

        public bool canSpawnPawns = true;

        public const int PawnSpawnRadius = 2;

        public const float MaxSpawnedPawnsPoints = 500f;

        public const float InitialPawnsPoints = 200f;

        private static readonly FloatRange PawnSpawnIntervalDays = new FloatRange(0.85f, 1.15f);

        public static readonly string MemoAttackedByEnemy = "TunnelAttacked";

        public static readonly string MemoDeSpawned = "TunnelDeSpawned";

        public static readonly string MemoBurnedBadly = "TunnelBurnedBadly";

        public static readonly string MemoDestroyedNonRoofCollapse = "TunnelDestroyedNonRoofCollapse";

        private Lord Lord
        {
            get
            {
                Predicate<Pawn> hasDefendTunnelLord = delegate (Pawn x)
                {
                    Lord lord = x.GetLord();
                    return lord != null && lord.LordJob is LordJob_BombPlanting;
                };
                Pawn foundPawn = spawnedPawns.Find(hasDefendTunnelLord);
                if (Spawned)
                {
                    if (foundPawn == null)
                    {
                        RegionTraverser.BreadthFirstTraverse(this.GetRegion(RegionType.Set_Passable), (Region from, Region to) => true, delegate (Region r)
                        {
                            List<Thing> list = r.ListerThings.ThingsOfDef(RatkinBuildingDefOf.RK_GuerrillaTunnel);
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (list[i] != this)
                                {
                                    if (list[i].Faction == Faction)
                                    {
                                        foundPawn = ((Building_GuerrillaTunnel)list[i]).spawnedPawns.Find(hasDefendTunnelLord);
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
                FilterOutUnspawnedPawns();
                float num = 0f;
                for (int i = 0; i < spawnedPawns.Count; i++)
                {
                    num += spawnedPawns[i].kindDef.combatPower;
                }
                return num;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (Faction == null)
            {
                SetFaction(Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia), null);
            }
        }

        public void SpawnInitialPawns()
        {
            SpawnPawnsUntilPoints(eventPoint* 0.24f);
        }

        public void SpawnPawnsUntilPoints(float points)
        {
            int num = 0;
            while (SpawnedPawnsPoints < points)
            {
                num++;
                if (num > 1000)
                {
                    Log.Error("Too many iterations.");
                    break;
                }
                Pawn pawn;
                if (!TrySpawnPawn(out pawn, points))
                {
                    break;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Spawned)
            {
                FilterOutUnspawnedPawns();
                if (!active && !Position.Fogged(Map))
                {
                    Activate();
                }/*
                if (active && Find.TickManager.TicksGame >= nextPawnSpawnTick)
                {
                    if (SpawnedPawnsPoints < 500f)
                    {
                        Pawn pawn;
                        bool flag = TrySpawnPawn(out pawn);
                        if (flag && pawn.caller != null)
                        {
                            pawn.caller.DoCall();
                        }
                    }
                }*/
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.DeSpawn(mode);
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                lords[i].ReceiveMemo(MemoDeSpawned);
            }
            RatkinTunnelUtility.Notify_TunnelDespawned(this, map);
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (dinfo.Def.ExternalViolenceFor(this) && dinfo.Instigator != null && dinfo.Instigator.Faction != null)
            {
                Lord lord = Lord;
                if (lord != null)
                {
                    lord.ReceiveMemo(MemoAttackedByEnemy);
                }
            }
            base.PostApplyDamage(dinfo, totalDamageDealt);
        }

        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            if (Spawned && (dinfo == null || dinfo.Value.Category != DamageInfo.SourceCategory.Collapse))
            {
                List<Lord> lords = Map.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    lords[i].ReceiveMemo(MemoDestroyedNonRoofCollapse);
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
            Scribe_Values.Look(ref eventPoint, "eventPoint");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                spawnedPawns.RemoveAll((Pawn x) => x == null);
            }
        }

        private void Activate()
        {
            active = true;
            SpawnInitialPawns();
            //CalculateNextPawnSpawnTick();
        }

        /*private void CalculateNextPawnSpawnTick()
        {
            float num = GenMath.LerpDouble(0f, 5f, 1f, 0.5f, spawnedPawns.Count);
            nextPawnSpawnTick = Find.TickManager.TicksGame + (int)(PawnSpawnIntervalDays.RandomInRange * 60000f / (num * Find.Storyteller.difficulty.enemyReproductionRateFactor));
        }*/

        private void FilterOutUnspawnedPawns()
        {
            for (int i = spawnedPawns.Count - 1; i >= 0; i--)
            {
                if (!spawnedPawns[i].Spawned)
                {
                    spawnedPawns.RemoveAt(i);
                }
            }
        }

        private bool TrySpawnPawn(out Pawn pawn,float limitPoint)
        {
            if (!canSpawnPawns)
            {
                pawn = null;
                return false;
            }
            float curPoints = SpawnedPawnsPoints;
            IEnumerable<PawnKindDef> source = from x in RatkinTunnelUtility.spawnableElitePawnKinds
                                              where curPoints + x.combatPower <= limitPoint
                                              select x;
            PawnKindDef kindDef;
            if (!source.TryRandomElement(out kindDef))
            {
                pawn = null;
                return false;
            }
            pawn = PawnGenerator.GeneratePawn(kindDef, Faction);
            spawnedPawns.Add(pawn);
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(Position, Map, 2, null), Map, WipeMode.Vanish);
            Lord lord = Lord;
            if (lord == null)
            {
                lord = CreateNewLord();
            }
            lord.AddPawn(pawn);
            SoundDefOf.CryptosleepCasket_Eject.PlayOneShot(this);
            Need rest = pawn.needs.TryGetNeed(NeedDefOf.Rest);
            rest.CurLevel = rest.MaxLevel;
            Need food = pawn.needs.TryGetNeed(NeedDefOf.Food);
            food.CurLevel = food.MaxLevel;
            return true;
        }

        public override bool PreventPlayerSellingThingsNearby(out string reason)
        {
            if (spawnedPawns.Count > 0)
            {
                if (spawnedPawns.Any((Pawn p) => !p.Downed))
                {
                    reason = def.label;
                    return true;
                }
            }
            reason = null;
            return false;
        }

        private Lord CreateNewLord()
        {
            return LordMaker.MakeNewLord(Faction, new LordJob_BombPlanting(Faction,Position, eventPoint), Map, null);
        }
    }
    public class Building_ThiefTunnel : ThingWithComps
    {
        public static List<PawnKindDef> spawnablePawnKinds = new List<PawnKindDef>();

        public override void Tick()
        {
            if (Spawned && !Destroyed)
            {
                if (!GenGrid.Impassable(Position, Map))
                {
                    WallDestroyed();
                }
            }
        }
        private void WallDestroyed()
        {
            Messages.Message(Translator.Translate("RatHoleGone"), MessageTypeDefOf.NegativeEvent, true);
            Destroy(DestroyMode.Deconstruct);
        }

    }
}
