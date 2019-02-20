using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;
using Verse.Sound;
using System.Text;
using System.Reflection;
using RimWorld;
using Harmony;
using UnityEngine;
using Verse.AI;

namespace NewRatkin
{
    [DefOf]
    public static class RatkinPawnKindDefOf
    {
        public static PawnKindDef RatkinNoble;

        public static PawnKindDef RatkinColonist;

        public static PawnKindDef RatkinServant;

        public static PawnKindDef RatkinCombatant;

        public static PawnKindDef RatkinSoldier;

        public static PawnKindDef RatkinSubject;

        public static PawnKindDef RatkinMercenary;

        public static PawnKindDef RatkinEliteGuardener;

        public static PawnKindDef RatkinPriest;

        public static PawnKindDef RatkinMerchant;

        public static PawnKindDef RatkinMurderer;

        //괴도 찍찍이 확장팩
        public static PawnKindDef RatkinDemonMan;
        public static PawnKindDef RatkinEliteSoldier;
    }
    [DefOf]
    public static class RatkinBuildingDefOf
    {
        public static ThingDef RK_GuerrillaTunnel;
    }

    [DefOf]
    public static class RatkinWeaponDefOf
    {
        public static ThingDef RK_FaceCleaner;
    }



}
