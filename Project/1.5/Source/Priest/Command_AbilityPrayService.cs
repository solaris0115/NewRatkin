using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewRatkin
{
	public enum PrayerServiceSpotBlockReason
	{
		None = 0,
		CannotReachPulpit,
		CannotReservePulpit,
		InteractionCellOutOfBounds,
		InteractionCellForbidden,
		InteractionCellNotStandable,
		PulpitForbidden,
		PulpitBurning,
		PulpitDangerous,
		RoomTooSmall
	}

    public class Command_AbilityPrayService : Command_Ability
	{
		public Command_AbilityPrayService(Ability ability, Pawn pawn) : base(ability, pawn) { }

		public override void ProcessInput(Event ev)
		{
			var organizer = ability.pawn;

			if (organizer.Drafted)
			{
				return;
			}

			LordJob_PrayerService lordJob_PrayerService = ability.pawn.GetLord()?.LordJob as LordJob_PrayerService;
			if (lordJob_PrayerService != null)
			{
				return;
			}

			if (ability.CooldownTicksRemaining > 0)
            {
				return;
            }

			if (!TryFindGatherSpot(organizer, out var pulpit, out var spot))
			{
				return;
			}

			int cooldownTicks = ability.def.cooldownTicksRange.RandomInRange;
			ability.StartCooldown(cooldownTicks);

			// gives global cooldown
			foreach (var pawn in PawnsFinder.AllMaps_FreeColonistsAndPrisoners)
            {
				pawn.abilities?.GetAbility(RatkinAbilityDefOf.RK_PrayerService)?.StartCooldown(cooldownTicks);
			}

			LordJob lordJob = new LordJob_PrayerService(pulpit, spot, organizer);
			LordMaker.MakeNewLord(organizer.Faction, lordJob, organizer.Map, (!lordJob.OrganizerIsStartingPawn) ? null : new Pawn[]
			{
				organizer
			});
		}

        protected override void DisabledCheck()
        {
            base.DisabledCheck();

			if (ability.CooldownTicksRemaining > 0)
			{
				disabledReason = "AbilityPrayerServiceCooldown".Translate() + ": " + ability.CooldownTicksRemaining.ToStringTicksToPeriod();
				disabled = true;
            }

			LordJob_PrayerService lordJob_PrayerService = ability.pawn.GetLord()?.LordJob as LordJob_PrayerService;
			if (lordJob_PrayerService != null)
			{
				disabledReason = "AbilityPrayerServiceDisabledAlreadyGivingPrayerService".Translate();
				disabled = true;
			}

			if (ability.pawn.Drafted)
			{
				disabledReason = "AbilityPrayerServiceDisabledDrafted".Translate();
				disabled = true;
			}

			var map = ability.pawn.Map;
			var allPulpits = map.listerBuildings.AllBuildingsColonistOfDef(RatkinBuildingDefOf.RK_Pulpit).ToList();
			if (!allPulpits.Any(b => IsPrayerServiceAvailableSpot(ability.pawn, b)))
			{
				disabledReason = GetPrayerServiceSpotDisabledReason(ability.pawn, allPulpits);
				disabled = true;
			}
		}

        public override bool InheritInteractionsFrom(Gizmo other)
		{
			return false;
		}

		protected bool TryFindGatherSpot(Pawn organizer, out Building pulpit, out IntVec3 spot)
		{
			var allBuildings = organizer.Map.listerBuildings.AllBuildingsColonistOfDef(RatkinBuildingDefOf.RK_Pulpit).Where(x => IsPrayerServiceAvailableSpot(organizer, x));
			if (allBuildings.EnumerableNullOrEmpty())
			{
				Log.Warning($"there is no PrayerService spot.");
				pulpit = null;
				spot = IntVec3.Invalid;
				return false;
			}

			var selectedBuliding = allBuildings.RandomElement();

			pulpit = selectedBuliding;
			spot = selectedBuliding.InteractionCell;
			return true;
		}

		public bool CanExecute(Map map, Pawn organizer = null)
		{
			if (organizer == null)
			{
				return false;
			}
			if (!TryFindGatherSpot(organizer, out Building _, out IntVec3 _))
			{
				return false;
			}

			return true;
		}

		public static PrayerServiceSpotBlockReason GetPrayerServiceSpotBlockReason(Pawn organizer, Building pulpit)
		{
			if (!organizer.CanReach(pulpit, PathEndMode.InteractionCell, Danger.None))
			{
				return PrayerServiceSpotBlockReason.CannotReachPulpit;
			}

			if (!organizer.CanReserve(pulpit))
			{
				return PrayerServiceSpotBlockReason.CannotReservePulpit;
			}

			Map map = organizer.Map;
			IntVec3 interactionCell = pulpit.InteractionCell;
			if (!interactionCell.InBounds(map))
			{
				return PrayerServiceSpotBlockReason.InteractionCellOutOfBounds;
			}

			if (interactionCell.IsForbidden(organizer))
			{
				return PrayerServiceSpotBlockReason.InteractionCellForbidden;
			}

			if (!interactionCell.Standable(map))
			{
				return PrayerServiceSpotBlockReason.InteractionCellNotStandable;
			}

			if (pulpit.IsForbidden(organizer))
			{
				return PrayerServiceSpotBlockReason.PulpitForbidden;
			}

			if (pulpit.IsBurning())
			{
				return PrayerServiceSpotBlockReason.PulpitBurning;
			}

			if (pulpit.IsDangerousFor(organizer))
			{
				return PrayerServiceSpotBlockReason.PulpitDangerous;
			}

			Room room = interactionCell.GetRoom(map);
			if (room != null && room.CellCount <= 25)
			{
				return PrayerServiceSpotBlockReason.RoomTooSmall;
			}

			return PrayerServiceSpotBlockReason.None;
		}

		public static string BlockReasonToTranslationKey(PrayerServiceSpotBlockReason reason)
		{
			switch (reason)
			{
				case PrayerServiceSpotBlockReason.CannotReachPulpit:
					return "AbilityPrayerServiceDisabledCannotReachPulpit";
				case PrayerServiceSpotBlockReason.CannotReservePulpit:
					return "AbilityPrayerServiceDisabledCannotReservePulpit";
				case PrayerServiceSpotBlockReason.InteractionCellOutOfBounds:
					return "AbilityPrayerServiceDisabledInteractionCellOutOfBounds";
				case PrayerServiceSpotBlockReason.InteractionCellForbidden:
					return "AbilityPrayerServiceDisabledInteractionCellForbidden";
				case PrayerServiceSpotBlockReason.InteractionCellNotStandable:
					return "AbilityPrayerServiceDisabledInteractionCellNotStandable";
				case PrayerServiceSpotBlockReason.PulpitForbidden:
					return "AbilityPrayerServiceDisabledPulpitForbidden";
				case PrayerServiceSpotBlockReason.PulpitBurning:
					return "AbilityPrayerServiceDisabledPulpitBurning";
				case PrayerServiceSpotBlockReason.PulpitDangerous:
					return "AbilityPrayerServiceDisabledPulpitDangerous";
				case PrayerServiceSpotBlockReason.RoomTooSmall:
					return "AbilityPrayerServiceDisabledRoomTooSmall";
				default:
					return "AbilityPrayerServiceDisabledNoPulpitBuilding";
			}
		}

		public static string GetPrayerServiceSpotDisabledReason(Pawn organizer, List<Building> colonistPulpits)
		{
			if (colonistPulpits.NullOrEmpty())
			{
				return "AbilityPrayerServiceDisabledNoPulpitBuilding".Translate();
			}

			List<PrayerServiceSpotBlockReason> reasons = new List<PrayerServiceSpotBlockReason>();
			foreach (Building b in colonistPulpits)
			{
				PrayerServiceSpotBlockReason r = GetPrayerServiceSpotBlockReason(organizer, b);
				if (r == PrayerServiceSpotBlockReason.None)
				{
					Log.Warning("GetPrayerServiceSpotDisabledReason: valid pulpit exists; UI should not query disabled text.");
					return string.Empty;
				}

				reasons.Add(r);
			}

			PrayerServiceSpotBlockReason chosen = reasons
				.GroupBy(r => r)
				.OrderByDescending(g => g.Count())
				.ThenBy(g => (int)g.Key)
				.First()
				.Key;

			return BlockReasonToTranslationKey(chosen).Translate();
		}

		public bool IsPrayerServiceAvailableSpot(Pawn organizer, Building pulpit)
		{
			return GetPrayerServiceSpotBlockReason(organizer, pulpit) == PrayerServiceSpotBlockReason.None;
		}
	}

	public class CompProperties_AbilityPrayService: AbilityCompProperties
	{
		public CompProperties_AbilityPrayService()
		{
			this.compClass = typeof(CompAbilityEffect_PrayService);
		}
	}
	public class CompAbilityEffect_PrayService : CompAbilityEffect
	{
		public new CompProperties_AbilityPrayService Props
		{
			get
			{
				return (CompProperties_AbilityPrayService)this.props;
			}
		}

		public override bool GizmoDisabled(out string reason)
		{
			reason = null;
			return false;
		}
	}
}
