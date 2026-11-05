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
    internal static class _FpCheck_TryllSamplingOverrides {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Sparse sampling override block. Null on any field means "inherit the selected model's default". No per-field (on_change) attributes: the outer sub-table field carries one callback that re-resolves the whole block and commits derived state.
    /// </summary>
    /// <summary>Sub-table: Sparse sampling override block. Null on any field means "inherit the selected model's default". No per-field (on_change) attributes: the outer sub-table field carries one callback that re-resolves the whole block and commits derived state.</summary>
    [Serializable]
    public sealed class TryllSamplingOverrides
    {
        public static readonly TryllSamplingOverrides Default = new TryllSamplingOverrides();

        [Tooltip("Randomness of the output: 0 = always the most likely token, 2 = very random. Unset = the model's default.")]
        [TryllOptionalRange(0.0f, 2.0f)]
        public TryllOptional<float> Temperature;

        [Tooltip("Nucleus sampling: pick only from the most likely tokens that together reach this probability (0–1). Unset = the model's default.")]
        [TryllOptionalRange(0.0f, 1.0f)]
        public TryllOptional<float> TopP;

        [Tooltip("Pick only from this many most likely tokens. Unset = the model's default.")]
        [TryllOptionalMin(0.0f)]
        public TryllOptional<int> TopK;

        [Tooltip("Drop tokens less likely than this fraction of the top token (0–1). Unset = the model's default.")]
        [TryllOptionalRange(0.0f, 1.0f)]
        public TryllOptional<float> MinP;

        [Tooltip("Longest answer this node may generate, in tokens. Must be smaller than Context Size. Unset = the model's default.")]
        [TryllStructural]
        [TryllOptionalMin(1.0f)]
        public TryllOptional<int> MaxTokens;

        [Tooltip("Random seed; the same seed and input give the same output. Unset = the model's default.")]
        public TryllOptional<int> Seed;

        [Tooltip("Penalty for repeating recent tokens; 1 = none, higher = less repetition. Unset = the model's default.")]
        [TryllOptionalMin(0.0f)]
        public TryllOptional<float> RepeatPenalty;

        [Tooltip("Penalty for any token already used, pushing toward new topics. Unset = the model's default.")]
        public TryllOptional<float> PresencePenalty;

        [Tooltip("Penalty that grows with how often a token was already used. Unset = the model's default.")]
        public TryllOptional<float> FrequencyPenalty;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            Tryll.NodeParams.SamplingOverrides.StartSamplingOverrides(fbb);
            if (Temperature.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddTemperature(fbb, Temperature.Value);
            if (TopP.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddTopP(fbb, TopP.Value);
            if (TopK.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddTopK(fbb, TopK.Value);
            if (MinP.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddMinP(fbb, MinP.Value);
            if (MaxTokens.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddMaxTokens(fbb, MaxTokens.Value);
            if (Seed.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddSeed(fbb, Seed.Value);
            if (RepeatPenalty.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddRepeatPenalty(fbb, RepeatPenalty.Value);
            if (PresencePenalty.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddPresencePenalty(fbb, PresencePenalty.Value);
            if (FrequencyPenalty.HasValue)
                Tryll.NodeParams.SamplingOverrides.AddFrequencyPenalty(fbb, FrequencyPenalty.Value);
            return Tryll.NodeParams.SamplingOverrides.EndSamplingOverrides(fbb).Value;
        }

        public TryllSamplingOverrides Clone()
        {
            var c = (TryllSamplingOverrides)MemberwiseClone();
            return c;
        }
    }
}
