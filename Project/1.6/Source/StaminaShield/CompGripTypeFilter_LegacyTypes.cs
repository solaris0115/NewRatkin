namespace NewRatkin
{
    /// <summary>
    /// 리네임 이전 Def XML·다른 모드 패치·<c>Class="NewRatkin.CompProperties_ShieldWeaponIncompatible"</c> 호환.
    /// 동작은 <see cref="CompGripTypeFilter"/>와 동일하며, 림월드 세이브는 보통 ThingDef 기준으로 comp를 재생성하므로
    /// 런타임 인스턴스는 항상 현재 Def에 맞는 타입이 된다.
    /// </summary>
    public class CompProperties_ShieldWeaponIncompatible : CompProperties_GripTypeFilter
    {
        public CompProperties_ShieldWeaponIncompatible()
        {
            this.compClass = typeof(CompShieldWeaponIncompatible);
        }
    }

    public class CompShieldWeaponIncompatible : CompGripTypeFilter
    {
        public new CompProperties_ShieldWeaponIncompatible Props =>
            (CompProperties_ShieldWeaponIncompatible)this.props;
    }
}
