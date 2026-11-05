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
    internal static class _FpCheck_TryllOutputFilterOverrides {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Bounded streaming cleanup of roleplay artifacts in Generate / GenerateAndSpeak output (speaker prefixes and paired markup spans). All flags default off. Budgets are UTF-8 byte counts. Missing wire sub-table is treated by the server as an explicit all-default/off table; generated clients still materialize the sub-table (same invariant as SamplingOverrides). No on_change — nodes read live params at Execute.
    /// </summary>
    /// <summary>Sub-table: Bounded streaming cleanup of roleplay artifacts in Generate / GenerateAndSpeak output (speaker prefixes and paired markup spans). All flags default off. Budgets are UTF-8 byte counts. Missing wire sub-table is treated by the server as an explicit all-default/off table; generated clients still materialize the sub-table (same invariant as SamplingOverrides). No on_change — nodes read live params at Execute.</summary>
    [Serializable]
    public sealed class TryllOutputFilterOverrides
    {
        public static readonly TryllOutputFilterOverrides Default = new TryllOutputFilterOverrides();

        [Tooltip("Remove a leading speaker tag such as 'Bob: ' from the answer, once per turn.")]
        public bool StripSpeakerPrefix = false;

        [Tooltip("Longest speaker tag Strip Speaker Prefix looks for, in bytes.")]
        [Range(1.0f, 64.0f)]
        public int MaxPrefixBytes = 16;

        [Tooltip("Remove text between asterisks, such as *smiles*.")]
        public bool StripAsteriskSpans = false;

        [Tooltip("Remove text in parentheses, such as (laughs).")]
        public bool StripParenSpans = false;

        [Tooltip("Remove text in square brackets, such as [aside].")]
        public bool StripBracketSpans = false;

        [Tooltip("Longest span the strip options can remove, in bytes. Longer spans are kept as they are.")]
        [Range(1.0f, 256.0f)]
        public int MaxSpanBytes = 64;


        internal int Pack(FlatBufferBuilder fbb)
        {
            // Pack nested objects and strings first (bottom-up). CreateString
            // after Start* throws "object serialization must not be nested".
            Tryll.NodeParams.OutputFilterOverrides.StartOutputFilterOverrides(fbb);
            Tryll.NodeParams.OutputFilterOverrides.AddStripSpeakerPrefix(fbb, StripSpeakerPrefix);
            Tryll.NodeParams.OutputFilterOverrides.AddMaxPrefixBytes(fbb, MaxPrefixBytes);
            Tryll.NodeParams.OutputFilterOverrides.AddStripAsteriskSpans(fbb, StripAsteriskSpans);
            Tryll.NodeParams.OutputFilterOverrides.AddStripParenSpans(fbb, StripParenSpans);
            Tryll.NodeParams.OutputFilterOverrides.AddStripBracketSpans(fbb, StripBracketSpans);
            Tryll.NodeParams.OutputFilterOverrides.AddMaxSpanBytes(fbb, MaxSpanBytes);
            return Tryll.NodeParams.OutputFilterOverrides.EndOutputFilterOverrides(fbb).Value;
        }

        public TryllOutputFilterOverrides Clone()
        {
            var c = (TryllOutputFilterOverrides)MemberwiseClone();
            return c;
        }
    }
}
