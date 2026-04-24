using Verse;

namespace NewRatkin
{
    /// <summary>
    /// statBases 풀각(도)에 곱하는 근접 스킬 배율. 0레벨 0.5× ~ 20레벨 1.2× (선형), 그 밖은 SimpleCurve 클램프.
    /// </summary>
    public static class ShieldDeflectAngleMeleeCurve
    {
        private static readonly SimpleCurve Curve = new SimpleCurve
        {
            { 0f, 0.5f },
            { 20f, 1.2f },
        };

        public static float Evaluate(float meleeSkillLevel)
        {
            return Curve.Evaluate(meleeSkillLevel);
        }
    }
}
