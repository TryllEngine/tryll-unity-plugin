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
    internal static class _FpCheck_TryllCannedResponseParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Canned-response node backed by session string storage. Resolution priority (factory): named string_storage → lines → server-wide default (default-canned-responses.txt from server-config).
    /// </summary>
    [Serializable]
    [AddTypeMenu("CannedResponse")]
    public sealed class TryllCannedResponseParams : TryllNodeParamsBase, ISerializationCallbackReceiver    {
        public override TryllNodeType NodeType => TryllNodeType.CannedResponse;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/canned-response.html";

        [TryllGroup("Storage", First = true)]
        [Tooltip("File of responses, one per line. Used instead of Lines. Both empty = the server's default responses.")]
        [TryllFilePicker]
        // Optional string — null means "use server default".
        public string StringStorage;

        [TryllGroup("Storage")]
        [Tooltip("Responses typed here; used when String Storage is empty.")]
        [TryllStructural]
        public TryllLocalizableText[] Lines;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Which response comes next: Random, First (always the first), or RoundRobin (each in turn).")]
        public TryllCannedResponseSelectionStrategy SelectionStrategy = TryllCannedResponseSelectionStrategy.RoundRobin;

        [TryllGroup("Output", First = true)]
        [Tooltip("Name later nodes use to read the chosen response (in their Input, or as the slot.<name> tag). Empty = this node's name.")]
        [TryllStructural]
        [TryllSlotName]
        // Optional string — null means "use server default".
        public string OutputName;

        [TryllGroup("Output")]
        [Tooltip("How the response reaches the game: Whole (in one piece), Streamed, or None (kept for later nodes only).")]
        public TryllSendAnswer Send = TryllSendAnswer.Whole;

        [TryllGroup("Output")]
        [Tooltip("Whether later turns remember this response as an assistant message. None = forgotten after this turn.")]
        public TryllHistoryRole HistoryRole = TryllHistoryRole.Assistant;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run after the response. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var string_storageOff = fbb.CreateString(StringStorage ?? "");
            var output_nameOff = fbb.CreateString(OutputName ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");
            VectorOffset linesVecOff = default;
            if (Lines != null && Lines.Length > 0)
            {
                var linesOffs = new Offset<Tryll.NodeParams.LocalizableText>[Lines.Length];
                for (int i = 0; i < Lines.Length; i++)
                    linesOffs[i] = new Offset<Tryll.NodeParams.LocalizableText>(Lines[i].Pack(fbb));
                linesVecOff = Tryll.NodeParams.CannedResponseParams.CreateLinesVector(fbb, linesOffs);
            }

            Tryll.NodeParams.CannedResponseParams.StartCannedResponseParams(fbb);
            Tryll.NodeParams.CannedResponseParams.AddStringStorage(fbb, string_storageOff);
            Tryll.NodeParams.CannedResponseParams.AddSelectionStrategy(fbb, (Tryll.CannedResponseSelectionStrategy)(byte)SelectionStrategy);
            Tryll.NodeParams.CannedResponseParams.AddOutputName(fbb, output_nameOff);
            Tryll.NodeParams.CannedResponseParams.AddSend(fbb, (Tryll.SendAnswer)(byte)Send);
            Tryll.NodeParams.CannedResponseParams.AddHistoryRole(fbb, (Tryll.HistoryRole)(byte)HistoryRole);
            Tryll.NodeParams.CannedResponseParams.AddDefaultExit(fbb, default_exitOff);
            if (Lines != null && Lines.Length > 0)
                Tryll.NodeParams.CannedResponseParams.AddLines(fbb, linesVecOff);
            var offset = Tryll.NodeParams.CannedResponseParams.EndCannedResponseParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.CannedResponseParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllCannedResponseParams)MemberwiseClone();
            if (Lines != null)
            {
                c.Lines = new TryllLocalizableText[Lines.Length];
                for (int i = 0; i < Lines.Length; i++)
                    c.Lines[i] = Lines[i]?.Clone();
            }
            if (InlineStrings != null)
                c.InlineStrings = (string[])InlineStrings.Clone();
            return c;
        }

        // Older assets stored this list as a string array under this name.
        // OnAfterDeserialize copies it into Lines when that list is
        // empty. OnBeforeSerialize drops it once Lines has the text,
        // so the next save does not keep both.
        [HideInInspector] public string[] InlineStrings;

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (Lines != null && Lines.Length > 0)
                return;
            if (InlineStrings == null || InlineStrings.Length == 0)
                return;
            Lines = new TryllLocalizableText[InlineStrings.Length];
            for (int i = 0; i < InlineStrings.Length; i++)
                Lines[i] = new TryllLocalizableText { Text = InlineStrings[i] ?? "" };
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (Lines != null && Lines.Length > 0)
                InlineStrings = null;
        }
    }
}
