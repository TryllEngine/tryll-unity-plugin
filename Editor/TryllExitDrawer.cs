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
    /// Inspector property drawer for fields annotated with <see cref="TryllExitAttribute"/>.
    ///
    /// Renders the exit-target string as a dropdown of valid node names drawn from the
    /// enclosing <see cref="TryllGraphDescription"/>.  The special <c>&lt;End&gt;</c> option
    /// maps to an empty string, meaning the turn finishes after this node.
    ///
    /// A red-tinted background is drawn when the current value is non-empty and does not
    /// match any node name in the graph (stale reference after a node rename or delete).
    ///
    /// Authoring notes
    /// ---------------
    /// The drawer walks the <see cref="SerializedProperty.propertyPath"/> to locate the
    /// nearest ancestor that is a <c>TryllGraphDescription</c>.  It supports both the
    /// <see cref="TryllWorkflowAsset"/> and <see cref="TryllAgentComponent"/> host types,
    /// as well as any other ScriptableObject / MonoBehaviour that directly embeds a
    /// <c>TryllGraphDescription</c> field.
    ///
    /// The drawer also excludes the exit's own node from the target list to discourage
    /// trivial self-loops (they are still valid at runtime for router-style graphs but
    /// are nearly always a mistake in simple linear graphs).
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllExitAttribute))]
    internal sealed class TryllExitDrawer : PropertyDrawer
    {
        private static readonly Color s_InvalidColor = new Color(1f, 0.35f, 0.35f, 0.4f);
        private const string EndLabel = "<End>";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // We only handle string fields.
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var exitAttr = (TryllExitAttribute)attribute;
            string currentValue = property.stringValue ?? string.Empty;

            // ── Find the enclosing graph and current node name ─────────────────
            TryllGraphIntrospection.TryFindEnclosingGraph(
                property, out var graph, out string currentNodeName);

            // ── Build the option list ──────────────────────────────────────────
            var options = new List<string> { EndLabel };
            if (graph != null)
            {
                foreach (string nodeName in TryllGraphIntrospection.GetNodeNames(graph))
                {
                    // Exclude the node that owns this exit to discourage self-loops.
                    if (nodeName != currentNodeName)
                        options.Add(nodeName);
                }
            }

            // ── Determine current popup index ──────────────────────────────────
            bool isStale = false;
            int selectedIndex = 0; // default: <End>
            if (!string.IsNullOrEmpty(currentValue))
            {
                int found = options.IndexOf(currentValue);
                if (found >= 0)
                    selectedIndex = found;
                else
                {
                    // Value is non-empty but not in the list: stale reference.
                    isStale = true;
                    // Add it temporarily so the popup can show something.
                    options.Add(currentValue + " (missing)");
                    selectedIndex = options.Count - 1;
                }
            }

            // ── Draw ──────────────────────────────────────────────────────────
            EditorGUI.BeginProperty(position, label, property);

            if (isStale)
            {
                EditorGUI.DrawRect(position, s_InvalidColor);
            }

            // Build a label that includes the exit-name suffix so users can see
            // which exit they are configuring at a glance:
            //   "Default Exit →"  (for exitAttr.ExitName == "default")
            string displayLabel = ObjectNames.NicifyVariableName(property.name) + " →";

            int newIndex = EditorGUI.Popup(position, displayLabel, selectedIndex, options.ToArray());

            if (newIndex != selectedIndex && !isStale)
            {
                // Write the new value back: index 0 → empty string (End).
                property.stringValue = newIndex == 0 ? string.Empty : options[newIndex];
            }
            else if (newIndex != selectedIndex && isStale)
            {
                // User chose a valid replacement for the stale value.
                property.stringValue = newIndex == 0 ? string.Empty : options[newIndex];
            }

            EditorGUI.EndProperty();
        }

    }
}
