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
using Harmony;
using RimWorld.Planet;

namespace NewRatkin
{
    [StaticConstructorOnStartup]
    public static class ColorPatch
    {
        static ColorPatch()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("com.NewRatkin.rimworld.mod");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
    [HarmonyPatch(typeof(GenRecipe))]
    [HarmonyPatch("PostProcessProduct")]
    public static class ProductFinishGenColorHook
    {

        [HarmonyPrefix]
        static void Prefix(ref Thing product)
        {
            ThingWithComps twc = product as ThingWithComps;
            if (twc != null)
            {
                CustomThingDef def = twc.def as CustomThingDef;
                if (def != null && !def.followStuffColor)
                {
                    twc.SetColor(Color.white);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PawnApparelGenerator))]
    [HarmonyPatch("GenerateStartingApparelFor")]
    public static class PawnGenColorHook
    {
        [HarmonyPostfix]
        static void Postfix(ref Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    CustomThingDef def = wornApparel[i].def as CustomThingDef;
                    if (def != null && !def.followStuffColor)
                    {
                        wornApparel[i].SetColor(Color.white);
                        wornApparel[i].SetColor(Color.black);
                        wornApparel[i].SetColor(Color.white);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(ThingMaker))]
    [HarmonyPatch("MakeThing")]
    public static class ThingMakeColorHook
    {
        [HarmonyPostfix]
        static void Postfix(ref Thing __result)
        {
            ThingWithComps twc = __result as ThingWithComps;
            if (twc != null)
            {
                CustomThingDef def = twc.def as CustomThingDef;
                if (def != null && !def.followStuffColor)
                {
                    twc.SetColor(Color.white);
                    twc.SetColor(Color.black);
                    twc.SetColor(Color.white);
                }
            }
        }
    }

    public class CustomThingDef : ThingDef
    {
        public bool followStuffColor = true;
    }

    public class StockGenerator_Mercenary : StockGenerator
    {
        private bool respectPopulationIntent;

        public override IEnumerable<Thing> GenerateThings(int forTile)
        {
            if (this.respectPopulationIntent && Rand.Value > StorytellerUtilityPopulation.PopulationIntent)
            {
                yield break;
            }
            int count = this.countRange.RandomInRange;
            for (int i = 0; i < count; i++)
            {
                Faction slaveFaction;
                if (!(from fac in Find.FactionManager.AllFactionsVisible
                      where fac != Faction.OfPlayer && fac.def.humanlikeFaction
                      select fac).TryRandomElement(out slaveFaction))
                {
                    yield break;
                }
                PawnKindDef slave = PawnKindDefOf.Slave;
                Faction faction = slaveFaction;
                PawnGenerationRequest request = new PawnGenerationRequest(slave, faction, PawnGenerationContext.NonPlayer, forTile, false, false, false, false, true, false, 1f, !this.trader.orbital, true, true, false, false, false, false, null, null, null, null, null, null, null, null);
                yield return PawnGenerator.GeneratePawn(request);
            }
            yield break;
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.category == ThingCategory.Pawn && thingDef.race.Humanlike && thingDef.tradeability != Tradeability.None;
        }
    }
    [DefOf]
    public static class RatkinPawnKindDefOf
    {
        public static PawnKindDef Colonist;

        public static PawnKindDef Slave;

        public static PawnKindDef Villager;

        public static PawnKindDef Drifter;
    }
}