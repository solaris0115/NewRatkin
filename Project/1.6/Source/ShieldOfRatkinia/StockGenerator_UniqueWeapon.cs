using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace NewRatkin
{
    public class StockGenerator_UniqueWeapon : StockGenerator
    {
        [NoTranslate]
        public string tradeTag;

        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            // Log.Message($"[StockGenerator_UniqueWeapon] GenerateThings 시작 - OdysseyActive: {ModsConfig.OdysseyActive}, tradeTag: {this.tradeTag}");
            
            if (!ModsConfig.OdysseyActive)
            {
                // Log.Message("[StockGenerator_UniqueWeapon] Odyssey DLC가 비활성화되어 있음");
                yield break;
            }

            // CompUniqueWeapon을 가진 ThingDef 찾기
            List<ThingDef> candidates = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.HasComp<CompUniqueWeapon>() 
                           && def.tradeability.TraderCanSell() 
                           && def.PlayerAcquirable
                           && (string.IsNullOrEmpty(this.tradeTag) || (def.tradeTags != null && def.tradeTags.Contains(this.tradeTag))))
                .ToList();

            // Log.Message($"[StockGenerator_UniqueWeapon] 후보 ThingDef 개수: {candidates.Count}");
            if (candidates.Count > 0)
            {
                // Log.Message($"[StockGenerator_UniqueWeapon] 후보 목록: {string.Join(", ", candidates.Select(c => c.defName))}");
            }

            if (candidates.Count == 0)
            {
                // Log.Message("[StockGenerator_UniqueWeapon] 후보가 없어 종료");
                yield break;
            }

            // countRange에 따라 생성
            int count = this.countRange.RandomInRange;
            // Log.Message($"[StockGenerator_UniqueWeapon] 초기 count: {count}, countRange: {this.countRange}");
            if (this.countRange != null && this.countRange.max > 0)
            {
                count = Mathf.Max(this.countRange.RandomInRange, count);
                // Log.Message($"[StockGenerator_UniqueWeapon] 수정된 count: {count}");
            }
            
            // totalPriceRange가 Zero면 가격 체크 없이 생성
            bool checkPrice = this.totalPriceRange.max > 0f;
            FloatRange priceRange = checkPrice ? this.totalPriceRange : FloatRange.Zero;
            // Log.Message($"[StockGenerator_UniqueWeapon] checkPrice: {checkPrice}, priceRange: {priceRange}");
            float totalValue = 0f;

            for (int i = 0; i < count; i++)
            {
                // Log.Message($"[StockGenerator_UniqueWeapon] 무기 생성 시도 {i + 1}/{count}");
                bool isLast = i == count - 1;
                int attempts = 999;
                Thing uniqueWeapon = null;

                do
                {
                    // ThingDef 랜덤 선택
                    ThingDef chosenDef = candidates.RandomElement();
                    // Log.Message($"[StockGenerator_UniqueWeapon] 선택된 ThingDef: {chosenDef.defName}");
                    
                    // 유니크 무기 생성 (ThingSetMaker_UniqueWeapon과 동일한 방식)
                    uniqueWeapon = ThingMaker.MakeThing(chosenDef, null);
                    // Log.Message($"[StockGenerator_UniqueWeapon] 생성된 무기: {uniqueWeapon?.Label}, MarketValue: {uniqueWeapon?.MarketValue}");
                    
                    attempts--;
                }
                while (checkPrice && attempts > 0 && uniqueWeapon != null && 
                       ((totalValue + uniqueWeapon.MarketValue > priceRange.max) || 
                        (isLast && totalValue + uniqueWeapon.MarketValue < priceRange.min)));

                if (attempts <= 0)
                {
                    // Log.Message($"[StockGenerator_UniqueWeapon] 가격 범위를 만족하는 무기를 찾지 못함 (시도 횟수 초과)");
                    break;
                }

                if (checkPrice)
                {
                    totalValue += uniqueWeapon.MarketValue;
                    // Log.Message($"[StockGenerator_UniqueWeapon] 총 가격 업데이트: {totalValue}");
                }
                
                // Log.Message($"[StockGenerator_UniqueWeapon] 무기 생성 완료: {uniqueWeapon?.Label}");
                yield return uniqueWeapon;
            }
            
            // Log.Message($"[StockGenerator_UniqueWeapon] GenerateThings 완료 - 총 생성된 무기: {count}");
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.HasComp<CompUniqueWeapon>();
        }
    }
}
