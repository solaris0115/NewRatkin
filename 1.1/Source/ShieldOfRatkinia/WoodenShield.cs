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

        static readonly Vector3 drawDraftedLocNorth = new Vector3(-0.2f, 0f, -0.09f);
        static readonly Vector3 drawDraftedLocSouth = new Vector3(0.2f, 0.03905f, -0.15f);
        static readonly Vector3 drawDraftedLocEast = new Vector3(0.2f, 0f, -0.2f);
        static readonly Vector3 drawDraftedLocWest = new Vector3(-0.2f, 0.05f, -0.15f);

        static readonly Vector3 drawBackLocNorth = new Vector3(0f, 0.5f, -0.2f);
        static readonly Vector3 drawBackLocSouth = new Vector3(0f, 0, -0.09f);
        static readonly Vector3 drawBackLocEast = new Vector3(-0.15f, 0.05f, -0.07f);
        static readonly Vector3 draWBackLocWest = new Vector3(0.15f, 0f, -0.07f);

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
            if (shieldGraphic==null)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    //Log.Message("2");
                    if (def.defName == "RK_WoodenShield")
                    {
                        shieldGraphic = GraphicDatabase.Get<Graphic_Multi>(path + def.defName, ShaderDatabase.Cutout, def.graphicData.drawSize, Color.white);
                    }
                    else
                    {
                        shieldGraphic = GraphicDatabase.Get<Graphic_Multi>(path + def.defName, ShaderDatabase.Cutout, def.graphicData.drawSize, Stuff.stuffProps.color);
                    }
                });
            }
            
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
                if (defenderAngle- attackerAngle >=-70 && defenderAngle - attackerAngle<=70)
                {
                    float blockingRate = pawn.skills.GetSkill(SkillDefOf.Melee).levelInt * 0.0375f;
                    switch (dinfo.Def.armorCategory)
                    {
                        case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Sharp:
                            blockingRate *= this.GetStatValue(StatDefOf.ArmorRating_Sharp);
                            break;
                        case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Blunt:
                            blockingRate *= this.GetStatValue(StatDefOf.ArmorRating_Blunt);
                            break;
                        case DamageArmorCategoryDef d when d == DamageArmorCategoryDefOf.Heat:
                            blockingRate *= this.GetStatValue(StatDefOf.ArmorRating_Heat);
                            break;
                        default:
                            break;
                    }
                    if (Rand.Value <= blockingRate)
                    {
                        if (Prefs.DevMode)
                        {
                            Log.Message(pawn + "ShieldBlockChance".Translate() + blockingRate.ToStringPercent());
                        }
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "ShieldBlock".Translate(), 1.9f);
                        EffecterDefOf.Deflect_Metal.Spawn().Trigger(pawn, dinfo.Instigator ?? pawn);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool AllowVerbCast(IntVec3 root, Map map, LocalTargetInfo targ, Verb verb)
        {
            return !(verb is Verb_LaunchProjectile) || ReachabilityImmediate.CanReachImmediate(root, targ, map, PathEndMode.Touch, null);
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            StringBuilder sharp = new StringBuilder();
            StringBuilder blunt = new StringBuilder();
            StringBuilder heat = new StringBuilder();
            float valueSharp= this.GetStatValue(StatDefOf.ArmorRating_Sharp);
            float valueBlunt= this.GetStatValue(StatDefOf.ArmorRating_Blunt);
            float valueHeat= this.GetStatValue(StatDefOf.ArmorRating_Heat);
            sharp.AppendLine("BlockChanceDefualtDesc".Translate());
            blunt.AppendLine("BlockChanceDefualtDesc".Translate());
            heat.AppendLine("BlockChanceDefualtDesc".Translate());
            if (Prefs.DevMode)
            {
                if (Wearer != null)
                {
                    float wearSkillLevel = Wearer.skills.GetSkill(SkillDefOf.Melee).levelInt;
                    float wearerBlockRate = wearSkillLevel * 0.0375f;
                    valueSharp *= wearerBlockRate;
                    valueBlunt *= wearerBlockRate;
                    valueHeat *= wearerBlockRate;

                    sharp.AppendLine(string.Format("\n{0}({1}): {2}\n{3}: {4}\n{5}: {6}", SkillDefOf.Melee.LabelCap, wearSkillLevel, wearerBlockRate.ToStringPercent(), StatDefOf.ArmorRating_Sharp.LabelCap, this.GetStatValue(StatDefOf.ArmorRating_Sharp).ToStringPercent(), "StatsReport_FinalValue".Translate(), valueSharp.ToStringPercent()));
                    blunt.AppendLine(string.Format("\n{0}({1}): {2}\n{3}: {4}\n{5}: {6}", SkillDefOf.Melee.LabelCap, wearSkillLevel, wearerBlockRate.ToStringPercent(), StatDefOf.ArmorRating_Blunt.LabelCap, this.GetStatValue(StatDefOf.ArmorRating_Blunt).ToStringPercent(), "StatsReport_FinalValue".Translate(), valueBlunt.ToStringPercent()));
                    heat.AppendLine(string.Format("\n{0}({1}): {2}\n{3}: {4}\n{5}: {6}", SkillDefOf.Melee.LabelCap, wearSkillLevel, wearerBlockRate.ToStringPercent(), StatDefOf.ArmorRating_Heat.LabelCap, this.GetStatValue(StatDefOf.ArmorRating_Heat).ToStringPercent(), "StatsReport_FinalValue".Translate(), valueHeat.ToStringPercent()));
                }
                else
                {
                    sharp.AppendLine(string.Format("\n{0}: {1}\n{2}: {3}({4})", StatDefOf.ArmorRating_Sharp.LabelCap, this.GetStatValue(StatDefOf.ArmorRating_Sharp).ToStringPercent(), "StatsReport_FinalValue".Translate(), valueSharp.ToStringPercent(), "CanLow".Translate()));
                    blunt.AppendLine(string.Format("\n{0}: {1}\n{2}: {3}({4})", StatDefOf.ArmorRating_Blunt.LabelCap, this.GetStatValue(StatDefOf.ArmorRating_Blunt).ToStringPercent(), "StatsReport_FinalValue".Translate(), valueBlunt.ToStringPercent(), "CanLow".Translate()));
                    heat.AppendLine(string.Format("\n{0}: {1}\n{2}: {3}({4})", StatDefOf.ArmorRating_Heat.LabelCap, this.GetStatValue(StatDefOf.ArmorRating_Heat).ToStringPercent(), "StatsReport_FinalValue".Translate(), valueHeat.ToStringPercent(), "CanLow".Translate()));
                }
            }
            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "BlockChance_Heat".Translate(), valueHeat.ToStringPercent(), heat.ToString(),20);
            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "BlockChance_Blunt".Translate(), valueBlunt.ToStringPercent(), blunt.ToString(), 20);
            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "BlockChance_Sharp".Translate(), valueSharp.ToStringPercent(), sharp.ToString(), 20);
        }

    }

}