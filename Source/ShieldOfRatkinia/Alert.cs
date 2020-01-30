using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace NewRatkin
{
    /*public class Alert_ShieldUserHasRangedWeapon : Alert
    {
        public static int count = 0;
        public Alert_ShieldUserHasRangedWeapon()
        {
            this.defaultLabel = "ShieldUserHasRangedWeapon".Translate();
            this.defaultExplanation = "ShieldUserHasRangedWeaponDesc".Translate();
        }

        private IEnumerable<Pawn> ShieldUsersWithRangedWeapon
        {
            get
            {
                foreach (Pawn p in PawnsFinder.AllMaps_FreeColonistsSpawned)
                {
                    if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
                    {
                        List<Apparel> ap = p.apparel.WornApparel;
                        for (int i = 0; i < ap.Count; i++)
                        {
                            if (ap[i] is Shield)
                            {
                                yield return p;
                                break;
                            }
                        }
                    }
                }
                yield break;
            }
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(ShieldUsersWithRangedWeapon);
        }
    }*/
}
