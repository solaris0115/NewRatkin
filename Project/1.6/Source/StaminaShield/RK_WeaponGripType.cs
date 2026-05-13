namespace NewRatkin
{
    /// <summary>
    /// 방패 호환 판정·정보 패널 표시용 무기 파지 유형.
    /// </summary>
    public enum RK_WeaponGripType
    {
        OneHand,
        TwoHand,
        /// <summary>양손이면서 방패와 함께 착용 가능한 특수 무기(건랜스 등). TwoHand와 병행 가능.</summary>
        Special
    }
}
