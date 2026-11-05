// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// A pass rate: <see cref="Passed"/> of <see cref="Total"/>, pooled over every seed, plus
    /// the per-seed rates its interval is computed from. Seeds, not turns, are the
    /// independent unit: turns from one seed share a conversation and fail together, so an
    /// interval over all turns would come out several times too narrow.
    /// </summary>
    internal sealed class TryllLabRate
    {
        public int Passed;
        public int Total;
        public List<double> PerSeed = new List<double>();

        public double Rate => Total > 0 ? (double)Passed / Total : 0;

        /// <summary>Standard error of the mean of the per-seed rates; 0 with fewer than 2 seeds.</summary>
        public double StandardError
        {
            get
            {
                int n = PerSeed.Count;
                if (n < 2) return 0;
                double mean = PerSeed.Average();
                double var = PerSeed.Sum(x => (x - mean) * (x - mean)) / (n - 1);
                return Math.Sqrt(var / n);
            }
        }

        /// <summary>Half-width of the 95% t-interval (df = seeds − 1).</summary>
        public double HalfWidth => TryllDialogLabStatistics.TCritical95(PerSeed.Count - 1) * StandardError;
        public double Low => Math.Max(0, Rate - HalfWidth);
        public double High => Math.Min(1, Rate + HalfWidth);
    }

    /// <summary>One variant's headline numbers.</summary>
    internal sealed class TryllLabVariantStats
    {
        public int VariantIndex;
        public string Label = "";
        public Color Swatch;
        public int Seeds;

        /// <summary>Generated turns whose checks all passed. Errors and not-run turns count
        /// as not passed. Seeded turns are excluded.</summary>
        public TryllLabRate Turns = new TryllLabRate();
        /// <summary>Authored checks on generated turns. The built-in "no unexpected tool call"
        /// gate fails turns but is not an authored check, so it is not counted here.</summary>
        public TryllLabRate Checks = new TryllLabRate();
        /// <summary>Dialogue × seed runs in which every generated turn passed.</summary>
        public TryllLabRate CleanDialogues = new TryllLabRate();

        public int Errors;
        public int NotRun;
        public double? MedianWords, P90Words, MedianTtftMs, P90TtftMs;
    }

    /// <summary>One generated turn's failures per variant: failed seeds of seeds.</summary>
    internal sealed class TryllLabTurnFailRow
    {
        public int DialogueIndex;
        public int StepIndex;
        /// <summary>Index into the sample's flat turn list (and the dataset's Turns).</summary>
        public int FlatIndex;
        public string User = "";
        public int[] Failed;
        public int[] Seeds;
    }

    /// <summary>One check type's failures per variant: failed of evaluated.</summary>
    internal sealed class TryllLabCheckTypeRow
    {
        public string CheckType = "";
        public int[] Failed;
        public int[] Evaluated;
    }

    /// <summary>
    /// Per-variant statistics for the Lab's Statistics view and the export header. Pure: a
    /// function of a run result and a dialogue filter, so the view, the export and (later)
    /// headless mode report the same numbers.
    /// </summary>
    internal sealed class TryllDialogLabStatistics
    {
        /// <summary>Below this many seeds per variant an interval is too wide to mean
        /// anything, so none is shown.</summary>
        public const int MinSeedsForInterval = 5;

        public List<TryllLabVariantStats> Variants = new List<TryllLabVariantStats>();
        public List<TryllLabTurnFailRow> WhereTurnsFail = new List<TryllLabTurnFailRow>();
        public List<TryllLabCheckTypeRow> ByCheckType = new List<TryllLabCheckTypeRow>();
        /// <summary>The filter this was computed for; null = every dialogue.</summary>
        public int? Dialogue;

        /// <summary>Smallest seed count over the variants — what gates the intervals.</summary>
        public int MinSeeds => Variants.Count == 0 ? 0 : Variants.Min(v => v.Seeds);
        public bool IntervalsAvailable => MinSeeds >= MinSeedsForInterval;

        public static TryllDialogLabStatistics Compute(TryllDialogLabRunResult run, int? dialogue = null)
        {
            var stats = new TryllDialogLabStatistics { Dialogue = dialogue };
            var snapshot = run?.Snapshot;
            if (snapshot == null) return stats;

            bool InFilter(int d) => dialogue == null || dialogue.Value == d;
            int variantCount = snapshot.Variants.Count;
            var byVariant = Enumerable.Range(0, variantCount)
                .Select(vi => run.Samples.Where(s => s.VariantIndex == vi).ToList())
                .ToList();

            for (int vi = 0; vi < variantCount; vi++)
            {
                var v = snapshot.Variants[vi];
                var vs = new TryllLabVariantStats
                {
                    VariantIndex = vi,
                    Label = v.Label ?? "",
                    Swatch = v.Swatch,
                    Seeds = byVariant[vi].Count,
                };
                var words = new List<double>();
                var ttft = new List<double>();

                foreach (var sample in byVariant[vi])
                {
                    int tk = 0, tn = 0, ck = 0, cn = 0, dk = 0, dn = 0;
                    foreach (var group in sample.Turns.Where(t => InFilter(t.DialogueIndex)).GroupBy(t => t.DialogueIndex))
                    {
                        var generated = group.Where(t => !t.Seeded).ToList();
                        if (generated.Count == 0) continue;
                        bool clean = true;
                        foreach (var t in generated)
                        {
                            tn++;
                            if (t.Status == TryllLabCellStatus.Ok) tk++;
                            else clean = false;
                            if (t.Status == TryllLabCellStatus.Error) vs.Errors++;
                            if (t.Status == TryllLabCellStatus.Skipped) vs.NotRun++;
                            foreach (var o in t.CheckOutcomes ?? new List<TryllLabCheckOutcome>())
                            {
                                if (o.Scope == TryllLabCheckScope.Gate) continue;
                                cn++;
                                if (o.Passed) ck++;
                            }
                            if (t.Status == TryllLabCellStatus.Ok || t.Status == TryllLabCellStatus.CheckFail)
                            {
                                words.Add(t.Words);
                                if (t.TtftMs > 0) ttft.Add(t.TtftMs);
                            }
                        }
                        dn++;
                        if (clean) dk++;
                    }
                    Add(vs.Turns, tk, tn);
                    Add(vs.Checks, ck, cn);
                    Add(vs.CleanDialogues, dk, dn);
                }

                vs.MedianWords = Percentile(words, 0.5);
                vs.P90Words = Percentile(words, 0.9);
                vs.MedianTtftMs = Percentile(ttft, 0.5);
                vs.P90TtftMs = Percentile(ttft, 0.9);
                stats.Variants.Add(vs);
            }

            // Where turns fail: one row per generated turn in the filter, read from any
            // sample (every sample has the same rectangular turn list).
            var template = run.Samples.FirstOrDefault();
            if (template != null)
            {
                for (int g = 0; g < template.Turns.Count; g++)
                {
                    var t = template.Turns[g];
                    if (t.Seeded || !InFilter(t.DialogueIndex)) continue;
                    var row = new TryllLabTurnFailRow
                    {
                        DialogueIndex = t.DialogueIndex,
                        StepIndex = t.TurnIndex,
                        FlatIndex = g,
                        User = t.UserText ?? "",
                        Failed = new int[variantCount],
                        Seeds = new int[variantCount],
                    };
                    for (int vi = 0; vi < variantCount; vi++)
                    {
                        foreach (var sample in byVariant[vi])
                        {
                            if (g >= sample.Turns.Count) continue;
                            row.Seeds[vi]++;
                            if (sample.Turns[g].Status != TryllLabCellStatus.Ok) row.Failed[vi]++;
                        }
                    }
                    stats.WhereTurnsFail.Add(row);
                }
            }

            // Failing checks by type, the gate included: it is the one failure that has
            // no authored row, so this is the only place its count shows.
            var types = new Dictionary<string, TryllLabCheckTypeRow>(StringComparer.Ordinal);
            for (int vi = 0; vi < variantCount; vi++)
            {
                foreach (var sample in byVariant[vi])
                foreach (var t in sample.Turns)
                {
                    if (t.Seeded || !InFilter(t.DialogueIndex)) continue;
                    foreach (var o in t.CheckOutcomes ?? new List<TryllLabCheckOutcome>())
                    {
                        string type = string.IsNullOrEmpty(o.CheckType) ? o.Kind ?? "" : o.CheckType;
                        if (!types.TryGetValue(type, out var row))
                            types[type] = row = new TryllLabCheckTypeRow
                            {
                                CheckType = type,
                                Failed = new int[variantCount],
                                Evaluated = new int[variantCount],
                            };
                        row.Evaluated[vi]++;
                        if (!o.Passed) row.Failed[vi]++;
                    }
                }
            }
            stats.ByCheckType = types.Values.OrderBy(r => r.CheckType, StringComparer.Ordinal).ToList();
            return stats;
        }

        /// <summary>
        /// Variant <paramref name="vi"/> minus variant 0, in rate units, with the half-width
        /// of a 95% interval on the difference: Welch's standard error of the two per-seed
        /// means, at the smaller of the two variants' degrees of freedom (conservative).
        /// </summary>
        public (double difference, double halfWidth) DifferenceFromBaseline(
            Func<TryllLabVariantStats, TryllLabRate> metric, int vi)
        {
            var a = metric(Variants[vi]);
            var b = metric(Variants[0]);
            double se = Math.Sqrt(a.StandardError * a.StandardError + b.StandardError * b.StandardError);
            int df = Math.Min(a.PerSeed.Count, b.PerSeed.Count) - 1;
            return (a.Rate - b.Rate, TCritical95(df) * se);
        }

        static void Add(TryllLabRate rate, int passed, int total)
        {
            rate.Passed += passed;
            rate.Total += total;
            if (total > 0) rate.PerSeed.Add((double)passed / total);
        }

        /// <summary>Nearest-rank percentile; null for no data.</summary>
        static double? Percentile(List<double> values, double p)
        {
            if (values.Count == 0) return null;
            var sorted = values.OrderBy(x => x).ToList();
            int rank = (int)Math.Ceiling(p * sorted.Count);
            return sorted[Mathf.Clamp(rank - 1, 0, sorted.Count - 1)];
        }

        static readonly double[] T95 =
        {
            double.NaN, 12.706, 4.303, 3.182, 2.776, 2.571, 2.447, 2.365, 2.306, 2.262, 2.228,
            2.201, 2.179, 2.160, 2.145, 2.131, 2.120, 2.110, 2.101, 2.093, 2.086,
            2.080, 2.074, 2.069, 2.064, 2.060, 2.056, 2.052, 2.048, 2.045, 2.042,
        };

        /// <summary>Two-sided 95% Student-t critical value; 0 for df &lt; 1 (no interval).</summary>
        public static double TCritical95(int df) =>
            df < 1 ? 0 : df < T95.Length ? T95[df] : 1.96 + 2.4 / df;

        // ── JSON for the export header ─────────────────────────────────────────

        /// <summary>"0.####": at most four decimals, trailing zeros and point dropped. The
        /// export header and the headless summary both write numbers this way.</summary>
        internal static string FormatNumber(double x) =>
            x.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

        public string ToJson()
        {
            string Num(double x) => FormatNumber(x);
            string Opt(double? x) => x.HasValue ? Num(x.Value) : "null";
            string RateJson(TryllLabRate r) =>
                $"{{\"passed\":{r.Passed},\"total\":{r.Total},\"rate\":{Num(r.Rate)}"
                + (IntervalsAvailable ? $",\"ci95\":[{Num(r.Low)},{Num(r.High)}]" : "")
                + "}";
            var variants = Variants.Select(v =>
                "{"
                + $"\"index\":{v.VariantIndex},\"label\":{Tryll.Client.Internal.TryllJsonWriter.Quote(v.Label)},\"seeds\":{v.Seeds},"
                + $"\"turns_passed\":{RateJson(v.Turns)},\"checks_passed\":{RateJson(v.Checks)},"
                + $"\"clean_dialogues\":{RateJson(v.CleanDialogues)},"
                + $"\"errors\":{v.Errors},\"not_run\":{v.NotRun},"
                + $"\"words_median\":{Opt(v.MedianWords)},\"words_p90\":{Opt(v.P90Words)},"
                + $"\"ttft_ms_median\":{Opt(v.MedianTtftMs)},\"ttft_ms_p90\":{Opt(v.P90TtftMs)}"
                + "}");
            return "{"
                 + $"\"interval\":\"t95 over per-seed rates\",\"min_seeds_for_interval\":{MinSeedsForInterval},"
                 + $"\"variants\":[{string.Join(",", variants)}]"
                 + "}";
        }
    }
}
