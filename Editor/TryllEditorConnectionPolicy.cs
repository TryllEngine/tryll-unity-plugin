// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Shared wait-versus-fail rule for editor consumers of the shared
    /// <see cref="TryllClient"/> connection. Keep this a pure function so tests
    /// can lock the contract without driving windows.
    /// </summary>
    internal static class TryllEditorConnectionPolicy
    {
        /// <summary>
        /// Transient <c>ConnectionChanged(false)</c> during startup is not terminal
        /// while the owner still has retry budget.
        /// </summary>
        public static bool ShouldWaitForOwnerRetry(bool isStarting, bool retriesRemaining)
            => isStarting && retriesRemaining;

        /// <summary>Exactly-once acquire for a window that stores one boolean lease.</summary>
        public static void AcquireOnce(TryllClient client, ref bool acquired)
        {
            if (client == null || acquired) return;
            client.AcquireConnection();
            acquired = true;
        }

        /// <summary>A panel that already holds the shared connection must not bind or acquire again.</summary>
        public static bool ShouldBindAndAcquire(bool alreadyAcquired) => !alreadyAcquired;

        /// <summary>Retry a failed local list/refresh when the owner session is still ready.</summary>
        public static bool ShouldRetryLocalWork(bool alreadyAcquired, bool sessionReady)
            => alreadyAcquired && sessionReady;
    }
}
