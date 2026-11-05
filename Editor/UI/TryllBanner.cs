// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Inline banner. Severity is explicit — never inferred from the calling window.
    /// </summary>
    internal sealed class TryllBanner : VisualElement
    {
        readonly Label _label;

        public TryllBanner()
        {
            name = TryllEditorElementNames.Banner;
            style.flexShrink = 0;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.marginBottom = 4;
            style.whiteSpace = WhiteSpace.Normal;
            SetRadius(this, 4);
            style.display = DisplayStyle.None;

            _label = new Label();
            _label.style.whiteSpace = WhiteSpace.Normal;
            _label.style.fontSize = 11;
            Add(_label);
        }

        public void Bind(string text, TryllStatusSeverity severity)
        {
            if (string.IsNullOrEmpty(text))
            {
                Dismiss();
                return;
            }

            _label.text = text;
            ApplySeverity(severity);
            style.display = DisplayStyle.Flex;
        }

        public void Dismiss()
        {
            _label.text = "";
            style.display = DisplayStyle.None;
        }

        /// <summary>Shown text, or null when dismissed.</summary>
        internal string DebugText =>
            style.display == DisplayStyle.None ? null : _label.text;

        void ApplySeverity(TryllStatusSeverity severity)
        {
            Color fg;
            Color bg;
            switch (severity)
            {
                case TryllStatusSeverity.Error:
                    fg = ColErr;
                    bg = ErrBg;
                    break;
                case TryllStatusSeverity.Warning:
                    fg = ColWarn;
                    bg = WarnBg;
                    break;
                case TryllStatusSeverity.Success:
                    fg = ColOk;
                    bg = OkBg;
                    break;
                default:
                    fg = Dim;
                    bg = Panel2;
                    break;
            }

            _label.style.color = fg;
            style.backgroundColor = bg;
        }
    }
}
