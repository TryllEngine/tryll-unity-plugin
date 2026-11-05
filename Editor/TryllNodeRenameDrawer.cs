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
    /// Inspector property drawer for <see cref="TryllNodeDescription.Name"/> (tagged
    /// <see cref="TryllNodeRenameAttribute"/>). Renders
    /// <see cref="EditorGUI.DelayedTextField"/> (commits on Enter/focus-loss) and, on
    /// commit, enforces grammar/reserved-name, uniquifies, and propagates the rename into
    /// every exit field, <c>StartNode</c>, and (when this node's effective slot was its own
    /// name) every <c>input</c> selector and <c>{{slot.&lt;old&gt;}}</c> template tag —
    /// see <see cref="TryllRenamePropagation"/>.
    ///
    /// Invalid or already-taken-after-uniquify-fails input reverts to the old name (a
    /// warning is logged) rather than silently accepting a bad rename.
    /// </summary>
    [CustomPropertyDrawer(typeof(TryllNodeRenameAttribute))]
    internal sealed class TryllNodeRenameDrawer : PropertyDrawer
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

            string oldName = property.stringValue ?? string.Empty;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            string requestedName = EditorGUI.DelayedTextField(position, label, oldName);
            if (EditorGUI.EndChangeCheck() && requestedName != oldName)
                CommitRename(property, oldName, requestedName);
            EditorGUI.EndProperty();
        }

        internal static void CommitRename(SerializedProperty property, string oldName, string requestedName)
        {
            if (!TryllGraphIntrospection.TryFindEnclosingGraph(property, out var graphProp, out _))
            {
                property.stringValue = requestedName;
                return;
            }

            var graph = TryllGraphIntrospection.GetTargetObjectOfProperty(graphProp) as TryllGraphDescription;
            if (graph?.Nodes == null)
            {
                property.stringValue = requestedName;
                return;
            }

            // Resolve the node this Name field belongs to by object identity rather than
            // by matching oldName. A name match skips empty-named nodes, so renaming a
            // freshly-added node (Name still "") would never find the node and silently
            // drop the rename (reverting to empty with no warning). This matters most for
            // the inline component graph, whose nodes start unnamed.
            TryllNodeDescription renamedNode = ResolveOwningNode(property);

            var existingNames = new List<string>();
            foreach (var n in graph.Nodes)
            {
                if (n == null || ReferenceEquals(n, renamedNode)) continue;
                if (!string.IsNullOrEmpty(n.Name))
                    existingNames.Add(n.Name);
            }

            if (!TryllRenamePropagation.IsValidGrammar(requestedName) || TryllRenamePropagation.IsReserved(requestedName))
            {
                Debug.LogWarning($"[Tryll] Invalid node name '{requestedName}': must match [A-Za-z_][A-Za-z0-9_]* " +
                                  $"and not be the reserved '{TryllRenamePropagation.ReservedUserMessage}'. Reverted to '{oldName}'.");
                property.stringValue = oldName;
                return;
            }

            string newName = TryllRenamePropagation.Uniquify(requestedName, existingNames);
            bool rewriteSlot = renamedNode != null && TryllRenamePropagation.EffectiveSlotIsNodeName(renamedNode);

            var undoHost = property.serializedObject.targetObject;
            Undo.RecordObject(undoHost, "Rename Tryll Node");

            if (renamedNode != null)
                renamedNode.Name = newName;
            else
                property.stringValue = newName;
            TryllRenamePropagation.PropagateNodeRename(graph, oldName, newName);
            if (rewriteSlot)
                TryllRenamePropagation.PropagateOwnedSlotRename(graph, renamedNode, oldName, newName);

            EditorUtility.SetDirty(undoHost);
            property.serializedObject.Update();
        }

        /// <summary>
        /// Resolves the <see cref="TryllNodeDescription"/> that owns this <c>Name</c>
        /// property (its parent element), independent of the current name value. Returns
        /// <c>null</c> if the parent cannot be resolved to a node.
        /// </summary>
        private static TryllNodeDescription ResolveOwningNode(SerializedProperty nameProp)
        {
            string path = nameProp.propertyPath;
            int lastDot = path.LastIndexOf('.');
            if (lastDot <= 0) return null;

            var nodeProp = nameProp.serializedObject.FindProperty(path.Substring(0, lastDot));
            if (nodeProp == null) return null;

            return TryllGraphIntrospection.GetTargetObjectOfProperty(nodeProp) as TryllNodeDescription;
        }
    }
}
