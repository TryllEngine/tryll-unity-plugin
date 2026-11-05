// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Loads a Dialog Lab JSONL file (format 4, or the older 3) into an immutable
    /// <see cref="TryllDialogLabRunResult"/> for read-only results display. Does not
    /// recreate assets, agents, or runnable config.
    /// </summary>
    internal static class TryllDialogLabImport
    {
        /// <summary>The pre-typed-check format, still readable: its step fields convert
        /// through the same migration as an old asset, and its failures map onto the
        /// converted checks (see <see cref="MapV3Outcomes"/>).</summary>
        public const int LegacyFormatVersion = 3;

        public static bool TryLoad(string text, out TryllDialogLabRunResult result, out string error)
        {
            result = null;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "File is empty.";
                return false;
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split(new[] { '\n' }, StringSplitOptions.None)
                .Where(l => l.Length > 0)
                .ToList();
            if (lines.Count < 1)
            {
                error = "File has no JSONL lines.";
                return false;
            }

            if (!TryllJson.TryParse(lines[0], out var header, out var headerErr) || header.Kind != TryllJsonKind.Object)
            {
                error = $"Line 1: invalid header JSON ({headerErr ?? "expected object"}).";
                return false;
            }

            int format = header.Get("format_version").AsInt(0);
            if (format != TryllDialogLabExport.FormatVersion && format != LegacyFormatVersion)
            {
                error = format < LegacyFormatVersion
                    ? $"Unsupported format_version {format}. Re-export from a current Dialog Lab (format {TryllDialogLabExport.FormatVersion})."
                    : $"Unsupported format_version {format} (this Lab reads formats {LegacyFormatVersion} and {TryllDialogLabExport.FormatVersion}).";
                return false;
            }
            bool legacy = format == LegacyFormatVersion;
            if (header.Get("kind").AsString("") != "dialog_lab_run")
            {
                error = "Line 1: kind must be \"dialog_lab_run\".";
                return false;
            }

            var dialogues = new List<TryllDialogScriptData>();
            if (legacy)
            {
                var scriptObj = header.Get("script");
                if (scriptObj.Kind != TryllJsonKind.Object)
                {
                    error = "Line 1: missing script object.";
                    return false;
                }
                var script = ParseScriptV3(scriptObj, out error);
                if (script == null) return false;
                dialogues.Add(script);
            }
            else
            {
                var dialogueArr = header.Get("dialogues").AsArray();
                if (dialogueArr.Count == 0)
                {
                    error = "Line 1: dialogues array is empty.";
                    return false;
                }
                for (int d = 0; d < dialogueArr.Count; d++)
                {
                    var script = ParseScriptV4(dialogueArr[d], out error);
                    if (script == null)
                    {
                        if (dialogueArr.Count > 1) error = error.Replace("Line 1: ", $"Line 1: dialogue {d + 1}: ");
                        return false;
                    }
                    dialogues.Add(script);
                }
            }

            var variantsArr = header.Get("variants").AsArray();
            if (variantsArr.Count == 0)
            {
                error = "Line 1: variants array is empty.";
                return false;
            }

            var snapshot = new TryllDialogLabRunSnapshot
            {
                RunId = header.Get("run_id").AsString(""),
                CapturedAtUtc = ParseTime(header.Get("started_at").AsString("")) ?? DateTimeOffset.UtcNow,
                Dialogues = dialogues,
                Repeats = Math.Max(1, header.Get("repeats").AsInt(1)),
                BaseSeed = Math.Max(1, header.Get("base_seed").AsInt(1)),
                DefaultAgentName = header.Get("default_agent_name").AsString(""),
                SettingsAssetName = header.Get("settings_asset_name").AsString(""),
            };

            for (int i = 0; i < variantsArr.Count; i++)
            {
                var v = variantsArr[i];
                var vs = new TryllLabVariantSnapshot
                {
                    Index = v.Get("index").AsInt(i),
                    Label = v.Get("label").AsString($"Variant {i + 1}"),
                    Swatch = PaletteColor(i),
                    AgentName = v.Get("agent").AsString(""),
                    AgentIsOverride = v.Get("agent_is_override").AsBool(false),
                    WorkflowAssetName = v.Get("workflow_asset").AsString(""),
                    GraphStartNode = v.Get("graph_start_node").AsString(""),
                    DefaultModelName = v.Get("default_model_name").AsString(""),
                    ModelNames = v.Get("model_names").AsArray().Select(x => x.AsString("")).ToList(),
                    // Absent in files written before the Lab honoured the agent's
                    // setting, and true is exactly what those runs used.
                    MaintainDialogueHistory = v.Get("maintain_dialogue_history").AsBool(true),
                    // Absent in files written before the live-tools opt-in, and false is
                    // exactly what those runs used (uncanned call failed the cell).
                    UseLiveTools = v.Get("use_live_tools").AsBool(false),
                    // Absent before per-variant models existed; empty means the run used
                    // whatever default_model_name already records.
                    ModelOverride = v.Get("model_override").AsString(""),
                };
                foreach (var n in v.Get("nodes").AsArray())
                    vs.Nodes.Add(new TryllLabNodeSummary
                    {
                        Name = n.Get("name").AsString(""),
                        Type = n.Get("type").AsString(""),
                    });
                foreach (var o in v.Get("overrides").AsArray())
                    vs.Overrides.Add(new TryllParamOverrideData
                    {
                        NodeName = o.Get("node").AsString(""),
                        FieldPath = o.Get("field").AsString(""),
                        Value = o.Get("value").AsString(""),
                    });
                snapshot.Variants.Add(vs);
            }

            var serverObj = header.Get("server");
            var server = new TryllDialogLabExport.ServerIdentity(
                serverObj.Get("exe_path").AsString(""),
                serverObj.Get("build_config").AsString(""),
                serverObj.Get("version").AsString(""),
                serverObj.Get("run_id").AsString(""),
                serverObj.Get("managed").AsBool(false));

            var run = new TryllDialogLabRunResult
            {
                Snapshot = snapshot,
                Server = server,
                StartedAtUtc = ParseTime(header.Get("started_at").AsString("")) ?? DateTimeOffset.UtcNow,
                CompletedAtUtc = ParseTime(header.Get("completed_at").AsString("")) ?? DateTimeOffset.UtcNow,
                Status = ParseStatus(header.Get("status").AsString("completed")),
                Issues = header.Get("issues").AsArray().Select(x => x.AsString("")).Where(s => s.Length > 0).ToList(),
            };

            var samplesById = new Dictionary<string, TryllLabSampleResult>(StringComparer.Ordinal);
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int li = 1; li < lines.Count; li++)
            {
                if (!TryllJson.TryParse(lines[li], out var turnObj, out var turnErr) || turnObj.Kind != TryllJsonKind.Object)
                {
                    error = $"Line {li + 1}: invalid turn JSON ({turnErr ?? "expected object"}).";
                    return false;
                }

                string sampleId = turnObj.Get("sample_id").AsString("");
                // Absent in format 3, which had one script.
                int dialogueIndex = turnObj.Get("dialogue_index").AsInt(0);
                if (dialogueIndex < 0 || dialogueIndex >= dialogues.Count)
                {
                    error = $"Line {li + 1}: dialogue_index out of range.";
                    return false;
                }
                var script = dialogues[dialogueIndex];
                int turnIndex = turnObj.Get("turn_index").AsInt(-1);
                if (string.IsNullOrEmpty(sampleId) || turnIndex < 0 || turnIndex >= script.Steps.Count)
                {
                    error = $"Line {li + 1}: invalid sample_id/turn_index.";
                    return false;
                }
                int flatIndex = snapshot.TurnOffset(dialogueIndex) + turnIndex;

                string key = $"{sampleId}:{dialogueIndex}:{turnIndex}";
                if (!seenKeys.Add(key))
                {
                    error = $"Line {li + 1}: duplicate cell {key}.";
                    return false;
                }

                if (!samplesById.TryGetValue(sampleId, out var sample))
                {
                    int variantIndex = turnObj.Get("variant_index").AsInt(0);
                    if (variantIndex < 0 || variantIndex >= snapshot.Variants.Count)
                    {
                        error = $"Line {li + 1}: variant_index out of range.";
                        return false;
                    }
                    sample = new TryllLabSampleResult
                    {
                        SampleId = sampleId,
                        VariantIndex = variantIndex,
                        VariantLabel = turnObj.Get("variant_label").AsString(snapshot.Variants[variantIndex].Label),
                        VariantSwatch = snapshot.Variants[variantIndex].Swatch,
                        Repeat = turnObj.Get("repeat").AsInt(0),
                        Seed = turnObj.Get("seed").AsInt(0),
                        Turns = Enumerable.Range(0, snapshot.TotalSteps)
                            .Select(i => (TryllLabTurnRecord)null)
                            .ToList(),
                    };
                    samplesById[sampleId] = sample;
                }

                var record = new TryllLabTurnRecord
                {
                    DialogueIndex = dialogueIndex,
                    TurnIndex = turnIndex,
                    Seeded = turnObj.Get("seeded").AsBool(false),
                    UserText = turnObj.Get("user_text").AsString(""),
                    AnswerText = turnObj.Get("answer_text").AsString(""),
                    Status = ParseCellStatus(turnObj.Get("status").AsString("Ok")),
                    ErrorMessage = turnObj.HasKey("error") ? turnObj.Get("error").AsString(null) : null,
                    DurationMs = turnObj.Get("duration_ms").AsInt(0),
                    TtftMs = turnObj.Get("ttft_ms").AsInt(0),
                    TokensGenerated = turnObj.Get("tokens_generated").AsInt(0),
                    Words = TryllDialogLabChecks.CountWords(turnObj.Get("answer_text").AsString("")),
                };
                record.CheckOutcomes = legacy
                    ? MapV3Outcomes(script, turnIndex, record, ParseV3Failures(turnObj))
                    : TryllDialogCheckJson.ReadOutcomes(turnObj.Get("check_outcomes"));

                var dbg = turnObj.Get("debug_info");
                if (!dbg.IsNull)
                {
                    record.DebugInfo = dbg;
                    record.DebugInfoRaw = PrettyCompact(dbg);
                }

                sample.Turns[flatIndex] = record;
            }

            // Rectangular completeness: every sample must have every turn of every dialogue.
            foreach (var sample in samplesById.Values)
            {
                for (int d = 0; d < dialogues.Count; d++)
                {
                    int offset = snapshot.TurnOffset(d);
                    for (int ti = 0; ti < dialogues[d].Steps.Count; ti++)
                    {
                        if (sample.Turns[offset + ti] == null)
                        {
                            error = dialogues.Count > 1
                                ? $"Sample {sample.SampleId} is missing dialogue_index {d} turn_index {ti}."
                                : $"Sample {sample.SampleId} is missing turn_index {ti}.";
                            return false;
                        }
                    }
                }
                run.Samples.Add(sample);
            }

            // Stable order: by variant index then repeat.
            run.Samples = run.Samples
                .OrderBy(s => s.VariantIndex)
                .ThenBy(s => s.Repeat)
                .ToList();

            if (run.Samples.Count == 0)
            {
                error = "File has a header but no turn records.";
                return false;
            }

            result = run;
            return true;
        }

        private static TryllDialogScriptData ParseScriptV4(TryllJsonValue scriptObj, out string error)
        {
            error = null;
            var script = new TryllDialogScriptData
            {
                Name = scriptObj.Get("name").AsString(""),
                Description = scriptObj.Get("description").AsString(""),
                SeedTurnCount = scriptObj.Get("seed_turn_count").AsInt(0),
            };
            if (!TryllDialogCheckJson.TryReadList(scriptObj.Get("global_checks"), script.GlobalChecks, out var checkErr))
            {
                error = $"Line 1: global_checks: {checkErr}.";
                return null;
            }
            var steps = scriptObj.Get("steps").AsArray();
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                var step = new TryllDialogStepData
                {
                    User = s.Get("user").AsString(""),
                    Assistant = s.Get("assistant").AsString(""),
                    ToolResults = ParseToolResults(s),
                };
                if (!TryllDialogCheckJson.TryReadList(s.Get("checks"), step.Checks, out checkErr))
                {
                    error = $"Line 1: step {i + 1} checks: {checkErr}.";
                    return null;
                }
                script.Steps.Add(step);
            }
            return ValidateShape(script, out error) ? script : null;
        }

        /// <summary>
        /// A format-3 script: its fixed check fields go through
        /// <see cref="TryllDialogCheckMigration"/>, exactly as an old asset does on load,
        /// so the imported checks are the ones that asset would have today.
        /// </summary>
        private static TryllDialogScriptData ParseScriptV3(TryllJsonValue scriptObj, out string error)
        {
            int seedTurnCount = scriptObj.Get("seed_turn_count").AsInt(0);
            var script = new TryllDialogScriptData
            {
                Name = scriptObj.Get("name").AsString(""),
                Description = scriptObj.Get("description").AsString(""),
                SeedTurnCount = seedTurnCount,
                // Absent in files written before script-level patterns existed; empty is
                // what those runs actually evaluated with.
                GlobalChecks = TryllDialogCheckMigration.FromLegacyGlobal(
                    scriptObj.Get("global_regex_forbid").AsArray().Select(x => x.AsString(""))),
            };
            var steps = scriptObj.Get("steps").AsArray();
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                var legacyStep = new TryllDialogScriptAsset.Step
                {
                    MustContain = s.Get("must_contain").AsArray().Select(x => x.AsString("")).ToArray(),
                    MustNotContain = s.Get("must_not_contain").AsArray().Select(x => x.AsString("")).ToArray(),
                    RegexForbid = s.Get("regex_forbid").AsArray().Select(x => x.AsString("")).ToArray(),
                    MaxWords = s.Get("max_words").AsInt(0),
                    IntentChecks = s.Get("intent_checks").AsArray()
                        .Select(c => new TryllDialogScriptAsset.IntentCheck
                        {
                            Node = c.Get("node").AsString(""),
                            Intent = c.Get("intent").AsString(""),
                            ExpectNotFound = c.Get("expect_not_found").AsBool(),
                        }).ToList(),
                    AttributeChecks = s.Get("attribute_checks").AsArray()
                        .Select(c => new TryllDialogScriptAsset.AttributeCheck
                        {
                            Node = c.Get("node").AsString(""),
                            Name = c.Get("name").AsString(""),
                            Value = c.Get("value").AsString(""),
                            ScoreRange = c.Get("score_range").AsBool(),
                            ScoreMin = (float)c.Get("score_min").AsDouble(0),
                            ScoreMax = (float)c.Get("score_max").AsDouble(0),
                        }).ToList(),
                    ToolResults = s.Get("tool_results").AsArray()
                        .Select(t => new TryllDialogScriptAsset.CannedToolResult
                        {
                            ToolName = t.Get("tool_name").AsString(""),
                            Result = t.Get("result").AsString(""),
                        }).ToList(),
                };
                script.Steps.Add(new TryllDialogStepData
                {
                    User = s.Get("user").AsString(""),
                    Assistant = s.Get("assistant").AsString(""),
                    Checks = TryllDialogCheckMigration.FromLegacyStep(legacyStep, generated: i >= seedTurnCount),
                    ToolResults = ParseToolResults(s),
                });
            }
            error = null;
            return ValidateShape(script, out error) ? script : null;
        }

        private static List<TryllCannedToolResultData> ParseToolResults(TryllJsonValue step) =>
            step.Get("tool_results").AsArray()
                .Select(t => new TryllCannedToolResultData
                {
                    ToolName = t.Get("tool_name").AsString(""),
                    Result = t.Get("result").AsString(""),
                }).ToList();

        private static bool ValidateShape(TryllDialogScriptData script, out string error)
        {
            error = null;
            if (script.Steps.Count == 0)
            {
                error = "Line 1: script.steps is empty.";
                return false;
            }
            if (script.SeedTurnCount < 0 || script.SeedTurnCount > script.Steps.Count)
            {
                error = "Line 1: invalid script.seed_turn_count.";
                return false;
            }
            return true;
        }

        private static List<TryllLabCheckOutcome> ParseV3Failures(TryllJsonValue turnObj)
        {
            var details = turnObj.Get("failed_check_details").AsArray();
            if (details.Count > 0)
            {
                return details.Select(d => new TryllLabCheckOutcome
                {
                    Kind = d.Get("kind").AsString(""),
                    Arg = d.Get("arg").AsString(""),
                    Actual = d.HasKey("actual") ? d.Get("actual").AsString(null) : null,
                    Label = d.Get("label").AsString(""),
                }).ToList();
            }

            // Fallback: labels only (should not appear in v3 writers).
            return turnObj.Get("failed_checks").AsArray()
                .Select(x => new TryllLabCheckOutcome
                {
                    Kind = "",
                    Arg = "",
                    Label = x.AsString(""),
                }).ToList();
        }

        /// <summary>
        /// Format 3 recorded only failures, identified by kind + arg. Tie each one to the
        /// converted check it came from. On a turn whose checks ran (Ok or CheckFail),
        /// every check with no failure passed, because format 3 evaluated all of them. A
        /// failure that matches nothing (a label-only record, or a global pattern on a
        /// seeded turn, which format 3 still checked) is kept with index -1 so the cell
        /// stays failed.
        /// </summary>
        internal static List<TryllLabCheckOutcome> MapV3Outcomes(
            TryllDialogScriptData script, int turnIndex, TryllLabTurnRecord record,
            List<TryllLabCheckOutcome> failures)
        {
            var step = script.Steps[turnIndex];
            bool evaluated = record.Status == TryllLabCellStatus.Ok || record.Status == TryllLabCellStatus.CheckFail;
            var remaining = new List<TryllLabCheckOutcome>(failures);
            var outcomes = new List<TryllLabCheckOutcome>();
            foreach (var row in TryllDialogLabChecks.AuthoredChecks(script, step, record.Seeded))
            {
                string checkType = CheckTypeAt(script, step, row);
                var failure = remaining.FirstOrDefault(f => V3FailureMatches(f, row));
                if (failure != null)
                {
                    remaining.Remove(failure);
                    failure.Scope = row.Scope;
                    failure.Index = row.Index;
                    failure.CheckType = checkType;
                    failure.Passed = false;
                    outcomes.Add(failure);
                }
                else if (evaluated)
                {
                    outcomes.Add(new TryllLabCheckOutcome
                    {
                        Scope = row.Scope,
                        Index = row.Index,
                        CheckType = checkType,
                        Kind = row.Kind,
                        Arg = row.Arg,
                        Passed = true,
                        Label = row.Label,
                    });
                }
            }
            foreach (var f in remaining)
            {
                f.Scope = TryllLabCheckScope.Step;
                f.Index = -1;
                f.Passed = false;
                outcomes.Add(f);
            }
            return outcomes;
        }

        static string CheckTypeAt(TryllDialogScriptData script, TryllDialogStepData step, LabCheck row) =>
            row.Scope == TryllLabCheckScope.Gate ? TryllDialogLabChecks.GateUnexpectedToolCall
            : row.Scope == TryllLabCheckScope.Global ? script.GlobalChecks[row.Index].TypeId
            : step.Checks[row.Index].TypeId;

        /// <summary>The format-3 kind folds: a malformed pattern failed as invalid_regex,
        /// an absent classifier as *_unavailable, a word limit as max_words, and both
        /// "must not call" forms of the tool assertion are now the gate.</summary>
        static bool V3FailureMatches(TryllLabCheckOutcome f, LabCheck row)
        {
            if (row.Scope == TryllLabCheckScope.Gate)
                return f.Kind == TryllDialogLabChecks.KindToolCall
                    && (f.Arg == "" || f.Arg == TryllDialogLabChecks.ToolCallOtherArg);
            if (!string.Equals(ArgFor(f), row.Arg, StringComparison.Ordinal)) return false;
            switch (f.Kind)
            {
                case TryllDialogLabChecks.KindInvalidRegex:
                    return row.Kind == TryllDialogLabChecks.KindRegexForbid;
                case TryllDialogLabChecks.KindMaxWords:
                    return row.Kind == TryllDialogLabChecks.KindWordCount;
                case TryllDialogLabChecks.KindIntentUnavailable:
                    return row.Kind == TryllDialogLabChecks.KindIntentIs || row.Kind == TryllDialogLabChecks.KindIntentNotFound;
                case TryllDialogLabChecks.KindAttributeUnavailable:
                    return row.Kind == TryllDialogLabChecks.KindAttributeIs || row.Kind == TryllDialogLabChecks.KindAttributeScore;
                case TryllDialogLabChecks.KindToolCall:
                    return row.Kind == TryllDialogLabChecks.KindToolCall && (f.Arg ?? "").Length > 0;
                default:
                    return f.Kind == row.Kind;
            }
        }

        /// <summary>A max_words failure's arg is the bare limit; the converted Word count
        /// check reports as "0|limit".</summary>
        static string ArgFor(TryllLabCheckOutcome f) =>
            f.Kind == TryllDialogLabChecks.KindMaxWords && int.TryParse(f.Arg, out int max)
                ? TryllDialogLabChecks.WordCountArg(0, max)
                : f.Arg ?? "";

        private static TryllDialogLabRunStatus ParseStatus(string s) => s?.ToLowerInvariant() switch
        {
            "stopped" => TryllDialogLabRunStatus.Stopped,
            "failed" => TryllDialogLabRunStatus.Failed,
            _ => TryllDialogLabRunStatus.Completed,
        };

        private static TryllLabCellStatus ParseCellStatus(string s) => s switch
        {
            nameof(TryllLabCellStatus.CheckFail) => TryllLabCellStatus.CheckFail,
            nameof(TryllLabCellStatus.Error) => TryllLabCellStatus.Error,
            nameof(TryllLabCellStatus.Skipped) => TryllLabCellStatus.Skipped,
            _ => TryllLabCellStatus.Ok,
        };

        private static DateTimeOffset? ParseTime(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            return DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dto)
                ? dto
                : (DateTimeOffset?)null;
        }

        private static Color PaletteColor(int i) => TryllEditorTheme.VariantSwatch(i);

        private static string PrettyCompact(TryllJsonValue v)
        {
            // Re-serialize enough for DebugInfoRaw consumers; Import keeps the DOM too.
            switch (v.Kind)
            {
                case TryllJsonKind.Null: return "null";
                case TryllJsonKind.Bool: return v.Bool ? "true" : "false";
                case TryllJsonKind.Number: return v.Number.ToString(CultureInfo.InvariantCulture);
                case TryllJsonKind.String: return "\"" + (v.String ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                case TryllJsonKind.Array:
                    return "[" + string.Join(",", v.Array.Select(PrettyCompact)) + "]";
                case TryllJsonKind.Object:
                    return "{" + string.Join(",", v.Object.Select(kv =>
                        "\"" + kv.Key.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\":" + PrettyCompact(kv.Value))) + "}";
                default: return "null";
            }
        }
    }
}
