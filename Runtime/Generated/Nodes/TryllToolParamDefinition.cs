// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/nodes.fbs  +  tools/codegen/templates/unity_node.cs.j2
// See: docs/dev/codegen.md
using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllToolParamDefinition {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// One named parameter in a tool's schema (used in SLM prompt construction).
    /// </summary>
    /// <summary>Sub-table: One named parameter in a tool's schema (used in SLM prompt construction).</summary>
    [Serializable]
    public sealed class TryllToolParamDefinition
    {
        public static readonly TryllToolParamDefinition Default = new TryllToolParamDefinition();

        [Tooltip("Parameter name the model fills in when it calls the tool.")]
        public string Name;

        [Tooltip("JSON Schema type of the value, such as 'string', 'number' or 'boolean'.")]
        public string Type;

        [Tooltip("What the parameter means; the model reads this to fill it in.")]
        [TryllLocalizable(true)]
        public string Description;
        [HideInInspector] public bool DescriptionLocalized;
        [HideInInspector] public string DescriptionTextId;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            StringOffset nameOff = default;
            if (Name != null)
                nameOff = fbb.CreateString(Name);
            StringOffset typeOff = default;
            if (Type != null)
                typeOff = fbb.CreateString(Type);
            StringOffset descriptionOff = default;
            if (Description != null)
                descriptionOff = fbb.CreateString(Description);
            Tryll.NodeParams.ToolParamDefinition.StartToolParamDefinition(fbb);
            if (Name != null)
                Tryll.NodeParams.ToolParamDefinition.AddName(fbb, nameOff);
            if (Type != null)
                Tryll.NodeParams.ToolParamDefinition.AddType(fbb, typeOff);
            if (Description != null)
                Tryll.NodeParams.ToolParamDefinition.AddDescription(fbb, descriptionOff);
            return Tryll.NodeParams.ToolParamDefinition.EndToolParamDefinition(fbb).Value;
        }

        public TryllToolParamDefinition Clone()
        {
            var c = (TryllToolParamDefinition)MemberwiseClone();
            return c;
        }
    }
}
