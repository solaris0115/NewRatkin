using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Verse
{
	public class DamageWorker_ChainSword : DamageWorker_AddInjury
	{
		protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
		{
			return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, BodyPartDepth.Outside, null);
		}

		protected override void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageWorker.DamageResult result)
		{
			// Cut과 동일한 Inside 처리
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

			// 여러 부위에 데미지 분산 (Cut 스타일)
			NewRatkin.ChainSwordDamageExtension extension = this.def.GetModExtension<NewRatkin.ChainSwordDamageExtension>();
			int splitCount = extension != null ? extension.splitPartsCount : 3;

			// 사용 가능한 부위들 수집
			IEnumerable<BodyPartRecord> availableParts = dinfo.HitPart.GetDirectChildParts();
			if (dinfo.HitPart.parent != null)
			{
				availableParts = availableParts.Concat(dinfo.HitPart.parent);
				if (dinfo.HitPart.parent.parent != null)
				{
					availableParts = availableParts.Concat(dinfo.HitPart.parent.GetDirectChildParts());
				}
			}

			// 사용 가능한 부위 필터링
			List<BodyPartRecord> targetParts = (from x in availableParts.Except(dinfo.HitPart).InRandomOrder(null)
			where !x.def.conceptual && x.coverageAbs > 0f && x.depth == BodyPartDepth.Outside && !pawn.health.hediffSet.PartIsMissing(x)
			select x).Take(splitCount - 1).ToList<BodyPartRecord>();

			// 원래 HitPart 추가
			targetParts.Add(dinfo.HitPart);

			// 데미지를 부위 수로 나눔
			float damagePerPart = totalDamage / (float)targetParts.Count;
			
			// 각 부위에 데미지 적용
			for (int j = 0; j < targetParts.Count; j++)
			{
				DamageInfo dinfo3 = dinfo;
				dinfo3.SetHitPart(targetParts[j]);
				float finalDamage = base.ReduceDamageToPreserveOutsideParts(damagePerPart, dinfo3, pawn);
				base.FinalizeAndAddInjury(pawn, finalDamage, dinfo3, result);
			}
		}
	}
}

