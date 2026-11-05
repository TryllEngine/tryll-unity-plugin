// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Shared helpers for Inspector property drawers that need to look up the enclosing
    /// <see cref="TryllGraphDescription"/> and enumerate its node names.
    ///
    /// Used by <see cref="TryllExitDrawer"/> (exit fields on node params) and
    /// <see cref="TryllNodeNameDrawer"/> (the <c>StartNode</c> field on the graph itself).
    /// </summary>
    internal static class TryllGraphIntrospection
    {
        /// <summary>
        /// Walks <paramref name="anyProp"/>'s property path to locate the nearest enclosing
        /// <c>TryllGraphDescription</c> <see cref="SerializedProperty"/>.
        ///
        /// Two layouts are understood:
        /// <list type="bullet">
        ///   <item>Exit field inside a node's <c>Params</c>:
        ///     <c>Graph.Nodes.Array.data[2].Params.DefaultExit</c> — the node element is found
        ///     first, then its <c>Name</c> is returned as <paramref name="ownerNodeName"/>
        ///     so callers can exclude the owning node from the target list.</item>
        ///   <item>Field directly on the graph struct (e.g. <c>StartNode</c>):
        ///     <c>Graph.StartNode</c> — the trailing segment is stripped; <paramref name="ownerNodeName"/>
        ///     is set to <see cref="string.Empty"/>.</item>
        /// </list>
        /// </summary>
        /// <returns><c>true</c> when a <c>TryllGraphDescription</c> was found.</returns>
        public static bool TryFindEnclosingGraph(
            SerializedProperty anyProp,
            out SerializedProperty graphProp,
            out string ownerNodeName)
        {
            graphProp     = null;
            ownerNodeName = string.Empty;

            string path = anyProp.propertyPath;
            var    so   = anyProp.serializedObject;

            // ── Case A: field inside a node's Params (the exit-field case) ───────
            // Path: Graph.Nodes.Array.data[2].Params.DefaultExit
            const string arrayMarker  = ".Array.data[";
            const string nodesSegment = "Nodes";

            int lastArrayIdx = path.LastIndexOf(arrayMarker, StringComparison.Ordinal);
            if (lastArrayIdx >= 0)
            {
                int closeBracket = path.IndexOf(']', lastArrayIdx);
                if (closeBracket >= 0)
                {
                    string nodeElementPath = path.Substring(0, closeBracket + 1);
                    int nodesIdx = nodeElementPath.LastIndexOf(
                        "." + nodesSegment + arrayMarker, StringComparison.Ordinal);
                    if (nodesIdx >= 0)
                    {
                        string graphPath = nodeElementPath.Substring(0, nodesIdx);

                        var nodeElemProp = so.FindProperty(nodeElementPath);
                        if (nodeElemProp != null)
                        {
                            var nameProp = nodeElemProp.FindPropertyRelative("Name");
                            if (nameProp != null)
                                ownerNodeName = nameProp.stringValue ?? string.Empty;
                        }

                        graphProp = string.IsNullOrEmpty(graphPath)
                            ? null
                            : so.FindProperty(graphPath);

                        if (graphProp != null)
                            return true;
                    }
                }
            }

            // ── Case B: field directly on the graph struct (e.g. StartNode) ──────
            // Path: Graph.StartNode
            int lastDot = path.LastIndexOf('.');
            if (lastDot > 0)
            {
                string parentPath = path.Substring(0, lastDot);
                var    parentProp = so.FindProperty(parentPath);
                if (parentProp != null && parentProp.FindPropertyRelative("Nodes") != null)
                {
                    graphProp = parentProp;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all non-empty node names from <paramref name="graphProp"/>'s
        /// <c>Nodes</c> array, in authoring order.
        /// </summary>
        public static List<string> GetNodeNames(SerializedProperty graphProp)
        {
            var names     = new List<string>();
            var nodesProp = graphProp.FindPropertyRelative("Nodes");
            if (nodesProp == null || !nodesProp.isArray)
                return names;

            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var elem     = nodesProp.GetArrayElementAtIndex(i);
                var nameProp = elem.FindPropertyRelative("Name");
                if (nameProp == null) continue;
                string nodeName = nameProp.stringValue ?? string.Empty;
                if (!string.IsNullOrEmpty(nodeName))
                    names.Add(nodeName);
            }
            return names;
        }

        /// <summary>
        /// Resolves the actual managed object a <see cref="SerializedProperty"/> refers to
        /// by walking its <see cref="SerializedProperty.propertyPath"/> with reflection.
        /// Used to hand the runtime <c>TryllGraphDescription</c> model (rather than a
        /// property-path view of it) to <see cref="TryllSlotGraphAnalysis"/> and the rename
        /// propagation routines, which operate on plain C# objects.
        /// </summary>
        public static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;

            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split('.');

            foreach (string element in elements)
            {
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf('['));
                    string indexPart = element.Substring(element.IndexOf('['))
                        .Replace("[", string.Empty).Replace("]", string.Empty);
                    int index = int.Parse(indexPart);
                    obj = GetIndexedValue(GetFieldOrPropertyValue(obj, elementName), index);
                }
                else
                {
                    obj = GetFieldOrPropertyValue(obj, element);
                }

                if (obj == null) return null;
            }
            return obj;
        }

        private static object GetFieldOrPropertyValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            while (type != null)
            {
                var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null) return field.GetValue(source);

                var property = type.GetProperty(name,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null) return property.GetValue(source, null);

                type = type.BaseType;
            }
            return null;
        }

        private static object GetIndexedValue(object source, int index)
        {
            if (!(source is System.Collections.IEnumerable enumerable)) return null;
            var enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
                if (!enumerator.MoveNext())
                    return null;
            return enumerator.Current;
        }
    }
}
