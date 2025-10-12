using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
    public class ProjectileProperties_BallistaArrow : ProjectileProperties
    {
        public DamageDef shockWaveDef;
    }
    public class Projectile_BallistaArrow : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef);

            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            if (hitThing != null)
            {
                DamageDef damageDef = def.projectile.damageDef;
                float amount = DamageAmount;
                float armorPenetration = ArmorPenetration;
                float y = ExactRotation.eulerAngles.y;
                Thing launcher = this.launcher;
                ThingDef equipmentDef = this.equipmentDef;
                DamageInfo dinfo = new DamageInfo(damageDef, amount, armorPenetration, y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn pawn = hitThing as Pawn;
                if (pawn != null && pawn.stances != null && pawn.BodySize <= def.projectile.StoppingPower + 0.001f)
                {
                    pawn.stances.stagger.StaggerFor(95);
                }
                RKSoundDefOf.Ballista_Impact.PlayOneShot(new TargetInfo(hitThing.Position, map, false));
                FilthMaker.TryMakeFilth(Position, map, ThingDefOf.Filth_Blood, 4);
            }
            else
            {
                MoteMaker.MakeStaticMote(this.ExactPosition, map, ThingDefOf.Mote_Bombardment, 1f);
                if (Position.GetTerrain(map).takeSplashes)
                {
                    MoteMaker.MakeBombardmentMote(this.DestinationCell, map, 4f);
                }
            }
            Explode();
        }
        protected virtual void Explode()
        {
            Map map = Map;
            Destroy(DestroyMode.Vanish);
            if (this.def.projectile.explosionEffect != null)
            {
                Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(Position, map, false), new TargetInfo(Position, map, false));
                effecter.Cleanup();
            }
            IntVec3 position = Position;
            Map map2 = map;
            float explosionRadius = this.def.projectile.explosionRadius;
            Thing launcher = this.launcher;
            DamageDef damageDef = ((ProjectileProperties_BallistaArrow)this.def.projectile).shockWaveDef; //BallistaDamageDefOf.ShockWaveHeavy;
            int damageAmount = damageDef.defaultDamage;
            float armorPenetration = damageDef.defaultArmorPenetration;
            SoundDef soundExplode = this.def.projectile.soundExplode;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            Thing thing = intendedTarget.Thing;
            ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            GenExplosion.DoExplosion(
                position, 
                map2,
                explosionRadius, 
                damageDef, 
                launcher,
                damageAmount, 
                armorPenetration,
                soundExplode,
                equipmentDef,
                def, 
                thing,
                postExplosionSpawnThingDef,
                postExplosionSpawnChance,
                postExplosionSpawnThingCount, 
                null,
                this.def.projectile.applyDamageToExplosionCellsNeighbors, 
                preExplosionSpawnThingDef,
                this.def.projectile.preExplosionSpawnChance,
                this.def.projectile.preExplosionSpawnThingCount, 
                this.def.projectile.explosionChanceToStartFire, 
                this.def.projectile.explosionDamageFalloff
                );
        }
    }

    public class ProjectileProperties_BallistaBoltAP : ProjectileProperties
    {
        public DamageDef shockWaveDef;
        public int maxPenetrationCount = 1;
        public float damageReduceRate = 1;
    }

    public class Projectile_BallistaBoltAP : Projectile
    {

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }
        private int currentPenetrationCount = 0;

        private Sustainer ambientSustainer;
        private static List<IntVec3> checkedCells = new List<IntVec3>();
        private static readonly List<Thing> cellThingsFiltered = new List<Thing>();

        public override void Tick()
        {
            if (AllComps != null)
            {
                int i = 0;
                int count = AllComps.Count;
                while (i < count)
                {
                    AllComps[i].CompTick();
                    i++;
                }
            }
            if (landed)
            {
                return;
            }
            Vector3 exactPosition = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBounds(Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
                return;
            }
            Vector3 exactPosition2 = ExactPosition;
            if (CheckForFreeInterceptBetween(exactPosition, exactPosition2))
            {
                return;
            }
            Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && this.def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (ticksToImpact <= 0)
            {
                if (DestinationCell.InBounds(Map))
                {
                    Position = DestinationCell;
                }
                ImpactSomething();
                return;
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
        }

        private bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
        {
            IntVec3 intVec = lastExactPos.ToIntVec3();
            IntVec3 intVec2 = newExactPos.ToIntVec3();
            if (intVec2 == intVec)
            {
                return false;
            }
            if (!intVec.InBounds(Map) || !intVec2.InBounds(Map))
            {
                return false;
            }
            if (intVec2.AdjacentToCardinal(intVec))
            {
                return CheckForFreeIntercept(intVec2);
            }
            if (VerbUtility.InterceptChanceFactorFromDistance(this.origin, intVec2) <= 0f)
            {
                return false;
            }
            Vector3 vector = lastExactPos;
            Vector3 v = newExactPos - lastExactPos;
            Vector3 b = v.normalized * 0.2f;
            int num = (int)(v.MagnitudeHorizontal() / 0.2f);
            checkedCells.Clear();
            int num2 = 0;
            for (; ; )
            {
                vector += b;
                IntVec3 intVec3 = vector.ToIntVec3();
                if (!checkedCells.Contains(intVec3))
                {
                    if (CheckForFreeIntercept(intVec3))
                    {
                        break;
                    }
                    checkedCells.Add(intVec3);
                }
                num2++;
                if (num2 > num)
                {
                    return false;
                }
                if (intVec3 == intVec2)
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckForFreeIntercept(IntVec3 c)
        {
            
            if (destination.ToIntVec3() == c)
            {
                return false;
            }
            float num = VerbUtility.InterceptChanceFactorFromDistance(origin, c);
            bool flag = false;
            List<Thing> thingList = c.GetThingList(Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (CanHit(thing))
                {
                    bool flag2 = false;
                    if (thing.def.Fillage == FillCategory.Full)
                    {
                        Building_Door building_Door = thing as Building_Door;
                        if (building_Door == null || !building_Door.Open)
                        {
                            ThrowDebugText("int-wall", c);
                            Impact(thing);
                            return true;
                        }
                        flag2 = true;
                    }
                    float num2 = 0f;
                    Pawn pawn = thing as Pawn;
                    if (pawn != null)
                    {
                        num2 = 0.8f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
                        if (pawn.GetPosture() != PawnPosture.Standing)
                        {
                            num2 *= 0.1f;
                        }
                        if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))//아군 적중률
                        {
                            num2 *= 0.4f;
                        }
                    }
                    else if (thing.def.fillPercent > 0.2f)
                    {
                        if (flag2)
                        {
                            num2 = 0.05f;
                        }
                        else if (DestinationCell.AdjacentTo8Way(c))
                        {
                            num2 = thing.def.fillPercent * 1f;
                        }
                        else
                        {
                            num2 = thing.def.fillPercent * 0.15f;
                        }
                    }
                    num2 *= num;
                    if (num2 > 1E-05f)
                    {
                        if (Rand.Chance(num2))
                        {
                            ThrowDebugText("int-" + num2.ToStringPercent(), c);
                            Impact(thing);
                            return true;
                        }
                        flag = true;
                        ThrowDebugText(num2.ToStringPercent(), c);
                    }
                }
            }
            if (!flag)
            {
                ThrowDebugText("o", c);
            }
            return false;
        }

        private void ThrowDebugText(string text, IntVec3 c)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), base.Map, text, -1f);
            }
        }

        protected new bool CanHit(Thing thing)
        {
            if (!thing.Spawned)
            {
                return false;
            }
            if (thing == launcher)
            {
                return false;
            }
            bool flag = false;
            CellRect c  = thing.OccupiedRect();
            foreach(IntVec3 v in c)
            {
                List<Thing> thingList = v.GetThingList(base.Map); 
                bool flag2 = false;
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] != thing && thingList[i].def.Fillage == FillCategory.Full && thingList[i].def.Altitude >= thing.def.Altitude)
                    {
                        flag2 = true;
                        break;
                    }
                }
                if (!flag2)
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                return false;
            }
            ProjectileHitFlags hitFlags = HitFlags;
            if (thing == intendedTarget && (hitFlags & ProjectileHitFlags.IntendedTarget) != ProjectileHitFlags.None)
            {
                return true;
            }
            if (thing != intendedTarget)
            {
                if (thing is Pawn)
                {
                    if ((hitFlags & ProjectileHitFlags.NonTargetPawns) != ProjectileHitFlags.None)
                    {
                        return true;
                    }
                }
                else if ((hitFlags & ProjectileHitFlags.NonTargetWorld) != ProjectileHitFlags.None)
                {
                    return true;
                }
            }
            return thing == intendedTarget && thing.def.Fillage == FillCategory.Full;
        }

        protected override void ImpactSomething()
        {
            if (def.projectile.flyOverhead)
            {
                RoofDef roofDef = base.Map.roofGrid.RoofAt(base.Position);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        ThrowDebugText("hit-thick-roof", Position);
                        def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(Position, Map, false));
                        Destroy(DestroyMode.Vanish);
                        return;
                    }
                    if (Position.GetEdifice(Map) == null || Position.GetEdifice(Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(Position, Map, null);
                    }
                }
            }
            if (!usedTarget.HasThing || !CanHit(usedTarget.Thing))
            {
                cellThingsFiltered.Clear();
                List<Thing> thingList = Position.GetThingList(Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if ((thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Pawn || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Plant) && this.CanHit(thing))
                    {
                        cellThingsFiltered.Add(thing);
                    }
                }
                cellThingsFiltered.Shuffle();
                for (int j = 0; j < cellThingsFiltered.Count; j++)
                {
                    Thing thing2 = cellThingsFiltered[j];
                    Pawn pawn = thing2 as Pawn;
                    float num;
                    if (pawn != null)
                    {
                        num = 0.5f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
                        if (pawn.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f)
                        {
                            num *= 0.2f;
                        }
                        if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
                        {
                            num *= VerbUtility.InterceptChanceFactorFromDistance(origin, Position);
                        }
                    }
                    else
                    {
                        num = 1.5f * thing2.def.fillPercent;
                    }
                    if (Rand.Chance(num))
                    {
                        ThrowDebugText("hit-" + num.ToStringPercent(), base.Position);
                        Impact(cellThingsFiltered.RandomElement<Thing>());
                        return;
                    }
                    ThrowDebugText("miss-" + num.ToStringPercent(), base.Position);
                }
                Impact(null);
                return;
            }
            Pawn pawn2 = usedTarget.Thing as Pawn;
            if (pawn2 != null && pawn2.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && !Rand.Chance(0.2f))
            {
                ThrowDebugText("miss-laying", base.Position);
                Impact(null);
                return;
            }
            Impact(usedTarget.Thing);
        }
        private int ticksToDetonation;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksToDetonation, "ticksToDetonation", 0, false);
            Scribe_Values.Look(ref currentPenetrationCount, "currentPenetrationCount", 0);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher, hitThing, intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            if (hitThing != null)
            {
                DamageDef damageDef = def.projectile.damageDef;
                float amount = DamageAmount - ((ProjectileProperties_BallistaBoltAP)def.projectile).damageReduceRate * currentPenetrationCount;
                float armorPenetration = ArmorPenetration - ((ProjectileProperties_BallistaBoltAP)def.projectile).damageReduceRate * currentPenetrationCount;
                float y = ExactRotation.eulerAngles.y;
                Thing launcher = this.launcher;
                ThingDef equipmentDef = this.equipmentDef;
                DamageInfo dinfo = new DamageInfo(damageDef, amount, armorPenetration, y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn pawn = hitThing as Pawn;
                if(pawn!=null)
                {
                    FilthMaker.TryMakeFilth(Position, map, ThingDefOf.Filth_Blood, 4);
                    RKSoundDefOf.Ballista_Impact.PlayOneShot(new TargetInfo(Position, map, false));
                    if (pawn.stances != null && pawn.BodySize <= def.projectile.StoppingPower + 0.001f)
                    {
                        pawn.stances.stagger.StaggerFor(95);
                    }
                }
                currentPenetrationCount++;
            }
            else
            {
                FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt, 1f);
                if (Position.GetTerrain(map).takeSplashes)
                {
                    FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
                }
            }
            if (currentPenetrationCount < ((ProjectileProperties_BallistaBoltAP)def.projectile).maxPenetrationCount)
            {
                if (hitThing != null && hitThing.def.category == ThingCategory.Building || ticksToImpact <= 0)
                {
                    Explode();
                }
            }
            else
            {
                base.Impact(hitThing);
            }
        }
        protected virtual void Explode()
        {
            Map map = Map;
            if (this.def.projectile.explosionEffect != null)
            {
                Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(Position, map, false), new TargetInfo(Position, map, false));
                effecter.Cleanup();
            }
            IntVec3 position = Position;
            Map map2 = map;
            float explosionRadius = this.def.projectile.explosionRadius;
            Thing launcher = this.launcher;
            DamageDef damageDef = ((ProjectileProperties_BallistaBoltAP)this.def.projectile).shockWaveDef; //BallistaDamageDefOf.ShockWaveHeavy;
            int damageAmount = damageDef.defaultDamage;
            float armorPenetration = damageDef.defaultArmorPenetration;
            SoundDef soundExplode = this.def.projectile.soundExplode;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            Thing thing = intendedTarget.Thing;
            ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            GenExplosion.DoExplosion(
                position, 
                map2, 
                explosionRadius, 
                damageDef, 
                launcher,
                damageAmount, 
                armorPenetration,
                soundExplode, 
                equipmentDef, 
                def,
                thing,
                postExplosionSpawnThingDef,
                postExplosionSpawnChance, 
                postExplosionSpawnThingCount,
                null,
                this.def.projectile.applyDamageToExplosionCellsNeighbors,
                preExplosionSpawnThingDef,
                this.def.projectile.preExplosionSpawnChance, 
                this.def.projectile.preExplosionSpawnThingCount, 
                this.def.projectile.explosionChanceToStartFire, 
                this.def.projectile.explosionDamageFalloff);
            Destroy(DestroyMode.Vanish);
        }
    }
    
    [DefOf]
    public static class BallistaDamageDefOf
    {
        public static DamageDef ShockWaveLight;
        public static DamageDef ShockWaveHeavy;
    }
    [DefOf]
    public static class RKSoundDefOf
    {
        public static SoundDef Ballista_Impact;
    }
}
