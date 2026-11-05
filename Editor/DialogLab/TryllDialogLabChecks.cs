// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tryll.Client.Editor
{
    /// <summary>Where an outcome's check lives: on the step, in the script's
    /// <see cref="TryllDialogScriptData.GlobalChecks"/>, or the Lab's built-in
    /// "no unexpected tool call" gate.</summary>
    internal enum TryllLabCheckScope { Step, Global, Gate }

    /// <summary>
    /// One evaluated check on one turn. Identity is <see cref="Scope"/> + <see cref="Index"/>
    /// (the check's position in its list), so two checks of the same type and argument
    /// are evaluated and reported separately. <see cref="Kind"/> says <i>how</i> it
    /// passed or failed (e.g. <c>intent_unavailable</c> for an Intent is check whose
    /// classifier never ran); <see cref="CheckType"/> says which check type it was.
    /// </summary>
    internal sealed class TryllLabCheckOutcome
    {
        public TryllLabCheckScope Scope;
        /// <summary>Position in the step's or the script's check list; 0 for the gate;
        /// -1 when an imported failure could not be tied to a check.</summary>
        public int Index;
        public string CheckType = "";
        public string Kind;   // must_contain | must_not_contain | regex_forbid | regex_match | invalid_regex | word_count | intent_* | attribute_* | tool_call
        public string Arg;
        public bool Passed;
        public string Actual; // optional: e.g. word count when Kind == word_count
        public string Label;  // human chip text only
    }

    /// <summary>Deterministic Dialog Lab checks — no model involved.</summary>
    internal static class TryllDialogLabChecks
    {
        public const string KindMustContain = "must_contain";
        public const string KindMustNotContain = "must_not_contain";
        public const string KindRegexForbid = "regex_forbid";
        public const string KindRegexMatch = "regex_match";
        public const string KindWordCount = "word_count";
        /// <summary>Pre-typed-check kind; only the v3 import still reads it.</summary>
        public const string KindMaxWords = "max_words";
        public const string KindInvalidRegex = "invalid_regex";
        public const string KindIntentIs = "intent_is";
        public const string KindIntentNotFound = "intent_not_found";
        public const string KindIntentUnavailable = "intent_unavailable";
        public const string KindAttributeIs = "attribute_is";
        public const string KindAttributeScore = "attribute_score";
        public const string KindAttributeUnavailable = "attribute_unavailable";
        public const string KindToolCall = "tool_call";
        /// <summary>Gate arg when the step has canned results: "must not call other tools".</summary>
        public const string ToolCallOtherArg = "*";
        /// <summary><see cref="TryllLabCheckOutcome.CheckType"/> of the built-in gate.</summary>
        public const string GateUnexpectedToolCall = "unexpected_tool_call";

        public static string AttributeArg(string node, string name, string value) =>
            (node ?? "") + "|" + (name ?? "") + "|" + (value ?? "");

        public static string AttributeScoreArg(string node, string name, float min, float max) =>
            (node ?? "") + "|" + (name ?? "") + "|"
            + min.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "|"
            + max.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);

        public static bool TryParseAttributeArg(string arg, out string node, out string name, out string rest)
        {
            node = "";
            name = "";
            rest = arg ?? "";
            var parts = (arg ?? "").Split(new[] { '|' }, 3);
            if (parts.Length < 3) return false;
            node = parts[0];
            name = parts[1];
            rest = parts[2];
            return true;
        }

        public static string IntentArg(string node, string intent)
        {
            node = node ?? "";
            intent = intent ?? "";
            return node.Length == 0 ? intent : node + "|" + intent;
        }

        public static bool TryParseIntentArg(string arg, out string node, out string intent)
        {
            node = "";
            intent = arg ?? "";
            if (string.IsNullOrEmpty(arg)) return true;
            int bar = arg.IndexOf('|');
            if (bar < 0) return true;
            node = arg.Substring(0, bar);
            intent = arg.Substring(bar + 1);
            return true;
        }

        public static string WordCountArg(int min, int max) => min + "|" + max;

        public static string LabelFor(string kind, string arg, string actual = null)
        {
            if (kind == KindMustContain) return $"must contain \"{arg}\"";
            if (kind == KindMustNotContain) return $"must not contain \"{arg}\"";
            if (kind == KindRegexForbid) return $"forbidden /{arg}/";
            if (kind == KindRegexMatch) return $"must match /{arg}/";
            if (kind == KindInvalidRegex) return $"invalid regex /{arg}/";
            if (kind == KindMaxWords)
            {
                return string.IsNullOrEmpty(actual)
                    ? $"≤ {arg} words"
                    : $"≤ {arg} words (got {actual})";
            }
            if (kind == KindWordCount)
            {
                var parts = (arg ?? "").Split('|');
                int.TryParse(parts.Length > 0 ? parts[0] : "", out int min);
                int.TryParse(parts.Length > 1 ? parts[1] : "", out int max);
                string range = min > 0 && max > 0 ? $"{min}–{max} words"
                             : max > 0 ? $"≤ {max} words"
                             : $"≥ {min} words";
                return string.IsNullOrEmpty(actual) ? range : $"{range} (got {actual})";
            }
            if (kind == KindIntentIs)
            {
                TryParseIntentArg(arg, out var node, out var intent);
                string baseLabel = string.IsNullOrEmpty(node)
                    ? $"intent is \"{intent}\""
                    : $"{node}: intent is \"{intent}\"";
                return string.IsNullOrEmpty(actual) ? baseLabel : baseLabel + " (" + actual + ")";
            }
            if (kind == KindIntentNotFound)
            {
                string node = arg ?? "";
                string baseLabel = string.IsNullOrEmpty(node)
                    ? "intent not found"
                    : node + ": intent not found";
                return string.IsNullOrEmpty(actual) ? baseLabel : baseLabel + " (" + actual + ")";
            }
            if (kind == KindIntentUnavailable)
                return string.IsNullOrEmpty(actual) ? "intent unavailable" : "intent unavailable (" + actual + ")";
            if (kind == KindAttributeIs)
            {
                TryParseAttributeArg(arg, out var node, out var name, out var value);
                string baseLabel = string.IsNullOrEmpty(node)
                    ? $"{name} is \"{value}\""
                    : $"{node}: {name} is \"{value}\"";
                return string.IsNullOrEmpty(actual) ? baseLabel : baseLabel + " (" + actual + ")";
            }
            if (kind == KindAttributeScore)
            {
                TryParseAttributeArg(arg, out var node, out var name, out var range);
                string who = string.IsNullOrEmpty(node) ? name : node + ": " + name;
                string baseLabel = who + " in " + range;
                return string.IsNullOrEmpty(actual) ? baseLabel : baseLabel + " (" + actual + ")";
            }
            if (kind == KindAttributeUnavailable)
                return string.IsNullOrEmpty(actual) ? "attribute unavailable" : "attribute unavailable (" + actual + ")";
            if (kind == KindToolCall)
            {
                string baseLabel = arg == ToolCallOtherArg
                    ? "must not call other tools"
                    : string.IsNullOrEmpty(arg)
                        ? "must not call a tool"
                        : "must call " + arg;
                return string.IsNullOrEmpty(actual) ? baseLabel : baseLabel + " (" + actual + ")";
            }
            return kind;
        }

        /// <summary>
        /// The kind and argument a check reports under when it passes (and, for most
        /// checks, when it fails). Feeds <see cref="LabelFor"/>, the export, and the v3
        /// import's failure matching.
        /// </summary>
        public static (string kind, string arg) Identity(TryllDialogCheck check)
        {
            switch (check)
            {
                case TryllMustContainCheck c: return (KindMustContain, c.Text ?? "");
                case TryllMustNotContainCheck c: return (KindMustNotContain, c.Text ?? "");
                case TryllRegexForbidCheck c: return (KindRegexForbid, c.Pattern ?? "");
                case TryllRegexMatchCheck c: return (KindRegexMatch, c.Pattern ?? "");
                case TryllWordCountCheck c: return (KindWordCount, WordCountArg(c.Min, c.Max));
                case TryllIntentIsCheck c: return (KindIntentIs, IntentArg(c.Node, c.Intent));
                case TryllIntentNotFoundCheck c: return (KindIntentNotFound, c.Node ?? "");
                case TryllAttributeEqualsCheck c: return (KindAttributeIs, AttributeArg(c.Node, c.Name, c.Value));
                case TryllAttributeInRangeCheck c: return (KindAttributeScore, AttributeScoreArg(c.Node, c.Name, c.Min, c.Max));
                case TryllCallsToolCheck c: return (KindToolCall, c.ToolName ?? "");
                case TryllCallsNoToolCheck _: return (KindToolCall, "");
                default: return (check?.TypeId ?? "", "");
            }
        }

        /// <summary>The label an authored row shows: the author's Note when set.</summary>
        public static string AuthoredLabel(TryllDialogCheck check)
        {
            if (!string.IsNullOrEmpty(check?.Note)) return check.Note;
            var (kind, arg) = Identity(check);
            return LabelFor(kind, arg);
        }

        /// <summary>
        /// Whether a check runs on a turn of this kind. Tool checks need the graph to have
        /// run; global checks skip seeded turns (author-written text, not model output);
        /// a disabled check never runs; a check with nothing to compare (a blank text or
        /// pattern, a word count with no bound) checks nothing — a blank forbidden
        /// pattern would otherwise match, and fail, every answer.
        /// </summary>
        public static bool AppliesTo(TryllDialogCheck check, TryllLabCheckScope scope, bool seeded)
        {
            if (check == null || !check.Enabled) return false;
            if (seeded && scope == TryllLabCheckScope.Global) return false;
            if (seeded && (check is TryllCallsToolCheck || check is TryllCallsNoToolCheck)) return false;
            switch (check)
            {
                case TryllMustContainCheck c: return !string.IsNullOrEmpty(c.Text);
                case TryllMustNotContainCheck c: return !string.IsNullOrEmpty(c.Text);
                case TryllRegexForbidCheck c: return !string.IsNullOrEmpty(c.Pattern);
                case TryllRegexMatchCheck c: return !string.IsNullOrEmpty(c.Pattern);
                case TryllWordCountCheck c: return c.Min > 0 || c.Max > 0;
                default: return true;
            }
        }

        /// <summary>
        /// The rows the inspector lists for one turn, in evaluation order: step checks,
        /// global checks, then the gate. Must describe exactly the set
        /// <see cref="EvaluateStep"/> evaluates, or a failure has no row to land on.
        /// </summary>
        public static List<LabCheck> AuthoredChecks(TryllDialogScriptData script, TryllDialogStepData step, bool seeded)
        {
            var rows = new List<LabCheck>();
            var stepChecks = step?.Checks ?? new List<TryllDialogCheck>();
            for (int i = 0; i < stepChecks.Count; i++)
                if (AppliesTo(stepChecks[i], TryllLabCheckScope.Step, seeded))
                    rows.Add(Row(TryllLabCheckScope.Step, i, stepChecks[i]));
            var globals = script?.GlobalChecks ?? new List<TryllDialogCheck>();
            for (int i = 0; i < globals.Count; i++)
                if (AppliesTo(globals[i], TryllLabCheckScope.Global, seeded))
                    rows.Add(Row(TryllLabCheckScope.Global, i, globals[i]));
            if (!seeded)
            {
                string arg = CannedToolNames(step).Count == 0 ? "" : ToolCallOtherArg;
                rows.Add(new LabCheck
                {
                    Scope = TryllLabCheckScope.Gate,
                    Index = 0,
                    Kind = KindToolCall,
                    Arg = arg,
                    Label = LabelFor(KindToolCall, arg),
                });
            }
            return rows;
        }

        static LabCheck Row(TryllLabCheckScope scope, int index, TryllDialogCheck check)
        {
            var (kind, arg) = Identity(check);
            return new LabCheck { Scope = scope, Index = index, Kind = kind, Arg = arg, Label = AuthoredLabel(check) };
        }

        /// <summary>Does this outcome belong to that authored row? By position, never by label.</summary>
        public static bool Matches(TryllLabCheckOutcome outcome, LabCheck authored) =>
            outcome != null && authored != null
            && outcome.Scope == authored.Scope
            && outcome.Index == authored.Index;

        public static int CountWords(string text) =>
            string.IsNullOrWhiteSpace(text)
                ? 0
                : text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;

        /// <summary>
        /// Evaluate one turn: the step's checks, the script's global checks (generated
        /// turns only), and the "no unexpected tool call" gate (generated turns only).
        /// Returns every outcome, passes included. Takes the script as well as the step
        /// because global checks live on the script — there is deliberately no overload
        /// without diagnostics, which would silently skip every intent check.
        /// <paramref name="useLiveTools"/> lets the gate accept a call a live handler
        /// answered; names in <paramref name="missingResumedTools"/> were empty-resumed
        /// and still fail it.
        /// </summary>
        public static (TryllLabCellStatus status, List<TryllLabCheckOutcome> outcomes) EvaluateStep(
            TryllDialogScriptData script,
            TryllDialogStepData step,
            string answerText,
            TryllJsonValue debugInfo,
            TryllDiagnosticsAbsenceReason absence,
            bool useLiveTools = false,
            IEnumerable<string> missingResumedTools = null)
        {
            bool seeded = absence == TryllDiagnosticsAbsenceReason.Seeded;
            var ctx = new TurnContext(answerText, debugInfo, absence);
            var outcomes = new List<TryllLabCheckOutcome>();

            var stepChecks = step?.Checks ?? new List<TryllDialogCheck>();
            for (int i = 0; i < stepChecks.Count; i++)
                if (AppliesTo(stepChecks[i], TryllLabCheckScope.Step, seeded))
                    outcomes.Add(At(EvaluateOne(stepChecks[i], ctx), TryllLabCheckScope.Step, i));

            var globals = script?.GlobalChecks ?? new List<TryllDialogCheck>();
            for (int i = 0; i < globals.Count; i++)
                if (AppliesTo(globals[i], TryllLabCheckScope.Global, seeded))
                    outcomes.Add(At(EvaluateOne(globals[i], ctx), TryllLabCheckScope.Global, i));

            if (!seeded)
                outcomes.Add(EvaluateGate(step, ctx, useLiveTools, missingResumedTools));

            var status = outcomes.Any(o => !o.Passed) ? TryllLabCellStatus.CheckFail : TryllLabCellStatus.Ok;
            return (status, outcomes);
        }

        /// <summary>
        /// One check against plain text, for the types that need nothing else (contain,
        /// regex, word count); null for the rest, which need a turn's diagnostics.
        /// </summary>
        public static TryllLabCheckOutcome EvaluateTextOnly(TryllDialogCheck check, string text)
        {
            if (!(check is TryllMustContainCheck || check is TryllMustNotContainCheck
                  || check is TryllRegexForbidCheck || check is TryllRegexMatchCheck
                  || check is TryllWordCountCheck))
                return null;
            return EvaluateOne(check, new TurnContext(text, TryllJsonValue.Null, TryllDiagnosticsAbsenceReason.Seeded));
        }

        sealed class TurnContext
        {
            public readonly string Text;
            public readonly string Lower;
            public readonly int Words;
            public readonly TryllJsonValue DebugInfo;
            public readonly TryllDiagnosticsAbsenceReason Absence;
            public readonly List<string> ActualTools;

            public TurnContext(string text, TryllJsonValue debugInfo, TryllDiagnosticsAbsenceReason absence)
            {
                Text = text ?? "";
                Lower = Text.ToLowerInvariant();
                Words = CountWords(Text);
                DebugInfo = debugInfo ?? TryllJsonValue.Null;
                Absence = absence;
                ActualTools = ActualToolNames(DebugInfo);
            }

            public bool HasDiagnostics => Absence == TryllDiagnosticsAbsenceReason.None && !DebugInfo.IsNull;
        }

        static TryllLabCheckOutcome At(TryllLabCheckOutcome o, TryllLabCheckScope scope, int index)
        {
            o.Scope = scope;
            o.Index = index;
            return o;
        }

        static TryllLabCheckOutcome EvaluateOne(TryllDialogCheck check, TurnContext ctx)
        {
            var (kind, arg) = Identity(check);
            switch (check)
            {
                case TryllMustContainCheck c:
                    return Result(check, kind, arg, Contains(ctx, c.Text, c.CaseSensitive));
                case TryllMustNotContainCheck c:
                    return Result(check, kind, arg, !Contains(ctx, c.Text, c.CaseSensitive));
                case TryllRegexForbidCheck c:
                    return EvaluateRegex(check, kind, arg, ctx, c.Pattern, forbid: true);
                case TryllRegexMatchCheck c:
                    return EvaluateRegex(check, kind, arg, ctx, c.Pattern, forbid: false);
                case TryllWordCountCheck c:
                {
                    bool ok = (c.Max <= 0 || ctx.Words <= c.Max) && (c.Min <= 0 || ctx.Words >= c.Min);
                    return Result(check, kind, arg, ok, ok ? null : ctx.Words.ToString());
                }
                case TryllIntentIsCheck c:
                    return EvaluateIntent(check, kind, arg, ctx, c.Node, c.Intent, expectNotFound: false);
                case TryllIntentNotFoundCheck c:
                    return EvaluateIntent(check, kind, arg, ctx, c.Node, null, expectNotFound: true);
                case TryllAttributeEqualsCheck c:
                    return EvaluateAttribute(check, kind, arg, ctx, c.Node, c.Name, value =>
                        string.Equals(value, c.Value ?? "", StringComparison.Ordinal));
                case TryllAttributeInRangeCheck c:
                    return EvaluateAttribute(check, kind, arg, ctx, c.Node, c.Name, value =>
                        float.TryParse(value, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var actual)
                        && actual >= c.Min && actual <= c.Max);
                case TryllCallsToolCheck c:
                {
                    bool called = ctx.ActualTools.Contains(c.ToolName ?? "");
                    return Result(check, kind, arg, called, called ? null : "not called");
                }
                case TryllCallsNoToolCheck _:
                {
                    bool none = ctx.ActualTools.Count == 0;
                    return Result(check, kind, arg, none, none ? null : "got " + string.Join(", ", ctx.ActualTools));
                }
                default:
                    return Result(check, kind, arg, false, "unknown check type");
            }
        }

        static bool Contains(TurnContext ctx, string needle, bool caseSensitive)
        {
            needle = needle ?? "";
            return caseSensitive
                ? ctx.Text.IndexOf(needle, StringComparison.Ordinal) >= 0
                : ctx.Lower.Contains(needle.ToLowerInvariant());
        }

        static TryllLabCheckOutcome EvaluateRegex(
            TryllDialogCheck check, string kind, string arg, TurnContext ctx, string pattern, bool forbid)
        {
            try
            {
                bool match = Regex.IsMatch(ctx.Text, pattern ?? "");
                return Result(check, kind, arg, forbid ? !match : match);
            }
            catch (ArgumentException)
            {
                // A malformed pattern fails rather than passing silently, and says why.
                return Result(check, KindInvalidRegex, arg, false);
            }
        }

        /// <summary>
        /// The Lab's built-in assertion on every generated turn: a call to a tool with no
        /// canned result on this step fails, unless the variant uses live tools and the
        /// live handler answered it. Not an authored check, so statistics leave it out of
        /// the check denominator, but it still fails the turn.
        /// </summary>
        static TryllLabCheckOutcome EvaluateGate(
            TryllDialogStepData step, TurnContext ctx, bool useLiveTools, IEnumerable<string> missingResumedTools)
        {
            var canned = CannedToolNames(step);
            var missingResumed = new HashSet<string>(
                missingResumedTools ?? Array.Empty<string>(), StringComparer.Ordinal);
            var violations = ctx.ActualTools
                .Where(n => !canned.Contains(n) && (!useLiveTools || missingResumed.Contains(n)))
                .ToList();
            string arg = canned.Count == 0 ? "" : ToolCallOtherArg;
            string actual = violations.Count == 0
                ? null
                : (canned.Count == 0 ? "got " : "unexpected ") + string.Join(", ", violations);
            return new TryllLabCheckOutcome
            {
                Scope = TryllLabCheckScope.Gate,
                Index = 0,
                CheckType = GateUnexpectedToolCall,
                Kind = KindToolCall,
                Arg = arg,
                Passed = violations.Count == 0,
                Actual = actual,
                Label = LabelFor(KindToolCall, arg, actual),
            };
        }

        static List<string> CannedToolNames(TryllDialogStepData step)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var result in step?.ToolResults ?? new List<TryllCannedToolResultData>())
            {
                if (result == null || string.IsNullOrEmpty(result.ToolName)) continue;
                if (seen.Add(result.ToolName)) names.Add(result.ToolName);
            }
            return names;
        }

        static TryllLabCheckOutcome EvaluateAttribute(
            TryllDialogCheck check, string kind, string arg, TurnContext ctx,
            string node, string name, Func<string, bool> accept)
        {
            if (!ctx.HasDiagnostics)
                return Result(check, KindAttributeUnavailable, arg, false, "diagnostics absent");
            if (!TryFindAttribute(ctx.DebugInfo, node, name, out var value, out var unavail))
                return Result(check, KindAttributeUnavailable, arg, false, unavail);
            bool ok = accept(value);
            return Result(check, kind, arg, ok, ok ? null : "got " + value);
        }

        static TryllLabCheckOutcome EvaluateIntent(
            TryllDialogCheck check, string kind, string arg, TurnContext ctx,
            string node, string intent, bool expectNotFound)
        {
            if (!ctx.HasDiagnostics)
                return Result(check, KindIntentUnavailable, arg, false, AbsenceActual(ctx.Absence));

            if (!TryAddress(ctx.DebugInfo, node, out var addressed, out var unavail))
                return Result(check, KindIntentUnavailable, arg, false, unavail);

            if (expectNotFound)
            {
                TryllIntentExecution accepted = null;
                foreach (var e in addressed)
                {
                    if (e.Accepted) { accepted = e; break; }
                }
                if (accepted == null) return Result(check, kind, arg, true);
                string got = TryllDialogLabIntent.ResolveLabel(accepted);
                string dist = accepted.Distance.HasValue
                    ? " @ " + TryllDialogLabIntent.FormatDistance(accepted.Distance)
                    : accepted.TopProb.HasValue
                        ? " @ " + TryllDialogLabIntent.FormatProb(accepted.TopProb)
                        : "";
                return Result(check, kind, arg, false, "got " + (string.IsNullOrEmpty(got) ? "found" : got) + dist);
            }

            TryllIntentExecution last = null;
            foreach (var e in addressed)
            {
                last = e;
                if (e.Accepted && string.Equals(
                        TryllDialogLabIntent.ResolveLabel(e), intent ?? "",
                        StringComparison.Ordinal))
                    return Result(check, kind, arg, true);
            }
            return Result(check, kind, arg, false, IntentIsActual(last, intent));
        }

        /// <summary>A check's outcome. A failing check with an author Note keeps the Note
        /// and appends what actually happened, so the chip stays recognisable.</summary>
        private static TryllLabCheckOutcome Result(
            TryllDialogCheck check, string kind, string arg, bool passed, string actual = null)
        {
            string label = string.IsNullOrEmpty(check?.Note)
                ? LabelFor(kind, arg ?? "", actual)
                : string.IsNullOrEmpty(actual) ? check.Note : check.Note + " (" + actual + ")";
            return new TryllLabCheckOutcome
            {
                CheckType = check?.TypeId ?? "",
                Kind = kind,
                Arg = arg ?? "",
                Passed = passed,
                Actual = actual,
                Label = label,
            };
        }

        // ── Diagnostics readers (unchanged from the field-based evaluator) ─────────

        static List<string> ActualToolNames(TryllJsonValue debugInfo)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (debugInfo == null || debugInfo.IsNull) return names;
            foreach (var call in debugInfo.Get("tool_calls").AsArray())
            {
                string name = call.Get("name").AsString("");
                if (string.IsNullOrEmpty(name)) continue;
                if (seen.Add(name)) names.Add(name);
            }
            return names;
        }

        /// <summary>Diagnostics type <c>value</c> by kind: a string for Choice, a bool for
        /// Boolean, a number for Score. Render each the way the attribute's slot does.</summary>
        static string AttributeValueText(TryllJsonValue value)
        {
            switch (value.Kind)
            {
                case TryllJsonKind.String: return value.String ?? "";
                case TryllJsonKind.Bool: return value.Bool ? "true" : "false";
                case TryllJsonKind.Number:
                    return value.Number.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
                default: return "";
            }
        }

        static bool TryFindAttribute(
            TryllJsonValue debugInfo,
            string nodeName,
            string attributeName,
            out string value,
            out string unavailable)
        {
            value = "";
            unavailable = "attribute \"" + (attributeName ?? "") + "\" did not run";
            bool sawNode = false;
            foreach (var node in debugInfo.Get("nodes").AsArray())
            {
                if (node == null || node.IsNull) continue;
                string name = node.Get("name").AsString("");
                if (!string.IsNullOrEmpty(nodeName) && !string.Equals(name, nodeName, StringComparison.Ordinal))
                    continue;
                string type = node.Get("_type").AsString("");
                if (!string.IsNullOrEmpty(nodeName) && type != "ClassifyAttributesLLMNode")
                {
                    unavailable = "node \"" + nodeName + "\" is a " + (string.IsNullOrEmpty(type) ? "unknown node" : type);
                    return false;
                }
                if (type != "ClassifyAttributesLLMNode") continue;
                sawNode = true;
                var attributes = node.Get("diagnostics").Get("output").Get("classification").Get("attributes").AsArray();
                foreach (var attribute in attributes)
                {
                    if (!string.Equals(attribute.Get("name").AsString(""), attributeName ?? "", StringComparison.Ordinal))
                        continue;
                    value = AttributeValueText(attribute.Get("value"));
                    return true;
                }
            }
            if (!sawNode && !string.IsNullOrEmpty(nodeName))
                unavailable = "node \"" + nodeName + "\" did not run";
            return false;
        }

        static bool TryAddress(
            TryllJsonValue debugInfo,
            string node,
            out List<TryllIntentExecution> addressed,
            out string unavailableActual)
        {
            addressed = new List<TryllIntentExecution>();
            unavailableActual = "";
            var nodes = debugInfo.Get("nodes").AsArray();
            node = node ?? "";

            if (node.Length > 0)
            {
                bool any = false;
                for (int i = 0; i < nodes.Count; i++)
                {
                    var n = nodes[i];
                    if (n.Get("name").AsString("") != node) continue;
                    any = true;
                    var type = n.Get("_type").AsString("");
                    if (!TryllDialogLabIntent.IsClassifierType(type))
                    {
                        unavailableActual = "node \"" + node + "\" is a " +
                            (string.IsNullOrEmpty(type) ? "unknown node" : type);
                        addressed.Clear();
                        return false;
                    }
                    addressed.Add(TryllDialogLabIntent.ParseOne(i, n));
                }
                if (!any)
                {
                    unavailableActual = "no classifier ran this turn";
                    return false;
                }
                return true;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (!TryllDialogLabIntent.IsClassifierType(n.Get("_type").AsString("")))
                    continue;
                addressed.Add(TryllDialogLabIntent.ParseOne(i, n));
            }
            if (addressed.Count == 0)
            {
                unavailableActual = "no classifier ran this turn";
                return false;
            }
            return true;
        }

        static string AbsenceActual(TryllDiagnosticsAbsenceReason absence) =>
            absence == TryllDiagnosticsAbsenceReason.None
                ? "diagnostics absent"
                : "diagnostics absent: " + absence;

        static string IntentIsActual(TryllIntentExecution e, string expected)
        {
            if (e == null) return "got nothing";
            if (!e.Accepted)
            {
                var rejected = TryllDialogLabIntent.ResolveLabel(e);
                var reason = string.IsNullOrEmpty(e.NotFoundReason) ? "not_found" : e.NotFoundReason;
                if (string.IsNullOrEmpty(rejected))
                    return "got not_found: " + reason;
                return "got not_found: " + reason + ", top-1 \"" + rejected + "\""
                    + (e.Distance.HasValue
                        ? " @ " + TryllDialogLabIntent.FormatDistance(e.Distance)
                        : "");
            }
            var got = TryllDialogLabIntent.ResolveLabel(e);
            return "got " + (string.IsNullOrEmpty(got) ? "found" : got);
        }
    }
}
