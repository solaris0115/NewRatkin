using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NewRatkin.AlienRaceCompat
{
    /// <summary>
    ///     <see cref="AlienRace.RaceSettings" />의 <c>universalBodyAddons</c>는 Def당 싱글톤이며,
    ///     <c>ConditionBodyPart.bodyPartLabel</c>은 HAR가 <c>BodyPartRecord.untranslatedCustomLabel</c>과 비교하므로
    ///     번역 문자열이 들어가면 깨집니다.
    ///     플레이 스테이지 진입 직후 <see cref="Game.FinalizeInit" />에서 한 번만 원문으로 맞춘다(신규 게임·세이브 로드 공통).
    ///     (디버그) AutoTranslation <c>InjectTranslation</c> 시점 로그는 아래 패치·메서드 주석 참고.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class RaceSettingsUniversalBodyAddonCanonicalRestore
    {
        private const string HarmonyId = "com.NewRatkin.rimworld.mod.racesettingsbodyaddonrestore";

        private const string RaceSettingsDefName = "RK_Race_Setting";

        // 디버그: InjectTranslation 추적 시에만 필요
        // private static readonly Type ConditionBodyPartType =
        //     AccessTools.TypeByName("AlienRace.ExtendedGraphics.ConditionBodyPart");

        /// <summary>그래픽 경로(랫킨 AlienRaceSettings.xml) → 첫 번째 BodyPart 조건에 복원할 값.</summary>
        private static readonly Dictionary<string, BodyPartConditionCanon> CanonByAddonPath =
            new Dictionary<string, BodyPartConditionCanon>
            {
                {
                    "Things/Ratkin/BodyAddon/RK_Texture_Tail",
                    new BodyPartConditionCanon("RK_BodyPart_Tail", "tail", false)
                },
                {
                    "Things/Ratkin/BodyAddon/RK_Texture_EarLeft",
                    new BodyPartConditionCanon("Ear", "left ear", false)
                },
                {
                    "Things/Ratkin/BodyAddon/RK_Texture_EarRight",
                    new BodyPartConditionCanon("Ear", "right ear", false)
                }
            };

        static RaceSettingsUniversalBodyAddonCanonicalRestore()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.Patch(
                AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)),
                postfix: new HarmonyMethod(
                    typeof(RaceSettingsUniversalBodyAddonCanonicalRestore),
                    nameof(AfterGameFinalizeInit)));

            // 디버그: AutoTranslation이 bodyPartLabel 넣는 순간 Player.log
            // Type injectParamsType =
            //     AccessTools.TypeByName("AutoTranslation.DefInjectionUtilityCustom+DefInjectionUntranslatedParams");
            // MethodInfo injectTranslation = injectParamsType == null
            //     ? null
            //     : AccessTools.Method(injectParamsType, "InjectTranslation");
            // if (injectTranslation != null)
            // {
            //     harmony.Patch(
            //         injectTranslation,
            //         postfix: new HarmonyMethod(
            //             typeof(RaceSettingsUniversalBodyAddonCanonicalRestore),
            //             nameof(LogAfterAutoTranslateInjectTranslation)));
            // }
        }

        /// <summary>
        ///     신규 게임 <c>InitNewGame</c> 또는 <c>LoadGame</c> 끝에서 호출된다. 맵이 준비된 뒤
        ///     <c>ProgramState.Playing</c>으로 넘어가기 직전 단계까지 끝난 시점이다.
        /// </summary>
        private static void AfterGameFinalizeInit(Game __instance)
        {
            if (__instance == null)
                return;

            try
            {
                ApplyCanonicalBodyPartConditions();
            }
            catch (Exception ex)
            {
                Log.Warning($"[Ratkin] RaceSettings universalBodyAddons canonical restore (Game.FinalizeInit): {ex}");
            }
        }

        /*
        /// <summary>
        ///     AutoTranslation이 <c>InjectTranslation</c>으로 필드를 쓴 직후: <c>ConditionBodyPart.bodyPartLabel</c>이면
        ///     어떤 값으로 바뀌었는지 로그한다.
        /// </summary>
        private static void LogAfterAutoTranslateInjectTranslation(object __instance)
        {
            try
            {
                if (ConditionBodyPartType == null || __instance == null)
                    return;

                Traverse tr = Traverse.Create(__instance);
                if (tr.Field("isCollection").GetValue<bool>())
                    return;

                FieldInfo fi = tr.Field("field").GetValue<FieldInfo>();
                if (fi == null || fi.Name != "bodyPartLabel" || fi.DeclaringType != ConditionBodyPartType)
                    return;

                object parent = tr.Field("parentObject").GetValue<object>();
                if (parent == null)
                    return;

                string valueNow = fi.GetValue(parent) as string;
                Def def = tr.Field("def").GetValue<Def>();
                string normalizedPath = tr.Field("normalizedPath").GetValue<string>();
                string suggestedPath = tr.Field("suggestedPath").GetValue<string>();
                string original = tr.Field("original").GetValue<string>();
                string translatedParam = tr.Field("translated").GetValue<string>();

                Log.Message(
                    "[Ratkin] bodyPartLabel 변경(InjectTranslation 직후) | def=" +
                    (def == null ? "null" : def.defName) +
                    " | normalizedPath=" + FmtQuoted(normalizedPath) +
                    " | suggestedPath=" + FmtQuoted(suggestedPath) +
                    " | original=" + FmtQuoted(original) +
                    " | translated(매개변수)=" + FmtQuoted(translatedParam) +
                    " | 필드값(현재)=" + FmtQuoted(valueNow));
            }
            catch (Exception ex)
            {
                Log.Warning("[Ratkin] bodyPartLabel InjectTranslation 로그 실패: " + ex);
            }
        }
        */

        private static void ApplyCanonicalBodyPartConditions()
        {
            Type raceSettingsType = AccessTools.TypeByName("AlienRace.RaceSettings");
            Type conditionBodyPartType = AccessTools.TypeByName("AlienRace.ExtendedGraphics.ConditionBodyPart");
            if (raceSettingsType == null || conditionBodyPartType == null)
                return;

            Def raceSettings = GenDefDatabase.GetDefSilentFail(raceSettingsType, RaceSettingsDefName);
            if (raceSettings == null)
                return;

            IList addons = Traverse.Create(raceSettings).Field("universalBodyAddons").GetValue<IList>();
            if (addons == null)
                return;

            for (int i = 0; i < addons.Count; i++)
            {
                object addon = addons[i];
                if (addon == null)
                    continue;

                string path = Traverse.Create(addon).Field("path").GetValue<string>();
                if (path.NullOrEmpty() || !CanonByAddonPath.TryGetValue(path, out BodyPartConditionCanon canon))
                    continue;

                IList conditions = Traverse.Create(addon).Field("conditions").GetValue<IList>();
                if (conditions == null)
                    continue;

                for (int c = 0; c < conditions.Count; c++)
                {
                    object cond = conditions[c];
                    if (cond == null || cond.GetType() != conditionBodyPartType)
                        continue;

                    Traverse t = Traverse.Create(cond);
                    bool applyBodyPart = !canon.BodyPartDefName.NullOrEmpty();
                    BodyPartDef partCanon = applyBodyPart
                        ? DefDatabase<BodyPartDef>.GetNamedSilentFail(canon.BodyPartDefName)
                        : null;

                    if (partCanon != null)
                        t.Field("bodyPart").SetValue(partCanon);

                    t.Field("bodyPartLabel").SetValue(canon.BodyPartLabel);
                    t.Field("drawWithoutPart").SetValue(canon.DrawWithoutPart);

                    // 디버그: 복구 전후 diff 로그
                    /*
                    BodyPartDef partWas = t.Field("bodyPart").GetValue<BodyPartDef>();
                    string labelWas = t.Field("bodyPartLabel").GetValue<string>();
                    bool drawWas = t.Field("drawWithoutPart").GetValue<bool>();
                    bool partDiff = applyBodyPart && partCanon != partWas;
                    bool labelDiff = canon.BodyPartLabel != labelWas;
                    bool drawDiff = canon.DrawWithoutPart != drawWas;
                    if (partDiff || labelDiff || drawDiff)
                    {
                        string partWasName = partWas == null ? "null" : partWas.defName;
                        string partCanonName = partCanon == null ? "null" : partCanon.defName;
                        Log.Message(
                            "[Ratkin] RaceSettings universalBodyAddon restore | path=" + path +
                            " | bodyPart: " + partWasName + " -> " + partCanonName +
                            " | bodyPartLabel: " + FmtQuoted(labelWas) + " -> " + FmtQuoted(canon.BodyPartLabel) +
                            " | drawWithoutPart: " + drawWas + " -> " + canon.DrawWithoutPart +
                            " (HAR는 untranslatedCustomLabel과 동일한 문자열이어야 함)");
                    }
                    */

                    break;
                }
            }
        }

        // 디버그: LogAfterAutoTranslateInjectTranslation / 위 블록에서만 사용
        /*
        private static string FmtQuoted(string s) =>
            s == null ? "null" : "\"" + s.Replace("\"", "\\\"") + "\"";
        */

        private readonly struct BodyPartConditionCanon
        {
            public readonly string BodyPartDefName;
            public readonly string BodyPartLabel;
            public readonly bool DrawWithoutPart;

            public BodyPartConditionCanon(string bodyPartDefName, string bodyPartLabel, bool drawWithoutPart)
            {
                this.BodyPartDefName = bodyPartDefName;
                this.BodyPartLabel = bodyPartLabel;
                this.DrawWithoutPart = drawWithoutPart;
            }
        }
    }
}
