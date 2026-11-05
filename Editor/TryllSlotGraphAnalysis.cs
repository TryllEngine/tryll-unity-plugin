// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Availability classification for one producer slot relative to a target node — drives
    /// the verbose <c>input</c> dropdown: every producer slot in the graph is listed,
    /// available ones enabled and unavailable ones disabled-with-a-reason, rather than
    /// silently omitted.
    /// </summary>
    internal enum TryllSlotAvailability
    {
        /// <summary>Every path from StartNode to the target writes this slot first.</summary>
        Available,
        /// <summary>Some, not all, paths from StartNode to the target write this slot first (may-available).</summary>
        BranchDependent,
        /// <summary>The producing node does not run before the target node (not an ancestor).</summary>
        NotUpstream,
        /// <summary>The producing node is an ancestor, but Graph.StartNode is empty.</summary>
        StartUnset,
        /// <summary>The producing node is an ancestor, but is not reachable from the (non-empty) StartNode.</summary>
        StartUnreachable,
        /// <summary>The slot is the target node's own output slot.</summary>
        Self,
        /// <summary>The current field value does not match any producer slot in the graph.</summary>
        Missing,
    }

    /// <summary>One dropdown entry: a candidate slot value plus its availability and human-readable reason.</summary>
    internal readonly struct TryllSlotOption
    {
        public readonly string Value;
        public readonly TryllSlotAvailability Availability;

        public TryllSlotOption(string value, TryllSlotAvailability availability)
        {
            Value = value;
            Availability = availability;
        }

        public bool IsEnabled =>
            Availability == TryllSlotAvailability.Available ||
            Availability == TryllSlotAvailability.BranchDependent;

        /// <summary>Short note appended inline to the entry label, e.g. "(branch-dependent)". Empty when none.</summary>
        public string ShortReason()
        {
            switch (Availability)
            {
                case TryllSlotAvailability.Available: return string.Empty;
                case TryllSlotAvailability.BranchDependent: return "(branch-dependent)";
                case TryllSlotAvailability.NotUpstream: return "(not upstream)";
                case TryllSlotAvailability.StartUnset: return "(Start Node not set)";
                case TryllSlotAvailability.StartUnreachable: return "(unreachable from Start)";
                case TryllSlotAvailability.Self: return "(this node)";
                case TryllSlotAvailability.Missing: return "(not in graph)";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Longer explanation for the reason. Unity's <c>GenericMenu</c> has no per-item
        /// tooltip support, so this is currently unused by the drawer but kept for parity
        /// with the Unreal customization and for potential future surfaces (e.g. a status bar).
        /// </summary>
        public string LongReason(string targetNodeName)
        {
            switch (Availability)
            {
                case TryllSlotAvailability.Available:
                    return $"Always available: every path to '{targetNodeName}' writes '{Value}' first.";
                case TryllSlotAvailability.BranchDependent:
                    return $"May be empty: '{Value}' is only written on some paths to '{targetNodeName}'.";
                case TryllSlotAvailability.NotUpstream:
                    return $"Unavailable: the producer of '{Value}' does not run before '{targetNodeName}'.";
                case TryllSlotAvailability.StartUnset:
                    return "Set Graph > Start Node so upstream slots resolve.";
                case TryllSlotAvailability.StartUnreachable:
                    return $"Unavailable: the graph's Start Node can't reach the producer of '{Value}'.";
                case TryllSlotAvailability.Self:
                    return "A node can't read its own output slot.";
                case TryllSlotAvailability.Missing:
                    return $"'{Value}' does not match any producer slot in this graph (stale after a rename/rewire, or authored out-of-band).";
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// Client-side, may-side-only slot reachability over a <see cref="TryllGraphDescription"/>
    /// (the editors need only the "may" side — "producers reachable-before this node on some
    /// path" — not the server's full must/may fixed-point).
    ///
    /// Used by <see cref="TryllSlotInputDrawer"/> (the <c>input</c> dropdown) and by
    /// <see cref="TryllWorkflowValidator"/> (impossible-read backstop checks).
    /// </summary>
    internal static class TryllSlotGraphAnalysis
    {
        /// <summary>
        /// The effective slot name a node writes ("output_name" override,
        /// or the node's own name when empty), or <c>null</c> when the node produces no slot.
        /// Most producers declare <c>[TryllSlotName]</c> on <c>output_name</c>; ToolCall is
        /// the exception — only CallOrAnswer writes the node's own name (when the model
        /// answers instead of calling a tool), mirroring <c>ToolCallNode::GetSlotMetadata</c>.
        /// </summary>
        public static string GetEffectiveWriteSlot(TryllNodeDescription node)
        {
            if (node?.Params == null)
                return null;

            if (node.Params is TryllToolCallParams toolCall)
                return toolCall.Mode == TryllToolCallMode.CallOrAnswer ? node.Name : null;

            var field = TryllNodeTypeRegistry.GetSlotNameField(node.Params.GetType());
            if (field == null)
                return null;

            string overrideName = TryllNodeTypeRegistry.ReadSlotName(node.Params, field);
            return string.IsNullOrEmpty(overrideName) ? node.Name : overrideName;
        }

        /// <summary>
        /// Every slot this node writes. ClassifyAttributesLLM writes one slot per
        /// attribute, <c>&lt;output_name&gt;_&lt;name&gt;</c>. Every other producer
        /// writes the single slot from <see cref="GetEffectiveWriteSlot"/>.
        /// </summary>
        public static IEnumerable<string> GetEffectiveWriteSlots(TryllNodeDescription node)
        {
            if (node?.Params is TryllClassifyAttributesLLMParams attributes)
            {
                string prefix = string.IsNullOrEmpty(attributes.OutputName) ? node.Name : attributes.OutputName;
                if (attributes.Attributes == null)
                    yield break;
                foreach (var attribute in attributes.Attributes)
                {
                    if (attribute == null || string.IsNullOrEmpty(attribute.Name))
                        continue;
                    yield return prefix + "_" + attribute.Name;
                }
                yield break;
            }

            string single = GetEffectiveWriteSlot(node);
            if (!string.IsNullOrEmpty(single))
                yield return single;
        }

        private static Dictionary<string, List<string>> BuildForwardAdjacency(
            TryllGraphDescription graph, out HashSet<string> allNodeNames)
        {
            var adjacency = new Dictionary<string, List<string>>();
            allNodeNames = new HashSet<string>();

            if (graph?.Nodes == null)
                return adjacency;

            foreach (var node in graph.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Name))
                    continue;
                allNodeNames.Add(node.Name);
                if (!adjacency.ContainsKey(node.Name))
                    adjacency[node.Name] = new List<string>();
            }

            foreach (var node in graph.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Name) || node.Params == null)
                    continue;

                foreach (var exit in TryllNodeTypeRegistry.GetExitFields(node.Params.GetType()))
                {
                    string target = TryllNodeTypeRegistry.ReadExit(node.Params, exit.Field);
                    if (!string.IsNullOrEmpty(target) && allNodeNames.Contains(target))
                        adjacency[node.Name].Add(target);
                }
            }

            return adjacency;
        }

        private static HashSet<string> ReachableFrom(string start, Dictionary<string, List<string>> adjacency)
        {
            var visited = new HashSet<string>();
            if (string.IsNullOrEmpty(start) || !adjacency.ContainsKey(start))
                return visited;

            var queue = new Queue<string>();
            queue.Enqueue(start);
            visited.Add(start);
            while (queue.Count > 0)
            {
                string cur = queue.Dequeue();
                foreach (string next in adjacency[cur])
                    if (visited.Add(next))
                        queue.Enqueue(next);
            }
            return visited;
        }

        private static HashSet<string> AncestorsOf(string target, Dictionary<string, List<string>> adjacency)
        {
            var reverse = new Dictionary<string, List<string>>();
            foreach (var kv in adjacency)
            {
                foreach (string to in kv.Value)
                {
                    if (!reverse.TryGetValue(to, out var list))
                        reverse[to] = list = new List<string>();
                    list.Add(kv.Key);
                }
            }

            var visited = new HashSet<string>();
            if (string.IsNullOrEmpty(target) || !reverse.TryGetValue(target, out var preds))
                return visited;

            var queue = new Queue<string>();
            foreach (string p in preds)
                if (visited.Add(p))
                    queue.Enqueue(p);

            while (queue.Count > 0)
            {
                string cur = queue.Dequeue();
                if (reverse.TryGetValue(cur, out var ps))
                    foreach (string p in ps)
                        if (visited.Add(p))
                            queue.Enqueue(p);
            }
            return visited;
        }

        /// <summary>
        /// Producers reachable-before <paramref name="targetNodeName"/> on some path from
        /// the graph's <c>StartNode</c> (may-side reachability) — the effective slot name of
        /// every such producer, plus the implicit <c>"user_message"</c> turn-input slot.
        /// De-duplicated; <c>"user_message"</c> first, then authoring order.
        ///
        /// Equivalent to filtering <see cref="ClassifySlots"/> down to enabled entries; kept
        /// as a separate entry point for callers (the validator backstop) that only need the
        /// allow-list, not the full classification.
        /// </summary>
        public static List<string> GetUpstreamSlots(TryllGraphDescription graph, string targetNodeName)
        {
            var result = new List<string> { "user_message" };
            foreach (var option in ClassifySlots(graph, targetNodeName))
                if (option.IsEnabled)
                    result.Add(option.Value);
            return result;
        }

        /// <summary>Lower is more available; used to keep the best classification when de-duplicating by slot name.</summary>
        private static int AvailabilityRank(TryllSlotAvailability a)
        {
            switch (a)
            {
                case TryllSlotAvailability.Available: return 0;
                case TryllSlotAvailability.BranchDependent: return 1;
                case TryllSlotAvailability.StartUnreachable: return 2;
                case TryllSlotAvailability.StartUnset: return 3;
                case TryllSlotAvailability.NotUpstream: return 4;
                case TryllSlotAvailability.Self: return 5;
                case TryllSlotAvailability.Missing: return 6;
                default: return 7;
            }
        }

        /// <summary>
        /// Classifies every distinct producer slot in the graph relative to
        /// <paramref name="targetNodeName"/> (the verbose <c>input</c> dropdown) — one entry
        /// per effective slot name, de-duplicated by keeping the most-available classification when
        /// multiple nodes write the same slot. Does not include the implicit
        /// <c>"user_message"</c> default entry (callers add that themselves, since its label
        /// is context-dependent).
        ///
        /// When <paramref name="currentValue"/> is non-empty and does not match any producer
        /// slot in the graph, a trailing <see cref="TryllSlotAvailability.Missing"/> entry for
        /// it is appended so it can still be shown (disabled, tinted) in the dropdown.
        /// </summary>
        public static List<TryllSlotOption> ClassifySlots(
            TryllGraphDescription graph, string targetNodeName, string currentValue = null)
        {
            var result = new List<TryllSlotOption>();
            if (graph?.Nodes == null)
                return result;

            var adjacency = BuildForwardAdjacency(graph, out _);
            bool startSet = !string.IsNullOrEmpty(graph.StartNode);
            var reachableFromStart = ReachableFrom(graph.StartNode, adjacency);
            var ancestors = AncestorsOf(targetNodeName, adjacency);

            var bySlot = new Dictionary<string, TryllSlotAvailability>();
            var order = new List<string>();

            foreach (var node in graph.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Name))
                    continue;
                foreach (string slot in GetEffectiveWriteSlots(node))
                {
                if (string.IsNullOrEmpty(slot))
                    continue;

                TryllSlotAvailability availability;
                if (node.Name == targetNodeName)
                    availability = TryllSlotAvailability.Self;
                else if (!ancestors.Contains(node.Name))
                    availability = TryllSlotAvailability.NotUpstream;
                else if (!startSet)
                    availability = TryllSlotAvailability.StartUnset;
                else if (!reachableFromStart.Contains(node.Name))
                    availability = TryllSlotAvailability.StartUnreachable;
                else if (IsMustAvailable(graph, targetNodeName, slot))
                    availability = TryllSlotAvailability.Available;
                else
                    availability = TryllSlotAvailability.BranchDependent;

                if (bySlot.TryGetValue(slot, out var existing))
                {
                    if (AvailabilityRank(availability) < AvailabilityRank(existing))
                        bySlot[slot] = availability;
                }
                else
                {
                    bySlot[slot] = availability;
                    order.Add(slot);
                }
                }
            }

            foreach (var slot in order)
                result.Add(new TryllSlotOption(slot, bySlot[slot]));

            if (!string.IsNullOrEmpty(currentValue) && currentValue != "user_message" && !bySlot.ContainsKey(currentValue))
                result.Add(new TryllSlotOption(currentValue, TryllSlotAvailability.Missing));

            return result;
        }

        /// <summary>
        /// True when <paramref name="slotName"/> is guaranteed to have been written before
        /// <paramref name="targetNodeName"/> executes on <em>every</em> path from
        /// <c>StartNode</c> — i.e. every such path visits at least one producer of
        /// <paramref name="slotName"/>. Implemented as a cut check: remove every producer
        /// node of <paramref name="slotName"/> from the graph (as a vertex, blocking any
        /// path through it) and see whether <paramref name="targetNodeName"/> is still
        /// reachable from <c>StartNode</c> — if so, some path avoids every producer, so the
        /// slot is only "may"-available (branch-dependent), not "must"-available.
        /// <c>"user_message"</c> is always must-available (turn input, not a node output).
        /// </summary>
        public static bool IsMustAvailable(TryllGraphDescription graph, string targetNodeName, string slotName)
        {
            if (slotName == "user_message") return true;
            if (graph?.Nodes == null || string.IsNullOrEmpty(slotName)) return false;

            var adjacency = BuildForwardAdjacency(graph, out _);

            // Conditional producers are excluded from the cut set: removing one as a vertex
            // would wrongly assume every path through it writes the slot, when it may not —
            // such a producer can only ever contribute to may-availability, never must.
            var producers = new HashSet<string>();
            foreach (var node in graph.Nodes)
                if (node != null && GetEffectiveWriteSlots(node).Contains(slotName)
                    && !TryllNodeTypeRegistry.IsWriteConditional(node))
                    producers.Add(node.Name);
            if (producers.Count == 0) return false;

            var visited = new HashSet<string>();
            if (string.IsNullOrEmpty(graph.StartNode) || !adjacency.ContainsKey(graph.StartNode))
                return false;

            var queue = new Queue<string>();
            queue.Enqueue(graph.StartNode);
            visited.Add(graph.StartNode);
            while (queue.Count > 0)
            {
                string cur = queue.Dequeue();
                if (producers.Contains(cur))
                    continue; // do not traverse past a producer while "avoiding" it
                foreach (string next in adjacency[cur])
                    if (visited.Add(next))
                        queue.Enqueue(next);
            }

            // If target is reachable while avoiding every producer, some path never
            // produces the slot — not must-available.
            return !visited.Contains(targetNodeName);
        }
    }
}
