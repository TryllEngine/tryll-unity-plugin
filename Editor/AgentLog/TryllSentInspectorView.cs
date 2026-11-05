// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll.Client.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor.AgentLog
{
    internal sealed class TryllSentInspectorView
    {
        VisualElement _root;
        ScrollView _scroll;
        Label _hint;

        public VisualElement Root => _root ?? Build();

        public void Render(TryllSentInspection model, TryllObservationEnvelope envelope = null)
        {
            EnsureBuilt();
            _scroll.Clear();

            if (model == null)
            {
                _hint.text = "select a row";
                _scroll.Add(Placeholder("Select a sent row to inspect the exact outbound payload."));
                return;
            }

            _hint.text = string.IsNullOrEmpty(model.Hint) ? model.Title : model.Hint;
            TryllAgentLogPayloadView.AddTextSection(_scroll, "Form", model.Form);
            TryllAgentLogPayloadView.AddTextSection(_scroll, "Summary", model.ExactPayloadText);
            TryllAgentLogPayloadView.AddTextSection(_scroll, "Deferred variables", model.DeferredVariablesSummary);
            TryllAgentLogPayloadView.AddTextSection(_scroll, "Request", model.RequestId == 0 ? "—" : model.RequestId.ToString());
            TryllAgentLogPayloadView.AddTextSection(_scroll, "Agent", model.AgentId == 0 ? "—" : model.AgentId.ToString());
            TryllAgentLogPayloadView.AddTextSection(
                _scroll,
                "Notes",
                string.IsNullOrEmpty(model.ExtraNotes) ? "No additional detail" : model.ExtraNotes);
            TryllAgentLogPayloadView.AddRawSection(_scroll, envelope);
        }

        void EnsureBuilt()
        {
            if (_root != null) return;
            Build();
        }

        VisualElement Build()
        {
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.backgroundColor = Panel;
            _root.style.borderLeftWidth = 1;
            _root.style.borderLeftColor = Line;
            _root.style.minWidth = TryllTurnInspectorView.MinWidth;

            var top = new VisualElement();
            top.style.flexShrink = 0;
            top.style.backgroundColor = Panel2;
            top.style.paddingLeft = 10;
            top.style.paddingRight = 10;
            top.style.paddingTop = 6;
            top.style.paddingBottom = 7;
            top.style.borderBottomWidth = 1;
            top.style.borderBottomColor = Line;

            var title = new Label("Sent payload");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Txt;
            top.Add(title);

            _hint = new Label("select a row");
            _hint.style.color = Faint;
            _hint.style.marginTop = 2;
            top.Add(_hint);
            _root.Add(top);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.style.flexGrow = 1;
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _root.Add(_scroll);
            return _root;
        }
    }
}
