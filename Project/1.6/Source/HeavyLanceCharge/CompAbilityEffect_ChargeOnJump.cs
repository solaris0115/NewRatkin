using RimWorld;
using Verse;

namespace NewRatkin
{
	public class CompAbilityEffect_ChargeOnJump : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
	{
		private new CompProperties_ChargeOnJump Props => (CompProperties_ChargeOnJump)props;

		/// <summary>
		/// 어빌리티 사용 즉시 Hediff 부여. CompEquippableAbility 기반 능력은 PawnFlyer.RespawnPawn에서
		/// GetAbility(includeTemporary:false)로 찾지 못하므로, Verb에서 돌진 시작 시점에 호출.
		/// </summary>
		public void ApplyHediffsImmediately(Pawn pawn)
		{
			if (pawn == null) return;

			if (Props.exhaustionHediffDef != null)
			{
				Hediff exhaustion = HediffMaker.MakeHediff(Props.exhaustionHediffDef, pawn, null);
				Verse.HediffComp_Disappears compExhaustion = exhaustion.TryGetComp<Verse.HediffComp_Disappears>();
				if (compExhaustion != null)
					compExhaustion.SetDuration(Props.exhaustionDurationTicks);
				pawn.health.AddHediff(exhaustion, null, null, null);
			}

			if (Props.focusHediffDef != null)
			{
				Hediff focus = HediffMaker.MakeHediff(Props.focusHediffDef, pawn, null);
				Verse.HediffComp_Disappears compFocus = focus.TryGetComp<Verse.HediffComp_Disappears>();
				if (compFocus != null)
					compFocus.SetDuration(Props.focusDurationTicks);
				pawn.health.AddHediff(focus, null, null, null);
			}

			if (Props.momentumHediffDef != null)
			{
				Hediff momentum = HediffMaker.MakeHediff(Props.momentumHediffDef, pawn, null);
				Verse.HediffComp_Disappears compMomentum = momentum.TryGetComp<Verse.HediffComp_Disappears>();
				if (compMomentum != null)
					compMomentum.SetDuration(Props.momentumDurationTicks);
				pawn.health.AddHediff(momentum, null, null, null);
			}
		}

		public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
		{
			ApplyHediffsImmediately(parent.pawn);
		}

		public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
		{
			if (target.Pawn == null)
				return false;
			if (Props.onlyHostilePawns && !target.Pawn.HostileTo(parent.pawn))
			{
				if (throwMessages)
					Messages.Message("CannotUseAbility".Translate(parent.def.label) + ": " + "RK_AbilityMustTargetHostile".Translate(), target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
				return false;
			}
			return base.Valid(target, throwMessages);
		}

		public override bool AICanTargetNow(LocalTargetInfo target)
		{
			if (target.Pawn == null)
				return false;
			if (Props.onlyHostilePawns && !target.Pawn.HostileTo(parent.pawn))
				return false;
			return true;
		}
	}
}
