using Verse;

namespace NewRatkin
{
    /// <summary>
    /// Prototype Pulse Rifle 전용 Verb - Comp_PulseRifleFireMode에서 현재 탄종을 가져와 발사
    /// </summary>
    public class Verb_PulseRifleShoot : Verb_Shoot
    {
        public override ThingDef Projectile
        {
            get
            {
                ThingWithComps equipmentSource = EquipmentSource;
                Comp_PulseRifleFireMode comp = equipmentSource?.GetComp<Comp_PulseRifleFireMode>();
                if (comp != null)
                {
                    return comp.CurrentProjectile;
                }
                return verbProps.defaultProjectile;
            }
        }
    }
}
