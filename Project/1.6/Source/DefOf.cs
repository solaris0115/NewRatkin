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
        public static ThingDef RK_GuerrillaTunnelSpawner;

        public static ThingDef RK_EmpBomb;

        public static ThingDef RK_Pulpit;
    }

    [DefOf]
    public static class RatkinWeaponDefOf
    {
        public static ThingDef RK_MagicWand;
        public static ThingDef RK_Weapon_RatHolicGun;
    }

    [DefOf]
    public static class RatkinPawnKindDefOf
    {
        public static PawnKindDef RatkinNoble;

        public static PawnKindDef RatkinColonist;

        public static PawnKindDef RatkinServant;

        public static PawnKindDef RatkinCombatant;

        public static PawnKindDef RatkinSoldier;

        public static PawnKindDef RatkinMercenary;

        public static PawnKindDef RatkinEliteGuardener;

        public static PawnKindDef RatkinPriest;

        public static PawnKindDef RatkinMerchant;

        public static PawnKindDef RatkinMurderer;

        //괴도 찍찍이 확장팩
        public static PawnKindDef RatkinDemonMan;
        public static PawnKindDef RatkinEliteSoldier;

        // 필그림
        public static PawnKindDef RK_PawnKind_Pilgrim;
        public static PawnKindDef RK_PawnKind_Priest;
        public static PawnKindDef RK_PawnKind_NoblePilgrim;

        // 유랑 상인 판매용
        public static PawnKindDef RK_PawnKind_Nomad;
        public static PawnKindDef RK_PawnKind_Wanderer;
        public static PawnKindDef RK_PawnKind_CaravanLeader;
        public static PawnKindDef RK_PawnKind_CaravanGuard;
        public static PawnKindDef Ratkin_KingHamster;
    }

    [DefOf]
    public static class RatkinFactionDefOf
    {
        public static FactionDef Rakinia;
        public static FactionDef RK_Faction_Pilgrims;
        public static FactionDef RK_Faction_Caravan;
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
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public static IncidentDef GiveQuest_ReliquaryPilgrims_Ratkin;
        public static IncidentDef RK_Incident_WanderingTrader;
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
        public static JobDef RK_Job_ShieldFaceDirection;
        public static JobDef RK_Job_TalkToCaravanLeader;
        public static JobDef RK_Job_DismissCaravanLeader;
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

    [DefOf]
    public static class RatkinHediffDefOf
    {
        public static HediffDef RK_Hediff_RatHolicGunSpooling;
    }

    [DefOf]
    public static class RatkinStatDefOf
    {
        public static StatDef RK_Stat_RangeCoolDown;
        public static StatDef RK_Stat_RangeCoolDownMultiplier;
        public static StatDef RK_Stat_ShieldBlockChance;
        public static StatDef RK_Stat_DeflectAngle;
        public static StatDef RK_Stat_ShieldStuffBase;
        public static StatDef RK_Stat_Shield_Sharp;
        public static StatDef RK_Stat_Shield_Blunt;
        public static StatDef RK_Stat_Shield_Heat;
    }

    [DefOf]
    public static class RatkinKeyBindingDefOf
    {
        public static KeyBindingDef RK_OpenInfoCard;
    }
}
