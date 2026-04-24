using Verse;
using UnityEngine;

namespace NewRatkin
{
    public class CompProperties_DPSDisplay : CompProperties
    {
        public CompProperties_DPSDisplay()
        {
            compClass = typeof(Comp_DPSDisplay);
        }

        public float healRatePerSecond = 10f; // 초당 회복량
    }

    public class Comp_DPSDisplay : ThingComp
    {
        private int firstDamageTick = -1; // 첫 피해 시점
        private int lastDamageTick = -1; // 마지막 피해 시점
        private float totalDamage = 0f; // 누적 총 피해량
        private Mote_CountDown tempMote;
        private const int ResetTimeTicks = 600; // 5초 = 300틱 (60틱/초)

        public CompProperties_DPSDisplay Props
        {
            get
            {
                return (CompProperties_DPSDisplay)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // 초기화는 필요 없음
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map);
            if (tempMote != null && !tempMote.Destroyed)
            {
                tempMote.Destroy();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            // 저장 불필요 - 매번 새로 계산
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            
            // 실제로 적용된 피해량이 0보다 크면 집계
            if (totalDamageDealt > 0)
            {
                int currentTick = Find.TickManager.TicksGame;
                
                // 첫 피해면 시작 시점 기록
                if (firstDamageTick < 0)
                {
                    firstDamageTick = currentTick;
                }
                
                // 마지막 피해 시점 업데이트
                lastDamageTick = currentTick;
                
                // 총 피해량 누적 (실제 적용된 피해량)
                totalDamage += totalDamageDealt;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            
            // HP 자동 회복 (매 틱당 4 HP, 초당 240 HP 회복)
            if (parent.def.useHitPoints && parent.HitPoints < parent.MaxHitPoints)
            {
                int healAmount = 4; // 매 틱당 회복량 (Long tick당 1000과 동일한 비율)
                int newHitPoints = Mathf.Min(parent.HitPoints + healAmount, parent.MaxHitPoints);
                parent.HitPoints = newHitPoints;
            }
            
            int currentTick = Find.TickManager.TicksGame;
            
            // 마지막 피해 시점부터 10초 이상 피해가 없으면 초기화
            if (lastDamageTick >= 0 && currentTick - lastDamageTick >= ResetTimeTicks)
            {
                firstDamageTick = -1;
                lastDamageTick = -1;
                totalDamage = 0f;
            }
            
            // DPS 계산
            float dps = 0f;
            if (firstDamageTick >= 0 && totalDamage > 0)
            {
                // 경과 시간 계산 (틱을 초로 변환)
                float elapsedSeconds = (currentTick - firstDamageTick) / 60f;
                if (elapsedSeconds > 0)
                {
                    dps = totalDamage / elapsedSeconds;
                }
            }
            
            // DPS 항상 표시 (매 틱마다 업데이트)
            Map map = parent.Map;
            if (map != null)
            {
                string dpsText = dps.ToString("F1") + " DPS";
                Color textColor = Color.green; // 기본 색상 (피해 없을 때)
                
                // DPS가 높을수록 빨간색으로
                if (dps > 50)
                {
                    textColor = Color.red;
                }
                else if (dps > 20)
                {
                    textColor = Color.Lerp(Color.yellow, Color.red, (dps - 20) / 30f);
                }
                else if (dps > 0)
                {
                    textColor = Color.Lerp(Color.green, Color.yellow, dps / 20f);
                }
                
                ThrowText(parent.DrawPos + new Vector3(0, 0, 0.5f), map, dpsText, textColor, 0.1f);
            }
        }

        private void ThrowText(Vector3 loc, Map map, string text, Color color, float timeBeforeStartFadeout = -1f)
        {
            if (tempMote != null && !tempMote.Destroyed)
            {
                tempMote.Destroy();
            }
            Mote_CountDown moteText = (Mote_CountDown)ThingMaker.MakeThing(RatkinMoteDefOf.Mote_CountDown);
            moteText.exactPosition = loc;
            moteText.text = text;
            moteText.textColor = color;
            if (timeBeforeStartFadeout >= 0f)
            {
                moteText.overrideTimeBeforeStartFadeout = timeBeforeStartFadeout;
            }
            tempMote = moteText;
            GenSpawn.Spawn(moteText, parent.Position, map);
        }
    }
}

