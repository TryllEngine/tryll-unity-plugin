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
    internal static class _FpCheck_TryllWriteToMemoryParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Upsert the current input slot into a named mutable store. An empty `input` reads the player's message. Only when that resolved text is empty does the node write nothing and take written_exit; only real write failures take failed_exit.
    /// </summary>
    [Serializable]
    [AddTypeMenu("WriteToMemory")]
    public sealed class TryllWriteToMemoryParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.WriteToMemory;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/write-to-memory.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text to store: the player's message or an earlier node's output. Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Storage", First = true)]
        [Tooltip("Memory store to write to, by name; it lasts for the session. May include agent variables (var.*).")]
        [TryllStructural]
        // Optional string — null means "use server default".
        public string StorageName;

        [TryllGroup("Storage")]
        [Tooltip("Record schema of the store. Nodes that read the same store must use the same schema.")]
        [TryllStructural]
        [TryllSchema]
        // Optional string — null means "use server default".
        public string Schema;

        [TryllGroup("Storage")]
        [Tooltip("Record id; writing an existing id replaces that record. May include agent variables (var.*). Empty = add a new record each time.")]
        // Optional string — null means "use server default".
        public string Id;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run after the write, or when the text to store is empty (nothing is written). Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("written")]
        // Graph exit "written" — target node name; empty = END.
        public string WrittenExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when the store rejects the write (for example, the store is full). Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("failed")]
        // Graph exit "failed" — target node name; empty = END.
        public string FailedExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var storage_nameOff = fbb.CreateString(StorageName ?? "");
            var schemaOff = fbb.CreateString(Schema ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var idOff = fbb.CreateString(Id ?? "");
            var written_exitOff = fbb.CreateString(WrittenExit ?? "");
            var failed_exitOff = fbb.CreateString(FailedExit ?? "");

            Tryll.NodeParams.WriteToMemoryParams.StartWriteToMemoryParams(fbb);
            Tryll.NodeParams.WriteToMemoryParams.AddStorageName(fbb, storage_nameOff);
            Tryll.NodeParams.WriteToMemoryParams.AddSchema(fbb, schemaOff);
            Tryll.NodeParams.WriteToMemoryParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.WriteToMemoryParams.AddId(fbb, idOff);
            Tryll.NodeParams.WriteToMemoryParams.AddWrittenExit(fbb, written_exitOff);
            Tryll.NodeParams.WriteToMemoryParams.AddFailedExit(fbb, failed_exitOff);
            var offset = Tryll.NodeParams.WriteToMemoryParams.EndWriteToMemoryParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.WriteToMemoryParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllWriteToMemoryParams)MemberwiseClone();
            return c;
        }
    }
}
