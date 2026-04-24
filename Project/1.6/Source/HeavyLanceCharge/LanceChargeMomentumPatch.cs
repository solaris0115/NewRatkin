using HarmonyLib;
using RimWorld;
using Verse;

namespace NewRatkin
{
	/// <summary>
	/// 근접 공격 완료 시 RK_Hediff_LanceChargeMomentum 제거.
	/// TryCastShot Postfix는 피해 적용(ApplyMeleeDamageToTarget) 이후에 실행되므로,
	/// 첫 공격에는 MeleeDamageFactor 보너스가 적용된 뒤 hediff가 소멸함.
	/// </summary>
	[StaticConstructorOnStartup]
	public static class LanceChargeMomentumPatch
	{
		private static readonly HediffDef MomentumHediffDef = DefDatabase<HediffDef>.GetNamed("RK_Hediff_LanceChargeMomentum", false);

		static LanceChargeMomentumPatch()
		{
			if (MomentumHediffDef == null) return;

			Harmony harmony = new Harmony("com.NewRatkin.rimworld.mod.lancecharge");
			harmony.Patch(
				AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot"),
				postfix: new HarmonyMethod(typeof(LanceChargeMomentumPatch), nameof(TryCastShot_Postfix))
			);
		}

		public static void TryCastShot_Postfix(Verb __instance)
		{
			Pawn caster = __instance.CasterPawn;
			if (caster == null || caster.health?.hediffSet == null) return;

			Hediff momentum = caster.health.hediffSet.GetFirstHediffOfDef(MomentumHediffDef);
			if (momentum != null)
				caster.health.RemoveHediff(momentum);
		}
	}
}
