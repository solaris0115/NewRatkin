using HarmonyLib;
using RimWorld;
using Verse;

namespace NewRatkin
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker))]
    [HarmonyPatch("DestroyAll")]
    public static class DestroyGearOnDropPatch
    {
        [HarmonyPrefix]
        static bool Prefix(Pawn ___pawn, ThingOwner<Apparel> ___wornApparel, DestroyMode mode)
        {
            DestroyApparelExtension mod = ___pawn.kindDef.GetModExtension<DestroyApparelExtension>() ?? DestroyApparelExtension.defaultValues;
            if (mod.destroyOnlyWeapon)
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Hediff_Injury))]
    [HarmonyPatch("PostAdd")]
    public static class Hediff_InjuryPatch
    {
        [HarmonyPrefix]
        static bool Prefix(Hediff_Injury __instance, ref DamageInfo? dinfo)
        {
            if (__instance.comps != null)
            {
                for (int i = 0; i < __instance.comps.Count; i++)
                {
                    __instance.comps[i].CompPostPostAdd(dinfo);
                }
            }
            return false;
        }
    }

    public class DestroyApparelExtension : DefModExtension
    {
        public static readonly DestroyApparelExtension defaultValues = new DestroyApparelExtension();

        public bool destroyOnlyWeapon = false;
    }
}
