using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using LudeonTK;

namespace NewRatkin
{
	public static class DebugAutotests
	{
		public static CellRect overRect;
		private static Map Map
		{
			get
			{
				return Find.CurrentMap;
			}
		}
		[DebugAction("Mods", "Make RatkinTest (Full)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void MakeColonyFull()
		{
			// 최소 권장 맵 크기: 75x75
			const int minMapSize = 75;
			if (Map.Size.x < minMapSize || Map.Size.z < minMapSize)
			{
				string message = $"맵 크기가 너무 작습니다! 현재 맵 크기: {Map.Size.x}x{Map.Size.z}, 최소 권장 크기: {minMapSize}x{minMapSize}. 더 큰 맵에서 실행해주세요.";
				Messages.Message(message, MessageTypeDefOf.RejectInput);
				// 경고음 재생 (RejectInput 메시지 타입이 자동으로 경고음을 재생함)
				return;
			}
			
			//간격
			int count = (from pawnKindDef in DefDatabase<PawnKindDef>.AllDefs where pawnKindDef.defaultFactionDef == RatkinFactionDefOf.Rakinia select pawnKindDef).Count();
			if (count == 0)
			{
				RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_NoPawnKindFull, "No Ratkin PawnKindDef found!");
				return;
			}
			
			// 맵 크기 고려: 50명을 배치할 수 있도록 interval 계산
			// 사용 가능한 공간: Map.Size.x - 10 (시작점) - 5 (여유공간)
			int availableWidth = Map.Size.x - 15;
			int minInterval = Math.Max(2, availableWidth / 50); // 50명을 배치하기 위한 최소 interval
			int interval = Math.Max(minInterval, Map.Size.x / ((int)Math.Sqrt(count) + 2));
			
			int x = 10;
			int y = 10;
			overRect = new CellRect(Map.Size.x / 4, Map.Size.z / 4, Map.Size.x/2,Map.Size.z/2);
			ClearArea(Map);
			Pawn p = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), RatkinPawnKindDefOf.RatkinColonist, Faction.OfPlayer);
			Faction ratkinFaction = Find.FactionManager.AllFactions.FirstOrDefault(faction => faction.def == RatkinFactionDefOf.Rakinia);
			if (ratkinFaction == null)
			{
				RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_NoFactionFull, "Ratkin faction not found!");
				return;
			}
			foreach (PawnKindDef pawnKindDef in (from def in DefDatabase<PawnKindDef>.AllDefs where def.defaultFactionDef == RatkinFactionDefOf.Rakinia select def) )
			{
				//Log.Message(pawnKindDef.defName+"---------");
				for (int n=0; n<50;n++)
				{
					// 맵 경계 체크: x가 맵 밖으로 나가기 전에 다음 줄로 이동
					if (x >= Map.Size.x - 5)
					{
						x = 10;
						y += interval;
						if (y >= Map.Size.z - 5)
						{
							RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_NotEnoughSpaceFull, "Not enough space on map! Stopping at " + pawnKindDef.defName + " (n=" + n + ")");
							break;
						}
					}
					
					// 위치가 맵 경계 내인지 확인
					if (!new IntVec3(x, 0, y).InBounds(Map))
					{
						RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_PositionOobFull, "Position out of bounds! Stopping at " + pawnKindDef.defName + " (n=" + n + ", x=" + x + ", y=" + y + ")");
						break;
					}
					
					Pawn temp = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), pawnKindDef, ratkinFaction);
					// 클로저 문제 해결: 지역 변수로 복사
					Pawn capturedPawn = temp;
					LongEventHandler.ExecuteWhenFinished(delegate { 
						if (capturedPawn != null && !capturedPawn.Destroyed)
						{
							capturedPawn.SetFaction(Faction.OfPlayer, p);
						}
					});
					x += interval;
				}
				// 다음 PawnKindDef를 위해 다음 줄로 이동
				x = 10;
				y += interval;
				if (y >= Map.Size.z - 5)
				{
					RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_NotEnoughSpaceFullEnd, "Not enough space on map! Stopping at " + pawnKindDef.defName);
					break;
				}
			}
		}

		[DebugAction("Mods", "Make RatkinTest (ForEach)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void MakeColonyForEach()
		{
			// 최소 권장 맵 크기: 75x75
			const int minMapSize = 75;
			if (Map.Size.x <= minMapSize || Map.Size.z <= minMapSize)
			{
				string message = $"맵 크기가 너무 작습니다! 현재 맵 크기: {Map.Size.x}x{Map.Size.z}, 최소 권장 크기: {minMapSize}x{minMapSize}. 더 큰 맵에서 실행해주세요.";
				Messages.Message(message, MessageTypeDefOf.RejectInput);
				// 경고음 재생 (RejectInput 메시지 타입이 자동으로 경고음을 재생함)
				return;
			}
			
			//간격
			int count = (from def in DefDatabase<PawnKindDef>.AllDefs where def.defaultFactionDef == RatkinFactionDefOf.Rakinia select def).Count();
			if (count == 0)
			{
				RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_NoPawnKindForEach, "No Ratkin PawnKindDef found!");
				return;
			}
			int interval = 2;
			int x = 10;
			int y = 10;
			overRect = new CellRect(Map.Size.x / 4, Map.Size.z / 4, Map.Size.x / 2, Map.Size.z / 2);
			ClearArea(Map);
			Pawn p = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), RatkinPawnKindDefOf.RatkinColonist, Faction.OfPlayer);
			x += interval;
			Faction ratkinFaction = Find.FactionManager.AllFactions.FirstOrDefault(faction => faction.def == RatkinFactionDefOf.Rakinia);
			if (ratkinFaction == null)
			{
				RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_NoFactionForEach, "Ratkin faction not found!");
				return;
			}
			foreach (PawnKindDef pawnKindDef in (from def in DefDatabase<PawnKindDef>.AllDefs where def.defaultFactionDef == RatkinFactionDefOf.Rakinia select def))
			{
				// 맵 경계 체크
				if (x >= Map.Size.x - 5)
				{
					x = 10;
					y += interval;
					if (y >= Map.Size.z - 5)
					{
						RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_NotEnoughSpaceForEach, "Not enough space on map! Stopping at " + pawnKindDef.defName);
						break;
					}
				}
				
				// 위치가 맵 경계 내인지 확인
				if (!new IntVec3(x, 0, y).InBounds(Map))
				{
					RatkinLimitedLog.Warning(RatkinLogKeys.AutoTests_PositionOobForEach, "Position out of bounds! Stopping at " + pawnKindDef.defName + " (x=" + x + ", y=" + y + ")");
					break;
				}
				
				Pawn temp = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), pawnKindDef, ratkinFaction);
				// 클로저 문제 해결: 지역 변수로 복사
				Pawn capturedPawn = temp;
				LongEventHandler.ExecuteWhenFinished(delegate {
					if (capturedPawn != null && !capturedPawn.Destroyed)
					{
						capturedPawn.SetFaction(Faction.OfPlayer, p);
					}
				});
				x += interval;
			}
		}

		private static void ClearArea(Map map)
		{
			if (map == null)
			{
				return;
			}
			Thing.allowDestroyNonDestroyable = true;
			try
			{
				// overRect 영역만 정리 (성능 개선)
				if (overRect.Width > 0 && overRect.Height > 0)
				{
					foreach (IntVec3 c in overRect)
					{
						if (c.InBounds(map))
						{
							map.roofGrid.SetRoof(c, null);
							foreach (Thing thing in c.GetThingList(map).ToList())
							{
								thing.Destroy(DestroyMode.Vanish);
							}
						}
					}
				}
				else
				{
					// overRect가 유효하지 않으면 전체 맵 정리 (기존 동작)
					foreach (IntVec3 c in map)
					{
						map.roofGrid.SetRoof(c, null);
					}
					foreach (IntVec3 c2 in map)
					{
						foreach (Thing thing in c2.GetThingList(map).ToList())
						{
							thing.Destroy(DestroyMode.Vanish);
						}
					}
				}
			}
			finally
			{
				Thing.allowDestroyNonDestroyable = false;
			}
		}
	}



	public static class AutoTests_ColonyMaker
	{
		private static Map Map
		{
			get
			{
				return Find.CurrentMap;
			}
		}
		public static Pawn MakeRatkinColonists(IntVec3 position, PawnKindDef pawnKind,Faction faction)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(pawnKind, faction);
			GenSpawn.Spawn(pawn, position, Map,WipeMode.Vanish);
			pawn.Name = new NameTriple(pawn.kindDef.defName, pawn.kindDef.defName, pawn.kindDef.defName);
			return pawn;
			//pawn.SetFaction(Faction.OfPlayer,recruiter);
			//Log.Message(pawn.kindDef.defName);

		}
	}
}
