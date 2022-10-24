using Verse;
using Verse.AI.Group;
using RimWorld;
using Verse.AI;

namespace NewRatkin
{
    public class LordJob_BombPlanting : LordJob
    {
        private Faction faction;

        private IntVec3 defendingSpot;

        private float eventPoint;

        public LordJob_BombPlanting()
        {
        }

        public LordJob_BombPlanting(Faction faction, IntVec3 defendingSpot, float eventPoint)
        {
            this.faction = faction;
            this.defendingSpot = defendingSpot;
            this.eventPoint = eventPoint;
        }

        public override bool GuiltyOnDowned
        {
            get
            {
                return true;
            }
        }
        public override bool AddFleeToil
        {
            get
            {
                return false;
            }
        }
        //NewRatkin.LordJob_BombPlanting:LordJob
        public override StateGraph CreateGraph()
        {   
            StateGraph stateGraph = new StateGraph();
            LordToil startingToil = stateGraph.AttachSubgraph(new LordJob_Travel(defendingSpot).CreateGraph()).StartingToil;
            //거점 방어 상태
            LordToil_DefendBomb lordToil_Siege = new LordToil_DefendBomb(defendingSpot,eventPoint);
            stateGraph.AddToil(lordToil_Siege);

            //목적지 이동 트랜지션
            Transition moveToDest = new Transition(startingToil, lordToil_Siege, false, true);
            moveToDest.AddTrigger(new Trigger_Memo("TravelArrived"));
            moveToDest.AddTrigger(new Trigger_TicksPassed(5000));
            stateGraph.AddTransition(moveToDest, false);

            //정착지 공격 상태
            LordToil noFleeAssaultColony = new LordToil_AssaultColony(false);
            noFleeAssaultColony.useAvoidGrid = true;
            stateGraph.AddToil(noFleeAssaultColony);

            //정착지 공격 트랜잭션
            Transition assultColony = new Transition(lordToil_Siege, noFleeAssaultColony, false, true);
            assultColony.AddTrigger(new Trigger_Memo("NoBuilders"));
            assultColony.AddTrigger(new Trigger_Memo("NoBomb"));
            assultColony.AddTrigger(new Trigger_TicksPassed(15000));

            FloatRange floatRange = new FloatRange(0.25f, 0.35f);
            float randomInRange = floatRange.RandomInRange;
            assultColony.AddPreAction(new TransitionAction_Message("MessageCommandoAssaulting".Translate(faction.def.pawnsPlural, faction), MessageTypeDefOf.ThreatBig, null, 1f));
            assultColony.AddPostAction(new TransitionAction_WakeAll());

            //퇴각 상태
            LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false);
            lordToil_ExitMap.useAvoidGrid = true;
            stateGraph.AddToil(lordToil_ExitMap);
            
            //퇴각 트랜지션
            Transition exitMap = new Transition(noFleeAssaultColony, lordToil_ExitMap, false, true);
            exitMap.AddTrigger(new Trigger_FractionColonyDamageTaken(randomInRange, 1200));
            exitMap.AddPreAction(new TransitionAction_Message("MessageRaidersSatisfiedLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name), null, 1f));
            stateGraph.AddTransition(assultColony, false);
            stateGraph.AddTransition(exitMap, false);

            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction", false);
            Scribe_Values.Look(ref defendingSpot, "siegeSpot", default(IntVec3), false);
            Scribe_Values.Look(ref eventPoint, "eventPoint", 0f, false);
        }
    }
}
