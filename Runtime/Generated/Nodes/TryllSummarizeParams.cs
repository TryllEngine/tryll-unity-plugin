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
    internal static class _FpCheck_TryllSummarizeParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Rolling conversation summary. Every time the history since the last fold crosses a trigger, this node compresses the older interactions (everything outside `keep_recent_turns`) into a SummaryComponent. Generate-class projection then renders that summary at the end of its system message and replays history only from `covered_through + 1`, so a long dialogue stays coherent at a fixed token cost instead of being trimmed away. Place it last in the graph, after the node that produced the player-facing reply.
    /// </summary>
    [Serializable]
    [AddTypeMenu("Summarize")]
    public sealed class TryllSummarizeParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Summarize;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/summarize.html";

        [TryllGroup("Model", First = true)]
        [Tooltip("Language model that writes the summaries. Empty = the workflow's Default Model Name. Prefer a plain instruct model over a roleplay fine-tune.")]
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
        [Tooltip("Instructions for the summarizer. Empty = the built-in default (third person, named speakers, no speculation).")]
        [TryllLocalizable(true)]
        // Optional string — null means "use server default".
        public string SystemPrompt;
        // Client-only. Pack() still writes SystemPrompt; the resolve step fills it from SystemPromptTextId.
        [HideInInspector] public bool SystemPromptLocalized;
        [HideInInspector] public string SystemPromptTextId;

        [TryllGroup("Prompt")]
        [Tooltip("Request sent to the summarizer. Can use the tags previous_summary, #turns (index, user, assistant) and max_summary_tokens. Empty = built-in.")]
        [TryllLocalizable(true)]
        // Optional string — null means "use server default".
        public string Template;
        // Client-only. Pack() still writes Template; the resolve step fills it from TemplateTextId.
        [HideInInspector] public bool TemplateLocalized;
        [HideInInspector] public string TemplateTextId;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Summarize when unsummarized history reaches this many tokens. Keep it below the point where the reply node trims history. 0 = off.")]
        [Min(0.0f)]
        public int TriggerTokens = 1024;

        [TryllGroup("Behavior")]
        [Tooltip("Summarize every N turns. 0 = off. With both triggers off, the node does nothing.")]
        [Min(0.0f)]
        public int TriggerTurns = 0;

        [TryllGroup("Behavior")]
        [Tooltip("Latest turns never summarized; they are always replayed word for word. Max 512.")]
        [Range(0.0f, 512.0f)]
        public int KeepRecentTurns = 3;

        [TryllGroup("Behavior")]
        [Tooltip("Longest summary, in tokens. Must leave at least 64 tokens of Context Size free.")]
        [Min(16.0f)]
        public int MaxSummaryTokens = 256;

        [TryllGroup("Output", First = true)]
        [Tooltip("Send the game a summarized event after each summary.")]
        public bool NotifyClient = false;

        [TryllGroup("Advanced", First = true)]
        [Tooltip("Overrides for the model's sampling settings (temperature, top-p, max tokens…). Unset values keep the model's own.")]
        public TryllSamplingOverrides Sampling = new TryllSamplingOverrides();

        [TryllGroup("Advanced")]
        [Tooltip("Must stay on: summarize inside the turn that triggered it, which adds the summary time to that turn. Background mode is not available yet.")]
        public bool RunInline = true;

        [TryllGroup("Advanced")]
        [Tooltip("Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.")]
        [TryllStructural]
        public TryllContextOverrides Context = new TryllContextOverrides();

        [TryllGroup("Advanced")]
        [Tooltip("LoRA adapters applied to this node's model. Empty = the base model.")]
        [TryllStructural]
        public TryllModelAdaptation Adaptation = new TryllModelAdaptation();

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run next, whether or not a summary was written. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var samplingOff = new Offset<Tryll.NodeParams.SamplingOverrides>((Sampling ?? TryllSamplingOverrides.Default).Pack(fbb));
            var contextOff = new Offset<Tryll.NodeParams.ContextOverrides>((Context ?? TryllContextOverrides.Default).Pack(fbb));
            var adaptationOff = new Offset<Tryll.NodeParams.ModelAdaptation>((Adaptation ?? TryllModelAdaptation.Default).Pack(fbb));
            var model_nameOff = fbb.CreateString(ModelName ?? "");
            var system_promptOff = fbb.CreateString(SystemPrompt ?? "");
            var templateOff = fbb.CreateString(Template ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");

            Tryll.NodeParams.SummarizeParams.StartSummarizeParams(fbb);
            Tryll.NodeParams.SummarizeParams.AddModelName(fbb, model_nameOff);
            Tryll.NodeParams.SummarizeParams.AddContextSize(fbb, ContextSize);
            Tryll.NodeParams.SummarizeParams.AddSystemPrompt(fbb, system_promptOff);
            Tryll.NodeParams.SummarizeParams.AddTemplate(fbb, templateOff);
            Tryll.NodeParams.SummarizeParams.AddTriggerTokens(fbb, TriggerTokens);
            Tryll.NodeParams.SummarizeParams.AddTriggerTurns(fbb, TriggerTurns);
            Tryll.NodeParams.SummarizeParams.AddKeepRecentTurns(fbb, KeepRecentTurns);
            Tryll.NodeParams.SummarizeParams.AddMaxSummaryTokens(fbb, MaxSummaryTokens);
            Tryll.NodeParams.SummarizeParams.AddSampling(fbb, samplingOff);
            Tryll.NodeParams.SummarizeParams.AddNotifyClient(fbb, NotifyClient);
            Tryll.NodeParams.SummarizeParams.AddRunInline(fbb, RunInline);
            Tryll.NodeParams.SummarizeParams.AddDefaultExit(fbb, default_exitOff);
            Tryll.NodeParams.SummarizeParams.AddContext(fbb, contextOff);
            Tryll.NodeParams.SummarizeParams.AddAdaptation(fbb, adaptationOff);
            var offset = Tryll.NodeParams.SummarizeParams.EndSummarizeParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.SummarizeParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllSummarizeParams)MemberwiseClone();
            c.Sampling = Sampling != null
                ? Sampling.Clone()
                : null;
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
