// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// One choice row. Prompt is a multiline string; the built-in text area
    /// would share its scroll offset with every other choice in the list.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllClassifyAttributeChoice))]
    internal sealed class TryllClassifyAttributeChoiceDrawer : PropertyDrawer
    {
        const float Spacing = 2f;
        static readonly string[] ChildNames = { "Id", "Prompt", "Triggers" };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;
            foreach (var name in ChildNames)
            {
                var child = property.FindPropertyRelative(name);
                if (child == null)
                    continue;
                height += Spacing + EditorGUI.GetPropertyHeight(child, true);
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            var idProp = property.FindPropertyRelative("Id");
            var header = new Rect(position.x, position.y, position.width, line);
            string title = idProp != null && !string.IsNullOrEmpty(idProp.stringValue)
                ? idProp.stringValue
                : label.text;
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, title, true);
            if (property.isExpanded)
            {
                float y = header.yMax + Spacing;
                EditorGUI.indentLevel++;
                foreach (var name in ChildNames)
                {
                    var child = property.FindPropertyRelative(name);
                    if (child == null)
                        continue;
                    float h = EditorGUI.GetPropertyHeight(child, true);
                    var row = new Rect(position.x, y, position.width, h);
                    if (name == "Prompt")
                        TryllIndependentTextArea.Draw(row, child);
                    else
                        EditorGUI.PropertyField(row, child, true);
                    y += h + Spacing;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }
    }
}
