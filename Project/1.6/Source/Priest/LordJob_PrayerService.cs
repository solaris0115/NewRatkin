using RimWorld;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace NewRatkin
{
    public class LordJob_PrayerService : LordJob_VoluntarilyJoinable
    {
        protected Building pulpit;
        protected IntVec3 spot;
        protected Pawn organizer;
        protected CellRect spectatorRect;

        public Pawn Organizer => organizer;

        private Trigger_TicksPassed triggerProgress;

        public LordJob_PrayerService()
        {
        }

        public LordJob_PrayerService(Building pulpit, IntVec3 spot, Pawn organizer)
        {
            this.pulpit = pulpit;
            this.spot = spot;
            this.organizer = organizer;
            this.spectatorRect = CalculateSpectateRect();

            Messages.Message("MessagePrayerServiceStart".Translate(organizer.Named("PAWN")), organizer, MessageTypeDefOf.NeutralEvent, historical: false);
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();

            #region LordToils
            LordToil_JoinPrayerService lordToil_JoinPrayerService = new LordToil_JoinPrayerService(organizer, spot);
            stateGraph.AddToil(lordToil_JoinPrayerService);

            LordToil_PrayerService lordToil_PrayerService = new LordToil_PrayerService(pulpit, organizer, spot, spectatorRect);
            stateGraph.AddToil(lordToil_PrayerService);

            LordToil_End lordToil_End = new LordToil_End();
            stateGraph.AddToil(lordToil_End);
            #endregion

            #region Transition JoinPrayerService => Others
            {
                // 주최자가 방에 들어온 다음 일정틱 대기 후 다음 단계로 진행하도록
                Transition transition_JoinPrayerService_to_PrayerService = new Transition(lordToil_JoinPrayerService, lordToil_PrayerService);
                transition_JoinPrayerService_to_PrayerService.AddTrigger(new Trigger_TicksPassedAfterConditionMet(ConstPriest.PrayerServicePreStageTicks, () =>
                {
                    if (organizer.GetRoom() == spot.GetRoom(organizer.Map))
                    {
                        return true;
                    }

                    return false;

                }, checkEveryTicks: 250));
                stateGraph.AddTransition(transition_JoinPrayerService_to_PrayerService);
            }

            {
                // 주최자가 피해를 입었거나, 취소될만한 상황이 될 경우 강제로 종료
                Transition transition_JoinPrayerService_to_End = new Transition(lordToil_JoinPrayerService, lordToil_End);
                transition_JoinPrayerService_to_End.AddTrigger(new Trigger_TickCondition(IsAborted));
                transition_JoinPrayerService_to_End.AddTrigger(new Trigger_PawnHarmed());
                transition_JoinPrayerService_to_End.AddPreAction(new TransitionAction_Message("MessagePrayerServiceAborted".Translate(), MessageTypeDefOf.NeutralEvent));
                stateGraph.AddTransition(transition_JoinPrayerService_to_End);
            }
            #endregion

            #region Transition_PrayerService => Others
            {
                // 일정틱 진행되었거나, 취소될만한 상황이 될 경우 강제로 종료
                triggerProgress = new Trigger_TicksPassed(Rand.Range(ConstPriest.PrayerServiceOnStageTicksMin, ConstPriest.PrayerServiceOnStageTicksMax));
                Transition transition_PrayerService_to_End = new Transition(lordToil_PrayerService, lordToil_End);
                transition_PrayerService_to_End.AddTrigger(triggerProgress);
                transition_PrayerService_to_End.AddTrigger(new Trigger_PawnHarmed());
                transition_PrayerService_to_End.AddTrigger(new Trigger_TickCondition(IsAborted));
                transition_PrayerService_to_End.AddPreAction(new TransitionAction_Message("MessagePrayerServiceEnded".Translate()));
                stateGraph.AddTransition(transition_PrayerService_to_End);
            }
            #endregion

            return stateGraph;
        }

        public override float VoluntaryJoinPriorityFor(Pawn p)
        {
            if (IsAborted())
            {
                return 0f;
            }

            if (p == organizer)
            {
                return 100f;
            }

            if (IsGuest(p))
            {
                if (!lord.ownedPawns.Contains(p))
                {
                    // 끝나가는 중에는 참여 불가
                    if (triggerProgress.TicksLeft < 300)
                    {
                        return 0f;
                    }

                    if (!SpectatorCellFinder.TryFindSpectatorCellFor(p, spectatorRect, Map, out IntVec3 _, SpectateRectSide.All))
                    {
                        return 0f;
                    }

                    return p.story.traits.HasTrait(RatkinTraitDefOf.Faith) ? 100f : VoluntarilyJoinableLordJobJoinPriorities.SocialGathering;
                }
            }

            return 0f;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref spot, "spot");
            Scribe_References.Look(ref organizer, "organizer");
        }

        private bool IsGuest(Pawn p)
        {
            if (!p.RaceProps.Humanlike)
            {
                return false;
            }
            if (p == organizer)
            {
                return false;
            }
            if (p.Faction != organizer.Faction)
            {
                return false;
            }
            if (!GatheringsUtility.ShouldGuestKeepAttendingGathering(p))
            {
                return false;
            }

            return true;
        }

        private bool IsAborted()
        {
            if (organizer.DestroyedOrNull() || organizer.Drafted || organizer.Downed || organizer.Dead)
            {
                return true;
            }

            if (spot.GetRoom(organizer.Map) == null)
            {
                return true;
            }

            if (pulpit.DestroyedOrNull() || pulpit.IsForbidden(organizer) || pulpit.IsBurning())
            {
                return true;
            }

            return false;
        }

        private CellRect CalculateSpectateRect()
        {
            var cells = new CellRect(spot.x - 2, spot.z - 2, 5, 5).Cells.Where(x =>
            (x == spot) || (GenSight.LineOfSight(x, spot, organizer.Map, skipFirstCell: true) && !x.Impassable(organizer.Map)));
            var xValues = cells.Select(x => x.x);
            var zValues = cells.Select(x => x.z);

            int xMin = xValues.Min();
            int zMin = zValues.Min();
            int xMax = xValues.Max();
            int zMax = zValues.Max();

            if (xMin == xMax || zMin == zMax)
            {
                return CellRect.SingleCell(spot);
            }

            return new CellRect(xMin, zMin, xMax - xMin, zMax - zMin);
        }
    }
}
