using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;

namespace NewRatkin
{
    /// <summary>
    /// EquipmentUtility.CanEquip 패치 - GripTypeFilter comp가 부착된 장비와 무기의 호환성 검사.
    /// 양방향: 장비 착용 시 무기 호환성, 무기 착용 시 착용 중 장비 호환성.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class EquipmentUtility_CanEquip_Patch
    {
        private static readonly Type patchType = typeof(EquipmentUtility_CanEquip_Patch);

        static EquipmentUtility_CanEquip_Patch()
        {
            Harmony harmonyInstance = new Harmony("com.NewRatkin.rimworld.mod");
            
            MethodInfo canEquipMethod = AccessTools.Method(
                typeof(EquipmentUtility),
                "CanEquip",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }
            );
            
            if (canEquipMethod == null)
            {
                canEquipMethod = AccessTools.Method(
                    typeof(EquipmentUtility),
                    "CanEquip",
                    new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType() }
                );
            }
            
            if (canEquipMethod != null)
            {
                harmonyInstance.Patch(
                    canEquipMethod,
                    null,
                    new HarmonyMethod(patchType, nameof(CanEquip_Postfix))
                );
            }
            else
            {
                RatkinLimitedLog.Error(RatkinLogKeys.Equipment_CanEquipPatchMissingMethod, "[EquipmentUtility_CanEquip_Patch] Failed to find EquipmentUtility.CanEquip method");
            }
        }

        public static void CanEquip_Postfix(Thing thing, Pawn pawn, ref bool __result, ref string cantReason)
        {
            if (!__result)
            {
                return;
            }

            if (pawn == null || pawn.apparel == null || pawn.equipment == null)
            {
                return;
            }

            // 장비(방패·배너 등) 착용 시: 착용 중인 무기와 호환성 검사
            if (thing is Apparel apparelToEquip)
            {
                CompGripTypeFilter filterComp = apparelToEquip.GetComp<CompGripTypeFilter>();
                if (filterComp != null)
                {
                    ThingWithComps primaryWeapon = pawn.equipment.Primary;
                    if (primaryWeapon != null)
                    {
                        string reason;
                        if (!filterComp.TryIsWeaponAllowed(primaryWeapon.def, out reason))
                        {
                            __result = false;
                            if (!filterComp.Props.blockReasonKey.NullOrEmpty())
                            {
                                cantReason = "RK_GripIncompatible_Equip".Translate(
                                    apparelToEquip.Label,
                                    primaryWeapon.Label,
                                    filterComp.Props.blockReasonKey.Translate()
                                );
                            }
                            else
                            {
                                cantReason = "RK_GripIncompatible_Equip_Default".Translate(
                                    apparelToEquip.Label,
                                    primaryWeapon.Label
                                );
                            }
                            return;
                        }
                    }
                }
            }

            // 무기 착용 시: 착용 중인 장비(방패·배너 등)와 호환성 검사
            if (thing.def.IsWeapon)
            {
                ThingDef weaponDef = thing.def;
                List<Apparel> wornApparel = pawn.apparel.WornApparel;

                for (int i = 0; i < wornApparel.Count; i++)
                {
                    Apparel wornItem = wornApparel[i];
                    CompGripTypeFilter filterComp = wornItem.GetComp<CompGripTypeFilter>();
                    if (filterComp != null)
                    {
                        string reason;
                        if (!filterComp.TryIsWeaponAllowed(weaponDef, out reason))
                        {
                            __result = false;
                            if (!filterComp.Props.blockReasonKey.NullOrEmpty())
                            {
                                cantReason = "RK_WeaponGripIncompatible_Equip".Translate(
                                    weaponDef.label,
                                    wornItem.Label,
                                    filterComp.Props.blockReasonKey.Translate()
                                );
                            }
                            else
                            {
                                cantReason = "RK_WeaponGripIncompatible_Equip_Default".Translate(
                                    weaponDef.label,
                                    wornItem.Label
                                );
                            }
                            return;
                        }
                    }
                }
            }
        }
    }
}
