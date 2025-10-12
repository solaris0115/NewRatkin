using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
	[StaticConstructorOnStartup]
	public class Gizmo_GunlanceStatus : Gizmo
	{
		public CompGunlanceFuel compGunlanceFuel;

		private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));

		private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

		private static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated", true);

		private const float ArrowScale = 0.5f;

		public Gizmo_GunlanceStatus()
		{
			Order = -100f;
		}
		public override float GetWidth(float maxWidth)
		{
			return 140f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect overRect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
			Find.WindowStack.ImmediateWindow(1523289473, overRect, WindowLayer.GameUI, delegate
			{
				Rect rect2;
				Rect rect = rect2 = overRect.AtZero().ContractedBy(6f);
				rect2.height = overRect.height / 2f;
				Text.Font = GameFont.Tiny;
				Widgets.Label(rect2, compGunlanceFuel.Props.FuelGizmoLabel);
				Rect rect3 = rect;
				rect3.yMin = overRect.height / 2f;
				float fillPercent = this.compGunlanceFuel.Fuel / this.compGunlanceFuel.Props.fuelCapacity;
				Widgets.FillableBar(rect3, fillPercent, FullBarTex, EmptyBarTex, false);
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(rect3, compGunlanceFuel.Fuel.ToString("F0") + " / " + this.compGunlanceFuel.Props.fuelCapacity.ToString("F0"));
				Text.Anchor = TextAnchor.UpperLeft;
			}, true, false, 1f);
			return new GizmoResult(GizmoState.Clear);
		}
    }
}
