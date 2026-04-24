using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;

namespace NewRatkin
{
    /// <summary>
    /// EquipmentUtility.CanEquip 패치 - 방패와 무기의 호환성 검사
    /// 양방향 검사: 방패 착용 시 무기 호환성, 무기 착용 시 방패 호환성
    /// </summary>
    [StaticConstructorOnStartup]
    public static class EquipmentUtility_CanEquip_Patch
    {
        private static readonly Type patchType = typeof(EquipmentUtility_CanEquip_Patch);

        static EquipmentUtility_CanEquip_Patch()
        {
            Harmony harmonyInstance = new Harmony("com.NewRatkin.rimworld.mod");
            
            // EquipmentUtility.CanEquip의 정확한 시그니처 지정
            // public static bool CanEquip(Thing thing, Pawn pawn, out string cantReason, bool checkBonded = true)
            MethodInfo canEquipMethod = AccessTools.Method(
                typeof(EquipmentUtility),
                "CanEquip",
                new Type[] { typeof(Thing), typeof(Pawn), typeof(string).MakeByRefType(), typeof(bool) }
            );
            
            if (canEquipMethod == null)
            {
                // checkBonded 파라미터 없는 오버로드 시도
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

        /// <summary>
        /// EquipmentUtility.CanEquip Postfix 패치
        /// 착용 가능 여부를 검사한 후 호환성 검사 수행
        /// </summary>
        public static void CanEquip_Postfix(Thing thing, Pawn pawn, ref bool __result, ref string cantReason)
        {
            // 원래 결과가 false면 추가 검사 불필요
            if (!__result)
            {
                return;
            }

            // Pawn이 없거나 장비 시스템이 없으면 통과
            if (pawn == null || pawn.apparel == null || pawn.equipment == null)
            {
                return;
            }

            // 방패 착용 시: 착용 중인 무기와 호환성 검사
            if (thing is Apparel apparelToEquip)
            {
                CompShieldWeaponIncompatible shieldComp = apparelToEquip.GetComp<CompShieldWeaponIncompatible>();
                if (shieldComp != null)
                {
                    // 착용 중인 무기 확인
                    ThingWithComps primaryWeapon = pawn.equipment.Primary;
                    if (primaryWeapon != null)
                    {
                        string reason;
                        if (!shieldComp.TryIsWeaponAllowed(primaryWeapon.def, out reason))
                        {
                            __result = false;
                            // 번역 키가 있으면 사용, 없으면 기본 메시지
                            if (!shieldComp.Props.blockReasonKey.NullOrEmpty())
                            {
                                cantReason = "RK_ShieldWeaponIncompatible_Equip".Translate(
                                    apparelToEquip.Label,
                                    primaryWeapon.Label,
                                    shieldComp.Props.blockReasonKey.Translate()
                                );
                            }
                            else
                            {
                                cantReason = "RK_ShieldWeaponIncompatible_Equip_Default".Translate(
                                    apparelToEquip.Label,
                                    primaryWeapon.Label
                                );
                            }
                            return;
                        }
                    }
                }
            }

            // 무기 착용 시: 착용 중인 방패와 호환성 검사
            if (thing.def.IsWeapon)
            {
                ThingDef weaponDef = thing.def;
                List<Apparel> wornApparel = pawn.apparel.WornApparel;

                for (int i = 0; i < wornApparel.Count; i++)
                {
                    Apparel wornShield = wornApparel[i];
                    CompShieldWeaponIncompatible shieldComp = wornShield.GetComp<CompShieldWeaponIncompatible>();
                    if (shieldComp != null)
                    {
                        string reason;
                        if (!shieldComp.TryIsWeaponAllowed(weaponDef, out reason))
                        {
                            __result = false;
                            // 번역 키가 있으면 사용, 없으면 기본 메시지
                            if (!shieldComp.Props.blockReasonKey.NullOrEmpty())
                            {
                                cantReason = "RK_WeaponShieldIncompatible_Equip".Translate(
                                    weaponDef.label,
                                    wornShield.Label,
                                    shieldComp.Props.blockReasonKey.Translate()
                                );
                            }
                            else
                            {
                                cantReason = "RK_WeaponShieldIncompatible_Equip_Default".Translate(
                                    weaponDef.label,
                                    wornShield.Label
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

