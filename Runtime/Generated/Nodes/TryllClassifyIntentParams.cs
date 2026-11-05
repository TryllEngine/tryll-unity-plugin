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
    internal static class _FpCheck_TryllClassifyIntentParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Intent classification node (embedding-based, top-1 nearest-neighbour). Searches a labelled embedded storage and attaches an IntentionComponent.
    /// </summary>
    [Serializable]
    [AddTypeMenu("ClassifyIntent")]
    public sealed class TryllClassifyIntentParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.ClassifyIntent;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/classify-intent.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text to classify: the player's message or an earlier node's output. Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Storage", First = true)]
        [Tooltip("Embedded file of example phrases, each labelled with its intent.")]
        [TryllStructural]
        [TryllFilePicker]
        // Optional string — null means "use server default".
        public string EmbeddedStringStorage;

        [TryllGroup("Storage")]
        [Tooltip("Metadata field in that file that holds each example's intent label.")]
        // Optional string — null means "use server default".
        public string MetadataField;

        [TryllGroup("Storage")]
        [Tooltip("JSON filter on example metadata; only matching examples are compared. Empty = no filter.")]
        // Optional string — null means "use server default".
        public string Filter;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Max cosine distance of the closest example (0 = identical, 2 = opposite). Farther = Not Found.")]
        [Range(0.0f, 2.0f)]
        public float Threshold = 0.25f;

        [TryllGroup("Output", First = true)]
        [Tooltip("When to send the game an intent_classified event: Disabled, OnFound (Found exit only), or Always.")]
        public TryllNotifyClient NotifyClient = TryllNotifyClient.Disabled;

        [TryllGroup("Advanced", First = true)]
        [Tooltip("How many closest examples to report in diagnostics. Routing always uses the closest one.")]
        [Range(1.0f, 64.0f)]
        public int DiagnosticTopk = 5;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run when the closest example is within Threshold. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("found")]
        // Graph exit "found" — target node name; empty = END.
        public string FoundExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when no example is close enough. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("not_found")]
        // Graph exit "not_found" — target node name; empty = END.
        public string NotFoundExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var embedded_string_storageOff = fbb.CreateString(EmbeddedStringStorage ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var metadata_fieldOff = fbb.CreateString(MetadataField ?? "");
            var filterOff = fbb.CreateString(Filter ?? "");
            var found_exitOff = fbb.CreateString(FoundExit ?? "");
            var not_found_exitOff = fbb.CreateString(NotFoundExit ?? "");

            Tryll.NodeParams.ClassifyIntentParams.StartClassifyIntentParams(fbb);
            Tryll.NodeParams.ClassifyIntentParams.AddEmbeddedStringStorage(fbb, embedded_string_storageOff);
            Tryll.NodeParams.ClassifyIntentParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.ClassifyIntentParams.AddMetadataField(fbb, metadata_fieldOff);
            Tryll.NodeParams.ClassifyIntentParams.AddThreshold(fbb, Threshold);
            Tryll.NodeParams.ClassifyIntentParams.AddFilter(fbb, filterOff);
            Tryll.NodeParams.ClassifyIntentParams.AddNotifyClient(fbb, (Tryll.NotifyClient)(byte)NotifyClient);
            Tryll.NodeParams.ClassifyIntentParams.AddDiagnosticTopk(fbb, DiagnosticTopk);
            Tryll.NodeParams.ClassifyIntentParams.AddFoundExit(fbb, found_exitOff);
            Tryll.NodeParams.ClassifyIntentParams.AddNotFoundExit(fbb, not_found_exitOff);
            var offset = Tryll.NodeParams.ClassifyIntentParams.EndClassifyIntentParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.ClassifyIntentParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllClassifyIntentParams)MemberwiseClone();
            return c;
        }
    }
}
