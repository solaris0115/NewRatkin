using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
namespace NewRatkin
{
	public class CompProperties_GunlanceFuel : CompProperties_Refuelable
	{
		public CompProperties_GunlanceFuel()
		{
			compClass = typeof(CompGunlanceFuel);
		}
		public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
		{
			yield break;
		}
	}
}
