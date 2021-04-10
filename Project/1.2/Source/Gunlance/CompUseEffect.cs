﻿using System;
using Verse;
using RimWorld;

namespace NewRatkin
{
	public class CompUseEffect_ReloadGunlance : CompUseEffect
	{
		public CompProperties_GunlanceReload Prop
		{
			get
			{
				return props as CompProperties_GunlanceReload;
			}
		}
		public override float OrderPriority
		{
			get
			{
				return -1000f;
			}
		}

		public override void DoEffect(Pawn usedBy)
		{
			base.DoEffect(usedBy);
			CompRefuelable refuelable = usedBy.equipment.Primary.GetComp<CompRefuelable>();
			int needAmount = (int)(refuelable.Props.fuelCapacity-refuelable.Fuel);
			if (parent.stackCount > needAmount)
			{
				refuelable.Refuel(needAmount);
				parent.SplitOff(needAmount).Destroy(DestroyMode.Vanish);
			}
			else
			{
				refuelable.Refuel(parent.stackCount);
				parent.Destroy(DestroyMode.Vanish);
			}
		}
		public override bool CanBeUsedBy(Pawn p, out string failReason)
		{
			if (p.equipment == null || p.equipment.Primary == null)
			{
				failReason = null;
				return false;
			}
			if (p.equipment.Primary.def.weaponTags==null || !(p.equipment.Primary.def == Prop.gunLanceDef) )
			{
				failReason = null;
				return false;
			}
			CompRefuelable refuelable = p.equipment.Primary.GetComp<CompRefuelable>();
			if (refuelable != null && refuelable.IsFull)
			{
				failReason = "FullAmmo".Translate();
				return false;
			}
			failReason = null;
			return true;
		}
	}

	public class CompProperties_GunlanceReload: CompProperties_UseEffect
	{
		public ThingDef gunLanceDef;
	}
}
