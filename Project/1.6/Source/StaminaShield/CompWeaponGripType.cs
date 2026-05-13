using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace NewRatkin
{
    /// <summary>
    /// 무기 ThingDef에 파지 유형(한손/양손/특수)을 선언하고 정보 카드에 표시한다.
    /// </summary>
    public class CompProperties_WeaponGripType : CompProperties
    {
        public CompProperties_WeaponGripType()
        {
            compClass = typeof(CompWeaponGripType);
        }

        public List<RK_WeaponGripType> gripTypes;
    }

    public class CompWeaponGripType : ThingComp
    {
        public CompProperties_WeaponGripType Props => (CompProperties_WeaponGripType)props;

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            string value = FormatGripTypesForDisplay(Props.gripTypes);
            if (value.NullOrEmpty())
            {
                yield break;
            }

            ThingDef def = parent.def;
            StatCategoryDef category = def.IsRangedWeapon
                ? StatCategoryDefOf.Weapon_Ranged
                : StatCategoryDefOf.Weapon_Melee;
            int priority = def.IsRangedWeapon ? 3000 : 4500;

            StringBuilder report = new StringBuilder();
            report.AppendLine("RK_WeaponGripType_Report".Translate());

            yield return new StatDrawEntry(
                category,
                "RK_WeaponGripType_Label".Translate(),
                value,
                report.ToString().TrimEnd(),
                priority,
                null,
                null,
                false,
                false);
        }

        /// <summary>ThingDef에 선언된 파지 유형(없으면 null).</summary>
        public static List<RK_WeaponGripType> GripTypesFor(ThingDef weaponDef)
        {
            if (weaponDef == null)
            {
                return null;
            }

            CompProperties_WeaponGripType props = weaponDef.GetCompProperties<CompProperties_WeaponGripType>();
            return props?.gripTypes;
        }

        public static string FormatGripTypesForDisplay(IList<RK_WeaponGripType> types)
        {
            if (types == null || types.Count == 0)
            {
                return null;
            }

            IEnumerable<RK_WeaponGripType> ordered = types.Distinct().OrderBy(t => (int)t);
            return ordered.Select(TranslateOneGripType).ToCommaList(false);
        }

        private static string TranslateOneGripType(RK_WeaponGripType t)
        {
            switch (t)
            {
                case RK_WeaponGripType.OneHand:
                    return "RK_GripType_OneHand".Translate();
                case RK_WeaponGripType.TwoHand:
                    return "RK_GripType_TwoHand".Translate();
                default:
                    return "RK_GripType_Special".Translate();
            }
        }

        /// <summary>교집합이 있으면 호환.</summary>
        public static bool IsGripCompatible(IList<RK_WeaponGripType> weaponGrips, IList<RK_WeaponGripType> allowedGrips)
        {
            if (weaponGrips == null || allowedGrips == null)
            {
                return false;
            }

            for (int i = 0; i < weaponGrips.Count; i++)
            {
                RK_WeaponGripType g = weaponGrips[i];
                for (int j = 0; j < allowedGrips.Count; j++)
                {
                    if (g == allowedGrips[j])
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
