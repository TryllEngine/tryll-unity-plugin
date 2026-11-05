// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Chip showing the deployment mode the connected server reported in
    /// <c>ConnectionReady</c>, only when it is not the default <c>client-dev</c>, so
    /// customers never see it. Hidden while disconnected or when the server predates
    /// the field. Tryll Dev is tinted as a warning: it runs with telemetry off.
    /// </summary>
    internal sealed class TryllServerModeBadge : Label
    {
        const long PollMs = 500;

        readonly Func<TryllClient> _client;
        string _shown;

        public TryllServerModeBadge(Func<TryllClient> client)
        {
            _client = client;
            style.fontSize = 10;
            style.paddingLeft = 5; style.paddingRight = 5;
            style.paddingTop = 0; style.paddingBottom = 1;
            style.marginLeft = 6; style.marginRight = 6;
            style.flexShrink = 0;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            SetRadius(this, RadiusChip);
            style.display = DisplayStyle.None;
            schedule.Execute(Refresh).Every(PollMs);
        }

        /// <summary>Text for <paramref name="mode"/>, or null when the badge should hide.</summary>
        internal static string Caption(bool connected, string mode) =>
            connected && !string.IsNullOrEmpty(mode) && mode != "client-dev" ? mode : null;

        void Refresh()
        {
            var client = _client?.Invoke();
            string caption = Caption(client != null && client.IsConnected, client?.ServerMode);
            if (caption == _shown) return;
            _shown = caption;

            if (caption == null)
            {
                style.display = DisplayStyle.None;
                return;
            }

            Color fg = caption == "tryll-dev" ? ColWarn : Dim;
            text = caption;
            tooltip = "Deployment mode reported by the connected Tryll server.";
            style.color = fg;
            style.backgroundColor = Wash(fg, 0.14f);
            SetBorder(this, Wash(fg, 0.45f), 1);
            style.display = DisplayStyle.Flex;
        }
    }
}
