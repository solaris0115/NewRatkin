using HarmonyLib;
using System;
using Verse;

namespace NewRatkin
{
    public class CompProperties_RK_Shield : CompProperties
    {
        public CompProperties_RK_Shield()
        {
            this.compClass = typeof(CompRKShield);
        }

        public int startingTicksToReset = 3200;

        public float minDrawSize = 1.2f;

        public float maxDrawSize = 1.55f;

        public float energyLossPerDamage = 0.033f;

        public float energyOnReset = 0.2f;

        public bool blocksRangedWeapons = true;
    }
    [StaticConstructorOnStartup]
    public class CompRKShield : ThingComp
    {

    }

[StaticConstructorOnStartup]
    public static class ShieldPatch
    {
        private static readonly Type patchType = typeof(ShieldPatch);
        static ShieldPatch()
        {
            Harmony harmonyInstance = new Harmony("com.NewRatkin.rimworld.mod");
            harmonyInstance.Patch(AccessTools.PropertyGetter(typeof(ThingDef), "IsShieldThatBlocksRanged"), null, new HarmonyMethod(patchType, "IsShieldBlocksRangedPostifx"));
        }

        public static void IsShieldBlocksRangedPostifx(ref bool __result, ThingDef __instance)
        {
            if (__result == true) return;
            if (__instance == null) return;

            if (__instance.HasComp(typeof(CompRKShield)) == true && __instance.GetCompProperties<CompProperties_RK_Shield>() != null )
            {
                __result = true;
            }
        }
    }
}