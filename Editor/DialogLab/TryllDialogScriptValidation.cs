// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;

namespace Tryll.Client.Editor
{
    /// <summary>Pre-run validation for dialog scripts — shared by the Lab window and tests.</summary>
    internal static class TryllDialogScriptValidation
    {
        public static bool TryValidate(TryllDialogScriptAsset script, out string error) =>
            TryValidate(TryllDialogScriptData.FromAsset(script), out error);

        public static bool TryValidate(TryllDialogScriptData script, out string error)
        {
            error = null;
            if (script == null)
            {
                error = "Assign a Dialog Script.";
                return false;
            }

            var steps = script.Steps ?? new List<TryllDialogStepData>();
            if (script.SeedTurnCount < 0)
            {
                error = "Dialog Script: SeedTurnCount cannot be negative.";
                return false;
            }
            if (script.SeedTurnCount > steps.Count)
            {
                error = "Dialog Script: SeedTurnCount exceeds the number of steps.";
                return false;
            }

            int generated = steps.Count - script.SeedTurnCount;
            if (generated <= 0)
            {
                error = "Dialog Script: add at least one generated step (a user line after the seeded history).";
                return false;
            }

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i] ?? new TryllDialogStepData();
                if (string.IsNullOrWhiteSpace(step.User))
                {
                    error = i < script.SeedTurnCount
                        ? $"Dialog Script: seeded step {i + 1} needs a user line."
                        : $"Dialog Script: generated step {i + 1} needs a user line.";
                    return false;
                }
                if (i < script.SeedTurnCount && string.IsNullOrWhiteSpace(step.Assistant))
                {
                    error = $"Dialog Script: seeded step {i + 1} needs an assistant line.";
                    return false;
                }

                string kind = i < script.SeedTurnCount ? "seeded" : "generated";
                var checks = step.Checks ?? new List<TryllDialogCheck>();
                for (int ci = 0; ci < checks.Count; ci++)
                {
                    if (!TryValidateCheck(checks[ci], $"{kind} step {i + 1}, check {ci + 1}",
                            seeded: i < script.SeedTurnCount, out error))
                        return false;
                }
            }

            var globals = script.GlobalChecks ?? new List<TryllDialogCheck>();
            for (int ci = 0; ci < globals.Count; ci++)
            {
                if (!TryValidateCheck(globals[ci], $"global check {ci + 1}", seeded: false, out error))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// One check's authoring errors. <paramref name="where"/> prefixes the message.
        /// Disabled checks are not evaluated, so they are not validated either. A
        /// malformed regex is deliberately not an error here: it fails its turn as
        /// <c>invalid regex</c>, which is visible in the results.
        /// </summary>
        static bool TryValidateCheck(TryllDialogCheck check, string where, bool seeded, out string error)
        {
            error = null;
            string prefix = "Dialog Script: " + where;
            // An inspector slot with no type picked yet is dropped by the projection.
            if (check == null || !check.Enabled) return true;

            bool needsGraph = check is TryllIntentIsCheck || check is TryllIntentNotFoundCheck
                || check is TryllAttributeEqualsCheck || check is TryllAttributeInRangeCheck
                || check is TryllCallsToolCheck || check is TryllCallsNoToolCheck;
            if (seeded && needsGraph)
            {
                error = $"{prefix} ({TypeName(check)}) needs the graph to run, but a seeded turn is "
                      + "injected with AppendInteractions and never runs it, so there is nothing to check. "
                      + "Move the check to a generated step.";
                return false;
            }

            switch (check)
            {
                case TryllMustContainCheck c when string.IsNullOrEmpty(c.Text):
                case TryllMustNotContainCheck n when string.IsNullOrEmpty(n.Text):
                    error = $"{prefix}: the text is empty.";
                    return false;
                case TryllRegexForbidCheck c when string.IsNullOrEmpty(c.Pattern):
                case TryllRegexMatchCheck m when string.IsNullOrEmpty(m.Pattern):
                    error = $"{prefix}: the pattern is empty.";
                    return false;
                case TryllWordCountCheck c when c.Min <= 0 && c.Max <= 0:
                    error = $"{prefix}: set Min, Max, or both (0 leaves a bound unchecked).";
                    return false;
                case TryllWordCountCheck c when c.Max > 0 && c.Min > c.Max:
                    error = $"{prefix}: Min ({c.Min}) is greater than Max ({c.Max}).";
                    return false;
                case TryllIntentIsCheck c when string.IsNullOrWhiteSpace(c.Intent):
                    error = $"{prefix} has an intent check with no expected label.";
                    return false;
                case TryllIntentIsCheck c when (c.Node ?? "").IndexOf('|') >= 0 || (c.Intent ?? "").IndexOf('|') >= 0:
                case TryllIntentNotFoundCheck n when (n.Node ?? "").IndexOf('|') >= 0:
                    error = $"{prefix}: intent check names must not contain \"|\".";
                    return false;
                case TryllAttributeEqualsCheck c when string.IsNullOrWhiteSpace(c.Name):
                case TryllAttributeInRangeCheck r when string.IsNullOrWhiteSpace(r.Name):
                    error = $"{prefix}: the attribute name is empty.";
                    return false;
                case TryllAttributeInRangeCheck c when c.Min > c.Max:
                    error = $"{prefix}: Min ({c.Min}) is greater than Max ({c.Max}).";
                    return false;
                case TryllCallsToolCheck c when string.IsNullOrWhiteSpace(c.ToolName):
                    error = $"{prefix}: the tool name is empty.";
                    return false;
            }
            return true;
        }

        static string TypeName(TryllDialogCheck check) =>
            (check?.TypeId ?? "").Replace('_', ' ');

        /// <summary>
        /// Non-blocking warnings for intent checks against a variant's effective graph.
        /// </summary>
        public static List<string> CollectIntentCheckWarnings(
            TryllDialogScriptData script, TryllGraphDescription graph)
        {
            var warnings = new List<string>();
            if (script?.Steps == null) return warnings;

            var nodeNames = new HashSet<string>();
            int classifierCount = 0;
            var llmLabels = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            if (graph?.Nodes != null)
            {
                foreach (var n in graph.Nodes)
                {
                    if (n == null || string.IsNullOrEmpty(n.Name)) continue;
                    nodeNames.Add(n.Name);
                    var nt = n.Params != null ? n.Params.NodeType : (TryllNodeType)255;
                    if (nt == TryllNodeType.ClassifyIntent || nt == TryllNodeType.ClassifyIntentLLM)
                        classifierCount++;
                    if (nt == TryllNodeType.ClassifyIntentLLM
                        && n.Params is TryllClassifyIntentLLMParams llm)
                    {
                        var set = CollectLlmIntentIds(llm);
                        if (set.Count > 0)
                            llmLabels[n.Name] = set;
                    }
                }
            }

            for (int i = 0; i < script.Steps.Count; i++)
            {
                if (i < script.SeedTurnCount) continue;
                foreach (var check in script.Steps[i]?.Checks ?? new List<TryllDialogCheck>())
                {
                    if (check == null || !check.Enabled) continue;
                    string node, intent = null;
                    bool expectNotFound;
                    if (check is TryllIntentIsCheck isCheck) { node = isCheck.Node; intent = isCheck.Intent; expectNotFound = false; }
                    else if (check is TryllIntentNotFoundCheck nf) { node = nf.Node; expectNotFound = true; }
                    else continue;

                    if (!string.IsNullOrEmpty(node) && !nodeNames.Contains(node))
                    {
                        warnings.Add(
                            $"generated step {i + 1}: intent check names node \"{node}\" which is not in this workflow.");
                    }
                    if (string.IsNullOrEmpty(node) && classifierCount > 1)
                    {
                        warnings.Add(
                            $"generated step {i + 1}: unscoped intent check will address all {classifierCount} classifier nodes.");
                    }
                    if (!expectNotFound && !string.IsNullOrEmpty(intent) && !string.IsNullOrEmpty(node)
                        && llmLabels.TryGetValue(node, out var labels) && !labels.Contains(intent))
                    {
                        warnings.Add(
                            $"generated step {i + 1}: intent \"{intent}\" is not in node \"{node}\" intents.");
                    }
                }
            }
            return warnings;
        }

        /// <summary>
        /// Non-blocking warnings for seeded assistant lines that break a global check.
        /// Global checks run on generated turns only, so such a line is never counted; it
        /// is flagged before Run because the history may be what the check guards against.
        /// Only checks that need nothing but the text are evaluated, and an invalid regex
        /// is left to fail its generated turns.
        /// </summary>
        public static List<string> CollectSeededGlobalCheckWarnings(TryllDialogScriptData script)
        {
            var warnings = new List<string>();
            if (script?.Steps == null || script.GlobalChecks == null) return warnings;
            int seeded = Math.Min(Math.Max(0, script.SeedTurnCount), script.Steps.Count);
            for (int i = 0; i < seeded; i++)
            {
                string line = script.Steps[i]?.Assistant ?? "";
                for (int g = 0; g < script.GlobalChecks.Count; g++)
                {
                    var check = script.GlobalChecks[g];
                    if (!TryllDialogLabChecks.AppliesTo(check, TryllLabCheckScope.Step, seeded: true)) continue;
                    var outcome = TryllDialogLabChecks.EvaluateTextOnly(check, line);
                    if (outcome == null || outcome.Passed || outcome.Kind == TryllDialogLabChecks.KindInvalidRegex)
                        continue;
                    warnings.Add(SeededGlobalCheckWarning(i, g, TryllDialogLabChecks.AuthoredLabel(check)));
                }
            }
            return warnings;
        }

        internal static string SeededGlobalCheckWarning(int step, int check, string label) =>
            $"seeded step {step + 1}: the assistant line breaks global check {check + 1} ({label}). "
          + "Global checks skip seeded turns, so this is not counted; edit the line if the check should hold there too.";

        static HashSet<string> CollectLlmIntentIds(TryllClassifyIntentLLMParams llm)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (llm.Intents != null && llm.Intents.Length > 0)
            {
                foreach (var entry in llm.Intents)
                {
                    if (entry == null) continue;
                    var id = (entry.Id ?? "").Trim();
                    if (id.Length > 0) set.Add(id);
                }
            }
            return set;
        }

        /// <summary>
        /// The half of validation that needs the agent as well as the script.
        /// A stateless agent (<c>MaintainDialogueHistory = false</c>) has its dialogue
        /// cleared by the server after every turn, so scripted history injected by
        /// <c>AppendInteractions</c> survives exactly one generated turn and is then
        /// gone. Running it anyway would silently apply the seed to the first generated
        /// step only — a result that looks fine and means nothing — so it is refused
        /// instead.
        /// </summary>
        public static bool TryValidateForAgent(
            TryllDialogScriptData script,
            bool maintainDialogueHistory,
            string agentName,
            out string error)
        {
            error = null;
            if (maintainDialogueHistory) return true;
            if (script == null || script.SeedTurnCount <= 0) return true;

            error = $"agent '{agentName}' is stateless (Maintain Dialogue History is off), "
                  + $"but the script seeds {script.SeedTurnCount} turn(s) of history. The server "
                  + "clears a stateless agent's dialogue after every turn, so the seed would only "
                  + "reach the first generated turn. Set SeedTurnCount to 0, or run this script "
                  + "against a stateful agent.";
            return false;
        }
    }
}
