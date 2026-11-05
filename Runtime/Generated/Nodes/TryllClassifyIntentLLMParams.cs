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
    internal static class _FpCheck_TryllClassifyIntentLLMParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Intent classification node (LLM first-token logprob over letter candidates). Scores labels via a dedicated LLM, attaches an IntentionComponent on found, and writes the decision to slot.<output_name>.
    /// </summary>
    [Serializable]
    [AddTypeMenu("ClassifyIntentLLM")]
    public sealed class TryllClassifyIntentLLMParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.ClassifyIntentLLM;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/classify-intent-llm.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text to classify: the player's message or an earlier node's output. Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Input")]
        [Tooltip("How many earlier dialog turns the classifier sees. 0 = the current message only.")]
        [Range(0.0f, 64.0f)]
        public int HistoryTurns = 2;

        [TryllGroup("Model", First = true)]
        [Tooltip("Language model that scores the intents. Empty = the workflow's Default Model Name.")]
        [TryllStructural]
        [TryllModelType("language")]
        // Optional string — null means "use server default".
        public string ModelName;

        [TryllGroup("Model")]
        [Tooltip("Context window for this node, in tokens; must fit the model's trained maximum. 0 = 2048.")]
        [TryllStructural]
        [Min(0.0f)]
        public int ContextSize = 0;

        [TryllGroup("Prompt", First = true)]
        [Tooltip("Instructions for the classifier. Must include the intents_block tag (the lettered intent list); the letters tag ('A, B, C') is optional.")]
        [TryllLocalizable(true)]
        // Optional string — null means "use server default".
        public string SystemPrompt;
        // Client-only. Pack() still writes SystemPrompt; the resolve step fills it from SystemPromptTextId.
        [HideInInspector] public bool SystemPromptLocalized;
        [HideInInspector] public string SystemPromptTextId;

        [TryllGroup("Prompt")]
        [Tooltip("The intents to choose from, 2–26. Each has an id (the label written to the output) and a description the model reads.")]
        [TryllStructural]
        public TryllClassifyIntentEntry[] Intents;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Minimum probability (0–1) the winning intent needs; below it = Not Found.")]
        [Range(0.0f, 1.0f)]
        public float Threshold = 0.5f;

        [TryllGroup("Behavior")]
        [Tooltip("Minimum lead (0–1) of the winner over the runner-up; less = Not Found.")]
        [Range(0.0f, 1.0f)]
        public float Margin = 0.15f;

        [TryllGroup("Behavior")]
        [Tooltip("Sharpens (below 1) or flattens (above 1) the probabilities before Threshold and Margin. 0 = unchanged.")]
        [Range(0.0f, 8.0f)]
        public float Temperature = 0.0f;

        [TryllGroup("Behavior")]
        [Tooltip("Intent id that means 'none of these': if it wins, take Not Found. Also the output on Not Found. May be #uncertain or empty.")]
        // Optional string — null means "use server default".
        public string NotFoundIntent;

        [TryllGroup("Output", First = true)]
        [Tooltip("Name later nodes use to read the result: the winning intent id, or Not Found Intent. Empty = this node's name.")]
        [TryllStructural]
        [TryllSlotName]
        // Optional string — null means "use server default".
        public string OutputName;

        [TryllGroup("Output")]
        [Tooltip("When to send the game an intent_llm_classified event: Disabled, OnFound (Found exit only), or Always.")]
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
        [Tooltip("Node to run when an intent wins clearly (passes Threshold and Margin). Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("found")]
        // Graph exit "found" — target node name; empty = END.
        public string FoundExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when no intent wins clearly. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("not_found")]
        // Graph exit "not_found" — target node name; empty = END.
        public string NotFoundExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var contextOff = new Offset<Tryll.NodeParams.ContextOverrides>((Context ?? TryllContextOverrides.Default).Pack(fbb));
            var adaptationOff = new Offset<Tryll.NodeParams.ModelAdaptation>((Adaptation ?? TryllModelAdaptation.Default).Pack(fbb));
            var model_nameOff = fbb.CreateString(ModelName ?? "");
            var system_promptOff = fbb.CreateString(SystemPrompt ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var output_nameOff = fbb.CreateString(OutputName ?? "");
            var not_found_intentOff = fbb.CreateString(NotFoundIntent ?? "");
            var found_exitOff = fbb.CreateString(FoundExit ?? "");
            var not_found_exitOff = fbb.CreateString(NotFoundExit ?? "");
            VectorOffset intentsVecOff = default;
            if (Intents != null && Intents.Length > 0)
            {
                var intentsOffs = new Offset<Tryll.NodeParams.ClassifyIntentEntry>[Intents.Length];
                for (int i = 0; i < Intents.Length; i++)
                    intentsOffs[i] = new Offset<Tryll.NodeParams.ClassifyIntentEntry>(Intents[i].Pack(fbb));
                intentsVecOff = Tryll.NodeParams.ClassifyIntentLLMParams.CreateIntentsVector(fbb, intentsOffs);
            }

            Tryll.NodeParams.ClassifyIntentLLMParams.StartClassifyIntentLLMParams(fbb);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddModelName(fbb, model_nameOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddContextSize(fbb, ContextSize);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddSystemPrompt(fbb, system_promptOff);
            if (Intents != null && Intents.Length > 0)
                Tryll.NodeParams.ClassifyIntentLLMParams.AddIntents(fbb, intentsVecOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddOutputName(fbb, output_nameOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddHistoryTurns(fbb, HistoryTurns);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddThreshold(fbb, Threshold);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddMargin(fbb, Margin);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddTemperature(fbb, Temperature);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddNotifyClient(fbb, (Tryll.NotifyClient)(byte)NotifyClient);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddNotFoundIntent(fbb, not_found_intentOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddFoundExit(fbb, found_exitOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddNotFoundExit(fbb, not_found_exitOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddContext(fbb, contextOff);
            Tryll.NodeParams.ClassifyIntentLLMParams.AddAdaptation(fbb, adaptationOff);
            var offset = Tryll.NodeParams.ClassifyIntentLLMParams.EndClassifyIntentLLMParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.ClassifyIntentLLMParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllClassifyIntentLLMParams)MemberwiseClone();
            if (Intents != null)
            {
                c.Intents = new TryllClassifyIntentEntry[Intents.Length];
                for (int i = 0; i < Intents.Length; i++)
                    c.Intents[i] = Intents[i]?.Clone();
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
