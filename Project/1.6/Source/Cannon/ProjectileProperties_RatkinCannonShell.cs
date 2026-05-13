using Verse;

namespace NewRatkin
{
	public class ProjectileProperties_RatkinCannonShell : ProjectileProperties
	{
		/// <summary>지면 도탄 순간 폭발 반경(셀, 고정값). 최종 폭발 <c>explosionRadius</c>와 별개.</summary>
		public float groundTouchExplosionRadius = 2.85f;

		/// <summary>
		/// 지면 도탄 순간 폭발 피해량. 0 이상이면 이 값을 사용하고,
		/// 생략 시 기본 -1은 투사체의 <see cref="Projectile.DamageAmount"/>와 동일하게 처리.
		/// </summary>
		public int groundTouchDamageAmount = -1;

		/// <summary>
		/// 빈 지면 도탄 후 같은 방향으로 추가 비행할 수 있는 최대 거리(셀).
		/// 첫 비행 구간 길이의 절반과 비교해 더 작은 값이 적용된다.
		/// </summary>
		public float groundBounceMaxDistance = 13f;
	}
}
