using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin
{
	public class PawnFlyer_LanceCharge : PawnFlyer
	{
		private static readonly FieldInfo targetField =
			typeof(PawnFlyer).GetField("target", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly FieldInfo destCellField =
			typeof(PawnFlyer).GetField("destCell", BindingFlags.NonPublic | BindingFlags.Instance);

		private LocalTargetInfo chargeTarget;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad && targetField != null)
			{
				chargeTarget = (LocalTargetInfo)targetField.GetValue(this);
			}
		}

		protected override void TickInterval(int delta)
		{
			Pawn pawn = FlyingPawn;
			Map map = Map;

			if (pawn != null && map != null && chargeTarget.IsValid)
			{
				Vector3 targetPos = chargeTarget.HasThing
					? chargeTarget.Thing.DrawPos
					: chargeTarget.Cell.ToVector3Shifted();
				Vector3 direction = targetPos - DrawPos;
				if (direction.MagnitudeHorizontalSquared() > 0.001f)
				{
					pawn.Rotation = Pawn_RotationTracker.RotFromAngleBiased(direction.AngleFlat());
				}

				// 비행 중 매 틱 대상의 현재 위치로 착지 지점 갱신 (대상 추적)
				IntVec3 targetCell = chargeTarget.HasThing
					? chargeTarget.Thing.Position
					: chargeTarget.Cell;
				if (chargeTarget.HasThing && (chargeTarget.Thing.Destroyed || !chargeTarget.Thing.Spawned))
					targetCell = IntVec3.Invalid;

				if (targetCell.IsValid && destCellField != null)
				{
					IntVec3 newDest = FindLandingNearTarget(targetCell, pawn, map);
					if (newDest.IsValid)
						destCellField.SetValue(this, newDest);
				}
			}

			base.TickInterval(delta);
		}

		/// <summary>
		/// targetCell 주변 8셀 중 유효한 착지 셀. 현재 비행 위치에 가장 가까운 셀 선택.
		/// </summary>
		private IntVec3 FindLandingNearTarget(IntVec3 targetCell, Pawn flyingPawn, Map map)
		{
			Vector3 currentPos = DrawPos;
			IntVec3 best = IntVec3.Invalid;
			float bestDistSq = float.MaxValue;
			for (int i = 0; i < GenAdj.AdjacentCells.Length; i++)
			{
				IntVec3 cell = targetCell + GenAdj.AdjacentCells[i];
				if (!JumpUtility.ValidJumpTarget(flyingPawn, map, cell))
					continue;
				float d = (cell.ToVector3Shifted() - currentPos).MagnitudeHorizontalSquared();
				if (d < bestDistSq)
				{
					bestDistSq = d;
					best = cell;
				}
			}
			return best;
		}

		protected override void RespawnPawn()
		{
			Pawn pawn = FlyingPawn;
			LocalTargetInfo savedTarget = chargeTarget;

			if (savedTarget.IsValid)
			{
				Vector3 targetPos = savedTarget.HasThing
					? savedTarget.Thing.DrawPos
					: savedTarget.Cell.ToVector3Shifted();
				Vector3 dir = targetPos - DestinationPos;
				if (dir.MagnitudeHorizontalSquared() > 0.001f)
				{
					base.Rotation = Pawn_RotationTracker.RotFromAngleBiased(dir.AngleFlat());
				}
			}

			base.RespawnPawn();

			if (pawn != null && pawn.Spawned && savedTarget.IsValid)
			{
				pawn.stances.SetStance(new Stance_Cooldown(30, savedTarget, null));
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_TargetInfo.Look(ref chargeTarget, "chargeTarget");
		}
	}
}
