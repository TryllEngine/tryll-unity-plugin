// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// A large, wrapped editor for one multiline string field (a system prompt, a
    /// template), opened by the small button beside the field in the inspector.
    /// Edits apply as you type, through the same SerializedObject path as the
    /// inspector, so Undo and the inspector both see them. One window: opening
    /// another field retargets it. Follows docs/dev/editor-window-checklist.md:
    /// theme roles only, copy from <see cref="TryllEditorUiContract"/>, and a debug
    /// surface for headless tests.
    /// </summary>
    internal sealed class TryllTextEditWindow : EditorWindow
    {
        internal const string ElementText = "tryll-text-edit-text";
        internal const string ElementGone = "tryll-text-edit-gone";
        const long PullIntervalMs = 250;

        // Session continuity: survives a domain reload, not an editor restart.
        [SerializeField] Object _target;
        [SerializeField] string _path;
        [SerializeField] string _field;

        SerializedObject _so;
        Label _heading;
        Label _owner;
        Label _gone;
        TextField _text;

        /// <summary>True when the button can open this property: one string, one value.</summary>
        internal static bool CanOpen(SerializedProperty property)
            => property != null
               && property.propertyType == SerializedPropertyType.String
               && !property.hasMultipleDifferentValues
               && property.serializedObject.targetObjects.Length == 1;

        internal static void Open(SerializedProperty property)
        {
            if (!CanOpen(property))
                return;
            var window = TryllCommunityView.OpenWindow<TryllTextEditWindow>(
                Title(property.displayName), new Vector2(360, 240), new Vector2(720, 560));
            window.Retarget(property.serializedObject.targetObject, property.propertyPath, property.displayName);
            window.Focus();
        }

        /// <summary>
        /// The small square button the drawers place at the right of a row,
        /// as tall as a checkbox.
        /// </summary>
        internal static void DrawOpenButton(Rect rect, SerializedProperty property)
        {
            using (new EditorGUI.DisabledScope(!CanOpen(property)))
            {
                if (GUI.Button(rect, OpenContent(), EditorStyles.miniButton))
                    Open(property);
            }
        }

        internal static Rect OpenButtonRect(Rect row)
        {
            float side = EditorGUIUtility.singleLineHeight;
            return new Rect(row.xMax - side, row.y, side, side);
        }

        static GUIContent s_OpenContent;

        static GUIContent OpenContent()
        {
            if (s_OpenContent != null) return s_OpenContent;
            // "_Popup" is the editor's own "open in a new window" icon; no glyph needed.
            var icon = EditorGUIUtility.IconContent("_Popup");
            s_OpenContent = new GUIContent(icon.image, TryllEditorUiContract.TextEditorOpen);
            if (s_OpenContent.image == null)
                s_OpenContent.text = "…";
            return s_OpenContent;
        }

        static string Title(string field)
            => TryllEditorUiContract.TextEditorTitle.Replace("{field}", field ?? "");

        void OnEnable()
        {
            Build();
            TryllEditorTheme.HostColorsChanged += OnHostColorsChanged;
            Undo.undoRedoPerformed += Pull;
        }

        void OnDisable()
        {
            TryllEditorTheme.HostColorsChanged -= OnHostColorsChanged;
            Undo.undoRedoPerformed -= Pull;
            _so?.Dispose();
            _so = null;
        }

        void OnHostColorsChanged()
        {
            Build();
        }

        void Retarget(Object target, string path, string field)
        {
            _target = target;
            _path = path;
            _field = field;
            _so?.Dispose();
            _so = null;
            Pull();
        }

        void Build()
        {
            rootVisualElement.Clear();
            TryllEditorTheme.Attach(rootVisualElement, paintWindowBackground: false);

            var body = new VisualElement();
            body.style.flexGrow = 1;
            body.style.paddingLeft = body.style.paddingRight = 12;
            body.style.paddingTop = 10;
            body.style.paddingBottom = 10;
            rootVisualElement.Add(body);

            _heading = TryllCommunityView.Heading("", 14);
            body.Add(_heading);
            _owner = TryllCommunityView.Paragraph("", dim: true);
            _owner.style.marginBottom = 6;
            body.Add(_owner);

            _gone = TryllCommunityView.Paragraph(TryllEditorUiContract.TextEditorGone);
            _gone.name = ElementGone;
            body.Add(_gone);

            _text = new TextField { name = ElementText, multiline = true };
            _text.style.flexGrow = 1;
            _text.style.flexShrink = 1;
            _text.style.marginLeft = _text.style.marginRight = 0;
            _text.style.whiteSpace = WhiteSpace.Normal;
            _text.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _text.RegisterValueChangedCallback(e => Push(e.newValue));
            body.Add(_text);

            // The inspector can change the field too (typing there, a revert).
            _text.schedule.Execute(Pull).Every(PullIntervalMs);
            Pull();
        }

        SerializedProperty Property()
        {
            if (_target == null || string.IsNullOrEmpty(_path))
                return null;
            if (_so == null || _so.targetObject != _target)
            {
                _so?.Dispose();
                _so = new SerializedObject(_target);
            }
            _so.Update();
            var property = _so.FindProperty(_path);
            return property != null && property.propertyType == SerializedPropertyType.String ? property : null;
        }

        void Pull()
        {
            if (_text == null)
                return;
            var property = Property();
            bool live = property != null;
            titleContent = new GUIContent(Title(_field));
            _heading.text = _field ?? "";
            _owner.text = _target != null ? _target.name : "";
            _gone.style.display = live ? DisplayStyle.None : DisplayStyle.Flex;
            _text.style.display = live ? DisplayStyle.Flex : DisplayStyle.None;
            if (live && _text.value != property.stringValue)
                _text.SetValueWithoutNotify(property.stringValue ?? "");
        }

        void Push(string value)
        {
            var property = Property();
            if (property == null || property.stringValue == value)
                return;
            property.stringValue = value ?? "";
            _so.ApplyModifiedProperties();
            // IMGUI inspectors and the graph window's params panel repaint on events only.
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        // ------------------------------------------------------------------
        // Debug surface for headless window tests
        // ------------------------------------------------------------------

        internal string DebugText => _text?.value ?? "";
        internal bool DebugGoneVisible => _gone != null && _gone.style.display == DisplayStyle.Flex;

        internal void DebugRetargetForTests(SerializedProperty property)
            => Retarget(property.serializedObject.targetObject, property.propertyPath, property.displayName);

        // A headless window has no panel to dispatch typing; run what a keystroke runs.
        internal void DebugTypeForTests(string value)
        {
            _text.SetValueWithoutNotify(value);
            Push(value);
        }

        internal void DebugPullForTests() => Pull();
    }
}
