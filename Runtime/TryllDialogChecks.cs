// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// One deterministic Dialog Lab assertion on a script step (or, in
    /// <see cref="TryllDialogScriptAsset.GlobalChecks"/>, on every generated turn).
    /// Pure data: evaluation lives in the editor (<c>TryllDialogLabChecks</c>).
    ///
    /// Subclass names are part of the asset format — <c>[SerializeReference]</c> stores
    /// the type name in the YAML — so renaming one needs <c>[MovedFrom]</c>.
    /// <see cref="TypeId"/> is the stable name the JSONL export uses instead.
    /// </summary>
    [Serializable]
    public abstract class TryllDialogCheck
    {
        /// <summary>Off = kept in the asset but not evaluated, shown, or counted.</summary>
        public bool Enabled = true;

        /// <summary>Optional author label; replaces the generated label in the Lab.</summary>
        public string Note = "";

        /// <summary>Stable snake_case id written to and read from the JSONL export.</summary>
        public abstract string TypeId { get; }

        /// <summary>Every field is a value or a string, so a shallow copy is a deep one.</summary>
        public TryllDialogCheck Clone() => (TryllDialogCheck)MemberwiseClone();

        public const string TypeMustContain = "must_contain";
        public const string TypeMustNotContain = "must_not_contain";
        public const string TypeRegexForbid = "regex_forbid";
        public const string TypeRegexMatch = "regex_match";
        public const string TypeWordCount = "word_count";
        public const string TypeIntentIs = "intent_is";
        public const string TypeIntentNotFound = "intent_not_found";
        public const string TypeAttributeEquals = "attribute_equals";
        public const string TypeAttributeInRange = "attribute_in_range";
        public const string TypeCallsTool = "calls_tool";
        public const string TypeCallsNoTool = "calls_no_tool";

        /// <summary>A blank check for <paramref name="typeId"/>, or null for an unknown id.</summary>
        public static TryllDialogCheck Create(string typeId)
        {
            switch (typeId)
            {
                case TypeMustContain: return new TryllMustContainCheck();
                case TypeMustNotContain: return new TryllMustNotContainCheck();
                case TypeRegexForbid: return new TryllRegexForbidCheck();
                case TypeRegexMatch: return new TryllRegexMatchCheck();
                case TypeWordCount: return new TryllWordCountCheck();
                case TypeIntentIs: return new TryllIntentIsCheck();
                case TypeIntentNotFound: return new TryllIntentNotFoundCheck();
                case TypeAttributeEquals: return new TryllAttributeEqualsCheck();
                case TypeAttributeInRange: return new TryllAttributeInRangeCheck();
                case TypeCallsTool: return new TryllCallsToolCheck();
                case TypeCallsNoTool: return new TryllCallsNoToolCheck();
                default: return null;
            }
        }
    }

    /// <summary>The answer contains <see cref="Text"/> (case-insensitive by default).</summary>
    [Serializable, AddTypeMenu("Text/Must contain", 0)]
    public sealed class TryllMustContainCheck : TryllDialogCheck
    {
        public string Text = "";
        public bool CaseSensitive;
        public override string TypeId => TypeMustContain;
    }

    /// <summary>The answer does not contain <see cref="Text"/> (case-insensitive by default).</summary>
    [Serializable, AddTypeMenu("Text/Must not contain", 1)]
    public sealed class TryllMustNotContainCheck : TryllDialogCheck
    {
        public string Text = "";
        public bool CaseSensitive;
        public override string TypeId => TypeMustNotContain;
    }

    /// <summary>The answer does not match <see cref="Pattern"/> (.NET regex).</summary>
    [Serializable, AddTypeMenu("Regex/Forbid", 10)]
    public sealed class TryllRegexForbidCheck : TryllDialogCheck
    {
        public string Pattern = "";
        public override string TypeId => TypeRegexForbid;
    }

    /// <summary>The answer matches <see cref="Pattern"/> (.NET regex).</summary>
    [Serializable, AddTypeMenu("Regex/Match", 11)]
    public sealed class TryllRegexMatchCheck : TryllDialogCheck
    {
        public string Pattern = "";
        public override string TypeId => TypeRegexMatch;
    }

    /// <summary>Whitespace-delimited word count within [Min, Max]; 0 leaves a bound unchecked.</summary>
    [Serializable, AddTypeMenu("Length/Word count", 20)]
    public sealed class TryllWordCountCheck : TryllDialogCheck
    {
        [Min(0)] public int Min;
        [Min(0)] public int Max;
        public override string TypeId => TypeWordCount;
    }

    /// <summary>An addressed classifier accepted <see cref="Intent"/>. Empty Node = any classifier.</summary>
    [Serializable, AddTypeMenu("Intent/Intent is", 30)]
    public sealed class TryllIntentIsCheck : TryllDialogCheck
    {
        public string Node = "";
        public string Intent = "";
        public override string TypeId => TypeIntentIs;
    }

    /// <summary>Every addressed classifier took not_found. Empty Node = any classifier.</summary>
    [Serializable, AddTypeMenu("Intent/Intent not found", 31)]
    public sealed class TryllIntentNotFoundCheck : TryllDialogCheck
    {
        public string Node = "";
        public override string TypeId => TypeIntentNotFound;
    }

    /// <summary>A ClassifyAttributesLLM slot equals <see cref="Value"/> exactly.</summary>
    [Serializable, AddTypeMenu("Attribute/Attribute equals", 40)]
    public sealed class TryllAttributeEqualsCheck : TryllDialogCheck
    {
        public string Node = "";
        public string Name = "";
        public string Value = "";
        public override string TypeId => TypeAttributeEquals;
    }

    /// <summary>A ClassifyAttributesLLM score lies in [Min, Max], inclusive.</summary>
    [Serializable, AddTypeMenu("Attribute/Attribute in range", 41)]
    public sealed class TryllAttributeInRangeCheck : TryllDialogCheck
    {
        public string Node = "";
        public string Name = "";
        public float Min = 0f;
        public float Max = 1f;
        public override string TypeId => TypeAttributeInRange;
    }

    /// <summary>The turn called <see cref="ToolName"/> at least once.</summary>
    [Serializable, AddTypeMenu("Tools/Calls tool", 50)]
    public sealed class TryllCallsToolCheck : TryllDialogCheck
    {
        public string ToolName = "";
        public override string TypeId => TypeCallsTool;
    }

    /// <summary>
    /// The turn called no tool at all — canned and live calls included. Stricter than
    /// the Lab's built-in "no unexpected tool call" gate, which only fails calls that
    /// had no canned result.
    /// </summary>
    [Serializable, AddTypeMenu("Tools/Calls no tool", 51)]
    public sealed class TryllCallsNoToolCheck : TryllDialogCheck
    {
        public override string TypeId => TypeCallsNoTool;
    }

    /// <summary>
    /// Converts the pre-typed-check step fields (MustContain, MaxWords, IntentChecks, …)
    /// into typed checks. Shared by asset load (<see cref="TryllDialogScriptAsset"/>) and
    /// the v3 JSONL import, so both produce the same checks in the same order.
    /// </summary>
    public static class TryllDialogCheckMigration
    {
        /// <summary>
        /// <paramref name="generated"/>: a canned tool result on a generated step used to
        /// assert "must call that tool" implicitly; that becomes an explicit
        /// <see cref="TryllCallsToolCheck"/>. Seeded steps never ran tools, so get none.
        /// </summary>
        public static List<TryllDialogCheck> FromLegacyStep(TryllDialogScriptAsset.Step step, bool generated)
        {
            var checks = new List<TryllDialogCheck>();
            if (step == null) return checks;
            foreach (var s in step.MustContain ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(s)) checks.Add(new TryllMustContainCheck { Text = s });
            foreach (var s in step.MustNotContain ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(s)) checks.Add(new TryllMustNotContainCheck { Text = s });
            foreach (var s in step.RegexForbid ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(s)) checks.Add(new TryllRegexForbidCheck { Pattern = s });
            if (step.MaxWords > 0)
                checks.Add(new TryllWordCountCheck { Max = step.MaxWords });
            foreach (var c in step.IntentChecks ?? new List<TryllDialogScriptAsset.IntentCheck>())
            {
                if (c.ExpectNotFound) checks.Add(new TryllIntentNotFoundCheck { Node = c.Node ?? "" });
                else checks.Add(new TryllIntentIsCheck { Node = c.Node ?? "", Intent = c.Intent ?? "" });
            }
            foreach (var c in step.AttributeChecks ?? new List<TryllDialogScriptAsset.AttributeCheck>())
            {
                if (c.ScoreRange)
                    checks.Add(new TryllAttributeInRangeCheck { Node = c.Node ?? "", Name = c.Name ?? "", Min = c.ScoreMin, Max = c.ScoreMax });
                else
                    checks.Add(new TryllAttributeEqualsCheck { Node = c.Node ?? "", Name = c.Name ?? "", Value = c.Value ?? "" });
            }
            if (generated)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var t in step.ToolResults ?? new List<TryllDialogScriptAsset.CannedToolResult>())
                    if (!string.IsNullOrEmpty(t.ToolName) && seen.Add(t.ToolName))
                        checks.Add(new TryllCallsToolCheck { ToolName = t.ToolName });
            }
            return checks;
        }

        public static List<TryllDialogCheck> FromLegacyGlobal(IEnumerable<string> globalRegexForbid)
        {
            var checks = new List<TryllDialogCheck>();
            foreach (var s in globalRegexForbid ?? Array.Empty<string>())
                if (!string.IsNullOrEmpty(s)) checks.Add(new TryllRegexForbidCheck { Pattern = s });
            return checks;
        }

        public static bool HasLegacyFields(TryllDialogScriptAsset.Step step) =>
            step != null
            && ((step.MustContain?.Length ?? 0) > 0
                || (step.MustNotContain?.Length ?? 0) > 0
                || (step.RegexForbid?.Length ?? 0) > 0
                || step.MaxWords > 0
                || (step.IntentChecks?.Count ?? 0) > 0
                || (step.AttributeChecks?.Count ?? 0) > 0);

        public static void ClearLegacyFields(TryllDialogScriptAsset.Step step)
        {
            step.MustContain = Array.Empty<string>();
            step.MustNotContain = Array.Empty<string>();
            step.RegexForbid = Array.Empty<string>();
            step.MaxWords = 0;
            step.IntentChecks = new List<TryllDialogScriptAsset.IntentCheck>();
            step.AttributeChecks = new List<TryllDialogScriptAsset.AttributeCheck>();
        }
    }
}
