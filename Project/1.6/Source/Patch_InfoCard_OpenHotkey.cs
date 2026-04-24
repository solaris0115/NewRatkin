using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// 갓모드일 때만: 키바인딩(RK_OpenInfoCard, 기본 I)로 정보(Dialog_InfoCard) 연다.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Patch_InfoCard_OpenHotkey
    {
        static Patch_InfoCard_OpenHotkey()
        {
            new Harmony("com.NewRatkin.rimworld.mod.infocardopenhotkey").Patch(
                AccessTools.Method(typeof(MainTabsRoot), nameof(MainTabsRoot.HandleLowPriorityShortcuts)),
                postfix: new HarmonyMethod(typeof(Patch_InfoCard_OpenHotkey), nameof(Postfix_HandleLowPriorityShortcuts)));
        }

        private static void Postfix_HandleLowPriorityShortcuts()
        {
            if (!DebugSettings.godMode)
                return;
            if (Current.ProgramState != ProgramState.Playing)
                return;
            if (RatkinKeyBindingDefOf.RK_OpenInfoCard == null)
                return;
            if (!RatkinKeyBindingDefOf.RK_OpenInfoCard.KeyDownEvent)
                return;
            if (!string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
                return;

            if (WorldRendererUtility.WorldSelected)
            {
                WorldObject wo = Find.WorldSelector.SingleSelectedObject;
                if (wo != null)
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(wo));
                    Event.current.Use();
                    return;
                }

                PlanetTile tile = Find.WorldSelector.SelectedTile;
                if (tile.Valid)
                {
                    BiomeDef biome = Find.WorldGrid[tile].PrimaryBiome;
                    if (biome != null)
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(biome, null));
                        Event.current.Use();
                    }
                }

                return;
            }

            Thing thing = Find.Selector.SingleSelectedThing;
            if (thing == null)
                return;

            OpenInfoCardForThing(thing);
            Event.current.Use();
        }

        private static void OpenInfoCardForThing(Thing thing)
        {
            IConstructible constructible = thing as IConstructible;
            if (constructible != null)
            {
                ThingDef thingDef = thing.def.entityDefToBuild as ThingDef;
                if (thingDef != null)
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thingDef, constructible.EntityToBuildStuff(), null));
                    return;
                }

                Find.WindowStack.Add(new Dialog_InfoCard(thing.def.entityDefToBuild, null));
                return;
            }

            Find.WindowStack.Add(new Dialog_InfoCard(thing, null));
        }
    }
}
