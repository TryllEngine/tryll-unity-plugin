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
    /// Host renderer for <see cref="TryllStatusPresentation"/>. Severity drives a
    /// text/dot cue as well as colour — never colour alone.
    /// </summary>
    internal sealed class TryllStatusBar : VisualElement
    {
        readonly Label _cue;
        readonly Label _caption;
        readonly Button _action;
        Action _onAction;

        public TryllStatusBar()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.flexShrink = 0;

            _cue = new Label("◌") { name = TryllEditorElementNames.StatusCue };
            _cue.style.marginRight = 4;
            _cue.style.fontSize = 11;
            _cue.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(_cue);

            _caption = new Label { name = TryllEditorElementNames.StatusCaption };
            _caption.style.unityFontStyleAndWeight = FontStyle.Bold;
            _caption.style.flexGrow = 1;
            _caption.style.flexShrink = 1;
            _caption.style.overflow = Overflow.Hidden;
            _caption.style.textOverflow = TextOverflow.Ellipsis;
            _caption.style.whiteSpace = WhiteSpace.NoWrap;
            Add(_caption);

            _action = new Button(OnActionClicked) { name = TryllEditorElementNames.StatusAction };
            _action.style.marginLeft = 8;
            _action.style.height = 20;
            _action.style.display = DisplayStyle.None;
            Add(_action);
        }

        public void Bind(TryllStatusPresentation presentation, Action onAction = null)
        {
            _onAction = onAction;
            _caption.text = presentation.Caption ?? "";
            ApplySeverity(presentation.Severity);
            BindAction(presentation.Action);
        }

        void BindAction(TryllStatusActionIntent action)
        {
            if (action == TryllStatusActionIntent.None || _onAction == null)
            {
                _action.style.display = DisplayStyle.None;
                return;
            }

            _action.text = action switch
            {
                TryllStatusActionIntent.Retry => "Retry",
                TryllStatusActionIntent.Start => "Start",
                TryllStatusActionIntent.Stop => "Stop",
                _ => "",
            };
            _action.style.display = DisplayStyle.Flex;
        }

        void ApplySeverity(TryllStatusSeverity severity)
        {
            bool filled = severity == TryllStatusSeverity.Success
                || severity == TryllStatusSeverity.Error
                || severity == TryllStatusSeverity.Warning;
            _cue.text = filled ? "●" : "◌";
            Color color = severity switch
            {
                TryllStatusSeverity.Success => ColOk,
                TryllStatusSeverity.Error => ColErr,
                TryllStatusSeverity.Warning => ColWarn,
                _ => Dim,
            };
            _cue.style.color = color;
            _caption.style.color = color;
        }

        void OnActionClicked() => _onAction?.Invoke();

        internal string DebugCaption => _caption.text;

        /// <summary>Action button text, or null when the button is hidden.</summary>
        internal string DebugActionText =>
            _action.style.display == DisplayStyle.None ? null : _action.text;

        /// <summary>Press the action button as a click would.</summary>
        internal void DebugClickActionForTests() => OnActionClicked();
    }
}
