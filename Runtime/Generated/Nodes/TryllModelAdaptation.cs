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
    internal static class _FpCheck_TryllModelAdaptation {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Changes to how the model behaves for one LLM node, applied to the node's llama context after it is created. Kept apart from ContextOverrides, which only sizes memory and batching. Control vectors (steering) will join here.
    /// </summary>
    /// <summary>Sub-table: Changes to how the model behaves for one LLM node, applied to the node's llama context after it is created. Kept apart from ContextOverrides, which only sizes memory and batching. Control vectors (steering) will join here.</summary>
    [Serializable]
    public sealed class TryllModelAdaptation
    {
        public static readonly TryllModelAdaptation Default = new TryllModelAdaptation();

        [Tooltip("LoRA adapters applied together, up to 8, each file once. Order does not matter. Empty = none.")]
        public TryllLoraAdapter[] Loras;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            VectorOffset lorasVecOff = default;
            if (Loras != null && Loras.Length > 0)
            {
                var lorasOffs = new Offset<Tryll.NodeParams.LoraAdapter>[Loras.Length];
                for (int i = 0; i < Loras.Length; i++)
                    lorasOffs[i] = new Offset<Tryll.NodeParams.LoraAdapter>(Loras[i].Pack(fbb));
                lorasVecOff = Tryll.NodeParams.ModelAdaptation.CreateLorasVector(fbb, lorasOffs);
            }
            Tryll.NodeParams.ModelAdaptation.StartModelAdaptation(fbb);
            if (Loras != null && Loras.Length > 0)
                Tryll.NodeParams.ModelAdaptation.AddLoras(fbb, lorasVecOff);
            return Tryll.NodeParams.ModelAdaptation.EndModelAdaptation(fbb).Value;
        }

        public TryllModelAdaptation Clone()
        {
            var c = (TryllModelAdaptation)MemberwiseClone();
            if (Loras != null)
            {
                c.Loras = new TryllLoraAdapter[Loras.Length];
                for (int i = 0; i < Loras.Length; i++)
                    c.Loras[i] = Loras[i]?.Clone();
            }
            return c;
        }
    }
}
