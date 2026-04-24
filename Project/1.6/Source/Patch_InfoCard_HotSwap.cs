using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// 갓모드(DebugSettings.godMode)일 때만 동작.
    /// Dialog_InfoCard HotSwap, 맵 선택 동기화, 창 바깥 클릭/카메라 동작 완화.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_InfoCard_HotSwap
    {
        private static bool _swapping = false;

        private static readonly AccessTools.FieldRef<Dialog_InfoCard, Thing>       f_thing     = AccessTools.FieldRefAccess<Dialog_InfoCard, Thing>("thing");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, ThingDef>    f_stuff     = AccessTools.FieldRefAccess<Dialog_InfoCard, ThingDef>("stuff");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, Def>         f_def       = AccessTools.FieldRefAccess<Dialog_InfoCard, Def>("def");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, WorldObject> f_worldObj  = AccessTools.FieldRefAccess<Dialog_InfoCard, WorldObject>("worldObject");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, RoyalTitleDef> f_titleDef = AccessTools.FieldRefAccess<Dialog_InfoCard, RoyalTitleDef>("titleDef");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, Faction>     f_faction   = AccessTools.FieldRefAccess<Dialog_InfoCard, Faction>("faction");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, Pawn>        f_pawn      = AccessTools.FieldRefAccess<Dialog_InfoCard, Pawn>("pawn");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, Hediff>      f_hediff    = AccessTools.FieldRefAccess<Dialog_InfoCard, Hediff>("hediff");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, Dialog_InfoCard.InfoCardTab> f_tab = AccessTools.FieldRefAccess<Dialog_InfoCard, Dialog_InfoCard.InfoCardTab>("tab");
        private static readonly AccessTools.FieldRef<Dialog_InfoCard, Precept_ThingStyle> f_precept = AccessTools.FieldRefAccess<Dialog_InfoCard, Precept_ThingStyle>("precept");

        static Patch_InfoCard_HotSwap()
        {
            var harmony = new Harmony("com.NewRatkin.rimworld.mod.infocardhotswap");
            harmony.Patch(
                AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)),
                prefix: new HarmonyMethod(typeof(Patch_InfoCard_HotSwap), nameof(Prefix_WindowStack_Add)));
            harmony.Patch(
                AccessTools.Method(typeof(Dialog_InfoCard), "Setup"),
                postfix: new HarmonyMethod(typeof(Patch_InfoCard_HotSwap), nameof(Postfix_Dialog_InfoCard_Setup)));
            harmony.Patch(
                AccessTools.Method(typeof(Selector), nameof(Selector.Select), new[] { typeof(object), typeof(bool), typeof(bool) }),
                postfix: new HarmonyMethod(typeof(Patch_InfoCard_HotSwap), nameof(Postfix_Selector_Select)));
        }

        private static void Postfix_Dialog_InfoCard_Setup(Dialog_InfoCard __instance)
        {
            if (!DebugSettings.godMode)
                return;
            __instance.closeOnClickedOutside = false;
            __instance.absorbInputAroundWindow = false;
            __instance.preventCameraMotion = false;
        }

        public static bool Prefix_WindowStack_Add(WindowStack __instance, Window window)
        {
            if (_swapping)
                return true;
            if (!DebugSettings.godMode)
                return true;

            if (!(window is Dialog_InfoCard newCard))
                return true;

            Dialog_InfoCard existing = Find.WindowStack.WindowOfType<Dialog_InfoCard>();
            if (existing == null || existing == newCard)
                return true;

            CopyInfoCardFields(existing, newCard);
            StatsReportUtility.Reset();

            _swapping = true;
            try
            {
                __instance.TryRemove(existing, false);
                __instance.Add(existing);
            }
            finally
            {
                _swapping = false;
            }

            return false;
        }

        private static void Postfix_Selector_Select()
        {
            if (!DebugSettings.godMode)
                return;
            if (Current.ProgramState != ProgramState.Playing)
                return;
            if (!WorldRendererUtility.DrawingMap)
                return;

            Dialog_InfoCard card = Find.WindowStack.WindowOfType<Dialog_InfoCard>();
            if (card == null)
                return;

            Thing thing = Find.Selector.SingleSelectedThing;
            if (thing == null)
                return;

            if (InfoCardAlreadyMatchesThing(card, thing))
                return;

            ApplyThingToInfoCard(card, thing);
            StatsReportUtility.Reset();
        }

        /// <summary>인스펙트 i 버튼 / I키와 동일한 대상 규칙으로 카드 필드를 맞춘다.</summary>
        public static void ApplyThingToInfoCard(Dialog_InfoCard card, Thing thing)
        {
            f_precept(card) = null;
            f_tab(card) = Dialog_InfoCard.InfoCardTab.Stats;
            f_worldObj(card) = null;
            f_titleDef(card) = null;
            f_faction(card) = null;
            f_pawn(card) = null;
            f_hediff(card) = null;

            IConstructible constructible = thing as IConstructible;
            if (constructible != null)
            {
                ThingDef thingDef = thing.def.entityDefToBuild as ThingDef;
                if (thingDef != null)
                {
                    f_thing(card) = null;
                    f_def(card) = thingDef;
                    f_stuff(card) = constructible.EntityToBuildStuff();
                }
                else
                {
                    f_thing(card) = null;
                    f_def(card) = thing.def.entityDefToBuild;
                    f_stuff(card) = null;
                }
            }
            else
            {
                f_thing(card) = thing;
                f_def(card) = null;
                f_stuff(card) = null;
            }
        }

        private static bool InfoCardAlreadyMatchesThing(Dialog_InfoCard card, Thing thing)
        {
            IConstructible constructible = thing as IConstructible;
            if (constructible != null)
            {
                ThingDef thingDef = thing.def.entityDefToBuild as ThingDef;
                if (thingDef != null)
                {
                    return f_thing(card) == null
                        && f_def(card) == thingDef
                        && f_stuff(card) == constructible.EntityToBuildStuff()
                        && f_worldObj(card) == null;
                }

                return f_thing(card) == null
                    && f_def(card) == thing.def.entityDefToBuild
                    && f_worldObj(card) == null;
            }

            return f_thing(card) == thing
                && f_def(card) == null
                && f_worldObj(card) == null;
        }

        private static void CopyInfoCardFields(Dialog_InfoCard dest, Dialog_InfoCard src)
        {
            f_thing(dest) = f_thing(src);
            f_stuff(dest) = f_stuff(src);
            f_def(dest) = f_def(src);
            f_worldObj(dest) = f_worldObj(src);
            f_titleDef(dest) = f_titleDef(src);
            f_faction(dest) = f_faction(src);
            f_pawn(dest) = f_pawn(src);
            f_hediff(dest) = f_hediff(src);
            f_tab(dest) = f_tab(src);
            f_precept(dest) = f_precept(src);
        }
    }
}
