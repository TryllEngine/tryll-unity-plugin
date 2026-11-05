// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;

namespace Tryll.Client
{
    /// <summary>
    /// Wire type of one declared per-agent variable. Mirrors the
    /// <c>Tryll.VariableValue</c> union member that carries the value
    /// (VarInt/VarFloat/VarString/VarBool/VarStringSet).
    /// </summary>
    public enum TryllVariableType : byte
    {
        Int       = 0,
        Float     = 1,
        String    = 2,
        Bool      = 3,
        StringSet = 4,
    }

    /// <summary>
    /// One declared per-agent variable — name, type, and initial value.
    /// Authored on <see cref="TryllWorkflowAsset.Variables"/>; the complete
    /// list is sent once in <c>CreateAgentRequest</c>
    /// and locks each variable's type for the agent's lifetime. Only one of
    /// the value fields is meaningful, selected by <see cref="Type"/> — the
    /// Inspector drawer shows only that field.
    /// </summary>
    [Serializable]
    public sealed class TryllVariableDecl
    {
        public string           Name           = "";
        public TryllVariableType Type          = TryllVariableType.Int;
        public long              IntValue       = 0;
        public double            FloatValue     = 0.0;
        public string            StringValue    = "";
        public bool              BoolValue      = false;
        public List<string>      StringSetValue = new List<string>();
        public bool              AllowOutputSubstitution = false;

        /// <summary>Return this declaration's value as a boxed native object (long/double/string/bool/List&lt;string&gt;).</summary>
        public object GetBoxedValue()
        {
            switch (Type)
            {
                case TryllVariableType.Int:       return IntValue;
                case TryllVariableType.Float:     return FloatValue;
                case TryllVariableType.String:    return StringValue ?? "";
                case TryllVariableType.Bool:      return BoolValue;
                case TryllVariableType.StringSet: return new List<string>(StringSetValue ?? new List<string>());
                default:                          return null;
            }
        }

        /// <summary>Build the wire union POCO for this declaration's value (used when encoding CreateAgentRequest.variables).</summary>
        internal Tryll.VariableValueUnion ToVariableValueUnion()
        {
            switch (Type)
            {
                case TryllVariableType.Int:
                    return Tryll.VariableValueUnion.FromVarInt(new Tryll.VarIntT { V = IntValue });
                case TryllVariableType.Float:
                    return Tryll.VariableValueUnion.FromVarFloat(new Tryll.VarFloatT { V = FloatValue });
                case TryllVariableType.String:
                    return Tryll.VariableValueUnion.FromVarString(new Tryll.VarStringT { V = StringValue ?? "" });
                case TryllVariableType.Bool:
                    return Tryll.VariableValueUnion.FromVarBool(new Tryll.VarBoolT { V = BoolValue });
                case TryllVariableType.StringSet:
                    return Tryll.VariableValueUnion.FromVarStringSet(
                        new Tryll.VarStringSetT { V = new List<string>(StringSetValue ?? new List<string>()) });
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Per-component value override for one of the workflow asset's declared
    /// variables (Material-Instance pattern: the asset owns the declaration —
    /// name and type — the component may only override the initial value).
    /// <see cref="Name"/> and <see cref="Value"/>.Type must match a variable
    /// declared on the asset; non-matching (stale) overrides are skipped with
    /// a warning at agent-creation time.
    /// </summary>
    /// <remarks>
    /// Sparse serialization: only variables the author has enabled (or previously
    /// enabled) appear in <see cref="TryllAgentComponent.VariableOverrides"/>.
    /// The custom Inspector synthesizes one row per declaration; enabling creates
    /// an entry seeded from the workflow default.
    /// </remarks>
    [Serializable]
    public sealed class TryllVariableOverride
    {
        public string           Name    = "";
        public bool              Enabled = false;
        public TryllVariableDecl Value   = new TryllVariableDecl();
    }
}
