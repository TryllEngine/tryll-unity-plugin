// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Pure parsing/presentation helpers for RetrieveNode diagnostics in Dialog Lab.
    /// Kept free of UIToolkit so editor tests can cover Hybrid null-distance ordering.
    /// </summary>
    internal static class TryllDialogLabRetrieval
    {
        internal readonly struct HitRow
        {
            public readonly string Id;
            public readonly string Text;
            /// <summary>Null when the server emitted JSON null (lexical-only / no dense leg).</summary>
            public readonly double? Distance;
            public readonly double? Bm25Score;
            public readonly int? RankDense;
            public readonly int? RankSparse;

            public HitRow(
                string id,
                string text,
                double? distance,
                double? bm25Score,
                int? rankDense,
                int? rankSparse)
            {
                Id = id ?? "";
                Text = text ?? "";
                Distance = distance;
                Bm25Score = bm25Score;
                RankDense = rankDense;
                RankSparse = rankSparse;
            }

            public string FormatDistance() =>
                Distance.HasValue
                    ? Distance.Value.ToString("0.0000", CultureInfo.InvariantCulture)
                    : "—";
        }

        /// <summary>
        /// Parse <c>output.results[]</c> preserving server rank order. Null distance
        /// stays null (never coerced to 0.0).
        /// </summary>
        internal static List<HitRow> ParseHits(TryllJsonValue resultsArray)
        {
            var rows = new List<HitRow>();
            if (resultsArray == null || resultsArray.IsNull)
                return rows;

            foreach (var h in resultsArray.AsArray())
            {
                if (h == null || h.IsNull)
                    continue;
                var distNode = h.Get("distance");
                double? distance = distNode.IsNull || distNode.Kind != TryllJsonKind.Number
                    ? (double?)null
                    : distNode.AsDouble();

                double? bm25 = null;
                var bm25Node = h.Get("bm25_score");
                if (!bm25Node.IsNull && bm25Node.Kind == TryllJsonKind.Number)
                    bm25 = bm25Node.AsDouble();

                int? rankDense = null;
                var rd = h.Get("rank_dense");
                if (!rd.IsNull && rd.Kind == TryllJsonKind.Number)
                    rankDense = rd.AsInt();

                int? rankSparse = null;
                var rs = h.Get("rank_sparse");
                if (!rs.IsNull && rs.Kind == TryllJsonKind.Number)
                    rankSparse = rs.AsInt();

                rows.Add(new HitRow(
                    h.Get("id").AsString(""),
                    h.Get("text").AsString(""),
                    distance,
                    bm25,
                    rankDense,
                    rankSparse));
            }
            return rows;
        }

        internal static string FormatHeader(
            int resultCount,
            string retrievalMode,
            double threshold,
            int rrfK)
        {
            var mode = string.IsNullOrEmpty(retrievalMode) ? "Dense" : retrievalMode;
            var sb = new StringBuilder();
            sb.Append(resultCount);
            sb.Append(" results · ");
            sb.Append(mode);
            if (string.Equals(mode, "Hybrid", System.StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" · rrf_k ");
                sb.Append(rrfK.ToString(CultureInfo.InvariantCulture));
            }
            sb.Append(" · threshold ");
            sb.Append(threshold.ToString("0.00", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        internal static string FormatEmptyMessage(string retrievalMode)
        {
            var mode = string.IsNullOrEmpty(retrievalMode) ? "Dense" : retrievalMode;
            if (string.Equals(mode, "Lexical", System.StringComparison.OrdinalIgnoreCase))
                return "No BM25 hits → not_found exit.";
            if (string.Equals(mode, "Hybrid", System.StringComparison.OrdinalIgnoreCase))
                return "No fused hits → not_found exit.";
            return "No hit under threshold → not_found exit.";
        }

        internal static string FormatHitMeta(HitRow hit)
        {
            var parts = new List<string>();
            if (hit.Bm25Score.HasValue)
                parts.Add("bm25 " + hit.Bm25Score.Value.ToString("0.###", CultureInfo.InvariantCulture));
            if (hit.RankDense.HasValue)
                parts.Add("d#" + hit.RankDense.Value.ToString(CultureInfo.InvariantCulture));
            if (hit.RankSparse.HasValue)
                parts.Add("s#" + hit.RankSparse.Value.ToString(CultureInfo.InvariantCulture));
            return parts.Count == 0 ? "" : string.Join(" · ", parts);
        }
    }
}
