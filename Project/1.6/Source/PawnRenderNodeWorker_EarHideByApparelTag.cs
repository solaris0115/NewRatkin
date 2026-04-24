using RimWorld;
using UnityEngine;

namespace Verse
{
	public class PawnRenderNodeWorker_EarHideByApparelTag : PawnRenderNodeWorker_FlipWhenCrawling
	{
		public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
		{
			// 의류가 없으면 바로 통과
			if (parms.pawn.apparel == null || parms.pawn.apparel.WornApparelCount == 0)
			{
				return base.CanDrawNow(node, parms);
			}
			
			PawnRenderNodeProperties_EarHideByApparelTag props = node.Props as PawnRenderNodeProperties_EarHideByApparelTag;
			
			// 숨김 태그가 설정되지 않았으면 바로 통과
			if (props == null || props.hiddenUnderApparelTags == null || props.hiddenUnderApparelTags.Count == 0)
			{
				return base.CanDrawNow(node, parms);
			}
			
			// XML에서 설정한 태그를 가진 의류 착용 시 귀 숨김
			foreach (Apparel apparel in parms.pawn.apparel.WornApparel)
			{
				if (apparel.def.apparel.tags != null)
				{
					foreach (string tag in apparel.def.apparel.tags)
					{
						if (props.hiddenUnderApparelTags.Contains(tag))
						{
							return false;
						}
					}
				}
			}
			
			return base.CanDrawNow(node, parms);
		}
	}
}

