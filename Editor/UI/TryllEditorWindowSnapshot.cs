// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// What a window shows, read back for tests. Tests assert on this rather than
    /// walking the visual tree, so a layout change does not break them.
    /// </summary>
    internal readonly struct TryllEditorWindowSnapshot
    {
        public readonly TryllOwnerPhase OwnerPhase;
        /// <summary>Window-local state name (Chat's <c>State</c>, the session state elsewhere).</summary>
        public readonly string LocalState;
        public readonly string StatusCaption;
        /// <summary>Status-bar action button text ("Retry", …), or null when hidden.</summary>
        public readonly string StatusAction;
        /// <summary>Banner text, or null when dismissed.</summary>
        public readonly string BannerText;
        /// <summary>Model names (Model Manager), component labels (Memory), assistant replies (Chat).</summary>
        public readonly IReadOnlyList<string> Rows;
        public readonly bool LeaseHeld;
        public readonly int ListGeneration;

        public TryllEditorWindowSnapshot(
            TryllOwnerPhase ownerPhase,
            string localState,
            string statusCaption,
            string statusAction,
            string bannerText,
            IReadOnlyList<string> rows,
            bool leaseHeld,
            int listGeneration)
        {
            OwnerPhase = ownerPhase;
            LocalState = localState;
            StatusCaption = statusCaption;
            StatusAction = statusAction;
            BannerText = bannerText;
            Rows = rows ?? new List<string>();
            LeaseHeld = leaseHeld;
            ListGeneration = listGeneration;
        }
    }

    /// <summary>
    /// UITK <c>name</c>s on the controls tests and automation touch. Engine-local;
    /// not part of <c>editor_ui.json</c>.
    /// </summary>
    internal static class TryllEditorElementNames
    {
        public const string StatusCue = "tryll-status-cue";
        public const string StatusCaption = "tryll-status-caption";
        public const string StatusAction = "tryll-status-action";
        public const string Banner = "tryll-banner";
        public const string Refresh = "tryll-refresh";
        public const string ModelRow = "tryll-model-row";
        public const string ChatStart = "tryll-chat-start";
        public const string ChatInput = "tryll-chat-input";
        public const string ChatSend = "tryll-chat-send";
        public const string ChatUserBubble = "tryll-chat-user-bubble";
        public const string ChatAssistantBubble = "tryll-chat-assistant-bubble";
    }
}
