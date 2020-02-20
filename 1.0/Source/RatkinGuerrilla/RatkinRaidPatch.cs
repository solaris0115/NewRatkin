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
using Harmony;
using Verse.AI.Group;
using RimWorld.Planet;

namespace NewRatkin.RatkinGuerrilla
{
    /*
    [StaticConstructorOnStartup]
    public static class RatkinRaidPatch
    {
        private static readonly Type patchType = typeof(RatkinRaidPatch);
        static RatkinRaidPatch()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("com.NewRatkin.rimworld.mod");
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "Init"), new HarmonyMethod(patchType, "InitPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "UpdateAllDuties"), null, new HarmonyMethod(patchType, "UpdateAllDutiesPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "LordToilTick"), null, new HarmonyMethod(patchType, "LordToilTickPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "Cleanup"), null, new HarmonyMethod(patchType, "CleanupPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "VoluntaryJoinDutyHookFor"), null, new HarmonyMethod(patchType, "VoluntaryJoinDutyHookForPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "Notify_PawnLost"), null, new HarmonyMethod(patchType, "Notify_PawnLostPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "Notify_ReachedDutyLocation"), null, new HarmonyMethod(patchType, "Notify_ReachedDutyLocationPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "Notify_ConstructionFailed"), null, new HarmonyMethod(patchType, "Notify_ConstructionFailedPrefix", null));
            harmonyInstance.Patch(AccessTools.Method(typeof(LordToil_DefendBomb), "AddFailCondition"), null, new HarmonyMethod(patchType, "AddFailConditionPrefix", null));
        }
        public static void InitPrefix()
        {
            Log.Message("Init");
        }
        public static void UpdateAllDutiesPrefix()
        {
            Log.Message("UpdateAllDuties");
        }
        public static void LordToilTickPrefix()
        {
            Log.Message("LordToilTick");
        }
        public static void CleanupPrefix()
        {
            Log.Message("Cleanup");
        }
        public static void VoluntaryJoinDutyHookForPrefix()
        {
            Log.Message("VoluntaryJoinDutyHookFor");
        }
        public static void Notify_PawnLostPrefix()
        {
            Log.Message("Notify_PawnLost");
        }
        public static void Notify_ReachedDutyLocationPrefix()
        {
            Log.Message("Notify_ReachedDutyLocation");
        }
        public static void Notify_ConstructionFailedPrefix()
        {
            Log.Message("Notify_ConstructionFailed");
        }
        public static void AddFailConditionPrefix()
        {
            Log.Message("AddFailCondition");
        }
    }*/
}
