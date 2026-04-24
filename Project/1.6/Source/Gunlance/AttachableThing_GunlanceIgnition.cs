using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
	/// <summary>
	/// positionOffset XZ는 조준 방향 기준(수평면에서 angle만큼 회전), Y는 월드 상승.
	/// </summary>
	internal static class GunlanceIgnitionOffsets
	{
		internal static Vector3 RotatedPositionOffset(Vector3 positionOffset, float aimAngleDegrees)
		{
			Quaternion q = Quaternion.AngleAxis(aimAngleDegrees, Vector3.up);
			Vector3 xz = q * new Vector3(positionOffset.x, 0f, positionOffset.z);
			return xz + new Vector3(0f, positionOffset.y, 0f);
		}
	}

	[StaticConstructorOnStartup]
	public class AttachableThing_GunlanceIgnition : AttachableThing
	{
		private Pawn parentPawn;
		private float currentPower = 0;
		private Graphic currentGraphic;
		private CompAttachableIgnition compIgnition;
		private CompProperties_AttachableIgnition ignitionProps;

		public static Graphic[] graphics = new Graphic[] { GraphicDatabase.Get<Graphic_Single>("Things/Special/PreIgnitionA"), GraphicDatabase.Get<Graphic_Single>("Things/Special/PreIgnitionB") };
		private bool swap = false;

		private CompProperties_AttachableIgnition Props
		{
			get
			{
				if (this.ignitionProps == null)
				{
					this.CacheIgnitionProps();
				}
				return this.ignitionProps;
			}
		}

		private Vector3 PositionOffset
		{
			get
			{
				return Props.positionOffset;
			}
		}

		private void CacheIgnitionProps()
		{
			this.compIgnition = this.TryGetComp<CompAttachableIgnition>();
			if (this.compIgnition != null)
			{
				this.ignitionProps = this.compIgnition.Props;
				return;
			}
			this.ignitionProps = null;
			if (this.def != null && this.def.comps != null)
			{
				for (int i = 0; i < this.def.comps.Count; i++)
				{
					if (this.def.comps[i] is CompProperties_AttachableIgnition p)
					{
						this.ignitionProps = p;
						return;
					}
				}
			}
			if (this.ignitionProps == null)
			{
				this.ignitionProps = new CompProperties_AttachableIgnition();
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			parentPawn = parent as Pawn;
			this.CacheIgnitionProps();
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref currentPower, "currentPower");
			Scribe_Values.Look(ref swap, "swap");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (swap)
				{
					currentGraphic = graphics[0];
				}
				else
				{
					currentGraphic = graphics[1];
				}
				parentPawn = parent as Pawn;
				this.CacheIgnitionProps();
			}
		}
		protected override void Tick()
		{
			if (currentPower < Props.maxPower)
			{
				currentPower += Props.powerIncreasePerTick;
			}
			swap = !swap;
		}
		public Color FireColor
		{
			get
			{
				if (currentPower < 0.5f)
				{
					return new Color(1, 0.5f + currentPower, 0);
				}
				return new Color(1.75f - currentPower, 1.35f - currentPower * 0.5f, currentPower * 2 - 0.5f);
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			Vector3 targetVector;
			if (parentPawn != null && parentPawn.stances != null && parentPawn.stances.curStance != null)
			{
				Stance_Busy stance_Busy = parentPawn.stances.curStance as Stance_Busy;
				if (stance_Busy != null)
				{
					if (stance_Busy.focusTarg.HasThing)
					{
						targetVector = stance_Busy.focusTarg.Thing.DrawPos;
					}
					else
					{
						targetVector = stance_Busy.focusTarg.Cell.ToVector3Shifted();
					}
					if (swap)
					{
						currentGraphic = graphics[0];
					}
					else
					{
						currentGraphic = graphics[1];
					}
					currentGraphic.MatSingle.color = FireColor;
					float angle = (targetVector - parent.TrueCenter()).AngleFlat();

					// Mesh 크기 계산 (외부 크기 배율 적용)
					float baseMeshWidth = Props.preIgnitionBaseSize - Mathf.Clamp(currentPower * Props.preIgnitionSizeMultiplier, 0, Props.preIgnitionMaxSize);
					float meshWidth = baseMeshWidth * Props.preIgnitionSizeScale;
					float meshHeight = Props.preIgnitionHeightBase - currentPower * Props.preIgnitionHeightReduction;

					// 방향 오프셋 계산
					float offsetDistance = Props.directionOffsetDistance;
					Vector3 directionOffset = new Vector3(
						Mathf.Sin(angle * Mathf.Deg2Rad) * offsetDistance,
						1f,
						Mathf.Cos(angle * Mathf.Deg2Rad) * offsetDistance
					);

					Vector3 finalPosition = parent.TrueCenter() + GunlanceIgnitionOffsets.RotatedPositionOffset(PositionOffset, angle) + directionOffset;
					Graphics.DrawMesh(
						MeshPool.GridPlane(new Vector2(meshWidth, meshHeight)),
						finalPosition,
						Quaternion.AngleAxis(angle, Vector3.up),
						currentGraphic.MatSingle,
						0
					);
				}
			}

		}
		public override string InspectStringAddon
		{
			get
			{
				return null;
			}
		}
	}

	public class AttachableThing_AfterIgnition : AttachableThing
	{
		private Pawn parentPawn;
		private float currentPower = 1f;
		private CompAttachableIgnition compIgnition;
		private CompProperties_AttachableIgnition ignitionProps;

		private CompProperties_AttachableIgnition Props
		{
			get
			{
				if (this.ignitionProps == null)
				{
					this.CacheIgnitionProps();
				}
				return this.ignitionProps;
			}
		}

		private Vector3 PositionOffset
		{
			get
			{
				return Props.positionOffset;
			}
		}

		private void CacheIgnitionProps()
		{
			this.compIgnition = this.TryGetComp<CompAttachableIgnition>();
			if (this.compIgnition != null)
			{
				this.ignitionProps = this.compIgnition.Props;
				return;
			}
			this.ignitionProps = null;
			if (this.def != null && this.def.comps != null)
			{
				for (int i = 0; i < this.def.comps.Count; i++)
				{
					if (this.def.comps[i] is CompProperties_AttachableIgnition p)
					{
						this.ignitionProps = p;
						return;
					}
				}
			}
			if (this.ignitionProps == null)
			{
				this.ignitionProps = new CompProperties_AttachableIgnition();
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			parentPawn = parent as Pawn;
			this.CacheIgnitionProps();
			currentPower = this.ignitionProps.initialPower;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref currentPower, "currentPower");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				parentPawn = parent as Pawn;
				this.CacheIgnitionProps();
			}
		}
		protected override void Tick()
		{
			if (currentPower > 0)
			{
				currentPower -= Props.powerDecreasePerTick;
			}
			else
			{
				Destroy();
			}
		}
		public Color FireColor
		{
			get
			{
				Color baseColor = Props.afterIgnitionBaseColor;
				return new Color(baseColor.r, baseColor.g, baseColor.b, currentPower * Props.afterIgnitionAlphaMultiplier);
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			Vector3 targetVector;
			if (parentPawn != null && parentPawn.stances != null && parentPawn.stances.curStance != null)
			{
				Stance_Busy stance_Busy = parentPawn.stances.curStance as Stance_Busy;
				if (stance_Busy != null)
				{
					if (stance_Busy.focusTarg.HasThing)
					{
						targetVector = stance_Busy.focusTarg.Thing.DrawPos;
					}
					else
					{
						targetVector = stance_Busy.focusTarg.Cell.ToVector3Shifted();
					}
					Graphic.MatSingle.color = FireColor;
					float angle = (targetVector - parent.TrueCenter()).AngleFlat();

					// Mesh 크기 계산 (외부 크기 배율 적용)
					float baseMeshWidth = currentPower * Props.afterIgnitionSizeMultiplier;
					float meshWidth = baseMeshWidth * Props.afterIgnitionSizeScale;
					float meshHeight = Mathf.Clamp01(currentPower * Props.afterIgnitionHeightMultiplier);

					// 방향 오프셋 계산
					float offsetDistance = Props.directionOffsetDistance;
					Vector3 directionOffset = new Vector3(
						Mathf.Sin(angle * Mathf.Deg2Rad) * offsetDistance,
						1f,
						Mathf.Cos(angle * Mathf.Deg2Rad) * offsetDistance
					);

					Vector3 finalPosition = parent.TrueCenter() + GunlanceIgnitionOffsets.RotatedPositionOffset(PositionOffset, angle) + directionOffset;
					Graphics.DrawMesh(
						MeshPool.GridPlane(new Vector2(meshWidth, meshHeight)),
						finalPosition,
						Quaternion.AngleAxis(angle, Vector3.up),
						Graphic.MatSingle,
						0
					);
				}
			}

		}
		public override string InspectStringAddon
		{
			get
			{
				return null;
			}
		}
	}

}
