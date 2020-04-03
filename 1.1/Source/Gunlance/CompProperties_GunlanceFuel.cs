using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
namespace NewRatkin
{
	public class CompProperties_GunlanceFuel : CompProperties
	{
		public float fuelCapacity = 1;

		public float fuelConsumptionRate = 1;

		public float initialFuel =1;

		public float autoRefueLlimit = 0;

		public ThingFilter fuelFilter;

		public bool showFuelGizmo;

		public bool initialAllowAutoRefuel = true;

		public bool showAllowAutoRefuelToggle;

		public bool drawOutOfFuelOverlay = true;

		public float minimumFueledThreshold;

		public bool drawFuelGaugeInMap;

		public bool atomicFueling;

		public string fuelLabel;

		public string fuelGizmoLabel;

		public string outOfFuelMessage;

		public string fuelIconPath;

		private Texture2D fuelIcon;

		public string FuelLabel
		{
			get
			{
				if (this.fuelLabel.NullOrEmpty())
				{
					return "Fuel".TranslateSimple();
				}
				return this.fuelLabel;
			}
		}

		public string FuelGizmoLabel
		{
			get
			{
				if (this.fuelGizmoLabel.NullOrEmpty())
				{
					return "Fuel".TranslateSimple();
				}
				return this.fuelGizmoLabel;
			}
		}

		public Texture2D FuelIcon
		{
			get
			{
				if (this.fuelIcon == null)
				{
					if (!this.fuelIconPath.NullOrEmpty())
					{
						this.fuelIcon = ContentFinder<Texture2D>.Get(this.fuelIconPath, true);
					}
					else
					{
						ThingDef thingDef;
						if (this.fuelFilter.AnyAllowedDef != null)
						{
							thingDef = this.fuelFilter.AnyAllowedDef;
						}
						else
						{
							thingDef = ThingDefOf.Chemfuel;
						}
						this.fuelIcon = thingDef.uiIcon;
					}
				}
				return this.fuelIcon;
			}
		}

		public CompProperties_GunlanceFuel()
		{
			compClass = typeof(CompGunlanceFuel);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			fuelFilter.ResolveReferences();
		}
	}
}
