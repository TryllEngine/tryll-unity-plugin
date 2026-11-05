// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
//
// Material-Instance pattern: the workflow asset
// owns the declaration (name + type); a TryllAgentComponent's VariableOverrides
// may only override the initial value. This drawer greys out (and labels) an
// override whose Name/Type no longer matches any declared variable — the
// component-side merge (TryllAgentComponent.MergeVariableOverrides) independently
// skips such entries with a warning at agent-creation time, so this is purely a
// visual heads-up, not the source of truth for the actual skip.
//
// The primary Inspector UX for overrides is TryllAgentComponentEditor (one row
// per declaration). This drawer remains for the legacy/stale foldout and any
// direct PropertyField drawing of TryllVariableOverride elements.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    [CustomPropertyDrawer(typeof(TryllVariableOverride))]
    internal sealed class TryllVariableOverrideDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            var valueProp = property.FindPropertyRelative("Value");
            float valueHeight = valueProp != null
                ? TryllVariableDeclGui.GetValueHeight(valueProp, EffectiveType(property))
                : line;
            // Name+Enabled row, Type row, Value row.
            return line + Spacing + line + Spacing + valueHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var nameProp    = property.FindPropertyRelative("Name");
            var enabledProp = property.FindPropertyRelative("Enabled");
            var valueProp   = property.FindPropertyRelative("Value");
            var typeProp    = valueProp.FindPropertyRelative("Type");

            // Sync the override's Type from the declared variable of the same
            // name so the (read-only) Type field and the shown value field match
            // the declaration — otherwise the Type stays at its Int default and
            // the component-side merge drops the override as a type mismatch.
            var declaredType = DeclaredTypeForName(property, nameProp.stringValue);
            if (declaredType.HasValue && typeProp.enumValueIndex != (int)declaredType.Value)
                typeProp.enumValueIndex = (int)declaredType.Value;

            bool isStale = enabledProp.boolValue && !string.IsNullOrEmpty(nameProp.stringValue)
                           && !declaredType.HasValue;

            float line = EditorGUIUtility.singleLineHeight;
            const float toggleWidth = 18f;

            var toggleRect = new Rect(position.x, position.y, toggleWidth, line);
            var nameRect   = new Rect(toggleRect.xMax + Spacing, position.y, position.width - toggleWidth - Spacing, line);
            EditorGUI.PropertyField(toggleRect, enabledProp, GUIContent.none);

            var prevColor = GUI.color;
            if (isStale) GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, 0.5f);
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent(isStale ? "Name (stale)" : "Name"));

            var typeRect = new Rect(position.x, toggleRect.yMax + Spacing, position.width, line);
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Type (declared)"));

            var type = (TryllVariableType)typeProp.enumValueIndex;
            float valueHeight = TryllVariableDeclGui.GetValueHeight(valueProp, type);
            var valueRect = new Rect(position.x, typeRect.yMax + Spacing, position.width, valueHeight);
            using (new EditorGUI.DisabledScope(!enabledProp.boolValue))
                TryllVariableDeclGui.DrawValueField(valueRect, valueProp, type, new GUIContent("Value"));
            GUI.color = prevColor;

            EditorGUI.EndProperty();
        }

        private static TryllVariableType EffectiveType(SerializedProperty overrideProp)
        {
            var nameProp = overrideProp.FindPropertyRelative("Name");
            var typeProp = overrideProp.FindPropertyRelative("Value").FindPropertyRelative("Type");
            return DeclaredTypeForName(overrideProp, nameProp.stringValue)
                   ?? (TryllVariableType)typeProp.enumValueIndex;
        }

        private static TryllVariableType? DeclaredTypeForName(SerializedProperty overrideProp, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var target = overrideProp.serializedObject.targetObject;
            List<TryllVariableDecl> declared = null;
            if (target is TryllAgentComponent component)
                declared = TryllVariableOverrideListUtil.ResolveDeclared(component);
            else if (target is TryllWorkflowAsset asset)
                declared = asset.Variables;
            else
                return null;

            if (declared == null) return null;
            foreach (var d in declared)
                if (d != null && d.Name == name)
                    return d.Type;
            return null;
        }
    }
}
