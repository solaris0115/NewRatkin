using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

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
     



}