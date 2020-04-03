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
    public class Verb_WyvernFire: Verb
	{
		public VerbProperties_Gunlance verbProperties;

		public override bool Available()
		{
			if(burstShotsLeft>0)
			{
				return true;
			}
			if (verbProps.consumeFuelPerShot > 0f)
			{
				CompGunlanceFuel compGunlance = this.EquipmentSource.TryGetComp<CompGunlanceFuel>();
				if ((compGunlance != null && compGunlance.Fuel < verbProps.consumeFuelPerShot))
				{
					return false;
				}
			}
			return true;
		}
		protected override int ShotsPerBurst
		{
			get
			{
				return this.verbProps.burstShotCount;
			}
		}
		protected override bool TryCastShot()
		{
			if (burstShotsLeft>1)
			{
				RatkinSoundDefOf.RK_Charge.PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
				if (verbProps.consumeFuelPerShot > 0f)
				{
					CompRefuelable compGunlance = EquipmentSource.TryGetComp<CompGunlanceFuel>();
					if (compGunlance != null)
					{
						compGunlance.ConsumeFuel(verbProps.consumeFuelPerShot);
					}
				}
			}
			else
			{
				RatkinSoundDefOf.RK_WyvernFire.PlayOneShot(new TargetInfo(caster.Position, caster.Map, false));
				MakeExplosion();
			}
			return true;
		}

		protected void MakeExplosion()
		{
			Pawn casterPawn = CasterPawn;
			if (!casterPawn.Spawned)
			{
				return;
			}
			Thing targetThing = currentTarget.Thing;
			if (targetThing !=null &&IsTargetImmobile(currentTarget) && casterPawn.skills != null)
			{
				casterPawn.skills.Learn(SkillDefOf.Shooting, 250 * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
			}
			verbProperties = verbProps as VerbProperties_Gunlance;
			if (verbProperties != null)
			{
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
				explosion.PreStartExplosion(TeleUtils.circularSectorCellsStartedCaster(casterPawn.Position, casterPawn.Map, currentTarget.Cell, verbProperties.range, verbProperties.angle, false).ToList());
				explosion.StartExplosion(null, null);
			}
			if (casterPawn != null && !casterPawn.Dead && casterPawn.Spawned)
			{
				casterPawn.stances.StaggerFor(95);
			}
		}

		private bool IsTargetImmobile(LocalTargetInfo target)
		{
			Thing thing = target.Thing;
			Pawn pawn = thing as Pawn;
			return thing.def.category != ThingCategory.Pawn || pawn.Downed || pawn.GetPosture() > PawnPosture.Standing;
		}

	}
}
