// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Writes a Dialog Lab run as versioned JSONL: one header line carrying provenance
    /// and the full script, then one line per cell (seeded, generated, and skipped).
    /// Export always consumes an immutable <see cref="TryllDialogLabRunResult"/> — never
    /// live window fields.
    /// </summary>
    internal static class TryllDialogLabExport
    {
        // 4: typed checks. The header carries `dialogues[]` (a run may cover several
        //    scripts) instead of one `script`; each step has `checks[]` of
        //    {type, enabled, note, …} and each script `global_checks[]`; each turn line
        //    carries `dialogue_index` and `check_outcomes[]` — every evaluated check,
        //    passes included, identified by scope + index. Import still reads 3.
        //
        // 3: full script in header, seeded/skipped turns, structured checks, timestamps,
        //    workflow/model provenance.
        //
        // Fields added later without a bump must have an absent-value default that is
        // exactly what older files ran with (as `maintain_dialogue_history` defaults to
        // true and `use_live_tools` to false): the reader gate is exact equality, so a
        // bump makes every earlier file unreadable.
        public const int FormatVersion = 4;

        public readonly struct ServerIdentity
        {
            public readonly string ExePath, BuildConfig, Version, RunId;
            public readonly bool Managed;
            public ServerIdentity(string exePath, string buildConfig, string version, string runId, bool managed = false)
            {
                ExePath = exePath ?? "";
                BuildConfig = buildConfig ?? "";
                Version = version ?? "";
                RunId = runId ?? "";
                Managed = managed;
            }

            public static ServerIdentity FromClient(TryllClient c) =>
                new ServerIdentity(
                    c?.ServerExePath, c?.ServerBuildConfig, c?.ServerVersion, c?.ServerRunId,
                    c != null && c.ServerManaged);
        }

        public static string ExportToJsonl(TryllDialogLabRunResult run)
        {
            if (run?.Snapshot?.Dialogues == null || run.Snapshot.Dialogues.Count == 0)
                throw new ArgumentNullException(nameof(run));

            var sb = new StringBuilder();
            var header = new StringBuilder();
            header.Append('{');
            AppendKv(header, "format_version", FormatVersion, first: true);
            AppendKv(header, "kind", "dialog_lab_run");
            AppendKv(header, "run_id", run.Snapshot.RunId ?? "");
            AppendKv(header, "started_at", run.StartedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            AppendKv(header, "completed_at", run.CompletedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            AppendKv(header, "status", run.Status.ToString().ToLowerInvariant());
            AppendKv(header, "default_agent_name", run.Snapshot.DefaultAgentName ?? "");
            AppendKv(header, "settings_asset_name", run.Snapshot.SettingsAssetName ?? "");
            AppendKv(header, "repeats", run.Snapshot.Repeats);
            AppendKv(header, "base_seed", run.Snapshot.BaseSeed);
            AppendRaw(header, "dialogues", $"[{string.Join(",", run.Snapshot.Dialogues.Select(ScriptJson))}]");
            AppendRaw(header, "variants", VariantsJson(run.Snapshot.Variants));
            // Derived, for readers of the file; import recomputes rather than trusting it.
            AppendRaw(header, "stats", TryllDialogLabStatistics.Compute(run).ToJson());
            AppendRaw(header, "server", ServerJson(run.Server));
            AppendRaw(header, "issues", ArrayOfStrings(run.Issues ?? new List<string>()));
            header.Append('}');
            sb.Append(header).Append('\n');

            foreach (var sample in run.Samples ?? new List<TryllLabSampleResult>())
            {
                foreach (var turn in sample.Turns ?? new List<TryllLabTurnRecord>())
                {
                    var line = new StringBuilder();
                    line.Append('{');
                    AppendKv(line, "sample_id", sample.SampleId ?? "", first: true);
                    AppendKv(line, "variant_index", sample.VariantIndex);
                    AppendKv(line, "variant_label", sample.VariantLabel ?? "");
                    AppendKv(line, "repeat", sample.Repeat);
                    AppendKv(line, "seed", sample.Seed);
                    AppendKv(line, "dialogue_index", turn.DialogueIndex);
                    AppendKv(line, "turn_index", turn.TurnIndex);
                    AppendKv(line, "seeded", turn.Seeded);
                    AppendKv(line, "user_text", turn.UserText ?? "");
                    AppendKv(line, "answer_text", turn.AnswerText ?? "");
                    AppendKv(line, "status", turn.Status.ToString());
                    AppendRaw(line, "failed_checks", FailedCheckLabels(turn.FailedChecks));
                    AppendRaw(line, "check_outcomes", TryllDialogCheckJson.WriteOutcomes(turn.CheckOutcomes));
                    if (!string.IsNullOrEmpty(turn.ErrorMessage)) AppendKv(line, "error", turn.ErrorMessage);
                    AppendKv(line, "duration_ms", turn.DurationMs);
                    AppendKv(line, "ttft_ms", turn.TtftMs);
                    AppendKv(line, "tokens_generated", turn.TokensGenerated);
                    AppendRaw(line, "debug_info",
                        string.IsNullOrEmpty(turn.DebugInfoRaw) ? "null" : EscapeAsRawJsonOrString(turn.DebugInfoRaw));
                    line.Append('}');
                    sb.Append(line).Append('\n');
                }
            }

            return sb.ToString();
        }

        private static string ScriptJson(TryllDialogScriptData script)
        {
            var steps = script.Steps.Select(s =>
            {
                var tools = (s.ToolResults ?? new List<TryllCannedToolResultData>()).Select(t =>
                    $"{{\"tool_name\":{Json(t.ToolName)},\"result\":{Json(t.Result)}}}");
                return "{"
                     + $"\"user\":{Json(s.User)},\"assistant\":{Json(s.Assistant)},"
                     + $"\"checks\":{TryllDialogCheckJson.WriteList(s.Checks)},"
                     + $"\"tool_results\":[{string.Join(",", tools)}]"
                     + "}";
            });
            return "{"
                 + $"\"name\":{Json(script.Name)},\"description\":{Json(script.Description)},"
                 + $"\"seed_turn_count\":{script.SeedTurnCount},"
                 + $"\"global_checks\":{TryllDialogCheckJson.WriteList(script.GlobalChecks)},"
                 + $"\"steps\":[{string.Join(",", steps)}]"
                 + "}";
        }

        private static string VariantsJson(List<TryllLabVariantSnapshot> variants)
        {
            var items = variants.Select(v =>
            {
                var overrides = v.Overrides.Select(o =>
                    $"{{\"node\":{Json(o.NodeName)},\"field\":{Json(o.FieldPath)},\"value\":{Json(o.Value)}}}");
                var nodes = v.Nodes.Select(n =>
                    $"{{\"name\":{Json(n.Name)},\"type\":{Json(n.Type)}}}");
                return "{"
                     + $"\"index\":{v.Index},\"label\":{Json(v.Label)},"
                     + $"\"agent\":{Json(v.AgentName)},\"agent_is_override\":{(v.AgentIsOverride ? "true" : "false")},"
                     + $"\"workflow_asset\":{Json(v.WorkflowAssetName)},"
                     + $"\"graph_start_node\":{Json(v.GraphStartNode)},"
                     + $"\"default_model_name\":{Json(v.DefaultModelName)},"
                     + $"\"model_names\":{ArrayOfStrings(v.ModelNames)},"
                     + $"\"maintain_dialogue_history\":{(v.MaintainDialogueHistory ? "true" : "false")},"
                     + $"\"use_live_tools\":{(v.UseLiveTools ? "true" : "false")},"
                     // default_model_name above is the EFFECTIVE model; this says whether
                     // it came from a per-variant override or from the workflow.
                     + $"\"model_override\":{Json(v.ModelOverride)},"
                     + $"\"nodes\":[{string.Join(",", nodes)}],"
                     + $"\"overrides\":[{string.Join(",", overrides)}]"
                     + "}";
            });
            return $"[{string.Join(",", items)}]";
        }

        private static string ServerJson(ServerIdentity server)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            AppendKv(sb, "exe_path", server.ExePath, first: true);
            AppendKv(sb, "build_config", server.BuildConfig);
            AppendKv(sb, "version", server.Version);
            AppendKv(sb, "run_id", server.RunId);
            AppendKv(sb, "managed", server.Managed);
            sb.Append('}');
            return sb.ToString();
        }

        private static string FailedCheckLabels(IEnumerable<TryllLabCheckOutcome> failures) =>
            ArrayOfStrings((failures ?? Enumerable.Empty<TryllLabCheckOutcome>()).Select(f => f.Label ?? ""));

        private static string ArrayOfStrings(IEnumerable<string> values) =>
            $"[{string.Join(",", (values ?? Enumerable.Empty<string>()).Select(Json))}]";

        private static void AppendKv(StringBuilder sb, string key, string value, bool first = false)
        {
            if (!first) sb.Append(',');
            sb.Append(Json(key)).Append(':').Append(Json(value));
        }

        private static void AppendKv(StringBuilder sb, string key, int value, bool first = false)
        {
            if (!first) sb.Append(',');
            sb.Append(Json(key)).Append(':').Append(value);
        }

        private static void AppendKv(StringBuilder sb, string key, bool value, bool first = false)
        {
            if (!first) sb.Append(',');
            sb.Append(Json(key)).Append(':').Append(value ? "true" : "false");
        }

        private static void AppendRaw(StringBuilder sb, string key, string rawJson, bool first = false)
        {
            if (!first) sb.Append(',');
            sb.Append(Json(key)).Append(':').Append(rawJson);
        }

        private static string Json(string s)
        {
            if (s == null) return "null";
            return TryllJsonWriter.Quote(s);
        }

        private static string EscapeAsRawJsonOrString(string raw)
        {
            if (TryllJson.TryParse(raw, out _, out _)) return raw;
            return Json(raw);
        }
    }
}
