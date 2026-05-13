using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// BFR HE탄 - Bullet 기반 부채꼴 폭발 투사체.
    /// Projectile_Explosive를 상속하지 않아 CausesExplosion == false.
    /// Impact()에서 직접 부채꼴 셀 판정 → 폭발 처리.
    /// </summary>
    public class Projectile_ProximityBurst : Bullet
    {
        private ProjectileProperties_ProximityBurst ProximityProps => def.projectile as ProjectileProperties_ProximityBurst;
        private bool reachedDetonationPoint;

        protected override int MaxTickIntervalRate => 1;

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            float preDet = ProximityProps?.preDetonationDistance ?? 0f;
            if (preDet > 0f)
            {
                Vector3 targetVec = usedTarget.Cell.ToVector3Shifted();
                Vector3 dir = (targetVec - origin).Yto0();
                float dist = dir.magnitude;
                if (dist > preDet + 1f)
                {
                    Vector3 detonationPoint = targetVec - dir.normalized * preDet;
                    IntVec3 detonationCell = detonationPoint.ToIntVec3();
                    usedTarget = new LocalTargetInfo(detonationCell);
                }
            }
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }

        protected override void ImpactSomething()
        {
            reachedDetonationPoint = true;
            base.ImpactSomething();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 impactPos = Position;

            ProjectileProperties_ProximityBurst props = ProximityProps;
            if (map == null || props == null)
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            if (blockedByShield)
            {
                GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
                Destroy(DestroyMode.Vanish);
                return;
            }

            if (ShouldSuppressSectorBurstOnHit(hitThing))
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);

            if (!reachedDetonationPoint && props.preDetonationDistance > 0f)
            {
                ApplyDirectHitDamage(map, impactPos, props);
                Destroy(DestroyMode.Vanish);
                return;
            }

            List<IntVec3> shieldedCells;
            HashSet<CompProjectileInterceptor> hitShields;
            List<IntVec3> sectorCells = GetSectorCells(impactPos, map, props, out shieldedCells, out hitShields);
            if (sectorCells.Count > 0)
            {
                DoSectorExplosion(impactPos, map, sectorCells, props);
            }

            SpawnCenterExplosionVisual(impactPos, map, props);
            SpawnSectorCellEffects(map, sectorCells, props);
            SpawnShieldBlockEffects(map, shieldedCells, hitShields);

            Destroy(DestroyMode.Vanish);
        }

        private static bool ShouldSuppressSectorBurstOnHit(Thing hitThing)
        {
            if (hitThing is Building b)
            {
                if (b.def.IsWall) return true;
                if (b.def.building != null && b.def.building.isNaturalRock) return true;
            }
            return hitThing is Building_Door d && !d.Open;
        }

        private int ScaledRangedDamageFromBase(int baseAmount)
        {
            if (baseAmount <= 0)
                return 0;
            float mult = equipment != null ? equipment.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier, true, -1) : 1f;
            return Mathf.RoundToInt(baseAmount * mult);
        }

        private float ScaledExplicitArmorPen(float explicitBase, DamageDef dmgDef)
        {
            if (dmgDef == null || dmgDef.armorCategory == null)
                return 0f;
            float mult = equipment != null ? equipment.GetStatValue(StatDefOf.RangedWeapon_ArmorPenetrationMultiplier, true, -1) : 1f;
            return explicitBase * mult;
        }

        private void ApplyDirectHitDamage(Map map, IntVec3 impactPos, ProjectileProperties_ProximityBurst props)
        {
            int damAmount = props.damageAmountDirect > 0 ? ScaledRangedDamageFromBase(props.damageAmountDirect) : DamageAmount;
            DamageDef damageDef = props.damageDefDirect ?? DamageDef;
            float armorPen = props.armorPenetrationDirect >= 0f ? ScaledExplicitArmorPen(props.armorPenetrationDirect, damageDef) : ArmorPenetration;

            foreach (Thing thing in impactPos.GetThingList(map).ToList())
            {
                if (thing == launcher || thing == this)
                    continue;
                if (thing.Destroyed)
                    continue;

                Pawn pawn = launcher as Pawn;
                bool instigatorGuilty = pawn == null || !pawn.Drafted;
                DamageInfo dinfo = new DamageInfo(
                    damageDef,
                    damAmount,
                    armorPen,
                    ExactRotation.eulerAngles.y,
                    launcher,
                    null,
                    equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    intendedTarget.Thing,
                    instigatorGuilty,
                    true,
                    QualityCategory.Normal,
                    true,
                    false);
                dinfo.SetWeaponQuality(equipmentQuality);
                thing.TakeDamage(dinfo);
            }
        }

        private List<IntVec3> GetSectorCells(IntVec3 center, Map map, ProjectileProperties_ProximityBurst props,
            out List<IntVec3> shieldedCells, out HashSet<CompProjectileInterceptor> hitShields)
        {
            List<IntVec3> result = new List<IntVec3>();
            shieldedCells = new List<IntVec3>();
            hitShields = new HashSet<CompProjectileInterceptor>();
            float sectorRadius = props.sectorRadius > 0f ? props.sectorRadius : props.explosionRadius;
            float halfAngle = (props.sectorAngle > 0f ? props.sectorAngle : 90f) * 0.5f;
            float wallBreachRadius = props.wallBreachRadius > 0f ? props.wallBreachRadius : 1.7f;
            float wallBreachRadiusSq = wallBreachRadius * wallBreachRadius;

            var shieldZones = GetHostileShieldZones(map, center);

            Vector3 centerVec = center.ToVector3Shifted().Yto0();
            Vector3 dirToBack = (destination - origin).Yto0();
            if (dirToBack.sqrMagnitude < 1E-06f)
                return result;
            dirToBack.Normalize();
            float centerAngle = Vector3.SignedAngle(Vector3.right, dirToBack, Vector3.up);

            int numCells = GenRadial.NumCellsInRadius(sectorRadius);
            for (int i = 1; i < numCells; i++)
            {
                IntVec3 offset = GenRadial.RadialPattern[i];
                IntVec3 cell = center + offset;
                if (!cell.InBounds(map))
                    continue;
                if (cell == center)
                    continue;

                Vector3 cellVec = cell.ToVector3Shifted().Yto0();
                float cellAngle = Vector3.SignedAngle(Vector3.right, (cellVec - centerVec).normalized, Vector3.up);
                if (Mathf.Abs(Mathf.DeltaAngle(cellAngle, centerAngle)) > halfAngle)
                    continue;

                int shieldIdx = GetBlockingShieldIndex(cell, shieldZones);
                if (shieldIdx >= 0)
                {
                    shieldedCells.Add(cell);
                    hitShields.Add(shieldZones[shieldIdx].comp);
                    continue;
                }

                float distSq = (cell - center).LengthHorizontalSquared;
                if (distSq <= wallBreachRadiusSq)
                {
                    result.Add(cell);
                }
                else if (GenSight.LineOfSight(center, cell, map, true, null, 0, 0))
                {
                    result.Add(cell);
                }
            }

            if (center.InBounds(map))
            {
                int centerShield = GetBlockingShieldIndex(center, shieldZones);
                if (centerShield >= 0)
                {
                    shieldedCells.Add(center);
                    hitShields.Add(shieldZones[centerShield].comp);
                }
                else if (!result.Contains(center))
                {
                    result.Add(center);
                }
            }

            AddAdjacentWallCells(center, map, sectorRadius, result);
            return result;
        }

        private static int GetBlockingShieldIndex(IntVec3 cell, List<ShieldZone> zones)
        {
            Vector3 cellVec = cell.ToVector3Shifted();
            Vector2 cellPos = new Vector2(cellVec.x, cellVec.z);
            for (int i = 0; i < zones.Count; i++)
            {
                float dx = cellPos.x - zones[i].center.x;
                float dy = cellPos.y - zones[i].center.y;
                if (dx * dx + dy * dy <= zones[i].radiusSq)
                    return i;
            }
            return -1;
        }

        private struct ShieldZone
        {
            public Vector2 center;
            public float radiusSq;
            public CompProjectileInterceptor comp;
        }

        private List<ShieldZone> GetHostileShieldZones(Map map, IntVec3 explosionCenter)
        {
            var zones = new List<ShieldZone>();
            Vector3 expVec = explosionCenter.ToVector3Shifted();
            Vector2 expPos = new Vector2(expVec.x, expVec.z);
            List<Thing> interceptors = map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < interceptors.Count; i++)
            {
                Thing shieldThing = interceptors[i];
                var comp = shieldThing.TryGetComp<CompProjectileInterceptor>();
                if (comp == null || !comp.Active)
                    continue;
                if (!comp.Props.interceptGroundProjectiles)
                    continue;
                if (launcher != null && shieldThing.Faction != null && !launcher.HostileTo(shieldThing))
                    continue;
                Vector3 pos = shieldThing.Position.ToVector3Shifted();
                float r = comp.Props.radius;
                float rSq = r * r;
                float dx = expPos.x - pos.x;
                float dz = expPos.y - pos.z;
                if (dx * dx + dz * dz <= rSq)
                    continue;
                zones.Add(new ShieldZone { center = new Vector2(pos.x, pos.z), radiusSq = rSq, comp = comp });
            }
            return zones;
        }

        private void AddAdjacentWallCells(IntVec3 center, Map map, float radius, List<IntVec3> result)
        {
            HashSet<IntVec3> resultSet = new HashSet<IntVec3>(result);
            List<IntVec3> adjWalls = new List<IntVec3>();
            for (int j = 0; j < result.Count; j++)
            {
                IntVec3 cell = result[j];
                Building edifice = cell.GetEdifice(map);
                if (cell.Walkable(map))
                {
                    if (edifice != null && edifice.def.Fillage == FillCategory.Full)
                    {
                        Building_Door door = edifice as Building_Door;
                        if (door == null || !door.Open)
                            continue;
                    }
                    for (int k = 0; k < 4; k++)
                    {
                        IntVec3 adj = cell + GenAdj.CardinalDirections[k];
                        if (adj.InHorDistOf(center, radius) && adj.InBounds(map) && !adj.Standable(map) && adj.GetEdifice(map) != null && !resultSet.Contains(adj) && !adjWalls.Contains(adj))
                        {
                            adjWalls.Add(adj);
                        }
                    }
                }
            }
            result.AddRange(adjWalls);
        }

        private void SpawnCenterExplosionVisual(IntVec3 center, Map map, ProjectileProperties_ProximityBurst props)
        {
            List<IntVec3> overrideCells = new List<IntVec3> { center };
            SoundDef explosionSound = def.projectile.soundExplode;
            float visualRadius = props.centerExplosionVisualRadius > 0f ? props.centerExplosionVisualRadius : 1f;
            GenExplosion.DoExplosion(
                center, map, visualRadius, DamageDefOf.Bomb, null,
                0, -1f, explosionSound,
                null, null, null, null,
                0f, 1, null, null, 255, false,
                null, 0f, 1, 0f, false, null, null, null, false,
                1f, 0f, true, null, 1f, null, overrideCells, null, null);

            FleckDef wyvernFleck = props.centerExplosionFleckDef ?? DefDatabase<FleckDef>.GetNamedSilentFail("RK_WyvernFireExplosion");
            if (wyvernFleck != null)
            {
                Vector3 dir = (destination - origin).Yto0();
                float rot = dir.sqrMagnitude > 1E-06f ? dir.AngleFlat() : 0f;
                var data = FleckMaker.GetDataStatic(center.ToVector3Shifted(), map, wyvernFleck, 1f);
                data.exactScale = new Vector3?(props.centerExplosionFleckScale);
                data.rotation = rot;
                data.instanceColor = props.centerExplosionFleckColor;
                map.flecks.CreateFleck(data);
            }
        }

        private void SpawnSectorCellEffects(Map map, List<IntVec3> sectorCells, ProjectileProperties_ProximityBurst props)
        {
            int effectsPerCell = props.sectorEffectsPerCell > 0 ? props.sectorEffectsPerCell : 2;
            float noiseRange = props.sectorEffectNoiseRange >= 0f ? props.sectorEffectNoiseRange : 0.3f;
            FleckDef fleck = props.sectorCellFleckDef ?? FleckDefOf.ShotHit_Dirt;

            foreach (IntVec3 cell in sectorCells)
            {
                Vector3 basePos = cell.ToVector3Shifted();
                for (int i = 0; i < effectsPerCell; i++)
                {
                    Vector3 noise = new Vector3(Rand.Range(-noiseRange, noiseRange), 0f, Rand.Range(-noiseRange, noiseRange));
                    Vector3 spawnPos = basePos + noise;
                    if (spawnPos.InBounds(map))
                    {
                        FleckMaker.Static(spawnPos, map, fleck, 1f);
                    }
                }
            }
        }

        private void SpawnShieldBlockEffects(Map map, List<IntVec3> shieldedCells, HashSet<CompProjectileInterceptor> hitShields)
        {
            if (shieldedCells.Count == 0)
                return;

            foreach (var comp in hitShields)
            {
                Vector3 shieldPos = comp.parent.Position.ToVector3Shifted();
                Vector2 shieldCenter = new Vector2(shieldPos.x, shieldPos.z);
                float radius = comp.Props.radius;
                float radiusSq = radius * radius;
                float innerThreshold = (radius - 1.5f) * (radius - 1.5f);

                EffecterDef effecterDef = comp.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile;
                HashSet<IntVec3> usedCells = new HashSet<IntVec3>();

                for (int i = 0; i < shieldedCells.Count; i++)
                {
                    Vector3 cv = shieldedCells[i].ToVector3Shifted();
                    Vector2 cp = new Vector2(cv.x, cv.z);
                    float dx = cp.x - shieldCenter.x;
                    float dy = cp.y - shieldCenter.y;
                    float dSq = dx * dx + dy * dy;
                    if (dSq >= innerThreshold && dSq <= radiusSq && !usedCells.Contains(shieldedCells[i]))
                    {
                        usedCells.Add(shieldedCells[i]);
                        Effecter effecter = new Effecter(effecterDef);
                        effecter.Trigger(new TargetInfo(shieldedCells[i], map, false), TargetInfo.Invalid);
                        effecter.Cleanup();
                    }
                }

                if (usedCells.Count == 0 && shieldedCells.Count > 0)
                {
                    IntVec3 fallback = shieldedCells[0];
                    Effecter effecter = new Effecter(effecterDef);
                    effecter.Trigger(new TargetInfo(fallback, map, false), TargetInfo.Invalid);
                    effecter.Cleanup();
                }
            }
        }

        private void DoSectorExplosion(IntVec3 center, Map map, List<IntVec3> sectorCells, ProjectileProperties_ProximityBurst props)
        {
            DamageDef damageDef = props.damageDefExplosion ?? DamageDef;
            int damAmount = props.damageAmountExplosion > 0 ? ScaledRangedDamageFromBase(props.damageAmountExplosion) : DamageAmount;
            float armorPen = props.armorPenetrationExplosion >= 0f ? ScaledExplicitArmorPen(props.armorPenetrationExplosion, damageDef) : ArmorPenetration;

            float launcherAngle = (destination - origin).Yto0().AngleFlat();

            Pawn instigatorPawn = launcher as Pawn;
            bool instigatorGuilty = instigatorPawn == null || !instigatorPawn.Drafted;

            HashSet<Thing> alreadyDamaged = new HashSet<Thing>();

            foreach (IntVec3 cell in sectorCells)
            {
                List<Thing> thingList = cell.GetThingList(map);
                for (int i = thingList.Count - 1; i >= 0; i--)
                {
                    Thing t = thingList[i];
                    if (t == launcher || t == this) continue;
                    if (t.Destroyed) continue;
                    if (t.def.category == ThingCategory.Mote || t.def.category == ThingCategory.Ethereal) continue;
                    if (alreadyDamaged.Contains(t)) continue;

                    alreadyDamaged.Add(t);

                    float angle;
                    if (launcher != null && launcher.Spawned && t.Position != launcher.Position)
                    {
                        angle = (t.Position - launcher.Position).AngleFlat;
                    }
                    else
                    {
                        angle = launcherAngle;
                    }

                    DamageInfo dinfo = new DamageInfo(
                        damageDef,
                        damAmount,
                        armorPen,
                        angle,
                        launcher,
                        null,
                        equipmentDef,
                        DamageInfo.SourceCategory.ThingOrUnknown,
                        intendedTarget.Thing,
                        instigatorGuilty,
                        true,
                        QualityCategory.Normal,
                        true,
                        false);
                    dinfo.SetWeaponQuality(equipmentQuality);
                    t.TakeDamage(dinfo);
                }
            }
        }
    }
}
