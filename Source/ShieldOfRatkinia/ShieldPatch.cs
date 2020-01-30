using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using Harmony;
using Verse.AI.Group;
using RimWorld.Planet;

namespace NewRatkin
{

    [StaticConstructorOnStartup]
    public static class ShieldPatch
    {
        private static readonly Type patchType = typeof(ShieldPatch);
        static ShieldPatch()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("com.NewRatkin.rimworld.mod");
            harmonyInstance.Patch(AccessTools.Method(typeof(WorkGiver_HunterHunt), "HasShieldAndRangedWeapon"), new HarmonyMethod(patchType, "HasShieldAndRangedWeaponPrefix"));
            harmonyInstance.Patch(AccessTools.Property(typeof(Alert_ShieldUserHasRangedWeapon), "ShieldUsersWithRangedWeapon").GetGetMethod(true), new HarmonyMethod(patchType, "ShieldUsersWithRangedWeaponPrefix"));
        } 
        public static bool ShieldUsersWithRangedWeaponPrefix(ref IEnumerable<Pawn> __result)
        {
            __result = ShieldUsersWithRangedWeapon();
            return false;
        }
        public static IEnumerable<Pawn> ShieldUsersWithRangedWeapon()
        {
            foreach (Pawn p in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
                {
                    List<Apparel> ap = p.apparel.WornApparel;
                    for (int i = 0; i < ap.Count; i++)
                    {
                        if (ap[i] is ShieldBelt || ap[i] is Shield)
                        {
                            yield return p;
                            break;
                        }
                    }
                }
            }
            yield break;
        }
        public static bool HasShieldAndRangedWeaponPrefix(ref bool __result, Pawn p)
        {
            if (p.equipment.Primary != null && p.equipment.Primary.def.IsWeaponUsingProjectiles)
            {
                List<Apparel> wornApparel = p.apparel.WornApparel;
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    if (wornApparel[i] is ShieldBelt || wornApparel[i] is Shield)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }
    }
}