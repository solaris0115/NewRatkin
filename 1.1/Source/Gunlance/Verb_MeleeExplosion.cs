using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
using Verse;

namespace NewRatkin
{
	[StaticConstructorOnStartup]
    public class Verb_GunlanceFiring : Verb
	{
		public VerbProperties_Gunlance verbProperties; 
		protected override bool TryCastShot()
		{;
			Pawn casterPawn = CasterPawn;
			if (!casterPawn.Spawned|| casterPawn.stances.FullBodyBusy)
			{
				return false;
			}

			Thing targetThing = currentTarget.Thing;
			if (!CanHitTarget(targetThing))
			{
				Log.Warning(string.Concat(new object[]
				{
					casterPawn,
					" meleed ",
					targetThing,
					" from out of melee position."
				}), false);
			}
			if (!IsTargetImmobile(currentTarget) && casterPawn.skills != null)
			{
				casterPawn.skills.Learn(SkillDefOf.Melee, 100f * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
				casterPawn.skills.Learn(SkillDefOf.Shooting, 100f * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
			}
			Pawn targetPawn = targetThing as Pawn;
			if (targetPawn != null && !targetPawn.Dead && (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || targetPawn.MentalStateDef != MentalStateDefOf.SocialFighting))
			{
				targetPawn.mindState.meleeThreat = casterPawn;
				targetPawn.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
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
			explosion.PreStartExplosion(TeleUtils.circularSectorCells(casterPawn.Position, casterPawn.Map,targetThing.Position, verbProperties.range, verbProperties.angle, true).ToList());
			explosion.StartExplosion(null, null);
			CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, false);
			Log.Message("1");

			if (casterPawn.Spawned)
			{
				casterPawn.Drawer.Notify_MeleeAttackOn(targetThing);
			}
			if (targetPawn != null && !targetPawn.Dead && targetPawn.Spawned)
			{
				targetPawn.stances.StaggerFor(95);
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
		public BattleLogEntry_MeleeCombat CreateCombatLog(Func<ManeuverDef, RulePackDef> rulePackGetter, bool alwaysShow)
		{
			if (maneuver == null)
			{
				return null;
			}
			if (tool == null)
			{
				return null;
			}
			BattleLogEntry_MeleeCombat battleLogEntry_MeleeCombat = new BattleLogEntry_MeleeCombat(rulePackGetter(maneuver), alwaysShow, this.CasterPawn, this.currentTarget.Thing, base.ImplementOwnerType, this.tool.labelUsedInLogging ? this.tool.label : "", (base.EquipmentSource == null) ? null : base.EquipmentSource.def, (base.HediffCompSource == null) ? null : base.HediffCompSource.Def, this.maneuver.logEntryDef);
			Find.BattleLog.Add(battleLogEntry_MeleeCombat);
			return battleLogEntry_MeleeCombat;
		}


		private bool IsTargetImmobile(LocalTargetInfo target)
		{
			Thing thing = target.Thing;
			Pawn pawn = thing as Pawn;
			return thing.def.category != ThingCategory.Pawn || pawn.Downed || pawn.GetPosture() > PawnPosture.Standing;
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
			return SoundDefOf.Pawn_Melee_Punch_HitBuilding;
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
		public static IEnumerable<IntVec3> circularSectorCells(IntVec3 center, Map map, IntVec3 target, float radius, float angle, bool useCenter = false)
		{
			int cellCount = GenRadial.NumCellsInRadius(radius);
			float currentDirectionAngle = Vector3Utility.AngleFlat(target.ToVector3Shifted() - center.ToVector3Shifted());
			float angleMin = AngleWrapped(currentDirectionAngle - angle * 0.5f);
			float angleMax = AngleWrapped(angleMin + angle);
			/*Log.Message(
					 "currentDirectionAngle: " + currentDirectionAngle +
					 "\nangleMin: " + angleMin +
					 "\nangleMax: " + angleMax
					 );*/
			if (useCenter)
			{
				yield return GenRadial.RadialPattern[0] + target;
			}
			for (int i = 1; i < cellCount; i++)
			{
				IntVec3 cell = GenRadial.RadialPattern[i] + target;
				float targetCellAngle = Vector3Utility.AngleFlat(cell.ToVector3Shifted() - target.ToVector3Shifted());
				bool flag = (angleMin > angleMax) ? (targetCellAngle >= angleMin || targetCellAngle <= angleMax) : (targetCellAngle >= angleMin && targetCellAngle <= angleMax);
				//Log.Message("Los[" + i + "]" + GenRadial.RadialPattern[i] + cell + ":" + GenSight.LineOfSight(target, cell, map, true));
				if (GenGrid.InBounds(cell, map) && Verse.GenSight.LineOfSight(target, cell, map, true) && flag)
				{
					yield return cell;
				}
			}
			yield break;
		}
	}
}
