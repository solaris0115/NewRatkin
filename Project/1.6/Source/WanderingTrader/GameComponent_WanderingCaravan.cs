using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 유랑단 캐러반 인물 명부를 게임 전체에서 영속 저장.
	/// 풀/로스터 분리: 전체 풀에 유랑민 보관, 매년 랜덤으로 로스터 선택하여 방문.
	/// </summary>
	public class GameComponent_WanderingCaravan : GameComponent
	{
		private List<Pawn> rosterLeader = new List<Pawn>();
		private List<Pawn> rosterGuards = new List<Pawn>();
		private List<Pawn> settlerPool = new List<Pawn>();
		private Dictionary<Pawn, SettlementJoinRequirement> settlerRequirements = new Dictionary<Pawn, SettlementJoinRequirement>();
		private Dictionary<Pawn, int> settlerAppearanceCount = new Dictionary<Pawn, int>();
		private List<Pawn> rosterSettlers = new List<Pawn>();
		private int lastVisitYear = -1;

		/// <summary>Scribe Dictionary용. 모드 간 데이터 유지에 클래스 필드 필수.</summary>
		private List<Pawn> tmpReqPawns;
		private List<SettlementJoinRequirement> tmpReqValues;
		private List<Pawn> tmpCountPawns;
		private List<int> tmpCountValues;

		/// <summary>플레이어 공격으로 퇴각한 경우, roster 추가 건너뜀 및 패널티 적용용</summary>
		private bool wasAttackedByPlayer = false;
		/// <summary>패널티 만료 틱. 이 틱 이후에야 캐러반 방문 가능</summary>
		private int attackPenaltyUntilTick = 0;

		public GameComponent_WanderingCaravan(Game game) { }

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref rosterLeader, "rosterLeader", LookMode.Reference);
			Scribe_Collections.Look(ref rosterGuards, "rosterGuards", LookMode.Reference);
			Scribe_Collections.Look(ref settlerPool, "settlerPool", LookMode.Reference);
			Scribe_Collections.Look(ref rosterSettlers, "rosterSettlers", LookMode.Reference);
			Scribe_Values.Look(ref lastVisitYear, "lastVisitYear", -1);
			Scribe_Values.Look(ref wasAttackedByPlayer, "wasAttackedByPlayer", false);
			Scribe_Values.Look(ref attackPenaltyUntilTick, "attackPenaltyUntilTick", 0);

			Scribe_Collections.Look(ref settlerRequirements, "settlerRequirements", LookMode.Reference, LookMode.Deep, ref tmpReqPawns, ref tmpReqValues, true, false, false);
			Scribe_Collections.Look(ref settlerAppearanceCount, "settlerAppearanceCount", LookMode.Reference, LookMode.Value, ref tmpCountPawns, ref tmpCountValues, true, false, false);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (rosterLeader == null) rosterLeader = new List<Pawn>();
				if (rosterGuards == null) rosterGuards = new List<Pawn>();
				if (settlerPool == null) settlerPool = new List<Pawn>();
				if (rosterSettlers == null) rosterSettlers = new List<Pawn>();
				if (settlerRequirements == null) settlerRequirements = new Dictionary<Pawn, SettlementJoinRequirement>();
				if (settlerAppearanceCount == null) settlerAppearanceCount = new Dictionary<Pawn, int>();
			}
		}

		/// <summary>플레이어 공격으로 퇴각 시 호출. 로스터/풀 초기화 및 패널티 적용</summary>
		public void NotifyCaravanAttacked()
		{
			wasAttackedByPlayer = true;
			attackPenaltyUntilTick = GenTicks.TicksGame + (120 * 60000); // 120일 = 2년

			// 풀 pawn들을 WorldPawns에서 KeepForever → Decide로 전환 (자연 GC 허용)
			foreach (Pawn p in settlerPool.ToList())
			{
				if (p != null && !p.DestroyedOrNull() && !p.Dead && p.IsWorldPawn())
				{
					Find.WorldPawns.RemovePawn(p);
					Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
				}
			}

			rosterLeader.Clear();
			rosterGuards.Clear();
			rosterSettlers.Clear();
			settlerPool.Clear();
			settlerRequirements.Clear();
			settlerAppearanceCount.Clear();
		}

		/// <summary>공격 패널티 기간 중인지</summary>
		public bool IsAttackPenaltyActive => attackPenaltyUntilTick > 0 && GenTicks.TicksGame < attackPenaltyUntilTick;

		/// <summary>플레이어 공격으로 퇴각 중인지 (Pawn_ExitMap_Patch 등에서 사용)</summary>
		public bool WasAttackedByPlayer => wasAttackedByPlayer;

		/// <summary>패널티 만료 후 다음 방문 시 플래그 리셋 (IncidentWorker에서 호출)</summary>
		public void ResetAttackedFlag()
		{
			wasAttackedByPlayer = false;
		}

		/// <summary>사망/파괴된 폰을 풀 및 명부에서 제거</summary>
		public void CleanupDeadPawns()
		{
			rosterLeader.RemoveAll(p => p == null || p.DestroyedOrNull() || p.Dead);
			rosterGuards.RemoveAll(p => p == null || p.DestroyedOrNull() || p.Dead);
			foreach (Pawn p in settlerPool.Where(p => p == null || p.DestroyedOrNull() || p.Dead).ToList())
			{
				settlerPool.Remove(p);
				settlerRequirements.Remove(p);
				settlerAppearanceCount.Remove(p);
			}
		}

		/// <summary>만료된 유랑민을 풀에서 영구 제거 (expireAfterAppearances 초과 시)</summary>
		public void RemoveExpiredFromPool(int expireAfter)
		{
			if (expireAfter < 0) return;
			foreach (Pawn p in settlerPool.Where(p =>
			{
				int c;
				return settlerAppearanceCount.TryGetValue(p, out c) && c >= expireAfter;
			}).ToList())
			{
				settlerPool.Remove(p);
				settlerRequirements.Remove(p);
				settlerAppearanceCount.Remove(p);
			}
		}

		/// <summary>유저가 유랑민 수락 시 풀·로스터·조건·등장횟수에서 제거</summary>
		/// <remarks>첫 방문 시 TakePawnsForSpawn이 호출되지 않아 rosterSettlers가 비워지지 않음. 합류 시 rosterSettlers에서도 제거해야 HasRosterOrPoolPawnsSpawned/트리거에 영향 없음.</remarks>
		public void OnSettlerAccepted(Pawn pawn)
		{
			if (pawn == null) return;
			settlerPool.Remove(pawn);
			rosterSettlers.Remove(pawn);
			settlerRequirements.Remove(pawn);
			settlerAppearanceCount.Remove(pawn);
		}

		/// <summary>풀에 유랑민+조건 추가</summary>
		public void AddToPool(Pawn pawn, SettlementJoinRequirement requirement)
		{
			if (pawn == null) return;
			if (!settlerPool.Contains(pawn))
				settlerPool.Add(pawn);
			if (requirement != null)
				settlerRequirements[pawn] = requirement;
			if (!settlerAppearanceCount.ContainsKey(pawn))
				settlerAppearanceCount[pawn] = 0;
		}

		/// <summary>풀에서 랜덤 maxCount명 선택 → 로스터에 설정, 등장횟수 증가</summary>
		public List<Pawn> SelectRosterFromPool(int maxCount)
		{
			CleanupDeadPawns();
			var valid = settlerPool.Where(p => p != null && !p.DestroyedOrNull() && !p.Dead).ToList();
			int take = Math.Min(maxCount, valid.Count);
			if (take <= 0)
			{
				rosterSettlers.Clear();
				return new List<Pawn>();
			}
			var selected = valid.InRandomOrder().Take(take).ToList();
			rosterSettlers.Clear();
			rosterSettlers.AddRange(selected);
			foreach (Pawn p in selected)
			{
				int c;
				settlerAppearanceCount[p] = settlerAppearanceCount.TryGetValue(p, out c) ? c + 1 : 1;
			}
			return new List<Pawn>(rosterSettlers);
		}

		/// <summary>폰의 합류 조건 조회</summary>
		public SettlementJoinRequirement GetRequirement(Pawn pawn)
		{
			SettlementJoinRequirement r;
			return pawn != null && settlerRequirements.TryGetValue(pawn, out r) ? r : null;
		}

		/// <summary>돌려보내기로 맵 끝 이동 후, 각 pawn이 ExitMap할 때 호출. roster에 추가 (WorldPawns는 Pawn_ExitMap_Patch에서 KeepForever 처리)</summary>
		public void OnCaravanPawnExitedMap(Pawn p)
		{
			if (p == null || p.DestroyedOrNull() || p.Dead) return;
			// 플레이어 공격으로 퇴각 중이면 roster에 추가하지 않음
			if (wasAttackedByPlayer) return;

			if (p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanLeader)
				rosterLeader.Add(p);
			else if (p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard)
				rosterGuards.Add(p);
			else if (WanderingCaravanUtility.IsSettlerPoolKind(p.kindDef))
				rosterSettlers.Add(p);
			// 짐꾼(동물)은 roster에 포함하지 않음 - 매번 새로 생성
		}

		/// <summary>캐러반 퇴장 시 Lord의 생존 인물로 명부 갱신 후 WorldPawn 보존. 유랑민은 풀에 유지(이미 있음).</summary>
		public void OnCaravanExited(Lord lord)
		{
			if (lord?.ownedPawns == null) return;

			rosterLeader.Clear();
			rosterGuards.Clear();
			rosterSettlers.Clear();

			LordJob_WanderingCaravan job = lord.LordJob as LordJob_WanderingCaravan;
			Pawn leaderRef = job?.leader;

			foreach (Pawn p in lord.ownedPawns.ToList())
			{
				if (p == null || p.DestroyedOrNull() || p.Dead) continue;

				if (p == leaderRef || p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanLeader)
					rosterLeader.Add(p);
				else if (p.kindDef == RatkinPawnKindDefOf.RK_PawnKind_CaravanGuard)
					rosterGuards.Add(p);
				else if (WanderingCaravanUtility.IsSettlerPoolKind(p.kindDef))
					rosterSettlers.Add(p);
			}

			var toWorld = rosterLeader.Concat(rosterGuards).Concat(rosterSettlers);
			foreach (Pawn p in toWorld)
			{
				if (p.Spawned)
				{
					lord.Notify_PawnLost(p, PawnLostCondition.LeftVoluntarily);
					p.DeSpawn(DestroyMode.Vanish);
				}
				if (!p.IsWorldPawn())
					Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
			}
		}

		/// <summary>명부가 비어있는지 (첫 방문 여부)</summary>
		public bool IsRosterEmpty => (rosterLeader.Count == 0 && rosterGuards.Count == 0 && settlerPool.Count == 0);

		/// <summary>roster 또는 settlerPool에 스폰된 pawn이 있는지. 캐러반이 아직 맵에 있어 중복 인시던트 방지용.</summary>
		public bool HasRosterOrPoolPawnsSpawned()
		{
			foreach (Pawn p in rosterLeader.Concat(rosterGuards).Concat(rosterSettlers).Concat(settlerPool))
			{
				if (p != null && !p.DestroyedOrNull() && !p.Dead && p.Spawned)
					return true;
			}
			return false;
		}

		/// <summary>현재 연도가 마지막 방문 이후 새해인지</summary>
		public bool IsNewYearFor(int tile)
		{
			return lastVisitYear >= 0 && GenLocalDate.Year(tile) > lastVisitYear;
		}

		/// <summary>마지막 방문 연도 갱신</summary>
		public void UpdateLastVisitYear(int tile)
		{
			lastVisitYear = GenLocalDate.Year(tile);
		}

		/// <summary>명부에서 폰 가져오기 (WorldPawn에서 제거 후 반환, 스폰용). 유랑민은 SelectRosterFromPool로 이미 설정된 rosterSettlers 사용.</summary>
		public void TakePawnsForSpawn(Map map, out Pawn leader, out List<Pawn> guards, out List<Pawn> settlers)
		{
			CleanupDeadPawns();

			leader = rosterLeader.FirstOrDefault();
			guards = new List<Pawn>(rosterGuards);
			settlers = new List<Pawn>(rosterSettlers);

			rosterLeader.Clear();
			rosterGuards.Clear();
			rosterSettlers.Clear();

			Action<Pawn> removeFromWorld = p =>
			{
				if (p != null && p.IsWorldPawn())
					Find.WorldPawns.RemovePawn(p);
			};

			removeFromWorld(leader);
			guards.ForEach(removeFromWorld);
			settlers.ForEach(removeFromWorld);
		}

		/// <summary>명부에 리더/호위 추가 (유랑민은 풀 시스템으로 별도 관리)</summary>
		public void AddToRoster(Pawn leaderPawn, List<Pawn> guardPawns, List<Pawn> settlerPawns)
		{
			if (leaderPawn != null) rosterLeader.Add(leaderPawn);
			if (guardPawns != null) rosterGuards.AddRange(guardPawns);
			if (settlerPawns != null)
			{
				foreach (Pawn p in settlerPawns)
					if (p != null && !rosterSettlers.Contains(p))
						rosterSettlers.Add(p);
			}
		}

		public void AddLeader(Pawn p) { if (p != null) rosterLeader.Add(p); }
		public void AddGuard(Pawn p) { if (p != null) rosterGuards.Add(p); }

		/// <summary>풀 크기</summary>
		public int PoolCount => settlerPool.Count(p => p != null && !p.DestroyedOrNull() && !p.Dead);

		/// <summary>매년 풀에 신규 유랑민 추가 (조건 부여)</summary>
		public void RefillPool(Map map, Faction faction, int toAdd, int maxPoolSize, IncidentDefExtension_WanderingCaravan ext = null)
		{
			CleanupDeadPawns();
			int current = PoolCount;
			if (maxPoolSize >= 0 && current >= maxPoolSize) return;
			int add = Math.Min(toAdd, maxPoolSize < 0 ? toAdd : Math.Max(0, maxPoolSize - current));
			if (add <= 0) return;

			for (int i = 0; i < add; i++)
			{
				if (maxPoolSize >= 0 && PoolCount >= maxPoolSize) break;
				PawnKindDef kind = WanderingCaravanUtility.RandomSettlerKind();
				Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
					kind, faction, PawnGenerationContext.NonPlayer, map.Tile,
					false, false, false, true, kind.isFighter, 1f, true, true, true, true, true,
					false, false, false, false, 0f, 0f, null, 1f, null, null, null, null,
					null, null, null, null, null, null, null, null, false, false, false, false,
					null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null,
					false, false, false, -1, 0, false));
				if (pawn != null)
					AddToPool(pawn, SettlementJoinRequirement.GenerateForPawnKind(pawn.kindDef, ext));
			}
		}
	}
}
