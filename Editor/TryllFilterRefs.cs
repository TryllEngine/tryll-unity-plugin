// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Lightweight regex-based scanner (not a full JSON parse) for the operand shapes the
    /// filter grammar uses to reference external state: <c>{"var": "&lt;name&gt;"}</c>
    /// (declared per-agent Variables, any Filter/Condition field) and
    /// <c>{"slot": "&lt;name&gt;"}</c> (turn-local slots — currently only Branch's
    /// Expression <c>Condition</c> compiles with slot capability enabled). Sufficient for
    /// validation and rename-propagation tooling since both operands are always a
    /// two-token key/string-literal pair — see <c>tryll/server/workflow/src/filter/Compiler.cpp</c>.
    /// </summary>
    internal static class TryllFilterRefs
    {
        private static readonly Regex s_varPattern =
            new Regex("\"var\"\\s*:\\s*\"([A-Za-z_][A-Za-z0-9_]*)\"", RegexOptions.Compiled);
        private static readonly Regex s_slotPattern =
            new Regex("\"slot\"\\s*:\\s*\"([A-Za-z_][A-Za-z0-9_]*)\"", RegexOptions.Compiled);

        /// <summary>Every <c>{"var": "&lt;name&gt;"}</c> operand name in <paramref name="filterJson"/>.</summary>
        public static IEnumerable<string> ExtractVarRefs(string filterJson)
        {
            if (string.IsNullOrEmpty(filterJson)) yield break;
            foreach (Match m in s_varPattern.Matches(filterJson))
                yield return m.Groups[1].Value;
        }

        /// <summary>Every <c>{"slot": "&lt;name&gt;"}</c> operand name in <paramref name="filterJson"/>.</summary>
        public static IEnumerable<string> ExtractSlotRefs(string filterJson)
        {
            if (string.IsNullOrEmpty(filterJson)) yield break;
            foreach (Match m in s_slotPattern.Matches(filterJson))
                yield return m.Groups[1].Value;
        }

        /// <summary>
        /// Rewrites every <c>{"slot": "&lt;from&gt;"}</c> operand in <paramref name="filterJson"/>
        /// to reference <paramref name="to"/> instead. A no-op when <paramref name="from"/> is
        /// never referenced.
        /// </summary>
        public static string RenameSlotRefs(string filterJson, string from, string to)
        {
            if (string.IsNullOrEmpty(from) || from == to)
                return filterJson ?? string.Empty;
            return RenameSlotRefs(filterJson, new Dictionary<string, string> { { from, to } });
        }

        /// <summary>
        /// Rewrites every slot operand in one scan. A name produced by an earlier
        /// pair is not matched again.
        /// </summary>
        public static string RenameSlotRefs(string filterJson, IReadOnlyDictionary<string, string> renames)
        {
            if (string.IsNullOrEmpty(filterJson) || renames == null || renames.Count == 0)
                return filterJson ?? string.Empty;

            return s_slotPattern.Replace(filterJson, m =>
            {
                var g = m.Groups[1];
                if (!renames.TryGetValue(g.Value, out string to) || to == g.Value)
                    return m.Value;
                int localStart = g.Index - m.Index;
                return m.Value.Substring(0, localStart) + to + m.Value.Substring(localStart + g.Length);
            });
        }
    }
}
