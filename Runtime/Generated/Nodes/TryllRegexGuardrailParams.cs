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
    internal static class _FpCheck_TryllRegexGuardrailParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Regex guardrail node backed by session string storage. Branches on whether the node's input slot text matches a pattern in the storage.  Resolution priority (factory): named string_storage → inline_strings → server-wide default (default-guardrail-patterns.txt from server-config).
    /// </summary>
    [Serializable]
    [AddTypeMenu("RegexGuardrail")]
    public sealed class TryllRegexGuardrailParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.RegexGuardrail;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/regex-guardrail.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text checked against the patterns: the player's message or an earlier node's output. Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Storage", First = true)]
        [Tooltip("File of regex patterns, one per line. Used instead of Inline Strings. Both empty = the server's default patterns.")]
        [TryllFilePicker]
        // Optional string — null means "use server default".
        public string StringStorage;

        [TryllGroup("Storage")]
        [Tooltip("Regex patterns typed here; used when String Storage is empty.")]
        [TryllStructural]
        public string[] InlineStrings;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run when the Input matches a pattern. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("triggered")]
        // Graph exit "triggered" — target node name; empty = END.
        public string TriggeredExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when no pattern matches. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("not_triggered")]
        // Graph exit "not_triggered" — target node name; empty = END.
        public string NotTriggeredExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var string_storageOff = fbb.CreateString(StringStorage ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var triggered_exitOff = fbb.CreateString(TriggeredExit ?? "");
            var not_triggered_exitOff = fbb.CreateString(NotTriggeredExit ?? "");
            VectorOffset inline_stringsVecOff = default;
            if (InlineStrings != null && InlineStrings.Length > 0)
            {
                var inline_stringsStrOffs = new StringOffset[InlineStrings.Length];
                for (int i = 0; i < InlineStrings.Length; i++)
                    inline_stringsStrOffs[i] = fbb.CreateString(InlineStrings[i] ?? "");
                inline_stringsVecOff = Tryll.NodeParams.RegexGuardrailParams.CreateInlineStringsVector(fbb, inline_stringsStrOffs);
            }

            Tryll.NodeParams.RegexGuardrailParams.StartRegexGuardrailParams(fbb);
            Tryll.NodeParams.RegexGuardrailParams.AddStringStorage(fbb, string_storageOff);
            if (InlineStrings != null && InlineStrings.Length > 0)
                Tryll.NodeParams.RegexGuardrailParams.AddInlineStrings(fbb, inline_stringsVecOff);
            Tryll.NodeParams.RegexGuardrailParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.RegexGuardrailParams.AddTriggeredExit(fbb, triggered_exitOff);
            Tryll.NodeParams.RegexGuardrailParams.AddNotTriggeredExit(fbb, not_triggered_exitOff);
            var offset = Tryll.NodeParams.RegexGuardrailParams.EndRegexGuardrailParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.RegexGuardrailParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllRegexGuardrailParams)MemberwiseClone();
            if (InlineStrings != null)
                c.InlineStrings = (string[])InlineStrings.Clone();
            return c;
        }
    }
}
