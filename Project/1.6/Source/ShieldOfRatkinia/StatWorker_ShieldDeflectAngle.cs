using RimWorld;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// DeflectAngle — Apparel statBases 고정값 읽기.
    /// statBases에 값이 있는 방패 아이템에서만 표시.
    /// 스킬/재질/품질 보정 없음.
    /// </summary>
    public class StatWorker_ShieldDeflectAngle : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            ThingDef def = (req.Thing?.def ?? req.Def) as ThingDef;
            if (def == null || !def.IsApparel) return false;
            return ReadStatBase(def) > 0f;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            ThingDef def = (req.Thing?.def ?? req.Def) as ThingDef;
            if (def != null)
                return ReadStatBase(def);
            return stat.defaultBaseValue;
        }

        private float ReadStatBase(ThingDef def)
        {
            if (def.statBases == null) return 0f;
            for (int i = 0; i < def.statBases.Count; i++)
                if (def.statBases[i].stat == stat)
                    return def.statBases[i].value;
            return 0f;
        }
    }
}
