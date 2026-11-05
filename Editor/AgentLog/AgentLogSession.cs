// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll.Client;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Resolves the shared editor <see cref="TryllClient"/> for Agent Log attach (§7.5).
    /// Mirrors <see cref="TryllEditorSession"/> acquisition without owning session lifecycle.
    /// </summary>
    internal static class AgentLogSession
    {
        public static TryllClient ResolveClient() => TryllClient.EnsureEditorInstance();
    }
}
