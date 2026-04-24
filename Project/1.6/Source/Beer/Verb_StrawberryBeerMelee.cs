using RimWorld;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 맥주 근접 공격 Verb - 공격 시 취기 Hediff 부여
	/// </summary>
	public class Verb_StrawberryBeerMelee : RimWorld.Verb_MeleeAttackDamage
	{
	protected override bool TryCastShot()
	{
		return base.TryCastShot();
	}

	protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
	{
		// 기본 데미지 적용
		DamageWorker.DamageResult result = base.ApplyMeleeDamageToTarget(target);
		
		// 타겟이 Pawn이고 살아있는 경우 취기 Hediff 부여
		Pawn targetPawn = target.Thing as Pawn;
		if (targetPawn != null && !targetPawn.Dead && targetPawn.RaceProps.Humanlike)
		{
			HediffDef alcoholHighDef = HediffDefOf.AlcoholHigh;
			if (alcoholHighDef != null && targetPawn.health != null && targetPawn.health.hediffSet != null)
			{
				Hediff existingHediff = targetPawn.health.hediffSet.GetFirstHediffOfDef(alcoholHighDef, false);
				if (existingHediff != null)
				{
					// 이미 있으면 약간 증가 (최대값 제한)
					existingHediff.Severity = System.Math.Min(existingHediff.def.maxSeverity, existingHediff.Severity + 0.03f);
				}
				else
				{
					// 없으면 새로 추가 (약한 수준)
					Hediff newHediff = HediffMaker.MakeHediff(alcoholHighDef, targetPawn);
					newHediff.Severity = 0.03f;
					targetPawn.health.AddHediff(newHediff);
				}
			}
		}
		
		return result;
	}
	}
}

