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
    internal static class _FpCheck_TryllContextOverrides {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Backend context construction. Null fields inherit ModelVariant then server config, except `n_outputs_max` which CreateContext defaults to 1. Structural at the field: these are baked into llama_context_params at context creation, so a change means a new context and a lost KV cache.
    /// </summary>
    /// <summary>Sub-table: Backend context construction. Null fields inherit ModelVariant then server config, except `n_outputs_max` which CreateContext defaults to 1. Structural at the field: these are baked into llama_context_params at context creation, so a change means a new context and a lost KV cache.</summary>
    [Serializable]
    public sealed class TryllContextOverrides
    {
        public static readonly TryllContextOverrides Default = new TryllContextOverrides();

        [Tooltip("Number format of the context memory (KV cache). Smaller types save memory at some quality cost. Inherit = the model's setting, else q8_0.")]
        public TryllKvCacheType KvCacheType = TryllKvCacheType.Inherit;

        [Tooltip("Keep the context memory on the GPU. Can only turn it off, not on for a model placed in RAM. Unset = server and model placement decide.")]
        public TryllOptional<bool> OffloadKqv;

        [Tooltip("Tokens processed per GPU step; sizes the working buffers. Unset = the model's setting.")]
        [TryllOptionalMin(1.0f)]
        public TryllOptional<int> NUbatch;

        [Tooltip("Leave unset. Advanced: how many positions return scores per step.")]
        [TryllOptionalMin(1.0f)]
        public TryllOptional<int> NOutputsMax;

        [Tooltip("Most tokens sent to the model in one call while reading the prompt. Unset = the model's setting.")]
        [TryllOptionalMin(1.0f)]
        public TryllOptional<int> NBatch;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            Tryll.NodeParams.ContextOverrides.StartContextOverrides(fbb);
            Tryll.NodeParams.ContextOverrides.AddKvCacheType(fbb, (Tryll.KvCacheType)(byte)KvCacheType);
            if (OffloadKqv.HasValue)
                Tryll.NodeParams.ContextOverrides.AddOffloadKqv(fbb, OffloadKqv.Value);
            if (NUbatch.HasValue)
                Tryll.NodeParams.ContextOverrides.AddNUbatch(fbb, NUbatch.Value);
            if (NOutputsMax.HasValue)
                Tryll.NodeParams.ContextOverrides.AddNOutputsMax(fbb, NOutputsMax.Value);
            if (NBatch.HasValue)
                Tryll.NodeParams.ContextOverrides.AddNBatch(fbb, NBatch.Value);
            return Tryll.NodeParams.ContextOverrides.EndContextOverrides(fbb).Value;
        }

        public TryllContextOverrides Clone()
        {
            var c = (TryllContextOverrides)MemberwiseClone();
            return c;
        }
    }
}
