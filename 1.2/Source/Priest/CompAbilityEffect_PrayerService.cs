using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace NewRatkin
{
    public class CompAbilityEffect_PrayerService : CompAbilityEffect
	{
		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			var organizer = parent.pawn;
			organizer.drafter.Drafted = false;

			if (!TryFindGatherSpot(organizer, out var pulpit, out var spot))
            {
				return;
            }

			LordJob lordJob = new LordJob_PrayerService(pulpit, spot, organizer);
			LordMaker.MakeNewLord(organizer.Faction, lordJob, organizer.Map, (!lordJob.OrganizerIsStartingPawn) ? null : new Pawn[]
			{
				organizer
			});
		}

		public override bool GizmoDisabled(out string reason)
		{
			LordJob_PrayerService lordJob_PrayerService = parent.pawn.GetLord()?.LordJob as LordJob_PrayerService;
			if (lordJob_PrayerService != null)
			{
				reason = "AbilityPrayerServiceDisabledAlreadyGivingPrayerService".Translate();
				return true;
			}

			if (parent.pawn.Drafted)
			{
				reason = "AbilityPrayerServiceDisabledDrafted".Translate();
				return true;
            }

			var pulpits = parent.pawn.Map.listerBuildings.allBuildingsColonist.Where(x => x.def == RatkinBuildingDefOf.RK_Pulpit);
			if (pulpits.Count() == 0)
			{
				reason = "AbilityPrayerServiceDisabledNoPulpit".Translate();
				return true;
            }

			reason = null;
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
