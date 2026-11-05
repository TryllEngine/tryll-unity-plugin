// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client
{
    /// <summary>One tool call surfaced by a ToolCall node.</summary>
    public sealed class TryllToolCall
    {
        public string CallId { get; }
        public string NodeName { get; }
        public string ToolName { get; }
        public string ArgumentsJson { get; }
        /// <summary>
        /// Snapshot of the node's disposition ("route_only" / "acknowledge" /
        /// "notify" / "notify_and_acknowledge" / "pause" /
        /// "pause_and_acknowledge" / "await_result"). RegisterTool uses this
        /// to decide whether auto-resume must carry <see cref="TryllToolResult"/>s
        /// (<c>await_result</c> only).
        /// </summary>
        public string Disposition { get; }

        public TryllToolCall(
            string callId,
            string nodeName,
            string toolName,
            string argumentsJson,
            string disposition = "")
        {
            CallId = callId ?? "";
            NodeName = nodeName ?? "";
            ToolName = toolName ?? "";
            ArgumentsJson = argumentsJson ?? "";
            Disposition = disposition ?? "";
        }

        public bool RequiresNativeResults =>
            string.Equals(Disposition, "await_result", System.StringComparison.Ordinal);
    }

    /// <summary>One model-visible result correlated to a tool-call ID.</summary>
    public sealed class TryllToolResult
    {
        public string CallId { get; }
        public string Result { get; }

        public TryllToolResult(string callId, string result)
        {
            CallId = callId ?? "";
            Result = result ?? "";
        }
    }
}
