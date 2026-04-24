using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반 인시던트 XML 설정. IncidentDef의 modExtensions에 연결.
	/// 유랑민 풀/로스터, 매년 충원 수 등을 XML에서 제어.
	/// </summary>
	public class IncidentDefExtension_WanderingCaravan : DefModExtension
	{
		/// <summary>첫 방문 시 생성되는 유랑민 수. 기본 3</summary>
		public int initialSettlerCount = 3;

		/// <summary>매년 방문 시 데리고 오는 최대 유랑민 수. 기본 5</summary>
		public int maxRosterCount = 5;

		/// <summary>매년 풀에 추가되는 신규 유랑민 수 범위. 기본 1~2</summary>
		public IntRange yearlyRecruitRange = new IntRange(1, 2);

		/// <summary>풀이 maxRosterCount 이상일 때 추가 생성 수. 기본 1</summary>
		public int overflowRecruitCount = 1;

		/// <summary>전체 풀 최대 크기. -1 = 무제한</summary>
		public int maxPoolSize = -1;

		/// <summary>N번 로스터에 뽑혀 등장했는데 선택 안 되면 풀에서 제거. -1 = 무제한</summary>
		public int expireAfterAppearances = -1;

		/// <summary>유랑민 풀에 들어갈 PawnKind 가중치 목록. &lt;PawnKind&gt;가중치&lt;/PawnKind&gt; 형식. 비어있으면 Nomad, Wanderer 기본 사용.</summary>
		public List<PawnKindDefWeight> settlerPawnKinds;
	}
}
