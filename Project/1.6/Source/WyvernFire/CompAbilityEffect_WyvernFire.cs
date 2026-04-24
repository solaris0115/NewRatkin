using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
	public class CompAbilityEffect_WyvernFire : CompAbilityEffect
	{
		/// <summary>
		/// 동일 def 부착물이 여러 개 붙는 경우(GetAttachment는 첫 개만 반환)까지 제거.
		/// 워밍업 취소·재시전 시 잔류 방지.
		/// </summary>
		public static void DestroyAllGunlancePreIgnitionOn(Pawn pawn)
		{
			if (pawn == null || !pawn.Spawned || GunlanceDefOf.GunlancePreIgnition == null)
			{
				return;
			}
			CompAttachBase compAttach = pawn.TryGetComp<CompAttachBase>();
			if (compAttach?.attachments == null)
			{
				return;
			}
			ThingDef def = GunlanceDefOf.GunlancePreIgnition;
			for (int i = compAttach.attachments.Count - 1; i >= 0; i--)
			{
				AttachableThing t = compAttach.attachments[i];
				if (t != null && !t.Destroyed && t.def == def)
				{
					t.Destroy();
				}
			}
		}

		private readonly List<IntVec3> tmpCells = new List<IntVec3>();

		private new CompProperties_AbilityWyvernFire Props
		{
			get
			{
				return (CompProperties_AbilityWyvernFire)this.props;
			}
		}

		/// <summary>
		/// meleeCooldownTime 값을 반환 (Ability_WyvernFire에서 접근용)
		/// </summary>
		public float GetMeleeCooldownTime()
		{
			return this.Props.meleeCooldownTime;
		}

		/// <summary>
		/// cooldownEndSound 값을 반환 (Ability_WyvernFire에서 접근용)
		/// </summary>
		public SoundDef GetCooldownEndSound()
		{
			return this.Props.cooldownEndSound;
		}

		private Pawn Pawn
		{
			get
			{
				return this.parent.pawn;
			}
		}

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			Pawn pawn = this.Pawn;

			// 발사 전 효과(PreIgnition) 제거
			if (pawn != null && pawn.Spawned)
			{
				DestroyAllGunlancePreIgnitionOn(pawn);

				// AfterIgnition 스폰 제거 (발사시에는 표시하지 않음)
				// AttachableThing_AfterIgnition afterIgnition = ThingMaker.MakeThing(GunlanceDefOf.GunlanceAfterIgnition, null) as AttachableThing_AfterIgnition;
				// if (afterIgnition != null)
				// {
				// 	afterIgnition.AttachTo(pawn);
				// 	GenSpawn.Spawn(afterIgnition, pawn.Position, pawn.Map, pawn.Rotation, WipeMode.Vanish, false);
				// }
			}

			IntVec3 cell = target.Cell;
			Map mapHeld = this.parent.pawn.MapHeld;
			float radius = 0f;

			// Use configured damageDef instead of hardcoded Flame
			DamageDef damageDef = this.Props.damageDef ?? DamageDefOf.Bomb;

			Thing pawnThing = pawn;
			int damAmount = this.Props.damAmount;
			if (damAmount == -1)
			{
				damAmount = damageDef.defaultDamage;
			}

			float armorPenetration = this.Props.armorPenetration;
			if (Mathf.Approximately(armorPenetration, -1f))
			{
				armorPenetration = damageDef.defaultArmorPenetration;
			}

			SoundDef explosionSound = null;
			ThingDef weapon = null;
			ThingDef projectile = null;
			Thing intendedTarget = null;
			ThingDef postExplosionSpawnThingDef = null;
			float postExplosionSpawnChance = 0f;
			int postExplosionSpawnThingCount = 0;
			SimpleCurve flammabilityAttachFireChanceCurve = null;
			List<IntVec3> overrideCells = this.AffectedCells(target);

			GenExplosion.DoExplosion(
				cell, mapHeld, radius, damageDef, pawnThing,
				damAmount, armorPenetration, explosionSound, weapon, projectile,
				intendedTarget, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount,
				null, null, 255, false, null, 0f, 1, 1f, false,
				null, null, null, false, 0f, 0f, false,
				null, 1f, flammabilityAttachFireChanceCurve, overrideCells, null, null);

			// Anomaly 없음: 1.5 건랜스와 동일하게 GunlanceExplosion이 셀 목록을 틱마다 밟으며 폭발 텍스처(피해는 위에서 이미 적용, 여기서 damAmount 0)
			if (!ModsConfig.AnomalyActive && pawn != null && pawn.Spawned && overrideCells != null && overrideCells.Count > 0)
			{
				this.TrySpawnGunlanceStyleVisualCone(pawn, mapHeld, damageDef, pawn.equipment?.Primary?.def, overrideCells);
			}

			base.Apply(target, dest);

			// WyvernFire 발사 후 후딜레이(cooldown) 적용
			// VerbTick에서 BurstingTick이 호출되어 state가 Idle로 변경된 후에 설정되도록
			// Ability_WyvernFire의 AbilityTick에서 처리하도록 플래그 설정
			Ability_WyvernFire ability = this.parent as Ability_WyvernFire;
			if (ability != null)
			{
				ability.SetShouldApplyCooldown(true);
			}
		}

		public override IEnumerable<PreCastAction> GetPreCastActions()
		{
			// 발사 전 충전 효과 (PreIgnition) — 워밍업 첫 틱에 실행되려면 ticksAwayFromCast가
			// Verb.TryStartCastOn과 동일한 초기 ticksLeft와 맞아야 함 (AimingDelayFactor·SecondsToTicks).
			// Round(warmupTime*60)만 쓰면 스탠스보다 짧아져 첫 틱에 조건이 false → 1틱 이상 늦게 붙음.
			Pawn caster = this.Pawn;
			float aimDelay = caster != null
				? caster.GetStatValue(StatDefOf.AimingDelayFactor, true, -1)
				: 1f;
			int warmupTicks = (this.parent.verb.WarmupTime * aimDelay).SecondsToTicks();
			yield return new PreCastAction
			{
				action = delegate (LocalTargetInfo a, LocalTargetInfo b)
				{
					Pawn pawn = this.Pawn;
					if (pawn != null && pawn.Spawned)
					{
						DestroyAllGunlancePreIgnitionOn(pawn);

						// 새로운 PreIgnition 스폰
						AttachableThing_GunlanceIgnition ignition = ThingMaker.MakeThing(GunlanceDefOf.GunlancePreIgnition, null) as AttachableThing_GunlanceIgnition;
						if (ignition != null)
						{
							ignition.AttachTo(pawn);
							GenSpawn.Spawn(ignition, pawn.Position, pawn.Map, pawn.Rotation, WipeMode.Vanish, false);
						}
					}
				},
				ticksAwayFromCast = warmupTicks
			};

			if (this.Props.effecterDef != null)
			{
				yield return new PreCastAction
				{
					action = delegate (LocalTargetInfo a, LocalTargetInfo b)
					{
						this.parent.AddEffecterToMaintain(this.Props.effecterDef.Spawn(this.parent.pawn.Position, a.Cell, this.parent.pawn.Map, 1f), this.Pawn.Position, a.Cell, 17, this.Pawn.MapHeld);
					},
					ticksAwayFromCast = 17
				};
			}
			yield break;
		}

		public override void DrawEffectPreview(LocalTargetInfo target)
		{
			GenDraw.DrawFieldEdges(this.AffectedCells(target), 2900);
		}

		public override bool AICanTargetNow(LocalTargetInfo target)
		{
			if (this.Pawn.Faction != null)
			{
				foreach (IntVec3 c in this.AffectedCells(target))
				{
					List<Thing> thingList = c.GetThingList(this.Pawn.Map);
					for (int i = 0; i < thingList.Count; i++)
					{
						if (thingList[i].Faction == this.Pawn.Faction)
						{
							return false;
						}
					}
				}
				return true;
			}
			return true;
		}

		private List<IntVec3> AffectedCells(LocalTargetInfo target)
		{
			this.tmpCells.Clear();
			Vector3 b = this.Pawn.Position.ToVector3Shifted().Yto0();
			IntVec3 intVec = target.Cell.ClampInsideMap(this.Pawn.Map);
			if (this.Pawn.Position == intVec)
			{
				return this.tmpCells;
			}
			float lengthHorizontal = (intVec - this.Pawn.Position).LengthHorizontal;
			float num = (float)(intVec.x - this.Pawn.Position.x) / lengthHorizontal;
			float num2 = (float)(intVec.z - this.Pawn.Position.z) / lengthHorizontal;
			intVec.x = Mathf.RoundToInt((float)this.Pawn.Position.x + num * this.Props.range);
			intVec.z = Mathf.RoundToInt((float)this.Pawn.Position.z + num2 * this.Props.range);
			float target2 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - b, Vector3.right, Vector3.up);
			float num3 = this.Props.lineWidthEnd / 2f;
			float num4 = Mathf.Sqrt(Mathf.Pow((intVec - this.Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num3, 2f));
			float num5 = 57.29578f * Mathf.Asin(num3 / num4);
			int num6 = GenRadial.NumCellsInRadius(this.Props.range);
			for (int i = 0; i < num6; i++)
			{
				IntVec3 intVec2 = this.Pawn.Position + GenRadial.RadialPattern[i];
				if (this.CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - b, Vector3.right, Vector3.up), target2)) <= num5)
				{
					this.tmpCells.Add(intVec2);
				}
			}
			List<IntVec3> list = GenSight.BresenhamCellsBetween(this.Pawn.Position, intVec);
			for (int j = 0; j < list.Count; j++)
			{
				IntVec3 intVec3 = list[j];
				if (!this.tmpCells.Contains(intVec3) && this.CanUseCell(intVec3))
				{
					this.tmpCells.Add(intVec3);
				}
			}
			return this.tmpCells;
		}

		private bool CanUseCell(IntVec3 c)
		{
			ShootLine shootLine;
			return c.InBounds(this.Pawn.Map) &&
				!(c == this.Pawn.Position) &&
				(this.Props.canHitFilledCells || !c.Filled(this.Pawn.Map)) &&
				c.InHorDistOf(this.Pawn.Position, this.Props.range) &&
				this.parent.verb.TryFindShootLineFromTo(this.parent.pawn.Position, c, out shootLine, false);
		}

		/// <summary>
		/// 1.5 GunlanceExplosion과 같이 overrideCells를 틱마다 처리하는 코어 Explosion. 피해는 0(연출만).
		/// </summary>
		private void TrySpawnGunlanceStyleVisualCone(Pawn pawn, Map map, DamageDef damageDef, ThingDef weaponDef, List<IntVec3> coneCells)
		{
			List<IntVec3> cellCopy = new List<IntVec3>(coneCells);
			Explosion explosion = GenSpawn.Spawn(ThingDefOf.Explosion, pawn.Position, map, WipeMode.Vanish) as Explosion;
			if (explosion == null)
			{
				return;
			}
			explosion.radius = this.Props.range;
			explosion.damType = damageDef;
			explosion.damAmount = 0;
			explosion.armorPenetration = 0f;
			explosion.instigator = pawn;
			explosion.weapon = weaponDef;
			explosion.projectile = null;
			explosion.intendedTarget = null;
			explosion.preExplosionSpawnThingDef = null;
			explosion.preExplosionSpawnChance = 0f;
			explosion.preExplosionSpawnThingCount = 1;
			explosion.postExplosionSpawnThingDef = null;
			explosion.postExplosionSpawnChance = 0f;
			explosion.postExplosionSpawnThingCount = 1;
			explosion.postExplosionGasType = null;
			explosion.postExplosionGasRadiusOverride = null;
			explosion.postExplosionGasAmount = 255;
			explosion.applyDamageToExplosionCellsNeighbors = false;
			explosion.chanceToStartFire = 0f;
			explosion.damageFalloff = false;
			explosion.needLOSToCell1 = null;
			explosion.needLOSToCell2 = null;
			explosion.excludeRadius = 0f;
			explosion.affectedAngle = null;
			explosion.doVisualEffects = true;
			explosion.propagationSpeed = 1f;
			explosion.doSoundEffects = false;
			explosion.screenShakeFactor = 0f;
			explosion.flammabilityChanceCurve = null;
			explosion.overrideCells = cellCopy;
			explosion.postExplosionSpawnSingleThingDef = null;
			explosion.preExplosionSpawnSingleThingDef = null;
			explosion.StartExplosion(null, null);
		}
	}
}
