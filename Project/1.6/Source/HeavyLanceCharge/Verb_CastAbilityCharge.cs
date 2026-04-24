using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
	/// <summary>
	/// 헤비랜스 돌진: LOS 기반 목적지, 최소/최대 거리(verb minRange~EffectiveRange).
	/// 직선 경로 상 시야 차단 또는 경로상 적 직전까지 이동.
	/// </summary>
	public class Verb_CastAbilityCharge : Verb_CastAbilityJump
	{
		/// <summary>
		/// 무기에 JumpRange 스탯이 없으면 verbProps.range 사용.
		/// </summary>
		public override float EffectiveRange
		{
			get
			{
				float r = base.EffectiveRange;
				if (r <= 0f)
					return verbProps.range;
				return r;
			}
		}

		/// <summary>
		/// 최소 거리(verb minRange) 미만이면 타겟 불가.
		/// </summary>
		public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
		{
			float minR = verbProps.minRange;
			if (minR > 0f)
			{
				float distSq = (float)root.DistanceToSquared(targ.Cell);
				float minRangeSq = minR * minR;
				if (distSq < minRangeSq)
					return false;
			}

			if (targ.Thing != null && targ.Thing == caster)
				return targetParams.canTargetSelf;
			if (targ.Pawn != null && targ.Pawn.IsPsychologicallyInvisible() && caster.HostileTo(targ.Pawn))
				return false;
			if (ApparelPreventsShooting())
				return false;
			return JumpUtility.CanHitTargetFrom(CasterPawn, root, targ, EffectiveRange);
		}

		/// <summary>
		/// LOS 기반 목적지: caster→target 직선 경로 상, LOS 차단 또는 경로 상 Pawn 직전까지 돌진.
		/// 경로에 적대 Pawn이 있으면 그 Pawn을 effectiveTarget으로 변경하여 멈춤.
		/// 최대 EffectiveRange 이내로 제한. 이동 중 대상은 직전 시점 위치 기준.
		/// </summary>
		protected virtual LocalTargetInfo ResolveChargeDestination(out LocalTargetInfo effectiveTarget)
		{
			effectiveTarget = currentTarget;
			LocalTargetInfo targ = currentTarget;
			if (!targ.IsValid || CasterPawn?.Map == null)
				return LocalTargetInfo.Invalid;

			if (targ.Pawn != null && targ.Pawn.pather.MovingNow)
			{
				effectiveTarget = targ;
				LocalTargetInfo dest = FindAdjacentLanding(targ.Cell);
				return dest.IsValid ? dest : new LocalTargetInfo(targ.Cell);
			}

			IntVec3 targetCell = targ.Cell;
			IntVec3 casterPos = CasterPawn.Position;
			Map map = CasterPawn.Map;

			IntVec3 lastValid = IntVec3.Invalid;
			float maxRangeSq = EffectiveRange * EffectiveRange;

			foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(casterPos, targetCell))
			{
				if (cell == casterPos)
					continue;
				if ((float)casterPos.DistanceToSquared(cell) > maxRangeSq)
					break;
				if (cell == targetCell)
					break;

				Pawn pathPawn = cell.GetFirstPawn(map);
				if (pathPawn != null && pathPawn != targ.Pawn && IsValidChargeTarget(pathPawn))
				{
					effectiveTarget = new LocalTargetInfo(pathPawn);
					if (lastValid.IsValid)
						return new LocalTargetInfo(lastValid);
					return FindAdjacentLanding(pathPawn.Position);
				}

				if (!cell.CanBeSeenOverFast(map))
					break;
				if (JumpUtility.ValidJumpTarget(CasterPawn, map, cell))
					lastValid = cell;
			}

			if (lastValid.IsValid)
				return new LocalTargetInfo(lastValid);

			return ResolveChargeDestinationFallback(out effectiveTarget);
		}

		/// <summary>
		/// LOS 경로로 착지를 못 찾을 때: 대상 주변 8셀 중 가장 가까운 유효 셀 등(구버전 보조).
		/// </summary>
		private LocalTargetInfo ResolveChargeDestinationFallback(out LocalTargetInfo effectiveTarget)
		{
			effectiveTarget = currentTarget;
			LocalTargetInfo targ = currentTarget;
			if (!targ.IsValid || CasterPawn?.Map == null)
				return LocalTargetInfo.Invalid;

			IntVec3 targetCell = targ.Cell;
			IntVec3 casterPos = CasterPawn.Position;
			Map map = CasterPawn.Map;
			IntVec3 best = IntVec3.Invalid;
			int bestDistSq = int.MaxValue;

			for (int i = 0; i < GenAdj.AdjacentCells.Length; i++)
			{
				IntVec3 cell = targetCell + GenAdj.AdjacentCells[i];
				if (!JumpUtility.ValidJumpTarget(CasterPawn, map, cell))
					continue;
				int d = cell.DistanceToSquared(casterPos);
				if (d < bestDistSq)
				{
					bestDistSq = d;
					best = cell;
				}
			}

			if (best.IsValid)
				return new LocalTargetInfo(best);
			return new LocalTargetInfo(targetCell);
		}

		private LocalTargetInfo FindAdjacentLanding(IntVec3 targetCell)
		{
			IntVec3 casterPos = CasterPawn.Position;
			Map map = CasterPawn.Map;
			IntVec3 best = IntVec3.Invalid;
			int bestDistSq = int.MaxValue;
			for (int i = 0; i < GenAdj.AdjacentCells.Length; i++)
			{
				IntVec3 cell = targetCell + GenAdj.AdjacentCells[i];
				if (!JumpUtility.ValidJumpTarget(CasterPawn, map, cell))
					continue;
				int d = cell.DistanceToSquared(casterPos);
				if (d < bestDistSq)
				{
					bestDistSq = d;
					best = cell;
				}
			}
			return best.IsValid ? new LocalTargetInfo(best) : LocalTargetInfo.Invalid;
		}

		private bool IsValidChargeTarget(Pawn p)
		{
			if (p == null || p.Destroyed || !p.Spawned)
				return false;
			CompAbilityEffect_ChargeOnJump chargeComp = ability?.comps?.OfType<CompAbilityEffect_ChargeOnJump>().FirstOrDefault();
			CompProperties_ChargeOnJump props = chargeComp?.props as CompProperties_ChargeOnJump;
			if (props != null && props.onlyHostilePawns && !p.HostileTo(CasterPawn))
				return false;
			return true;
		}

		protected override bool TryCastShot()
		{
			if (!CanHitTarget(currentTarget))
				return false;

			CompAbilityEffect_ChargeOnJump chargeComp = ability?.comps?.OfType<CompAbilityEffect_ChargeOnJump>().FirstOrDefault();
			chargeComp?.ApplyHediffsImmediately(CasterPawn);

			LocalTargetInfo effectiveTarget;
			LocalTargetInfo dest = ResolveChargeDestination(out effectiveTarget);
			if (!dest.IsValid)
				return false;

			if (verbProps.soundCast != null && CasterPawn?.MapHeld != null)
				verbProps.soundCast.PlayOneShot(new TargetInfo(CasterPawn.Position, CasterPawn.MapHeld, false));

			return (ability?.Activate(effectiveTarget, currentDestination) ?? false)
				&& JumpUtility.DoJump(CasterPawn, dest, ReloadableCompSource, verbProps, ability, effectiveTarget, JumpFlyerDef);
		}

		public override ThingDef JumpFlyerDef
		{
			get
			{
				CompAbilityEffect_ChargeOnJump chargeComp = ability?.comps?.OfType<CompAbilityEffect_ChargeOnJump>().FirstOrDefault();
				CompProperties_ChargeOnJump props = chargeComp?.props as CompProperties_ChargeOnJump;
				return props?.pawnFlyerDef ?? ThingDefOf.PawnFlyer;
			}
		}

		public override void OrderForceTarget(LocalTargetInfo target)
		{
			if (ability != null && target.IsValid)
			{
				ability.QueueCastingJob(target, null);
				return;
			}
			base.OrderForceTarget(target);
		}

		public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
		{
			if (!base.ValidateTarget(target, showMessages))
				return false;
			if (ability?.EffectComps == null)
				return true;
			for (int i = 0; i < ability.EffectComps.Count; i++)
			{
				if (!ability.EffectComps[i].Valid(target, showMessages))
					return false;
			}
			return true;
		}
	}
}
