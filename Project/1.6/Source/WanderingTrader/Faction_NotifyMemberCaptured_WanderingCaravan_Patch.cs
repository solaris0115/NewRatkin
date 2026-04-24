using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// RK_Faction_Caravan은 hidden → HasGoodwill=false라 바닐라 TryAffectGoodwillWith(체포)가 실행되지 않음.
	/// 플레이어가 유랑단원을 포로로 잡으면 일반 팩션과 같이 적대 관계로 둔다.
	/// </summary>
	[StaticConstructorOnStartup]
	public static class Faction_NotifyMemberCaptured_WanderingCaravan_Patch
	{
		static Faction_NotifyMemberCaptured_WanderingCaravan_Patch()
		{
			Harmony harmony = new Harmony("com.NewRatkin.rimworld.mod.wanderingtrader");
			MethodInfo target = AccessTools.Method(typeof(Faction), nameof(Faction.Notify_MemberCaptured));
			harmony.Patch(target, postfix: new HarmonyMethod(typeof(Faction_NotifyMemberCaptured_WanderingCaravan_Patch), nameof(Postfix)));
		}

		private static void Postfix(Faction __instance, Pawn member, Faction violator)
		{
			if (__instance == null || member == null)
				return;
			if (__instance.def != RatkinFactionDefOf.RK_Faction_Caravan)
				return;
			if (violator != Faction.OfPlayer)
				return;
			if (violator == __instance)
				return;
			if (__instance.temporary || member.IsSlaveOfColony)
				return;
			if (__instance.HostileTo(Faction.OfPlayer))
				return;

			string reason = HistoryEventDefOf.MemberCaptured?.LabelCap ?? null;
			__instance.SetRelationDirect(
				Faction.OfPlayer,
				FactionRelationKind.Hostile,
				true,
				reason,
				new GlobalTargetInfo(member));
		}
	}
}
