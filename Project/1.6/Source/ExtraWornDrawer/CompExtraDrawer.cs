using System;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
    /// <summary>
    /// 방향별 드로잉 데이터 (그래픽 경로 + 오프셋 + 각도)
    /// </summary>
    public class GraphicDrawData
    {
        public string graphicPath;
        
        public Vector3 offsetNorth = Vector3.zero;
        public Vector3 offsetEast = Vector3.zero;
        public Vector3 offsetSouth = Vector3.zero;
        public Vector3 offsetWest = Vector3.zero;

        public float angleNorth = 0f;
        public float angleEast = 0f;
        public float angleSouth = 0f;
        public float angleWest = 0f;
    }

    /// <summary>
    /// 이념 아이콘 드로잉 데이터 (위치 + 각도, 경로는 Pawn.Ideo에서 자동 참조)
    /// </summary>
    public class IdeoIconDrawData
    {
        public Vector3 offsetNorth = Vector3.zero;
        public Vector3 offsetEast = Vector3.zero;
        public Vector3 offsetSouth = Vector3.zero;
        public Vector3 offsetWest = Vector3.zero;

        public float angleNorth = 0f;
        public float angleEast = 0f;
        public float angleSouth = 0f;
        public float angleWest = 0f;

        /// <summary>
        /// 이념 아이콘 크기 (기본값: 0.5)
        /// </summary>
        public float iconSize = 0.5f;
    }

    /// <summary>
    /// Apparel의 조건부 추가 드로잉을 담당하는 Component
    /// RimWorld CompShield 패턴을 따름
    /// </summary>
    public class CompProperties_ExtraDrawer : CompProperties
    {
        /// <summary>
        /// 드로잉 크기 (기본값: 1.0)
        /// </summary>
        public Vector2 drawSize = new Vector2(1f, 1f);

        /// <summary>
        /// 소집 시 드로잉 데이터 (그래픽 경로 + 팔/어깨 위치)
        /// </summary>
        public GraphicDrawData draftedDrawData;

        /// <summary>
        /// 비소집 시 드로잉 데이터 (그래픽 경로 + 등 위치)
        /// </summary>
        public GraphicDrawData backDrawData;

        /// <summary>
        /// 소집 시 이념 아이콘 드로잉 데이터 (위치 + 각도)
        /// </summary>
        public IdeoIconDrawData draftedIdeoIconData;

        /// <summary>
        /// 비소집 시 이념 아이콘 드로잉 데이터 (위치 + 각도)
        /// </summary>
        public IdeoIconDrawData backIdeoIconData;

        public CompProperties_ExtraDrawer()
        {
            this.compClass = typeof(CompExtraDrawer);

            // 기본값 설정 (이전 하드코딩 값)
            draftedDrawData = new GraphicDrawData
            {
                graphicPath = "Apparel/Util/RK_TextureApparel_BannerArm",
                offsetNorth = new Vector3(-0.25f, 0.15f, -0.08f),
                offsetSouth = new Vector3(0.25f, 0.15f, -0.08f),
                offsetEast = new Vector3(0.22f, 0.12f, -0.12f),
                offsetWest = new Vector3(-0.22f, 0.12f, -0.12f),
                angleNorth = 0f,
                angleEast = 0f,
                angleSouth = 0f,
                angleWest = 0f
            };

            backDrawData = new GraphicDrawData
            {
                graphicPath = "Apparel/Util/RK_TextureApparel_BannerUnarm",
                offsetNorth = new Vector3(0f, -0.18f, -0.08f),
                offsetSouth = new Vector3(0f, -0.12f, -0.12f),
                offsetEast = new Vector3(-0.12f, -0.15f, -0.08f),
                offsetWest = new Vector3(0.12f, -0.15f, -0.08f),
                angleNorth = 0f,
                angleEast = 0f,
                angleSouth = 0f,
                angleWest = 0f
            };
        }
    }

    [StaticConstructorOnStartup]
    public class CompExtraDrawer : ThingComp
    {
        private Graphic extraGraphicDrafted;  // 소집 시 그래픽
        private Graphic extraGraphicBack;     // 비소집 시 그래픽

        /// <summary>
        /// CompProperties 캐스팅
        /// </summary>
        public CompProperties_ExtraDrawer Props => (CompProperties_ExtraDrawer)this.props;

        /// <summary>
        /// 부모 Thing을 Apparel로 캐스팅
        /// </summary>
        private Apparel Apparel => this.parent as Apparel;

        /// <summary>
        /// Apparel을 착용한 Pawn
        /// </summary>
        private Pawn Wearer => Apparel?.Wearer;

        /// <summary>
        /// 팔/어깨에 추가 그래픽을 표시해야 하는지 여부 (소집 상태)
        /// CompShield의 ShouldDisplay 패턴을 따름
        /// </summary>
        private bool ShouldShowOnArm
        {
            get
            {
                Pawn wearer = Wearer;
                if (wearer == null || !wearer.Spawned || wearer.Dead || wearer.Downed)
                {
                    return false;
                }

                // CompShield와 동일한 조건
                return wearer.Drafted ||
                       wearer.InAggroMentalState ||
                       (wearer.CurJob != null && wearer.CurJob.def.alwaysShowWeapon) ||
                       (wearer.mindState.duty != null && wearer.mindState.duty.def.alwaysShowWeapon);
            }
        }

        /// <summary>
        /// Comp가 생성될 때 Graphic 로딩
        /// </summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            LoadGraphic();
        }

        /// <summary>
        /// Apparel 착용 시 호출됨 - Graphic 로딩 보장 (Dev Tool 소환 등 타이밍 문제 대응)
        /// </summary>
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            LoadGraphic();
        }

        /// <summary>
        /// Graphic 로딩 (비동기 처리)
        /// </summary>
        private void LoadGraphic()
        {
            if (extraGraphicDrafted != null && extraGraphicBack != null) return;

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (parent == null) return;

                // drawSize 가져오기 (Props 또는 parent.def.graphicData에서)
                Vector2 drawSize = Props.drawSize;
                if (drawSize == Vector2.zero && parent.def.graphicData != null)
                {
                    drawSize = parent.def.graphicData.drawSize;
                }
                if (drawSize == Vector2.zero)
                {
                    drawSize = new Vector2(1f, 1f);
                }

                // Shader 선택: 부모 Apparel의 useWornGraphicMask 설정 확인
                // RimWorld의 ApparelGraphicRecordGetter 로직과 동일
                Shader shader = ShaderDatabase.Cutout;
                Apparel apparel = Apparel;
                if (apparel != null)
                {
                    ThingStyleDef styleDef = apparel.StyleDef;
                    if (styleDef != null && styleDef.graphicData != null && styleDef.graphicData.shaderType != null)
                    {
                        shader = styleDef.graphicData.shaderType.Shader;
                    }
                    else if ((styleDef == null && apparel.def.apparel.useWornGraphicMask) || 
                             (styleDef != null && styleDef.UseWornGraphicMask))
                    {
                        shader = ShaderDatabase.CutoutComplex;
                    }
                }

                // ThingWithComps.DrawColor가 stuff 색상 / CompColorable(Active) / graphicData.color 순서로 처리
                Color drawColor = parent.DrawColor;

                // 소집 시 그래픽 로딩
                string graphicPathDrafted = Props.draftedDrawData?.graphicPath;
                if (graphicPathDrafted.NullOrEmpty())
                {
                    graphicPathDrafted = "Apparel/Util/RK_TextureApparel_BannerArm";
                }
                extraGraphicDrafted = GraphicDatabase.Get<Graphic_Multi>(
                    graphicPathDrafted,
                    shader,
                    drawSize,
                    drawColor);

                // 비소집 시 그래픽 로딩
                string graphicPathBack = Props.backDrawData?.graphicPath;
                if (graphicPathBack.NullOrEmpty())
                {
                    graphicPathBack = "Apparel/Util/RK_TextureApparel_BannerUnarm";
                }
                extraGraphicBack = GraphicDatabase.Get<Graphic_Multi>(
                    graphicPathBack,
                    shader,
                    drawSize,
                    drawColor);
            });
        }

        /// <summary>
        /// Apparel 착용 시 추가 드로잉 처리
        /// RimWorld의 표준 패턴: CompDrawWornExtras 오버라이드
        /// </summary>
        public override void CompDrawWornExtras()
        {
            base.CompDrawWornExtras();

            // 유효성 검증
            if (Wearer == null || !Wearer.Spawned)
            {
                return;
            }

            // Graphic이 아직 로딩되지 않은 경우 로딩 시도 (Dev Tool 소환 등 타이밍 문제 대응)
            if (extraGraphicDrafted == null || extraGraphicBack == null)
            {
                LoadGraphic();
                // 비동기 로딩이므로 이번 프레임에서는 그리지 않음
                return;
            }

            Pawn pawn = Wearer;
            Vector3 rootLoc = pawn.DrawPos;

            // 소집 상태에 따라 다른 위치와 그래픽으로 그리기
            if (ShouldShowOnArm)
            {
                // 소집 시 - 팔/어깨에 표시 (Arm 그래픽)
                GraphicDrawData drawData = Props.draftedDrawData;
                switch (pawn.Rotation.AsInt)
                {
                    case 0: // North
                        DrawExtra(extraGraphicDrafted.MatNorth, rootLoc + drawData.offsetNorth, drawData.angleNorth);
                        break;
                    case 1: // East
                        DrawExtra(extraGraphicDrafted.MatEast, rootLoc + drawData.offsetEast, drawData.angleEast);
                        break;
                    case 2: // South
                        DrawExtra(extraGraphicDrafted.MatSouth, rootLoc + drawData.offsetSouth, drawData.angleSouth);
                        break;
                    case 3: // West
                        DrawExtra(extraGraphicDrafted.MatWest, rootLoc + drawData.offsetWest, drawData.angleWest);
                        break;
                    default:
                        break;
                }

                // 소집 시 이념 아이콘 그리기
                if (Props.draftedIdeoIconData != null)
                {
                    DrawIdeoIcon(pawn, Props.draftedIdeoIconData, rootLoc);
                }
            }
            else
            {
                // 평상시 - 등에 표시 (Unarm 그래픽)
                if (!pawn.Dead && pawn.GetPosture() == PawnPosture.Standing)
                {
                    GraphicDrawData drawData = Props.backDrawData;
                    switch (pawn.Rotation.AsInt)
                    {
                        case 0: // North
                            DrawExtra(extraGraphicBack.MatNorth, rootLoc + drawData.offsetNorth, drawData.angleNorth);
                            break;
                        case 1: // East
                            DrawExtra(extraGraphicBack.MatEast, rootLoc + drawData.offsetEast, drawData.angleEast);
                            break;
                        case 2: // South
                            DrawExtra(extraGraphicBack.MatSouth, rootLoc + drawData.offsetSouth, drawData.angleSouth);
                            break;
                        case 3: // West
                            DrawExtra(extraGraphicBack.MatWest, rootLoc + drawData.offsetWest, drawData.angleWest);
                            break;
                        default:
                            break;
                    }

                    // 비소집 시 이념 아이콘 그리기
                    if (Props.backIdeoIconData != null)
                    {
                        DrawIdeoIcon(pawn, Props.backIdeoIconData, rootLoc);
                    }
                }
            }
        }

        /// <summary>
        /// 추가 그래픽을 실제로 그리는 메서드 (WoodenShield.DrawShield 패턴)
        /// 염색 색상을 실시간으로 반영하기 위해 MaterialPropertyBlock 사용
        /// </summary>
        /// <param name="mat">Material</param>
        /// <param name="drawLoc">그릴 위치</param>
        /// <param name="angle">회전 각도</param>
        private void DrawExtra(Material mat, Vector3 drawLoc, float angle)
        {
            Color drawColor = parent.DrawColor;

            MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();
            matPropertyBlock.SetColor(ShaderPropertyIDs.Color, drawColor);

            Vector2 size = Props.drawSize;
            Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size.x, 1f, size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, matPropertyBlock);
        }

        /// <summary>
        /// 이념 아이콘을 그리는 메서드
        /// 리포트 참고: Ideo.iconDef.iconPath에서 텍스처 경로를 가져옴
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="iconData">이념 아이콘 드로잉 데이터</param>
        /// <param name="rootLoc">기준 위치</param>
        private void DrawIdeoIcon(Pawn pawn, IdeoIconDrawData iconData, Vector3 rootLoc)
        {
            // Ideology DLC 활성화 확인
            if (!ModsConfig.IdeologyActive)
            {
                return;
            }

            // 이념이 없으면 그리지 않음
            if (pawn.Ideo == null)
            {
                return;
            }

            // 매번 pawn.Ideo에서 직접 텍스처와 경로 가져오기 (사상 변경 대응)
            // Ideo.Icon 속성은 iconDef.iconPath에서 텍스처를 로드함
            Texture2D ideoIconTex = pawn.Ideo.Icon;
            if (ideoIconTex == null || ideoIconTex == BaseContent.BadTex)
            {
                return;
            }

            // 텍스처 경로 가져오기 (Material 생성용)
            string iconPath = null;
            if (pawn.Ideo.iconDef != null && !pawn.Ideo.iconDef.iconPath.NullOrEmpty())
            {
                iconPath = pawn.Ideo.iconDef.iconPath;
            }
            else
            {
                // iconDef가 없으면 텍스처의 name을 경로로 사용
                iconPath = ideoIconTex.name;
            }

            if (iconPath.NullOrEmpty())
            {
                return;
            }

            // 방향에 따른 오프셋과 각도 결정
            Vector3 offset;
            float angle;
            int rotation = pawn.Rotation.AsInt;
            switch (rotation)
            {
                case 0: // North
                    offset = iconData.offsetNorth;
                    angle = iconData.angleNorth;
                    break;
                case 1: // East
                    offset = iconData.offsetEast;
                    angle = iconData.angleEast;
                    break;
                case 2: // South
                    offset = iconData.offsetSouth;
                    angle = iconData.angleSouth;
                    break;
                case 3: // West
                    offset = iconData.offsetWest;
                    angle = iconData.angleWest;
                    break;
                default:
                    return;
            }

            // 이념 색상 가져오기 (리포트 참고: Ideo.Color 속성 사용)
            Color ideoColor = pawn.Ideo.Color;

            // Material 생성
            Material iconMaterial = MaterialPool.MatFrom(iconPath, ShaderDatabase.Cutout);
            if (iconMaterial == null)
            {
                return;
            }

            // 아이콘 그리기
            Vector3 iconLoc = rootLoc + offset;
            float iconSize = iconData.iconSize;
            
            MaterialPropertyBlock matPropertyBlock = new MaterialPropertyBlock();
            matPropertyBlock.SetColor(ShaderPropertyIDs.Color, ideoColor);

            // 아이콘 크기 적용을 위한 스케일 매트릭스
            Matrix4x4 matrix = Matrix4x4.TRS(iconLoc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(iconSize, 1f, iconSize));
            
            Mesh mesh = MeshPool.plane10;
            Graphics.DrawMesh(
                mesh,
                matrix,
                iconMaterial,
                0,
                null,
                0,
                matPropertyBlock);
        }

        /// <summary>
        /// Save/Load 처리
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            // 로드 후 Graphic 재로딩
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                LoadGraphic();
            }
        }
    }
}

