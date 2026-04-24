using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
	public class Verb_HalberdCleave : RimWorld.Verb_MeleeAttackDamage
	{
		private VerbProperties_HalberdCleave CleaveProps => verbProps as VerbProperties_HalberdCleave;

		protected override bool TryCastShot()
		{
			bool result = base.TryCastShot();
			if (result)
			{
				TryPerformCleave();
			}
			return result;
		}

		private void TryPerformCleave()
		{
			VerbProperties_HalberdCleave props = CleaveProps;
			if (props == null || props.cleaveChance <= 0f || props.maxCleaveTargets <= 0)
			{
				return;
			}

			if (!Rand.Chance(props.cleaveChance))
			{
				return;
			}

			Pawn caster = CasterPawn;
			Thing mainTarget = currentTarget.Thing;
			if (caster == null || !caster.Spawned || mainTarget == null)
			{
				return;
			}

			Map map = caster.Map;
			if (map == null)
			{
				return;
			}

			// 공격 직후 대상이 사망해 despawn될 수 있으므로 currentTarget.Cell 사용
			IntVec3 targetCell = currentTarget.Cell;
			if (!targetCell.InBounds(map))
			{
				return;
			}

			HashSet<IntVec3> attackerCells = GetCells9(caster.Position);
			HashSet<IntVec3> targetCells = GetCells9(targetCell);
			attackerCells.IntersectWith(targetCells);

			List<Pawn> cleaveCandidates = new List<Pawn>();
			foreach (IntVec3 cell in attackerCells)
			{
				if (!cell.InBounds(map))
				{
					continue;
				}

				foreach (Thing thing in map.thingGrid.ThingsAt(cell))
				{
					Pawn pawn = thing as Pawn;
					if (pawn == null || pawn.Dead || pawn == caster || pawn == mainTarget)
					{
						continue;
					}

					if (!pawn.HostileTo(caster))
					{
						continue;
					}

					cleaveCandidates.Add(pawn);
				}
			}

			cleaveCandidates = cleaveCandidates.Distinct().ToList();
			if (cleaveCandidates.Count == 0)
			{
				return;
			}

			int count = Mathf.Min(props.maxCleaveTargets, cleaveCandidates.Count);
			List<Pawn> cleaveTargets = cleaveCandidates.InRandomOrder().Take(count).ToList();

			float baseDamage = verbProps.AdjustedMeleeDamageAmount(this, caster);
			float cleaveDamage = baseDamage * props.cleaveDamageRatio;
			cleaveDamage = Rand.Range(cleaveDamage * 0.8f, cleaveDamage * 1.2f);
			float armorPen = verbProps.AdjustedArmorPenetration(this, caster);

			ThingDef source = EquipmentSource != null ? EquipmentSource.def : caster.def;
			QualityCategory weaponQuality = QualityCategory.Normal;
			if (EquipmentSource != null)
			{
				EquipmentSource.TryGetQuality(out weaponQuality);
			}
			DamageDef damageDef = verbProps.meleeDamageDef ?? DamageDefOf.Cut;
			BodyPartGroupDef bodyPartGroup = CasterIsPawn ? verbProps.AdjustedLinkedBodyPartsGroup(tool) : null;

			foreach (Pawn target in cleaveTargets)
			{
				if (target.Dead || !target.Spawned)
				{
					continue;
				}

				Vector3 direction = (target.Position - caster.Position).ToVector3();
				DamageInfo dinfo = new DamageInfo(
					damageDef,
					cleaveDamage,
					armorPen,
					-1f,
					caster,
					null,
					source,
					DamageInfo.SourceCategory.ThingOrUnknown,
					null,
					true,
					true,
					weaponQuality,
					true,
					false);
				dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
				dinfo.SetWeaponBodyPartGroup(bodyPartGroup);
				dinfo.SetAngle(direction);
				dinfo.SetTool(tool);
				dinfo.SetWeaponQuality(weaponQuality);

				target.TakeDamage(dinfo);

				SoundDef hitSound = target.def.race?.soundMeleeHitPawn ?? SoundDefOf.Pawn_Melee_Punch_HitPawn;
				if (hitSound != null)
				{
					hitSound.PlayOneShot(new TargetInfo(target.Position, map, false));
				}

				if (target.Spawned && !target.Dead)
				{
					target.stances.stagger.StaggerFor(95, 0.17f);
				}
			}
		}

		private static HashSet<IntVec3> GetCells9(IntVec3 center)
		{
			HashSet<IntVec3> cells = new HashSet<IntVec3>();
			for (int i = 0; i < 9; i++)
			{
				cells.Add(center + GenAdj.AdjacentCellsAndInside[i]);
			}
			return cells;
		}
	}
}
