using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace NewRatkin
{
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

        
        protected override float DesiredPowerOutput
        {
            get
            {
                if(currentSpinPower>0)
                {
                    return base.DesiredPowerOutput * currentSpinPower;
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

        public override void PostDeSpawn(Map map)
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
            Vector3 turningPosition = parent.TrueCenter();
            ////Log.Message(parent.Rotation.FacingCell.ToVector3().ToString());
            turningPosition += parent.Rotation.FacingCell.ToVector3() * -0.1f + new Vector3(-0.65f,0,0);//양수면 위로감
            turningPosition.y = AltitudeLayer.Pawn.AltitudeFor();
            ////Log.Message((spinPosition).ToString());
            for (int i = 0; i < 16; i++)
            {
                float drawPosition = spinPosition + spinFactor * i / 16f;
                float cosin = Mathf.Cos(drawPosition);

                Vector3 scale = new Vector3(2, 1, 3.2f);
                Matrix4x4 matrix = default(Matrix4x4);
                Vector3 position = turningPosition + new Vector3(Mathf.Sin(drawPosition) * 0.95f, 0.086875f * cosin, 0.9f * cosin);// + Vector3.right * Mathf.Sin(drawPosition) + Vector3.up * 0.046875f * Mathf.Cos(drawPosition) + Vector3.forward * 0.5f * Mathf.Cos(drawPosition);
                matrix.SetTRS(position, parent.Rotation.AsQuat, scale);
                Graphics.DrawMesh(MeshPool.plane10, matrix, BladesMat, 0);
            }
            float drawPosition2 = spinPosition;

            Vector3 scale2 = new Vector3(2, 1, 2);

            Matrix4x4 backMatrix = default(Matrix4x4);
            Matrix4x4 frontMatrix = default(Matrix4x4);

            Vector3 frontPosition = turningPosition + Vector3.up * 1f + Vector3.back*0.2f;
            Vector3 backPosition = turningPosition + Vector3.down + Vector3.forward*0.3f;

            Quaternion rotation = (drawPosition2 / spinFactor * 360).ToQuat();

            frontMatrix.SetTRS(frontPosition, rotation, scale2);
            backMatrix.SetTRS(backPosition, rotation, scale2);

            Graphics.DrawMesh(MeshPool.plane10, frontMatrix, Front, 0);
            Graphics.DrawMesh(MeshPool.plane10, backMatrix, Back, 0);
        }

        public void StartTurnning(float Speed,Pawn user)
        {
            maxSpinPower = Speed * 100;
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
