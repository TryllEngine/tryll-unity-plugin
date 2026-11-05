// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Inspector rows for one <see cref="TryllVariableDecl"/>: Name, Type, and only the
    /// value field active for the current <see cref="TryllVariableType"/> — works both
    /// in the default Inspector (e.g. <see cref="TryllWorkflowAsset.Variables"/>) and any
    /// custom Inspector/graph-window side panel that draws the list with
    /// <c>EditorGUILayout.PropertyField</c>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllVariableDecl))]
    internal sealed class TryllVariableDeclDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            var typeProp = property.FindPropertyRelative("Type");
            float valueHeight = TryllVariableDeclGui.GetValueHeight(
                property, (TryllVariableType)typeProp.enumValueIndex);
            return line + Spacing + line + Spacing + line + Spacing + valueHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var nameProp = property.FindPropertyRelative("Name");
            var typeProp = property.FindPropertyRelative("Type");
            var allowProp = property.FindPropertyRelative("AllowOutputSubstitution");
            float line = EditorGUIUtility.singleLineHeight;

            var nameRect = new Rect(position.x, position.y, position.width, line);
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Name"));

            var typeRect = new Rect(position.x, nameRect.yMax + Spacing, position.width, line);
            EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Type"));

            var type = (TryllVariableType)typeProp.enumValueIndex;
            bool supportsSubstitution = type != TryllVariableType.StringSet;
            if (!supportsSubstitution)
                allowProp.boolValue = false;

            var allowRect = new Rect(position.x, typeRect.yMax + Spacing, position.width, line);
            using (new EditorGUI.DisabledScope(!supportsSubstitution))
                EditorGUI.PropertyField(
                    allowRect, allowProp, new GUIContent("Allow Output Substitution"));

            float valueHeight = TryllVariableDeclGui.GetValueHeight(property, type);
            var valueRect = new Rect(position.x, allowRect.yMax + Spacing, position.width, valueHeight);
            TryllVariableDeclGui.DrawValueField(valueRect, property, type, new GUIContent("Value"));

            EditorGUI.EndProperty();
        }
    }
}
