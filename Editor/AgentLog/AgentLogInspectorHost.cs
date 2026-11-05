// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using Tryll.Client.Editor;
using Tryll.Client.Internal;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Polymorphic inspector host: turn diagnostics, sent payload, or system detail (R12.5).
    /// </summary>
    internal sealed class AgentLogInspectorHost
    {
        readonly VisualElement _root;
        readonly VisualElement _turnHost;
        readonly VisualElement _sentHost;
        readonly VisualElement _systemHost;
        readonly TryllTurnInspectorView _turnView = new TryllTurnInspectorView();
        readonly TryllSentInspectorView _sentView = new TryllSentInspectorView();
        readonly TryllSystemInspectorView _systemView = new TryllSystemInspectorView();

        public VisualElement Root => _root;

        public AgentLogInspectorHost()
        {
            _root = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column,
                    minWidth = TryllTurnInspectorView.MinWidth,
                },
            };

            _turnHost = Wrap(_turnView.Root);
            _sentHost = Wrap(_sentView.Root);
            _systemHost = Wrap(_systemView.Root);

            _root.Add(_turnHost);
            _root.Add(_sentHost);
            _root.Add(_systemHost);

            Render(null, null, null);
        }

        static VisualElement Wrap(VisualElement child)
        {
            var host = new VisualElement { style = { flexGrow = 1 } };
            child.style.flexGrow = 1;
            host.Add(child);
            return host;
        }

        public void Render(
            AgentLogRow row,
            IEnumerable<TryllObservationEnvelope> journal,
            AgentDescriptorRegistry descriptors)
        {
            HideAll();

            if (row == null)
            {
                _turnHost.style.display = DisplayStyle.Flex;
                _turnView.Render(TryllTurnInspectionFactory.Empty("select a row"));
                return;
            }

            switch (row.Kind)
            {
            case AgentLogRowKind.Sent:
                _sentHost.style.display = DisplayStyle.Flex;
                _sentView.Render(AgentLogInspectionBuilder.BuildSent(row), row.Source);
                break;

            case AgentLogRowKind.Received:
                _turnHost.style.display = DisplayStyle.Flex;
                _turnView.Render(AgentLogInspectionBuilder.BuildTurn(row, journal, descriptors));
                break;

            case AgentLogRowKind.System:
            case AgentLogRowKind.CaptureGap:
                _systemHost.style.display = DisplayStyle.Flex;
                _systemView.Render(AgentLogInspectionBuilder.BuildSystem(row), row.Source);
                break;

            default:
                _systemHost.style.display = DisplayStyle.Flex;
                _systemView.Render(
                    new TryllSystemInspection
                    {
                        Title = "Unknown row",
                        Hint = row.SummaryText ?? "",
                        ExtraNotes = "No additional detail",
                    },
                    row.Source);
                break;
            }
        }

        void HideAll()
        {
            _turnHost.style.display = DisplayStyle.None;
            _sentHost.style.display = DisplayStyle.None;
            _systemHost.style.display = DisplayStyle.None;
        }
    }
}
