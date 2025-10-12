using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
	[StaticConstructorOnStartup]
    public class Verb_GunlanceFiring : Verb_MeleeAttack
	{
		public VerbProperties_Gunlance verbProperties; 

		protected override bool TryCastShot()
		{
			Pawn casterPawn = CasterPawn;
			if (!casterPawn.Spawned|| casterPawn.stances.FullBodyBusy)
			{
				return false;
			}

			Thing targetThing = currentTarget.Thing;
			if(targetThing==null)
			{
				return true;
			}
			if (!CanHitTarget(targetThing))
			{
				Log.Warning(string.Concat(new object[]
				{
					casterPawn,
					" meleed ",
					targetThing,
					" from out of melee position."
				}));
			}
			Pawn targetPawn = targetThing as Pawn;
			if (targetPawn != null && !targetPawn.Dead && (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || targetPawn.MentalStateDef != MentalStateDefOf.SocialFighting))
			{
				targetPawn.mindState.meleeThreat = casterPawn;
				targetPawn.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
			}
			if (!IsTargetImmobile(currentTarget) && casterPawn.skills != null)
			{;
				casterPawn.skills.Learn(SkillDefOf.Melee, 100f * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
				casterPawn.skills.Learn(SkillDefOf.Shooting, 100f * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
			}
			verbProperties = verbProps as VerbProperties_Gunlance;
			GunlanceExplosion explosion = GenSpawn.Spawn(GunlanceDefOf.GunlanceExplosion, caster.Position, caster.Map, 0) as GunlanceExplosion;
			explosion.radius = verbProperties.range;
			explosion.damType = verbProperties.damageDef ?? DamageDefOf.Bomb;
			explosion.instigator = CasterPawn;
			explosion.damAmount = verbProperties.damageAmount;
			explosion.armorPenetration = 1f;
			explosion.weapon = CasterPawn.equipment.Primary.def;
			explosion.projectile = null;
			explosion.intendedTarget = null;
			explosion.preExplosionSpawnThingDef = null;
			explosion.preExplosionSpawnChance = 0f;
			explosion.preExplosionSpawnThingCount = 0;
			explosion.postExplosionSpawnThingDef = null;
			explosion.postExplosionSpawnChance = 0f;
			explosion.postExplosionSpawnThingCount = 0;
			explosion.applyDamageToExplosionCellsNeighbors = false;
			explosion.chanceToStartFire = 0f;
			explosion.damageFalloff = false;
			explosion.needLOSToCell1 = null;
			explosion.needLOSToCell2 = null;
			explosion.PreStartExplosion(TeleUtils.circularSectorCellsStartedTarget(casterPawn.Position, casterPawn.Map,targetThing.Position, verbProperties.range, verbProperties.angle, true).ToList());
			explosion.StartExplosion(null, null);
			CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, true);


			if (casterPawn.Spawned)
			{
				casterPawn.Drawer.Notify_MeleeAttackOn(targetThing);
			}
			if (targetPawn != null && !targetPawn.Dead && targetPawn.Spawned)
			{
				targetPawn.stances.stagger.StaggerFor(95);
			}
			if (casterPawn.Spawned)
			{
				casterPawn.rotationTracker.FaceCell(targetThing.Position);
			}
			if (casterPawn.caller != null)
			{
				casterPawn.caller.Notify_DidMeleeAttack();
			}
			return true;
		}


		private bool IsTargetImmobile(LocalTargetInfo target)
		{
			Thing thing = target.Thing;
			Pawn pawn = thing as Pawn;
			return pawn==null || pawn.Downed || pawn.GetPosture() > PawnPosture.Standing;
		}

		private IEnumerable<DamageInfo> DamageInfosToApply(LocalTargetInfo target)
		{
			float num = this.verbProps.AdjustedMeleeDamageAmount(this, this.CasterPawn);
			float armorPenetration = this.verbProps.AdjustedArmorPenetration(this, this.CasterPawn);
			DamageDef def = this.verbProps.meleeDamageDef;
			BodyPartGroupDef bodyPartGroupDef = null;
			HediffDef hediffDef = null;
			num = Rand.Range(num * 0.8f, num * 1.2f);
			if (this.CasterIsPawn)
			{
				bodyPartGroupDef = this.verbProps.AdjustedLinkedBodyPartsGroup(this.tool);
				if (num >= 1f)
				{
					if (base.HediffCompSource != null)
					{
						hediffDef = base.HediffCompSource.Def;
					}
				}
				else
				{
					num = 1f;
					def = DamageDefOf.Blunt;
				}
			}
			ThingDef source;
			if (base.EquipmentSource != null)
			{
				source = base.EquipmentSource.def;
			}
			else
			{
				source = this.CasterPawn.def;
			}
			Vector3 direction = (target.Thing.Position - this.CasterPawn.Position).ToVector3();
			DamageInfo damageInfo = new DamageInfo(def, num, armorPenetration, -1f, this.caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null);
			damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
			damageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
			damageInfo.SetWeaponHediff(hediffDef);
			damageInfo.SetAngle(direction);
			yield return damageInfo;
			if (this.tool != null && this.tool.extraMeleeDamages != null)
			{
				foreach (ExtraDamage extraDamage in this.tool.extraMeleeDamages)
				{
					if (Rand.Chance(extraDamage.chance))
					{
						num = extraDamage.amount;
						num = Rand.Range(num * 0.8f, num * 1.2f);
						damageInfo = new DamageInfo(extraDamage.def, num, extraDamage.AdjustedArmorPenetration(this, this.CasterPawn), -1f, this.caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null);
						damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
						damageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
						damageInfo.SetWeaponHediff(hediffDef);
						damageInfo.SetAngle(direction);
						yield return damageInfo;
					}
				}
			}
			if (this.surpriseAttack && ((this.verbProps.surpriseAttack != null && !this.verbProps.surpriseAttack.extraMeleeDamages.NullOrEmpty<ExtraDamage>()) || (this.tool != null && this.tool.surpriseAttack != null && !this.tool.surpriseAttack.extraMeleeDamages.NullOrEmpty<ExtraDamage>())))
			{
				IEnumerable<ExtraDamage> enumerable = Enumerable.Empty<ExtraDamage>();
				if (this.verbProps.surpriseAttack != null && this.verbProps.surpriseAttack.extraMeleeDamages != null)
				{
					enumerable = enumerable.Concat(this.verbProps.surpriseAttack.extraMeleeDamages);
				}
				if (this.tool != null && this.tool.surpriseAttack != null && !this.tool.surpriseAttack.extraMeleeDamages.NullOrEmpty<ExtraDamage>())
				{
					enumerable = enumerable.Concat(this.tool.surpriseAttack.extraMeleeDamages);
				}
				foreach (ExtraDamage extraDamage2 in enumerable)
				{
					int num2 = GenMath.RoundRandom(extraDamage2.AdjustedDamageAmount(this, this.CasterPawn));
					float armorPenetration2 = extraDamage2.AdjustedArmorPenetration(this, this.CasterPawn);
					DamageInfo damageInfo2 = new DamageInfo(extraDamage2.def, (float)num2, armorPenetration2, -1f, this.caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null);
					damageInfo2.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
					damageInfo2.SetWeaponBodyPartGroup(bodyPartGroupDef);
					damageInfo2.SetWeaponHediff(hediffDef);
					damageInfo2.SetAngle(direction);
					yield return damageInfo2;
				}
			}
			yield break;
		}

		protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
		{
			DamageWorker.DamageResult result = new DamageWorker.DamageResult();
			foreach (DamageInfo dinfo in this.DamageInfosToApply(target))
			{
				if (target.ThingDestroyed)
				{
					break;
				}
				result = target.Thing.TakeDamage(dinfo);
			}
			return result;
		}
		private SoundDef SoundMiss()
		{
			if (this.CasterPawn != null && !this.CasterPawn.def.race.soundMeleeMiss.NullOrUndefined())
			{
				return this.CasterPawn.def.race.soundMeleeMiss;
			}
			return SoundDefOf.Pawn_Melee_Punch_Miss;
		}
		private SoundDef SoundHitBuilding()
		{
			if (base.EquipmentSource != null && !base.EquipmentSource.def.meleeHitSound.NullOrUndefined())
			{
				return base.EquipmentSource.def.meleeHitSound;
			}
			if (base.EquipmentSource != null && base.EquipmentSource.Stuff != null)
			{
				if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
				{
					if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
					{
						return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
					}
				}
				else if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
				{
					return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
				}
			}
			if (this.CasterPawn != null && !this.CasterPawn.def.race.soundMeleeHitBuilding.NullOrUndefined())
			{
				return this.CasterPawn.def.race.soundMeleeHitBuilding;
			}
			return SoundDefOf.Pawn_Melee_Punch_HitBuilding_Generic;
		}
		private SoundDef SoundHitPawn()
		{
			if (base.EquipmentSource != null && !base.EquipmentSource.def.meleeHitSound.NullOrUndefined())
			{
				return base.EquipmentSource.def.meleeHitSound;
			}
			if (base.EquipmentSource != null && base.EquipmentSource.Stuff != null)
			{
				if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
				{
					if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
					{
						return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
					}
				}
				else if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
				{
					return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
				}
			}
			if (this.CasterPawn != null && !this.CasterPawn.def.race.soundMeleeHitPawn.NullOrUndefined())
			{
				return this.CasterPawn.def.race.soundMeleeHitPawn;
			}
			return SoundDefOf.Pawn_Melee_Punch_HitPawn;
		}
		private float GetDodgeChance(LocalTargetInfo target)
		{
			if (this.surpriseAttack)
			{
				return 0f;
			}
			if (this.IsTargetImmobile(target))
			{
				return 0f;
			}
			Pawn pawn = target.Thing as Pawn;
			if (pawn == null)
			{
				return 0f;
			}
			Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
			if (stance_Busy != null && stance_Busy.verb != null && !stance_Busy.verb.verbProps.IsMeleeAttack)
			{
				return 0f;
			}
			return pawn.GetStatValue(StatDefOf.MeleeDodgeChance, true);
		}
		private float GetNonMissChance(LocalTargetInfo target)
		{
			if (this.surpriseAttack)
			{
				return 1f;
			}
			if (this.IsTargetImmobile(target))
			{
				return 1f;
			}
			return this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChance, true);
		}
	}

	public static class TeleUtils
	{
		public static float AngleWrapped(float angle)
		{
			while (angle > 360f)
			{
				angle -= 360f;
			}
			while (angle < 0f)
			{
				angle += 360f;
			}
			return (angle == 360f) ? 0f : angle;
		}
		public static IEnumerable<IntVec3> circularSectorCellsStartedTarget(IntVec3 center, Map map, IntVec3 target, float radius, float angle, bool useCenter = false)
		{
			int cellCount = GenRadial.NumCellsInRadius(radius);
			float currentDirectionAngle = Vector3Utility.AngleFlat(target.ToVector3Shifted() - center.ToVector3Shifted());
			float angleMin = AngleWrapped(currentDirectionAngle - angle * 0.5f);
			float angleMax = AngleWrapped(angleMin + angle);
			if (useCenter)
			{
				yield return GenRadial.RadialPattern[0] + target;
			}
			for (int i = 1; i < cellCount; i++)
			{
				IntVec3 cell = GenRadial.RadialPattern[i] + target;
				float targetCellAngle = Vector3Utility.AngleFlat(cell.ToVector3Shifted() - target.ToVector3Shifted());
				bool flag = (angleMin > angleMax) ? (targetCellAngle >= angleMin || targetCellAngle <= angleMax) : (targetCellAngle >= angleMin && targetCellAngle <= angleMax);
				if (GenGrid.InBounds(cell, map) && Verse.GenSight.LineOfSight(target, cell, map, true) && flag)
				{
					yield return cell;
				}
			}
			yield break;
		}
		public static IEnumerable<IntVec3> circularSectorCellsStartedCaster(IntVec3 center, Map map, IntVec3 target, float radius, float angle, bool useCenter = false)
		{
			int cellCount = GenRadial.NumCellsInRadius(radius);
			float currentDirectionAngle = Vector3Utility.AngleFlat(target.ToVector3Shifted() - center.ToVector3Shifted());
			float angleMin = AngleWrapped(currentDirectionAngle - angle * 0.5f);
			float angleMax = AngleWrapped(angleMin + angle);
			if (useCenter)
			{
				yield return GenRadial.RadialPattern[0] + center;
			}
			for (int i = 1; i < cellCount; i++)
			{
				IntVec3 cell = GenRadial.RadialPattern[i] + center;
				float targetCellAngle = Vector3Utility.AngleFlat(cell.ToVector3Shifted() - center.ToVector3Shifted());
				bool flag = (angleMin > angleMax) ? (targetCellAngle >= angleMin || targetCellAngle <= angleMax) : (targetCellAngle >= angleMin && targetCellAngle <= angleMax);
				if (GenGrid.InBounds(cell, map) && Verse.GenSight.LineOfSight(center, cell, map, true) && flag)
				{
					yield return cell;
				}
			}
			yield break;
		}
	}
}
