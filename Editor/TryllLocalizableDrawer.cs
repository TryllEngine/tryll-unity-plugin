// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tryll.Client;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// One inspector block for a prompt field. The block title is the field name.
    /// Inside it, a "Localize text" checkbox swaps an unlabeled inline string for
    /// a Text ID, a searchable key list, and a preview from the project text provider.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllLocalizableAttribute))]
    sealed class TryllLocalizableDrawer : PropertyDrawer
    {
        const int PreviewLineLimit = 5;
        const int InlineLineCount = 5;
        const float StatusWidth = 150f;
        const float PreviewLabelWidth = 52f;
        const float LanguageWidth = 72f;
        const float OpenWidth = 52f;
        const float WarnHeight = 34f;

        static readonly Dictionary<string, string> PreviewLocales = new Dictionary<string, string>();
        static readonly Dictionary<string, bool> ShowAll = new Dictionary<string, bool>();
        // Width the preview was last drawn at, so GetPropertyHeight wraps the same way.
        static readonly Dictionary<string, float> LastWidth = new Dictionary<string, float>();

        static GUIStyle s_Rich;
        static GUIStyle s_WrappedTextArea;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var localized = Sibling(property, property.name + "Localized");
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            if (localized == null)
                return LabeledTextHeight();

            float h = line + gap + line;
            if (!localized.boolValue || localized.hasMultipleDifferentValues)
                return h + gap + TextBodyHeight();

            h += gap + line;
            h += gap + line;
            string key = StateKey(property);
            float width = LastWidth.TryGetValue(key, out var w) ? w : EditorGUIUtility.currentViewWidth - 40f;
            var result = Evaluate(property, Sibling(property, property.name + "TextId")?.stringValue);
            h += gap + PreviewBodyHeight(result, Mathf.Max(80f, width), key);
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var localized = Sibling(property, property.name + "Localized");
            var textId = Sibling(property, property.name + "TextId");
            if (localized == null || textId == null)
            {
                DrawLabeledText(position, property, label);
                return;
            }

            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            var row = new Rect(position.x, position.y, position.width, line);
            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.LabelField(row, label);

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = previousIndent + 1;
            var indented = EditorGUI.IndentedRect(row);
            EditorGUI.indentLevel = 0;
            try
            {
                row = new Rect(indented.x, row.yMax + gap, indented.width, line);
                var toggleRow = row;
                if (IsMultiline && !localized.boolValue && !localized.hasMultipleDifferentValues)
                {
                    var open = TryllTextEditWindow.OpenButtonRect(row);
                    TryllTextEditWindow.DrawOpenButton(open, property);
                    toggleRow.width -= open.width + 2f;
                }
                EditorGUI.showMixedValue = localized.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                bool tick = EditorGUI.ToggleLeft(toggleRow, "Localize text", localized.boolValue);
                if (EditorGUI.EndChangeCheck())
                    localized.boolValue = tick;
                EditorGUI.showMixedValue = false;
                row.y += line + gap;

                if (!localized.boolValue || localized.hasMultipleDifferentValues)
                {
                    row.height = TextBodyHeight();
                    DrawTextBody(row, property);
                    return;
                }

                DrawTextId(row, property, textId);
                row.y += line + gap;
                var result = Evaluate(property, textId.stringValue);
                DrawPreviewChrome(row, property, textId.stringValue, result);
                row.y += line + gap;
                row.height = position.yMax - row.y;
                // Only a Repaint rect is real. The Layout pass hands OnGUI a
                // dummy rect about 1 px wide; saving that width made
                // GetPropertyHeight wrap the preview into a huge block and push
                // every field below it out of view.
                if (Event.current.type == EventType.Repaint && row.width > 1f)
                    LastWidth[StateKey(property)] = row.width;
                DrawPreviewBody(row, property, result);
            }
            finally
            {
                EditorGUI.showMixedValue = false;
                EditorGUI.indentLevel = previousIndent;
                EditorGUI.EndProperty();
            }
        }

        bool IsMultiline => attribute is TryllLocalizableAttribute marker && marker.Multiline;

        float TextBodyHeight()
        {
            float line = EditorGUIUtility.singleLineHeight;
            return IsMultiline ? line * InlineLineCount : line;
        }

        float LabeledTextHeight()
        {
            float line = EditorGUIUtility.singleLineHeight;
            if (!IsMultiline) return line;
            return line + EditorGUIUtility.standardVerticalSpacing + TextBodyHeight();
        }

        void DrawTextBody(Rect row, SerializedProperty property)
        {
            // EditorStyles.textArea does not wrap, so a long prompt ran off the right edge.
            if (IsMultiline && !property.hasMultipleDifferentValues)
            {
                TryllIndependentTextArea.DrawField(row, property);
                return;
            }

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            string value = property.stringValue ?? "";
            value = IsMultiline
                ? EditorGUI.TextArea(row, value, WrappedTextArea())
                : EditorGUI.TextField(row, value);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = value;
            EditorGUI.showMixedValue = false;
        }

        void DrawLabeledText(Rect row, SerializedProperty property, GUIContent label)
        {
            if (!IsMultiline)
            {
                property.stringValue = EditorGUI.TextField(row, label, property.stringValue ?? "");
                return;
            }

            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            var labelRow = new Rect(row.x, row.y, row.width, line);
            TryllTextEditWindow.DrawOpenButton(TryllTextEditWindow.OpenButtonRect(labelRow), property);
            EditorGUI.LabelField(labelRow, label);
            var area = new Rect(row.x, row.y + line + gap, row.width, Mathf.Max(line, row.height - line - gap));
            TryllIndependentTextArea.DrawField(area, property);
        }

        static GUIStyle WrappedTextArea()
        {
            if (s_WrappedTextArea != null) return s_WrappedTextArea;
            s_WrappedTextArea = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            return s_WrappedTextArea;
        }

        static void DrawTextId(Rect row, SerializedProperty property, SerializedProperty textId)
        {
            var popup = new Rect(row.xMax - 22f, row.y, 22f, row.height);
            var field = new Rect(row.x, row.y, row.width - 24f, row.height);
            EditorGUI.showMixedValue = textId.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            string typed = EditorGUI.TextField(field, "Text ID", textId.stringValue ?? "");
            if (EditorGUI.EndChangeCheck())
                textId.stringValue = typed;
            EditorGUI.showMixedValue = false;

            if (!EditorGUI.DropdownButton(popup, GUIContent.none, FocusType.Passive))
                return;

            var provider = ProviderEditor();
            if (provider == null)
            {
                var menu = new GenericMenu();
                menu.AddDisabledItem(new GUIContent("No editor preview"));
                menu.DropDown(popup);
                return;
            }

            // Read fresh on every open: a key added a moment ago must be listed.
            var ids = provider.ListIds(TableOf(property)) ?? Array.Empty<string>();
            string path = textId.propertyPath;
            var so = property.serializedObject;
            new TextIdDropdown(ids, textId.stringValue, chosen =>
            {
                so.Update();
                var prop = so.FindProperty(path);
                if (prop != null) prop.stringValue = chosen;
                so.ApplyModifiedProperties();
            }).Show(new Rect(field.x, field.y, field.width + 24f, field.height));
        }

        static void DrawPreviewChrome(Rect row, SerializedProperty property, string id, TryllTextPreview.Result result)
        {
            var editor = ProviderEditor();
            // [Preview][language ▾][status ....][Open]. The label and the popup
            // are drawn apart: a labelled Popup reserves the full inspector label
            // width, which left no room for the language in a narrow panel.
            var open = new Rect(row.xMax - OpenWidth, row.y, OpenWidth, row.height);
            var caption = new Rect(row.x, row.y, PreviewLabelWidth, row.height);
            var lang = new Rect(caption.xMax + 2f, row.y,
                Mathf.Clamp(open.x - caption.xMax - 8f, 0f, LanguageWidth), row.height);
            var status = new Rect(lang.xMax + 4f, row.y,
                Mathf.Clamp(open.x - lang.xMax - 8f, 0f, StatusWidth), row.height);
            status.x = open.x - 4f - status.width;

            EditorGUI.LabelField(caption, "Preview");
            if (editor != null)
            {
                var locales = TryllTextPreviewCache.Locales(editor);
                string key = StateKey(property);
                string selected = PreviewLocale(property);
                if (string.IsNullOrEmpty(selected) && locales.Count > 0)
                    selected = locales[0];

                int index = 0;
                var labels = new string[locales.Count];
                for (int i = 0; i < locales.Count; i++)
                {
                    labels[i] = locales[i];
                    if (string.Equals(locales[i], selected, StringComparison.OrdinalIgnoreCase)) index = i;
                }
                if (labels.Length == 0)
                    EditorGUI.LabelField(lang, "No languages", EditorStyles.miniLabel);
                else
                {
                    int next = EditorGUI.Popup(lang, index, labels);
                    PreviewLocales[key] = labels[next];
                }
            }

            DrawStatus(status, result);

            using (new EditorGUI.DisabledScope(editor == null || string.IsNullOrEmpty(id)))
            {
                if (GUI.Button(open, "Open") && editor != null)
                    editor.OpenInEditor(TableOf(property), id);
            }
        }

        static void DrawStatus(Rect rect, TryllTextPreview.Result result)
        {
            string text;
            Color color;
            switch (result.Kind)
            {
                case TryllTextPreview.Kind.Found when result.MissingLocales.Count > 0:
                    text = "✓ found · some missing";
                    color = TryllEditorTheme.ColWarn;
                    break;
                case TryllTextPreview.Kind.Found:
                    text = "✓ found";
                    color = TryllEditorTheme.ColOk;
                    break;
                case TryllTextPreview.Kind.MissingHere:
                    text = "✗ missing";
                    color = TryllEditorTheme.ColErr;
                    break;
                case TryllTextPreview.Kind.EmptyId:
                    text = "✗ no Text ID";
                    color = TryllEditorTheme.ColErr;
                    break;
                default:
                    text = "no preview";
                    color = TryllEditorTheme.Dim;
                    break;
            }
            var style = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        static void DrawPreviewBody(Rect row, SerializedProperty property, TryllTextPreview.Result result)
        {
            string key = StateKey(property);
            if (result.Kind == TryllTextPreview.Kind.Found)
            {
                string shown = Highlight(Truncate(result.Text, key, out bool canExpand));
                var style = RichStyle();
                float textH = style.CalcHeight(new GUIContent(shown), row.width);
                var text = new Rect(row.x, row.y, row.width, textH);
                GUI.Label(text, shown, style);
                float y = text.yMax + 2f;
                if (canExpand)
                {
                    var toggle = new Rect(row.x, y, row.width, EditorGUIUtility.singleLineHeight);
                    bool show = ShowAll.TryGetValue(key, out var on) && on;
                    ShowAll[key] = EditorGUI.ToggleLeft(toggle, show ? "Show less" : "Show all", show);
                    y = toggle.yMax + 2f;
                }
                if (result.MissingLocales.Count > 0)
                {
                    EditorGUI.HelpBox(new Rect(row.x, y, row.width, WarnHeight),
                        "Missing in: " + string.Join(", ", result.MissingLocales),
                        MessageType.Warning);
                }
                return;
            }

            if (result.Kind == TryllTextPreview.Kind.MissingHere)
            {
                var box = new Rect(row.x, row.y, row.width, WarnHeight + 6f);
                EditorGUI.HelpBox(box, result.Message, MessageType.Error);
                if (result.IsFallback && !string.IsNullOrEmpty(result.Text))
                {
                    string shown = "<b>Fallback:</b> " + Highlight(Truncate(result.Text, key, out _));
                    float h = RichStyle().CalcHeight(new GUIContent(shown), row.width);
                    GUI.Label(new Rect(row.x, box.yMax + 2f, row.width, h), shown, RichStyle());
                }
                return;
            }

            var messageType = result.Kind == TryllTextPreview.Kind.EmptyId
                ? MessageType.Error
                : MessageType.None;
            EditorGUI.HelpBox(new Rect(row.x, row.y, row.width, WarnHeight + 6f), result.Message ?? "", messageType);
        }

        static float PreviewBodyHeight(TryllTextPreview.Result result, float width, string key)
        {
            if (result.Kind == TryllTextPreview.Kind.Found)
            {
                string shown = Highlight(Truncate(result.Text, key, out bool canExpand));
                float h = RichStyle().CalcHeight(new GUIContent(shown), width) + 2f;
                if (canExpand) h += EditorGUIUtility.singleLineHeight + 2f;
                if (result.MissingLocales.Count > 0) h += WarnHeight + 2f;
                return h;
            }
            float box = WarnHeight + 6f;
            if (result.Kind == TryllTextPreview.Kind.MissingHere && result.IsFallback && !string.IsNullOrEmpty(result.Text))
            {
                string shown = "<b>Fallback:</b> " + Highlight(Truncate(result.Text, key, out _));
                box += 2f + RichStyle().CalcHeight(new GUIContent(shown), width);
            }
            return box;
        }

        static TryllTextPreview.Result Evaluate(SerializedProperty property, string id)
        {
            var provider = TryllRuntimeSettings.Load().TextProvider;
            return TryllTextPreviewCache.Evaluate(provider, TableOf(property), id, PreviewLocale(property));
        }

        static string PreviewLocale(SerializedProperty property)
        {
            if (PreviewLocales.TryGetValue(StateKey(property), out var locale) && !string.IsNullOrEmpty(locale))
                return locale;
            return TryllRuntimeSettings.Load().TextProvider?.CurrentLocale ?? "";
        }

        static ITryllTextProviderEditor ProviderEditor()
            => TryllRuntimeSettings.Load().TextProvider as ITryllTextProviderEditor;

        static string TableOf(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is TryllWorkflowAsset asset)
                return asset.TextTable ?? "";
            var table = property.serializedObject.FindProperty("TextTable")
                ?? property.serializedObject.FindProperty("Graph.TextTable");
            return table?.stringValue ?? "";
        }

        static SerializedProperty Sibling(SerializedProperty property, string name)
        {
            string path = property.propertyPath;
            int dot = path.LastIndexOf('.');
            string sibling = dot < 0 ? name : path.Substring(0, dot + 1) + name;
            return property.serializedObject.FindProperty(sibling);
        }

        static string StateKey(SerializedProperty property)
        {
            int id = property.serializedObject.targetObject != null
                ? property.serializedObject.targetObject.GetInstanceID()
                : 0;
            return id + ":" + property.propertyPath;
        }

        static string Truncate(string text, string key, out bool canExpand)
        {
            text ??= "";
            var lines = text.Replace("\r\n", "\n").Split('\n');
            canExpand = lines.Length > PreviewLineLimit;
            bool show = ShowAll.TryGetValue(key, out var on) && on;
            if (!canExpand || show) return text;
            return string.Join("\n", lines, 0, PreviewLineLimit);
        }

        /// <summary>
        /// Rich text for a prompt: <c>{{ }}</c> keys coloured, everything else literal.
        /// IMGUI rich text has no escapes, so a zero-width space after each
        /// <c>&lt;</c> stops the prompt's own angle brackets from being read as tags.
        /// </summary>
        internal static string Highlight(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string escaped = text.Replace("<", "<​");
            return Regex.Replace(escaped, @"\{\{.*?\}\}", m => "<color=#c586c0>" + m.Value + "</color>");
        }

        static GUIStyle RichStyle()
        {
            if (s_Rich != null) return s_Rich;
            s_Rich = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
            return s_Rich;
        }

        /// <summary>Searchable list of the provider's Text IDs.</summary>
        sealed class TextIdDropdown : AdvancedDropdown
        {
            readonly IReadOnlyList<string> _ids;
            readonly string _current;
            readonly Action<string> _pick;

            public TextIdDropdown(IReadOnlyList<string> ids, string current, Action<string> pick)
                : base(new AdvancedDropdownState())
            {
                _ids = ids;
                _current = current;
                _pick = pick;
                minimumSize = new Vector2(280f, 320f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Text IDs");
                if (_ids.Count == 0)
                {
                    root.AddChild(new AdvancedDropdownItem("(no keys)") { enabled = false });
                    return root;
                }
                foreach (var id in _ids)
                {
                    string name = id == _current ? id + "  ✓" : id;
                    root.AddChild(new TextIdItem(name, id));
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is TextIdItem text) _pick(text.Id);
            }

            sealed class TextIdItem : AdvancedDropdownItem
            {
                public readonly string Id;
                public TextIdItem(string name, string id) : base(name) { Id = id; }
            }
        }
    }

    /// <summary>
    /// Preview results for the inspector and Check texts. Providers can be slow
    /// (file parsing, table loads), and IMGUI repaints often, so results are kept
    /// for a short time and dropped when <see cref="TryllTextEditorHooks.Revision"/> changes.
    /// </summary>
    static class TryllTextPreviewCache
    {
        const double MaxAgeSeconds = 2.0;
        const int MaxEntries = 1024;

        struct Entry<T>
        {
            public T Value;
            public double Time;
            public int Revision;
        }

        static readonly Dictionary<string, Entry<TryllTextPreview.Result>> s_Results
            = new Dictionary<string, Entry<TryllTextPreview.Result>>();
        static readonly Dictionary<string, Entry<IReadOnlyList<string>>> s_Locales
            = new Dictionary<string, Entry<IReadOnlyList<string>>>();
        static readonly Dictionary<string, Entry<string>> s_Texts
            = new Dictionary<string, Entry<string>>();

        public static TryllTextPreview.Result Evaluate(
            ITryllTextProvider provider, string table, string id, string locale)
        {
            string key = ProviderKey(provider) + "|" + table + "|" + id + "|" + locale;
            return Get(s_Results, key, () => TryllTextPreview.Evaluate(provider, table, id, locale));
        }

        public static IReadOnlyList<string> Locales(ITryllTextProviderEditor editor)
        {
            return Get(s_Locales, ProviderKey(editor),
                () => editor.ListLocales() ?? Array.Empty<string>());
        }

        public static string Text(ITryllTextProviderEditor editor, string table, string id, string locale)
        {
            string key = ProviderKey(editor) + "|" + table + "|" + id + "|" + locale;
            return Get(s_Texts, key, () => editor.GetTextForPreview(table, id, locale));
        }

        static T Get<T>(Dictionary<string, Entry<T>> cache, string key, Func<T> compute)
        {
            double now = EditorApplication.timeSinceStartup;
            int revision = TryllTextEditorHooks.Revision;
            if (cache.TryGetValue(key, out var entry)
                && entry.Revision == revision
                && now - entry.Time < MaxAgeSeconds)
                return entry.Value;

            var value = compute();
            if (cache.Count >= MaxEntries) cache.Clear();
            cache[key] = new Entry<T> { Value = value, Time = now, Revision = revision };
            return value;
        }

        static string ProviderKey(object provider)
        {
            if (provider == null) return "none";
            if (provider is UnityEngine.Object unityObject)
                return unityObject.GetInstanceID().ToString();
            return provider.GetType().FullName + "#" + provider.GetHashCode();
        }
    }

    [CustomEditor(typeof(TryllWorkflowAsset))]
    sealed class TryllWorkflowAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Check texts"))
                TryllTextCheckWindow.Open((TryllWorkflowAsset)target);
        }
    }
}
