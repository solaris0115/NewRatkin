using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
	[StaticConstructorOnStartup]
	public class CompGunlanceFuel : CompRefuelable
	{
		public override void CompTick()
		{
			base.CompTick();
			//방열식일 경우 여기서 방열
			/*
			if (!this.Props.consumeFuelOnlyWhenUsed && (this.flickComp == null || this.flickComp.SwitchIsOn))
			{
				this.ConsumeFuel(this.ConsumptionRatePerTick);
			}
			if (this.Props.fuelConsumptionPerTickInRain > 0f && this.parent.Spawned && this.parent.Map.weatherManager.RainRate > 0.4f && !this.parent.Map.roofGrid.Roofed(this.parent.Position))
			{
				this.ConsumeFuel(this.Props.fuelConsumptionPerTickInRain);
			}*/
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (Props.showFuelGizmo && Find.Selector.SingleSelectedThing!=null)
			{
				yield return new Gizmo_GunlanceStatus
				{
					compGunlanceFuel = this
				};
			}
			yield break;
		}
	}
}
