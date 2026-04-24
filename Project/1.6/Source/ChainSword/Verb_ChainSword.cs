using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
	public class Verb_ChainSword : Verb_MeleeAttack
	{
		private VerbProperties_ChainSword ChainSwordProps
		{
			get
			{
				return this.verbProps as VerbProperties_ChainSword;
			}
		}

		protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
		{
			DamageWorker.DamageResult result = new DamageWorker.DamageResult();
			
			// 첫 번째 데미지 적용 (기본 meleeDamageDef) - 여러 부위에 분산되어 적용됨
			DamageWorker.DamageResult firstResult = this.ApplyFirstDamage(target);
			result = firstResult;

			// 첫 번째 데미지가 방어구에 의해 막히거나 deflect된 경우 두 번째 데미지 적용하지 않음
			if (firstResult.deflected || firstResult.totalDamageDealt <= 0f)
			{
				return result;
			}

			// 두 번째 데미지 적용 (VerbProperties에서 설정한 damageType2)
			// 첫 번째 데미지가 적용된 모든 부위에 두 번째 데미지도 적용
			if (this.ChainSwordProps != null && this.ChainSwordProps.damageType2 != null && this.ChainSwordProps.damageAmount2 > 0f)
			{
				// 첫 번째 데미지가 적용된 모든 부위에 두 번째 데미지 적용
				if (!firstResult.parts.NullOrEmpty())
				{
					foreach (BodyPartRecord part in firstResult.parts)
					{
						DamageWorker.DamageResult secondResult = this.ApplySecondDamage(target, part);
						// 결과 병합
						result.totalDamageDealt += secondResult.totalDamageDealt;
						result.wounded |= secondResult.wounded;
						result.headshot |= secondResult.headshot;
						result.deflected |= secondResult.deflected;
						result.diminished |= secondResult.diminished;
						result.stunned |= secondResult.stunned;
						// parts와 hediffs 병합
						if (!secondResult.parts.NullOrEmpty())
						{
							result.parts = result.parts ?? new List<BodyPartRecord>();
							result.parts.AddRange(secondResult.parts);
						}
						if (!secondResult.hediffs.NullOrEmpty())
						{
							result.hediffs = result.hediffs ?? new List<Hediff>();
							result.hediffs.AddRange(secondResult.hediffs);
						}
					}
				}
				else if (firstResult.LastHitPart != null)
				{
					// parts가 없으면 LastHitPart에만 적용
					DamageWorker.DamageResult secondResult = this.ApplySecondDamage(target, firstResult.LastHitPart);
					result.totalDamageDealt += secondResult.totalDamageDealt;
					result.wounded |= secondResult.wounded;
					result.headshot |= secondResult.headshot;
					result.deflected |= secondResult.deflected;
					result.diminished |= secondResult.diminished;
					result.stunned |= secondResult.stunned;
					if (!secondResult.parts.NullOrEmpty())
					{
						result.parts = result.parts ?? new List<BodyPartRecord>();
						result.parts.AddRange(secondResult.parts);
					}
					if (!secondResult.hediffs.NullOrEmpty())
					{
						result.hediffs = result.hediffs ?? new List<Hediff>();
						result.hediffs.AddRange(secondResult.hediffs);
					}
				}
			}

			return result;
		}

		private DamageWorker.DamageResult ApplyFirstDamage(LocalTargetInfo target)
		{
			DamageWorker.DamageResult result = new DamageWorker.DamageResult();
			float num = this.verbProps.AdjustedMeleeDamageAmount(this, this.CasterPawn);
			float armorPenetration = this.verbProps.AdjustedArmorPenetration(this, this.CasterPawn);
			DamageDef def = this.verbProps.meleeDamageDef;
			BodyPartGroupDef bodyPartGroupDef = null;
			HediffDef hediffDef = null;
			num = UnityEngine.Random.Range(num * 0.8f, num * 1.2f);
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
			
			result = target.Thing.TakeDamage(damageInfo);
			return result;
		}

		private DamageWorker.DamageResult ApplySecondDamage(LocalTargetInfo target, BodyPartRecord hitPart)
		{
			VerbProperties_ChainSword props = this.ChainSwordProps;
			if (props == null || props.damageType2 == null || props.damageAmount2 <= 0f)
			{
				return new DamageWorker.DamageResult();
			}

			Pawn targetPawn = target.Thing as Pawn;
			if (targetPawn == null || hitPart == null)
			{
				return new DamageWorker.DamageResult();
			}

			// 첫 번째 데미지가 적용된 부위에 두 번째 데미지도 적용

			// 두 번째 데미지 정보 생성
			float armorPen = props.armorPenetration2 > 0f ? props.armorPenetration2 : this.verbProps.AdjustedArmorPenetration(this, this.CasterPawn);
			ThingDef source = base.EquipmentSource != null ? base.EquipmentSource.def : this.CasterPawn.def;
			Vector3 direction = (target.Thing.Position - this.CasterPawn.Position).ToVector3();

			DamageInfo secondDamageInfo = new DamageInfo(
				props.damageType2,
				props.damageAmount2,
				armorPen,
				direction.AngleFlat(),
				this.caster,
				hitPart,
				source,
				DamageInfo.SourceCategory.ThingOrUnknown,
				target.Thing
			);
			secondDamageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
			secondDamageInfo.SetWeaponBodyPartGroup(this.verbProps.AdjustedLinkedBodyPartsGroup(this.tool));
			if (this.tool != null)
			{
				secondDamageInfo.SetTool(this.tool);
			}

			// 두 번째 데미지 적용
			DamageWorker damageWorker = props.damageType2.Worker;
			return damageWorker.Apply(secondDamageInfo, targetPawn);
		}
	}
}

