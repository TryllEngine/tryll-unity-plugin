// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Shared grammar/uniquify rules and reference-rewriting for the rename-commit widgets
    /// (full scope: node <c>Name</c> and <c>output_name</c> both get commit-point widgets
    /// that propagate).
    ///
    /// Callers (the property drawers) own the actual commit point: read the old value,
    /// validate/uniquify the new one, write it back, then call the propagation helpers
    /// below <em>before</em> the underlying serialized field is written for node-name
    /// renames (exit/StartNode rewrites match by old-name string, independent of write
    /// order) — see <see cref="TryllNodeRenameDrawer"/> and <see cref="TryllSlotNameDrawer"/>.
    /// </summary>
    internal static class TryllRenamePropagation
    {
        public const string ReservedUserMessage = "user_message";

        private static readonly Regex s_grammar = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public static bool IsValidGrammar(string name) => !string.IsNullOrEmpty(name) && s_grammar.IsMatch(name);

        public static bool IsReserved(string name) => name == ReservedUserMessage;

        /// <summary>
        /// Returns <paramref name="baseName"/> unchanged if not already present in
        /// <paramref name="existingNames"/>; otherwise appends the smallest integer suffix
        /// (starting at 2) that makes it unique.
        /// </summary>
        public static string Uniquify(string baseName, IEnumerable<string> existingNames)
        {
            if (string.IsNullOrEmpty(baseName)) return baseName;

            var existing = new HashSet<string>(existingNames);
            if (!existing.Contains(baseName)) return baseName;

            int i = 2;
            string candidate;
            do
            {
                candidate = baseName + i;
                i++;
            } while (existing.Contains(candidate));
            return candidate;
        }

        /// <summary>
        /// True when <paramref name="node"/>'s effective output slot equals its own name —
        /// i.e. its <c>[TryllSlotName]</c>-tagged override field (<c>output_name</c>) is
        /// empty (the default: no <c>output_name</c> override, so the slot name falls back
        /// to the node's own name). Only such nodes need <see cref="PropagateSlotRename"/>
        /// when their node <c>Name</c> is renamed.
        /// </summary>
        public static bool EffectiveSlotIsNodeName(TryllNodeDescription node)
        {
            if (node?.Params == null) return false;
            var field = TryllNodeTypeRegistry.GetSlotNameField(node.Params.GetType());
            if (field == null) return false;
            return string.IsNullOrEmpty(TryllNodeTypeRegistry.ReadSlotName(node.Params, field));
        }

        /// <summary>
        /// Rewrites every exit field and <c>StartNode</c> equal to <paramref name="oldName"/>
        /// to <paramref name="newName"/>. Does not touch <c>input</c>/template slot
        /// references — call <see cref="PropagateSlotRename"/> too when
        /// <see cref="EffectiveSlotIsNodeName"/> was true for the renamed node (checked
        /// against its state *before* the rename).
        /// </summary>
        public static void PropagateNodeRename(TryllGraphDescription graph, string oldName, string newName)
        {
            if (graph?.Nodes == null || string.IsNullOrEmpty(oldName) || oldName == newName)
                return;

            if (graph.StartNode == oldName)
                graph.StartNode = newName;

            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null) continue;
                foreach (var exit in TryllNodeTypeRegistry.GetExitFields(node.Params.GetType()))
                {
                    if (TryllNodeTypeRegistry.ReadExit(node.Params, exit.Field) == oldName)
                        TryllNodeTypeRegistry.WriteExit(node.Params, exit.Field, newName);
                }
            }
        }

        /// <summary>
        /// Rewrites every <c>input</c>-style selector equal to <paramref name="oldSlot"/>,
        /// every <c>{{slot.&lt;oldSlot&gt;}}</c> template tag, and every
        /// <c>{"slot": "&lt;oldSlot&gt;"}</c> filter-JSON operand (Branch's <c>Condition</c>)
        /// to reference <paramref name="newSlot"/> instead.
        /// </summary>
        /// <summary>
        /// Rewrites downstream references when a node's own output prefix changes.
        /// ClassifyAttributesLLM writes <c>prefix_attribute</c>, not the bare prefix.
        /// </summary>
        public static void PropagateOwnedSlotRename(
            TryllGraphDescription graph, TryllNodeDescription node, string oldPrefix, string newPrefix)
        {
            if (node?.Params is TryllClassifyAttributesLLMParams attributes && attributes.Attributes != null)
            {
                var renames = new Dictionary<string, string>();
                foreach (var attribute in attributes.Attributes)
                {
                    if (attribute == null || string.IsNullOrEmpty(attribute.Name)) continue;
                    string oldSlot = oldPrefix + "_" + attribute.Name;
                    string newSlot = newPrefix + "_" + attribute.Name;
                    if (oldSlot == newSlot) continue;
                    renames[oldSlot] = newSlot;
                }
                ApplySlotRenames(graph, renames);
                return;
            }
            PropagateSlotRename(graph, oldPrefix, newPrefix);
        }

        /// <summary>
        /// Rewrites references to one attribute slot. The prefix is the node's
        /// output name, or its node name when that override is empty.
        /// </summary>
        public static void PropagateAttributeRename(
            TryllGraphDescription graph,
            TryllNodeDescription owner,
            string oldAttributeName,
            string newAttributeName)
        {
            if (owner?.Params is not TryllClassifyAttributesLLMParams attributes)
                return;
            if (string.IsNullOrEmpty(oldAttributeName)
                || string.IsNullOrEmpty(newAttributeName)
                || oldAttributeName == newAttributeName)
                return;
            string prefix = string.IsNullOrEmpty(attributes.OutputName) ? owner.Name : attributes.OutputName;
            if (string.IsNullOrEmpty(prefix))
                return;
            PropagateSlotRename(graph, prefix + "_" + oldAttributeName, prefix + "_" + newAttributeName);
        }

        public static void PropagateSlotRename(TryllGraphDescription graph, string oldSlot, string newSlot)
        {
            if (string.IsNullOrEmpty(oldSlot) || oldSlot == newSlot)
                return;
            ApplySlotRenames(graph, new Dictionary<string, string> { { oldSlot, newSlot } });
        }

        /// <summary>
        /// Applies every pair against the original reference text. A slot written
        /// by one pair is not eligible for another pair in the same call.
        /// </summary>
        static void ApplySlotRenames(TryllGraphDescription graph, IReadOnlyDictionary<string, string> renames)
        {
            if (graph?.Nodes == null || renames == null || renames.Count == 0)
                return;

            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null) continue;
                var paramsType = node.Params.GetType();

                foreach (var inputField in TryllNodeTypeRegistry.GetSlotInputFields(paramsType))
                {
                    string current = TryllNodeTypeRegistry.ReadSlotInput(node.Params, inputField);
                    if (current != null && renames.TryGetValue(current, out string renamed))
                        TryllNodeTypeRegistry.WriteSlotInput(node.Params, inputField, renamed);
                }

                foreach (var templateField in TryllNodeTypeRegistry.GetTemplateFields(paramsType))
                {
                    string tmpl = templateField.GetValue(node.Params) as string;
                    if (string.IsNullOrEmpty(tmpl)) continue;

                    string rewritten = TryllMustacheRefs.Rename(tmpl, renames);
                    if (rewritten != tmpl)
                        templateField.SetValue(node.Params, rewritten);
                }

                foreach (var filterField in TryllNodeTypeRegistry.GetFilterJsonFields(paramsType))
                {
                    string filterJson = filterField.GetValue(node.Params) as string;
                    if (string.IsNullOrEmpty(filterJson)) continue;

                    string rewritten = TryllFilterRefs.RenameSlotRefs(filterJson, renames);
                    if (rewritten != filterJson)
                        filterField.SetValue(node.Params, rewritten);
                }
            }
        }
    }
}
