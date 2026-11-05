// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Attach gate for Agent Log capture (Unreal <c>AttachIfNeeded</c> parity).
    /// </summary>
    internal static class AgentLogCaptureAttach
    {
        /// <summary>
        /// True when capture should (re)subscribe to the live client.
        /// User pause blocks enable/focus/connection-changed auto-attach.
        /// </summary>
        public static bool ShouldAttach(bool userPaused, bool alreadySubscribedToLiveClient)
            => !userPaused && !alreadySubscribedToLiveClient;
    }
}
