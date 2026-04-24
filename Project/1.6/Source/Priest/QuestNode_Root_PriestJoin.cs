using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 랫킨 사제 합류 퀘스트 노드
	/// 바닐라 QuestNode_Root_WandererJoin_WalkIn을 상속받아 GeneratePawn()만 오버라이드
	/// </summary>
	public class QuestNode_Root_PriestJoin : QuestNode_Root_WandererJoin_WalkIn
	{
		public override Pawn GeneratePawn()
		{
			// 랫킨 사제 PawnKindDef 가져오기
			PawnKindDef priestKind = DefDatabase<PawnKindDef>.GetNamed("RatkinPriest", false);
			if (priestKind == null)
			{
				return base.GeneratePawn();
			}

			PawnGenerationRequest request = new PawnGenerationRequest(
				priestKind,
				null, // faction - null이면 나중에 플레이어 팩션으로 설정됨
				PawnGenerationContext.NonPlayer,
				-1,
				true, // forceGenerateNewPawn
				false, // allowDead
				false, // allowDowned
				true, // canGeneratePawnRelations
				false, // mustBeCapableOfViolence
				20f, // colonistRelationChanceFactor
				false, // forceAddFreeWarmLayerIfNeeded
				true, // allowGay
				true, // allowPregnant
				true, // allowFood
				true, // allowAddictions
				false, // inhabitant
				false, // certainlyBeenInCryptosleep
				false, // forceRedressWorldPawnIfFormerColonist
				false, // worldPawnFactionDoesntMatter
				0f, // biocodeWeaponChance
				0f, // biocodeApparelChance
				null, // extraPawnForExtraRelationChance
				1f, // relationWithExtraPawnChanceFactor
				null, // validatorPreGear
				null, // validatorPostGear
				null, // forcedTraits
				null, // prohibitedTraits
				null, // minChanceToRedressWorldPawn
				null, // fixedBiologicalAge
				null, // fixedChronologicalAge
				null, // fixedGender
				null, // fixedLastName
				null, // fixedBirthName
				null, // fixedTitle
				null, // fixedIdeo
				false, // forceNoIdeo
				false, // forceNoBackstory
				false, // forbidAnyTitle
				false, // forceDead
				null, // forcedXenotype
				null, // forcedCustomXenotype
				null, // allowedXenotypes
				null, // forcedEndogenes
				null, // forcedXenogenes
				0f, // forceBaselinerChance
				DevelopmentalStage.Adult
			);

			// 아이 허용 설정 확인
			if (Find.Storyteller.difficulty.ChildrenAllowed)
			{
				request.AllowedDevelopmentalStages |= DevelopmentalStage.Child;
			}

			Pawn pawn = PawnGenerator.GeneratePawn(request);

			// PawnGenerator 패치가 적용되지 않는 경우 대비 - QuestNode에서 직접 기도회 능력 부여
			if (pawn.abilities != null && pawn.abilities.GetAbility(RatkinAbilityDefOf.RK_PrayerService) == null)
			{
				pawn.abilities.GainAbility(RatkinAbilityDefOf.RK_PrayerService);
			}

			if (!pawn.IsWorldPawn())
			{
				Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
			}

			return pawn;
		}
	}
}
