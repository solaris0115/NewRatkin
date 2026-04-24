using RimWorld;
using System.Collections.Generic;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// 장착 시 Hediff 부여, 해제 시 Hediff 제거하는 Comp 속성
    /// 공용 컴포넌트로 다양한 무기/장비에서 사용 가능
    /// </summary>
    public class CompProperties_EquipableHediff : CompProperties
    {
        /// <summary>
        /// 장착 시 부여할 HediffDef 리스트
        /// XML에서 설정 가능:
        /// &lt;hediffDefs&gt;
        ///   &lt;li&gt;RK_Hediff_RatHolicGun&lt;/li&gt;
        /// &lt;/hediffDefs&gt;
        /// </summary>
        public List<HediffDef> hediffDefs;

        public CompProperties_EquipableHediff()
        {
            this.compClass = typeof(Comp_EquipableHediff);
        }
    }

    /// <summary>
    /// 장착 시 Hediff 부여, 해제 시 Hediff 제거하는 Comp
    /// </summary>
    public class Comp_EquipableHediff : ThingComp
    {
        public CompProperties_EquipableHediff Props => 
            (CompProperties_EquipableHediff)this.props;

        /// <summary>
        /// 장착 시 Hediff 부여
        /// </summary>
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            AddHediffs(pawn);
        }

        /// <summary>
        /// 해제 시 Hediff 제거
        /// </summary>
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveHediffs(pawn);
        }

        /// <summary>
        /// 모든 Hediff 부여
        /// </summary>
        private void AddHediffs(Pawn pawn)
        {
            if (Props?.hediffDefs == null || pawn?.health == null)
            {
                return;
            }

            foreach (HediffDef hediffDef in Props.hediffDefs)
            {
                if (hediffDef == null)
                {
                    continue;
                }

                // 이미 부여되어 있는지 확인
                Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (existingHediff == null)
                {
                    // Hediff 부여
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                }
            }
        }

        /// <summary>
        /// 모든 Hediff 제거
        /// </summary>
        private void RemoveHediffs(Pawn pawn)
        {
            if (Props?.hediffDefs == null || pawn?.health?.hediffSet == null)
            {
                return;
            }

            foreach (HediffDef hediffDef in Props.hediffDefs)
            {
                if (hediffDef == null)
                {
                    continue;
                }

                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }

    /// <summary>
    /// RatHolic Gun 통합 HediffComp 속성
    /// 타겟 변경/삭제 시 제거할 Hediff와 사격 중이 아닐 때 제거할 Hediff를 관리
    /// </summary>
    public class HediffCompProperties_RatHolicGun : HediffCompProperties
    {
        /// <summary>
        /// 타겟 변경 또는 삭제 시 제거할 HediffDef 리스트
        /// 매 프레임 체크
        /// </summary>
        public List<HediffDef> targetChangeRemoveHediffs;

        /// <summary>
        /// 사격 중이 아닐 때 제거할 HediffDef 리스트
        /// longTick 체크
        /// </summary>
        public List<HediffDef> notFiringRemoveHediffs;

        public HediffCompProperties_RatHolicGun()
        {
            this.compClass = typeof(HediffComp_RatHolicGun);
        }
    }

    /// <summary>
    /// RatHolic Gun 통합 HediffComp
    /// 1. 매 프레임: 타겟 변경/삭제 시 특정 Hediff 제거
    /// 2. longTick: 사격 중이 아닐 때 특정 Hediff 제거
    /// </summary>
    public class HediffComp_RatHolicGun : HediffComp
    {

        /// <summary>
        /// Severity 업데이트 간격 (longTick 체크용)
        /// </summary>
        private const int SeverityUpdateInterval = 60;

        /// <summary>
        /// 마지막으로 조준했던 타겟 (타겟 변경 감지용)
        /// </summary>
        private LocalTargetInfo? lastAimingTarget = null;

        /// <summary>
        /// Comp 속성
        /// </summary>
        private HediffCompProperties_RatHolicGun Props => 
            (HediffCompProperties_RatHolicGun)this.props;

        /// <summary>
        /// 관리자 Hediff(RK_Hediff_RatHolicGun)가 제거될 때(무기 해제 등) 회전 축적·조준 동조도 함께 제거.
        /// 해제 시 Comp_EquipableHediff가 관리자만 지우므로, 자식 Hediff는 이 훅에서 정리한다.
        /// </summary>
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Pawn pawn = this.Pawn;
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            RemoveHediffs(pawn, Props?.targetChangeRemoveHediffs);
            RemoveHediffs(pawn, Props?.notFiringRemoveHediffs);
        }

        /// <summary>
        /// 매 프레임마다 호출 - 타겟 변경/삭제 체크
        /// </summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = this.Pawn;
            if (pawn == null)
            {
                return;
            }

            // Pawn이 죽었거나 Spawned가 아니면 모든 관련 Hediff 제거
            if (pawn.Dead || !pawn.Spawned)
            {
                RemoveHediffs(pawn, Props?.targetChangeRemoveHediffs);
                RemoveHediffs(pawn, Props?.notFiringRemoveHediffs);
                return;
            }

            // RatHolic Gun을 장착하고 있는지 확인
            ThingWithComps ratHolicGun = GetRatHolicGun(pawn);
            if (ratHolicGun == null || ratHolicGun.def != RatkinWeaponDefOf.RK_Weapon_RatHolicGun)
            {
                // 무기를 장착하지 않았으면 타겟 변경 제거 목록의 모든 Hediff 제거
                RemoveHediffs(pawn, Props?.targetChangeRemoveHediffs);
                return;
            }

            // 현재 타겟 확인 (조준 중이거나 발사 중일 때)
            LocalTargetInfo? currentAimingTarget = GetCurrentAimingTarget(pawn, ratHolicGun);

            // 재장전 중이면 타겟 변경 체크 안 함 (계속 유지)
            bool isReloading = IsPawnReloading(pawn, ratHolicGun);
            if (isReloading)
            {
                // 재장전 중이면 타겟은 유지하되 변경 체크는 하지 않음
                return;
            }

            // 조준 중이 아니고 발사 중도 아니면 타겟 없음
            // 하지만 lastAimingTarget은 유지하여 다음 조준 시 비교 가능하도록 함
            if (!currentAimingTarget.HasValue)
            {
                // 타겟이 없을 때는 아무것도 하지 않음 (lastAimingTarget 유지)
                return;
            }

            // 타겟이 변경되었는지 확인
            // 보고서 참고: warmupStance.focusTarg를 읽어서 이전 타겟과 비교
            if (lastAimingTarget.HasValue)
            {
                LocalTargetInfo lastTarget = lastAimingTarget.Value;
                LocalTargetInfo currentTarget = currentAimingTarget.Value;

                // LocalTargetInfo의 != 연산자로 타겟 변경 감지
                // (Thing 참조, Cell, Pawn 등을 자동으로 비교)
                if (lastTarget != currentTarget || !currentTarget.IsValid)
                {
                    RemoveHediffs(pawn, Props?.targetChangeRemoveHediffs);
                    // 타겟 변경 후 현재 타겟을 저장 (다음 변경 감지를 위해)
                    lastAimingTarget = currentAimingTarget;
                    return;
                }
            }

            // 현재 타겟 저장
            lastAimingTarget = currentAimingTarget;
        }

        /// <summary>
        /// longTick마다 호출 - 사격 중이 아닐 때 Hediff 제거
        /// </summary>
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);

            Pawn pawn = this.Pawn;
            if (pawn == null)
            {
                return;
            }

            // Pawn이 죽었거나 Spawned가 아니면 모든 관련 Hediff 제거
            if (pawn.Dead || !pawn.Spawned)
            {
                RemoveHediffs(pawn, Props?.targetChangeRemoveHediffs);
                RemoveHediffs(pawn, Props?.notFiringRemoveHediffs);
                return;
            }

            // HediffComp_SeverityModifierBase와 동일하게 IsHashIntervalTick 사용
            if (!pawn.IsHashIntervalTick(SeverityUpdateInterval, delta))
            {
                return;
            }

            // RatHolic Gun을 장착하고 있는지 확인
            ThingWithComps ratHolicGun = GetRatHolicGun(pawn);
            if (ratHolicGun == null || ratHolicGun.def != RatkinWeaponDefOf.RK_Weapon_RatHolicGun)
            {
                // 무기를 장착하지 않았으면 사격 중이 아닐 때 제거 목록의 모든 Hediff 제거
                RemoveHediffs(pawn, Props?.notFiringRemoveHediffs);
                return;
            }

            // 발사 중인지 확인 (조준, 발사, 재장전)
            bool isFiring = IsPawnFiring(pawn, ratHolicGun);

            if (!isFiring)
            {
                // 사격 중이 아니면 사격 중이 아닐 때 제거 목록의 모든 Hediff 제거
                RemoveHediffs(pawn, Props?.notFiringRemoveHediffs);
            }
        }

        /// <summary>
        /// 지정된 HediffDef 리스트의 모든 Hediff를 Pawn에서 제거
        /// </summary>
        private void RemoveHediffs(Pawn pawn, List<HediffDef> hediffDefs)
        {
            if (hediffDefs == null || pawn?.health?.hediffSet == null)
            {
                return;
            }

            foreach (HediffDef hediffDef in hediffDefs)
            {
                if (hediffDef == null)
                {
                    continue;
                }

                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        /// <summary>
        /// Pawn이 RatHolic Gun을 장착하고 있는지 확인하고 무기 반환
        /// </summary>
        private ThingWithComps GetRatHolicGun(Pawn pawn)
        {
            if (pawn?.equipment == null)
            {
                return null;
            }

            foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading)
            {
                if (equipment.def == RatkinWeaponDefOf.RK_Weapon_RatHolicGun)
                {
                    return equipment;
                }
            }

            return null;
        }

        /// <summary>
        /// Pawn이 현재 발사 중인지 확인
        /// 1. 조준 중 (Stance_Warmup)
        /// 2. 발사 중 (VerbState.Bursting)
        /// 3. 재장전 중 (Stance_Cooldown)
        /// </summary>
        private bool IsPawnFiring(Pawn pawn, ThingWithComps ratHolicGun)
        {
            if (pawn?.stances == null)
            {
                return false;
            }

            Stance curStance = pawn.stances.curStance;

            // 1. 조준 중인지 확인
            if (curStance is Stance_Warmup warmupStance)
            {
                // RatHolic Gun의 Verb인지 확인
                if (warmupStance.verb != null && warmupStance.verb.EquipmentSource == ratHolicGun)
                {
                    return true;
                }
            }

            // 2. 재장전 중인지 확인 (Stance_Cooldown)
            if (curStance is Stance_Cooldown cooldownStance)
            {
                // RatHolic Gun의 Verb인지 확인
                if (cooldownStance.verb != null && cooldownStance.verb.EquipmentSource == ratHolicGun)
                {
                    return true;
                }
            }

            // 3. 발사 중인지 확인 (VerbState.Bursting)
            CompEquippable compEquippable = ratHolicGun.GetComp<CompEquippable>();
            if (compEquippable != null)
            {
                foreach (Verb verb in compEquippable.AllVerbs)
                {
                    if (verb.state == VerbState.Bursting)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 조준 중이거나 발사 중인 타겟 반환 (조준/발사 중이 아니면 null)
        /// </summary>
        private LocalTargetInfo? GetCurrentAimingTarget(Pawn pawn, ThingWithComps ratHolicGun)
        {
            if (pawn?.stances == null)
            {
                return null;
            }

            Stance curStance = pawn.stances.curStance;

            // 1. 조준 중인지 확인 (Stance_Warmup)
            if (curStance is Stance_Warmup warmupStance)
            {
                // RatHolic Gun의 Verb인지 확인
                if (warmupStance.verb != null && warmupStance.verb.EquipmentSource == ratHolicGun)
                {
                    return warmupStance.focusTarg;
                }
            }

            // 2. 발사 중인지 확인 (VerbState.Bursting) - 발사 중에도 타겟 확인 가능
            CompEquippable compEquippable = ratHolicGun.GetComp<CompEquippable>();
            if (compEquippable != null)
            {
                foreach (Verb verb in compEquippable.AllVerbs)
                {
                    if (verb.state == VerbState.Bursting && verb.EquipmentSource == ratHolicGun)
                    {
                        // 발사 중일 때는 Verb의 CurrentTarget 사용 (public property)
                        LocalTargetInfo currentTarget = verb.CurrentTarget;
                        if (currentTarget.IsValid)
                        {
                            return currentTarget;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Pawn이 현재 재장전 중인지 확인 (발사 중은 제외)
        /// </summary>
        private bool IsPawnReloading(Pawn pawn, ThingWithComps ratHolicGun)
        {
            if (pawn?.stances == null)
            {
                return false;
            }

            Stance curStance = pawn.stances.curStance;

            // 재장전 중인지 확인 (Stance_Cooldown)
            if (curStance is Stance_Cooldown cooldownStance)
            {
                // RatHolic Gun의 Verb인지 확인
                if (cooldownStance.verb != null && cooldownStance.verb.EquipmentSource == ratHolicGun)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

