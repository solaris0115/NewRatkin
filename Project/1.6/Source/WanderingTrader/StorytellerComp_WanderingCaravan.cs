using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 유랑단 캐러반 이벤트 StorytellerComp.
	/// 봄에 MTB 시작, 여름에 확률 상승, 가을/겨울에 거의 확정.
	/// minRefireDays로 연간 중복 방지.
	/// </summary>
	public class StorytellerComp_WanderingCaravan : StorytellerComp
	{
		private StorytellerCompProperties_WanderingCaravan Props => (StorytellerCompProperties_WanderingCaravan)props;

		public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
		{
			if (!Props.incident.TargetAllowed(target))
				yield break;

			float daysPassed = GenDate.DaysPassedFloat;
			if (daysPassed < Props.minDaysPassed)
				yield break;

			// 마지막 발생 후 minRefireDays 경과 체크
			int lastFireTick;
			if (target.StoryState.lastFireTicks.TryGetValue(Props.incident, out lastFireTick))
			{
				float daysSinceLastFire = (GenTicks.TicksGame - lastFireTick) / 60000f;
				if (daysSinceLastFire < Props.minRefireDays)
					yield break;
			}

			// 시즌별 MTB
			float longitude = 0f;
			if (target is Map map)
				longitude = Find.WorldGrid.LongLatOf(map.Tile).x;
			else if (target is Caravan caravan && caravan.Tile >= 0)
				longitude = Find.WorldGrid.LongLatOf(caravan.Tile).x;

			Quadrum quadrum = GenDate.Quadrum(GenTicks.TicksAbs, longitude);
			float mtbDays;
			if (quadrum == Quadrum.Aprimay)
				mtbDays = Props.springMtbDays;
			else if (quadrum == Quadrum.Jugust)
				mtbDays = Props.summerMtbDays;
			else
				mtbDays = Props.fallbackMtbDays;

			if (!Rand.MTBEventOccurs(mtbDays, 60000f, 1000f))
				yield break;

			IncidentParms parms = GenerateParms(Props.incident.category, target);
			if (!Props.incident.Worker.CanFireNow(parms))
				yield break;

			yield return new FiringIncident(Props.incident, this, parms);
		}

		public override string ToString()
		{
			return base.ToString() + " " + Props.incident?.ToString();
		}
	}
}
