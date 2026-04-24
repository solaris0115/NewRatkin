using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// RatHolic Gun Spooling 효과를 위한 StatPart - RangedWeapon_Cooldown에 직접 적용
    /// Hediff의 RK_Stat_RangeCoolDownMultiplier statFactors를 읽어서 RangedWeapon_Cooldown에 배율 적용
    /// </summary>
    public class StatPart_RatHolicGunSpooling_Cooldown_Multiplier : StatPart
    {

        /// <summary>
        /// 무기에서 Pawn 찾기
        /// </summary>
        private Pawn GetPawnFromThing(Thing thing)
        {
            // Pawn인 경우 그대로 반환
            if (thing is Pawn pawn)
            {
                return pawn;
            }

            // 무기인 경우 소유자 찾기
            if (thing is ThingWithComps weapon)
            {
                // ParentHolder를 통해 Pawn 찾기
                IThingHolder parentHolder = weapon.ParentHolder;
                if (parentHolder is Pawn_EquipmentTracker equipmentTracker)
                {
                    return equipmentTracker.pawn;
                }
                
                // holdingOwner를 통해 Pawn 찾기
                if (weapon.holdingOwner != null)
                {
                    IThingHolder owner = weapon.holdingOwner.Owner;
                    if (owner is Pawn_EquipmentTracker equipmentTracker2)
                    {
                        return equipmentTracker2.pawn;
                    }
                    if (owner is Pawn pawn2)
                    {
                        return pawn2;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Hediff에서 직접 statFactors 가져오기
        /// HediffStatsUtility.GetStatFactorForSeverity를 사용하여 statFactors 적용
        /// </summary>
        private float GetCooldownMultiplierFromHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return 1f;
            }

            float multiplier = 1f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff.def == RatkinHediffDefOf.RK_Hediff_RatHolicGunSpooling)
                {
                    HediffStage curStage = hediff.CurStage;
                    if (curStage != null && RatkinStatDefOf.RK_Stat_RangeCoolDownMultiplier != null)
                    {
                        // HediffStatsUtility.GetStatFactorForSeverity를 사용하여 statFactors 가져오기
                        float factor = HediffStatsUtility.GetStatFactorForSeverity(
                            RatkinStatDefOf.RK_Stat_RangeCoolDownMultiplier, 
                            curStage, 
                            pawn, 
                            hediff.Severity
                        );
                        
                        // 배율 곱하기 (1.0이면 변경 없음)
                        if (Math.Abs(factor - 1f) > 1E-45f)
                        {
                            multiplier *= factor;
                        }
                    }
                }
            }

            return multiplier;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            // RK_Stat_RangeCoolDownMultiplier 스탯 확인
            if (RatkinStatDefOf.RK_Stat_RangeCoolDownMultiplier == null)
            {
                return;
            }

            // Thing이 없으면 무시
            if (!req.HasThing)
            {
                return;
            }

            // Pawn 찾기
            // 먼저 req.Pawn 확인 (StatRequest.For(Thing, Pawn)으로 생성된 경우)
            Pawn pawn = req.Pawn;
            
            // req.Pawn이 없으면 Thing에서 Pawn 찾기
            if (pawn == null)
            {
                // Thing 자체가 Pawn인지 확인
                if (req.Thing is Pawn pawnThing)
                {
                    pawn = pawnThing;
                }
                else
                {
                    // 무기인 경우 소유자(Pawn) 찾기
                    pawn = GetPawnFromThing(req.Thing);
                }
            }

            // Pawn을 찾지 못했으면 무시
            if (pawn == null)
            {
                return;
            }

            // Hediff에서 직접 statFactors 가져오기 (배율: 0.1 = 10%, 0.9 = 90% 등)
            float multiplier = GetCooldownMultiplierFromHediff(pawn);
            
            // 값이 1.0이면 무시 (변경 없음)
            if (Math.Abs(multiplier - 1f) < 1E-45f)
            {
                return;
            }

            // RangedWeapon_Cooldown에 배율 적용
            // multiplier를 곱함 (예: 1.5 × 0.1 = 0.15)
            // 최소값 0.01로 제한
            val = System.Math.Max(0.01f, val * multiplier);
        }

        public override string ExplanationPart(StatRequest req)
        {
            // RK_Stat_RangeCoolDownMultiplier 스탯 확인
            if (RatkinStatDefOf.RK_Stat_RangeCoolDownMultiplier == null)
            {
                return null;
            }

            // Thing이 없으면 설명 없음
            if (!req.HasThing)
            {
                return null;
            }

            // Pawn 찾기
            // 먼저 req.Pawn 확인 (StatRequest.For(Thing, Pawn)으로 생성된 경우)
            Pawn pawn = req.Pawn;
            
            // req.Pawn이 없으면 Thing에서 Pawn 찾기
            if (pawn == null)
            {
                // Thing 자체가 Pawn인지 확인
                if (req.Thing is Pawn pawnThing)
                {
                    pawn = pawnThing;
                }
                else
                {
                    // 무기인 경우 소유자(Pawn) 찾기
                    pawn = GetPawnFromThing(req.Thing);
                }
            }

            // Pawn을 찾지 못했으면 설명 없음
            if (pawn == null)
            {
                return null;
            }

            // Hediff에서 직접 statFactors 가져오기
            float multiplier = GetCooldownMultiplierFromHediff(pawn);
            
            // 값이 1.0이면 설명 없음 (변경 없음)
            if (Math.Abs(multiplier - 1f) < 1E-45f)
            {
                return null;
            }

            // 설명 반환
            return $"RatHolic Gun Spooling: ×{multiplier:F2} cooldown";
        }
    }
}

