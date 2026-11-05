// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Multiline string whose vertical scroll is stored per property path.
    /// Unity's built-in <c>TextAreaDrawer</c> keeps one <c>m_ScrollPosition</c> on a
    /// drawer the inspector reuses for every array element (the handler cache
    /// ignores array indices). Sibling System Prompt fields therefore scroll
    /// together. This draws the same label-above-field layout and keeps a
    /// separate offset for each path.
    /// </summary>
    internal static class TryllIndependentTextArea
    {
        const float WheelSpeed = 10f;

        static readonly Dictionary<string, float> ScrollY = new Dictionary<string, float>();

        static GUIStyle s_Inner;
        static GUIContent s_Measure;
        static bool s_ProSkin;

        public static void Draw(Rect position, SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, true);
                return;
            }

            Draw(position, property, new GUIContent(property.displayName, property.tooltip));
        }

        static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            float labelHeight = EditorGUIUtility.singleLineHeight;
            var labelLine = new Rect(position.x, position.y, position.width, labelHeight);
            // Drawn first so its click is not taken by the label's focus click below.
            TryllTextEditWindow.DrawOpenButton(TryllTextEditWindow.OpenButtonRect(labelLine), property);
            EditorGUI.LabelField(labelLine, label);

            string key = KeyOf(property);
            var labelClick = EditorGUI.IndentedRect(labelLine);
            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && labelClick.Contains(evt.mousePosition))
            {
                GUI.FocusControl(key);
                evt.Use();
            }

            var fieldRect = EditorGUI.IndentedRect(new Rect(
                position.x, labelLine.yMax, position.width, Mathf.Max(0f, position.height - labelHeight)));
            DrawField(fieldRect, property, key);

            EditorGUI.EndProperty();
        }

        /// <summary>The wrapped, scrollable box alone, for a drawer that draws its own label.</summary>
        internal static void DrawField(Rect fieldRect, SerializedProperty property)
        {
            DrawField(fieldRect, property, KeyOf(property));
        }

        internal static string ScrollKey(int instanceId, string propertyPath)
        {
            return instanceId.ToString(CultureInfo.InvariantCulture) + "|" + (propertyPath ?? string.Empty);
        }

        internal static float ClampScroll(float scroll, float contentHeight, float viewHeight)
        {
            return Mathf.Clamp(scroll, 0f, Mathf.Max(0f, contentHeight - viewHeight));
        }

        internal static float ScrollToReveal(float scroll, float cursorY, float lineHeight, float viewHeight)
        {
            if (cursorY < scroll)
                return cursorY;
            float bottom = cursorY + Mathf.Max(0f, lineHeight);
            if (bottom > scroll + viewHeight)
                return bottom - viewHeight;
            return scroll;
        }

        static string KeyOf(SerializedProperty property)
        {
            var target = property.serializedObject != null ? property.serializedObject.targetObject : null;
            int id = target != null ? target.GetInstanceID() : 0;
            return ScrollKey(id, property.propertyPath);
        }

        static void DrawField(Rect fieldRect, SerializedProperty property, string key)
        {
            if (fieldRect.width < 1f || fieldRect.height < 1f)
                return;

            var evt = Event.current;
            bool focused = GUI.GetNameOfFocusedControl() == key;
            if (evt.type == EventType.Repaint)
            {
                bool hover = fieldRect.Contains(evt.mousePosition);
                EditorStyles.textArea.Draw(fieldRect, GUIContent.none, hover, false, false, focused);
            }

            var box = EditorStyles.textArea;
            Rect clip = box.padding.Remove(fieldRect);
            if (clip.width < 1f || clip.height < 1f)
                return;

            string text = property.stringValue ?? string.Empty;
            var inner = InnerStyle();
            float barW = GUI.skin.verticalScrollbar.fixedWidth;
            if (barW < 1f)
                barW = 14f;

            float wrapW = clip.width;
            float textH = MeasureHeight(inner, text, wrapW);
            bool overflows = textH > clip.height + 0.5f;
            if (overflows)
            {
                wrapW = Mathf.Max(1f, clip.width - barW);
                textH = MeasureHeight(inner, text, wrapW);
                overflows = textH > clip.height + 0.5f;
            }

            if (!ScrollY.TryGetValue(key, out float scroll))
                scroll = 0f;
            float scrollBefore = scroll;

            if (overflows && evt.type == EventType.ScrollWheel && fieldRect.Contains(evt.mousePosition))
            {
                scroll = ClampScroll(scroll + evt.delta.y * WheelSpeed, textH, clip.height);
                evt.Use();
            }

            float visibleW = overflows ? wrapW : clip.width;
            var textRect = new Rect(0f, 0f, visibleW, Mathf.Max(textH, clip.height));

            bool keyEvent = evt.type == EventType.KeyDown;
            GUI.BeginClip(clip, new Vector2(0f, Mathf.Round(-scroll)), Vector2.zero, false);
            try
            {
                GUI.SetNextControlName(key);
                string edited = GUI.TextArea(textRect, text, inner);
                if (!string.Equals(edited, text, System.StringComparison.Ordinal))
                {
                    property.stringValue = edited ?? string.Empty;
                    GUI.changed = true;
                }

                if (keyEvent && GUI.GetNameOfFocusedControl() == key)
                {
                    var editor = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
                    if (editor != null)
                    {
                        float line = inner.lineHeight > 1f ? inner.lineHeight : 16f;
                        scroll = ScrollToReveal(scroll, editor.graphicalCursorPos.y, line, clip.height);
                        editor.scrollOffset = Vector2.zero;
                    }
                }
            }
            finally
            {
                GUI.EndClip();
            }

            if (overflows)
            {
                var bar = new Rect(clip.xMax - barW, clip.y + 1f, barW, Mathf.Max(1f, clip.height - 2f));
                scroll = GUI.VerticalScrollbar(bar, scroll, clip.height, 0f, textH);
            }

            scroll = ClampScroll(scroll, textH, clip.height);
            ScrollY[key] = scroll;
            if (Mathf.Abs(scroll - scrollBefore) > 0.01f)
                HandleUtility.Repaint();
        }

        static float MeasureHeight(GUIStyle style, string text, float width)
        {
            if (s_Measure == null)
                s_Measure = new GUIContent();
            s_Measure.text = string.IsNullOrEmpty(text) ? " " : text;
            return Mathf.Max(style.CalcHeight(s_Measure, Mathf.Max(1f, width)), style.lineHeight);
        }

        static GUIStyle InnerStyle()
        {
            if (s_Inner != null && s_ProSkin == EditorGUIUtility.isProSkin)
                return s_Inner;

            s_ProSkin = EditorGUIUtility.isProSkin;
            s_Inner = new GUIStyle(EditorStyles.textArea)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                richText = false,
                wordWrap = true,
            };
            ClearBackground(s_Inner.normal);
            ClearBackground(s_Inner.hover);
            ClearBackground(s_Inner.active);
            ClearBackground(s_Inner.focused);
            ClearBackground(s_Inner.onNormal);
            ClearBackground(s_Inner.onHover);
            ClearBackground(s_Inner.onActive);
            ClearBackground(s_Inner.onFocused);
            return s_Inner;
        }

        static void ClearBackground(GUIStyleState state)
        {
            if (state == null)
                return;
            state.background = null;
            state.scaledBackgrounds = new Texture2D[0];
        }
    }
}
