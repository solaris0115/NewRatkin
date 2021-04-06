using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewRatkin
{
    public class Command_AbilityPrayerService : Command_Ability
	{
		public Command_AbilityPrayerService(Ability ability)
			: base(ability)
		{
		}

		public override void ProcessInput(Event ev)
		{
			var organizer = ability.pawn;
			organizer.drafter.Drafted = false;

			if (!TryFindGatherSpot(organizer, out var pulpit, out var spot))
			{
				return;
			}

			ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);

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

			var pulpits = ability.pawn.Map.listerBuildings.allBuildingsColonist.Where(x => x.def == RatkinBuildingDefOf.RK_Pulpit);
			if (pulpits.Count() == 0)
			{
				disabledReason = "AbilityPrayerServiceDisabledNoPulpit".Translate();
				disabled = true;
			}
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

		public bool IsPrayerServiceAvailableSpot(Pawn organizer, Building pulpit)
		{
			if (!organizer.CanReserveAndReach(pulpit, PathEndMode.InteractionCell, Danger.None))
			{
				return false;
			}

			Map map = organizer.Map;
			var interactionCell = pulpit.InteractionCell;
			if (!interactionCell.InBounds(map) || interactionCell.IsForbidden(organizer) || !interactionCell.Standable(map))
			{
				return false;
			}

			if (pulpit.IsForbidden(organizer) || pulpit.IsBurning() || pulpit.IsDangerousFor(organizer))
			{
				return false;
			}

			Room room = interactionCell.GetRoom(map);
			if (room == null)
			{
				return false;
			}

			if (room.CellCount <= 25)
			{
				return false;
			}

			return true;
		}
	}
}
