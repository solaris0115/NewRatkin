using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NewRatkin.AlienRaceCompat
{
    /// <summary>
    ///     DefDatabase에 올라간 <c>AlienRace.RaceSettings</c> 싱글톤(들)과,
    ///     이미 구축된 경우 <c>Utilities.UniversalBodyAddons</c> 정적 캐시를 Player.log에 출력합니다.
    /// </summary>
    internal static class RaceSettingsDebugDumper
    {
        private const int MaxPublicFieldDumpDepth = 4;

        internal static void DumpToPlayerLog()
        {
            Type raceSettingsType = AccessTools.TypeByName("AlienRace.RaceSettings");
            if (raceSettingsType == null)
            {
                Log.Warning("[Ratkin] RaceSettings dump: AlienRace.RaceSettings type not found (Humanoid Alien Races not loaded?).");
                return;
            }

            var sb = new StringBuilder(8192);
            sb.AppendLine("======== [Ratkin] AlienRace.RaceSettings dump (Def instances) ========");

            IEnumerable<Def> allRaceSettings = GenDefDatabase.GetAllDefsInDatabaseForDef(raceSettingsType);
            int n = 0;
            foreach (Def def in allRaceSettings)
            {
                n++;
                AppendRaceSettingsBlock(sb, def);
            }

            if (n == 0)
                sb.AppendLine("(no RaceSettings defs in database)");

            sb.AppendLine("-------- Utilities.UniversalBodyAddons (static cache, if built) --------");
            AppendUniversalBodyAddonsCache(sb);

            sb.AppendLine("======== end RaceSettings dump ========");
            Log.Message(sb.ToString());
        }

        private static void AppendRaceSettingsBlock(StringBuilder sb, Def def)
        {
            string modLabel = def.modContentPack?.Name ?? "?";
            sb.AppendLine($"--- RaceSettings defName={def.defName}  mod={modLabel}  shortHash={def.shortHash}  GetHashCode={def.GetHashCode()} ---");

            object pawnKindSettings = Traverse.Create(def).Field("pawnKindSettings").GetValue();
            if (pawnKindSettings != null)
            {
                sb.AppendLine("  pawnKindSettings:");
                AppendIndentedPublicFields(sb, pawnKindSettings, "    ", MaxPublicFieldDumpDepth);
            }
            else
                sb.AppendLine("  pawnKindSettings: null");

            IList addons = Traverse.Create(def).Field("universalBodyAddons").GetValue<IList>();
            if (addons == null)
            {
                sb.AppendLine("  universalBodyAddons: null");
                return;
            }

            sb.AppendLine($"  universalBodyAddons: count={addons.Count}");
            for (int i = 0; i < addons.Count; i++)
            {
                object addon = addons[i];
                sb.AppendLine($"    [{i}] type={addon?.GetType().FullName ?? "null"}  hash={addon?.GetHashCode()}");
                if (addon == null)
                    continue;

                AppendIndentedPublicFields(sb, addon, "      ", MaxPublicFieldDumpDepth);
            }
        }

        private static void AppendUniversalBodyAddonsCache(StringBuilder sb)
        {
            Type utilitiesType = AccessTools.TypeByName("AlienRace.Utilities");
            if (utilitiesType == null)
            {
                sb.AppendLine("(AlienRace.Utilities not found)");
                return;
            }

            FieldInfo fi = utilitiesType.GetField("universalBodyAddons", BindingFlags.Static | BindingFlags.NonPublic);
            if (fi == null)
            {
                sb.AppendLine("(field universalBodyAddons not found)");
                return;
            }

            object raw = fi.GetValue(null);
            if (raw == null)
            {
                sb.AppendLine("(not yet built — first access to Utilities.UniversalBodyAddons fills this list)");
                return;
            }

            IList list = raw as IList;
            if (list == null)
            {
                sb.AppendLine($"(unexpected type {raw.GetType().FullName})");
                return;
            }

            sb.AppendLine($"count={list.Count}  (same BodyAddon instances as on RaceSettings defs once populated)");
            for (int i = 0; i < list.Count; i++)
            {
                object addon = list[i];
                sb.AppendLine($"  cache[{i}] type={addon?.GetType().FullName ?? "null"}  hash={addon?.GetHashCode()}");
                if (addon != null)
                    AppendIndentedPublicFields(sb, addon, "    ", MaxPublicFieldDumpDepth);
            }
        }

        private static void AppendIndentedPublicFields(StringBuilder sb, object obj, string indent, int maxDepth) =>
            AppendIndentedPublicFields(sb, obj, indent, maxDepth, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));

        private static void AppendIndentedPublicFields(StringBuilder sb, object obj, string indent, int maxDepth, int depth, HashSet<object> visited)
        {
            if (obj == null)
            {
                sb.AppendLine($"{indent}null");
                return;
            }

            Type t = obj.GetType();
            if (t.IsPrimitive || obj is string || obj is decimal)
            {
                sb.AppendLine($"{indent}{FormatLeaf(obj)}");
                return;
            }

            if (obj is Def defLeaf)
            {
                sb.AppendLine($"{indent}{t.Name}(defName={defLeaf.defName})");
                return;
            }

            if (depth >= maxDepth)
            {
                sb.AppendLine($"{indent}{t.Name} … (max depth)");
                return;
            }

            if (!t.IsValueType)
            {
                if (visited.Contains(obj))
                {
                    sb.AppendLine($"{indent}{t.Name} <cycle>");
                    return;
                }

                visited.Add(obj);
            }

            if (obj is IEnumerable enumerable && !(obj is string))
            {
                int idx = 0;
                foreach (object item in enumerable)
                {
                    sb.AppendLine($"{indent}[{idx}]");
                    AppendIndentedPublicFields(sb, item, indent + "  ", maxDepth, depth + 1, visited);
                    idx++;
                    if (idx >= 200)
                    {
                        sb.AppendLine($"{indent}… truncated after 200 elements");
                        break;
                    }
                }

                if (idx == 0)
                    sb.AppendLine($"{indent}(empty sequence)");

                if (!t.IsValueType)
                    visited.Remove(obj);

                return;
            }

            foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object val;
                try
                {
                    val = field.GetValue(obj);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{indent}{field.Name}: <read error {ex.Message}>");
                    continue;
                }

                if (val == null)
                {
                    sb.AppendLine($"{indent}{field.Name}: null");
                    continue;
                }

                Type ft = field.FieldType;
                if (ft.IsPrimitive || val is string || val is decimal)
                {
                    sb.AppendLine($"{indent}{field.Name}: {FormatLeaf(val)}");
                    continue;
                }

                if (val is Def d)
                {
                    sb.AppendLine($"{indent}{field.Name}: {ft.Name}(defName={d.defName})");
                    continue;
                }

                UnityEngine.Object uo = val as UnityEngine.Object;
                if (uo != null && !(val is Def))
                {
                    sb.AppendLine($"{indent}{field.Name}: UnityEngine.Object({ft.Name}) name={uo.name}");
                    continue;
                }

                if (val is IEnumerable seq && !(val is string))
                {
                    sb.AppendLine($"{indent}{field.Name}: IEnumerable<{GetEnumerableElementHint(ft)}> —");
                    AppendIndentedPublicFields(sb, seq, indent + "  ", maxDepth, depth + 1, visited);
                    continue;
                }

                sb.AppendLine($"{indent}{field.Name}: {ft.Name} —");
                AppendIndentedPublicFields(sb, val, indent + "  ", maxDepth, depth + 1, visited);
            }

            if (!t.IsValueType)
                visited.Remove(obj);
        }

        private static string FormatLeaf(object val)
        {
            if (val is string s)
                return "\"" + s + "\"";
            if (val is float f)
                return f.ToString("G9");
            if (val is double d)
                return d.ToString("G17");
            return val.ToString();
        }

        private static string GetEnumerableElementHint(Type ft)
        {
            if (!ft.IsGenericType)
                return "?";
            Type[] args = ft.GetGenericArguments();
            return args.Length > 0 ? args[0].Name : "?";
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
