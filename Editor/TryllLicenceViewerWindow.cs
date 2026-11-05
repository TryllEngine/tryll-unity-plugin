// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Read-only view of the package's <c>LICENSE.md</c> (<see cref="TryllLicence"/>), opened by
    /// "Read the licence" in the Welcome window. The text shows inside the editor, so no
    /// <c>.md</c> file association is needed; it scrolls and can be selected and copied.
    /// Offers "Accept licence" until the licence is accepted. Follows
    /// docs/dev/editor-window-checklist.md: theme roles only, copy from
    /// <see cref="TryllEditorUiContract"/>, and a debug surface for headless tests.
    /// </summary>
    internal sealed class TryllLicenceViewerWindow : EditorWindow
    {
        internal const string ElementText = "tryll-licence-text";
        internal const string ElementAccept = "tryll-licence-accept";

        TextField _text;
        Button _accept;

        internal static void Open()
        {
            TryllCommunityView.OpenWindow<TryllLicenceViewerWindow>(TryllEditorUiContract.WelcomeLicenceViewerTitle,
                new Vector2(420, 360), new Vector2(640, 720));
        }

        void OnEnable()
        {
            titleContent = new GUIContent(TryllEditorUiContract.WelcomeLicenceViewerTitle);
            Build();
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
            TryllLicence.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            TryllLicence.Changed -= Refresh;
        }

        void OnHostColorsChanged()
        {
            Build();
            Refresh();
        }

        void Build()
        {
            rootVisualElement.Clear();
            TryllEditorTheme.Attach(rootVisualElement, paintWindowBackground: false);

            var body = new VisualElement();
            body.style.flexGrow = 1;
            body.style.paddingLeft = body.style.paddingRight = 16;
            body.style.paddingTop = 14;
            body.style.paddingBottom = 12;
            rootVisualElement.Add(body);

            body.Add(TryllCommunityView.Heading(TryllEditorUiContract.WelcomeLicenceViewerTitle, 16));

            // A read-only text field: it scrolls, and the text can be selected and copied.
            _text = new TextField { name = ElementText, multiline = true, isReadOnly = true };
            _text.style.flexGrow = 1;
            _text.style.flexShrink = 1;
            _text.style.marginLeft = _text.style.marginRight = 0;
            _text.style.marginBottom = 10;
            _text.style.whiteSpace = WhiteSpace.Normal;
            _text.verticalScrollerVisibility = ScrollerVisibility.Auto;
            body.Add(_text);

            _accept = TryllCommunityView.PrimaryButton(TryllEditorUiContract.WelcomeLicenceAccept,
                () => TryllLicence.SetAccepted(true), ElementAccept);
            _accept.style.alignSelf = Align.FlexStart;
            body.Add(_accept);
        }

        void Refresh()
        {
            if (_text == null)
                return;
            // Without a LICENSE.md the Welcome window hides "Read the licence"; a viewer
            // that is already open says why instead of showing nothing.
            _text.SetValueWithoutNotify(TryllLicence.HasFile
                ? TryllLicence.LoadText()
                : TryllEditorUiContract.WelcomeLicencePending);
            _accept.style.display = TryllLicence.Accepted ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ------------------------------------------------------------------
        // Debug surface for headless window tests
        // ------------------------------------------------------------------

        internal string DebugText => _text?.value ?? "";
        internal bool DebugAcceptVisible => _accept.style.display == DisplayStyle.Flex;
        internal bool DebugTextReadOnly => _text.isReadOnly;

        // A headless window has no panel to dispatch clicks, so run the handler a click runs.
        internal void DebugAcceptForTests() => TryllLicence.SetAccepted(true);
    }
}
