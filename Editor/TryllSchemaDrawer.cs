// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Inspector picker for <see cref="TryllSchemaAttribute"/> fields. Reads the
    /// selected bundled server variant's <c>data/schemas.json</c>, keeps unknown
    /// values as "Unregistered", and warns when an external server may use a
    /// different catalog.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllSchemaAttribute))]
    internal sealed class TryllSchemaDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(none)";
        private const float BadgeWidth = 92f;
        private const float BadgeGap = 4f;
        private const float MinWarningHeight = 38f;
        private const float HelpBoxWidthSlack = 40f;

        private static readonly Color s_Registered = new Color(0.30f, 0.78f, 0.45f);
        private static readonly Color s_Unregistered = new Color(0.55f, 0.55f, 0.58f);

        private static GUIStyle s_BadgeStyle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;
            string warning = ExternalServerWarning();
            if (warning != null)
                h += EditorGUIUtility.standardVerticalSpacing + WarningHeight(warning);
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var catalog = TryllSchemaCatalog.LoadBundled();
            string current = property.stringValue ?? string.Empty;

            var names = new List<string> { string.Empty };
            names.AddRange(catalog.Names);
            if (!string.IsNullOrEmpty(current) && !names.Contains(current))
                names.Add(current);

            var display = new string[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                if (string.IsNullOrEmpty(names[i]))
                {
                    display[i] = NoneLabel;
                    continue;
                }
                bool known = catalog.Contains(names[i]);
                display[i] = known ? names[i] : $"{names[i]}  (Unregistered)";
            }

            int selected = Mathf.Max(0, names.IndexOf(current));
            float rowHeight = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginProperty(position, label, property);
            var popupRect = new Rect(position.x, position.y, position.width - BadgeWidth - BadgeGap, rowHeight);
            var badgeRect = new Rect(popupRect.xMax + BadgeGap, position.y, BadgeWidth, rowHeight);
            int next = EditorGUI.Popup(popupRect, label.text, selected, display);
            if (next != selected)
                property.stringValue = names[next];
            DrawBadge(badgeRect, current, catalog);
            string warning = ExternalServerWarning();
            if (warning != null)
            {
                var warnRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width, WarningHeight(warning));
                EditorGUI.HelpBox(warnRect, warning, MessageType.Warning);
            }
            EditorGUI.EndProperty();
        }

        private static void DrawBadge(Rect rect, string name, TryllSchemaCatalog catalog)
        {
            if (string.IsNullOrEmpty(name))
                return;
            bool known = catalog != null && catalog.Contains(name);
            Color color = known ? s_Registered : s_Unregistered;
            var fill = color;
            fill.a = 0.22f;
            EditorGUI.DrawRect(rect, fill);
            if (s_BadgeStyle == null)
            {
                s_BadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            s_BadgeStyle.normal.textColor = color;
            GUI.Label(rect, known ? "Catalog" : "Unregistered", s_BadgeStyle);
        }

        internal static string ExternalServerWarning(TryllRuntimeSettings settings = null)
        {
            settings ??= TryllRuntimeSettings.Load();
            if (settings == null) return null;
            if (settings.AutoLaunchServer) return null;
            return "This picker reads the bundled server catalog. An external server may expose a different schema list; the server remains authoritative.";
        }

        private static float WarningHeight(string text)
        {
            float width = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - HelpBoxWidthSlack);
            float measured = EditorStyles.helpBox.CalcHeight(new GUIContent(text), width);
            return Mathf.Max(MinWarningHeight, measured);
        }
    }
}
