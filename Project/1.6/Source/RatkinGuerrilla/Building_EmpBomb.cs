using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using UnityEngine;


namespace NewRatkin
{
    public class MinifiedThing_Custom: MinifiedThing
    {
        public override void Tick()
        {
            if (InnerThing == null)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }
            base.Tick();
            if (InnerThing is Building_EmpBomb)
            {
                InnerThing.Tick();
            }
        }
    }

    public class Building_EmpBomb : Building
    {
        public Comp_CountDown compCountDown;
        public Comp_Emp compEmp;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compEmp = GetComp<Comp_Emp>();
            compCountDown = GetComp<Comp_CountDown>();
        }
    }
    public class CompProperties_CountDown : CompProperties
    {
        public int timeLimit = 0;
    }
    public class Comp_CountDown : ThingComp
    {
        public bool isPlanted = false;
        public int remainingTick = 0;
        public int remainingSecond = 0;
        int timeLimitTick = 0;
        int plantedTick = 0;

        private float tempTick = 0;

        public Mote_CountDown tempMote;

        public CompProperties_CountDown Props
        {
            get
            {
                return (CompProperties_CountDown)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            timeLimitTick = GenTicks.SecondsToTicks(Props.timeLimit);
            if (!isPlanted)
            {
                isPlanted = true;
                plantedTick = Find.TickManager.TicksGame;
                remainingTick = timeLimitTick;
                remainingSecond = Props.timeLimit;
            }
            ThrowText(parent.DrawPos + new Vector3(0, 0, 0.5f), parent.Map, remainingSecond.ToString(), Color.red);
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (tempMote != null && !tempMote.Destroyed)
            {
                tempMote.Destroy();
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref plantedTick, "plantedTick");
            Scribe_Values.Look(ref isPlanted, "isPlanted");
            Scribe_Values.Look(ref remainingTick, "remainingTick");
            Scribe_Values.Look(ref remainingSecond, "remainingSecond");            
        }

        public override void CompTick()
        {
            if(isPlanted)
            {
                remainingTick +=-1;
                tempTick = remainingTick.TicksToSeconds();
                if (tempTick < remainingSecond)
                {
                    remainingSecond = (int)tempTick;
                    Map map = parent.Map;
                    if(map == null)
                    {
                        map = ((Thing)ParentHolder).Map;
                    }
                    if(map!=null)
                    {
                        ThrowText(parent.DrawPos + new Vector3(0, 0, 0.5f), map, remainingSecond.ToString(), Color.red);
                    }
                }
                if (remainingTick < 0)
                {
                    if (tempMote != null && !tempMote.Destroyed)
                    {
                        tempMote.Destroy();
                    }
                    parent.GetComp<Comp_Emp>().EmpActivate();
                    parent.Destroy(DestroyMode.Vanish);
                }
            }
            
        }

        public void ThrowText(Vector3 loc, Map map, string text, Color color, float timeBeforeStartFadeout = -1f)
        {
            if(tempMote!=null && !tempMote.Destroyed)
            {
                tempMote.Destroy();
            }
            Mote_CountDown moteText = (Mote_CountDown)ThingMaker.MakeThing(RatkinMoteDefOf.Mote_CountDown);
            moteText.exactPosition = loc;
            moteText.text = text;
            moteText.textColor = color;
            if (timeBeforeStartFadeout >= 0f)
            {
                moteText.overrideTimeBeforeStartFadeout = timeBeforeStartFadeout;
            }
            tempMote = moteText;
            GenSpawn.Spawn(moteText, parent.Position, map);
        }
    }
    public class Comp_Emp : ThingComp
    {
        public float eventPoint;
        public void EmpActivate()
        {
            HashSet<CompPower> tempComp = new HashSet<CompPower>();
            foreach (PowerNet pn in parent.Map.powerNetManager.AllNetsListForReading)
            {
                foreach (CompPower comp in pn.transmitters.FindAll((CompPower comp) => comp.parent.GetComp<CompBreakdownable>() != null).Take((int)(eventPoint*0.004f)))
                {
                    comp.parent.GetComp<CompBreakdownable>().DoBreakdown();
                }
            }
            Current.Game.GetComponent<GameComponent_EMPCheck>().DoEmpIncident(parent);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref eventPoint, "eventPoint");
        }
    }

    public class GameComponent_EMPCheck : GameComponent
    {
        public Lord lord;
        public int operableNextTick = 0;
        public bool isEmpSet = false;

        public GameComponent_EMPCheck(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref operableNextTick, "operableNextTick");
            Scribe_Values.Look(ref isEmpSet, "isEmpSet");
            Scribe_References.Look(ref lord, "lord");
        }
        public void DoEmpIncident(Thing thing)
        {
            if (operableNextTick < Find.TickManager.TicksGame)
            {
                operableNextTick = Find.TickManager.TicksGame + 60000;
                Find.LetterStack.ReceiveLetter("EMPExplode".Translate(), "EMPExplodeDesc".Translate(), LetterDefOf.ThreatBig);
                StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
                parms.faction = Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.Rakinia);
                parms.points *= 0.4f;
                parms.raidStrategy = DefDatabase<RaidStrategyDef>.GetNamed("ImmediateAttackSappers");
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                RatkinIncidentDefOf.RatkinFollowUpTroops.Worker.TryExecute(parms);
                GenExplosion.DoExplosion(thing.Position, thing.Map, 15, RatkinDamageDefOf.RK_EMP, thing);
            }
        }
    }
}
