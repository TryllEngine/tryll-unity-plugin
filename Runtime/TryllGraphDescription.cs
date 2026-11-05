// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Inference engine the session should use for model loading.
    /// Mirrors the FlatBuffers InferenceEngine enum in messages.fbs — ordinal values must stay in sync.
    /// </summary>
    public enum TryllInferenceEngine : byte
    {
        /// <summary>In-process mock engine used by tests. Performs no real inference.</summary>
        Mock        = 0,
        /// <summary>llama.cpp GGUF runtime. The default production engine.</summary>
        LlamaCpp    = 1,
        /// <summary>ONNX GenAI — reserved; not yet implemented.</summary>
        OnnxGenAI   = 2,
        /// <summary>Windows ML — reserved; not yet implemented.</summary>
        WindowsML   = 3,
        /// <summary>Intel OpenVINO — reserved; not yet implemented.</summary>
        OpenVino    = 4,
        /// <summary>NVIDIA TensorRT-LLM — reserved; not yet implemented.</summary>
        TensorRtLlm = 5,
        /// <summary>Sherpa-ONNX runtime — STT/TTS engine.</summary>
        SherpaOnnx  = 6,
        /// <summary>Bare ONNX Runtime — encoder-shaped graphs and the Chatterbox TTS family.</summary>
        OnnxRuntime = 7,
    }

    /// <summary>
    /// Workflow node types supported by the server.
    /// Mirrors the NodeParams union order in nodes.fbs (0-based; wire discriminator is +1).
    /// </summary>
    public enum TryllNodeType : byte
    {
        Generate              = 0,
        RegexGuardrail = 1,
        CannedResponse        = 2,
        ToolCall              = 3,
        Retrieve              = 4,
        Instruction           = 5,
        ClassifyIntent        = 6,
        IntentToInstruction   = 7,
        ClassifyIntentLLM     = 8,
        /// <summary>Generate text and stream synthesized speech (TTS) for the same answer.</summary>
        GenerateAndSpeak      = 9,
        /// <summary>TTS-only node — synthesizes speech for upstream/canned/inline text (no LLM).</summary>
        Speak                 = 10,
        /// <summary>No-op checkpoint — the executor pauses the turn after this node exits.</summary>
        Pause                 = 11,
        /// <summary>Model-free: renders a Mustache template into its own slot. No inference, no wire emission.</summary>
        Transform             = 12,
        /// <summary>Conditional routing on a slot or variable; then/else exits. No model, no wire emission.</summary>
        Branch                = 13,
        /// <summary>Experimental upsert into a named mutable knowledge base.</summary>
        WriteToMemory         = 14,
        /// <summary>Folds ageing turns into a rolling summary that Generate-class nodes render in their system message.</summary>
        Summarize             = 15,
        ClassifyAttributesLLM = 16,
    }

    /// <summary>Outcome of a workflow turn. Mirrors TurnStatus in messages.fbs.</summary>
    public enum TryllTurnStatus : byte
    {
        Success   = 0,
        Error     = 1,
        Cancelled = 2,
        /// <summary>
        /// A pause outlived its <c>pause_timeout_ms</c> budget and the pausing node
        /// authored no <c>timeout_exit</c>, so the server abandoned the turn. Nobody
        /// requested this — your game never called Resume — which is what makes it
        /// distinct from <see cref="Cancelled"/>. The partial interaction is kept.
        /// Subscribe to <c>TryllClient.Paused</c> (or RegisterTool a handler) so
        /// pauses get resumed.
        /// </summary>
        PauseTimedOut = 3,
    }

    /// <summary>How a mid-turn cancel treats the in-flight interaction. Mirrors CancelMode in messages.fbs.</summary>
    public enum TryllCancelMode : byte
    {
        /// <summary>Stop generating; keep the partial interaction in history (chat "Stop").</summary>
        StopAndKeep    = 0,
        /// <summary>Stop and discard the last interaction, as if the turn never happened.</summary>
        StopAndDiscard = 1,
    }

    // ── Placement and strategy enums (mirroring common_enums.fbs) ────────────

    /// <summary>
    /// Controls where the Mustache-rendered body is placed relative to the node's resolved
    /// <c>Input</c> in the projected message stream. There is no more <c>InPlaceOfUser</c> —
    /// use a <c>Transform</c> node + <c>Input</c> to fully replace what the model sees.
    /// </summary>
    public enum TryllPlacement : byte
    {
        BeforeUserAsUser   = 0,
        BeforeUserAsSystem = 1,
        AfterUserAsUser    = 2,
        AfterUserAsSystem  = 3,
    }

    /// <summary>Controls answer-text delivery to the client. Mirrors SendAnswer in common_enums.fbs.</summary>
    public enum TryllSendAnswer : byte
    {
        /// <summary>Captured into the node's slot only — never sent as AnswerText.</summary>
        None     = 0,
        /// <summary>One AnswerText frame, sent once the full text is ready.</summary>
        Whole    = 1,
        /// <summary>Per-delta AnswerText frames as text is generated.</summary>
        Streamed = 2,
    }

    /// <summary>Controls whether/how a node's slot replays as history on later turns. Mirrors HistoryRole in common_enums.fbs.</summary>
    public enum TryllHistoryRole : byte
    {
        /// <summary>Never replayed — a same-turn-only scratch value.</summary>
        None      = 0,
        /// <summary>Replayed as a separate assistant message on later turns.</summary>
        Assistant = 1,
    }

    /// <summary>Selection strategy for CannedResponseNode.</summary>
    public enum TryllCannedResponseSelectionStrategy : byte
    {
        Random     = 0,
        First      = 1,
        RoundRobin = 2,
    }

    /// <summary>Tool-choice policy for ToolCallNode. Mirrors ToolCallMode in common_enums.fbs.</summary>
    public enum TryllToolCallMode : byte
    {
        /// <summary>Model may call a tool or stay silent; no answer text when no call is parsed.</summary>
        DetectOnly   = 0,
        /// <summary>Same as DetectOnly, but residual text is emitted as a plain answer when no call is parsed.</summary>
        CallOrAnswer = 1,
        /// <summary>At least one tool call is mandatory; the model cannot produce plain text.</summary>
        RequireCall  = 2,
    }

    /// <summary>RetrieveNode search path. Mirrors RetrievalMode in common_enums.fbs.</summary>
    public enum TryllRetrievalMode : byte
    {
        /// <summary>HNSW cosine only (historical default).</summary>
        Dense   = 0,
        /// <summary>BM25 lexical only.</summary>
        Lexical = 1,
        /// <summary>Dense + BM25 fused with Reciprocal Rank Fusion.</summary>
        Hybrid  = 2,
    }

    /// <summary>How Retrieve addresses knowledge. Mirrors KnowledgeSourceKind in common_enums.fbs.</summary>
    public enum TryllKnowledgeSourceKind : byte
    {
        /// <summary>File-backed embedded string storage.</summary>
        FromFile = 0,
        /// <summary>Session-scoped named mutable store.</summary>
        FromName = 1,
    }

    /// <summary>End-to-end ToolCall notify / pause / history policy. Mirrors ToolCallDisposition in common_enums.fbs.</summary>
    public enum TryllToolCallDisposition : byte
    {
        /// <summary>No event, no pause; omit tool syntax from history.</summary>
        RouteOnly            = 0,
        /// <summary>No event, no pause; synthetic acknowledgement history. Schema default.</summary>
        Acknowledge          = 1,
        /// <summary>Emit tool_call event; no pause; omit from history.</summary>
        Notify               = 2,
        /// <summary>Emit tool_call event; no pause; synthetic acknowledgement history.</summary>
        NotifyAndAcknowledge = 3,
        /// <summary>Emit tool_call event and pause; omit from history.</summary>
        Pause                = 4,
        /// <summary>Emit tool_call event and pause; synthetic acknowledgement history.</summary>
        PauseAndAcknowledge  = 5,
        /// <summary>Emit tool_call event, pause, and require client Resume tool_results.</summary>
        AwaitResult          = 6,
    }

    /// <summary>When a classifier emits a NodeEvent. Mirrors NotifyClient in common_enums.fbs.</summary>
    public enum TryllNotifyClient : byte
    {
        /// <summary>Never emit a classification NodeEvent.</summary>
        Disabled = 0,
        /// <summary>Emit only on the found path (former notify_client=true).</summary>
        OnFound  = 1,
        /// <summary>Emit on found and every not_found path.</summary>
        Always   = 2,
    }

    /// <summary>llama.cpp KV cache dtype. Mirrors KvCacheType in common_enums.fbs.</summary>
    public enum TryllKvCacheType : byte
    {
        /// <summary>Use the model variant, then the engine q8_0 default.</summary>
        Inherit = 0,
        F16     = 1,
        Q8_0    = 2,
        Q4_0    = 3,
    }

    /// <summary>How ClassifyAttributesLLM turns a scored distribution into a slot value. Mirrors AttributeKind in common_enums.fbs.</summary>
    public enum TryllAttributeKind : byte
    {
        /// <summary>Winner choice id after threshold and margin.</summary>
        Choice = 0,
        /// <summary>One statement scored as A=True / B=False.</summary>
        Boolean = 1,
        /// <summary>Ordered levels normalised to 0..1.</summary>
        Score = 2,
    }

    /// <summary>Which Boolean result takes the triggered exit.</summary>
    public enum TryllBooleanTrigger : byte
    {
        Never = 0,
        True = 1,
        False = 2,
    }

    /// <summary>How a normalised Score compares with its threshold.</summary>
    public enum TryllScoreTrigger : byte
    {
        Never = 0,
        AtOrAbove = 1,
        Below = 2,
    }

    /// <summary>Exit taken when ClassifyAttributesLLM cannot score.</summary>
    public enum TryllAttributeFailureRoute : byte
    {
        Default = 0,
        Triggered = 1,
    }

    /// <summary>Predicate policy for BranchNode. Mirrors BranchTest in common_enums.fbs.</summary>
    public enum TryllBranchTest : byte
    {
        NonEmpty         = 0,
        IsTrue           = 1,
        Equals           = 2,
        EqualsIgnoreCase = 3,
        Contains         = 4,
        Regex            = 5,
        Expression       = 6,
    }

    /// <summary>
    /// Description of one workflow node sent to the server.
    /// Params is a typed <see cref="TryllNodeParamsBase"/> subclass — no more string KV bags.
    /// </summary>
    [Serializable]
    public class TryllNodeDescription
    {
        /// <summary>Graph-unique node name.</summary>
        [TryllNodeRename]
        public string Name;

        /// <summary>
        /// Typed node parameters.  Use [SerializeReference] in owning MonoBehaviour fields
        /// to allow Unity Inspector to show the concrete subclass.
        /// </summary>
        [SerializeReference]
        [SubclassSelector]
        public TryllNodeParamsBase Params;
    }

    /// <summary>
    /// Complete graph description: nodes and entry point. Sent inside CreateAgentRequest.
    ///
    /// Wiring lives on each node's typed <see cref="TryllNodeParamsBase"/>
    /// in fields marked <see cref="TryllExitAttribute"/> (one per declared exit).
    /// Empty value = END (turn finishes after the node).  There is no separate Routes list.
    /// </summary>
    [Serializable]
    public class TryllGraphDescription
    {
        public List<TryllNodeDescription> Nodes  = new List<TryllNodeDescription>();
        [TryllNodeName]
        public string                     StartNode;

        /// <summary>
        /// Fallback model name for nodes that do not specify their own model_name param.
        /// Empty means every Generate node must supply its own.
        /// </summary>
        [TryllModelType("language")]
        public string DefaultModelName;

        /// <summary>
        /// Text table name sent to <see cref="ITryllTextProvider.GetTextAsync"/>
        /// for every Localized field in this graph. Hidden: the workflow asset
        /// owns the visible field and copies it here so clones (Dialog Lab,
        /// per-agent copies) keep it.
        /// </summary>
        [HideInInspector]
        public string TextTable = "";

        /// <summary>
        /// Deep copy of this graph, including every node's params.
        ///
        /// <para><b>Required when one authored asset drives several agents.</b> A
        /// <see cref="TryllWorkflowAsset"/> holds a single graph instance; handing it to two agents and
        /// then customising per-agent fields would have them share the same params objects, so the last
        /// write would silently win for both. Clone first, then customise the copy.</para>
        ///
        /// <para>Nodes whose <see cref="TryllNodeDescription.Params"/> is null are copied as-is rather
        /// than throwing: an asset can legitimately be mid-edit in the Inspector.</para>
        /// </summary>
        public TryllGraphDescription Clone()
        {
            var copy = new TryllGraphDescription
            {
                StartNode        = StartNode,
                DefaultModelName = DefaultModelName,
                TextTable        = TextTable,
                Nodes            = new List<TryllNodeDescription>(Nodes?.Count ?? 0),
            };

            if (Nodes == null) return copy;

            foreach (var node in Nodes)
            {
                if (node == null) continue;
                copy.Nodes.Add(new TryllNodeDescription
                {
                    Name   = node.Name,
                    Params = node.Params?.Clone(),
                });
            }
            return copy;
        }

        /// <summary>
        /// Returns the params of the node called <paramref name="nodeName"/>, cast to
        /// <typeparamref name="T"/>, or null if there is no such node or its params are a different
        /// type.
        ///
        /// <para>Intended for the "author the graph in an asset, patch the per-agent fields in code"
        /// pattern: look the node up by the name a designer sees, and fail visibly if it was renamed or
        /// its type changed, rather than silently doing nothing.</para>
        /// </summary>
        public T FindParams<T>(string nodeName) where T : TryllNodeParamsBase
        {
            if (Nodes == null || string.IsNullOrEmpty(nodeName)) return null;
            foreach (var node in Nodes)
                if (node != null && node.Name == nodeName)
                    return node.Params as T;
            return null;
        }

        /// <summary>True when a node with this name exists, whatever its params type.</summary>
        public bool HasNode(string nodeName)
        {
            if (Nodes == null || string.IsNullOrEmpty(nodeName)) return false;
            foreach (var node in Nodes)
                if (node != null && node.Name == nodeName) return true;
            return false;
        }
    }
}
