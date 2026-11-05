// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll.Client.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor.AgentLog
{
    internal sealed class TryllSystemInspectorView
    {
        VisualElement _root;
        ScrollView _scroll;
        Label _hint;

        public VisualElement Root => _root ?? Build();

        public void Render(TryllSystemInspection model, TryllObservationEnvelope envelope = null)
        {
            EnsureBuilt();
            _scroll.Clear();

            if (model == null)
            {
                _hint.text = "select a row";
                _scroll.Add(Placeholder("Select a system row to inspect lifecycle details."));
                return;
            }

            _hint.text = string.IsNullOrEmpty(model.Hint) ? model.Title : model.Hint;
            var notes = string.IsNullOrEmpty(model.ExtraNotes) ? "No additional detail" : model.ExtraNotes;
            _scroll.Add(TryllAgentLogPayloadView.Section("Details", true, body =>
            {
                body.Add(Field("Target", model.Target));
                body.Add(Field("Before", model.BeforeText));
                body.Add(Field("After", model.AfterText));
                body.Add(Field("Ack / error", model.AckOrError));
                body.Add(Field("Notes", notes));
            }));
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

            var title = new Label("System event");
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

        static VisualElement Field(string label, string value)
        {
            var block = new VisualElement { style = { marginBottom = 8 } };
            var heading = new Label(label);
            heading.style.color = Dim;
            heading.style.fontSize = 10;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            block.Add(heading);

            var body = new Label(string.IsNullOrEmpty(value) ? "—" : value) { enableRichText = false };
            body.style.color = Txt;
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.fontSize = 11;
            block.Add(body);
            return block;
        }
    }
}
