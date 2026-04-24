using Verse;
using UnityEngine;


namespace NewRatkin
{
    public class Graphic_CountDown : Graphic_Collection
    {
        public override Material MatSingle
        {
            get
            {
                return subGraphics[subGraphics.Length - 1].MatSingle;
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_CountDown>(path, newShader, drawSize, newColor, newColorTwo, data);
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            if (thing == null)
            {
                return MatSingle;
            }
            return MatSingleFor(thing);
        }

        public override Material MatSingleFor(Thing thing)
        {
            if (thing == null)
            {
                return MatSingle;
            }
            return SubGraphicFor(0).MatSingle;
        }

        public Graphic SubGraphicFor(int number)
        {
            return SubGraphicForCountDown(number);
        }
        public void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation,int number)
        {
            Graphic graphic;
            if (thing != null)
            {
                graphic = SubGraphicFor(number);
            }
            else
            {
                graphic = subGraphics[0];
            }
            graphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            Graphic graphic;
            if (thing != null)
            {
                graphic = SubGraphicFor(0);
            }
            else
            {
                graphic = subGraphics[0];
            }
            graphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }

        public Graphic SubGraphicForCountDown(int number)
        {
            if(number<subGraphics.Length)
            {
                return subGraphics[number];
            }
            return subGraphics[0];
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "CountDown(path=",
                path,
                ", count=",
                subGraphics.Length,
                ")"
            });
        }
    }
}
