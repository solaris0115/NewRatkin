using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 돌려보내기 시 맵 끝까지 이동 후 퇴장하는 유랑단 캐러반 pawn이 WorldPawns에서 폐기되지 않도록 KeepForever 적용.
	/// </summary>
	[StaticConstructorOnStartup]
	public static class Pawn_ExitMap_Patch
	{
		static Pawn_ExitMap_Patch()
		{
			Harmony harmony = new Harmony("com.NewRatkin.rimworld.mod.wanderingtrader");
			MethodInfo target = AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.PassToWorld), new[] { typeof(Pawn), typeof(PawnDiscardDecideMode) });
			harmony.Patch(target, prefix: new HarmonyMethod(typeof(Pawn_ExitMap_Patch), nameof(PassToWorld_Prefix)));
		}

		public static void PassToWorld_Prefix(Pawn pawn, ref PawnDiscardDecideMode discardMode)
		{
			if (pawn?.Faction == null || pawn.kindDef == null)
				return;
			if (pawn.Faction.def != RatkinFactionDefOf.RK_Faction_Caravan)
				return;
			// 유랑단 캐러반 roster 대상(리더/호위/유랑민)만 처리. 짐꾼(동물)은 제외.
			if (pawn.kindDef != RatkinPawnKindDefOf.RK_PawnKind_CaravanLeader
				&& pawn.kindDef != RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard
				&& !WanderingCaravanUtility.IsSettlerPoolKind(pawn.kindDef))
				return;
			// 플레이어 공격으로 퇴각 중이면 Decide 모드 (다른 방법으로 등장 가능, 캐러반으로는 안 옴)
			if (Current.Game?.GetComponent<GameComponent_WanderingCaravan>()?.WasAttackedByPlayer == true)
			{
				discardMode = PawnDiscardDecideMode.Decide;
				return;
			}
			discardMode = PawnDiscardDecideMode.KeepForever;
		}
	}
}
