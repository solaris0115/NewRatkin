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
			//간격
			int count = (from def in DefDatabase<PawnKindDef>.AllDefs where def.defaultFactionType == RatkinFactionDefOf.Rakinia select def).Count();
			int interval = Map.Size.x / ((int)Math.Sqrt(count) + 2);
			int x = 10;
			int y = 10;
			overRect = new CellRect(Map.Size.x / 4, Map.Size.z / 4, Map.Size.x/2,Map.Size.z/2);
			ClearArea(Map);
			Pawn p = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), RatkinPawnKindDefOf.RatkinColonist, Faction.OfPlayer);
			foreach (PawnKindDef pawnKindDef in (from def in DefDatabase<PawnKindDef>.AllDefs where def.defaultFactionType == RatkinFactionDefOf.Rakinia select def) )
			{
				if (x >= Map.Size.x)
				{;
					x = interval;
					y += interval;
				}
				//Log.Message(pawnKindDef.defName+"---------");
				for (int n=0; n<50;n++)
				{
					Pawn temp = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), pawnKindDef, Find.FactionManager.AllFactions.First(faction => faction.def == RatkinFactionDefOf.Rakinia));
					LongEventHandler.ExecuteWhenFinished(delegate { 
						temp.SetFaction(Faction.OfPlayer,p);
					});
				}
				x+= interval;
			}
		}

		[DebugAction("Mods", "Make RatkinTest (ForEach)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void MakeColonyForEach()
		{
			//간격
			int count = (from def in DefDatabase<PawnKindDef>.AllDefs where def.defaultFactionType == RatkinFactionDefOf.Rakinia select def).Count();
			int interval = 2;//Map.Size.x / ((int)Math.Sqrt(count) + 2);
			int x = 10;
			int y = 10;
			overRect = new CellRect(Map.Size.x / 4, Map.Size.z / 4, Map.Size.x / 2, Map.Size.z / 2);
			ClearArea(Map);
			Pawn p = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), RatkinPawnKindDefOf.RatkinColonist, Faction.OfPlayer);
			x += interval;
			foreach (PawnKindDef pawnKindDef in (from def in DefDatabase<PawnKindDef>.AllDefs where def.defaultFactionType == RatkinFactionDefOf.Rakinia select def))
			{
				if (x >= 22)
				{
					x = 10;
					y += interval;
				}
				Pawn temp = AutoTests_ColonyMaker.MakeRatkinColonists(new IntVec3(x, 0, y), pawnKindDef, Find.FactionManager.AllFactions.First(faction => faction.def == RatkinFactionDefOf.Rakinia));
				LongEventHandler.ExecuteWhenFinished(delegate {
					temp.SetFaction(Faction.OfPlayer, p);
				});
				x += interval;
			}
		}

		private static void ClearArea(Map map)
		{
			Thing.allowDestroyNonDestroyable = true;
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
			Thing.allowDestroyNonDestroyable = false;
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
