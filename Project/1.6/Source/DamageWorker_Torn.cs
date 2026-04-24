using System;
using System.Collections.Generic;
using System.Linq;

namespace Verse
{
	public class DamageWorker_Torn : DamageWorker_AddInjury
	{
		protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
		{
			return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, BodyPartDepth.Outside, null);
		}

		protected override void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageWorker.DamageResult result)
		{
			if (dinfo.HitPart.depth == BodyPartDepth.Inside)
			{
				List<BodyPartRecord> list = new List<BodyPartRecord>();
				for (BodyPartRecord bodyPartRecord = dinfo.HitPart; bodyPartRecord != null; bodyPartRecord = bodyPartRecord.parent)
				{
					list.Add(bodyPartRecord);
					if (bodyPartRecord.depth == BodyPartDepth.Outside)
					{
						break;
					}
				}
				float num = (float)(list.Count - 1) + 0.5f;
				for (int i = 0; i < list.Count; i++)
				{
					DamageInfo dinfo2 = dinfo;
					dinfo2.SetHitPart(list[i]);
					base.FinalizeAndAddInjury(pawn, totalDamage / num * ((i == 0) ? 0.5f : 1f), dinfo2, result);
				}
				return;
			}
			int num2 = (this.def.cutExtraTargetsCurve != null) ? GenMath.RoundRandom(this.def.cutExtraTargetsCurve.Evaluate(Rand.Value)) : 0;
			List<BodyPartRecord> list2;
			if (num2 != 0)
			{
				// 모든 컬렉션을 먼저 리스트로 변환하여 즉시 평가 (컬렉션 수정 문제 방지)
				List<BodyPartRecord> availableParts = new List<BodyPartRecord>();
				
				// HitPart의 직접 자식들
				availableParts.AddRange(dinfo.HitPart.GetDirectChildParts());
				
				// 부모 부위
				if (dinfo.HitPart.parent != null)
				{
					availableParts.Add(dinfo.HitPart.parent);
					
					// 부모의 직접 자식들
					if (dinfo.HitPart.parent.parent != null)
					{
						availableParts.AddRange(dinfo.HitPart.parent.GetDirectChildParts());
					}
				}
				
				// HitPart 제외하고 필터링 후 랜덤 선택
				list2 = (from x in availableParts
				where x != dinfo.HitPart && !x.def.conceptual && x.coverageAbs > 0f
				select x).InRandomOrder(null).Take(num2).ToList<BodyPartRecord>();
			}
			else
			{
				list2 = new List<BodyPartRecord>();
			}
			list2.Add(dinfo.HitPart);
			float num3 = totalDamage * (1f + this.def.cutCleaveBonus) / ((float)list2.Count + this.def.cutCleaveBonus);
			if (num2 == 0)
			{
				num3 = base.ReduceDamageToPreserveOutsideParts(num3, dinfo, pawn);
			}
			for (int j = 0; j < list2.Count; j++)
			{
				DamageInfo dinfo3 = dinfo;
				dinfo3.SetHitPart(list2[j]);
				base.FinalizeAndAddInjury(pawn, num3, dinfo3, result);
			}
		}
	}
}

