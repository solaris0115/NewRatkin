using System;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 유랑단 캐러반 이벤트용 StorytellerComp Properties.
	/// 봄(Aprimay)에 MTB 시작, 여름(Jugust)에 확률 상승, 이후 거의 확정.
	/// minRefireDays로 연간 중복 방지.
	/// </summary>
	public class StorytellerCompProperties_WanderingCaravan : StorytellerCompProperties
	{
		public IncidentDef incident;

		/// <summary>
		/// 게임 시작 후 이 일수가 지나야 이벤트 트리거 가능
		/// </summary>
		public new float minDaysPassed = 30f;

		/// <summary>
		/// 봄(Aprimay) MTB 일수
		/// </summary>
		public float springMtbDays = 3f;

		/// <summary>
		/// 여름(Jugust) MTB 일수
		/// </summary>
		public float summerMtbDays = 1.5f;

		/// <summary>
		/// 가을/겨울 MTB 일수 (거의 확정)
		/// </summary>
		public float fallbackMtbDays = 0.5f;

		/// <summary>
		/// 마지막 발생 후 최소 경과 일수 (중복 방지)
		/// </summary>
		public float minRefireDays = 45f;

		public StorytellerCompProperties_WanderingCaravan()
		{
			this.compClass = typeof(StorytellerComp_WanderingCaravan);
		}
	}
}
