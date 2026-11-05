// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tryll.Client.Editor
{
    internal enum TryllIntentKind { Embedding, Llm }

    internal sealed class TryllIntentCandidate
    {
        public int Rank;
        public string Intent = "";
        public double? Distance;
    }

    internal sealed class TryllIntentLabelProb
    {
        public string Label = "";
        public double Prob;
        public int SourceIndex;
    }

    /// <summary>
    /// One classifier execution, normalised from the two node shapes so the
    /// inspector and the intent checks share one parse.
    /// </summary>
    internal sealed class TryllIntentExecution
    {
        public int NodeIndex;
        public string NodeName = "";
        public TryllIntentKind Kind;
        public string ExitRoute = "";
        public bool Accepted;
        public string Intent = "";
        public string RejectedIntent = "";
        public string NotFoundReason = "";
        public string Query = "";

        public double? Distance, RunnerUpDistance, Margin, Threshold;
        public string RunnerUpIntent = "";
        public string RecordId = "";
        public string RecordText = "";
        public int? RecordIndex;
        public List<TryllIntentCandidate> Topk = new List<TryllIntentCandidate>();

        public double? TopProb, SecondProb, MarginResult, MarginThreshold;
        public string PredictedLetter = "";
        public string ModelName = "";
        public int? HistoryTurns;
        public List<TryllIntentLabelProb> Probs = new List<TryllIntentLabelProb>();
        public bool ProbsLengthMismatch;
        public int ProbsCount, IntentsIdsCount;
        public List<string> IntentsIds = new List<string>();

        public string StorageName = "";
        public string MetadataField = "";
    }

    /// <summary>
    /// Pure parsing/presentation helpers for ClassifyIntent* diagnostics.
    /// UIToolkit-free so editor tests and the checker share one definition of
    /// "which label did this execution pick".
    /// </summary>
    internal static class TryllDialogLabIntent
    {
        public const string TypeEmbed = "ClassifyIntentNode";
        public const string TypeLlm = "ClassifyIntentLLMNode";

        public static bool IsClassifierType(string type) =>
            type == TypeEmbed || type == TypeLlm;

        public static string FormatDistance(double? value) =>
            value.HasValue
                ? value.Value.ToString("0.0000", CultureInfo.InvariantCulture)
                : "—";

        public static string FormatProb(double? value) =>
            value.HasValue
                ? value.Value.ToString("0.000", CultureInfo.InvariantCulture)
                : "—";

        /// <summary>
        /// The label this execution picked. Accepted route uses
        /// <see cref="TryllIntentExecution.Intent"/>; rejected uses
        /// <see cref="TryllIntentExecution.RejectedIntent"/> (A1 + fallbacks).
        /// A non-empty label on a non-found route is never treated as accepted.
        /// </summary>
        public static string ResolveLabel(TryllIntentExecution execution)
        {
            if (execution == null) return "";
            if (execution.Accepted) return execution.Intent ?? "";
            return execution.RejectedIntent ?? "";
        }

        public static string FormatHeader(IReadOnlyList<TryllIntentExecution> executions)
        {
            if (executions == null || executions.Count == 0) return "";
            if (executions.Count == 1)
            {
                var e = executions[0];
                if (e.Accepted)
                {
                    var label = string.IsNullOrEmpty(e.Intent) ? ResolveLabel(e) : e.Intent;
                    return string.IsNullOrEmpty(label) ? "found" : label + " · found";
                }
                if (!string.IsNullOrEmpty(e.NotFoundReason))
                    return "not_found · " + e.NotFoundReason;
                return "not_found";
            }

            int classified = 0;
            for (int i = 0; i < executions.Count; i++)
            {
                if (executions[i].Accepted) classified++;
            }
            return executions.Count + " nodes · " + classified + " classified";
        }

        public static string FormatEmptyMessage(TryllIntentExecution execution)
        {
            var reason = execution?.NotFoundReason ?? "";
            if (reason == "empty_message")
                return "no query text to classify";
            if (reason == "no_results")
                return "the storage returned no hits";
            if (reason == "score_failed")
                return "scoring failed";
            if (!string.IsNullOrEmpty(reason))
                return reason;
            return "no classification candidates";
        }

        public static string FormatMismatchWarning(TryllIntentExecution execution)
        {
            if (execution == null || !execution.ProbsLengthMismatch) return "";
            return "probs/intents_ids length mismatch ("
                 + execution.ProbsCount.ToString(CultureInfo.InvariantCulture)
                 + " vs "
                 + execution.IntentsIdsCount.ToString(CultureInfo.InvariantCulture)
                 + ") — labels may be misaligned";
        }

        public static string FormatEmbedSubHeader(TryllIntentExecution e)
        {
            var query = string.IsNullOrEmpty(e.Query) ? "(no query)" : e.Query;
            var sb = new StringBuilder();
            sb.Append(query);
            sb.Append(" → ");
            sb.Append(string.IsNullOrEmpty(e.StorageName) ? "storage" : e.StorageName);
            if (!string.IsNullOrEmpty(e.MetadataField))
            {
                sb.Append(" · ");
                sb.Append(e.MetadataField);
            }
            sb.Append(" · threshold ");
            sb.Append(e.Threshold.HasValue
                ? e.Threshold.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : "—");
            return sb.ToString();
        }

        public static string FormatLlmSubHeader(TryllIntentExecution e)
        {
            var sb = new StringBuilder();
            sb.Append(string.IsNullOrEmpty(e.ModelName) ? "model" : e.ModelName);
            sb.Append(" · threshold ");
            sb.Append(e.Threshold.HasValue
                ? e.Threshold.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : "—");
            sb.Append(" · margin ");
            sb.Append(e.MarginThreshold.HasValue
                ? e.MarginThreshold.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : "—");
            sb.Append(" · history_turns ");
            sb.Append(e.HistoryTurns.HasValue
                ? e.HistoryTurns.Value.ToString(CultureInfo.InvariantCulture)
                : "—");
            return sb.ToString();
        }

        public static List<TryllIntentExecution> ParseExecutions(
            TryllJsonValue debugInfo,
            IEnumerable<(int Index, TryllJsonValue Node)> scopedNodes)
        {
            var list = new List<TryllIntentExecution>();
            if (scopedNodes == null) return list;
            foreach (var (index, node) in scopedNodes)
            {
                if (node == null || node.IsNull) continue;
                var type = node.Get("_type").AsString("");
                if (!IsClassifierType(type)) continue;
                list.Add(ParseOne(index, node));
            }
            return list;
        }

        public static TryllIntentExecution ParseOne(int nodeIndex, TryllJsonValue node)
        {
            var e = new TryllIntentExecution
            {
                NodeIndex = nodeIndex,
                NodeName = node.Get("name").AsString(""),
                ExitRoute = node.Get("exit_route").AsString(""),
            };
            e.Accepted = e.ExitRoute == "found";

            var type = node.Get("_type").AsString("");
            e.Kind = type == TypeLlm ? TryllIntentKind.Llm : TryllIntentKind.Embedding;

            var diag = node.Get("diagnostics");
            var parameters = diag.Get("parameters");
            var classification = diag.Get("output").Get("classification");
            var input = diag.Get("input");

            e.Query = input.Get("query").AsString("");
            if (string.IsNullOrEmpty(e.Query))
                e.Query = classification.Get("query").AsString("");

            e.NotFoundReason = classification.Get("not_found_reason").AsString("");
            e.Threshold = ReadDouble(parameters.Get("threshold"));

            if (e.Kind == TryllIntentKind.Embedding)
                ParseEmbed(e, parameters, classification);
            else
                ParseLlm(e, parameters, classification);

            if (e.Accepted)
            {
                if (string.IsNullOrEmpty(e.Intent))
                    e.Intent = e.Kind == TryllIntentKind.Embedding
                        ? classification.Get("winner").Get("intent").AsString("")
                        : classification.Get("intent").AsString("");
            }
            else
            {
                e.Intent = "";
                if (string.IsNullOrEmpty(e.RejectedIntent))
                    e.RejectedIntent = RecoverRejected(e, parameters, classification);
            }

            return e;
        }

        public static List<string> AttachedIntents(TryllJsonValue debugInfo)
        {
            var result = new List<string>();
            if (debugInfo == null || debugInfo.IsNull) return result;
            var components = debugInfo.Get("interaction").Get("components");
            if (components.IsNull) return result;
            foreach (var c in components.AsArray())
            {
                if (c.Get("_type").AsString("") != "IntentionComponent") continue;
                var intent = c.Get("intent").AsString("");
                if (!string.IsNullOrEmpty(intent)) result.Add(intent);
            }
            return result;
        }

        static void ParseEmbed(
            TryllIntentExecution e,
            TryllJsonValue parameters,
            TryllJsonValue classification)
        {
            e.StorageName = parameters.Get("embedded_string_storage").AsString("");
            e.MetadataField = parameters.Get("metadata_field").AsString("");

            var winner = classification.Get("winner");
            if (!winner.IsNull)
            {
                e.Intent = winner.Get("intent").AsString("");
                e.RecordId = winner.Get("id").AsString("");
                e.RecordText = winner.Get("text").AsString("");
                e.Distance = ReadDouble(winner.Get("distance"));
                var idx = winner.Get("index");
                if (!idx.IsNull && idx.Kind == TryllJsonKind.Number)
                    e.RecordIndex = idx.AsInt();
            }

            var runner = classification.Get("runner_up");
            if (!runner.IsNull)
            {
                e.RunnerUpIntent = runner.Get("intent").AsString("");
                e.RunnerUpDistance = ReadDouble(runner.Get("distance"));
                e.Margin = ReadDouble(runner.Get("margin"));
            }

            foreach (var row in classification.Get("topk").AsArray())
            {
                if (row == null || row.IsNull) continue;
                e.Topk.Add(new TryllIntentCandidate
                {
                    Rank = row.Get("rank").AsInt(),
                    Intent = row.Get("intent").AsString(""),
                    Distance = ReadDouble(row.Get("distance")),
                });
            }

            e.RejectedIntent = classification.Get("rejected_intent").AsString("");
            var rejectedDist = ReadDouble(classification.Get("rejected_distance"));
            if (!e.Distance.HasValue) e.Distance = rejectedDist;
            if (!e.Distance.HasValue && e.Topk.Count > 0) e.Distance = e.Topk[0].Distance;
        }

        static void ParseLlm(
            TryllIntentExecution e,
            TryllJsonValue parameters,
            TryllJsonValue classification)
        {
            e.ModelName = parameters.Get("model_name").AsString("");
            e.MarginThreshold = ReadDouble(parameters.Get("margin"));
            var ht = parameters.Get("history_turns");
            if (!ht.IsNull && ht.Kind == TryllJsonKind.Number)
                e.HistoryTurns = ht.AsInt();

            e.Intent = classification.Get("intent").AsString("");
            e.RejectedIntent = classification.Get("rejected_intent").AsString("");
            e.PredictedLetter = classification.Get("predicted_letter").AsString("");
            e.TopProb = ReadDouble(classification.Get("top_prob"));
            e.SecondProb = ReadDouble(classification.Get("second_prob"));
            e.MarginResult = ReadDouble(classification.Get("margin_result"));

            foreach (var id in parameters.Get("intents_ids").AsArray())
                e.IntentsIds.Add(id.AsString(""));

            var rawProbs = new List<double>();
            foreach (var p in classification.Get("probs").AsArray())
            {
                if (p == null || p.IsNull || p.Kind != TryllJsonKind.Number) continue;
                rawProbs.Add(p.AsDouble());
            }

            e.ProbsCount = rawProbs.Count;
            e.IntentsIdsCount = e.IntentsIds.Count;
            e.ProbsLengthMismatch = e.ProbsCount > 0 && e.IntentsIdsCount > 0
                && e.ProbsCount != e.IntentsIdsCount;

            int common = Math.Min(
                e.ProbsCount > 0 ? e.ProbsCount : 0,
                e.IntentsIdsCount > 0 ? e.IntentsIdsCount : e.ProbsCount);
            // When ids are missing, still surface the numeric bars with blank labels.
            if (e.IntentsIdsCount == 0) common = e.ProbsCount;
            if (e.ProbsCount == 0) common = 0;

            for (int i = 0; i < common; i++)
            {
                e.Probs.Add(new TryllIntentLabelProb
                {
                    Label = i < e.IntentsIds.Count ? e.IntentsIds[i] : "",
                    Prob = rawProbs[i],
                    SourceIndex = i,
                });
            }

            // Descending probability; ties keep intents_ids order (SourceIndex).
            e.Probs.Sort((a, b) =>
            {
                int cmp = b.Prob.CompareTo(a.Prob);
                return cmp != 0 ? cmp : a.SourceIndex.CompareTo(b.SourceIndex);
            });
        }

        static string RecoverRejected(
            TryllIntentExecution e,
            TryllJsonValue parameters,
            TryllJsonValue classification)
        {
            var rejected = classification.Get("rejected_intent").AsString("");
            if (!string.IsNullOrEmpty(rejected)) return rejected;

            var topk0 = classification.Get("topk").AsArray();
            if (topk0.Count > 0)
            {
                var intent = topk0[0].Get("intent").AsString("");
                if (!string.IsNullOrEmpty(intent)) return intent;
            }

            int letterIdx = LetterIndex(classification.Get("predicted_letter").AsString(""));
            var ids = parameters.Get("intents_ids").AsArray();
            if (letterIdx >= 0 && letterIdx < ids.Count)
            {
                var mapped = ids[letterIdx].AsString("");
                if (!string.IsNullOrEmpty(mapped)) return mapped;
            }

            double best = double.NegativeInfinity;
            int bestI = -1;
            var probs = classification.Get("probs").AsArray();
            for (int i = 0; i < probs.Count; i++)
            {
                if (probs[i].Kind != TryllJsonKind.Number) continue;
                var p = probs[i].AsDouble();
                if (p > best) { best = p; bestI = i; }
            }
            if (bestI >= 0 && bestI < ids.Count)
                return ids[bestI].AsString("");
            return "";
        }

        static int LetterIndex(string letter)
        {
            if (string.IsNullOrEmpty(letter) || letter.Length != 1) return -1;
            char c = char.ToUpperInvariant(letter[0]);
            if (c < 'A' || c > 'Z') return -1;
            return c - 'A';
        }

        static double? ReadDouble(TryllJsonValue n)
        {
            if (n == null || n.IsNull || n.Kind != TryllJsonKind.Number) return null;
            return n.AsDouble();
        }
    }
}
