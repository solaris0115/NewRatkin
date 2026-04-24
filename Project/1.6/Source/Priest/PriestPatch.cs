using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NewRatkin
{
    [StaticConstructorOnStartup]
    public static class PriestPatch
    {
        private static readonly Type patchType = typeof(PriestPatch);

        static PriestPatch()
        {
            Harmony harmony = new Harmony("com.NewRatkin.rimworld.mod.priest");

            harmony.Patch(
                AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(patchType, nameof(GeneratePawn_Postfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Pawn_AbilityTracker), nameof(Pawn_AbilityTracker.ExposeData)),
                postfix: new HarmonyMethod(patchType, nameof(ExposeData_Postfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(NegativeInteractionUtility), nameof(NegativeInteractionUtility.NegativeInteractionChanceFactor)),
                prefix: new HarmonyMethod(patchType, nameof(NegativeInteraction_Prefix))
            );
        }

        public static void GeneratePawn_Postfix(Pawn __result)
        {
            if (__result?.abilities == null) return;

            bool isPriest = __result.kindDef == RatkinPawnKindDefOf.RatkinPriest
                || __result.kindDef == RatkinPawnKindDefOf.RK_PawnKind_Priest;

            if (isPriest && __result.abilities.GetAbility(RatkinAbilityDefOf.RK_PrayerService) == null)
            {
                __result.abilities.GainAbility(RatkinAbilityDefOf.RK_PrayerService);
            }
        }

        public static void ExposeData_Postfix(Pawn_AbilityTracker __instance, Pawn ___pawn)
        {
            if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs) return;

            bool isPriest = ___pawn?.kindDef == RatkinPawnKindDefOf.RatkinPriest
                || ___pawn?.kindDef == RatkinPawnKindDefOf.RK_PawnKind_Priest;

            if (isPriest && !__instance.abilities.Any(x => x.def == RatkinAbilityDefOf.RK_PrayerService))
            {
                __instance.GainAbility(RatkinAbilityDefOf.RK_PrayerService);
            }
        }

        public static bool NegativeInteraction_Prefix(ref float __result, Pawn initiator, Pawn recipient)
        {
            if (initiator.story?.Adulthood == RatkinBackstoryDefOf.Ratkin_Sister)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }
}
