﻿using System;
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
                var mercenaryPawnKindDef = RatkinPawnKindDefOf.RatkinMercenary;

                PawnGenerationRequest request = PawnGenerationRequest.MakeDefault();
                request.KindDef = mercenaryPawnKindDef;
                request.Faction = mercenaryFaction;
                request.Tile = forTile;
                request.ForceAddFreeWarmLayerIfNeeded = !this.trader.orbital;

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
