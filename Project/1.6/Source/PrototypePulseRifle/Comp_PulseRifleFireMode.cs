using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NewRatkin
{
    /// <summary>
    /// Prototype Pulse Rifle 3점사/단발 모드 토글 Comp
    /// 커맨드창 Gizmo로 모드 전환, 세이브/로드 시 마지막 선택 기억
    /// </summary>
    public class Comp_PulseRifleFireMode : ThingComp
    {
        private bool isBurstMode = true;

        private VerbProperties burstVerbProps;
        private VerbProperties singleVerbProps;

        private static readonly FieldInfo CachedBurstShotCountField = AccessTools.Field(typeof(Verb), "cachedBurstShotCount");
        private static readonly FieldInfo CachedTicksBetweenBurstShotsField = AccessTools.Field(typeof(Verb), "cachedTicksBetweenBurstShots");

        public CompProperties_PulseRifleFireMode Props => (CompProperties_PulseRifleFireMode)props;

        public bool IsBurstMode => isBurstMode;

        /// <summary>
        /// 현재 선택된 발사체 ThingDef
        /// </summary>
        public ThingDef CurrentProjectile => isBurstMode ? Props.projectileBurst : Props.projectileSingle;

        /// <summary>
        /// 현재 모드의 VerbProperties
        /// </summary>
        public VerbProperties CurrentVerbProps => isBurstMode ? burstVerbProps : singleVerbProps;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isBurstMode, "isBurstMode", true);
            // 불러오기 시 VerbTracker.InitVerbs가 Def의 verbProps로 덮어써서 range가 1.42로 됨.
            // 로드 직후 재적용하여 rangeBurst/rangeSingle 복구.
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureVerbPropsBuilt();
                ApplyVerbProps();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureVerbPropsBuilt();
            ApplyVerbProps();
        }

        public override float GetStatOffset(StatDef stat)
        {
            if (stat == null) return 0f;

            var offsets = isBurstMode ? Props.statOffsetsBurst : Props.statOffsetsSingle;
            if (offsets == null) return 0f;

            for (int i = 0; i < offsets.Count; i++)
            {
                if (offsets[i].stat == stat)
                {
                    return offsets[i].value;
                }
            }
            return 0f;
        }

        /// <summary>
        /// 탄종 토글 Gizmo 반환. 장비 시 CompGetEquippedGizmosExtra 패치에서 호출됨.
        /// </summary>
        public IEnumerable<Gizmo> GetToggleGizmos()
        {
            string iconPath = isBurstMode ? Props.iconPathBurst : Props.iconPathSingle;
            Texture2D icon = ContentFinder<Texture2D>.Get(iconPath, false);

            yield return new Command_Action
            {
                defaultLabel = (isBurstMode ? "RK_PulseRifleFireMode_LabelBurst" : "RK_PulseRifleFireMode_LabelSingle").Translate().ToString(),
                defaultDesc = (isBurstMode ? "RK_PulseRifleFireMode_DescToSingle" : "RK_PulseRifleFireMode_DescToBurst").Translate().ToString(),
                icon = icon,
                action = () =>
                {
                    isBurstMode = !isBurstMode;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                    ApplyVerbProps();
                    // 발사 직전 토글 시 잘못된 모드 발사 방지: 조준 초기화
                    Pawn holder = GetHolderPawn();
                    if (holder != null && holder.stances.curStance is Stance_Warmup)
                    {
                        holder.stances.CancelBusyStanceSoft();
                    }
                }
            };
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (!ShouldShowGizmo())
            {
                yield break;
            }

            foreach (Gizmo gizmo in GetToggleGizmos())
            {
                yield return gizmo;
            }
        }

        private void EnsureVerbPropsBuilt()
        {
            if (burstVerbProps != null && singleVerbProps != null)
            {
                return;
            }

            var baseVerbProps = GetBaseVerbProperties();
            if (baseVerbProps == null)
            {
                return;
            }

            burstVerbProps = CopyVerbProperties(baseVerbProps);
            burstVerbProps.range = Props.rangeBurst;
            burstVerbProps.burstShotCount = Props.burstShotCountBurst;
            burstVerbProps.ticksBetweenBurstShots = Props.ticksBetweenBurstShotsBurst;
            burstVerbProps.warmupTime = Props.warmupTimeBurst;
            burstVerbProps.defaultProjectile = Props.projectileBurst;

            singleVerbProps = CopyVerbProperties(baseVerbProps);
            singleVerbProps.range = Props.rangeSingle;
            singleVerbProps.burstShotCount = Props.burstShotCountSingle;
            singleVerbProps.ticksBetweenBurstShots = Props.ticksBetweenBurstShotsSingle;
            singleVerbProps.warmupTime = Props.warmupTimeSingle;
            singleVerbProps.defaultProjectile = Props.projectileSingle;
        }

        private VerbProperties GetBaseVerbProperties()
        {
            var verbs = parent.def.Verbs;
            if (verbs == null || verbs.Count == 0)
            {
                return null;
            }
            return verbs[0];
        }

        private static VerbProperties CopyVerbProperties(VerbProperties source)
        {
            var copy = new VerbProperties();
            var fields = typeof(VerbProperties).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.IsLiteral) continue;
                try
                {
                    var value = field.GetValue(source);
                    field.SetValue(copy, value);
                }
                catch
                {
                    // 일부 필드 복사 실패 시 무시
                }
            }
            return copy;
        }

        private void ApplyVerbProps()
        {
            var compEquippable = parent.GetComp<CompEquippable>();
            if (compEquippable == null) return;

            var verbs = compEquippable.AllVerbs;
            var targetProps = CurrentVerbProps;
            if (targetProps == null) return;

            foreach (var verb in verbs)
            {
                if (verb is Verb_PulseRifleShoot)
                {
                    verb.verbProps = targetProps;
                    InvalidateVerbCache(verb);
                }
            }
        }

        private static void InvalidateVerbCache(Verb verb)
        {
            try
            {
                CachedBurstShotCountField?.SetValue(verb, null);
                CachedTicksBetweenBurstShotsField?.SetValue(verb, null);
            }
            catch
            {
                // Reflection 실패 시 무시
            }
        }

        private bool ShouldShowGizmo()
        {
            if (parent.Faction != null && parent.Faction != Faction.OfPlayer)
            {
                return false;
            }

            if (parent.Spawned)
            {
                return parent.Map?.IsPlayerHome ?? false;
            }

            Pawn holderPawn = GetHolderPawn();
            if (holderPawn != null)
            {
                return holderPawn.Faction == Faction.OfPlayer;
            }

            if (parent.ParentHolder is Thing holderThing)
            {
                return holderThing.Map?.IsPlayerHome ?? false;
            }

            return false;
        }

        private Pawn GetHolderPawn()
        {
            if (parent.ParentHolder is Pawn_EquipmentTracker tracker)
            {
                return tracker.pawn;
            }
            return parent.ParentHolder as Pawn;
        }
    }
}
