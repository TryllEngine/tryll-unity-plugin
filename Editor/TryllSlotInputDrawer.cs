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
    /// Inspector property drawer for fields annotated with <see cref="TryllSlotInputAttribute"/>
    /// (the <c>input</c>-style fields on Generate/GenerateAndSpeak/Retrieve/Speak/ClassifyIntent/
    /// ClassifyIntentLLM/ToolCall).
    ///
    /// Renders a dropdown menu listing every producer slot in the graph —
    /// no free-text mode:
    /// <list type="bullet">
    ///   <item>The node's default (empty value) — labelled "User Message". Always enabled,
    ///     always first.</item>
    ///   <item>"Slot &lt;X&gt;" for every producer slot in the graph — enabled (available or
    ///     branch-dependent, with a short inline "(...)" note) or disabled with a short
    ///     inline reason (<see cref="TryllSlotGraphAnalysis.ClassifySlots"/>). Unity's
    ///     <see cref="GenericMenu"/> has no per-item tooltip, so the reason lives in the
    ///     label text.</item>
    /// </list>
    /// Entries are grouped: enabled entries first, then a separator, then disabled entries —
    /// keeps large graphs scannable.
    ///
    /// A non-empty current value that does not match any producer slot in the graph (stale
    /// after a rename/rewire, or authored out-of-band) is surfaced as a disabled, tinted menu
    /// entry so it stays visible; the field face is tinted too. The default entry and every
    /// enabled slot remain selectable, so the field is always escapable.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllSlotInputAttribute))]
    internal sealed class TryllSlotInputDrawer : PropertyDrawer
    {
        private static readonly Color s_InvalidColor = new Color(1f, 0.35f, 0.35f, 0.4f);

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
            string defaultLabel = DefaultLabelFor(property);
            // The server treats an empty `input` and an explicit "user_message" as
            // equivalent (both resolve to the turn's implicit input slot — see
            // Projection.h) — so explicit "user_message" is valid here too, not a
            // stale/missing producer slot (Unreal already handles both this way).
            bool isDefaultValue = string.IsNullOrEmpty(currentValue) || currentValue == "user_message";

            TryllGraphIntrospection.TryFindEnclosingGraph(property, out var graphProp, out string ownerNodeName);

            List<TryllSlotOption> options = null;
            if (graphProp != null)
            {
                var graph = TryllGraphIntrospection.GetTargetObjectOfProperty(graphProp) as TryllGraphDescription;
                if (graph != null)
                    options = TryllSlotGraphAnalysis.ClassifySlots(graph, ownerNodeName, currentValue);
            }
            options ??= new List<TryllSlotOption>();

            bool isCurrentInvalid = !isDefaultValue;
            if (isCurrentInvalid)
            {
                foreach (var option in options)
                {
                    if (option.Value != currentValue) continue;
                    isCurrentInvalid = !option.IsEnabled;
                    break;
                }
            }

            EditorGUI.BeginProperty(position, label, property);

            if (isCurrentInvalid)
                EditorGUI.DrawRect(position, s_InvalidColor);

            string buttonLabel = isDefaultValue ? defaultLabel : "Slot " + currentValue;
            if (isCurrentInvalid)
                buttonLabel += "  (not in graph)";

            Rect prefixRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            if (EditorGUI.DropdownButton(prefixRect, new GUIContent(buttonLabel), FocusType.Passive))
                ShowMenu(prefixRect, property, defaultLabel, currentValue, options);

            EditorGUI.EndProperty();
        }

        private static void ShowMenu(
            Rect activatorRect,
            SerializedProperty property,
            string defaultLabel,
            string currentValue,
            List<TryllSlotOption> options)
        {
            var menu = new GenericMenu();

            // ── Enabled section: default entry first, then available/branch-dependent slots ──
            bool isDefaultSelected = string.IsNullOrEmpty(currentValue) || currentValue == "user_message";
            menu.AddItem(new GUIContent(defaultLabel), isDefaultSelected, () => Apply(property, string.Empty));
            bool addedSeparator = false;
            foreach (var option in options)
            {
                if (!option.IsEnabled) continue;
                string note = option.ShortReason();
                string label = string.IsNullOrEmpty(note) ? "Slot " + option.Value : "Slot " + option.Value + "  " + note;
                menu.AddItem(new GUIContent(label), option.Value == currentValue, () => Apply(property, option.Value));
            }

            // ── Disabled section: not-upstream / start-unset / self / missing, etc. ──
            foreach (var option in options)
            {
                if (option.IsEnabled) continue;
                if (!addedSeparator)
                {
                    menu.AddSeparator(string.Empty);
                    addedSeparator = true;
                }
                string note = option.ShortReason();
                string label = string.IsNullOrEmpty(note) ? "Slot " + option.Value : "Slot " + option.Value + "  " + note;
                menu.AddDisabledItem(new GUIContent(label));
            }

            menu.DropDown(activatorRect);
        }

        private static void Apply(SerializedProperty property, string value)
        {
            property.stringValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// "User Message" — every input-bearing node (including <c>Speak</c>) defaults
        /// an empty <c>input</c> to the "user_message" slot.
        /// </summary>
        private string DefaultLabelFor(SerializedProperty property)
        {
            return "User Message";
        }
    }
}
