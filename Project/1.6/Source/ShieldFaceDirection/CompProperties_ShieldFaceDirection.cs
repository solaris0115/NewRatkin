using Verse;

namespace NewRatkin
{
    public class CompProperties_ShieldFaceDirection : CompProperties
    {
        public CompProperties_ShieldFaceDirection()
        {
            compClass = typeof(CompShieldFaceDirection);
        }

        public int cooldownTicks = 300;

        /// <summary>방향 고정 시 원거리 공격 차단 각도 (좌우 각도, 기본 70 = ±70도)</summary>
        public float deflectAngleHalf = 70f;
    }
}
