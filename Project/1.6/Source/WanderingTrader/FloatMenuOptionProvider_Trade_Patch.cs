using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 리더에 대해 FloatMenuOptionProvider_Trade가 "거래하기"를 띄우지 않도록 패치.
	/// 유랑단은 "대화하기" → 대화창에서 "물자 거래"로만 거래 가능.
	/// </summary>
	[StaticConstructorOnStartup]
	public static class FloatMenuOptionProvider_Trade_Patch
	{
		static FloatMenuOptionProvider_Trade_Patch()
		{
			Harmony harmony = new Harmony("com.NewRatkin.rimworld.mod.wanderingtrader");
			// GetOptionsFor(Thing) / GetOptionsFor(Pawn) 오버로드 구분 - Pawn 시그니처 명시
			MethodInfo target = AccessTools.Method(
				typeof(FloatMenuOptionProvider_Trade),
				nameof(FloatMenuOptionProvider_Trade.GetOptionsFor),
				new[] { typeof(Pawn), typeof(FloatMenuContext) }
			);
			harmony.Patch(target, prefix: new HarmonyMethod(typeof(FloatMenuOptionProvider_Trade_Patch), nameof(Prefix)));
		}

		public static bool Prefix(Pawn clickedPawn, ref IEnumerable<FloatMenuOption> __result)
		{
			if (clickedPawn?.GetLord()?.LordJob is LordJob_WanderingCaravan job && job.leader == clickedPawn)
			{
				__result = Enumerable.Empty<FloatMenuOption>();
				return false;
			}
			return true;
		}
	}
}
