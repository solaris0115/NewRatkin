using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using System.Text;

namespace NewRatkin
{
    [StaticConstructorOnStartup]
    public class Shield : Apparel
    {
        private const float BLOCK_RATE_FACTOR_BY_SKILL = 0.02f;

        public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
        {
            Pawn pawn = Wearer;
            if (!pawn.Dead && !pawn.Downed)
            {
                float attackerAngle = dinfo.Angle+180;
                float defenderAngle = pawn.Rotation.AsAngle;
                if (attackerAngle >= 360)
                {
                    attackerAngle += -360;
                }
                if (defenderAngle- attackerAngle >=-70 && defenderAngle - attackerAngle<=70)
                {
                    float blockRateBySkill = GetDeflectChanceByMeleeSkillLevel(pawn.skills.GetSkill(SkillDefOf.Melee).levelInt);
                    float armorBlockRateByStuff = 0;
                    switch (dinfo.Def.armorCategory)
                    {
                        case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Sharp:
                            armorBlockRateByStuff = this.GetStatValue(StatDefOf.ArmorRating_Sharp);
                            break;
                        case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Blunt:
                            armorBlockRateByStuff = this.GetStatValue(StatDefOf.ArmorRating_Blunt);
                            break;
                        case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Heat:
                            armorBlockRateByStuff = this.GetStatValue(StatDefOf.ArmorRating_Heat);
                            break;
                        default:
                            break;
                    }
                    float clampedBlockRateFromStuff = GetDeflectChanceByArmorRate(armorBlockRateByStuff);
                    var totalDeflectChance = blockRateBySkill + clampedBlockRateFromStuff;

                    if (Rand.Value <= totalDeflectChance)
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "ShieldBlock".Translate(), 1.9f);
                        EffecterDefOf.Deflect_Metal.Spawn().Trigger(pawn, dinfo.Instigator ?? pawn);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool AllowVerbCast(Verb verb)
        {
            return true;
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            StringBuilder sharp = new StringBuilder();
            StringBuilder blunt = new StringBuilder();
            StringBuilder heat = new StringBuilder();

            float delfectChanceSharp = GetDeflectChanceByArmorRate(this.GetStatValue(StatDefOf.ArmorRating_Sharp));
            float deflectChanceBlunt = GetDeflectChanceByArmorRate(this.GetStatValue(StatDefOf.ArmorRating_Blunt));
            float deflectChanceHeat = GetDeflectChanceByArmorRate(this.GetStatValue(StatDefOf.ArmorRating_Heat));

            sharp.AppendLine("BlockChanceDefualtDesc".Translate());
            blunt.AppendLine("BlockChanceDefualtDesc".Translate());
            heat.AppendLine("BlockChanceDefualtDesc".Translate());
            var deflectChanceByMeleeSkill = 0f;

            if (Prefs.DevMode)
            {
                if (Wearer != null)
                {
                    var meleeSkillLevel = Wearer.skills.GetSkill(SkillDefOf.Melee).levelInt;
                    deflectChanceByMeleeSkill = GetDeflectChanceByMeleeSkillLevel(meleeSkillLevel);

                    sharp.AppendLine($"\n{SkillDefOf.Melee.LabelCap}({meleeSkillLevel}): {deflectChanceByMeleeSkill.ToStringPercent()}" +
                        $"\n{StatDefOf.ArmorRating_Sharp.LabelCap}: {delfectChanceSharp.ToStringPercent()}" +
                        $"\n{"StatsReport_FinalValue".Translate()}: {(delfectChanceSharp + deflectChanceByMeleeSkill).ToStringPercent()}");

                    blunt.AppendLine($"\n{SkillDefOf.Melee.LabelCap}({meleeSkillLevel}): {deflectChanceByMeleeSkill.ToStringPercent()}" +
                        $"\n{StatDefOf.ArmorRating_Blunt.LabelCap}: {deflectChanceBlunt.ToStringPercent()}" +
                        $"\n{"StatsReport_FinalValue".Translate()}: {(deflectChanceBlunt + deflectChanceByMeleeSkill).ToStringPercent()}");

                    heat.AppendLine($"\n{SkillDefOf.Melee.LabelCap}({meleeSkillLevel}): {deflectChanceByMeleeSkill.ToStringPercent()}" +
                        $"\n{StatDefOf.ArmorRating_Heat.LabelCap}: {deflectChanceHeat.ToStringPercent()}" +
                        $"\n{"StatsReport_FinalValue".Translate()}: {(deflectChanceHeat + deflectChanceByMeleeSkill).ToStringPercent()}");
                }
                else
                {
                    sharp.AppendLine($"\n{StatDefOf.ArmorRating_Sharp.LabelCap}: {this.GetStatValue(StatDefOf.ArmorRating_Sharp).ToStringPercent()}\n{"StatsReport_FinalValue".Translate()}: {delfectChanceSharp.ToStringPercent()}({"CanLow".Translate()})");
                    blunt.AppendLine($"\n{StatDefOf.ArmorRating_Blunt.LabelCap}: {this.GetStatValue(StatDefOf.ArmorRating_Blunt).ToStringPercent()}\n{"StatsReport_FinalValue".Translate()}: {deflectChanceBlunt.ToStringPercent()}({"CanLow".Translate()})");
                    heat.AppendLine($"\n{StatDefOf.ArmorRating_Heat.LabelCap}: {this.GetStatValue(StatDefOf.ArmorRating_Heat).ToStringPercent()}\n{"StatsReport_FinalValue".Translate()}: {deflectChanceHeat.ToStringPercent()}({"CanLow".Translate()})");
                }
            }
            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "BlockChance_Heat".Translate(), (deflectChanceHeat + deflectChanceByMeleeSkill).ToStringPercent(), heat.ToString(), 20);
            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "BlockChance_Blunt".Translate(), (deflectChanceBlunt + deflectChanceByMeleeSkill).ToStringPercent(), blunt.ToString(), 20);
            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "BlockChance_Sharp".Translate(), (delfectChanceSharp + deflectChanceByMeleeSkill).ToStringPercent(), sharp.ToString(), 20);
        }

        /// <summary>
        /// 소재로 인한 튕겨낼 확률을 최대 50% 이하로 고정시켜버린다.
        /// </summary>
        private float GetDeflectChanceByArmorRate(float armorRate)
        {
            return Mathf.Clamp01(armorRate / 2) / 2;
        }
        /// <summary>
        /// 근접 전투 스킬 레벨 기반 공격을 튕겨낼 확률
        /// </summary>
        private float GetDeflectChanceByMeleeSkillLevel(int level)
        {
            return level * BLOCK_RATE_FACTOR_BY_SKILL;
        }
    }

}
