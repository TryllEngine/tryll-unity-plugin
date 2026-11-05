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
    internal static class _FpCheck_TryllRetrieveParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// RAG retrieval node.  Embeds the HumanMessage, searches an embedded storage, and attaches a KnowledgeComponent to the current interaction.
    /// </summary>
    [Serializable]
    [AddTypeMenu("Retrieve")]
    public sealed class TryllRetrieveParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Retrieve;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/retrieve.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text to search with: the player's message or an earlier node's output. Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Storage", First = true)]
        [Tooltip("From File = a prebuilt knowledge file. From Name = a memory store the game fills during play (Write To Memory).")]
        [TryllStructural]
        public TryllKnowledgeSourceKind SourceKind = TryllKnowledgeSourceKind.FromFile;

        [TryllGroup("Storage")]
        [Tooltip("Embedded knowledge file to search. Used when Source Kind is From File; leave empty with From Name.")]
        [TryllStructural]
        [TryllFilePicker]
        // Optional string — null means "use server default".
        public string EmbeddedStringStorage;

        [TryllGroup("Storage")]
        [Tooltip("JSON filter on chunk metadata; only matching chunks are searched. Empty = no filter.")]
        // Optional string — null means "use server default".
        public string Filter;

        [TryllGroup("Storage")]
        [Tooltip("Memory store to search, by name. Used when Source Kind is From Name. May include agent variables (var.*).")]
        [TryllStructural]
        // Optional string — null means "use server default".
        public string StorageName;

        [TryllGroup("Storage")]
        [Tooltip("Record schema of the store. Required with From Name; with From File, checked against the file.")]
        [TryllStructural]
        [TryllSchema]
        // Optional string — null means "use server default".
        public string Schema;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Dense = match by meaning. Lexical = match by keywords (BM25). Hybrid = both, merged.")]
        public TryllRetrievalMode RetrievalMode = TryllRetrievalMode.Dense;

        [TryllGroup("Behavior")]
        [Tooltip("How many matching chunks to return.")]
        [Range(1.0f, 1000.0f)]
        public int TopK = 2;

        [TryllGroup("Behavior")]
        [Tooltip("Max cosine distance for a chunk to count (0 = identical, 2 = opposite). 0 = no limit. Ignored in Lexical mode.")]
        [Range(0.0f, 2.0f)]
        public float Threshold = 0.5f;

        [TryllGroup("Behavior")]
        [Tooltip("Hybrid only: how strongly the merge favours top-ranked hits. Higher = flatter. 60 is the standard value.")]
        [Range(1.0f, 1000.0f)]
        public int RrfK = 60;

        [TryllGroup("Output", First = true)]
        [Tooltip("Name templates use for these results: the knowledge_<source> tag. Empty = this node's name.")]
        // Optional string — null means "use server default".
        public string Source;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run when at least one chunk is found. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("found")]
        // Graph exit "found" — target node name; empty = END.
        public string FoundExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when nothing is found. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("not_found")]
        // Graph exit "not_found" — target node name; empty = END.
        public string NotFoundExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var embedded_string_storageOff = fbb.CreateString(EmbeddedStringStorage ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var sourceOff = fbb.CreateString(Source ?? "");
            var filterOff = fbb.CreateString(Filter ?? "");
            var found_exitOff = fbb.CreateString(FoundExit ?? "");
            var not_found_exitOff = fbb.CreateString(NotFoundExit ?? "");
            var storage_nameOff = fbb.CreateString(StorageName ?? "");
            var schemaOff = fbb.CreateString(Schema ?? "");

            Tryll.NodeParams.RetrieveParams.StartRetrieveParams(fbb);
            Tryll.NodeParams.RetrieveParams.AddEmbeddedStringStorage(fbb, embedded_string_storageOff);
            Tryll.NodeParams.RetrieveParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.RetrieveParams.AddTopK(fbb, TopK);
            Tryll.NodeParams.RetrieveParams.AddThreshold(fbb, Threshold);
            Tryll.NodeParams.RetrieveParams.AddRetrievalMode(fbb, (Tryll.RetrievalMode)(byte)RetrievalMode);
            Tryll.NodeParams.RetrieveParams.AddRrfK(fbb, RrfK);
            Tryll.NodeParams.RetrieveParams.AddSource(fbb, sourceOff);
            Tryll.NodeParams.RetrieveParams.AddFilter(fbb, filterOff);
            Tryll.NodeParams.RetrieveParams.AddFoundExit(fbb, found_exitOff);
            Tryll.NodeParams.RetrieveParams.AddNotFoundExit(fbb, not_found_exitOff);
            Tryll.NodeParams.RetrieveParams.AddStorageName(fbb, storage_nameOff);
            Tryll.NodeParams.RetrieveParams.AddSchema(fbb, schemaOff);
            Tryll.NodeParams.RetrieveParams.AddSourceKind(fbb, (Tryll.KnowledgeSourceKind)(byte)SourceKind);
            var offset = Tryll.NodeParams.RetrieveParams.EndRetrieveParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.RetrieveParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllRetrieveParams)MemberwiseClone();
            return c;
        }
    }
}
