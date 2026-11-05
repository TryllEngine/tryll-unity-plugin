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
    internal static class _FpCheck_TryllTransformParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Model-free input-composition node.  Renders a Mustache template over `user_message` and the current turn's slots into its own slot; performs no inference and never emits on the wire (`send = None`, `history_role = none`, fixed). Typical use: query rewriting, fan-out composition feeding a downstream Retrieve/Generate `input`.
    /// </summary>
    [Serializable]
    [AddTypeMenu("Transform")]
    public sealed class TryllTransformParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Transform;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/transform.html";

        [TryllGroup("Prompt", First = true)]
        [Tooltip("Mustache text this node outputs. Can use the tags user_message, slot.<name>, knowledge_<source> and #instructions.")]
        [TryllLocalizable(true)]
        // Optional string — null means "use server default".
        public string Template;
        // Client-only. Pack() still writes Template; the resolve step fills it from TemplateTextId.
        [HideInInspector] public bool TemplateLocalized;
        [HideInInspector] public string TemplateTextId;

        [TryllGroup("Output", First = true)]
        [Tooltip("Name later nodes use to read the rendered text (in their Input, or as the slot.<name> tag). Empty = this node's name.")]
        [TryllStructural]
        [TryllSlotName]
        // Optional string — null means "use server default".
        public string OutputName;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run next. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var templateOff = fbb.CreateString(Template ?? "");
            var output_nameOff = fbb.CreateString(OutputName ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");

            Tryll.NodeParams.TransformParams.StartTransformParams(fbb);
            Tryll.NodeParams.TransformParams.AddTemplate(fbb, templateOff);
            Tryll.NodeParams.TransformParams.AddOutputName(fbb, output_nameOff);
            Tryll.NodeParams.TransformParams.AddDefaultExit(fbb, default_exitOff);
            var offset = Tryll.NodeParams.TransformParams.EndTransformParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.TransformParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllTransformParams)MemberwiseClone();
            return c;
        }
    }
}
