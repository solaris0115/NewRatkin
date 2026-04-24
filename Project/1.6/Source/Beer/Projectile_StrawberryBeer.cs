using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 맥주 프로젝타일 - 폭발 시 범위 내 Pawn에게 AlcoholHigh Hediff 부여
	/// </summary>
	public class Projectile_StrawberryBeer : Projectile_Explosive
	{
		protected override void Explode()
		{
		// 폭발 범위 내의 모든 Pawn에게 AlcoholHigh Hediff 부여 (base.Explode() 전에 정보 저장)
		Map map = base.Map;
		if (map != null)
		{
			float explosionRadius = this.def.projectile.explosionRadius;
			IntVec3 center = base.Position;

			// 범위 내의 모든 Pawn 찾기
			List<Pawn> affectedPawns = new List<Pawn>();
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, explosionRadius, true))
			{
				if (!cell.InBounds(map))
				{
					continue;
				}

				List<Thing> things = cell.GetThingList(map);
				foreach (Thing thing in things)
				{
					Pawn pawn = thing as Pawn;
					if (pawn != null && pawn.RaceProps.Humanlike && !pawn.Dead && !affectedPawns.Contains(pawn))
					{
						affectedPawns.Add(pawn);
					}
				}
			}

			// 각 Pawn에게 AlcoholHigh Hediff 부여 (약하게)
			HediffDef alcoholHighDef = HediffDefOf.AlcoholHigh;
			if (alcoholHighDef != null)
			{
				foreach (Pawn pawn in affectedPawns)
				{
					if (pawn.health != null && pawn.health.hediffSet != null)
					{
						Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(alcoholHighDef, false);
						if (existingHediff != null)
						{
							// 이미 있으면 약간 증가 (상한 제한 없음)
							existingHediff.Severity += 0.05f;
						}
						else
						{
							// 없으면 새로 추가 (약한 수준)
							Hediff newHediff = HediffMaker.MakeHediff(alcoholHighDef, pawn);
							newHediff.Severity = 0.05f;
							pawn.health.AddHediff(newHediff);
						}
					}
				}
			}
		}

		// 기본 폭발 처리 (화면 진동 없이)
		Map map2 = base.Map;
		if (map2 != null)
		{
			IntVec3 position = base.Position;
			float explosionRadius2 = this.def.projectile.explosionRadius;
			Thing launcher = this.launcher;
			DamageDef damageDef = this.def.projectile.damageDef;
			int damageAmount = this.DamageAmount;
			float armorPenetration = this.ArmorPenetration;
			SoundDef soundExplode = this.def.projectile.soundExplode;
			ThingDef equipmentDef = this.equipmentDef;
			ThingDef def = this.def;
			Thing intendedTarget = this.intendedTarget.Thing;
			ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
			float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
			int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
			ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
			float preExplosionSpawnChance = this.def.projectile.preExplosionSpawnChance;
			int preExplosionSpawnThingCount = this.def.projectile.preExplosionSpawnThingCount;
			bool applyDamageToExplosionCellsNeighbors = this.def.projectile.applyDamageToExplosionCellsNeighbors;
			float chanceToStartFire = this.def.projectile.explosionChanceToStartFire;
			bool damageFalloff = this.def.projectile.explosionDamageFalloff;
			
			// 폭발 이펙트 처리
			if (this.def.projectile.explosionEffect != null)
			{
				Effecter effecter = this.def.projectile.explosionEffect.Spawn();
				effecter.Trigger(new TargetInfo(position, map2, false), new TargetInfo(position, map2, false));
				effecter.Cleanup();
			}
			
			// GenExplosion.DoExplosion 호출 (화면 진동 없음: screenShakeFactor = 0f)
			GenExplosion.DoExplosion(
				position,
				map2,
				explosionRadius2,
				damageDef,
				launcher,
				damageAmount,
				armorPenetration,
				soundExplode,
				equipmentDef,
				def,
				intendedTarget,
				postExplosionSpawnThingDef,
				postExplosionSpawnChance,
				postExplosionSpawnThingCount,
				null,
				null,
				255,
				applyDamageToExplosionCellsNeighbors,
				preExplosionSpawnThingDef,
				preExplosionSpawnChance,
				preExplosionSpawnThingCount,
				chanceToStartFire,
				damageFalloff,
				null,
				null,
				null,
				true,
				1f,
				0f,
				true,
				null,
				0f,  // screenShakeFactor = 0f (화면 진동 없음)
				null,
				null,
				null,
				null);
		}
		
		// 프로젝타일 제거
		base.Destroy(DestroyMode.Vanish);
	}
	}
}

