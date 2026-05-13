using System;
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
                AccessTools.Method(typeof(Pawn_AgeTracker), "RecalculateLifeStageIndex"),
                postfix: new HarmonyMethod(patchType, nameof(RecalculateLifeStageIndex_Postfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(NegativeInteractionUtility), nameof(NegativeInteractionUtility.NegativeInteractionChanceFactor)),
                prefix: new HarmonyMethod(patchType, nameof(NegativeInteraction_Prefix))
            );
        }

        /// <summary>
        /// 사제 kind는 성인 단계에서만 예배 능력 부여. 신생아는 어머니 kind를 물려 받아 GeneratePawn 시점에 부여되던 문제 방지.
        /// 유아·아동은 성장 후 RecalculateLifeStageIndex 포스트픽스에서 성인 전환 시 보충.
        /// </summary>
        private static void TryGrantPriestPrayerService(Pawn pawn)
        {
            if (pawn?.abilities == null)
            {
                return;
            }

            if (!pawn.DevelopmentalStage.Adult())
            {
                return;
            }

            bool isPriest = pawn.kindDef == RatkinPawnKindDefOf.RatkinPriest
                || pawn.kindDef == RatkinPawnKindDefOf.RK_PawnKind_Priest;

            if (!isPriest || pawn.abilities.GetAbility(RatkinAbilityDefOf.RK_PrayerService) != null)
            {
                return;
            }

            pawn.abilities.GainAbility(RatkinAbilityDefOf.RK_PrayerService);
        }

        public static void GeneratePawn_Postfix(Pawn __result)
        {
            TryGrantPriestPrayerService(__result);
        }

        public static void ExposeData_Postfix(Pawn_AbilityTracker __instance, Pawn ___pawn)
        {
            if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs) return;

            TryGrantPriestPrayerService(___pawn);
        }

        public static void RecalculateLifeStageIndex_Postfix(Pawn_AgeTracker __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field<Pawn>("pawn").Value;
            TryGrantPriestPrayerService(pawn);
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
