// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor.AgentLog
{
    internal enum AgentLogTurnCollapseOverride : byte
    {
        None = 0,
        ForcedExpanded = 1,
        ForcedCollapsed = 2,
    }

    /// <summary>
    /// Session-local per-turn collapse overrides. "Collapse completed turns" only
    /// affects turns the user has not manually toggled.
    /// </summary>
    internal sealed class AgentLogTurnCollapseState
    {
        readonly Dictionary<TryllTurnKey, AgentLogTurnCollapseOverride> _overrides =
            new Dictionary<TryllTurnKey, AgentLogTurnCollapseOverride>();

        public void Clear() => _overrides.Clear();

        public AgentLogTurnCollapseOverride GetOverride(TryllTurnKey key)
        {
            if (!key.HasValue) return AgentLogTurnCollapseOverride.None;
            return _overrides.TryGetValue(key, out var value)
                ? value
                : AgentLogTurnCollapseOverride.None;
        }

        public bool IsCollapsed(TryllTurnKey key, bool turnComplete, bool collapseCompletedTurns)
        {
            if (!key.HasValue) return false;
            switch (GetOverride(key))
            {
            case AgentLogTurnCollapseOverride.ForcedExpanded:
                return false;
            case AgentLogTurnCollapseOverride.ForcedCollapsed:
                return true;
            default:
                return collapseCompletedTurns && turnComplete;
            }
        }

        /// <summary>Chevron toggle: flip current visual state into a sticky override.</summary>
        public void Toggle(TryllTurnKey key, bool currentlyCollapsed)
        {
            if (!key.HasValue) return;
            _overrides[key] = currentlyCollapsed
                ? AgentLogTurnCollapseOverride.ForcedExpanded
                : AgentLogTurnCollapseOverride.ForcedCollapsed;
        }
    }

    internal static class AgentLogTurnCollapse
    {
        /// <summary>
        /// Filter projected rows: keep headers of collapsed turns, hide body rows,
        /// and annotate headers with a body-count suffix.
        /// </summary>
        public static List<AgentLogRow> Apply(
            IReadOnlyList<AgentLogRow> rows,
            AgentLogTurnCollapseState state,
            bool collapseCompletedTurns)
        {
            var output = new List<AgentLogRow>();
            if (rows == null || rows.Count == 0 || state == null)
            {
                if (rows != null)
                {
                    for (int i = 0; i < rows.Count; ++i)
                        output.Add(rows[i]);
                }
                return output;
            }

            int index = 0;
            while (index < rows.Count)
            {
                var row = rows[index];
                if (row == null || !row.TurnKey.HasValue)
                {
                    output.Add(AnnotateNonCollapsible(row));
                    ++index;
                    continue;
                }

                int start = index;
                int end = index + 1;
                while (end < rows.Count
                       && rows[end] != null
                       && rows[end].TurnKey.HasValue
                       && rows[end].TurnKey.Equals(row.TurnKey))
                {
                    ++end;
                }

                int count = end - start;
                bool complete = false;
                for (int i = start; i < end; ++i)
                {
                    if (rows[i] != null && rows[i].IsTurnTerminal)
                    {
                        complete = true;
                        break;
                    }
                }

                bool canCollapse = count > 1;
                bool collapsed = canCollapse
                    && state.IsCollapsed(row.TurnKey, complete, collapseCompletedTurns);

                if (!canCollapse)
                {
                    output.Add(CloneWithCollapse(rows[start], canCollapse: false, collapsed: false, hiddenBody: 0));
                }
                else if (collapsed)
                {
                    output.Add(CloneWithCollapse(
                        rows[start],
                        canCollapse: true,
                        collapsed: true,
                        hiddenBody: count - 1));
                }
                else
                {
                    output.Add(CloneWithCollapse(rows[start], canCollapse: true, collapsed: false, hiddenBody: 0));
                    for (int i = start + 1; i < end; ++i)
                        output.Add(CloneWithCollapse(rows[i], canCollapse: false, collapsed: false, hiddenBody: 0));
                }

                index = end;
            }

            return output;
        }

        static AgentLogRow AnnotateNonCollapsible(AgentLogRow row)
        {
            if (row == null) return null;
            return CloneWithCollapse(row, canCollapse: false, collapsed: false, hiddenBody: 0);
        }

        static AgentLogRow CloneWithCollapse(
            AgentLogRow source,
            bool canCollapse,
            bool collapsed,
            int hiddenBody)
        {
            var summary = source.SummaryText ?? "";
            if (collapsed && hiddenBody > 0)
                summary = $"{summary}  ·  {hiddenBody + 1} events";

            return new AgentLogRow
            {
                EventId = source.EventId,
                Kind = source.Kind,
                TurnKey = source.TurnKey,
                AgentId = source.AgentId,
                WallTimeUtc = source.WallTimeUtc,
                GutterMarker = source.GutterMarker,
                SummaryText = summary,
                IsFirstRowOfTurn = source.IsFirstRowOfTurn,
                IsTurnTerminal = source.IsTurnTerminal,
                Source = source.Source,
                CorrelatedAck = source.CorrelatedAck,
                DeferredVariableUpdates = source.DeferredVariableUpdates,
                DeferredVariablesAck = source.DeferredVariablesAck,
                ShowAgentIdentityInGutter = source.ShowAgentIdentityInGutter,
                CanCollapseTurn = canCollapse,
                IsTurnCollapsed = collapsed,
                HiddenBodyCount = hiddenBody,
            };
        }
    }
}
