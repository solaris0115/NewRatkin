using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using System.Text;
using System.Reflection;
using RimWorld;
using Harmony;
using UnityEngine;
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

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil startingToil = stateGraph.AttachSubgraph(new LordJob_Travel(defendingSpot).CreateGraph()).StartingToil;
            LordToil_DefendBomb lordToil_Siege = new LordToil_DefendBomb(defendingSpot,eventPoint);
            stateGraph.AddToil(lordToil_Siege);
            Transition transition = new Transition(startingToil, lordToil_Siege, false, true);
            transition.AddTrigger(new Trigger_Memo("TravelArrived"));
            transition.AddTrigger(new Trigger_TicksPassed(5000));
            stateGraph.AddTransition(transition, false);

            LordToil noFleeAssaultColony = new LordToil_AssaultColony(false);
            noFleeAssaultColony.useAvoidGrid = true;
            stateGraph.AddToil(noFleeAssaultColony);

            Transition transition3 = new Transition(lordToil_Siege, noFleeAssaultColony, false, true);
            transition3.AddTrigger(new Trigger_Memo("NoBuilders"));
            transition3.AddTrigger(new Trigger_Memo("NoBomb"));
            transition3.AddTrigger(new Trigger_TicksPassed(15000));
            FloatRange floatRange = new FloatRange(0.25f, 0.35f);
            float randomInRange = floatRange.RandomInRange;
            transition3.AddPreAction(new TransitionAction_Message("MessageCommandoAssaulting".Translate(faction.def.pawnsPlural, faction), MessageTypeDefOf.ThreatBig, null, 1f));
            transition3.AddPostAction(new TransitionAction_WakeAll());
            LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, false);
            lordToil_ExitMap.useAvoidGrid = true;
            stateGraph.AddToil(lordToil_ExitMap);
            Transition transition4 = new Transition(noFleeAssaultColony, lordToil_ExitMap, false, true);
            transition4.AddTrigger(new Trigger_FractionColonyDamageTaken(randomInRange, 1200));
            transition4.AddPreAction(new TransitionAction_Message("MessageRaidersSatisfiedLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name), null, 1f));

            /*LordToil lordToil_Assault = stateGraph.AttachSubgraph(new LordJob_AssaultColony(faction, true, true, false, false, true).CreateGraph()).StartingToil;
            Transition transition2 = new Transition(lordToil_Siege, lordToil_Assault, false, true);
            transition2.AddTrigger(new Trigger_Memo("NoBuilders"));
            transition2.AddTrigger(new Trigger_Memo("NoBomb"));
            //transition2.AddTrigger(new Trigger_PawnHarmed(0.08f, false, null));
            //transition2.AddTrigger(new Trigger_FractionPawnsLost(0.3f));
            transition2.AddTrigger(new Trigger_TicksPassed(15000));
            transition2.AddPreAction(new TransitionAction_Message("MessageCommandoAssaulting".Translate(faction.def.pawnsPlural, faction), MessageTypeDefOf.ThreatBig, null, 1f));
            transition2.AddPostAction(new TransitionAction_WakeAll());*/
            stateGraph.AddTransition(transition3, false);

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
