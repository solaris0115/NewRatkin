using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace NewRatkin
{
    [StaticConstructorOnStartup]
    public class Shield : Apparel
    {
        private readonly string path = "Apparel/";
        private Graphic shieldGraphic;
        private static Graphic heavyShieldGraphic = GraphicDatabase.Get<Graphic_Multi>("Apparel/RK_HeavyShield", ShaderDatabase.Cutout, new Vector2(1, 1), new Color(1, 1, 1, 1));
        private static Graphic woodenShieldGraphic = GraphicDatabase.Get<Graphic_Multi>("Apparel/RK_WoodenShield", ShaderDatabase.Cutout, new Vector2(1, 1), new Color(1,1,1,1));
        /*
        private readonly Material materialSouth;//MaterialPool.MatFrom("Apparel/RK_HeavyShield_south", ShaderDatabase.Cutout);
        private readonly Material materialEast;
        private readonly Material materialNorth;
        private readonly Material materialWes;*/
        static readonly Vector3 drawDraftedLocNorth = new Vector3(-0.15f, 0f, -0.09f);
        static readonly Vector3 drawDraftedLocSouth = new Vector3(0.15f, 0.0390725f, -0.2f);
        static readonly Vector3 drawDraftedLocEast = new Vector3(0.2f, 0f, -0.2f);
        static readonly Vector3 drawDraftedLocWest = new Vector3(-0.2f, 0.05f, -0.2f);

        static readonly Vector3 drawBackLocNorth = new Vector3(0f, 0.5f, -0.2f);
        static readonly Vector3 drawBackLocSouth = new Vector3(0f, 0, -0.09f);
        static readonly Vector3 drawBackLocEast = new Vector3(-0.2f, 0.0390625f, -0.15f);
        static readonly Vector3 drawBackLocWest = new Vector3(0.2f, 0.0390625f, -0.15f);

        public Shield()
        {
            Log.Message("Creator");
            //graphicShieldBack=GraphicDatabase.Get<Graphic_Multi>("", ShaderDatabase.Cutout, def.graphicData.drawSize, DrawColor);
        }

        private bool ShouldShieldUp
        {
            get
            {
                Pawn wearer = Wearer;
                return wearer.Spawned &&
                    (wearer.InAggroMentalState || wearer.Drafted || (wearer.Faction.HostileTo(Faction.OfPlayer) &&
                    !wearer.IsPrisoner));
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Log.Message("SpawnSetup");
        }
        public override void PostMake()
        {
            base.PostMake();
            if (shieldGraphic==null)
            {
                shieldGraphic = GraphicDatabase.Get<Graphic_Multi>(path + def.defName, ShaderDatabase.Cutout, def.graphicData.drawSize, DrawColor);
            }
            Log.Message("PostMake");
        }
        public override void ExposeData()
        {
            base.ExposeData();

            Log.Message("ExposeData");
            if(Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (shieldGraphic == null)
                {
                    switch(def.defName)
                    {
                        case "RK_HeavyShield":
                            shieldGraphic = heavyShieldGraphic;
                            break;
                        case "RK_WoodenShield":
                            shieldGraphic = woodenShieldGraphic;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            Log.Message("Destroy");
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
                        DrawShield(shieldGraphic.MatNorth, rootLoc +drawDraftedLocNorth);
                        break;
                    case 1:
                        DrawShield(shieldGraphic.MatEast, rootLoc + drawDraftedLocEast);
                        break;
                    case 2:
                        DrawShield(shieldGraphic.MatSouth, rootLoc +drawDraftedLocSouth);
                        break;
                    case 3:
                        DrawShield(shieldGraphic.MatWest, rootLoc + drawDraftedLocWest);
                        break;
                    default:
                        break;
                }                
            }
            //등에 맴
            else
            {
                switch (pawn.Rotation.AsInt)
                {
                    case 0:
                        DrawShield(shieldGraphic.MatSouth, rootLoc + drawBackLocNorth);
                        break;
                    case 1:
                        DrawShield(shieldGraphic.MatWest, rootLoc + drawBackLocEast);
                        break;
                    case 2:
                        DrawShield(shieldGraphic.MatNorth, rootLoc + drawBackLocSouth);
                        break;
                    case 3:
                        DrawShield(shieldGraphic.MatEast, rootLoc + drawBackLocWest);
                        break;
                    default:
                        break;
                }
            }
        }

        public void DrawShield(Material mat, Vector3 drawLoc)
        {
            Mesh mesh = MeshPool.plane10;
            Graphics.DrawMesh(mesh, drawLoc, Quaternion.AngleAxis(0, Vector3.up), mat, 0);
            //Material material2 = apparelGraphics[j].graphic.MatAt(bodyFacing, null);
            //material2 = this.graphics.flasher.GetDamagedMat(material2);
            //GenDraw.DrawMeshNowOrLater(mesh3, loc2, quaternion, material2, portrait);
        }
        /*

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            Log.Message("Damage:"+ dinfo.Amount+"  Piercing: "+ dinfo.ArmorPenetrationInt+"  Type: " + dinfo.Def);
            base.PreApplyDamage(ref dinfo, out absorbed);
        }

        

        private void DrawEquipment(Vector3 rootLoc)
        {

        }*/
    }
}