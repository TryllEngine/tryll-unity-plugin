// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Shared Inspector helpers for drawing the active typed value field of a
    /// <see cref="TryllVariableDecl"/> (or the nested <c>Value</c> on an override).
    /// </summary>
    internal static class TryllVariableDeclGui
    {
        public static SerializedProperty ValuePropertyFor(SerializedProperty decl, TryllVariableType type)
        {
            if (decl == null) return null;
            switch (type)
            {
                case TryllVariableType.Int:       return decl.FindPropertyRelative("IntValue");
                case TryllVariableType.Float:     return decl.FindPropertyRelative("FloatValue");
                case TryllVariableType.String:    return decl.FindPropertyRelative("StringValue");
                case TryllVariableType.Bool:      return decl.FindPropertyRelative("BoolValue");
                case TryllVariableType.StringSet: return decl.FindPropertyRelative("StringSetValue");
                default:                          return null;
            }
        }

        public static float GetValueHeight(SerializedProperty decl, TryllVariableType type)
        {
            var valueProp = ValuePropertyFor(decl, type);
            if (valueProp == null) return EditorGUIUtility.singleLineHeight;
            return EditorGUI.GetPropertyHeight(valueProp, true);
        }

        public static void DrawValueField(
            Rect rect, SerializedProperty decl, TryllVariableType type, GUIContent label, bool includeChildren = true)
        {
            var valueProp = ValuePropertyFor(decl, type);
            if (valueProp == null) return;
            EditorGUI.PropertyField(rect, valueProp, label ?? GUIContent.none, includeChildren);
        }

        /// <summary>
        /// Copy name/type/value fields from <paramref name="source"/> onto the
        /// serialized <paramref name="destDecl"/> (a <see cref="TryllVariableDecl"/> property).
        /// </summary>
        public static void WriteDecl(SerializedProperty destDecl, TryllVariableDecl source)
        {
            if (destDecl == null || source == null) return;
            destDecl.FindPropertyRelative("Name").stringValue = source.Name ?? "";
            destDecl.FindPropertyRelative("Type").enumValueIndex = (int)source.Type;
            destDecl.FindPropertyRelative("IntValue").longValue = source.IntValue;
            destDecl.FindPropertyRelative("FloatValue").doubleValue = source.FloatValue;
            destDecl.FindPropertyRelative("StringValue").stringValue = source.StringValue ?? "";
            destDecl.FindPropertyRelative("BoolValue").boolValue = source.BoolValue;
            destDecl.FindPropertyRelative("AllowOutputSubstitution").boolValue =
                source.Type != TryllVariableType.StringSet && source.AllowOutputSubstitution;

            var setProp = destDecl.FindPropertyRelative("StringSetValue");
            setProp.ClearArray();
            var srcSet = source.StringSetValue;
            if (srcSet == null) return;
            for (int i = 0; i < srcSet.Count; i++)
            {
                setProp.InsertArrayElementAtIndex(i);
                setProp.GetArrayElementAtIndex(i).stringValue = srcSet[i] ?? "";
            }
        }

        /// <summary>Read a <see cref="TryllVariableDecl"/> snapshot from a serialized property.</summary>
        public static TryllVariableDecl ReadDecl(SerializedProperty decl)
        {
            if (decl == null) return null;
            var result = new TryllVariableDecl
            {
                Name        = decl.FindPropertyRelative("Name").stringValue,
                Type        = (TryllVariableType)decl.FindPropertyRelative("Type").enumValueIndex,
                IntValue    = decl.FindPropertyRelative("IntValue").longValue,
                FloatValue  = decl.FindPropertyRelative("FloatValue").doubleValue,
                StringValue = decl.FindPropertyRelative("StringValue").stringValue,
                BoolValue   = decl.FindPropertyRelative("BoolValue").boolValue,
                AllowOutputSubstitution =
                    (TryllVariableType)decl.FindPropertyRelative("Type").enumValueIndex !=
                    TryllVariableType.StringSet &&
                    decl.FindPropertyRelative("AllowOutputSubstitution").boolValue,
                StringSetValue = new System.Collections.Generic.List<string>(),
            };
            var setProp = decl.FindPropertyRelative("StringSetValue");
            for (int i = 0; i < setProp.arraySize; i++)
                result.StringSetValue.Add(setProp.GetArrayElementAtIndex(i).stringValue);
            return result;
        }
    }
}
