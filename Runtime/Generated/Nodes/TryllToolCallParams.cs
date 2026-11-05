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
    internal static class _FpCheck_TryllToolCallParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// LLM-driven tool-call node. Constructs a tool-call prompt, runs sampling, parses the result.
    /// </summary>
    [Serializable]
    [AddTypeMenu("ToolCall")]
    public sealed class TryllToolCallParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.ToolCall;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/tool-call.html";

        [TryllGroup("Input", First = true)]
        [Tooltip("Text the model reads to decide on a tool call: the player's message or an earlier node's output. Empty = the player's message.")]
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
        [Tooltip("Context window for this node, in tokens; must fit the model's trained maximum. 0 = 2048.")]
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
        [Tooltip("Tools the model may call, each with a name, description and parameters. The game carries out the call; the server only detects it.")]
        [TryllStructural]
        public TryllToolDefinition[] Tools;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("DetectOnly: call a tool or say nothing. CallOrAnswer: call a tool or answer in text. RequireCall: must call a tool.")]
        public TryllToolCallMode Mode = TryllToolCallMode.CallOrAnswer;

        [TryllGroup("Behavior")]
        [Tooltip("Allow several tool calls in one turn, if the model's chat template supports it.")]
        public bool ParallelToolCalls = false;

        [TryllGroup("Behavior")]
        [Tooltip("After a call: whether to tell the game (Notify), wait for it (Pause, AwaitResult), and record an acknowledgement in history (…Acknowledge).")]
        public TryllToolCallDisposition Disposition = TryllToolCallDisposition.Acknowledge;

        [TryllGroup("Behavior")]
        [Tooltip("Tool result recorded in history by the …Acknowledge dispositions. Empty = 'ok'.")]
        [TryllLocalizable(false)]
        // Optional string — null means "use server default".
        public string AcknowledgeText;
        // Client-only. Pack() still writes AcknowledgeText; the resolve step fills it from AcknowledgeTextTextId.
        [HideInInspector] public bool AcknowledgeTextLocalized;
        [HideInInspector] public string AcknowledgeTextTextId;

        [TryllGroup("Behavior")]
        [Tooltip("How long a pausing Disposition waits for the game, in ms. 0 = the server default (wait forever in released builds). On timeout the turn ends.")]
        [TryllStructural]
        [Min(0.0f)]
        public int PauseTimeoutMs = 0;

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
        [Tooltip("Node to run when the model calls a tool. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("tool_called")]
        // Graph exit "tool_called" — target node name; empty = END.
        public string ToolCalledExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run when no tool is called (under CallOrAnswer, after the text answer). Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("no_tool_called")]
        // Graph exit "no_tool_called" — target node name; empty = END.
        public string NoToolCalledExit;

        [HideInInspector]
        [Tooltip("Not used. Kept so older workflows still load.")]
        [TryllStructural]
        // Optional string — null means "use server default".
        public string ToolCallFormat;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var samplingOff = new Offset<Tryll.NodeParams.SamplingOverrides>((Sampling ?? TryllSamplingOverrides.Default).Pack(fbb));
            var contextOff = new Offset<Tryll.NodeParams.ContextOverrides>((Context ?? TryllContextOverrides.Default).Pack(fbb));
            var adaptationOff = new Offset<Tryll.NodeParams.ModelAdaptation>((Adaptation ?? TryllModelAdaptation.Default).Pack(fbb));
            var model_nameOff = fbb.CreateString(ModelName ?? "");
            var system_promptOff = fbb.CreateString(SystemPrompt ?? "");
            var inputOff = fbb.CreateString(Input ?? "");
            var tool_call_formatOff = fbb.CreateString(ToolCallFormat ?? "");
            var tool_called_exitOff = fbb.CreateString(ToolCalledExit ?? "");
            var no_tool_called_exitOff = fbb.CreateString(NoToolCalledExit ?? "");
            var acknowledge_textOff = fbb.CreateString(AcknowledgeText ?? "");
            VectorOffset toolsVecOff = default;
            if (Tools != null && Tools.Length > 0)
            {
                var toolsOffs = new Offset<Tryll.NodeParams.ToolDefinition>[Tools.Length];
                for (int i = 0; i < Tools.Length; i++)
                    toolsOffs[i] = new Offset<Tryll.NodeParams.ToolDefinition>(Tools[i].Pack(fbb));
                toolsVecOff = Tryll.NodeParams.ToolCallParams.CreateToolsVector(fbb, toolsOffs);
            }

            Tryll.NodeParams.ToolCallParams.StartToolCallParams(fbb);
            Tryll.NodeParams.ToolCallParams.AddModelName(fbb, model_nameOff);
            Tryll.NodeParams.ToolCallParams.AddContextSize(fbb, ContextSize);
            Tryll.NodeParams.ToolCallParams.AddSystemPrompt(fbb, system_promptOff);
            Tryll.NodeParams.ToolCallParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.ToolCallParams.AddToolCallFormat(fbb, tool_call_formatOff);
            if (Tools != null && Tools.Length > 0)
                Tryll.NodeParams.ToolCallParams.AddTools(fbb, toolsVecOff);
            Tryll.NodeParams.ToolCallParams.AddMode(fbb, (Tryll.ToolCallMode)(byte)Mode);
            Tryll.NodeParams.ToolCallParams.AddParallelToolCalls(fbb, ParallelToolCalls);
            Tryll.NodeParams.ToolCallParams.AddSampling(fbb, samplingOff);
            Tryll.NodeParams.ToolCallParams.AddToolCalledExit(fbb, tool_called_exitOff);
            Tryll.NodeParams.ToolCallParams.AddNoToolCalledExit(fbb, no_tool_called_exitOff);
            Tryll.NodeParams.ToolCallParams.AddDisposition(fbb, (Tryll.ToolCallDisposition)(byte)Disposition);
            Tryll.NodeParams.ToolCallParams.AddAcknowledgeText(fbb, acknowledge_textOff);
            Tryll.NodeParams.ToolCallParams.AddPauseTimeoutMs(fbb, PauseTimeoutMs);
            Tryll.NodeParams.ToolCallParams.AddContext(fbb, contextOff);
            Tryll.NodeParams.ToolCallParams.AddAdaptation(fbb, adaptationOff);
            var offset = Tryll.NodeParams.ToolCallParams.EndToolCallParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.ToolCallParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllToolCallParams)MemberwiseClone();
            if (Tools != null)
            {
                c.Tools = new TryllToolDefinition[Tools.Length];
                for (int i = 0; i < Tools.Length; i++)
                    c.Tools[i] = Tools[i]?.Clone();
            }
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
