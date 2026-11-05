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
    /// Reserved Choice value written when the gate refuses the winner.
    /// Not a menu letter. Keep in sync with nodes.fbs uncertain_choice.
    /// </summary>
    internal static class TryllUncertainChoice
    {
        internal const string Id = "#uncertain";
    }

    /// <summary>
    /// Shows only the fields the current attribute kind uses. Boolean has a
    /// statement and a true/false trigger. Choice has a menu, a margin, and
    /// an uncertain-value dropdown. Score has an ordered ladder and a
    /// normalised threshold.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllClassifyAttributeDef))]
    internal sealed class TryllClassifyAttributeDefDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            // The foldout still draws when collapsed. Reporting the open height
            // here leaves a blank block the size of the attribute inspector.
            if (!property.isExpanded)
                return height;
            var kind = KindOf(property);
            foreach (var name in VisibleFields(kind))
            {
                var child = property.FindPropertyRelative(name);
                if (child == null) continue;
                height += Spacing + EditorGUI.GetPropertyHeight(child, true);
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            // Unity's list "+" inserts a zeroed element and skips C# field
            // initializers. Hidden kind-specific floats then stay 0 instead of
            // the schema default (score_threshold 0.5, margin 0.15), and the
            // validator rejects a Choice the author cannot see a reason for.
            // Apply immediately: a direct SerializedProperty write does not set
            // GUI.changed, and the graph inspector discards unapplied writes on
            // its next Update(). A collapsed row never reaches the change check
            // below, so the write has to be committed here.
            if (CoerceInapplicableDefaults(property))
            {
                property.serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
            }
            float line = EditorGUIUtility.singleLineHeight;
            var nameProp = property.FindPropertyRelative("Name");
            var kindProp = property.FindPropertyRelative("Kind");
            var header = new Rect(position.x, position.y, position.width, line);
            var title = string.IsNullOrEmpty(nameProp.stringValue) ? label.text : nameProp.stringValue;
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, title, true);
            if (property.isExpanded)
            {
                float y = header.yMax + Spacing;
                EditorGUI.indentLevel++;
                var kind = KindOf(property);
                string oldAttributeName = nameProp != null ? nameProp.stringValue : string.Empty;
                EditorGUI.BeginChangeCheck();
                foreach (var field in VisibleFields(kind))
                {
                    var child = property.FindPropertyRelative(field);
                    if (child == null) continue;
                    float h = EditorGUI.GetPropertyHeight(child, true);
                    var row = new Rect(position.x, y, position.width, h);
                    if (field == "UncertainChoice")
                        DrawUncertainChoice(property, child, row);
                    else if (field == "SystemPrompt" || field == "Statement")
                        TryllIndependentTextArea.Draw(row, child);
                    else
                        EditorGUI.PropertyField(row, child, true);
                    y += h + Spacing;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    string newAttributeName = nameProp != null ? nameProp.stringValue : string.Empty;
                    if (oldAttributeName != newAttributeName
                        && !string.IsNullOrEmpty(oldAttributeName)
                        && !string.IsNullOrEmpty(newAttributeName))
                    {
                        PropagateAttributeRename(property, oldAttributeName, newAttributeName);
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        static void PropagateAttributeRename(
            SerializedProperty property, string oldAttributeName, string newAttributeName)
        {
            TryllGraphIntrospection.TryFindEnclosingGraph(property, out var graphProp, out string ownerNodeName);
            var graph = graphProp != null
                ? TryllGraphIntrospection.GetTargetObjectOfProperty(graphProp) as TryllGraphDescription
                : null;
            if (graph?.Nodes == null || string.IsNullOrEmpty(ownerNodeName))
                return;
            TryllNodeDescription owner = null;
            foreach (var node in graph.Nodes)
            {
                if (node != null && node.Name == ownerNodeName)
                {
                    owner = node;
                    break;
                }
            }
            TryllRenamePropagation.PropagateAttributeRename(
                graph, owner, oldAttributeName, newAttributeName);
        }

        static TryllAttributeKind KindOf(SerializedProperty property)
        {
            var kind = property.FindPropertyRelative("Kind");
            if (kind == null) return TryllAttributeKind.Choice;
            return (TryllAttributeKind)kind.enumValueIndex;
        }

        /// <summary>
        /// Write schema defaults onto scalars the current kind does not use.
        /// Those fields are hidden, so a zero from list insertion (or a value
        /// left behind by a kind change) cannot be corrected in the inspector.
        /// </summary>
        static bool CoerceInapplicableDefaults(SerializedProperty property)
        {
            var kind = KindOf(property);
            bool changed = false;
            if (kind != TryllAttributeKind.Boolean)
                changed |= SetEnum(property, "BooleanTrigger", (int)TryllBooleanTrigger.Never);
            if (kind != TryllAttributeKind.Score)
            {
                changed |= SetEnum(property, "ScoreTrigger", (int)TryllScoreTrigger.Never);
                changed |= SetFloat(property, "ScoreThreshold", 0.5f);
            }
            if (kind != TryllAttributeKind.Choice)
                changed |= SetFloat(property, "Margin", 0.15f);
            if (kind == TryllAttributeKind.Score)
                changed |= SetFloat(property, "Threshold", 0.5f);
            return changed;
        }

        static bool SetFloat(SerializedProperty property, string name, float expected)
        {
            var child = property.FindPropertyRelative(name);
            if (child == null || child.propertyType != SerializedPropertyType.Float)
                return false;
            if (Mathf.Abs(child.floatValue - expected) <= 0.0001f)
                return false;
            child.floatValue = expected;
            return true;
        }

        static bool SetEnum(SerializedProperty property, string name, int expected)
        {
            var child = property.FindPropertyRelative(name);
            if (child == null || child.propertyType != SerializedPropertyType.Enum)
                return false;
            if (child.enumValueIndex == expected)
                return false;
            child.enumValueIndex = expected;
            return true;
        }

        static string[] VisibleFields(TryllAttributeKind kind)
        {
            switch (kind)
            {
                case TryllAttributeKind.Boolean:
                    return new[] { "Name", "Kind", "Statement", "SystemPrompt", "Threshold", "BooleanTrigger" };
                case TryllAttributeKind.Score:
                    return new[] { "Name", "Kind", "Choices", "SystemPrompt", "ScoreTrigger", "ScoreThreshold" };
                default:
                    return new[]
                    {
                        "Name", "Kind", "Choices", "SystemPrompt", "Threshold", "Margin", "UncertainChoice",
                    };
            }
        }

        static void DrawUncertainChoice(
            SerializedProperty attribute, SerializedProperty value, Rect rect)
        {
            EditorGUI.BeginProperty(rect, GUIContent.none, value);
            var options = new List<string> { TryllUncertainChoice.Id };
            var choices = attribute.FindPropertyRelative("Choices");
            if (choices != null && choices.isArray)
            {
                for (int i = 0; i < choices.arraySize; i++)
                {
                    var id = choices.GetArrayElementAtIndex(i)?.FindPropertyRelative("Id");
                    if (id == null || string.IsNullOrEmpty(id.stringValue) || options.Contains(id.stringValue))
                        continue;
                    options.Add(id.stringValue);
                }
            }
            string current = value.stringValue ?? string.Empty;
            if (!string.IsNullOrEmpty(current) && !options.Contains(current))
                options.Add(current);
            int selected = string.IsNullOrEmpty(current) ? -1 : options.IndexOf(current);
            var labels = new GUIContent[options.Count];
            for (int i = 0; i < options.Count; i++)
                labels[i] = new GUIContent(options[i]);
            int next = EditorGUI.Popup(
                rect, new GUIContent("Uncertain", value.tooltip), selected, labels);
            if (next >= 0 && next < options.Count && options[next] != current)
                value.stringValue = options[next];
            EditorGUI.EndProperty();
        }
    }
}
