using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Harmony;
using UnityEngine;
using Verse.AI;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
/*
    [StaticConstructorOnStartup]
    public static class CustomVerbPatch
    {
        private static readonly Type patchType = typeof(CustomVerbPatch);
        static CustomVerbPatch()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("com.NewRatkin.rimworld.mod");
            HarmonyInstance labInstance = HarmonyInstance.Create("com.AdditionalVerb.rimworld.mod");
            if (!harmonyInstance.HasAnyPatches("com.AdditionalVerb.rimworld.mod"))
            {
                harmonyInstance.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "GetGizmos", null, null), null, new HarmonyMethod(patchType, "GetGizmosPostfix", null));

                harmonyInstance.Patch(AccessTools.Method(typeof(Targeter), "BeginTargeting", new Type[] { typeof(Verb) }), new HarmonyMethod(patchType, "BeginTargetingPrefix", null));
                harmonyInstance.Patch(AccessTools.Method(typeof(Targeter), "OrderPawnForceTarget", null, null), null, new HarmonyMethod(patchType, "OrderPawnForceTargetPostfix", null));
                harmonyInstance.Patch(AccessTools.Method(typeof(Targeter), "GetTargetingVerb", null, null), new HarmonyMethod(patchType, "GetTargetingVerbPostfix", null));
                harmonyInstance.Patch(AccessTools.Method(typeof(Targeter), "StopTargeting", null, null), new HarmonyMethod(patchType, "StopTargetingPrefix", null));

                harmonyInstance.Patch(AccessTools.Method(typeof(VerbTracker), "CreateVerbTargetCommand", null, null), new HarmonyMethod(patchType, "CreateVerbTargetCommandPrefix", null));
                harmonyInstance.Patch(AccessTools.Property(typeof(VerbTracker), "PrimaryVerb").GetGetMethod(), new HarmonyMethod(patchType, "PrimaryVerbPrefix", null));

                harmonyInstance.Patch(AccessTools.Method(typeof(VerbProperties), "AdjustedAccuracy", null, null), null, new HarmonyMethod(patchType, "AdjustedAccuracyPostfix", null));

                harmonyInstance.Patch(AccessTools.Method(typeof(TooltipUtility), "ShotCalculationTipString", null, null), new HarmonyMethod(patchType, "ShotCalculationTipStringPrefix", null));
                LongEventHandler.ExecuteWhenFinished
                (
                    delegate
                    {
                        currentCommandTexture = ContentFinder<Texture2D>.Get("UI/Commands/Select");
                    }
                );
                Log.Message("Done");
            }
        }
        public static Texture2D currentCommandTexture;
        public enum RangeCategory
        {
            Touch,
            Short,
            Medium,
            Long
        }
        public static IEnumerable<Gizmo> GetGizmosPostfix(IEnumerable<Gizmo> __result, Pawn_EquipmentTracker __instance)
        {
            int count = 0;
            foreach (Gizmo g in __result)
            {
                if (g is Command)
                {
                    switch (count)
                    {
                        case 0:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc1;
                            break;
                        case 1:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc3;
                            break;
                        case 2:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc4;
                            break;
                        case 3:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc5;
                            break;
                        case 4:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc7;
                            break;
                        case 5:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc8;
                            break;
                        case 6:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc9;
                            break;
                        case 7:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc10;
                            break;
                        case 8:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc11;
                            break;
                        default:
                            ((Command)g).hotKey = KeyBindingDefOf.Misc12;
                            break;
                    }
                }
                yield return g;
                count++;
            }
        }
        public static void BeginTargetingPrefix(Verb verb)
        {
            if(verb!=null && verb.verbTracker!=null && verb.verbTracker.directOwner!=null &&verb.DirectOwner is CompEquippable)
            {
                Comp_VerbSaveable comp = ((CompEquippable)verb.DirectOwner).parent.GetComp<Comp_VerbSaveable>();
                if (comp != null)
                {
                    comp.tempVerb = verb;
                }
            }
        }
        public static void OrderPawnForceTargetPostfix(Targeter __instance, Verb verb)
        {
            if (verb != null && verb.verbTracker != null && verb.verbTracker.directOwner != null && verb.DirectOwner is CompEquippable)
            {
                Comp_VerbSaveable comp = ((CompEquippable)verb.DirectOwner).parent.GetComp<Comp_VerbSaveable>();
                if (comp != null)
                {
                    if (!(Traverse.Create(__instance).Method("CurrentTargetUnderMouse", true).GetValue<LocalTargetInfo>().IsValid))
                    {
                        return;
                    }
                    comp.currentVerb = verb;
                }
            }
                
        }
        public static bool GetTargetingVerbPostfix(ref Verb __result, Pawn pawn)
        {
            Comp_VerbSaveable comp = pawn.equipment.Primary.GetComp<Comp_VerbSaveable>();
            if (comp != null)
            {
                __result = comp.currentVerb;
                return false;
            }
            return true;
        }
        public static void StopTargetingPrefix(Verb ___targetingVerb)
        {
            if (___targetingVerb != null&& ___targetingVerb.verbTracker != null && ___targetingVerb.verbTracker.directOwner != null)
            {
                CompEquippable compEquip = ___targetingVerb.DirectOwner as CompEquippable;
                if (compEquip != null)
                {
                    Comp_VerbSaveable compVerbSave = compEquip.parent.GetComp<Comp_VerbSaveable>();
                    if (compVerbSave != null)
                    {
                        compVerbSave.tempVerb = null;
                    }
                }          
            }
        }

        public static bool CreateVerbTargetCommandPrefix(ref Command_VerbTarget __result, Thing ownerThing, Verb verb)
        {
            Command_VerbTarget command_VerbTarget = new Command_VerbTarget();
            VerbProperties_Custom verbProps = verb.verbProps as VerbProperties_Custom;
            if (verbProps != null)
            {
                command_VerbTarget.defaultDesc = verbProps.desc;
                command_VerbTarget.defaultLabel = verbProps.label;
                Comp_VerbSaveable comp_VerbSaveable = ownerThing.TryGetComp<Comp_VerbSaveable>();
                if (comp_VerbSaveable != null && comp_VerbSaveable.currentVerb == verb)
                {
                    command_VerbTarget.icon = currentCommandTexture;
                }
                else
                {
                    command_VerbTarget.icon = verbProps.texture;
                }
            }
            else
            {
                command_VerbTarget.icon = ownerThing.def.uiIcon;
                command_VerbTarget.defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst();
            }
            command_VerbTarget.iconAngle = ownerThing.def.uiIconAngle;
            command_VerbTarget.iconOffset = ownerThing.def.uiIconOffset;
            command_VerbTarget.tutorTag = "VerbTarget";
            command_VerbTarget.verb = verb;
            if (verb.caster.Faction != Faction.OfPlayer)
            {
                command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
            }
            else if (verb.CasterIsPawn)
            {
                if (verb.CasterPawn.story.WorkTagIsDisabled(WorkTags.Violent))
                {
                    command_VerbTarget.Disable("IsIncapableOfViolence".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                }
                else if (!verb.CasterPawn.drafter.Drafted)
                {
                    command_VerbTarget.Disable("IsNotDrafted".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
                }
            }
            __result = command_VerbTarget;
            return false;
        }
        public static bool PrimaryVerbPrefix(VerbTracker __instance, ref Verb __result)
        {
            Comp_VerbSaveable comp = ((CompEquippable)__instance.directOwner).parent.GetComp<Comp_VerbSaveable>();
            if (comp != null)
            {
                __result = comp.currentVerb;
                if (__result != null)
                {
                    return false;
                }
            }
            return true;
        }

        public static void AdjustedAccuracyPostfix(VerbProperties __instance, ref float __result, RangeCategory cat)
        {
            if (__instance is VerbProperties_Custom)
            {
                switch (cat)
                {
                    case RangeCategory.Touch:
                        __result += __instance.accuracyTouch;
                        break;
                    case RangeCategory.Short:
                        __result += __instance.accuracyShort;
                        break;
                    case RangeCategory.Medium:
                        __result += __instance.accuracyMedium;
                        break;
                    case RangeCategory.Long:
                        __result += __instance.accuracyLong;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public static bool ShotCalculationTipStringPrefix(ref string __result, Thing target)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Find.Selector.SingleSelectedThing != null)
            {
                Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
                Verb verb = null;
                Pawn pawn = singleSelectedThing as Pawn;
                if (pawn != null && pawn != target && pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    Comp_VerbSaveable compsav = pawn.equipment.Primary.GetComp<Comp_VerbSaveable>();
                    if (compsav != null && compsav.tempVerb != null && compsav.tempVerb is Verb_LaunchProjectile)
                    {
                        verb = compsav.tempVerb;
                    }
                    else
                    {
                        verb = pawn.equipment.PrimaryEq.PrimaryVerb;
                    }
                }
                Building_TurretGun building_TurretGun = singleSelectedThing as Building_TurretGun;
                if (building_TurretGun != null && building_TurretGun != target)
                {
                    verb = building_TurretGun.AttackVerb;
                }
                if (verb != null)
                {
                    stringBuilder.Append("ShotBy".Translate(Find.Selector.SingleSelectedThing.LabelShort, Find.Selector.SingleSelectedThing) + ": ");
                    if (verb.CanHitTarget(target))
                    {
                        stringBuilder.Append(ShotReport.HitReportFor(verb.caster, verb, target).GetTextReadout());
                    }
                    else
                    {
                        stringBuilder.AppendLine("CannotHit".Translate());
                    }
                    Pawn pawn2 = target as Pawn;
                    if (pawn2 != null && pawn2.Faction == null && !pawn2.InAggroMentalState)
                    {
                        float manhunterOnDamageChance;
                        if (verb.IsMeleeAttack)
                        {
                            manhunterOnDamageChance = PawnUtility.GetManhunterOnDamageChance(pawn2, 0f, singleSelectedThing);
                        }
                        else
                        {
                            manhunterOnDamageChance = PawnUtility.GetManhunterOnDamageChance(pawn2, singleSelectedThing);
                        }
                        if (manhunterOnDamageChance > 0f)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine(string.Format("{0}: {1}", "ManhunterPerHit".Translate(), manhunterOnDamageChance.ToStringPercent()));
                        }
                    }
                }
            }
            __result = stringBuilder.ToString();
            return false;
        }
    }

    public class Comp_VerbSaveable : ThingComp
    {
        public Verb currentVerb;
        public Verb tempVerb;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            CompEquippable comp = parent.GetComp<CompEquippable>();
            if (currentVerb == null)
            {
                List<Verb> verbs = comp.AllVerbs;
                for (int i = 0; i < verbs.Count; i++)
                {
                    if (verbs[i].verbProps.isPrimary)
                    {
                        currentVerb = verbs[i];
                        return;
                    }
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref currentVerb, "currentVerb");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && currentVerb == null)
            {
                CompEquippable comp = parent.GetComp<CompEquippable>();
                List<Verb> verbs = comp.AllVerbs;
                for (int i = 0; i < verbs.Count; i++)
                {
                    if (verbs[i].verbProps.isPrimary)
                    {
                        currentVerb = verbs[i];
                        return;
                    }
                }
            }
        }
    }


    [StaticConstructorOnStartup]
    public class VerbProperties_Custom : VerbProperties
    {
        public string desc;
        public string texPath;
        public Texture2D texture;

        public VerbProperties_Custom()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (!texPath.NullOrEmpty())
                {
                    texture = ContentFinder<Texture2D>.Get(texPath);
                }
            });
        }


    }*/
}
