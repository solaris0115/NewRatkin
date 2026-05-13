using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
    public class CompProperties_ShieldDeflect : CompProperties
    {
        /// <summary>완전 블록 시 방패 내구도 손상 비율 (데미지 × 이 값). 기본 0.25%</summary>
        public float durabilityDamageOnBlock = 0.0025f;

        /// <summary>관통 시 방패 내구도 손상 비율 (데미지 × 이 값). 기본 1%</summary>
        public float durabilityDamageOnPenetrate = 0.01f;

        public CompProperties_ShieldDeflect()
        {
            compClass = typeof(CompShieldDeflect);
        }
    }

    public class CompShieldDeflect : ThingComp
    {
        public CompProperties_ShieldDeflect Props => (CompProperties_ShieldDeflect)props;

        private bool _processingDamage = false;

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (_processingDamage) return;

            Pawn pawn = (parent as Apparel)?.Wearer;
            if (pawn == null) return;

            var shield = parent as ApparelShieldTowerSecond;
            if (shield == null) return;

            // 1. 기본 조건
            if (pawn.Dead || pawn.Downed) return;
            if (!pawn.Drafted) return;
            if (!ApparelShieldTowerSecond.PawnCanDeflectWithShield(pawn)) return;
            if (dinfo.Def == null) return;
            if (dinfo.Def.ignoreShields || dinfo.Def == DamageDefOf.EMP) return;

            // 2. 입사각 판정
            if (!ApparelShieldTowerSecond.IsAngleWithinDeflectRange(shield, pawn, dinfo)) return;

            // 3-1. 블락 시도 판정
            float blockChance = pawn.GetStatValue(RatkinStatDefOf.RK_Stat_ShieldBlockChance);
            if (Rand.Value >= blockChance) return;

            // 3-2. 아머 판정
            float armorRating = ApparelShieldTowerSecond.GetArmorRatingForDamage(shield, dinfo);
            float num = Mathf.Max(armorRating - dinfo.ArmorPenetrationInt, 0f);
            bool blocked = Rand.Value < num;

            // 4. 방패 내구도 손상
            float durabilityRatio = blocked ? Props.durabilityDamageOnBlock : Props.durabilityDamageOnPenetrate;
            float durabilityDamage = dinfo.Amount * durabilityRatio;
            if (durabilityDamage > 0f)
            {
                _processingDamage = true;
                try
                {
                    parent.TakeDamage(new DamageInfo(
                        dinfo.Def,
                        durabilityDamage,
                        dinfo.ArmorPenetrationInt,
                        dinfo.Angle,
                        dinfo.Instigator,
                        null,
                        dinfo.Weapon,
                        dinfo.Category,
                        dinfo.IntendedTarget));
                }
                finally
                {
                    _processingDamage = false;
                }
            }

            if (blocked)
            {
                absorbed = true;
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "ShieldBlock".Translate(), 1.9f);
                EffecterDefOf.Deflect_Metal.Spawn().Trigger(pawn, dinfo.Instigator ?? pawn);
            }
        }
    }
}
