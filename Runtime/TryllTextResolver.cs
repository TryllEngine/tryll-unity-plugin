// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Result of filling ticked Text IDs on a graph clone.
    /// </summary>
    public readonly struct TryllTextResolveResult
    {
        public TryllError Error { get; }
        public bool AnyTicked { get; }

        /// <summary>
        /// Nodes where a ticked, non-structural field got different text.
        /// A node whose text did not change is not listed, so a refresh sends
        /// nothing for it.
        /// </summary>
        public IReadOnlyList<(string NodeName, TryllNodeParamsBase Params)> LiveNodes { get; }

        public TryllTextResolveResult(
            TryllError error,
            bool anyTicked,
            IReadOnlyList<(string, TryllNodeParamsBase)> liveNodes)
        {
            Error = error;
            AnyTicked = anyTicked;
            LiveNodes = liveNodes ?? Array.Empty<(string, TryllNodeParamsBase)>();
        }

        public static TryllTextResolveResult Ok(bool anyTicked, List<(string, TryllNodeParamsBase)> liveNodes)
            => new TryllTextResolveResult(TryllError.Ok(), anyTicked, liveNodes);
    }

    /// <summary>
    /// Walks a graph clone and replaces ticked localizable strings with provider text.
    /// Does not pack, and does not mutate the authored asset — callers pass a clone.
    /// </summary>
    public static class TryllTextResolver
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public;

        public struct TextIdRef
        {
            public string NodeName;
            public string FieldPath;
            public string Id;
        }

        /// <summary>Every ticked Text ID, including empty ones, for the Check texts window.</summary>
        public static List<TextIdRef> CollectTextIds(TryllGraphDescription graph)
        {
            var found = new List<TextIdRef>();
            if (graph?.Nodes == null) return found;
            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null) continue;
                Collect(node.Params, "", node.Name ?? "", found);
            }
            return found;
        }

        static void Collect(object obj, string path, string node, List<TextIdRef> found)
        {
            if (obj == null || obj is string || obj is UnityEngine.Object) return;
            if (obj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    Collect(list[i], path + "[" + i + "]", node, found);
                return;
            }
            var type = obj.GetType();
            if (type.IsPrimitive || type.IsEnum) return;
            foreach (var field in type.GetFields(Fields))
            {
                if (field.GetCustomAttribute<TryllLocalizableAttribute>() != null
                    && field.FieldType == typeof(string))
                {
                    var flag = type.GetField(field.Name + "Localized", Fields);
                    var idField = type.GetField(field.Name + "TextId", Fields);
                    if (flag != null && flag.FieldType == typeof(bool) && (bool)flag.GetValue(obj))
                    {
                        string fieldPath = string.IsNullOrEmpty(path) ? field.Name : path + "." + field.Name;
                        found.Add(new TextIdRef
                        {
                            NodeName = node,
                            FieldPath = fieldPath,
                            Id = idField?.GetValue(obj) as string ?? "",
                        });
                    }
                    continue;
                }
                var value = field.GetValue(obj);
                if (value == null) continue;
                var ft = value.GetType();
                if (ft == typeof(string) || ft.IsPrimitive || ft.IsEnum || value is UnityEngine.Object)
                    continue;
                string child = string.IsNullOrEmpty(path) ? field.Name : path + "." + field.Name;
                Collect(value, child, node, found);
            }
        }

        /// <summary>True when <paramref name="parms"/> has a ticked localizable field.</summary>
        public static bool HasTickedFields(TryllNodeParamsBase parms)
            => parms != null && HasTicked(parms);

        /// <summary>
        /// An inline value was written to <paramref name="fieldPath"/> on purpose
        /// (for example a Dialog Lab override), so it must win over the Text ID:
        /// untick that field. Paths look like <c>SystemPrompt</c> or
        /// <c>Intents[0].Prompt</c>. Returns true when a ticked field was unticked.
        /// </summary>
        public static bool UntickAt(object root, string fieldPath)
        {
            if (root == null || string.IsNullOrEmpty(fieldPath)) return false;
            var segments = fieldPath.Split('.');
            object current = root;
            for (int s = 0; s < segments.Length - 1 && current != null; s++)
                current = Step(current, segments[s]);
            if (current == null) return false;

            string leaf = segments[segments.Length - 1];
            if (leaf.IndexOf('[') >= 0) return false;
            var type = current.GetType();
            var field = type.GetField(leaf, Fields);
            if (field == null || field.FieldType != typeof(string)
                || field.GetCustomAttribute<TryllLocalizableAttribute>() == null)
                return false;
            var flag = type.GetField(leaf + "Localized", Fields);
            if (flag == null || flag.FieldType != typeof(bool) || !(bool)flag.GetValue(current))
                return false;
            flag.SetValue(current, false);
            return true;
        }

        static object Step(object obj, string segment)
        {
            string name = segment;
            int index = -1;
            int open = segment.IndexOf('[');
            if (open >= 0)
            {
                int close = segment.IndexOf(']', open);
                if (close < 0 || !int.TryParse(segment.Substring(open + 1, close - open - 1), out index))
                    return null;
                name = segment.Substring(0, open);
            }
            var value = obj.GetType().GetField(name, Fields)?.GetValue(obj);
            if (index < 0) return value;
            return value is IList list && index < list.Count ? list[index] : null;
        }

        public static bool HasTickedFields(TryllGraphDescription graph)
        {
            if (graph?.Nodes == null) return false;
            foreach (var node in graph.Nodes)
            {
                if (node?.Params != null && HasTicked(node.Params))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Resolve ticked fields on <paramref name="graph"/> in place.
        /// <paramref name="liveOnly"/> skips fields that sit in a structural
        /// container (they were fixed when the agent was created).
        /// </summary>
        public static async Task<TryllTextResolveResult> ResolveAsync(
            TryllGraphDescription graph,
            ITryllTextProvider provider,
            string assetName,
            bool liveOnly)
        {
            var live = new List<(string, TryllNodeParamsBase)>();
            if (graph?.Nodes == null)
                return TryllTextResolveResult.Ok(false, live);

            string asset = string.IsNullOrEmpty(assetName) ? "workflow" : assetName;
            string table = graph.TextTable ?? "";
            bool any = false;

            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null) continue;
                bool wroteLive = false;
                var error = await WalkAsync(
                    node.Params, structural: false, path: "",
                    asset, node.Name ?? "", table, provider, liveOnly,
                    ticked: () => any = true,
                    wrote: () => wroteLive = true);
                if (!error.IsOk) return new TryllTextResolveResult(error, any, live);
                if (wroteLive) live.Add((node.Name, node.Params));
            }

            return TryllTextResolveResult.Ok(any, live);
        }

        static bool HasTicked(object obj)
        {
            if (obj == null || obj is string || obj is UnityEngine.Object) return false;
            if (obj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    if (HasTicked(list[i])) return true;
                return false;
            }
            var type = obj.GetType();
            if (type.IsPrimitive || type.IsEnum) return false;

            foreach (var field in type.GetFields(Fields))
            {
                if (field.GetCustomAttribute<TryllLocalizableAttribute>() != null
                    && field.FieldType == typeof(string))
                {
                    var flag = type.GetField(field.Name + "Localized", Fields);
                    if (flag != null && flag.FieldType == typeof(bool) && (bool)flag.GetValue(obj))
                        return true;
                    continue;
                }
                var value = field.GetValue(obj);
                if (value == null) continue;
                var ft = value.GetType();
                if (ft == typeof(string) || ft.IsPrimitive || ft.IsEnum) continue;
                if (HasTicked(value)) return true;
            }
            return false;
        }

        static async Task<TryllError> WalkAsync(
            object obj,
            bool structural,
            string path,
            string asset,
            string node,
            string table,
            ITryllTextProvider provider,
            bool liveOnly,
            Action ticked,
            Action wrote)
        {
            if (obj == null || obj is string || obj is UnityEngine.Object)
                return TryllError.Ok();

            if (obj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var error = await WalkAsync(
                        list[i], structural, path + "[" + i + "]",
                        asset, node, table, provider, liveOnly, ticked, wrote);
                    if (!error.IsOk) return error;
                }
                return TryllError.Ok();
            }

            var type = obj.GetType();
            if (type.IsPrimitive || type.IsEnum) return TryllError.Ok();

            foreach (var field in type.GetFields(Fields))
            {
                bool nextStructural = structural || field.GetCustomAttribute<TryllStructuralAttribute>() != null;
                if (field.GetCustomAttribute<TryllLocalizableAttribute>() != null
                    && field.FieldType == typeof(string))
                {
                    var error = await ResolveFieldAsync(
                        obj, type, field, nextStructural, path,
                        asset, node, table, provider, liveOnly, ticked, wrote);
                    if (!error.IsOk) return error;
                    continue;
                }

                var value = field.GetValue(obj);
                if (value == null) continue;
                var ft = value.GetType();
                if (ft == typeof(string) || ft.IsPrimitive || ft.IsEnum || value is UnityEngine.Object)
                    continue;
                string child = string.IsNullOrEmpty(path) ? field.Name : path + "." + field.Name;
                var nested = await WalkAsync(
                    value, nextStructural, child,
                    asset, node, table, provider, liveOnly, ticked, wrote);
                if (!nested.IsOk) return nested;
            }
            return TryllError.Ok();
        }

        static async Task<TryllError> ResolveFieldAsync(
            object obj,
            Type type,
            FieldInfo field,
            bool structural,
            string path,
            string asset,
            string node,
            string table,
            ITryllTextProvider provider,
            bool liveOnly,
            Action ticked,
            Action wrote)
        {
            var flag = type.GetField(field.Name + "Localized", Fields);
            var idField = type.GetField(field.Name + "TextId", Fields);
            if (flag == null || flag.FieldType != typeof(bool) || idField == null || idField.FieldType != typeof(string))
                return TryllError.Ok();
            if (!(bool)flag.GetValue(obj))
                return TryllError.Ok();

            ticked();
            if (liveOnly && structural)
                return TryllError.Ok();

            string fieldPath = string.IsNullOrEmpty(path) ? field.Name : path + "." + field.Name;
            string id = idField.GetValue(obj) as string ?? "";
            if (string.IsNullOrEmpty(id))
            {
                return Fail(asset, node, fieldPath, table, id, "Text ID is empty");
            }
            if (provider == null)
            {
                return Fail(asset, node, fieldPath, table, id, "no text provider is set");
            }

            string text;
            try
            {
                text = await provider.GetTextAsync(table, id);
            }
            catch (Exception ex)
            {
                return Fail(asset, node, fieldPath, table, id,
                    $"the text provider failed: {ex.GetBaseException().Message}");
            }
            if (string.IsNullOrEmpty(text))
            {
                return Fail(asset, node, fieldPath, table, id, "not found");
            }

            string previous = field.GetValue(obj) as string;
            field.SetValue(obj, text);
            if (!structural && !string.Equals(previous, text, StringComparison.Ordinal))
                wrote();
            return TryllError.Ok();
        }

        static TryllError Fail(string asset, string node, string field, string table, string id, string problem)
        {
            return new TryllError(
                TryllErrorCode.InvalidParamValue,
                $"Localized text: asset '{asset}', node '{node}', field '{field}', table '{table}', id '{id}': {problem}");
        }
    }
}
