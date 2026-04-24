using System;

namespace RimWorld
{
	/// <summary>
	/// 랫킨 사제 합류 이벤트용 StorytellerComp Properties (1회성)
	/// - fireAfterDaysPassed: 이벤트가 트리거 가능해지는 최소 경과 일수
	/// - mtbDays: 조건 충족 후 평균 발생 일수 (MTB 방식)
	/// - 이벤트는 게임당 1회만 발생
	/// </summary>
	public class StorytellerCompProperties_RK_PriestJoin : StorytellerCompProperties
	{
		public IncidentDef incident;

		/// <summary>
		/// 게임 시작 후 이 일수가 지나야 이벤트 트리거 가능
		/// </summary>
		public float fireAfterDaysPassed = 60f;

		/// <summary>
		/// 조건 충족 후 평균 발생 일수 (MTB: Mean Time Between)
		/// 값이 작을수록 빨리 발생, 클수록 늦게 발생
		/// </summary>
		public float mtbDays = 5f;

		public StorytellerCompProperties_RK_PriestJoin()
		{
			this.compClass = typeof(StorytellerComp_RK_PriestJoin);
		}
	}
}
