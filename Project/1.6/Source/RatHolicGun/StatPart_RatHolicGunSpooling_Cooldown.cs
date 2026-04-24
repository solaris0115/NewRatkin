using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// RatHolic Gun Spooling 효과를 위한 StatPart - RangedWeapon_Cooldown에 직접 적용
    /// Hediff의 RK_Stat_RangeCoolDown offset을 읽어서 RangedWeapon_Cooldown에서 직접 감소
    /// </summary>
    public class StatPart_RatHolicGunSpooling_Cooldown : StatPart
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
        /// Hediff에서 직접 statOffset 가져오기
        /// </summary>
        private float GetCooldownReductionFromHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return 0f;
            }

            float cooldownReduction = 0f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff.def == RatkinHediffDefOf.RK_Hediff_RatHolicGunSpooling)
                {
                    HediffStage curStage = hediff.CurStage;
                    if (curStage != null && curStage.statOffsets != null && RatkinStatDefOf.RK_Stat_RangeCoolDown != null)
                    {
                        cooldownReduction += curStage.statOffsets.GetStatOffsetFromList(RatkinStatDefOf.RK_Stat_RangeCoolDown);
                    }
                }
            }

            return cooldownReduction;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            // RK_Stat_RangeCoolDown 스탯 확인
            if (RatkinStatDefOf.RK_Stat_RangeCoolDown == null)
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

            // Hediff에서 직접 statOffset 가져오기 (음수 값: -0.2, -0.4 등)
            float cooldownReduction = GetCooldownReductionFromHediff(pawn);
            
            // 값이 0이면 무시
            if (cooldownReduction == 0f)
            {
                return;
            }

            // RangedWeapon_Cooldown에서 직접 감소
            // cooldownReduction은 이미 음수이므로 더하면 감소됨 (예: 1.5 + (-0.2) = 1.3)
            // 최소값 0.01로 제한
            val = System.Math.Max(0.01f, val + cooldownReduction);
        }

        public override string ExplanationPart(StatRequest req)
        {
            // RK_Stat_RangeCoolDown 스탯 확인
            if (RatkinStatDefOf.RK_Stat_RangeCoolDown == null)
            {
                return null;
            }

            // 구간 2: Thing이 없으면 설명 없음
            if (!req.HasThing)
            {
                return null;
            }

            // 구간 3: Pawn 찾기
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

            // 구간 4: Pawn을 찾지 못했으면 설명 없음
            if (pawn == null)
            {
                return null;
            }

            // 구간 5: Hediff에서 직접 statOffset 가져오기
            float cooldownReduction = GetCooldownReductionFromHediff(pawn);
            
            // 구간 6: 값이 0이면 설명 없음
            if (cooldownReduction == 0f)
            {
                return null;
            }

            // 구간 7: 설명 반환
            return $"RatHolic Gun Spooling: -{cooldownReduction:F2}s cooldown";
        }
    }
}

