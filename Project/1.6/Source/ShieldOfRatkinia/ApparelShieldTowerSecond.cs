using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
    /// <summary>
    /// 방패 thingClass. 블록 판정은 CompShieldDeflect에서 처리.
    /// 이 클래스는 각도 판정·방어 스탯 조회 공유 메서드를 제공한다.
    /// </summary>
    public class ApparelShieldTowerSecond : Apparel
    {
        private CompProperties_ShieldFaceDirection FaceDirectionProps =>
            this.GetComp<CompShieldFaceDirection>()?.Props as CompProperties_ShieldFaceDirection;

        /// <summary>DamageInfo의 armorCategory에 따라 방패 방어 스탯 반환.</summary>
        internal static float GetArmorRatingForDamage(ApparelShieldTowerSecond shield, DamageInfo dinfo)
        {
            switch (dinfo.Def.armorCategory)
            {
                case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Sharp:
                    return shield.GetStatValue(RatkinStatDefOf.RK_Stat_Shield_Sharp);
                case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Blunt:
                    return shield.GetStatValue(RatkinStatDefOf.RK_Stat_Shield_Blunt);
                case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Heat:
                    return shield.GetStatValue(RatkinStatDefOf.RK_Stat_Shield_Heat);
                default:
                    return 0f;
            }
        }

        /// <summary>입사각이 방패 허용 각도 이내인지 판정.</summary>
        internal static bool IsAngleWithinDeflectRange(ApparelShieldTowerSecond shield, Pawn pawn, DamageInfo dinfo)
        {
            float attackerAngle = dinfo.Angle + 180f;
            if (attackerAngle >= 360f) attackerAngle -= 360f;
            if (attackerAngle < 0f) attackerAngle += 360f;

            float defenderAngle = pawn.Rotation.AsAngle;
            float angleDiff = defenderAngle - attackerAngle;

            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;

            float deflectAngleFull = shield.GetStatValue(RatkinStatDefOf.RK_Stat_DeflectAngle);
            if (deflectAngleFull <= 0f)
                deflectAngleFull = shield.FaceDirectionProps?.deflectAngleHalf ?? 140f;

            float deflectAngleHalf = deflectAngleFull * 0.5f;

            return angleDiff >= -deflectAngleHalf && angleDiff <= deflectAngleHalf;
        }

        /// <summary>Pawn이 방패 블록을 할 수 있는 상태인지 (기절/화염/정신붕괴 시 false).</summary>
        internal static bool PawnCanDeflectWithShield(Pawn pawn)
        {
            if (pawn?.stances?.stunner == null) return false;
            if (pawn.stances.stunner.Stunned) return false;
            if (pawn.IsBurning()) return false;
            if (pawn.InMentalState) return false;
            return true;
        }
    }
}
