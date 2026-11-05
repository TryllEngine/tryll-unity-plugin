// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// The single place the workflow graph editor reflects over node param types.
    ///
    /// Everything else (node views, marshalling) reads through here, so swapping this
    /// for a codegen-emitted metadata table later is an internal change with no caller
    /// impact. Results are cached — reflection runs once per editor domain.
    /// </summary>
    internal static class TryllNodeTypeRegistry
    {
        /// <summary>One workflow exit declared on a params class.</summary>
        public readonly struct ExitField
        {
            public readonly FieldInfo Field;
            public readonly string    ExitName;

            public ExitField(FieldInfo field, string exitName)
            {
                Field    = field;
                ExitName = exitName;
            }
        }

        private static IReadOnlyList<Type> s_nodeParamTypes;
        private static readonly Dictionary<Type, IReadOnlyList<ExitField>> s_exitFields = new Dictionary<Type, IReadOnlyList<ExitField>>();
        private static readonly Dictionary<Type, FieldInfo> s_slotNameFields = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<Type, IReadOnlyList<FieldInfo>> s_slotInputFields = new Dictionary<Type, IReadOnlyList<FieldInfo>>();
        private static readonly Dictionary<Type, IReadOnlyList<FieldInfo>> s_templateFields = new Dictionary<Type, IReadOnlyList<FieldInfo>>();
        private static readonly Dictionary<Type, IReadOnlyList<FieldInfo>> s_filterJsonFields = new Dictionary<Type, IReadOnlyList<FieldInfo>>();

        /// <summary>All concrete <see cref="TryllNodeParamsBase"/> subclasses.</summary>
        public static IReadOnlyList<Type> NodeParamTypes
        {
            get
            {
                if (s_nodeParamTypes == null)
                {
                    s_nodeParamTypes = typeof(TryllNodeParamsBase).Assembly
                        .GetTypes()
                        .Where(t => !t.IsAbstract && typeof(TryllNodeParamsBase).IsAssignableFrom(t))
                        .ToArray();
                }
                return s_nodeParamTypes;
            }
        }

        /// <summary>Inspector display name from <c>[AddTypeMenu]</c>, falling back to the type name.</summary>
        public static string DisplayName(Type paramsType)
        {
            if (paramsType == null) return "<null>";
            var menu = Attribute.GetCustomAttribute(paramsType, typeof(AddTypeMenuAttribute)) as AddTypeMenuAttribute;
            return menu?.GetTypeNameWithoutPath() ?? paramsType.Name;
        }

        /// <summary>The <c>[TryllExit]</c>-tagged string fields of a params class, in declaration order.</summary>
        public static IReadOnlyList<ExitField> GetExitFields(Type paramsType)
        {
            if (paramsType == null) return Array.Empty<ExitField>();
            if (s_exitFields.TryGetValue(paramsType, out var cached)) return cached;

            var exits = new List<ExitField>();
            foreach (var field in paramsType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = field.GetCustomAttribute<TryllExitAttribute>();
                if (attr != null)
                    exits.Add(new ExitField(field, attr.ExitName));
            }

            s_exitFields[paramsType] = exits;
            return exits;
        }

        public static string ReadExit(TryllNodeParamsBase prms, FieldInfo field)
            => (field.GetValue(prms) as string) ?? string.Empty;

        public static void WriteExit(TryllNodeParamsBase prms, FieldInfo field, string target)
            => field.SetValue(prms, target ?? string.Empty);

        /// <summary>
        /// The <c>[TryllSlotName]</c>-tagged field of a params class (its effective-output-
        /// slot override, e.g. <c>OutputName</c>), or <c>null</c> if
        /// the node type does not produce a slot. At most one per params type.
        /// </summary>
        public static FieldInfo GetSlotNameField(Type paramsType)
        {
            if (paramsType == null) return null;
            if (s_slotNameFields.TryGetValue(paramsType, out var cached)) return cached;

            FieldInfo found = null;
            foreach (var field in paramsType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<TryllSlotNameAttribute>() != null)
                {
                    found = field;
                    break;
                }
            }
            s_slotNameFields[paramsType] = found;
            return found;
        }

        /// <summary>The <c>[TryllSlotInput]</c>-tagged string fields of a params class.</summary>
        public static IReadOnlyList<FieldInfo> GetSlotInputFields(Type paramsType)
        {
            if (paramsType == null) return Array.Empty<FieldInfo>();
            if (s_slotInputFields.TryGetValue(paramsType, out var cached)) return cached;

            var found = new List<FieldInfo>();
            foreach (var field in paramsType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<TryllSlotInputAttribute>() != null)
                    found.Add(field);
            }
            s_slotInputFields[paramsType] = found;
            return found;
        }

        /// <summary>
        /// True when a node's write is conditional — it does not happen on every execution,
        /// so a downstream reader of its slot must be treated as branch-dependent (may-only),
        /// never guaranteed, even when this is the graph's only producer of that slot. Mirrors
        /// the server's <c>writeIsConditional</c> flag
        /// (<c>INode::GetSlotMetadata</c>/<c>NodeSlotMetadata</c>) for the two node types that
        /// set it: IntentToInstruction (writes only on a storage hit) and ToolCall in
        /// CallOrAnswer mode (writes only when the model answers instead of calling a tool).
        /// Hardcoded per-type (like the server) rather than reflection-driven, since there is
        /// no schema attribute expressing "conditional on this specific enum value".
        /// </summary>
        public static bool IsWriteConditional(TryllNodeDescription node)
        {
            switch (node?.Params)
            {
                case TryllIntentToInstructionParams _:
                    return true;
                case TryllToolCallParams toolCall:
                    return toolCall.Mode == TryllToolCallMode.CallOrAnswer;
                default:
                    return false;
            }
        }

        public static string ReadSlotName(TryllNodeParamsBase prms, FieldInfo field)
            => (field.GetValue(prms) as string) ?? string.Empty;

        public static void WriteSlotName(TryllNodeParamsBase prms, FieldInfo field, string value)
            => field.SetValue(prms, value ?? string.Empty);

        public static string ReadSlotInput(TryllNodeParamsBase prms, FieldInfo field)
            => (field.GetValue(prms) as string) ?? string.Empty;

        public static void WriteSlotInput(TryllNodeParamsBase prms, FieldInfo field, string value)
            => field.SetValue(prms, value ?? string.Empty);

        /// <summary>
        /// Fields whose text is Mustache-scanned for <c>{{slot.&lt;name&gt;}}</c> /
        /// <c>{{user_message}}</c> references and therefore need rename propagation.
        /// By schema convention this is exactly the field named
        /// <c>Template</c> (Generate/GenerateAndSpeak/Transform) — the server only ever
        /// scans each node type's <c>template</c> field (see
        /// <c>GenerateNode::GetSlotMetadata</c> and friends), never <c>system_prompt</c> or
        /// other multiline text.
        /// </summary>
        public static IReadOnlyList<FieldInfo> GetTemplateFields(Type paramsType)
        {
            if (paramsType == null) return Array.Empty<FieldInfo>();
            if (s_templateFields.TryGetValue(paramsType, out var cached)) return cached;

            var found = new List<FieldInfo>();
            var field = paramsType.GetField("Template", BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(string))
                found.Add(field);

            s_templateFields[paramsType] = found;
            return found;
        }

        /// <summary>
        /// Fields holding a JSON filter/condition expression, scanned by <see cref="TryllFilterRefs"/>
        /// for <c>{"var": …}</c> (declared agent Variables) and <c>{"slot": …}</c> (turn-local
        /// slots) operand references. By schema convention this is exactly the field named
        /// <c>Filter</c> (Retrieve/ClassifyIntent) or <c>Condition</c> (Branch's Expression test) —
        /// the server only ever compiles those two fields as filter JSON.
        /// </summary>
        public static IReadOnlyList<FieldInfo> GetFilterJsonFields(Type paramsType)
        {
            if (paramsType == null) return Array.Empty<FieldInfo>();
            if (s_filterJsonFields.TryGetValue(paramsType, out var cached)) return cached;

            var found = new List<FieldInfo>();
            foreach (var name in new[] { "Filter", "Condition" })
            {
                var field = paramsType.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(string))
                    found.Add(field);
            }

            s_filterJsonFields[paramsType] = found;
            return found;
        }
    }
}
