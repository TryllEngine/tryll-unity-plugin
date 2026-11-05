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
    internal static class _FpCheck_TryllGenerateParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// LLM-driven text generation node. Renders a Mustache template, runs sampling, produces a streamed AnswerText.
    /// </summary>
    [Serializable]
    [AddTypeMenu("Generate")]
    public sealed class TryllGenerateParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Generate;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/generate.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text this node answers: the player's message or an earlier node's output. Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Model", First = true)]
        [Tooltip("Language model this node runs. Empty = the workflow's Default Model Name.")]
        [TryllStructural]
        [TryllModelType("language")]
        // Optional string — null means "use server default".
        public string ModelName;

        [TryllGroup("Model")]
        [Tooltip("Context window for this node, in tokens; must fit the model's trained maximum. 0 = the model's own size, else the server default.")]
        [TryllStructural]
        [Min(0.0f)]
        public int ContextSize = 0;

        [TryllGroup("Prompt", First = true)]
        [Tooltip("System message the model reads before the dialog. Empty = none.")]
        [TryllLocalizable(true)]
        // Optional string — null means "use server default".
        public string SystemPrompt;
        // Client-only. Pack() still writes SystemPrompt; the resolve step fills it from SystemPromptTextId.
        [HideInInspector] public bool SystemPromptLocalized;
        [HideInInspector] public string SystemPromptTextId;

        [TryllGroup("Prompt")]
        [Tooltip("Mustache text added next to the Input (where: see Placement), for example the slot.<name> tag or retrieved knowledge. Empty = none.")]
        [TryllLocalizable(true)]
        // Optional string — null means "use server default".
        public string Template;
        // Client-only. Pack() still writes Template; the resolve step fills it from TemplateTextId.
        [HideInInspector] public bool TemplateLocalized;
        [HideInInspector] public string TemplateTextId;

        [TryllGroup("Prompt")]
        [Tooltip("Where Template goes: before or after the Input, as a system or a user message.")]
        public TryllPlacement Placement = TryllPlacement.BeforeUserAsSystem;

        [TryllGroup("Prompt")]
        [Tooltip("GBNF grammar the answer must follow; needs a `root` rule. Empty = free text. Use it to force a format, such as JSON or a fixed set of lines.")]
        [TextArea(3, 10)]
        // Optional string — null means "use server default".
        public string Grammar;

        [TryllGroup("Output", First = true)]
        [Tooltip("Name later nodes use to read this node's answer (in their Input, or as the slot.<name> tag). Empty = this node's name.")]
        [TryllStructural]
        [TryllSlotName]
        // Optional string — null means "use server default".
        public string OutputName;

        [TryllGroup("Output")]
        [Tooltip("How the answer reaches the game: Streamed (token by token), Whole (once, at the end), or None (kept for later nodes only).")]
        public TryllSendAnswer Send = TryllSendAnswer.Streamed;

        [TryllGroup("Output")]
        [Tooltip("Whether later turns remember this answer as an assistant message. None = forgotten after this turn.")]
        public TryllHistoryRole HistoryRole = TryllHistoryRole.Assistant;

        [TryllGroup("Output")]
        [Tooltip("Replace __NAME__ markers in the answer with the values of agent variables that allow output substitution. Not case-sensitive.")]
        public bool SubstituteAgentVariables = false;

        [TryllGroup("Output")]
        [Tooltip("Removes roleplay artifacts from the answer: 'Bob:' prefixes, *actions*, (asides), [notes]. All off by default.")]
        public TryllOutputFilterOverrides OutputFilter = new TryllOutputFilterOverrides();

        [TryllGroup("Advanced", First = true)]
        [Tooltip("Overrides for the model's sampling settings (temperature, top-p, max tokens…). Unset values keep the model's own.")]
        public TryllSamplingOverrides Sampling = new TryllSamplingOverrides();

        [TryllGroup("Advanced")]
        [Tooltip("Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.")]
        [TryllStructural]
        public TryllContextOverrides Context = new TryllContextOverrides();

        [TryllGroup("Advanced")]
        [Tooltip("LoRA adapters applied to this node's model. Empty = the base model.")]
        [TryllStructural]
        public TryllModelAdaptation Adaptation = new TryllModelAdaptation();

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run after the answer is generated. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var samplingOff = new Offset<Tryll.NodeParams.SamplingOverrides>((Sampling ?? TryllSamplingOverrides.Default).Pack(fbb));
            var output_filterOff = new Offset<Tryll.NodeParams.OutputFilterOverrides>((OutputFilter ?? TryllOutputFilterOverrides.Default).Pack(fbb));
            var contextOff = new Offset<Tryll.NodeParams.ContextOverrides>((Context ?? TryllContextOverrides.Default).Pack(fbb));
            var adaptationOff = new Offset<Tryll.NodeParams.ModelAdaptation>((Adaptation ?? TryllModelAdaptation.Default).Pack(fbb));
            var model_nameOff = fbb.CreateString(ModelName ?? "");
            var system_promptOff = fbb.CreateString(SystemPrompt ?? "");
            var output_nameOff = fbb.CreateString(OutputName ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var templateOff = fbb.CreateString(Template ?? "");
            var grammarOff = fbb.CreateString(Grammar ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");

            Tryll.NodeParams.GenerateParams.StartGenerateParams(fbb);
            Tryll.NodeParams.GenerateParams.AddModelName(fbb, model_nameOff);
            Tryll.NodeParams.GenerateParams.AddContextSize(fbb, ContextSize);
            Tryll.NodeParams.GenerateParams.AddSystemPrompt(fbb, system_promptOff);
            Tryll.NodeParams.GenerateParams.AddOutputName(fbb, output_nameOff);
            Tryll.NodeParams.GenerateParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.GenerateParams.AddTemplate(fbb, templateOff);
            Tryll.NodeParams.GenerateParams.AddPlacement(fbb, (Tryll.Placement)(byte)Placement);
            Tryll.NodeParams.GenerateParams.AddSend(fbb, (Tryll.SendAnswer)(byte)Send);
            Tryll.NodeParams.GenerateParams.AddHistoryRole(fbb, (Tryll.HistoryRole)(byte)HistoryRole);
            Tryll.NodeParams.GenerateParams.AddSampling(fbb, samplingOff);
            Tryll.NodeParams.GenerateParams.AddGrammar(fbb, grammarOff);
            Tryll.NodeParams.GenerateParams.AddDefaultExit(fbb, default_exitOff);
            Tryll.NodeParams.GenerateParams.AddSubstituteAgentVariables(fbb, SubstituteAgentVariables);
            Tryll.NodeParams.GenerateParams.AddOutputFilter(fbb, output_filterOff);
            Tryll.NodeParams.GenerateParams.AddContext(fbb, contextOff);
            Tryll.NodeParams.GenerateParams.AddAdaptation(fbb, adaptationOff);
            var offset = Tryll.NodeParams.GenerateParams.EndGenerateParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.GenerateParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllGenerateParams)MemberwiseClone();
            c.Sampling = Sampling != null
                ? Sampling.Clone()
                : null;
            c.OutputFilter = OutputFilter != null
                ? OutputFilter.Clone()
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
