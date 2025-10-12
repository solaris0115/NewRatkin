using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
    public class Verb_MeleeAttackDamage : Verb_MeleeAttack
    {
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            return null;
        }

        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            if (!casterPawn.Spawned)
            {
                return false;
            }
            if (casterPawn.stances.FullBodyBusy)
            {
                return false;
            }
            Thing thing = currentTarget.Thing;
            if (!CanHitTarget(thing))
            {
                Log.Warning(string.Concat(new object[]
                {
                    casterPawn,
                    " meleed ",
                    thing,
                    " from out of melee position."
                }));
            }
            casterPawn.rotationTracker.Face(thing.DrawPos);
            if (!IsTargetImmobile(currentTarget) && casterPawn.skills != null)
            {
                casterPawn.skills.Learn(SkillDefOf.Melee, 200f * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
            }
            Pawn pawn = thing as Pawn;
            if (pawn != null && !pawn.Dead && (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || pawn.MentalStateDef != MentalStateDefOf.SocialFighting))
            {
                pawn.mindState.meleeThreat = casterPawn;
                pawn.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
            }
            Map map = thing.Map;
            Vector3 drawPos = thing.DrawPos;
            SoundDef soundDef;
            bool result;
            if (Rand.Chance(GetNonMissChance(thing)))
            {
                if (!Rand.Chance(GetDodgeChance(thing)))
                {
                    if (thing.def.category == ThingCategory.Building)
                    {
                        soundDef = SoundHitBuilding();
                    }
                    else
                    {
                        soundDef = SoundHitPawn();
                    }
                    if (verbProps.impactMote != null)
                    {
                        MoteMaker.MakeStaticMote(drawPos, map, verbProps.impactMote, 1f);
                    }
                    //BattleLogEntry_MeleeCombat battleLogEntry_MeleeCombat = CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, true);
                    
                    result = true;
                    Explosion();
                }
                else
                {
                    result = false;
                    soundDef = SoundMiss();
                    MoteMaker.ThrowText(drawPos, map, "TextMote_Dodge".Translate(), 1.9f);
                    CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesDodge, false);
                }
            }
            else
            {
                result = false;
                soundDef = SoundMiss();
                CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesMiss, false);
            }
            soundDef.PlayOneShot(new TargetInfo(thing.Position, map, false));
            if (casterPawn.Spawned)
            {
                casterPawn.Drawer.Notify_MeleeAttackOn(thing);
            }
            if (pawn != null && !pawn.Dead && pawn.Spawned)
            {
                pawn.stances.stagger.StaggerFor(95);
            }
            if (casterPawn.Spawned)
            {
                casterPawn.rotationTracker.FaceCell(thing.Position);
            }
            if (casterPawn.caller != null)
            {
                casterPawn.caller.Notify_DidMeleeAttack();
            }
            return result;
        }
        public void Explosion()
        {
            Map map2 = EquipmentSource.Map;
            GenExplosion.DoExplosion
                (
                    center: currentTarget.Thing.Position,
                    map: CasterPawn.Map,
                    radius: 2f,
                    damType: RatkinDamageDefOf.DemoBomb,
                    instigator: EquipmentSource,
                    damAmount: 15,
                    armorPenetration: 0.60f,
                    explosionSound: null,
                    weapon: EquipmentSource.def,
                    projectile: null,
                    intendedTarget: currentTarget.Thing,
                    chanceToStartFire: 0,
                    damageFalloff: true
                );
            if(CasterPawn!=null && CasterPawn.inventory!=null)
            {
                Thing magicWand = CasterPawn.inventory.innerContainer.FirstOrDefault((Thing t) => t!=null && t.def == RatkinWeaponDefOf.RK_MagicWand);
                if (magicWand!=null)
                {
                    magicWand.stackCount -= 1;
                    if (magicWand.stackCount <= 0)
                    {
                        CasterPawn.inventory.innerContainer.Remove(magicWand);
                        CasterPawn.inventory.Notify_ItemRemoved(magicWand);
                        magicWand.Destroy(DestroyMode.Vanish);
                    }
                }
                else
                {
                    if (EquipmentSource != null && !EquipmentSource.Destroyed)
                    {
                        EquipmentSource.Destroy(DestroyMode.Vanish);
                    }
                }
            }           
        }
        private SoundDef SoundMiss()
        {
            if (CasterPawn != null && !CasterPawn.def.race.soundMeleeMiss.NullOrUndefined())
            {
                return CasterPawn.def.race.soundMeleeMiss;
            }
            return SoundDefOf.Pawn_Melee_Punch_Miss;
        }
        private SoundDef SoundHitPawn()
        {
            if (EquipmentSource != null && EquipmentSource.Stuff != null)
            {
                if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
                    {
                        return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
                    }
                }
                else if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                {
                    return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
                }
            }
            if (CasterPawn != null && !CasterPawn.def.race.soundMeleeHitPawn.NullOrUndefined())
            {
                return base.CasterPawn.def.race.soundMeleeHitPawn;
            }
            return SoundDefOf.Pawn_Melee_Punch_HitPawn;
        }
        private SoundDef SoundHitBuilding()
        {
            if (EquipmentSource != null && EquipmentSource.Stuff != null)
            {
                if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
                    {
                        return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
                    }
                }
                else if (!base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                {
                    return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
                }
            }
            if (CasterPawn != null && !CasterPawn.def.race.soundMeleeHitBuilding.NullOrUndefined())
            {
                return base.CasterPawn.def.race.soundMeleeHitBuilding;
            }
            return SoundDefOf.Pawn_Melee_Punch_HitBuilding_Generic;
        }
        private bool IsTargetImmobile(LocalTargetInfo target)
        {
            Thing thing = target.Thing;
            Pawn pawn = thing as Pawn;
            return thing.def.category != ThingCategory.Pawn || pawn.Downed || pawn.GetPosture() != PawnPosture.Standing;
        }
        private float GetNonMissChance(LocalTargetInfo target)
        {
            if (surpriseAttack)
            {
                return 1f;
            }
            if (IsTargetImmobile(target))
            {
                return 1f;
            }
            return CasterPawn.GetStatValue(StatDefOf.MeleeHitChance, true);
        }
        private float GetDodgeChance(LocalTargetInfo target)
        {
            if (surpriseAttack)
            {
                return 0f;
            }
            if (IsTargetImmobile(target))
            {
                return 0f;
            }
            Pawn pawn = target.Thing as Pawn;
            if (pawn == null)
            {
                return 0f;
            }
            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
            if (stance_Busy != null && stance_Busy.verb != null && !stance_Busy.verb.verbProps.IsMeleeAttack)
            {
                return 0f;
            }
            return pawn.GetStatValue(StatDefOf.MeleeDodgeChance, true);
        }
    }
}
