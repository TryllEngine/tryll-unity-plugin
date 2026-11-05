// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// JSON form of typed checks and their outcomes for the Lab's JSONL export (format 4):
    /// <c>{"type": "must_contain", "enabled": true, "note": "", "text": "…", …}</c>.
    /// <c>type</c> is <see cref="TryllDialogCheck.TypeId"/>, never the C# class name, so
    /// the file outlives a class rename. Write and read live together so they cannot drift.
    /// </summary>
    internal static class TryllDialogCheckJson
    {
        public static string Write(TryllDialogCheck check)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            Kv(sb, "type", check.TypeId, first: true);
            Kv(sb, "enabled", check.Enabled);
            Kv(sb, "note", check.Note ?? "");
            switch (check)
            {
                case TryllMustContainCheck c: Kv(sb, "text", c.Text); Kv(sb, "case_sensitive", c.CaseSensitive); break;
                case TryllMustNotContainCheck c: Kv(sb, "text", c.Text); Kv(sb, "case_sensitive", c.CaseSensitive); break;
                case TryllRegexForbidCheck c: Kv(sb, "pattern", c.Pattern); break;
                case TryllRegexMatchCheck c: Kv(sb, "pattern", c.Pattern); break;
                case TryllWordCountCheck c: Kv(sb, "min", c.Min); Kv(sb, "max", c.Max); break;
                case TryllIntentIsCheck c: Kv(sb, "node", c.Node); Kv(sb, "intent", c.Intent); break;
                case TryllIntentNotFoundCheck c: Kv(sb, "node", c.Node); break;
                case TryllAttributeEqualsCheck c: Kv(sb, "node", c.Node); Kv(sb, "name", c.Name); Kv(sb, "value", c.Value); break;
                case TryllAttributeInRangeCheck c: Kv(sb, "node", c.Node); Kv(sb, "name", c.Name); Kv(sb, "min", c.Min); Kv(sb, "max", c.Max); break;
                case TryllCallsToolCheck c: Kv(sb, "tool_name", c.ToolName); break;
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string WriteList(IEnumerable<TryllDialogCheck> checks) =>
            "[" + string.Join(",", (checks ?? Enumerable.Empty<TryllDialogCheck>()).Select(Write)) + "]";

        /// <summary>Null (with <paramref name="error"/>) for an unknown or missing type.</summary>
        public static TryllDialogCheck Read(TryllJsonValue obj, out string error)
        {
            error = null;
            string type = obj.Get("type").AsString("");
            var check = TryllDialogCheck.Create(type);
            if (check == null)
            {
                error = $"unknown check type \"{type}\"";
                return null;
            }
            check.Enabled = obj.Get("enabled").AsBool(true);
            check.Note = obj.Get("note").AsString("");
            switch (check)
            {
                case TryllMustContainCheck c: c.Text = Str(obj, "text"); c.CaseSensitive = obj.Get("case_sensitive").AsBool(false); break;
                case TryllMustNotContainCheck c: c.Text = Str(obj, "text"); c.CaseSensitive = obj.Get("case_sensitive").AsBool(false); break;
                case TryllRegexForbidCheck c: c.Pattern = Str(obj, "pattern"); break;
                case TryllRegexMatchCheck c: c.Pattern = Str(obj, "pattern"); break;
                case TryllWordCountCheck c: c.Min = obj.Get("min").AsInt(0); c.Max = obj.Get("max").AsInt(0); break;
                case TryllIntentIsCheck c: c.Node = Str(obj, "node"); c.Intent = Str(obj, "intent"); break;
                case TryllIntentNotFoundCheck c: c.Node = Str(obj, "node"); break;
                case TryllAttributeEqualsCheck c: c.Node = Str(obj, "node"); c.Name = Str(obj, "name"); c.Value = Str(obj, "value"); break;
                case TryllAttributeInRangeCheck c:
                    c.Node = Str(obj, "node");
                    c.Name = Str(obj, "name");
                    c.Min = (float)obj.Get("min").AsDouble(0);
                    c.Max = (float)obj.Get("max").AsDouble(1);
                    break;
                case TryllCallsToolCheck c: c.ToolName = Str(obj, "tool_name"); break;
            }
            return check;
        }

        public static bool TryReadList(TryllJsonValue arr, List<TryllDialogCheck> into, out string error)
        {
            error = null;
            foreach (var item in arr.AsArray())
            {
                var check = Read(item, out error);
                if (check == null) return false;
                into.Add(check);
            }
            return true;
        }

        public static string WriteOutcomes(IEnumerable<TryllLabCheckOutcome> outcomes)
        {
            var items = (outcomes ?? Enumerable.Empty<TryllLabCheckOutcome>()).Select(o =>
            {
                var sb = new StringBuilder();
                sb.Append('{');
                Kv(sb, "scope", ScopeName(o.Scope), first: true);
                Kv(sb, "index", o.Index);
                Kv(sb, "type", o.CheckType ?? "");
                Kv(sb, "kind", o.Kind ?? "");
                Kv(sb, "passed", o.Passed);
                Kv(sb, "arg", o.Arg ?? "");
                if (!string.IsNullOrEmpty(o.Actual)) Kv(sb, "actual", o.Actual);
                Kv(sb, "label", o.Label ?? "");
                sb.Append('}');
                return sb.ToString();
            });
            return "[" + string.Join(",", items) + "]";
        }

        public static List<TryllLabCheckOutcome> ReadOutcomes(TryllJsonValue arr) =>
            arr.AsArray().Select(o => new TryllLabCheckOutcome
            {
                Scope = ParseScope(o.Get("scope").AsString("step")),
                Index = o.Get("index").AsInt(-1),
                CheckType = o.Get("type").AsString(""),
                Kind = o.Get("kind").AsString(""),
                Passed = o.Get("passed").AsBool(false),
                Arg = o.Get("arg").AsString(""),
                Actual = o.HasKey("actual") ? o.Get("actual").AsString(null) : null,
                Label = o.Get("label").AsString(""),
            }).ToList();

        static string ScopeName(TryllLabCheckScope scope) => scope switch
        {
            TryllLabCheckScope.Global => "global",
            TryllLabCheckScope.Gate => "gate",
            _ => "step",
        };

        static TryllLabCheckScope ParseScope(string s) => s switch
        {
            "global" => TryllLabCheckScope.Global,
            "gate" => TryllLabCheckScope.Gate,
            _ => TryllLabCheckScope.Step,
        };

        static string Str(TryllJsonValue obj, string key) => obj.Get(key).AsString("");

        static void Kv(StringBuilder sb, string key, string value, bool first = false)
        {
            if (!first) sb.Append(',');
            sb.Append(TryllJsonWriter.Quote(key)).Append(':').Append(TryllJsonWriter.Quote(value ?? ""));
        }

        static void Kv(StringBuilder sb, string key, bool value)
        {
            sb.Append(',').Append(TryllJsonWriter.Quote(key)).Append(':').Append(value ? "true" : "false");
        }

        static void Kv(StringBuilder sb, string key, int value)
        {
            sb.Append(',').Append(TryllJsonWriter.Quote(key)).Append(':').Append(value.ToString(CultureInfo.InvariantCulture));
        }

        static void Kv(StringBuilder sb, string key, float value)
        {
            sb.Append(',').Append(TryllJsonWriter.Quote(key)).Append(':').Append(value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
