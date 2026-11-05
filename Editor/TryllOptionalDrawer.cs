// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Inspector row for optional scalar node params: [toggle] Label [value].
    /// Reads [TryllOptionalRange] / [TryllOptionalMin] from the outer field (not Unity [Range]/[Min],
    /// which hijack drawing and break TryllOptional fields).
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllOptional<>))]
    internal sealed class TryllOptionalDrawer : PropertyDrawer
    {
        private const float ToggleWidth = 18f;
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var hasValueProp = property.FindPropertyRelative("_hasValue");
            var valueProp = property.FindPropertyRelative("_value");

            if (hasValueProp == null || valueProp == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var toggleRect = new Rect(position.x, position.y, ToggleWidth, position.height);
            var labelRect = new Rect(toggleRect.xMax + Spacing, position.y, EditorGUIUtility.labelWidth - ToggleWidth - Spacing, position.height);
            var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.PropertyField(toggleRect, hasValueProp, GUIContent.none);
            EditorGUI.LabelField(labelRect, label);

            EditorGUI.BeginDisabledGroup(!hasValueProp.boolValue);
            DrawValueControl(valueRect, valueProp);
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }

        private void DrawValueControl(Rect rect, SerializedProperty valueProp)
        {
            var valueType = fieldInfo.FieldType.GetGenericArguments()[0];
            var range = (TryllOptionalRangeAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(TryllOptionalRangeAttribute));
            var min = (TryllOptionalMinAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(TryllOptionalMinAttribute));

            if (valueType == typeof(float))
            {
                if (range != null)
                {
                    valueProp.floatValue = EditorGUI.Slider(rect, valueProp.floatValue, range.Min, range.Max);
                    return;
                }

                var f = EditorGUI.DelayedFloatField(rect, valueProp.floatValue);
                if (min != null)
                    f = Mathf.Max(f, min.Min);
                valueProp.floatValue = f;
                return;
            }

            if (valueType == typeof(int))
            {
                if (range != null)
                {
                    valueProp.intValue = EditorGUI.IntSlider(rect, valueProp.intValue, (int)range.Min, (int)range.Max);
                    return;
                }

                var i = EditorGUI.DelayedIntField(rect, valueProp.intValue);
                if (min != null)
                    i = Math.Max(i, (int)min.Min);
                valueProp.intValue = i;
                return;
            }

            EditorGUI.PropertyField(rect, valueProp, GUIContent.none);
        }
    }
}
