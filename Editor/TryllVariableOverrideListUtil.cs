// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Pure helpers for Material-Instance style variable overrides on
    /// <see cref="TryllAgentComponent"/>. Used by the custom Inspector and covered
    /// by Edit Mode tests — no SerializedProperty / GUI dependency.
    /// </summary>
    internal static class TryllVariableOverrideListUtil
    {
        /// <summary>
        /// Declarations that drive the override UI: the assigned workflow asset's
        /// Variables, or empty when no asset is set.
        /// </summary>
        public static List<TryllVariableDecl> ResolveDeclared(TryllAgentComponent component)
        {
            if (component == null) return new List<TryllVariableDecl>();
            if (component.WorkflowAsset != null)
                return component.WorkflowAsset.Variables ?? new List<TryllVariableDecl>();
            return new List<TryllVariableDecl>();
        }

        /// <summary>Index of the first override whose <c>Name</c> equals <paramref name="name"/>, or -1.</summary>
        public static int FindFirstOverrideIndex(IList<TryllVariableOverride> overrides, string name)
        {
            if (overrides == null || string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < overrides.Count; i++)
            {
                var o = overrides[i];
                if (o != null && o.Name == name) return i;
            }
            return -1;
        }

        /// <summary>
        /// Ensure a sparse override entry exists for <paramref name="decl"/>. When the
        /// entry is newly created, or its nested type disagrees with the declaration,
        /// copy name/type/value from the declaration (Material-Instance seed).
        /// Does not change <see cref="TryllVariableOverride.Enabled"/>.
        /// </summary>
        public static TryllVariableOverride EnsureEntry(
            IList<TryllVariableOverride> overrides, TryllVariableDecl decl)
        {
            if (overrides == null || decl == null || string.IsNullOrEmpty(decl.Name))
                return null;

            int idx = FindFirstOverrideIndex(overrides, decl.Name);
            if (idx < 0)
            {
                var created = new TryllVariableOverride
                {
                    Name    = decl.Name,
                    Enabled = false,
                    Value   = CloneDecl(decl),
                };
                overrides.Add(created);
                return created;
            }

            var existing = overrides[idx];
            if (existing == null)
            {
                existing = new TryllVariableOverride { Name = decl.Name, Enabled = false };
                overrides[idx] = existing;
            }

            existing.Name = decl.Name;
            if (existing.Value == null || existing.Value.Type != decl.Type)
                existing.Value = CloneDecl(decl);
            else
                existing.Value.Name = decl.Name; // keep name in sync; leave authored values

            return existing;
        }

        /// <summary>Overwrite an override's nested value from the workflow declaration default.</summary>
        public static void SeedFromDeclaration(TryllVariableOverride ov, TryllVariableDecl decl)
        {
            if (ov == null || decl == null) return;
            ov.Name  = decl.Name;
            ov.Value = CloneDecl(decl);
        }

        /// <summary>
        /// Indices of overrides that should not appear as declaration-driven rows:
        /// empty/null names, names absent from the declaration set, or duplicate
        /// names after the first occurrence. Order is ascending index.
        /// </summary>
        public static List<int> CollectLegacyIndices(
            IList<TryllVariableOverride> overrides, IEnumerable<TryllVariableDecl> declared)
        {
            var result = new List<int>();
            if (overrides == null) return result;

            var declaredNames = new HashSet<string>();
            if (declared != null)
            {
                foreach (var d in declared)
                {
                    if (d != null && !string.IsNullOrEmpty(d.Name))
                        declaredNames.Add(d.Name);
                }
            }

            var seenNames = new HashSet<string>();
            for (int i = 0; i < overrides.Count; i++)
            {
                var o = overrides[i];
                if (o == null || string.IsNullOrEmpty(o.Name))
                {
                    result.Add(i);
                    continue;
                }
                if (!declaredNames.Contains(o.Name) || !seenNames.Add(o.Name))
                    result.Add(i);
            }
            return result;
        }

        /// <summary>
        /// Remove legacy / stale override entries (highest index first so removals stay valid).
        /// Returns the number of entries removed.
        /// </summary>
        public static int RemoveLegacyEntries(
            IList<TryllVariableOverride> overrides, IEnumerable<TryllVariableDecl> declared)
        {
            if (overrides == null) return 0;
            var legacy = CollectLegacyIndices(overrides, declared);
            for (int i = legacy.Count - 1; i >= 0; i--)
                overrides.RemoveAt(legacy[i]);
            return legacy.Count;
        }

        /// <summary>
        /// Push the effective Inspector value onto the live agent mirror.
        /// When <paramref name="enabled"/> is true, assign the override value;
        /// when false, assign the workflow declaration default (not
        /// <see cref="TryllAgentVariables.Reset"/>, which restores CreateAgent-time
        /// merged initials that may themselves have been overrides).
        /// </summary>
        public static TryllError ApplyEffectiveValue(
            TryllAgentVariables vars,
            TryllVariableDecl declared,
            TryllVariableDecl overrideValue,
            bool enabled)
        {
            if (vars == null)
                return new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready");
            if (declared == null || string.IsNullOrEmpty(declared.Name))
                return new TryllError(TryllErrorCode.InvalidVariableValue, "Missing declaration");

            var source = enabled && overrideValue != null ? overrideValue : declared;
            return AssignFromDecl(vars, declared.Name, declared.Type, source);
        }

        public static TryllError AssignFromDecl(
            TryllAgentVariables vars, string name, TryllVariableType type, TryllVariableDecl source)
        {
            if (vars == null)
                return new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready");
            if (source == null)
                return new TryllError(TryllErrorCode.InvalidVariableValue, "Missing value");

            switch (type)
            {
                case TryllVariableType.Int:       return vars.SetInt(name, source.IntValue);
                case TryllVariableType.Float:     return vars.SetFloat(name, source.FloatValue);
                case TryllVariableType.String:    return vars.SetString(name, source.StringValue ?? "");
                case TryllVariableType.Bool:      return vars.SetBool(name, source.BoolValue);
                case TryllVariableType.StringSet: return vars.SetStringSet(name, source.StringSetValue ?? new List<string>());
                default:
                    return new TryllError(TryllErrorCode.InvalidVariableValue, $"Unsupported type {type}");
            }
        }

        public static TryllVariableDecl CloneDecl(TryllVariableDecl source)
        {
            if (source == null) return null;
            return new TryllVariableDecl
            {
                Name           = source.Name ?? "",
                Type           = source.Type,
                IntValue       = source.IntValue,
                FloatValue     = source.FloatValue,
                StringValue    = source.StringValue ?? "",
                BoolValue      = source.BoolValue,
                StringSetValue = new List<string>(source.StringSetValue ?? new List<string>()),
            };
        }
    }
}
