// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Linq;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// A headless run's verdict: the exit code, the summary JSON written next to the JSONL, and
    /// the lines printed to the log. Pure, so tests pin it without a server, and Unreal's
    /// FTryllDialogLabBatchSummary writes the same bytes.
    /// </summary>
    internal sealed class TryllDialogLabBatchSummary
    {
        public const int ExitPassed = 0;
        public const int ExitThresholdMissed = 1;
        public const int ExitRunFailed = 2;

        public int ExitCode;
        public string Json = "";
        public List<string> Lines = new List<string>();

        /// <param name="run">The finished run, or null when it never started.</param>
        /// <param name="failure">Why the run could not start or finish; null when it ran.</param>
        public static TryllDialogLabBatchSummary Build(
            TryllDialogLabRunResult run, TryllDialogLabBatchConfig config, string failure = null)
        {
            var summary = new TryllDialogLabBatchSummary();
            var issues = new List<string>(run?.Issues ?? new List<string>());
            string status = run == null ? "failed" : run.Status.ToString().ToLowerInvariant();

            TryllDialogLabStatistics stats = run != null ? TryllDialogLabStatistics.Compute(run) : null;
            bool ranToEnd = failure == null && run != null && run.Status == TryllDialogLabRunStatus.Completed;
            if (!ranToEnd && failure == null)
                failure = run == null ? "The run did not start." : $"The run ended as {status}.";
            // A run can complete with part of it broken: a dialogue whose agent could not be
            // created, a failed reseed or rewind, a turn the server errored. Those cells are Error
            // or not run, never a check failure, so they are the rig failing, not the NPC.
            if (ranToEnd && failure == null)
                failure = InfrastructureFailure(stats);
            // The config refuses this up front; a run that still has fewer seeds per variant
            // (an imported or synthesised one) must not gate on a bound its own stats omit.
            if (ranToEnd && failure == null && config.UseLowerBound && !stats.IntervalsAvailable)
                failure = TryllDialogLabBatchConfig.LowerBoundSeedsError(stats.MinSeeds, "run");
            if (failure != null)
            {
                ranToEnd = false;
                issues.Add(failure);
                if (status == "completed") status = "failed";
            }
            var checks = new List<string>();
            int missed = 0;
            if (ranToEnd)
            {
                string basis = config.UseLowerBound ? "lower_bound" : "point";
                var v = stats.Variants.FirstOrDefault();
                void Gate(string metric, double? required, Func<TryllLabVariantStats, TryllLabRate> pick)
                {
                    if (!required.HasValue || v == null) return;
                    var rate = pick(v);
                    double actual = config.UseLowerBound ? rate.Low : rate.Rate;
                    bool passed = actual >= required.Value - 1e-12;
                    if (!passed) missed++;
                    checks.Add("{"
                        + $"\"metric\":\"{metric}\",\"required\":{Num(required.Value)},\"actual\":{Num(actual)},"
                        + $"\"basis\":\"{basis}\",\"passed\":{(passed ? "true" : "false")}"
                        + "}");
                    summary.Lines.Add(
                        $"{metric}: {Num(actual)} {(passed ? ">=" : "<")} {Num(required.Value)} ({basis}) {(passed ? "PASS" : "FAIL")}");
                }
                Gate("turns_passed", config.TurnsPassed, s => s.Turns);
                Gate("checks_passed", config.ChecksPassed, s => s.Checks);
                Gate("clean_dialogues", config.CleanDialogues, s => s.CleanDialogues);
            }

            summary.ExitCode = !ranToEnd ? ExitRunFailed : missed > 0 ? ExitThresholdMissed : ExitPassed;
            summary.Lines.Add(summary.ExitCode == ExitPassed
                ? $"Dialog Lab: PASS (exit {ExitPassed})"
                : summary.ExitCode == ExitThresholdMissed
                    ? $"Dialog Lab: FAIL, {missed} threshold{(missed == 1 ? "" : "s")} missed (exit {ExitThresholdMissed})"
                    : $"Dialog Lab: ERROR, {failure} (exit {ExitRunFailed})");

            summary.Json = "{"
                + $"\"format\":{TryllDialogLabBatchConfig.Format},\"status\":\"{status}\",\"exit_code\":{summary.ExitCode},"
                + $"\"run_id\":{TryllJsonWriter.Quote(run?.Snapshot?.RunId ?? "")},"
                + $"\"stats\":{(stats != null ? stats.ToJson() : "null")},"
                + $"\"thresholds\":[{string.Join(",", checks)}],"
                + $"\"issues\":[{string.Join(",", issues.Select(TryllJsonWriter.Quote))}]"
                + "}";
            return summary;
        }

        /// <summary>Null when every generated turn ran and none errored.</summary>
        internal static string InfrastructureFailure(TryllDialogLabStatistics stats)
        {
            int errors = stats.Variants.Sum(v => v.Errors);
            int notRun = stats.Variants.Sum(v => v.NotRun);
            if (errors == 0 && notRun == 0) return null;
            return $"{errors} generated turn(s) ended in an error and {notRun} did not run; "
                 + "the results are incomplete (see the other issues).";
        }

        static string Num(double x) => TryllDialogLabStatistics.FormatNumber(x);
    }
}
