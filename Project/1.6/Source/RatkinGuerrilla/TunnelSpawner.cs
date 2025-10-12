using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using LudeonTK;

namespace NewRatkin
{ 
    [StaticConstructorOnStartup]
    public class ThiefTunnelSpawner : ThingWithComps
    {
        private int secondarySpawnTick;

        public bool spawnTunnel = true;
        
        private Sustainer sustainer;

        private static MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

        private readonly FloatRange ResultSpawnDelay = new FloatRange(5f, 8f);

        [TweakValue("Gameplay", 0f, 1f)]
        private static float DustMoteSpawnMTB = 0.2f;

        [TweakValue("Gameplay", 0f, 1f)]
        private static float FilthSpawnMTB = 0.3f;

        [TweakValue("Gameplay", 0f, 10f)]
        private static float FilthSpawnRadius = 3f;

        private static readonly Material TunnelMaterial = MaterialPool.MatFrom("Things/Filth/Grainy/GrainyA", ShaderDatabase.Transparent);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref secondarySpawnTick, "secondarySpawnTick", 0, false);
            Scribe_Values.Look(ref spawnTunnel, "spawnTunnel", true, false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                secondarySpawnTick = Find.TickManager.TicksGame + ResultSpawnDelay.RandomInRange.SecondsToTicks();
            }
            CreateSustainer();
        }

        public override void Tick()
        {
            if (Spawned)
            {
                sustainer.Maintain();
                Vector3 vector = Position.ToVector3Shifted();
                IntVec3 c;
                if (Rand.MTBEventOccurs(FilthSpawnMTB, 1f, 5.TicksToSeconds()) && CellFinder.TryFindRandomReachableNearbyCell(Position, Map, FilthSpawnRadius, TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false), null, null, out c, 999999))
                {
                    FilthMaker.TryMakeFilth(c, Map, RatkinTunnelUtility.filthTypes.RandomElement<ThingDef>(), 1);
                }

                if (Rand.MTBEventOccurs(DustMoteSpawnMTB, 1f, 1.TicksToSeconds()))
                {
                    FleckMaker.ThrowDustPuffThick(new Vector3(vector.x, 0f, vector.z)
                    {
                        y = AltitudeLayer.MoteOverhead.AltitudeFor()
                    }, Map, Rand.Range(1.5f, 3f), new Color(1f, 1f, 1f, 2.5f));
                }
                if (secondarySpawnTick <= Find.TickManager.TicksGame)
                {
                    sustainer.End();
                    Map map = Map;
                    IntVec3 position = Position;
                    Destroy(DestroyMode.Vanish);
                    if (spawnTunnel)
                    {
                        Building_ThiefTunnel tunnel = (Building_ThiefTunnel)GenSpawn.Spawn(ThingMaker.MakeThing(RatkinBuildingDefOf.RK_ThiefTunnel, null), position, map, WipeMode.Vanish);
                        tunnel.SetFaction(Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia), null);
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Rand.PushState();
            Rand.Seed = thingIDNumber;
            for (int i = 0; i < 6; i++)
            {
                DrawDustPart(Rand.Range(0f, 360f), Rand.Range(0.9f, 1.1f) * Rand.Sign * 4f, Rand.Range(1f, 1.5f));
            }
            Rand.PopState();
        }

        private void DrawDustPart(float initialAngle, float speedMultiplier, float scale)
        {
            float num = (Find.TickManager.TicksGame - secondarySpawnTick).TicksToSeconds();
            Vector3 pos = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Filth);
            pos.y += 0.046875f * Rand.Range(0f, 1f);
            Color value = new Color(0.470588237f, 0.384313732f, 0.3254902f, 0.7f);
            matPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0f, initialAngle + speedMultiplier * num, 0f), Vector3.one * scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TunnelMaterial, 0, null, 0, matPropertyBlock);
        }

        private void CreateSustainer()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                SoundDef tunnel = SoundDefOf.Tunnel;
                sustainer = tunnel.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
            });
        }
    }

    [StaticConstructorOnStartup]
    public class GuerrillaTunnelSpawner : ThingWithComps
    {
        public float eventPoint = 0;
        private int secondarySpawnTick;

        public bool spawnTunnel = true;

        private Sustainer sustainer;

        private static MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();

        private readonly FloatRange ResultSpawnDelay = new FloatRange(12f, 16f);

        [TweakValue("Gameplay", 0f, 1f)]
        private static float DustMoteSpawnMTB = 0.2f;

        [TweakValue("Gameplay", 0f, 1f)]
        private static float FilthSpawnMTB = 0.3f;

        [TweakValue("Gameplay", 0f, 10f)]
        private static float FilthSpawnRadius = 3f;

        private static readonly Material TunnelMaterial = MaterialPool.MatFrom("Things/Filth/Grainy/GrainyA", ShaderDatabase.Transparent);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref secondarySpawnTick, "secondarySpawnTick", 0, false);
            Scribe_Values.Look(ref spawnTunnel, "spawnTunnel", true, false);
            Scribe_Values.Look(ref eventPoint, "eventPoint");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                secondarySpawnTick = Find.TickManager.TicksGame + ResultSpawnDelay.RandomInRange.SecondsToTicks();
            }
            CreateSustainer();
        }

        public override void Tick()
        {
            if (Spawned)
            {
                sustainer.Maintain();
                Vector3 vector = Position.ToVector3Shifted();
                IntVec3 c;
                if (Rand.MTBEventOccurs(FilthSpawnMTB, 1f, 1.TicksToSeconds()) && CellFinder.TryFindRandomReachableNearbyCell(Position, Map, FilthSpawnRadius, TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false), null, null, out c, 999999))
                {
                    FilthMaker.TryMakeFilth(c, Map, RatkinTunnelUtility.filthTypes.RandomElement(), 1);
                }
                if (Rand.MTBEventOccurs(DustMoteSpawnMTB, 1f, 1.TicksToSeconds()))
                {

                    FleckMaker.ThrowDustPuffThick(new Vector3(vector.x, 0f, vector.z)
                    {
                        y = AltitudeLayer.MoteOverhead.AltitudeFor()
                    }, Map, Rand.Range(1.5f, 3f), new Color(1f, 1f, 1f, 2.5f));
                }
                if (secondarySpawnTick <= Find.TickManager.TicksGame)
                {
                    sustainer.End();
                    Map map = Map;
                    IntVec3 position = Position;
                    Destroy(DestroyMode.Vanish);
                    if (spawnTunnel)
                    {
                        Building_GuerrillaTunnel tunnel = (Building_GuerrillaTunnel)GenSpawn.Spawn(ThingMaker.MakeThing(RatkinBuildingDefOf.RK_GuerrillaTunnel, null), position, map, WipeMode.Vanish);
                        tunnel.SetFaction(Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia), null);
                        tunnel.eventPoint = eventPoint;
                        tunnel.SpawnInitialPawns();
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Rand.PushState();
            Rand.Seed = thingIDNumber;
            for (int i = 0; i < 6; i++)
            {
                DrawDustPart(Rand.Range(0f, 360f), Rand.Range(0.9f, 1.1f) * Rand.Sign * 4f, Rand.Range(1f, 1.5f));
            }
            Rand.PopState();
        }

        private void DrawDustPart(float initialAngle, float speedMultiplier, float scale)
        {
            float num = (Find.TickManager.TicksGame - secondarySpawnTick).TicksToSeconds();

            Vector3 pos = Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Filth);
            pos.y += 0.046875f * Rand.Range(0f, 1f);

            Color value = new Color(0.47f, 0.38f, 0.32f, 0.7f);

            matPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0f, initialAngle + speedMultiplier * num, 0f), Vector3.one * scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, TunnelMaterial, 0, null, 0, matPropertyBlock);
        }

        private void CreateSustainer()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                SoundDef tunnel = SoundDefOf.Tunnel;
                sustainer = tunnel.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
            });
        }
    }


}
