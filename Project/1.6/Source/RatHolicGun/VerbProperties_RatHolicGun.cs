using RimWorld;
using System.Collections.Generic;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// RatHolic Gun용 Verb 속성
    /// </summary>
    public class VerbProperties_RatHolicGun : VerbProperties
    {
        /// <summary>
        /// Spooling 효과를 위한 HediffDef 리스트
        /// XML에서 설정 가능: 
        /// &lt;hediffDefs&gt;
        ///   &lt;li&gt;RK_Hediff_RatHolicGunSpooling&lt;/li&gt;
        ///   &lt;li&gt;RK_Hediff_RatHolicGunSpoolingAim&lt;/li&gt;
        /// &lt;/hediffDefs&gt;
        /// </summary>
        public List<HediffDef> hediffDefs;

        /// <summary>
        /// 최대 중첩 수
        /// XML에서 설정 가능: &lt;maxStacks&gt;6&lt;/maxStacks&gt;
        /// </summary>
        public int maxStacks = 5;

        /// <summary>
        /// 사격 스킬 경험치 배율 (코어 Verb_Shoot: 적 Pawn 기준 170×AdjustedFullCycleTime에 곱함).
        /// 고연사로 사이클이 짧아도 기본식과 동일 계수라 초당 경험치가 과하면 1 미만으로 낮춤.
        /// </summary>
        public float shootingXpFactor = 1f;
    }
}

