using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
    /// <summary>
    /// 햄스터 휠 발전기의 드로잉 설정을 담는 Properties 클래스
    /// </summary>
    public class CompProperties_PowerHamsterWheel : CompProperties_Power
    {
        public CompProperties_PowerHamsterWheel()
        {
            this.compClass = typeof(CompPowerPlantHamsterWheel);
        }

        // 회전 중심축 오프셋
        public Vector3 centerOffset = new Vector3(-0.65f, 0f, 0f);
        public float centerFacingOffset = -0.1f;

        // Front/Back 테두리 설정
        public Vector3 frontOffset = new Vector3(0f, 1f, -0.2f);
        public Vector3 backOffset = new Vector3(0f, -1f, 0.3f);
        public Vector3 rimScale = new Vector3(2f, 1f, 2f);

        // Blades(살) 설정
        public Vector3 bladeScale = new Vector3(2f, 1f, 3.2f);
        public float bladeRadius = 0.95f;
        public float bladeYOffset = 0.086875f;
        public float bladeZOffset = 0.9f;
        public int bladeCount = 16;

        /// <summary>최대 전력(W). <c>basePowerConsumption</c>의 절댓값(기준 W/스핀 단위)과 곱해져 상한이 된다.</summary>
        public float maxPowerOutputWatts = 1000f;

        /// <summary>이 <see cref="StatDefOf.MoveSpeed"/> 값일 때 <see cref="maxPowerOutputWatts"/>까지 스핀 상한에 도달한다.</summary>
        public float referenceMoveSpeedForFullOutput = 4f;
    }

    [StaticConstructorOnStartup]
    public class CompPowerPlantHamsterWheel : CompPowerPlant
    {
        private float spinPosition;
        public Pawn user;

        private const float spinRate = 2f;

        private const float PowerFactorIfWaterDoubleUsed = 0.3f;

        private const float spinFactor = 6.28318548f;

        private const float SpinRateFactor = 0.013333334f*0.01f;

        private const float BladeOffset = 2.36f;

        private const int BladeCount = 9;

        public bool isUsingNow=false;
        public int currentSpinPower = 0;
        
        public float maxSpinPower=0;

        public static readonly Material BladesMat = MaterialPool.MatFrom("Things/Building/RK_HamsterWheelGeneratorBlades");
        public static readonly Material Back = MaterialPool.MatFrom("Things/Building/Back");
        public static readonly Material Front = MaterialPool.MatFrom("Things/Building/Front");

        public new CompProperties_PowerHamsterWheel Props => (CompProperties_PowerHamsterWheel)this.props;

        private float EffectiveMaxPowerOutputWatts => Mathf.Max(0f, Props.maxPowerOutputWatts);

        private float EffectiveReferenceMoveSpeed =>
            Mathf.Max(0.01f, Props.referenceMoveSpeedForFullOutput);

        
        protected override float DesiredPowerOutput
        {
            get
            {
                if(currentSpinPower>0)
                {
                    float powerOutput = base.DesiredPowerOutput * currentSpinPower;
                    // 최대 전력 생산량 제한 적용
                    return Mathf.Min(powerOutput, EffectiveMaxPowerOutputWatts);
                }
                else
                {
                    return 0;
                }
            }
        }
        public bool CanUseNow
        {
            get
            {
                return parent.Spawned &&parent.Faction == Faction.OfPlayer;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            spinPosition = Rand.Range(0f, 15f);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isUsingNow, "isUsingNow");
            Scribe_Values.Look(ref maxSpinPower, "maxSpinPower");
            Scribe_Values.Look(ref currentSpinPower, "currentSpinPower");
            Scribe_References.Look(ref user, "user");
        }

        public override void CompTick()
        {
            base.CompTick();
            if(isUsingNow &&currentSpinPower< maxSpinPower)
            {
                currentSpinPower++;
            }
            if(currentSpinPower>0)
            {
                spinPosition = (spinPosition + SpinRateFactor * currentSpinPower + spinFactor) % spinFactor;
                if (!isUsingNow)
                {
                    currentSpinPower--;
                }                
            }

        }

        public override void PostDraw()
        {
            base.PostDraw();
            //현재 회전 정보
            float spinAngle = spinPosition;
            Quaternion rotation = (spinAngle / spinFactor * 360).ToQuat();

            //회전 중심축 (XML에서 설정 가능)
            Vector3 centerPos = parent.TrueCenter();
            centerPos += parent.Rotation.FacingCell.ToVector3() * Props.centerFacingOffset + Props.centerOffset;
            centerPos.y = AltitudeLayer.Pawn.AltitudeFor();

            //쳇바퀴 테두리 (XML에서 설정 가능)
            Matrix4x4 backMatrix = default;
            Matrix4x4 frontMatrix = default;
            Vector3 frontPosition = centerPos + Props.frontOffset;
            Vector3 backPosition = centerPos + Props.backOffset;

            frontMatrix.SetTRS(frontPosition, rotation, Props.rimScale);
            backMatrix.SetTRS(backPosition, rotation, Props.rimScale);
            Graphics.DrawMesh(MeshPool.plane10, frontMatrix, Front, 0);
            Graphics.DrawMesh(MeshPool.plane10, backMatrix, Back, 0);
            
            Matrix4x4 matrix = default;
            Vector3 position = default;
            float cosin = 0;

            //쳇바퀴 살 회전 (XML에서 설정 가능)
            for (int i = 0; i < Props.bladeCount; i++)
            {
                spinAngle += spinFactor * i / (float)Props.bladeCount;
                cosin = Mathf.Cos(spinAngle);
                position = centerPos + new Vector3(Mathf.Sin(spinAngle) * Props.bladeRadius, Props.bladeYOffset * cosin, Props.bladeZOffset * cosin);
                matrix.SetTRS(position, parent.Rotation.AsQuat, Props.bladeScale);
                Graphics.DrawMesh(MeshPool.plane10, matrix, BladesMat, 0);
            }
        }

        public void StartTurnning(float Speed,Pawn user)
        {
            // 최대 전력 생산량 제한을 고려하여 maxSpinPower 계산
            // maxPowerOutputWatts = base.DesiredPowerOutput * maxSpinPower
            // 따라서 maxSpinPower = maxPowerOutputWatts / base.DesiredPowerOutput
            float capWatts = EffectiveMaxPowerOutputWatts;
            float spinPowerAtFullOutput = capWatts / base.DesiredPowerOutput;
            float calculatedMaxSpinPower = Speed / EffectiveReferenceMoveSpeed * spinPowerAtFullOutput;
            maxSpinPower = Mathf.Min(calculatedMaxSpinPower, spinPowerAtFullOutput);
            isUsingNow = true;
            this.user = user;
            //spinPower = 100;

        }
        public void UsingDone()
        {
            isUsingNow = false;
            user = null;
        }

        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            if (currentSpinPower>0)
            {
                text = text + "\n" + "RK_MakeGeneratePower".Translate();
            }
            return text;
        }
    }
}
