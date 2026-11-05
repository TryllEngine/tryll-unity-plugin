// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// What the Lab's Dialog Script field holds: a single <see cref="TryllDialogScriptAsset"/>
    /// (a run of one dialogue) or a <see cref="TryllDialogSuiteAsset"/> (several, in order).
    /// </summary>
    internal static class TryllDialogLabDialogues
    {
        public static bool IsAcceptedSource(UnityEngine.Object source) =>
            source is TryllDialogScriptAsset || source is TryllDialogSuiteAsset;

        /// <summary>
        /// The scripts a source runs, in order. A suite's empty slots are skipped; a suite
        /// with no scripts, or any other asset type, is an error.
        /// </summary>
        public static bool TryResolve(
            UnityEngine.Object source, out List<TryllDialogScriptAsset> scripts, out string error)
        {
            scripts = new List<TryllDialogScriptAsset>();
            error = null;
            switch (source)
            {
                case null:
                    error = "Assign a Dialog Script or Dialog Suite.";
                    return false;
                case TryllDialogScriptAsset script:
                    scripts.Add(script);
                    return true;
                case TryllDialogSuiteAsset suite:
                    scripts.AddRange((suite.Dialogues ?? new List<TryllDialogScriptAsset>()).Where(s => s != null));
                    if (scripts.Count == 0)
                    {
                        error = $"Dialog Suite '{suite.name}' has no dialog scripts.";
                        return false;
                    }
                    return true;
                default:
                    error = $"'{source.name}' is a {source.GetType().Name}; drop a Dialog Script or Dialog Suite.";
                    return false;
            }
        }

        /// <summary>Validation messages name the dialogue only when there is more than one.</summary>
        public static string Prefix(int index, string name, int count) =>
            count <= 1 ? "" : $"Dialogue {index + 1} '{name}': ";
    }
}
