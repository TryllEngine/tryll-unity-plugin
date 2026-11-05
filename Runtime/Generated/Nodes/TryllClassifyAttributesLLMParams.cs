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
    internal static class _FpCheck_TryllClassifyAttributesLLMParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Several specialist classifiers scored in one cached batched call. Each attribute writes slot.<output_name>_<name>. triggered_exit is taken when any attribute's own rule fires.
    /// </summary>
    [Serializable]
    [AddTypeMenu("ClassifyAttributesLLM")]
    public sealed class TryllClassifyAttributesLLMParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.ClassifyAttributesLLM;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/classify-attributes-llm.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text to classify: the player's message or an earlier node's output. Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Input")]
        [Tooltip("How many earlier dialog turns the classifier sees. 0 = the current message only.")]
        [Range(0.0f, 64.0f)]
        public int HistoryTurns = 0;

        [TryllGroup("Model", First = true)]
        [Tooltip("Language model that scores the attributes. Empty = the workflow's Default Model Name.")]
        [TryllStructural]
        [TryllModelType("language")]
        // Optional string — null means "use server default".
        public string ModelName;

        [TryllGroup("Model")]
        [Tooltip("Context window for this node, in tokens. 0 = sized automatically from the attribute prompts.")]
        [TryllStructural]
        [Min(0.0f)]
        public int ContextSize = 0;

        [TryllGroup("Prompt", First = true)]
        [Tooltip("Questions to classify, 1–16, all scored in one pass. Each writes its answer to <Output Name>_<attribute Name>.")]
        public TryllClassifyAttributeDef[] Attributes;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Sharpens (below 1) or flattens (above 1) every attribute's probabilities before its thresholds. 0 = unchanged.")]
        [Range(0.0f, 8.0f)]
        public float Temperature = 0.0f;

        [TryllGroup("Behavior")]
        [Tooltip("Exit to take if scoring fails: Default (fail open) or Triggered (fail closed).")]
        public TryllAttributeFailureRoute OnFailure = TryllAttributeFailureRoute.Default;

        [TryllGroup("Output", First = true)]
        [Tooltip("Prefix of this node's results: each attribute writes <Output Name>_<attribute Name>. Empty = this node's name.")]
        [TryllStructural]
        [TryllSlotName]
        // Optional string — null means "use server default".
        public string OutputName;

        [TryllGroup("Output")]
        [Tooltip("When to send the game an attributes_classified event: Disabled, OnFound (Triggered exit only), or Always.")]
        public TryllNotifyClient NotifyClient = TryllNotifyClient.Disabled;

        [TryllGroup("Advanced", First = true)]
        [Tooltip("Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.")]
        [TryllStructural]
        public TryllContextOverrides Context = new TryllContextOverrides();

        [TryllGroup("Advanced")]
        [Tooltip("LoRA adapters applied to this node's model. Empty = the base model.")]
        [TryllStructural]
        public TryllModelAdaptation Adaptation = new TryllModelAdaptation();

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run when any attribute fires (or scoring fails and On Failure is Triggered). Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("triggered")]
        // Graph exit "triggered" — target node name; empty = END.
        public string TriggeredExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when no attribute fires. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var contextOff = new Offset<Tryll.NodeParams.ContextOverrides>((Context ?? TryllContextOverrides.Default).Pack(fbb));
            var adaptationOff = new Offset<Tryll.NodeParams.ModelAdaptation>((Adaptation ?? TryllModelAdaptation.Default).Pack(fbb));
            var model_nameOff = fbb.CreateString(ModelName ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var output_nameOff = fbb.CreateString(OutputName ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");
            var triggered_exitOff = fbb.CreateString(TriggeredExit ?? "");
            VectorOffset attributesVecOff = default;
            if (Attributes != null && Attributes.Length > 0)
            {
                var attributesOffs = new Offset<Tryll.NodeParams.ClassifyAttributeDef>[Attributes.Length];
                for (int i = 0; i < Attributes.Length; i++)
                    attributesOffs[i] = new Offset<Tryll.NodeParams.ClassifyAttributeDef>(Attributes[i].Pack(fbb));
                attributesVecOff = Tryll.NodeParams.ClassifyAttributesLLMParams.CreateAttributesVector(fbb, attributesOffs);
            }

            Tryll.NodeParams.ClassifyAttributesLLMParams.StartClassifyAttributesLLMParams(fbb);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddModelName(fbb, model_nameOff);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddContextSize(fbb, ContextSize);
            if (Attributes != null && Attributes.Length > 0)
                Tryll.NodeParams.ClassifyAttributesLLMParams.AddAttributes(fbb, attributesVecOff);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddHistoryTurns(fbb, HistoryTurns);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddTemperature(fbb, Temperature);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddOnFailure(fbb, (Tryll.AttributeFailureRoute)(byte)OnFailure);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddNotifyClient(fbb, (Tryll.NotifyClient)(byte)NotifyClient);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddOutputName(fbb, output_nameOff);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddDefaultExit(fbb, default_exitOff);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddTriggeredExit(fbb, triggered_exitOff);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddContext(fbb, contextOff);
            Tryll.NodeParams.ClassifyAttributesLLMParams.AddAdaptation(fbb, adaptationOff);
            var offset = Tryll.NodeParams.ClassifyAttributesLLMParams.EndClassifyAttributesLLMParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.ClassifyAttributesLLMParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllClassifyAttributesLLMParams)MemberwiseClone();
            if (Attributes != null)
            {
                c.Attributes = new TryllClassifyAttributeDef[Attributes.Length];
                for (int i = 0; i < Attributes.Length; i++)
                    c.Attributes[i] = Attributes[i]?.Clone();
            }
            c.Context = Context != null
                ? Context.Clone()
                : null;
            c.Adaptation = Adaptation != null
                ? Adaptation.Clone()
                : null;
            return c;
        }
    }
}
