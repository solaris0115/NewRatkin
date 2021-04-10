using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using HarmonyLib;
using RimWorld.Planet;


namespace NewRatkin
{
    public class StockGenerator_Mercenary : StockGenerator
    {
        private bool respectPopulationIntent = false;

        public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
        {
            if (this.respectPopulationIntent && Rand.Value > StorytellerUtilityPopulation.PopulationIntent)
            {
                yield break;
            }
            int count = this.countRange.RandomInRange;
            for (int i = 0; i < count; i++)
            {
                Faction mercenaryFaction;
                if (!(from fac in Find.FactionManager.AllFactionsVisible
                      where fac != Faction.OfPlayer && fac.def.humanlikeFaction
                      select fac).TryRandomElement(out mercenaryFaction))
                {
                    yield break;
                }
                PawnKindDef mercenary = RatkinPawnKindDefOf.RatkinMercenary;
                Faction f = mercenaryFaction;
                PawnGenerationRequest request = new PawnGenerationRequest(mercenary, f, PawnGenerationContext.NonPlayer, forTile, false, false, false, false, true, true, 0.5f, !this.trader.orbital, true, true, false, false, false, false, false,0f, null,0.5f, null, null, null, null, null);
                yield return PawnGenerator.GeneratePawn(request);
            }
            yield break;
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.category == ThingCategory.Pawn && thingDef.race.Humanlike && thingDef.tradeability != Tradeability.None;
        }
    }
}
