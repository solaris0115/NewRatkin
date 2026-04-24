using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// CompEquippable.CompGetEquippedGizmosExtra에 Prototype Pulse Rifle 발사 모드 토글 Gizmo 추가
    /// 장비된 무기는 CompGetGizmosExtra가 호출되지 않으므로 이 패치 필요
    /// </summary>
    [StaticConstructorOnStartup]
    public static class PulseRifleGizmoPatch
    {
        static PulseRifleGizmoPatch()
        {
            Harmony harmony = new Harmony("com.NewRatkin.rimworld.mod.pulserifle");
            harmony.Patch(
                AccessTools.Method(typeof(CompEquippable), nameof(CompEquippable.CompGetEquippedGizmosExtra)),
                postfix: new HarmonyMethod(typeof(PulseRifleGizmoPatch), nameof(Postfix))
            );
        }

        public static void Postfix(CompEquippable __instance, ref IEnumerable<Gizmo> __result)
        {
            Comp_PulseRifleFireMode toggle = __instance.parent.GetComp<Comp_PulseRifleFireMode>();
            if (toggle == null)
            {
                return;
            }

            Pawn holder = GetHolder(__instance);
            if (holder == null || holder.Faction != Faction.OfPlayer)
            {
                return;
            }

            __result = __result.Concat(toggle.GetToggleGizmos());
        }

        private static Pawn GetHolder(CompEquippable comp)
        {
            if (comp.parent?.ParentHolder is Pawn_EquipmentTracker tracker)
            {
                return tracker.pawn;
            }
            return null;
        }
    }
}
