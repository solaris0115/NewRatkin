using RimWorld;
using System;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// RatHolic Gun용 커스텀 Verb - 매 발사마다 재장전 속도 감소 Hediff 부여 (burstShotCount로 나눠서 증가)
    /// </summary>
    public class Verb_RatHolicGun : Verb_Shoot
    {
        /// <summary>
        /// VerbProperties에서 속성 가져오기
        /// </summary>
        private VerbProperties_RatHolicGun RatHolicGunProps => 
            this.verbProps as VerbProperties_RatHolicGun;

        /// <summary>
        /// Verb_Shoot.WarmupComplete는 조준 종료 시 170/20×AdjustedFullCycleTime 만큼 Learn한다.
        /// base를 호출하면 그대로 들어가므로, Verb_LaunchProjectile과 동일한 전처리 후 배율만 곱해 Learn한다.
        /// </summary>
        public override void WarmupComplete()
        {
            this.burstShotsLeft = this.ShotsPerBurst;
            this.state = VerbState.Bursting;
            this.TryCastNextBurstShot();

            BattleLog battleLog = Find.BattleLog;
            Thing caster = this.caster;
            Thing target = this.currentTarget.HasThing ? this.currentTarget.Thing : null;
            ThingWithComps equipmentSource = base.EquipmentSource;
            battleLog.Add(new BattleLogEntry_RangedFire(
                caster,
                target,
                equipmentSource != null ? equipmentSource.def : null,
                this.Projectile,
                this.ShotsPerBurst > 1));

            Pawn pawn = this.currentTarget.Thing as Pawn;
            if (pawn != null && !pawn.Downed && !pawn.IsColonyMech && this.CasterIsPawn && this.CasterPawn.skills != null)
            {
                float num = pawn.HostileTo(this.caster) ? 170f : 20f;
                float num2 = this.verbProps.AdjustedFullCycleTime(this, this.CasterPawn);
                float factor = RatHolicGunProps != null ? RatHolicGunProps.shootingXpFactor : 1f;
                this.CasterPawn.skills.Learn(SkillDefOf.Shooting, num * num2 * factor, false, false);
            }
        }

        protected override bool TryCastShot()
        {
            // 기본 발사 로직 실행
            bool shotSuccess = base.TryCastShot();

            // 매 발사마다 Hediff 증가 (burstShotCount로 나눠서 증가)
            if (shotSuccess && this.CasterIsPawn && this.CasterPawn != null)
            {
                AddSpoolingHediff(this.CasterPawn);
            }

            return shotSuccess;
        }

        /// <summary>
        /// Spooling Hediff 추가 또는 Severity 증가
        /// 매 발사마다 1스택을 burstShotCount로 나눈 만큼 증가
        /// 예: 20발 버스트면 각 발사마다 0.2/20 = 0.01 Severity 증가
        /// Severity: 0.2 = 1스택, 0.4 = 2스택, ..., 1.2 = 6스택
        /// 모든 HediffDef 리스트를 순회하며 각각 처리
        /// </summary>
        private void AddSpoolingHediff(Pawn pawn)
        {
            VerbProperties_RatHolicGun props = RatHolicGunProps;
            if (props?.hediffDefs == null || pawn?.health == null)
            {
                return;
            }

            int maxStacks = props.maxStacks;
            int burstShotCount = this.BurstShotCount;

            // 모든 HediffDef를 순회하며 처리
            foreach (HediffDef hediffDef in props.hediffDefs)
            {
                if (hediffDef == null)
                {
                    continue;
                }

                float maxSeverity = hediffDef.maxSeverity;

                // 한 스택의 Severity 값
                float severityPerStack = maxSeverity / maxStacks;
                // 매 발사마다 증가할 Severity (burstShotCount로 나눔)
                float severityPerShot = severityPerStack / burstShotCount;

                // 기존 Hediff 확인
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);

                if (hediff == null)
                {
                    // Hediff가 없으면 새로 추가
                    hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(hediff);
                    hediff.Severity = severityPerShot; // 첫 발사분
                }
                else
                {
                    // Hediff가 있으면 Severity 증가 (매 발사마다 증가)
                    // 최대 Severity까지 증가 가능
                    if (hediff.Severity < maxSeverity)
                    {
                        hediff.Severity = System.Math.Min(maxSeverity, hediff.Severity + severityPerShot);
                    }
                }
            }
        }
    }
}

