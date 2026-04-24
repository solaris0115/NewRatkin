using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 유랑 상인 pawn 가격 설정. TraderKindDef의 modExtensions에 연결.
	/// 가격 = basePawnPrice + (combatPower * combatPowerPriceMultiplier)
	/// </summary>
	public class WanderingTraderSettings : DefModExtension
	{
		public float basePawnPrice = 500f;
		public float combatPowerPriceMultiplier = 5f;

		public int GetPriceForPawn(Pawn pawn)
		{
			if (pawn?.kindDef == null)
				return (int)basePawnPrice;
			float price = basePawnPrice + (pawn.kindDef.combatPower * combatPowerPriceMultiplier);
			return (int)price;
		}
	}
}
