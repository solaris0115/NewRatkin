using Verse;

namespace NewRatkin
{
    /// <summary>
    /// BFR 3000 전용 Verb - Comp_BFRAmmoToggle에서 현재 탄종을 가져와 발사
    /// </summary>
    public class Verb_BFRShoot : Verb_Shoot
    {
        /// <summary>
        /// 바닐라 <see cref="Verb.CanHitTarget"/>는 자기 <see cref="Thing"/>일 때 <c>canTargetSelf</c>를 건너뛰어
        /// 다중 선택 시 자기 조준이 깨진다. <see cref="Verb_SectorShot.CanHitTarget"/>와 동일 규칙(발밑 칸은 단일 선택에서만 금지).
        /// </summary>
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            if (caster != null && targ.IsValid && !verbProps.targetParams.canTargetSelf)
            {
                if (targ.HasThing && targ.Thing == caster)
                    return false;
                if (!targ.HasThing && targ.Cell == caster.Position
                    && (Find.Selector == null || Find.Selector.NumSelected <= 1))
                    return false;
            }
            return base.CanHitTarget(targ);
        }

        public override ThingDef Projectile
        {
            get
            {
                ThingWithComps equipmentSource = EquipmentSource;
                Comp_BFRAmmoToggle comp = equipmentSource?.GetComp<Comp_BFRAmmoToggle>();
                if (comp != null)
                {
                    return comp.CurrentProjectile;
                }
                return verbProps.defaultProjectile;
            }
        }
    }
}
