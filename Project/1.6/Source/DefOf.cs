using Verse;
using RimWorld;
using Verse.AI;

namespace NewRatkin
{

    [DefOf]
    public static class DamageArmorCategoryDefOf
    {
        public static DamageArmorCategoryDef Sharp;
        public static DamageArmorCategoryDef Blunt;
        public static DamageArmorCategoryDef Heat;
    }

    [DefOf]
    public static class RatkinNeedDefOf
    {
        public static NeedDef Outdoors;
    }
    [DefOf]
    public static class RatkinDamageDefOf
    {
        public static DamageDef RK_EMP;
        public static DamageDef DemoBomb;
    }
    [DefOf]
    public static class RatkinBuildingDefOf
    {
        public static ThingDef RK_GuerrillaTunnel;
        public static ThingDef RK_ThiefTunnel;
        public static ThingDef RK_GuerrillaTunnelSpawner;
        public static ThingDef RK_ThiefTunnelSpawner;

        public static ThingDef RK_EmpBomb;

        public static ThingDef RK_Pulpit;
    }

    [DefOf]
    public static class RatkinWeaponDefOf
    {
        public static ThingDef RK_MagicWand;
    }

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
    public static class RatkinFactionDefOf
    {
        public static FactionDef Rakinia;
    }
    [DefOf]
    public static class RatkinMoteDefOf
    {
        public static ThingDef Mote_CountDown;
    }

    [DefOf]
    public static class RatkinIncidentDefOf
    {
        public static IncidentDef RatkinFollowUpTroops;
    }
    [DefOf]
    public static class RatkinRaceDefOf
    {
        public static ThingDef Ratkin;
    }

    [DefOf]
    public static class RatkinAbilityDefOf
    {
        public static AbilityDef RK_PrayerService;
    }

    [DefOf]
    public static class RatkinDutyDefOf
    {
        public static DutyDef RK_JoinPrayerService;
        public static DutyDef RK_OrganizePrayerService;
        public static DutyDef RK_SpectatePrayerService;
    }

    [DefOf]
    public static class RatkinJobDefOf
    {
        public static JobDef RK_Job_PrayerService;
        public static JobDef RK_Job_SpectatePray;
    }

    [DefOf]
    public static class RatkinTraitDefOf
    {
        public static TraitDef Faith;
    }

    [DefOf]
    public static class RatkinThoughtDefOf
    {
        public static ThoughtDef RK_AttendPrayerMeetingMood;
    }

    [DefOf]
    public static class RatkinBackstoryDefOf
    {
        public static BackstoryDef Ratkin_Sister;
    }
}
