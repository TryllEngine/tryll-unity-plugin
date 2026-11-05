// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;

namespace Tryll.Client.Editor.AgentLog
{
    internal sealed class TryllSentInspection
    {
        public string Title = "";
        public string Hint = "";
        public string Form = "";
        public ulong RequestId;
        public ulong AgentId;
        public DateTime WallTimeUtc;
        public string ExactPayloadText = "";
        public string DeferredVariablesSummary = "";
        public string ExtraNotes = "";
    }
}
