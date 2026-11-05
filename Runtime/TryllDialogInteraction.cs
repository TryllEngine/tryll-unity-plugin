// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;

namespace Tryll.Client
{
    /// <summary>
    /// One scripted user/assistant exchange for <see cref="TryllAgent.AppendInteractions"/>.
    /// Empty strings omit that side; both empty → skipped server-side.
    /// </summary>
    [Serializable]
    public struct TryllDialogInteraction
    {
        public string UserMessage;
        public string AssistantMessage;
    }
}
