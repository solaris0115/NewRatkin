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
			if (this.Props.showFuelGizmo)
			{
				yield return new Gizmo_GunlanceStatus
				{
					compGunlanceFuel = this
				};
			}
			if (Props.showAllowAutoRefuelToggle)
			{
				yield return new Command_Toggle
				{
					defaultLabel = "CommandToggleAllowAutoRefuel".Translate(),
					defaultDesc = "CommandToggleAllowAutoRefuelDesc".Translate(),
					hotKey = KeyBindingDefOf.Command_ItemForbid,
					icon = (allowAutoRefuel ? TexCommand.ForbidOff : TexCommand.ForbidOn),
					isActive = (() => allowAutoRefuel),
					toggleAction = delegate ()
					{
						allowAutoRefuel = !allowAutoRefuel;
					}
				};
			}
			yield break;
		}
	}
}
