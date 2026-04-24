using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace NewRatkin
{
	/// <summary>
	/// 유랑단 캐러반의 정착 희망 유랑민 목록. 각각 받기/일괄 받기. 판매 없음.
	/// CanAcceptPawn 슬롯: 나중에 유랑민별 조건 추가 시 override. 현재는 항상 true.
	/// 성인기 Backstory, 상위 스킬 2가지(열정 포함) 표시.
	/// </summary>
	public class Dialog_CaravanSettlers : Window
	{
		private readonly Pawn trader;
		private List<Pawn> settlers;
		private Vector2 scrollPosition;
		private float scrollViewHeight;

		public Dialog_CaravanSettlers(Pawn trader, List<Pawn> settlers)
		{
			this.trader = trader;
			this.settlers = new List<Pawn>(settlers);
			optionalTitle = "RK_WanderingCaravan_SettleProposal".Translate();
			doCloseButton = true;
			doCloseX = true;
			absorbInputAroundWindow = true;
			forcePause = true;
		}

		public override Vector2 InitialSize => new Vector2(800f, 900f);

		public override void DoWindowContents(Rect inRect)
		{
			Rect scrollRect = new Rect(0f, 0f, inRect.width - 20f, inRect.height - 80f);
			Rect viewRect = new Rect(0f, 0f, scrollRect.width - 20f, scrollViewHeight);

			Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect, true);

			float y = 0f;
			List<Pawn> toRemove = new List<Pawn>();

			float leftPadding = 5f;
			float portraitSize = 140f;
			float rowHeight = 185f;

			foreach (Pawn pawn in settlers)
			{
				if (pawn.DestroyedOrNull() || pawn.Dead)
				{
					toRemove.Add(pawn);
					continue;
				}

				Rect rowRect = new Rect(0f, y, viewRect.width, rowHeight);
				Widgets.DrawHighlightIfMouseover(rowRect);

				// 전신 포트레이트 (왼쪽, 남쪽 방향)
				Rect portraitRect = new Rect(leftPadding, rowRect.y, portraitSize, portraitSize);
				RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(portraitSize, portraitSize), Rot4.South, default(Vector3), 1f, true, true, true, true, null, null, false, null);
				GUI.DrawTexture(portraitRect, portrait);

				float infoX = leftPadding + portraitSize + 10f;
				float buttonAreaWidth = 220f;
				float infoWidth = rowRect.width - infoX - buttonAreaWidth - 10f;

				// 이름 + PawnKind만
				Rect labelRect = new Rect(infoX, rowRect.y, infoWidth, 24f);
				Widgets.Label(labelRect, pawn.LabelShortCap + " - " + (pawn.kindDef?.label ?? "?"));

				float infoY = rowRect.y + 26f;

				// 합류 조건: DescShort + 필요 아이템/스킬 등 구체 수치(열거형) + 충족 여부
				var comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
				var req = comp?.GetRequirement(pawn);
				if (req != null)
				{
					bool met = req.IsMet(Find.CurrentMap);
					Color reqColor = met ? Color.green : Color.red;
					string details = req.GetRequirementDetails();
					// desc=분위기(line1), descShort=세부(line2). 생성·로드 시점에 이미 번역됨. 충족/미충족은 색상으로 표시.
					string line1 = req.Desc;
					string line2 = !string.IsNullOrEmpty(details) ? details : req.DescShort;
					string displayText = line1 + "\n" + line2;
					float reqHeight = Text.CalcHeight(displayText, infoWidth);
					reqHeight = Mathf.Min(Mathf.Max(reqHeight, 36f), 120f);
					Rect reqRect = new Rect(infoX, infoY, infoWidth, reqHeight);
					GUI.color = reqColor;
					Widgets.Label(reqRect, displayText);
					GUI.color = Color.white;
					infoY += reqHeight + 4f;
				}

				// 성인기 Backstory (일단 비표시)
				// if (pawn.story?.Adulthood != null)
				// {
				// 	Rect backstoryRect = new Rect(infoX, infoY, infoWidth, 36f);
				// 	Widgets.Label(backstoryRect, pawn.story.Adulthood.TitleCapFor(pawn.gender));
				// 	infoY += 38f;
				// }

				// 가장 높은 스킬 2가지 (열정 포함) - 일단 주석처리
				// string skillsStr = GetTopTwoSkillsDesc(pawn);
				// if (!string.IsNullOrEmpty(skillsStr))
				// {
				// 	Rect skillsRect = new Rect(infoX, infoY, infoWidth, 36f);
				// 	Widgets.Label(skillsRect, skillsStr);
				// }

				// 각각 받기 버튼 (조건 슬롯: CanAcceptPawn)
				bool canAccept = CanAcceptPawn(pawn);
				Rect acceptOneRect = new Rect(rowRect.width - buttonAreaWidth, rowRect.y + 5f, 100f, 35f);
				GUI.enabled = canAccept;
				if (Widgets.ButtonText(acceptOneRect, "RK_WanderingCaravan_AcceptOne".Translate()))
				{
					AcceptPawn(pawn);
					toRemove.Add(pawn);
				}
				GUI.enabled = true;

				// Info 버튼
				Rect infoRect = new Rect(rowRect.width - buttonAreaWidth + 110f, rowRect.y + 5f, 100f, 35f);
				if (Widgets.ButtonText(infoRect, "RK_WanderingTrader_Info".Translate()))
				{
					Find.WindowStack.Add(new Dialog_InfoCard(pawn));
				}

				y += rowHeight + 4f;
			}

			foreach (Pawn p in toRemove)
				settlers.Remove(p);

			scrollViewHeight = y;
			Widgets.EndScrollView();

			// 일괄 받기 버튼 (조건 충족한 유랑민만)
			List<Pawn> acceptableSettlers = settlers
				.Where(p => p != null && !p.DestroyedOrNull() && !p.Dead && CanAcceptPawn(p))
				.ToList();
			Rect acceptRect = new Rect(inRect.width - 220f, inRect.height - 45f, 200f, 40f);
			GUI.enabled = acceptableSettlers.Count > 0;
			if (Widgets.ButtonText(acceptRect, "RK_WanderingCaravan_AcceptAll".Translate()))
			{
				AcceptAllSettlers(acceptableSettlers);
				Close();
			}
			GUI.enabled = true;
		}

		/// <summary>
		/// 유랑민 수락 조건. GameComponent에서 합류 조건 조회 후 IsMet(map) 반환.
		/// 조건 없으면 true (레거시 호환).
		/// </summary>
		protected virtual bool CanAcceptPawn(Pawn pawn)
		{
			var comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
			var req = comp?.GetRequirement(pawn);
			return req == null || req.IsMet(Find.CurrentMap);
		}

		private void AcceptPawn(Pawn pawn)
		{
			Map map = Find.CurrentMap;
			if (map == null || pawn.DestroyedOrNull() || pawn.Dead)
				return;

			Current.Game.GetComponent<GameComponent_WanderingCaravan>()?.OnSettlerAccepted(pawn);

			Lord lord = pawn.GetLord();
			lord?.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);
			pawn.SetFaction(Faction.OfPlayer, null);
			Messages.Message("RK_WanderingCaravan_OneJoined".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
		}

		private static string GetTopTwoSkillsDesc(Pawn pawn)
		{
			if (pawn?.skills == null) return "";
			var skills = pawn.skills.skills
				.Where(s => !s.TotallyDisabled)
				.OrderByDescending(s => s.Level)
				.ThenByDescending(s => (int)s.passion)
				.Take(2)
				.ToList();
			if (skills.Count == 0) return "";
			return string.Join(", ", skills.Select(s =>
			{
				string passionStr = s.passion == Passion.Major ? " (*)" : (s.passion == Passion.Minor ? " (+) " : "");
				return s.def.label + " " + s.Level + passionStr;
			}));
		}

		private void AcceptAllSettlers(List<Pawn> pawns)
		{
			Map map = Find.CurrentMap;
			if (map == null || pawns == null || pawns.Count == 0)
				return;

			var comp = Current.Game.GetComponent<GameComponent_WanderingCaravan>();
			Lord lord = pawns[0].GetLord();
			foreach (Pawn pawn in pawns)
			{
				if (pawn.DestroyedOrNull() || pawn.Dead)
					continue;
				comp?.OnSettlerAccepted(pawn);
				lord?.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);
				pawn.SetFaction(Faction.OfPlayer, null);
			}

			Messages.Message("RK_WanderingCaravan_AllJoined".Translate(pawns.Count), MessageTypeDefOf.PositiveEvent);
		}
	}
}
