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
    [StaticConstructorOnStartup]
    public static class ColorPatch
    {
        static ColorPatch()
        {
            Harmony harmonyInstance = new Harmony("com.NewRatkin.rimworld.mod");
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
    [HarmonyPatch(typeof(Pawn_NeedsTracker))]
    [HarmonyPatch("ShouldHaveNeed")]
    public static class PawnNeedPatch
    {
        [HarmonyPostfix]
        static void Postfix(ref bool __result,NeedDef nd,Pawn ___pawn)
        {
            if (nd == RatkinNeedDefOf.Outdoors && ___pawn.def == RatkinRaceDefOf.Ratkin)
            {
                __result = false;
            }
        }
    }

    public class CustomThingDef : ThingDef
    {
        public bool followStuffColor = true;
    }
     
    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch("TryGenerateNewPawnInternal")]
    public static class PawnGeneratorPatch
    {
        [HarmonyPostfix]
        static void Postfix(Pawn __result)
        {
            if (__result != null)
            {
                if (__result?.story?.adulthood == BackstoryCache.Ratkin_Sister)
                {
                    __result.abilities?.GainAbility(RatkinAbilityDefOf.RK_PrayerService);
                }
            }
        }
    }

    // For backward compatiblity
    [HarmonyPatch(typeof(Pawn_AbilityTracker))]
    [HarmonyPatch("ExposeData")]
    public static class Pawn_AbilityTrackerPatch
    {
        [HarmonyPostfix]
        static void Postfix(Pawn_AbilityTracker __instance, Pawn ___pawn)
        {
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                if (___pawn?.story?.adulthood == BackstoryCache.Ratkin_Sister && !__instance.abilities.Any(x => x.def == RatkinAbilityDefOf.RK_PrayerService))
                {
                    __instance.GainAbility(RatkinAbilityDefOf.RK_PrayerService);
                }
            }
        }
    }

    [HarmonyPatch(typeof(NegativeInteractionUtility))]
    [HarmonyPatch("NegativeInteractionChanceFactor")]
    public static class NegativeInteractionUtilityPatch
    {
        [HarmonyPrefix]
        static bool Prefix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator.story?.adulthood == BackstoryCache.Ratkin_Sister)
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }
}