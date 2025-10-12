using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace NewRatkin
{
	[StaticConstructorOnStartup]
	public class AttachableThing_GunlanceIgnition : AttachableThing
	{
		Pawn parentPawn;
		public float currentPower = 0;
		public readonly Vector3 posFix = new Vector3(0, 0, -0.2f);
		public Graphic currentGraphic;

		public static Graphic[] graphics = new Graphic[] { GraphicDatabase.Get<Graphic_Single>("Things/Special/PreIgnitionA"), GraphicDatabase.Get<Graphic_Single>("Things/Special/PreIgnitionB") };
		public bool swap=false;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			parentPawn = parent as Pawn;
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
			}
		}
		public override void Tick()
		{
			if(currentPower<1)
			{
				currentPower +=0.02f;
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
				return new Color(1.75f - currentPower, 1.35f - currentPower*0.5f, currentPower*2-0.5f);
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			Vector3 targetVector;
			if(parentPawn!=null&& parentPawn.stances!=null && parentPawn.stances.curStance!=null)
			{
				Stance_Busy stance_Busy = parentPawn.stances.curStance as Stance_Busy;
				if (stance_Busy!=null)
				{
					if(stance_Busy.focusTarg.HasThing)
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
					Graphics.DrawMesh(MeshPool.GridPlane(new Vector2(3 - Mathf.Clamp(currentPower*4,0,2.5f), 1.1f - currentPower * 0.2f)), parent.TrueCenter()+ posFix + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 1.1f, 1, Mathf.Cos(angle * Mathf.Deg2Rad)) * 1.1f, Quaternion.AngleAxis(angle, Vector3.up), currentGraphic.MatSingle, 0);
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
		Pawn parentPawn;
		public float currentPower = 1;
		public readonly Vector3 posFix = new Vector3(0, 0, -0.2f);

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			parentPawn = parent as Pawn;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref currentPower, "currentPower");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				parentPawn = parent as Pawn;
			}
		}
		public override void Tick()
		{
			if (currentPower > 0)
			{
				currentPower -= 0.1f;
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
				return new Color(1, 0.75f,0.75f, currentPower*1.25f);
				/*if (currentPower > 0.5f)
				{
					return new Color(1, currentPower-0.3f, currentPower - 0.25f);
				}
				return new Color(1.75f - currentPower, 1.35f - currentPower * 0.5f, 0.25f currentPower * 2 - 0.5f);*/
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
					Graphics.DrawMesh(MeshPool.GridPlane(new Vector2(currentPower*4f, Mathf.Clamp01(currentPower*2f))), parent.TrueCenter() + posFix + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 1.1f, 1, Mathf.Cos(angle * Mathf.Deg2Rad)) * 1.1f, Quaternion.AngleAxis(angle, Vector3.up), Graphic.MatSingle, 0);
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
