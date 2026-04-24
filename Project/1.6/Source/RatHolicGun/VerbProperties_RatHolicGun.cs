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
    }
}

