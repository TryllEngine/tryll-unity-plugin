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
    /// Inspector property drawer for fields annotated with <see cref="TryllNodeNameAttribute"/>.
    ///
    /// Renders the field as a dropdown of valid node names drawn from the enclosing
    /// <see cref="TryllGraphDescription"/>.  Unlike <see cref="TryllExitDrawer"/> there is no
    /// <c>&lt;End&gt;</c> option and no owner-node exclusion, because this field must always
    /// resolve to a real node (e.g. <c>StartNode</c>).
    ///
    /// A red-tinted background is drawn when the current value is non-empty and does not
    /// match any node in the graph (stale reference after a rename or delete).
    ///
    /// When the field is empty a <c>&lt;unset&gt;</c> placeholder is shown at the top of the
    /// dropdown, giving designers a visible cue that the field must be filled.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllNodeNameAttribute))]
    internal sealed class TryllNodeNameDrawer : PropertyDrawer
    {
        private static readonly Color s_InvalidColor = new Color(1f, 0.35f, 0.35f, 0.4f);
        private const string          UnsetLabel      = "<unset>";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            string currentValue = property.stringValue ?? string.Empty;

            // ── Find the enclosing graph ───────────────────────────────────────
            TryllGraphIntrospection.TryFindEnclosingGraph(
                property, out var graphProp, out _);

            // ── Build the option list ──────────────────────────────────────────
            // Index 0 is always <unset> (maps to empty string).
            var options = new List<string> { UnsetLabel };
            if (graphProp != null)
                options.AddRange(TryllGraphIntrospection.GetNodeNames(graphProp));

            // ── Determine current popup index ──────────────────────────────────
            bool isStale       = false;
            int  selectedIndex = 0; // default: <unset>
            if (!string.IsNullOrEmpty(currentValue))
            {
                int found = options.IndexOf(currentValue);
                if (found >= 0)
                {
                    selectedIndex = found;
                }
                else
                {
                    isStale = true;
                    options.Add(currentValue + " (missing)");
                    selectedIndex = options.Count - 1;
                }
            }

            // ── Draw ──────────────────────────────────────────────────────────
            EditorGUI.BeginProperty(position, label, property);

            if (isStale)
                EditorGUI.DrawRect(position, s_InvalidColor);

            int newIndex = EditorGUI.Popup(
                position,
                ObjectNames.NicifyVariableName(property.name),
                selectedIndex,
                options.ToArray());

            if (newIndex != selectedIndex)
                property.stringValue = newIndex == 0 ? string.Empty : options[newIndex];

            EditorGUI.EndProperty();
        }
    }
}
