using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace NewRatkin
{
	public class IncidentDefExtension_WandererJoin : DefModExtension
	{
		public List<PawnKindCount> pawnKindCounts = new List<PawnKindCount>();
	}

	public class PawnKindCount
	{
		public PawnKindDef pawnKind;
		public int count = 1;
		// PawnKind별 폭력 요구사항 설정 (null이면 IncidentDef의 pawnMustBeCapableOfViolence 사용)
		public bool? mustBeCapableOfViolence = null;
	}

	public class IncidentWorker_WandererJoin : IncidentWorker
	{
		private const float RelationWithColonistWeight = 20f;

		protected override bool CanFireNowSub(IncidentParms parms)
		{
			if (!base.CanFireNowSub(parms))
			{
				return false;
			}
			Map map = (Map)parms.target;
			return this.CanSpawnJoiner(map);
		}

		public virtual Pawn GeneratePawn(PawnKindDef pawnKind, bool? mustBeCapableOfViolenceOverride = null)
		{
			Gender? gender = null;
			if (this.def.pawnFixedGender != Gender.None)
			{
				gender = new Gender?(this.def.pawnFixedGender);
			}
			Ideo ideo = null;
			if (ModsConfig.IdeologyActive)
			{
				if (!(from i in Find.IdeoManager.IdeosListForReading
				where !Faction.OfPlayer.ideos.Has(i)
				select i).TryRandomElementByWeight((Ideo x) => IdeoUtility.IdeoChangeToWeight(null, x), out ideo))
				{
					(from i in Find.IdeoManager.IdeosListForReading
					where !Faction.OfPlayer.ideos.IsPrimary(i)
					select i).TryRandomElementByWeight((Ideo x) => IdeoUtility.IdeoChangeToWeight(null, x), out ideo);
				}
			}
			Faction ofPlayer = Faction.OfPlayer;
			PawnGenerationContext context = PawnGenerationContext.NonPlayer;
			// PawnKind별 설정이 있으면 우선 사용, 없으면 IncidentDef의 기본값 사용
			bool pawnMustBeCapableOfViolence = mustBeCapableOfViolenceOverride ?? this.def.pawnMustBeCapableOfViolence;
			Gender? fixedGender = gender;
			Ideo fixedIdeo = ideo;
			// 최소 나이 20세 설정
			FloatRange? biologicalAgeRange = new FloatRange(20f, float.MaxValue);
			return PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, ofPlayer, context, null, true, false, false, true, pawnMustBeCapableOfViolence, 20f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, fixedGender, null, null, null, fixedIdeo, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, biologicalAgeRange, false, false, false, -1, 0, false));
		}

		public virtual bool CanSpawnJoiner(Map map)
		{
			IntVec3 intVec;
			return this.TryFindEntryCell(map, out intVec);
		}

		public virtual void SpawnJoiner(Map map, Pawn pawn, IntVec3 spawnCell)
		{
			GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (!this.CanSpawnJoiner(map))
			{
				return false;
			}

			// 모든 pawn을 같은 위치에 spawn하기 위해 한 번만 위치 찾기
			IntVec3 spawnCell;
			if (!this.TryFindEntryCell(map, out spawnCell))
			{
				return false;
			}

			IncidentDefExtension_WandererJoin extension = this.def.GetModExtension<IncidentDefExtension_WandererJoin>();
			List<Pawn> spawnedPawns = new List<Pawn>();

			// 여러 pawnKind 지원
			if (extension != null && extension.pawnKindCounts != null && extension.pawnKindCounts.Count > 0)
			{
				foreach (PawnKindCount pawnKindCount in extension.pawnKindCounts)
				{
					if (pawnKindCount.pawnKind == null || pawnKindCount.count <= 0)
					{
						continue;
					}
					for (int i = 0; i < pawnKindCount.count; i++)
					{
						Pawn pawn = this.GeneratePawn(pawnKindCount.pawnKind, pawnKindCount.mustBeCapableOfViolence);
						if (pawn == null)
						{
							RatkinLimitedLog.Warning(RatkinLogKeys.WandererJoin_GenerateFailedKinds, $"Failed to generate pawn of kind {pawnKindCount.pawnKind?.defName ?? "null"} after multiple attempts. Skipping.");
							continue;
						}
						this.SpawnJoiner(map, pawn, spawnCell);
						if (this.def.pawnHediff != null)
						{
							pawn.health.AddHediff(this.def.pawnHediff, null, null, null);
						}
						spawnedPawns.Add(pawn);
					}
				}
			}
			// 기존 단일 pawnKind 지원 (하위 호환성)
			else if (this.def.pawnKind != null)
			{
				Pawn pawn = this.GeneratePawn(this.def.pawnKind);
				if (pawn == null)
				{
					RatkinLimitedLog.Warning(RatkinLogKeys.WandererJoin_GenerateFailedSingle, $"Failed to generate pawn of kind {this.def.pawnKind.defName} after multiple attempts.");
					return false;
				}
				this.SpawnJoiner(map, pawn, spawnCell);
				if (this.def.pawnHediff != null)
				{
					pawn.health.AddHediff(this.def.pawnHediff, null, null, null);
				}
				spawnedPawns.Add(pawn);
			}
			else
			{
				return false;
			}

			if (spawnedPawns.Count == 0)
			{
				return false;
			}

			// 편지 생성
			Pawn firstPawn = spawnedPawns[0];
			TaggedString baseLetterText;
			if (this.def.pawnHediff != null)
			{
				baseLetterText = this.def.letterText.Formatted(firstPawn.Named("PAWN"), this.def.pawnHediff.Named("HEDIFF")).AdjustedFor(firstPawn, "PAWN", true);
			}
			else
			{
				baseLetterText = this.def.letterText.Formatted(firstPawn.Named("PAWN")).AdjustedFor(firstPawn, "PAWN", true);
			}

			// 여러 명인 경우 정보 추가
			if (spawnedPawns.Count > 1)
			{
				baseLetterText += "\n\n";
				for (int i = 0; i < spawnedPawns.Count; i++)
				{
					if (i > 0)
					{
						baseLetterText += ", ";
					}
					baseLetterText += spawnedPawns[i].LabelShortCap;
				}
			}

			TaggedString baseLetterLabel = this.def.letterLabel.Formatted(firstPawn.Named("PAWN")).AdjustedFor(firstPawn, "PAWN", true);
			PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref baseLetterText, ref baseLetterLabel, firstPawn);
			base.SendStandardLetter(baseLetterLabel, baseLetterText, LetterDefOf.PositiveEvent, parms, firstPawn, Array.Empty<NamedArgument>());
			return true;
		}

		private bool TryFindEntryCell(Map map, out IntVec3 cell)
		{
			return CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out cell);
		}
	}
}

