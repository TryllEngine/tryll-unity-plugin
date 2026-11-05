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
    internal static class _FpCheck_TryllToolDefinition {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// A callable tool definition used for prompt construction by ToolCallNode. The server does not execute tools; these are schema-only for SLM prompting.
    /// </summary>
    /// <summary>Sub-table: A callable tool definition used for prompt construction by ToolCallNode. The server does not execute tools; these are schema-only for SLM prompting.</summary>
    [Serializable]
    public sealed class TryllToolDefinition
    {
        public static readonly TryllToolDefinition Default = new TryllToolDefinition();

        [Tooltip("Tool name the model uses to call it, and the game receives with the call.")]
        public string Name;

        [Tooltip("What the tool does; the model reads this to decide when to call it.")]
        [TryllLocalizable(true)]
        public string Description;
        [HideInInspector] public bool DescriptionLocalized;
        [HideInInspector] public string DescriptionTextId;

        [Tooltip("Values the model must supply when it calls this tool.")]
        public TryllToolParamDefinition[] Parameters;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            StringOffset nameOff = default;
            if (Name != null)
                nameOff = fbb.CreateString(Name);
            StringOffset descriptionOff = default;
            if (Description != null)
                descriptionOff = fbb.CreateString(Description);
            VectorOffset parametersVecOff = default;
            if (Parameters != null && Parameters.Length > 0)
            {
                var parametersOffs = new Offset<Tryll.NodeParams.ToolParamDefinition>[Parameters.Length];
                for (int i = 0; i < Parameters.Length; i++)
                    parametersOffs[i] = new Offset<Tryll.NodeParams.ToolParamDefinition>(Parameters[i].Pack(fbb));
                parametersVecOff = Tryll.NodeParams.ToolDefinition.CreateParametersVector(fbb, parametersOffs);
            }
            Tryll.NodeParams.ToolDefinition.StartToolDefinition(fbb);
            if (Name != null)
                Tryll.NodeParams.ToolDefinition.AddName(fbb, nameOff);
            if (Description != null)
                Tryll.NodeParams.ToolDefinition.AddDescription(fbb, descriptionOff);
            if (Parameters != null && Parameters.Length > 0)
                Tryll.NodeParams.ToolDefinition.AddParameters(fbb, parametersVecOff);
            return Tryll.NodeParams.ToolDefinition.EndToolDefinition(fbb).Value;
        }

        public TryllToolDefinition Clone()
        {
            var c = (TryllToolDefinition)MemberwiseClone();
            if (Parameters != null)
            {
                c.Parameters = new TryllToolParamDefinition[Parameters.Length];
                for (int i = 0; i < Parameters.Length; i++)
                    c.Parameters[i] = Parameters[i]?.Clone();
            }
            return c;
        }
    }
}
