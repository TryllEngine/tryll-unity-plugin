// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Where a Dialog Lab tool-call handler should take its result from. Extracted so
    /// the decision table can be tested without a server or a scene agent.
    /// </summary>
    internal enum TryllDialogLabToolSource
    {
        Canned,
        Live,
        Missing,
    }

    internal static class TryllDialogLabToolResolution
    {
        /// <summary>
        /// Canned always wins. Live is only offered when the variant opted in *and* the
        /// scene agent has a handler. Otherwise the source is Missing: the runner
        /// resumes with an empty result and EvaluateStep CheckFails the cell.
        /// </summary>
        public static TryllDialogLabToolSource Resolve(
            bool hasCanned, bool useLiveTools, bool hasLiveHandler)
        {
            if (hasCanned) return TryllDialogLabToolSource.Canned;
            if (useLiveTools && hasLiveHandler) return TryllDialogLabToolSource.Live;
            return TryllDialogLabToolSource.Missing;
        }
    }
}
