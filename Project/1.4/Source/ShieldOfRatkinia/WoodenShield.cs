using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using System.Text;

namespace NewRatkin
{
    [StaticConstructorOnStartup]
    public class Shield : Apparel
    {
        private readonly string path = "Apparel/";
        private Graphic shieldGraphic;
        private static Graphic heavyShieldGraphic = GraphicDatabase.Get<Graphic_Multi>("Apparel/RK_HeavyShield", ShaderDatabase.CutoutComplex, new Vector2(1, 1), new Color(1, 1, 1, 1));
        private static Graphic woodenShieldGraphic = GraphicDatabase.Get<Graphic_Multi>("Apparel/RK_WoodenShield", ShaderDatabase.Cutout, new Vector2(1, 1), new Color(1,1,1,1));

        static readonly Vector3 drawDraftedLocNorth = new Vector3(-0.2f, -0.2f, -0.09f);
        static readonly Vector3 drawDraftedLocSouth = new Vector3(0.2f, 0.2f, -0.15f);
        static readonly Vector3 drawDraftedLocEast = new Vector3(0.2f, -0.2f, -0.2f);
        static readonly Vector3 drawDraftedLocWest = new Vector3(-0.2f, 0.2f, -0.15f);

        static readonly Vector3 drawBackLocNorth = new Vector3(0f, 0.2f, -0.2f);
        static readonly Vector3 drawBackLocSouth = new Vector3(0f, -0.2f, -0.09f);
        static readonly Vector3 drawBackLocEast = new Vector3(-0.15f, 0.05f, -0.07f);
        static readonly Vector3 draWBackLocWest = new Vector3(0.15f, -2f, -0.07f);


        private const float BLOCK_RATE_FACTOR_BY_SKILL = 0.02f;

        private bool ShouldShieldUp
        {
            get
            {
                Pawn wearer = Wearer;
                return wearer.Spawned && 
                    (wearer.InAggroMentalState || wearer.Drafted || wearer.Drafted || (wearer.CurJob != null && wearer.CurJob.def.alwaysShowWeapon) || (wearer.mindState.duty != null && wearer.mindState.duty.def.alwaysShowWeapon));
            }
        }
        public override void PostMake()
        {
            base.PostMake();
            if (shieldGraphic != null) { return; }

            Action finishAction = () =>
            {
                //나무 방패의 경우 소재 안따라가도록 세팅.
                Color shieldColor = Color.white;
                if (def.defName != "RK_WoodenShield") { shieldColor = Stuff.stuffProps.color; }

                shieldGraphic = GraphicDatabase.Get<Graphic_Multi>(path + def.defName, ShaderDatabase.Cutout, def.graphicData.drawSize, shieldColor);
            };
            LongEventHandler.ExecuteWhenFinished(finishAction);

        }

        public override void ExposeData()
        {
            base.ExposeData();
            if(Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (shieldGraphic == null)
                    {
                        if(def.defName == "RK_WoodenShield")
                        {
                            shieldGraphic = GraphicDatabase.Get<Graphic_Multi>(path + def.defName, ShaderDatabase.Cutout, def.graphicData.drawSize, Color.white);
                        }
                        else
                        {
                            shieldGraphic = GraphicDatabase.Get<Graphic_Multi>(path + def.defName, ShaderDatabase.Cutout, def.graphicData.drawSize, Stuff.stuffProps.color);
                        }
                    }
                });
            }
        }


        public override void DrawWornExtras()
        {
            Pawn pawn = Wearer;
            Vector3 rootLoc = pawn.DrawPos;
            if (ShouldShieldUp)
            {
                switch(pawn.Rotation.AsInt)
                {
                    case 0:
                        DrawShield(shieldGraphic.MatNorth, rootLoc +drawDraftedLocNorth, 0);
                        break;
                    case 1:
                        DrawShield(shieldGraphic.MatEast, rootLoc + drawDraftedLocEast, 0);
                        break;
                    case 2:
                        DrawShield(shieldGraphic.MatSouth, rootLoc +drawDraftedLocSouth, 0);
                        break;
                    case 3:
                        DrawShield(shieldGraphic.MatWest, rootLoc + drawDraftedLocWest, 0);
                        break;
                    default:
                        break;
                }                
            }
            else
            {
                if (!pawn.Dead && pawn.GetPosture()==PawnPosture.Standing)
                {
                    switch (pawn.Rotation.AsInt)
                    {
                        case 0:
                            DrawShield(shieldGraphic.MatSouth, rootLoc + drawBackLocNorth, 0);
                            break;
                        case 1:
                            DrawShield(shieldGraphic.MatWest, rootLoc + drawBackLocEast, 15);
                            break;
                        case 2:
                            DrawShield(shieldGraphic.MatNorth, rootLoc + drawBackLocSouth, 0);
                            break;
                        case 3:
                            DrawShield(shieldGraphic.MatEast, rootLoc + draWBackLocWest, -15);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void DrawShield(Material mat, Vector3 drawLoc,float angle)
        {
            Mesh mesh = MeshPool.plane10;
            Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(angle, Vector3.up), mat, 0);
        }

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
                //바라보는 시야 140도 이내만 방어
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
                        if (Prefs.DevMode) { Log.Message(pawn + "ShieldBlockChance".Translate() + totalDeflectChance.ToStringPercent()); }

                        //튕겨냄 TxtMote
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "ShieldBlock".Translate(), 1.9f);
                        //튕겨내는 이펙트
                        EffecterDefOf.Deflect_Metal.Spawn().Trigger(pawn, dinfo.Instigator ?? pawn);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool AllowVerbCast(Verb verb)
        {
            return !(verb is Verb_LaunchProjectile);
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
        /// <param name="armorRate"></param>
        /// <returns></returns>
        private float GetDeflectChanceByArmorRate(float armorRate)
        {
            //괴물 소재로 인해 200% 방어력 넘는거에 대한 제한
            return Mathf.Clamp01(armorRate / 2) / 2;
        }
        /// <summary>
        /// 근접 전투 스킬 레벨 기반 공격을 튕겨낼 확률
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private float GetDeflectChanceByMeleeSkillLevel(int level)
        {
            return level * BLOCK_RATE_FACTOR_BY_SKILL;
        }
    }

}