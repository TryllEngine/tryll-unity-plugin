// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEditor;
using UnityEngine;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Inspector property drawer for fields annotated with <see cref="TryllSlotNameAttribute"/>
    /// (<c>output_name</c>). Renders
    /// <see cref="EditorGUI.DelayedTextField"/>; empty stays empty (meaning "use the node's
    /// own name" — the default). On commit, a non-empty value is grammar/reserved-name
    /// checked (matching the server's rules — see CompiledGraph::Compile) but never
    /// uniquified: unlike node names, two nodes legally sharing an effective output slot is
    /// allowed server-side (the server only warns about the resulting same-path double-write,
    /// non-blocking), so forcing uniqueness here would reject valid graphs. Node-name
    /// uniqueness is unrelated and stays handled separately by
    /// <see cref="TryllNodeRenameDrawer"/>. After a successful commit, the change in
    /// *effective* slot name (old override, or the node's own name when the override was
    /// empty → new override, or the node's own name when the new override is empty) is
    /// propagated via <see cref="TryllRenamePropagation.PropagateSlotRename"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllSlotNameAttribute))]
    internal sealed class TryllSlotNameDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            string oldOverride = property.stringValue ?? string.Empty;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            string requested = EditorGUI.DelayedTextField(position, label, oldOverride);
            if (EditorGUI.EndChangeCheck() && requested != oldOverride)
                CommitRename(property, oldOverride, requested);
            EditorGUI.EndProperty();
        }

        private static void CommitRename(SerializedProperty property, string oldOverride, string requested)
        {
            TryllGraphIntrospection.TryFindEnclosingGraph(property, out var graphProp, out string ownerNodeName);
            var graph = graphProp != null
                ? TryllGraphIntrospection.GetTargetObjectOfProperty(graphProp) as TryllGraphDescription
                : null;

            TryllNodeDescription ownerNode = null;
            if (graph?.Nodes != null && !string.IsNullOrEmpty(ownerNodeName))
                foreach (var n in graph.Nodes)
                    if (n != null && n.Name == ownerNodeName) { ownerNode = n; break; }

            if (graph?.Nodes == null || ownerNode?.Params == null)
            {
                // No graph context to validate/propagate against — accept as-is.
                property.stringValue = requested;
                return;
            }

            var slotNameField = TryllNodeTypeRegistry.GetSlotNameField(ownerNode.Params.GetType());
            if (slotNameField == null)
            {
                property.stringValue = requested;
                return;
            }

            string newOverride = requested ?? string.Empty;
            if (!string.IsNullOrEmpty(newOverride))
            {
                if (!TryllRenamePropagation.IsValidGrammar(newOverride) || TryllRenamePropagation.IsReserved(newOverride))
                {
                    Debug.LogWarning($"[Tryll] Invalid slot name '{newOverride}': must match [A-Za-z_][A-Za-z0-9_]* " +
                                      $"and not be the reserved '{TryllRenamePropagation.ReservedUserMessage}'. Reverted to '{oldOverride}'.");
                    property.stringValue = oldOverride;
                    return;
                }

                // Not uniquified: the server permits two nodes to legally share an effective
                // output slot (a non-blocking double-write warning only — see
                // CompiledGraph::Compile), so forcing uniqueness here would silently reject
                // an author's intentional shared-slot graph. Surface the same warning here.
                foreach (var n in graph.Nodes)
                {
                    if (n == null || n.Name == ownerNodeName) continue;
                    if (TryllSlotGraphAnalysis.GetEffectiveWriteSlot(n) == newOverride)
                    {
                        Debug.LogWarning($"[Tryll] Slot '{newOverride}' is also written by node '{n.Name}' — " +
                                          "legal, but a downstream reader may see whichever write happened last this turn.");
                        break;
                    }
                }
            }

            string oldEffective = string.IsNullOrEmpty(oldOverride) ? ownerNodeName : oldOverride;
            string newEffective = string.IsNullOrEmpty(newOverride) ? ownerNodeName : newOverride;

            var undoHost = property.serializedObject.targetObject;
            Undo.RecordObject(undoHost, "Rename Tryll Slot");

            TryllNodeTypeRegistry.WriteSlotName(ownerNode.Params, slotNameField, newOverride);
            if (oldEffective != newEffective)
                TryllRenamePropagation.PropagateOwnedSlotRename(graph, ownerNode, oldEffective, newEffective);

            EditorUtility.SetDirty(undoHost);
            property.serializedObject.Update();
        }
    }
}
