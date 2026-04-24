using Verse;

namespace NewRatkin
{
	/// <summary>볼터 투사체: 스턴·보너스 EMP는 XML(ThingDef)에서 조절.</summary>
	public class ProjectileProperties_Bolter : ProjectileProperties
	{
		/// <summary>유효 직격 시 스턴 시도 확률(0~1).</summary>
		public float stunOnHitChance = 0.3f;

		/// <summary>스턴 지속(초), 엔진 TicksToSeconds로 변환.</summary>
		public float stunDurationSeconds = 2f;

		/// <summary>보너스 EMP 시도 확률(0~1). 메카노이드·실드에 동일.</summary>
		public float extraEmpChance = 1f;

		/// <summary>EMP DamageDef 기준 보너스 양.</summary>
		public float extraEmpDamage = 6f;
	}
}
