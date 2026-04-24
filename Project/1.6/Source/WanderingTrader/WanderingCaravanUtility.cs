using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반 관련 유틸. IncidentDefExtension_WanderingCaravan의 settlerPawnKinds를 참조.
	/// </summary>
	public static class WanderingCaravanUtility
	{
		private const string IncidentDefName = "RK_Incident_WanderingTrader";

		/// <summary>해당 PawnKind가 유랑민 풀(정착 제안 대상)에 포함되는지.</summary>
		public static bool IsSettlerPoolKind(PawnKindDef kind)
		{
			if (kind == null) return false;
			var kinds = GetSettlerPawnKinds();
			return kinds != null && kinds.Contains(kind);
		}

		/// <summary>유랑민 풀에 사용할 PawnKind 목록. XML 미지정 시 Nomad, Wanderer 기본.</summary>
		public static List<PawnKindDef> GetSettlerPawnKinds()
		{
			var weights = GetSettlerPawnKindWeights();
			if (weights == null || weights.Count == 0)
				return new List<PawnKindDef> { RatkinPawnKindDefOf.RK_PawnKind_Nomad, RatkinPawnKindDefOf.RK_PawnKind_Wanderer };
			return weights.ConvertAll(w => w.kindDef);
		}

		/// <summary>유랑민 풀 가중치 목록. XML 미지정 시 null.</summary>
		private static List<PawnKindDefWeight> GetSettlerPawnKindWeights()
		{
			var incident = DefDatabase<IncidentDef>.GetNamedSilentFail(IncidentDefName);
			var ext = incident?.GetModExtension<IncidentDefExtension_WanderingCaravan>();
			return ext?.settlerPawnKinds;
		}

		/// <summary>유랑민 풀에서 가중치에 따라 랜덤 PawnKind 선택.</summary>
		public static PawnKindDef RandomSettlerKind()
		{
			var weights = GetSettlerPawnKindWeights();
			if (weights == null || weights.Count == 0)
				return RatkinPawnKindDefOf.RK_PawnKind_Nomad;
			var w = weights.RandomElementByWeight(x => x.weight);
			return w?.kindDef ?? RatkinPawnKindDefOf.RK_PawnKind_Nomad;
		}

		/// <summary>
		/// 어느 맵이든 LordJob_WanderingCaravan이 있거나, RK_Faction_Caravan 소속 유랑단 맴버가 스폰되어 있으면 true.
		/// </summary>
		public static bool HasWanderingCaravanActiveAnywhere()
		{
			FactionDef caravanFaction = RatkinFactionDefOf.RK_Faction_Caravan;
			if (caravanFaction == null) return false;

			foreach (Map m in Find.Maps)
			{
				if (m == null) continue;
				foreach (Lord lord in m.lordManager.lords)
				{
					// 마지막 폰 퇴장 틱에는 RemovePawn 직후·Lord.Destroy 직전이라 Lord만 남는다. 이때는 활성 캐러반으로 취급하지 않는다.
					if (lord?.LordJob is LordJob_WanderingCaravan
						&& CaravanLordHasSpawnedOwnedPawn(lord))
						return true;
				}
				foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
				{
					if (p == null || p.DestroyedOrNull() || p.Dead) continue;
					if (p.Faction?.def != caravanFaction) continue;
					if (p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanLeader
						|| p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard
						|| IsSettlerPoolKind(p.kindDef))
						return true;
				}
			}

			GameComponent_WanderingCaravan comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
			if (comp != null && comp.HasRosterOrPoolPawnsSpawned())
				return true;

			return false;
		}

		/// <summary>유랑단 Lord가 아직 맵에 스폰된 소유 폰(짐꾼 동물 포함)을 갖는지. 빈 Lord는 틱 끝에서 Destroy 직전까지 존재할 수 있다.</summary>
		private static bool CaravanLordHasSpawnedOwnedPawn(Lord lord)
		{
			if (lord?.ownedPawns == null) return false;
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn p = lord.ownedPawns[i];
				if (p == null || p.DestroyedOrNull() || p.Dead) continue;
				if (p.Spawned) return true;
			}
			return false;
		}

		/// <summary>
		/// 적대(이벤트·BecamePlayerEnemy 등) 후 유랑단 맴버가 맵에 더 없으면 팩션 관계만 중립으로 복구.
		/// 공격 패널티(WasAttacked/내방 금지)는 건드리지 않는다.
		/// </summary>
		public static void TryResetCaravanFactionToNeutralIfCleared()
		{
			if (Current.Game == null) return;
			if (HasWanderingCaravanActiveAnywhere()) return;
			Faction f = Find.FactionManager.FirstFactionOfDef(RatkinFactionDefOf.RK_Faction_Caravan);
			if (f == null) return;
			if (!f.HostileTo(Faction.OfPlayer)) return;
			f.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false, null, null);
		}
	}
}
