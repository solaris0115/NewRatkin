using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;

namespace NewRatkin
{
    public class StockGenerator_Relic : StockGenerator
    {
        [NoTranslate]
        public string tradeTag;

        [NoTranslate]
        public string weaponTag;

        [NoTranslate]
        public string apparelTag;

        private static Ideo tempIdeo = null;

        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            if (!ModsConfig.IdeologyActive)
            {
                yield break;
            }

            // 태그에 맞는 ThingDef 찾기
            List<ThingDef> candidates = new List<ThingDef>();
            
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (!def.tradeability.TraderCanSell() || !def.PlayerAcquirable)
                {
                    continue;
                }

                bool matches = false;

                // TradeTag 체크
                if (!string.IsNullOrEmpty(this.tradeTag))
                {
                    if (def.tradeTags != null && def.tradeTags.Contains(this.tradeTag))
                    {
                        matches = true;
                    }
                }

                // WeaponTag 체크 (무기인 경우)
                if (!matches && !string.IsNullOrEmpty(this.weaponTag))
                {
                    if (def.IsWeapon && def.weaponTags != null && def.weaponTags.Contains(this.weaponTag))
                    {
                        matches = true;
                    }
                }

                // ApparelTag 체크 (갑옷인 경우)
                if (!matches && !string.IsNullOrEmpty(this.apparelTag))
                {
                    if (def.IsApparel && def.apparel != null && def.apparel.tags != null && def.apparel.tags.Contains(this.apparelTag))
                    {
                        matches = true;
                    }
                }

                if (matches && def.techLevel <= this.maxTechLevelGenerate)
                {
                    candidates.Add(def);
                }
            }

            if (candidates.Count == 0)
            {
                yield break;
            }

            // 임시 Ideo 생성 (한 번만)
            if (tempIdeo == null)
            {
                tempIdeo = IdeoGenerator.GenerateIdeo(new IdeoGenerationParms(
                    null, // Faction 없음
                    false, // forceNoExpansionIdeo
                    null, // disallowedPrecepts
                    null, // disallowedMemes
                    null, // forcedMemes
                    false, // classicMode
                    false, // fluid
                    false, // allowNonPlayerFactionIdeos
                    false, // allowPlayerFactionIdeos
                    "", // ideoName
                    null, // styles
                    null, // deityPresets
                    false, // hiddenIdeo
                    "", // ideoDescription
                    false  // requiredPreceptsOnly
                ));
            }

            // Precept_Relic 생성
            PreceptDef relicPreceptDef = PreceptDefOf.IdeoRelic;
            if (relicPreceptDef == null)
            {
                yield break;
            }

            // countRange에 따라 여러 개 생성
            int count = this.countRange.RandomInRange;
            
            for (int i = 0; i < count; i++)
            {
                // 무작위로 ThingDef 선택
                ThingDef chosenDef = candidates.RandomElement();

                Precept_Relic relicPrecept = (Precept_Relic)PreceptMaker.MakePrecept(relicPreceptDef);
                
                // ideo를 먼저 설정 (ThingDef setter에서 ideo를 참조하므로)
                relicPrecept.ideo = tempIdeo;
                
                // ThingDef 설정 (이제 ideo가 설정되어 있으므로 Notify_ThingDefSet()에서 안전하게 사용 가능)
                relicPrecept.ThingDef = chosenDef;  // setter 사용
                relicPrecept.SetRandomStuff();
                
                // Precept를 이데올로기에 추가 (효과가 작동하려면 필요)
                tempIdeo.AddPrecept(relicPrecept, false); // 이미 ideo와 ThingDef가 설정되어 있으므로 init=false
                relicPrecept.RegenerateName();

                // 유물 생성
                Thing relic = relicPrecept.GenerateRelic();
                if (relic != null)
                {
                    yield return relic;
                }
            }
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            // 유물은 일반 거래 불가
            return false;
        }
    }
}
