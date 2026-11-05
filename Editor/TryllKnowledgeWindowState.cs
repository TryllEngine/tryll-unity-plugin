// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    public enum TryllKnowledgeBrowseMode
    {
        Filter = 0,
        Query = 1,
    }

    /// <summary>
    /// One row in the Knowledge records pane: a listed record or a query hit.
    /// </summary>
    public readonly struct TryllKnowledgeVisibleRow
    {
        public readonly string Id;
        public readonly string Text;
        public readonly float? Distance;

        public TryllKnowledgeVisibleRow(string id, string text, float? distance = null)
        {
            Id = id ?? "";
            Text = text ?? "";
            Distance = distance;
        }
    }

    /// <summary>
    /// Testable Knowledge-window reducer. Owns the last-good store list, records,
    /// query hits, and banners. Network/UI stay outside so tests can drive refresh
    /// and destructive confirmation without opening editor dialogs.
    /// </summary>
    public sealed class TryllKnowledgeWindowState
    {
        public const uint ListLimit = 1024;

        public int RefreshGeneration { get; private set; }
        public List<TryllMutableKbInfo> Stores { get; private set; } = new List<TryllMutableKbInfo>();
        public string SelectedName { get; private set; } = "";
        public TryllMutableKbListPage Page { get; private set; } = new TryllMutableKbListPage();
        public TryllMutableKbStats Stats { get; private set; }
        public string Filter { get; set; } = "";
        public string SelectedRecordId { get; set; } = "";
        public string StaleBanner { get; private set; } = "";
        public List<string> Warnings { get; } = new List<string>();
        public bool HasLastGoodView { get; private set; }
        public bool ListTruncated { get; private set; }
        public TryllKnowledgeBrowseMode Mode { get; private set; } = TryllKnowledgeBrowseMode.Filter;
        public List<TryllMutableKbHit> QueryHits { get; private set; } = new List<TryllMutableKbHit>();

        public IReadOnlyList<TryllKnowledgeVisibleRow> VisibleRows
        {
            get
            {
                var rows = new List<TryllKnowledgeVisibleRow>();
                if (Mode == TryllKnowledgeBrowseMode.Query)
                {
                    foreach (var hit in QueryHits)
                    {
                        if (hit == null) continue;
                        rows.Add(new TryllKnowledgeVisibleRow(hit.Id, hit.Text, hit.Distance));
                    }
                    return rows;
                }

                var records = Page?.Records ?? new List<TryllMutableKbRecord>();
                foreach (var rec in records)
                {
                    if (rec == null) continue;
                    if (!string.IsNullOrEmpty(Filter))
                    {
                        bool idHit = rec.Id != null
                            && rec.Id.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool textHit = rec.Text != null
                            && rec.Text.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!idHit && !textHit) continue;
                    }
                    rows.Add(new TryllKnowledgeVisibleRow(rec.Id, rec.Text));
                }
                return rows;
            }
        }

        public int BeginRefresh()
        {
            return ++RefreshGeneration;
        }

        public bool IsCurrent(int generation) => generation == RefreshGeneration;

        public void ApplyEnumerate(IReadOnlyList<TryllMutableKbInfo> stores)
        {
            Stores = stores == null ? new List<TryllMutableKbInfo>() : new List<TryllMutableKbInfo>(stores);
            HasLastGoodView = true;
            StaleBanner = "";
            if (!string.IsNullOrEmpty(SelectedName))
            {
                bool found = false;
                foreach (var store in Stores)
                {
                    if (store != null && store.Name == SelectedName)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    AddWarning($"Store '{SelectedName}' is no longer present.");
            }
            else if (Stores.Count > 0 && !string.IsNullOrEmpty(Stores[0].Name))
            {
                SelectedName = Stores[0].Name;
            }
        }

        public void SelectStore(string name)
        {
            if (SelectedName == name) return;
            SelectedName = name ?? "";
            SelectedRecordId = "";
            Page = new TryllMutableKbListPage();
            Stats = null;
            ListTruncated = false;
            QueryHits = new List<TryllMutableKbHit>();
        }

        public void ApplyList(TryllMutableKbListPage page, TryllMutableKbStats stats)
        {
            Page = page ?? new TryllMutableKbListPage();
            Stats = stats;
            ListTruncated = Page.HasMore;
            HasLastGoodView = true;
            StaleBanner = "";
            PruneSelectionAgainstVisible();
        }

        public void SetMode(TryllKnowledgeBrowseMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            if (mode == TryllKnowledgeBrowseMode.Filter)
                QueryHits = new List<TryllMutableKbHit>();
            PruneSelectionAgainstVisible();
        }

        public void ApplyQueryHits(IReadOnlyList<TryllMutableKbHit> hits)
        {
            Mode = TryllKnowledgeBrowseMode.Query;
            QueryHits = hits == null
                ? new List<TryllMutableKbHit>()
                : new List<TryllMutableKbHit>(hits);
            HasLastGoodView = true;
            StaleBanner = "";
            PruneSelectionAgainstVisible();
        }

        public void MarkStale(string message)
        {
            StaleBanner = message ?? "Refresh failed";
        }

        public void AddWarning(string warning)
        {
            if (string.IsNullOrEmpty(warning)) return;
            Warnings.Add(warning);
        }

        public void ClearWarnings() => Warnings.Clear();

        public static bool IsDestructiveClearConfirmed(bool userConfirmed) => userConfirmed;

        public static bool ShouldSurfaceBusy(TryllError error)
            => error.Code == (int)TryllErrorCode.StorageBusy;

        public static string FormatWarning(TryllError error, IReadOnlyList<string> extra = null)
        {
            var parts = new List<string>();
            if (!error.IsOk && !string.IsNullOrEmpty(error.Message))
                parts.Add(error.Message);
            if (extra != null)
            {
                foreach (var w in extra)
                {
                    if (!string.IsNullOrEmpty(w))
                        parts.Add(w);
                }
            }
            return string.Join("\n", parts);
        }

        void PruneSelectionAgainstVisible()
        {
            if (string.IsNullOrEmpty(SelectedRecordId)) return;
            foreach (var row in VisibleRows)
            {
                if (row.Id == SelectedRecordId) return;
            }
            SelectedRecordId = "";
        }
    }
}
