using UnityEngine;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 물건 의뢰 창 (초기 구현: 빈 셸, 추후 확장)
	/// </summary>
	public class Dialog_CaravanCommission : Window
	{
		public Dialog_CaravanCommission(Pawn leader)
		{
			optionalTitle = "RK_WanderingCaravan_Commission".Translate();
			doCloseButton = true;
			doCloseX = true;
			absorbInputAroundWindow = true;
			forcePause = true;
		}

		public override Vector2 InitialSize => new Vector2(400f, 200f);

		public override void DoWindowContents(Rect inRect)
		{
			Rect labelRect = new Rect(0f, 0f, inRect.width, 80f);
			Widgets.Label(labelRect, "RK_WanderingCaravan_CommissionPlaceholder".Translate());
		}
	}
}
