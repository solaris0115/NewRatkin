using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// Prototype Pulse Rifle 발사 모드 토글 Comp 속성
    /// 3점사/단발 모드별 VerbProperties, 탄환, stat offset, 아이콘 경로 정의
    /// </summary>
    public class CompProperties_PulseRifleFireMode : CompProperties
    {
        public ThingDef projectileBurst;
        public ThingDef projectileSingle;

        public float rangeBurst = 31f;
        public float rangeSingle = 31f;
        public int burstShotCountBurst = 3;
        public int burstShotCountSingle = 1;
        public int ticksBetweenBurstShotsBurst = 10;
        public int ticksBetweenBurstShotsSingle = 0;
        public float warmupTimeBurst = 1.5f;
        public float warmupTimeSingle = 2f;

        public List<StatModifier> statOffsetsBurst;
        public List<StatModifier> statOffsetsSingle;

        public string iconPathBurst = "UI/Commands/RK_Icon_BurstShot";
        public string iconPathSingle = "UI/Commands/RK_Icon_Snipe";

        public CompProperties_PulseRifleFireMode()
        {
            compClass = typeof(Comp_PulseRifleFireMode);
        }
    }
}
