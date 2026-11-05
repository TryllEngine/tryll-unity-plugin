// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Collapsing sections for a sent or system row, plus the Raw JSON of its frame.
    /// </summary>
    internal static class TryllAgentLogPayloadView
    {
        public static VisualElement Section(
            string title,
            bool open,
            Action<VisualElement> build,
            string trailing = null,
            VisualElement headerExtra = null,
            Func<string> copy = null)
        {
            var wrap = new VisualElement();
            wrap.style.borderBottomWidth = 1;
            wrap.style.borderBottomColor = Line;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;
            head.style.backgroundColor = Panel2;
            head.style.paddingLeft = 10;
            head.style.paddingRight = 10;
            head.style.paddingTop = 4;
            head.style.paddingBottom = 4;

            var caret = new Label(open ? "▾" : "▸");
            caret.style.color = Faint;
            caret.style.width = 12;
            caret.style.flexShrink = 0;
            head.Add(caret);

            var heading = new Label(title.ToUpperInvariant());
            heading.style.fontSize = 11;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.color = Dim;
            head.Add(heading);
            head.Add(new VisualElement { style = { flexGrow = 1 } });
            if (!string.IsNullOrEmpty(trailing))
            {
                var trail = new Label(trailing);
                trail.style.color = Faint;
                trail.style.fontSize = 11;
                head.Add(trail);
            }
            if (headerExtra != null)
                head.Add(headerExtra);
            if (copy != null)
                head.Add(CopyButton(copy));
            wrap.Add(head);

            var body = new VisualElement();
            body.style.paddingLeft = 10;
            body.style.paddingRight = 10;
            body.style.paddingTop = 6;
            body.style.paddingBottom = 8;
            body.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            build(body);
            wrap.Add(body);

            head.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target is Button)
                    return;
                bool nowOpen = body.style.display == DisplayStyle.None;
                body.style.display = nowOpen ? DisplayStyle.Flex : DisplayStyle.None;
                caret.text = nowOpen ? "▾" : "▸";
            });
            return wrap;
        }

        public static Button HeaderButton(string text)
        {
            var button = new Button { text = text ?? "" };
            button.style.height = 15;
            button.style.minHeight = 0;
            button.style.flexShrink = 0;
            button.style.fontSize = 10;
            button.style.marginTop = 0;
            button.style.marginBottom = 0;
            button.style.marginLeft = 8;
            button.style.paddingLeft = 4;
            button.style.paddingRight = 4;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            return button;
        }

        public static void AddTextSection(VisualElement parent, string title, string value, bool open = true)
        {
            if (parent == null)
                return;
            parent.Add(Section(title, open, body =>
            {
                var label = new Label(string.IsNullOrEmpty(value) ? "—" : value) { enableRichText = false };
                label.style.color = Txt;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.fontSize = 11;
                body.Add(label);
            }));
        }

        public static void AddRawSection(VisualElement parent, TryllObservationEnvelope envelope)
        {
            if (parent == null)
                return;
            var raw = RawJson(envelope);
            parent.Add(Section(
                "Raw JSON",
                false,
                body => FillRaw(body, raw),
                copy: () => raw));
        }

        public static void FillRaw(VisualElement body, string json)
        {
            if (body == null)
                return;
            body.Clear();
            var host = new VisualElement();
            SetBorder(host, Line, 1);
            SetRadius(host, 3);
            host.style.paddingLeft = 7;
            host.style.paddingRight = 7;
            host.style.paddingTop = 5;
            host.style.paddingBottom = 5;
            var label = new Label(string.IsNullOrEmpty(json) ? "—" : json) { enableRichText = false };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 11;
            label.style.color = Dim;
            host.Add(label);
            body.Add(host);
        }

        public static string RawJson(TryllObservationEnvelope envelope)
        {
            if (envelope?.Frame == null || envelope.Frame.Length == 0)
                return "";
            return TryllAgentLogPayloadModel.WriteJson(envelope, indent: true);
        }

        static Button CopyButton(Func<string> copy)
        {
            var button = HeaderButton("copy");
            button.style.width = 48;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.tooltip = "Copy to clipboard";
            button.clicked += () =>
            {
                EditorGUIUtility.systemCopyBuffer = copy() ?? "";
                button.text = "copied";
                button.schedule.Execute(() => button.text = "copy").StartingIn(900);
            };
            return button;
        }
    }
}
