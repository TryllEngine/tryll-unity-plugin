// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.FlatBuffers;
using Tryll;
using Tryll.Client.Internal;
using UnityEngine;  // Debug.LogWarning (unresumable-pause diagnostic)

namespace Tryll.Client
{
    /// <summary>
    /// Handle for a server-side agent.
    /// Created by <see cref="TryllClient"/> after a successful CreateAgentResponse.
    ///
    /// Mirrors FTryllAgent from the Unreal plugin.
    ///
    /// Ownership
    /// ---------
    /// When obtained via <see cref="TryllAgentComponent"/>, the component owns this agent and
    /// disposes it automatically in <c>OnDisable</c>/<c>OnDestroy</c>.
    ///
    /// When obtained by calling <see cref="TryllClient.RequestCreateAgentAsync"/> directly,
    /// <strong>the caller owns the agent</strong> and must call <see cref="Dispose"/> (or use
    /// a <c>using</c> statement).
    ///
    /// WARNING: letting a TryllAgent be garbage-collected without calling <see cref="Dispose"/>
    /// will leak the server-side agent until the session closes.
    ///
    /// Callbacks
    /// ---------
    /// All callbacks are invoked on the Unity main thread (from <see cref="TryllClient.Update"/>).
    /// </summary>
    public sealed class TryllAgent : IDisposable, ITryllLocalizedTarget
    {
        private readonly TryllClient _client;
        private ulong _agentId;
        private bool  _disposed;

        /// <summary>
        /// Authored params baseline per node name — updated after each successful
        /// CreateAgent and ChangeParams call.  Used by GetNodeParamsBaseline.
        /// </summary>
        private readonly Dictionary<string, TryllNodeParamsBase> _paramsBaseline
            = new Dictionary<string, TryllNodeParamsBase>();

        // Where ticked Text IDs on this agent's params are looked up.
        private string _textTable = "";
        private string _textAssetName = "workflow";

        // In-flight lifecycle RPCs owned by this agent. SendMessage is not counted.
        private int _inflightLifecycle;

        private readonly Dictionary<string, Func<TryllToolCall, Task<string>>> _toolHandlers
            = new Dictionary<string, Func<TryllToolCall, Task<string>>>();
        private int _pausedBatchGeneration;
        private readonly List<TryllToolCall> _pendingToolCalls
            = new List<TryllToolCall>();
        // True once any call in _pendingToolCalls reached a tool-call listener
        // (agent or client scope). That listener can answer with
        // ResumeWithToolResult(s), so the pause that surfaces the batch is
        // resumable without a Paused subscriber. Consumed by that pause.
        private bool _pendingBatchObserved;

        // Retained paused-call batch: the calls surfaced by the current pause, kept
        // alive through handler execution and the resume Ack (not just until handlers
        // start running). Cleared only on a successful resume Ack, turn end/cancel,
        // Dispose, or a terminal disconnect — never merely because a handler ran.
        private readonly List<TryllToolCall> _pausedToolCalls
            = new List<TryllToolCall>();
        // Results already produced by a handler for this batch, keyed by call id.
        // Lets RetryPausedToolBatch skip handlers that already succeeded instead of
        // re-running them (and their side effects) after a partial failure.
        private readonly Dictionary<string, TryllToolResult> _pausedToolResults
            = new Dictionary<string, TryllToolResult>();

        internal bool CreatedWithDiagnostics { get; }

        /// <summary>
        /// Optional editor/display label (e.g. hosting GameObject name). Not on the wire.
        /// </summary>
        public string DisplayName { get; set; }
        internal bool CreatedBeforeDiagnosticsCapture { get; set; }

        /// <summary>Local mirror of this agent's declared per-agent Variables.</summary>
        public TryllAgentVariables Variables { get; }

        /// <summary>
        /// Delivery mode last accepted by the server. Empty means the voice profile's
        /// built-in neutral delivery.
        /// </summary>
        public string DeliveryMode { get; private set; }

        /// <summary>The voice last accepted by the server, or null for an unvoiced agent.</summary>
        public TryllVoiceProfileData Voice { get; private set; }

        // Last-writer-wins delivery callbacks. Register via SetOn*; pass null to clear.
        private Action<string, string, bool, bool>      OnAnswerText;     // (nodeName, text, isDelta, isFinal)
        private Action<TryllTurnStatus, string, int>    OnTurnComplete;   // (status, debugInfo, tokensGenerated)
        private Action<TryllError>                      OnError;

        /// <summary>
        /// Fired on the main thread when RegisterTool handler execution or its
        /// automatic result-bearing Resume fails. The turn remains paused.
        /// </summary>
        public event Action<TryllError> ToolHandlerError;

        /// <summary>This agent's tool call. Args: (toolName, argumentsJson).</summary>
        public event Action<string, string> ToolCallNotification;

        /// <summary>This agent's tool call with its call id. Args: (callId, toolName, argumentsJson).</summary>
        public event Action<string, string, string> ToolCallNotificationWithId;

        /// <summary>
        /// This agent's detailed tool call (node and disposition). Fired in addition to
        /// <see cref="ToolCallNotification"/> and <see cref="ToolCallNotificationWithId"/>.
        /// A subscriber suppresses <see cref="NodeEvent"/> for that event on this agent only.
        /// </summary>
        public event Action<TryllToolCall> ToolCallDetailed;

        /// <summary>Embedding ClassifyIntent result for this agent. The payload has no agent id.</summary>
        public event Action<TryllIntentClassification> IntentClassified;

        /// <summary>ClassifyIntentLLM result for this agent. The payload has no agent id.</summary>
        public event Action<TryllIntentLlmClassification> IntentLlmClassified;

        /// <summary>
        /// This agent's turn paused between nodes. Args: (nodeName, pendingExit).
        /// Subscribe and call <see cref="Resume"/>, or <see cref="RegisterTool"/> a handler
        /// for every tool in the batch. <see cref="TryllClient.Paused"/> remains the
        /// all-agents surface.
        /// </summary>
        public event Action<string, string> Paused;

        /// <summary>
        /// Generic NodeEvent for this agent, when no typed listener is bound here.
        /// Args: (nodeName, eventType, kvPairs). A listener on <see cref="TryllClient.NodeEvent"/>
        /// does not suppress this, and the reverse.
        /// </summary>
        public event Action<string, string, IReadOnlyList<KeyValuePair<string, string>>> NodeEvent;

        /// <summary>
        /// Fired on the Unity main thread for each <c>TtsVisemeFrame</c>. Additive
        /// multicast — subscribe with <c>+=</c>. The array is owned by this
        /// invocation; copy it if you need to retain events past the callback.
        /// </summary>
        public event Action<string, TryllVisemeEvent[]> OnTtsVisemes;

        // TTS audio. End-of-stream is signalled by TurnComplete (TtsAudioFrame carries no is_final).
        // OnTtsAudio receives a read-only view; the agent returns the underlying buffer to
        // ArrayPool<short>.Shared after the callback returns. nodeName is the producing
        // node's name (multi-sender attribution — mirrors SetOnAnswerText).
        private Action<string, int, int, int>            OnTtsAudioFormat; // (nodeName, sampleRate, channels, bps)
        private Action<string, ArraySegment<short>>      OnTtsAudio;       // (nodeName, pcm)

        // ── Constructor / Dispose ─────────────────────────────────────────────

        internal TryllAgent(TryllClient client, ulong agentId, List<TryllVariableDecl> declaredVariables = null,
            bool createdWithDiagnostics = false, TryllVoiceProfileData voice = null)
        {
            _client  = client;
            _agentId = agentId;
            CreatedWithDiagnostics = createdWithDiagnostics;
            Variables = new TryllAgentVariables(this, declaredVariables);
            Voice = voice?.Clone();
            DeliveryMode = string.Equals(Voice?.DefaultDeliveryMode, TryllVoiceProfileData.NeutralMode,
                StringComparison.Ordinal) ? string.Empty : Voice?.DefaultDeliveryMode ?? string.Empty;
        }

        /// <summary>
        /// Sends DestroyAgentRequest to the server and releases local state.
        /// Safe to call multiple times — subsequent calls are no-ops.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _client?.Connection.FailOwnedBy(this, new TryllError(TryllErrorCode.InvalidAgentId, "Agent destroyed"));
            if (_agentId != 0)
            {
                _client?.RequestDestroyAgentInternal(this);
                _agentId = 0;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public ulong AgentId  => _agentId;
        public bool  IsValid  => !_disposed && _agentId != 0;

        // ── Callback setters ─────────────────────────────────────────────────

        /// <summary>
        /// Register (or clear) the streaming-text callback. Replaces any previously
        /// set callback; pass <c>null</c> to unregister. Invoked on the Unity main
        /// thread for every <c>AnswerText</c> frame.
        /// Arguments: <c>(nodeName, text, isDelta, isFinal)</c>.
        /// </summary>
        public void SetOnAnswerText(Action<string, string, bool, bool> callback)
            => OnAnswerText = callback;

        /// <summary>
        /// Register (or clear) the turn-complete callback. Replaces any previously
        /// set callback; pass <c>null</c> to unregister. Invoked on the Unity main
        /// thread once per turn after the last answer chunk.
        /// Arguments: <c>(status, debugInfo, tokensGenerated)</c>.
        /// </summary>
        public void SetOnTurnComplete(Action<TryllTurnStatus, string, int> callback)
            => OnTurnComplete = callback;

        /// <summary>
        /// Register (or clear) the agent-level error callback. Replaces any
        /// previously set callback; pass <c>null</c> to unregister. Invoked on
        /// the Unity main thread. Does not fire for
        /// <see cref="TryllTurnStatus.Error"/> turns (those arrive via
        /// <see cref="SetOnTurnComplete"/>).
        /// </summary>
        public void SetOnError(Action<TryllError> callback)
            => OnError = callback;

        /// <summary>
        /// Register (or clear) the TTS format callback. Replaces any previously
        /// set callback; pass <c>null</c> to unregister. Fires once per turn,
        /// before any <see cref="SetOnTtsAudio"/> chunk. Arguments:
        /// <c>(nodeName, sampleRate, channels, bitsPerSample)</c>.
        /// </summary>
        public void SetOnTtsAudioFormat(Action<string, int, int, int> callback)
            => OnTtsAudioFormat = callback;

        /// <summary>
        /// Register (or clear) the streaming TTS audio callback. Replaces any
        /// previously set callback; pass <c>null</c> to unregister. The
        /// <see cref="ArraySegment{T}"/> is valid only for the duration of the
        /// callback; the agent returns the buffer to the pool afterwards.
        /// End-of-stream is signalled by <see cref="SetOnTurnComplete"/>.
        /// Arguments: <c>(nodeName, pcm)</c>.
        /// </summary>
        public void SetOnTtsAudio(Action<string, ArraySegment<short>> callback)
            => OnTtsAudio = callback;

        /// <summary>
        /// Send a text message to the agent.
        /// Streaming response arrives via <see cref="SetOnAnswerText"/>; turn ends via <see cref="SetOnTurnComplete"/>.
        /// </summary>
        public void SendMessage(string text)
        {
            if (!IsValid) return;
            if (string.IsNullOrEmpty(text)) return;
            var connection = _client.Connection;
            var rid = connection.NextRequestId();
            var agentId = _agentId;
            Variables.FlushIfDirty(carrierRequestId: rid);
            var frame = ClientCodec.BuildSendMessageRequest(rid, agentId, text);
            var accumulated = "";
            connection.Call(rid, frame, this, (msg, err) =>
            {
                if (!err.IsOk || msg.ByteBuffer == null)
                {
                    var error = err.IsOk
                        ? new TryllError(TryllErrorCode.MalformedMessage, "unexpected reply")
                        : err;
                    Deliver(connection, () =>
                    {
                        _client.NoteInbound(TryllObservationNote.Reply(MessageBody.SendMessageRequest, false, rid).Fill(n => { n.Message = error.Message; n.AgentId = agentId; n.Code = (uint)error.Code; }));
                        HandleError(error);
                    });
                    return true;
                }
                if (msg.BodyType == MessageBody.AnswerText)
                {
                    var body = msg.BodyAsAnswerText();
                    var chunk = body.Text ?? "";
                    var node = body.NodeName ?? "";
                    var isDelta = body.IsDelta;
                    var isFinal = body.IsFinal;
                    var answerAgentId = body.AgentId;
                    if (isDelta)
                        accumulated += chunk;
                    else
                        accumulated = chunk;
                    Deliver(connection, () =>
                    {
                        _client.NoteInbound(TryllObservationNote.Push(MessageBody.AnswerText, MessageBody.SendMessageRequest).Fill(n => { n.RequestId = rid; n.Text = chunk; n.Message = node; n.AgentId = answerAgentId; n.Flag = isDelta; n.Flag2 = isFinal; n.NodeName = node; }));
                        if (_client.TryGetAgent(answerAgentId, out var agent))
                            agent.HandleAnswerText(node, chunk, isDelta, isFinal);
                    });
                    return false;
                }
                if (msg.BodyType == MessageBody.TurnComplete)
                {
                    var body = msg.BodyAsTurnComplete();
                    var status = (TryllTurnStatus)body.Status;
                    var debug = body.DebugInfo ?? "";
                    var tokens = (int)body.TokensGenerated;
                    var turnAgentId = body.AgentId;
                    _ = accumulated;
                    Deliver(connection, () =>
                    {
                        _client.NoteInbound(TryllObservationNote.Reply(MessageBody.SendMessageRequest, true, rid).Fill(n => { n.Text = debug; n.AgentId = turnAgentId; n.TurnStatus = (byte)status; n.TokensGenerated = (uint)tokens; }));
                        if (_client.TryGetAgent(turnAgentId, out var agent))
                            agent.HandleTurnComplete(status, debug, tokens);
                    });
                    return true;
                }
                if (msg.BodyType == MessageBody.ErrorResponse)
                {
                    var body = msg.BodyAsErrorResponse();
                    var error = new TryllError(body.Code, body.Message);
                    var errorAgentId = body.AgentId != 0 ? body.AgentId : agentId;
                    Deliver(connection, () =>
                    {
                        _client.NoteInbound(TryllObservationNote.Reply(MessageBody.SendMessageRequest, false, rid).Fill(n => { n.Message = error.Message; n.AgentId = errorAgentId; n.Code = (uint)error.Code; }));
                        if (_client.TryGetAgent(errorAgentId, out var agent))
                            agent.HandleError(error);
                    });
                    // AgentBusy is the reply: the server sends no TurnComplete.
                    // Any other error is followed by TurnComplete, which is what
                    // ends the turn. Consuming the entry here would drop it.
                    return error.Code == (int)TryllErrorCode.AgentBusy;
                }
                var malformed = new TryllError(TryllErrorCode.MalformedMessage, "unexpected reply");
                Deliver(connection, () =>
                {
                    _client.NoteLocalError(rid, agentId, (uint)malformed.Code, "MalformedMessage", malformed.Message, LogErrorOrigin.unexpected_reply);
                    HandleError(malformed);
                });
                return true;
            }, MessageBody.SendMessageRequest, requestId => NoteAgent(
                MessageBody.SendMessageRequest, requestId, flush: false, fill: n => n.Text = text));
        }

        /// <summary>
        /// Cooperatively cancel the agent's in-flight turn. The turn ends with
        /// <see cref="TryllTurnStatus.Cancelled"/> via <see cref="SetOnTurnComplete"/>.
        /// No-op if the agent is idle. <paramref name="mode"/> selects whether the
        /// partial interaction is kept (default) or discarded.
        /// </summary>
        public void Cancel(TryllCancelMode mode = TryllCancelMode.StopAndKeep)
        {
            if (!IsValid) return;
            var agentId = _agentId;
            TryllCalls.Cancel(
                _client.Connection, this, agentId, (CancelMode)mode,
                _ => { },
                rid => NoteAgent(MessageBody.CancelRequest, rid, flush: false, fill: n => n.TurnStatus = (byte)mode));
        }

        /// <summary>
        /// Resume a turn that is currently paused (see <see cref="TryllClient.Paused"/>).
        /// </summary>
        /// <param name="resumeNode">
        /// Empty continues via the paused node's pending exit route; non-empty jumps
        /// to that node by name (must exist in the agent's graph).
        /// </param>
        /// <param name="onComplete">
        /// Called on the main thread with a <see cref="TryllError"/> value. Check
        /// <see cref="TryllError.IsOk"/> — success is not a null error
        /// (<see cref="TryllError"/> is a value type). Failures include
        /// <see cref="TryllErrorCode.AgentNotPaused"/> if the agent is not currently
        /// paused, and <see cref="TryllErrorCode.UnknownNode"/> if
        /// <paramref name="resumeNode"/> does not name a node in the graph.
        /// </param>
        public void Resume(string resumeNode = "", Action<TryllError> onComplete = null)
            => ResumeInternal(resumeNode, null, onComplete);

        /// <summary>Resume with one result correlated to a tool-call ID.</summary>
        public void ResumeWithToolResult(
            TryllToolResult toolResult,
            string resumeNode = "",
            Action<TryllError> onComplete = null)
        {
            var results = toolResult == null
                ? new List<TryllToolResult>()
                : new List<TryllToolResult> { toolResult };
            ResumeWithToolResults(results, resumeNode, onComplete);
        }

        /// <summary>
        /// Resume with the complete result batch for one paused tool-call pass.
        /// </summary>
        public void ResumeWithToolResults(
            IReadOnlyList<TryllToolResult> toolResults,
            string resumeNode = "",
            Action<TryllError> onComplete = null)
        {
            var wireResults = new List<Tryll.ToolResultT>();
            if (toolResults != null)
            {
                foreach (var toolResult in toolResults)
                {
                    if (toolResult == null) continue;
                    wireResults.Add(new Tryll.ToolResultT
                    {
                        CallId = toolResult.CallId,
                        Result = toolResult.Result,
                    });
                }
            }
            ResumeInternal(resumeNode, wireResults, onComplete);
        }

        private void ResumeInternal(
            string resumeNode,
            List<Tryll.ToolResultT> toolResults,
            Action<TryllError> onComplete)
        {
            if (!IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return;
            }
            var node = resumeNode ?? "";
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.ResumeAgent(
                _client.Connection, this,
                rid => ClientCodec.BuildResumeAgentRequest(rid, agentId, node, toolResults),
                err => FinishAck(err, onComplete, globalError: true, MessageBody.ResumeAgentRequest, notedId,
                    onSuccess: () => PausedNode = ""),
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.ResumeAgentRequest, rid, flush: true, fill: n => n.Text = node);
                }));
        }

        /// <summary>
        /// Register main-thread sugar for a named tool. A complete paused batch
        /// auto-resumes only when every call in that batch has a registered
        /// handler and every handler returns successfully.
        /// </summary>
        public void RegisterTool(string toolName, Func<TryllToolCall, string> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            RegisterTool(toolName, call => Task.FromResult(handler(call)));
        }

        /// <summary>
        /// Register an async tool handler. The paused batch is retained until every
        /// handler completes on the main thread and the result-bearing Resume is acked.
        /// </summary>
        public void RegisterTool(string toolName, Func<TryllToolCall, Task<string>> handler)
        {
            if (string.IsNullOrEmpty(toolName))
                throw new ArgumentException("Tool name must not be empty", nameof(toolName));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            _toolHandlers[toolName] = handler;
        }

        /// <summary>Remove a previously registered tool handler.</summary>
        public bool UnregisterTool(string toolName) => _toolHandlers.Remove(toolName ?? "");

        /// <summary>
        /// True while a paused tool-call batch is retained: surfaced by the current
        /// pause and not yet cleared by a successful resume Ack, turn end/cancel,
        /// Dispose, or a terminal disconnect. A retained batch may already have some
        /// handlers succeeded (see <see cref="RetryPausedToolBatch"/>).
        /// </summary>
        public bool HasPausedToolBatch => _pausedToolCalls.Count > 0;

        /// <summary>
        /// Name of the node this agent's turn is paused after, or an empty string
        /// when it is not paused. Non-empty means the server is holding the turn
        /// open until Resume (or Cancel) — <see cref="SendMessage"/> keeps failing
        /// with AgentBusy until one of those arrives. Covers Pause nodes as well as
        /// pausing tool calls, unlike <see cref="HasPausedToolBatch"/>.
        /// </summary>
        public string PausedNode { get; private set; } = "";

        /// <summary>
        /// True when this handle has no in-flight lifecycle RPC and is not paused.
        /// SendMessage in-flight is tracked by the caller (e.g. CharacterInstance.IsTurnInFlight).
        /// </summary>
        public bool IsLifecycleIdle =>
            string.IsNullOrEmpty(PausedNode)
            && !HasPausedToolBatch
            && _inflightLifecycle == 0
            && _pendingToolCalls.Count == 0;

        /// <summary>
        /// Re-attempt the retained paused tool-call batch: handlers that already
        /// produced a result on a previous attempt are skipped (not re-run — no
        /// duplicate side effects), remaining handlers run, and a fresh Resume is
        /// sent. No-op if there is no retained batch (<see cref="HasPausedToolBatch"/>
        /// is false). Use after <see cref="ToolHandlerError"/> reports a failed
        /// handler or a rejected/disconnected resume.
        /// </summary>
        public void RetryPausedToolBatch()
        {
            if (_pausedToolCalls.Count == 0) return;
            RunPausedBatch();
        }

        /// <summary>
        /// Apply typed node parameters to a workflow node at runtime.
        /// Allowed while the agent is idle or paused (rejected with
        /// <see cref="TryllErrorCode.AgentBusy"/> only while a turn is actively
        /// running between pauses).
        ///
        /// Clone-set-send idiom:
        /// <code>
        ///   var p = (TryllGenerateParams)agent.GetNodeParamsBaseline("gen");
        ///   p.SystemPrompt = "New system prompt";
        ///   agent.ChangeParams("gen", p, err => { if (!err.IsOk) Debug.LogError(err); });
        /// </code>
        /// </summary>
        /// <param name="nodeName">Instance name of the target node.</param>
        /// <param name="params">Typed node parameters (must match the node's concrete type).</param>
        /// <param name="onComplete">
        /// Called on the main thread with a <see cref="TryllError"/> value. Check
        /// <see cref="TryllError.IsOk"/> — success is not a null error
        /// (<see cref="TryllError"/> is a value type).
        /// </param>
        public void ChangeParams(
            string nodeName,
            TryllNodeParamsBase @params,
            Action<TryllError> onComplete = null)
        {
            if (!IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return;
            }
            // A ticked Text ID is resolved first, on a copy, with the same
            // fail-fast rules as CreateAgent. An async provider can delay the
            // send by a frame.
            if (TryllTextResolver.HasTickedFields(@params))
            {
                _ = ChangeLocalizedParamsAsync(nodeName, @params, onComplete);
                return;
            }
            SendChangeParams(nodeName, @params, onComplete, globalError: true);
        }

        private async Task ChangeLocalizedParamsAsync(
            string nodeName, TryllNodeParamsBase @params, Action<TryllError> onComplete)
        {
            var copy = @params.Clone();
            var graph = new TryllGraphDescription
            {
                TextTable = _textTable,
                Nodes = new List<TryllNodeDescription>
                {
                    new TryllNodeDescription { Name = nodeName, Params = copy },
                },
            };
            TryllTextResolveResult resolved;
            try
            {
                resolved = await TryllTextResolver.ResolveAsync(
                    graph, _client.TextProvider, _textAssetName, liveOnly: false);
            }
            catch (Exception ex)
            {
                resolved = new TryllTextResolveResult(
                    new TryllError(TryllErrorCode.InvalidParamValue,
                        $"Localized text: provider failed: {ex.GetBaseException().Message}"),
                    true, null);
            }
            if (!resolved.Error.IsOk)
            {
                onComplete?.Invoke(resolved.Error);
                _client.RaiseError(resolved.Error);
                return;
            }
            if (!IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return;
            }
            try
            {
                SendChangeParams(nodeName, copy, onComplete, globalError: true);
            }
            catch (Exception ex)
            {
                // Nobody awaits this task, so a throw would be lost.
                var error = new TryllError(TryllErrorCode.InvalidParamValue, ex.Message);
                onComplete?.Invoke(error);
                _client.RaiseError(error);
            }
        }

        private void SendChangeParams(
            string nodeName,
            TryllNodeParamsBase @params,
            Action<TryllError> onComplete,
            bool globalError)
        {
            if (!IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return;
            }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.ChangeAgentParam(
                _client.Connection, this,
                rid => ClientCodec.BuildChangeAgentParamsRequest(rid, agentId, nodeName, @params),
                err => FinishAck(err, onComplete, globalError, MessageBody.ChangeAgentParamRequest, notedId, onSuccess: () =>
                {
                    if (@params != null)
                        SetNodeParamsBaseline(nodeName, @params.Clone());
                }),
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.ChangeAgentParamRequest, rid, flush: true, fill: n => n.NodeName = nodeName);
                }));
        }

        // ── Voice (protocol v27) ──────────────────────────────────────────────

        /// <summary>
        /// Replace this agent's voice. Admitted only while the agent is idle (or paused
        /// before Resume); a running turn rejects it with <see cref="TryllErrorCode.AgentBusy"/>.
        /// A profile the loaded model cannot use is rejected with
        /// <see cref="TryllErrorCode.InvalidVoiceProfile"/> and the old voice stays. The active
        /// delivery mode survives when the new profile defines a mode of the same name;
        /// otherwise the new profile's default mode applies.
        /// </summary>
        /// <returns>The server's verdict; check <see cref="TryllError.IsOk"/>.</returns>
        public Task<TryllError> SetVoiceAsync(TryllVoiceProfileData voice)
        {
            if (voice == null)
                return Task.FromResult(new TryllError(TryllErrorCode.InvalidVoiceProfile, "Voice profile is null"));
            return SetVoiceAndTrackAsync(voice);
        }

        private async Task<TryllError> SetVoiceAndTrackAsync(TryllVoiceProfileData voice)
        {
            var error = await SetVoiceAsync(voice.ToWire());
            if (!error.IsOk)
                return error;

            var previousMode = DeliveryMode;
            Voice = voice.Clone();
            if (!string.IsNullOrEmpty(previousMode) &&
                !string.Equals(previousMode, TryllVoiceProfileData.NeutralMode, StringComparison.Ordinal) &&
                (Voice.DeliveryModes == null ||
                 !Voice.DeliveryModes.Exists(m => m != null && m.Name == previousMode)))
            {
                DeliveryMode = string.Equals(Voice.DefaultDeliveryMode, TryllVoiceProfileData.NeutralMode,
                    StringComparison.Ordinal) ? string.Empty : Voice.DefaultDeliveryMode ?? string.Empty;
            }
            return error;
        }

        /// <summary>Wire-form overload of <see cref="SetVoiceAsync(TryllVoiceProfileData)"/>.</summary>
        public Task<TryllError> SetVoiceAsync(Tryll.VoiceProfileT voice)
        {
            var tcs = new TaskCompletionSource<TryllError>();
            if (!IsValid)
            {
                tcs.TrySetResult(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return tcs.Task;
            }
            if (voice == null)
            {
                tcs.TrySetResult(new TryllError(TryllErrorCode.InvalidVoiceProfile, "Voice profile is null"));
                return tcs.Task;
            }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.SetAgentVoice(
                _client.Connection, this, agentId, voice,
                err => FinishAck(err, e => tcs.TrySetResult(e), globalError: false,
                    MessageBody.SetAgentVoiceRequest, notedId),
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.SetAgentVoiceRequest, rid, flush: true);
                }));
            return tcs.Task;
        }

        /// <summary>
        /// Switch this agent's delivery mode (a name from its voice's delivery modes, or the
        /// built-in <c>"neutral"</c>). Accepted at any time, including mid-turn: the change
        /// applies from the next sentence, never inside one. <paramref name="intensity"/> is
        /// clamped to 0..1 by the server and scales the mode where the model can (speed scale,
        /// Chatterbox expressiveness, Maya1 intensity). Errors:
        /// <see cref="TryllErrorCode.UnknownDeliveryMode"/>, or
        /// <see cref="TryllErrorCode.AgentVoiceRequired"/> when the agent has no voice.
        /// </summary>
        public Task<TryllError> SetDeliveryModeAsync(string mode, float intensity = 1f)
        {
            var tcs = new TaskCompletionSource<TryllError>();
            if (!IsValid)
            {
                tcs.TrySetResult(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return tcs.Task;
            }
            var agentId = _agentId;
            ulong notedId = 0;
            // Not a lifecycle call: the server admits it mid-turn, so it must not hold
            // IsLifecycleIdle false or delay a queued SendMessage.
            TryllCalls.SetAgentDeliveryMode(
                _client.Connection, this, agentId, mode ?? string.Empty, intensity,
                err =>
                {
                    NoteReply(MessageBody.SetAgentDeliveryModeRequest, err.IsOk, err, notedId);
                    if (err.IsOk)
                        DeliveryMode = string.Equals(mode, TryllVoiceProfileData.NeutralMode,
                            StringComparison.Ordinal) ? string.Empty : mode ?? string.Empty;
                    tcs.TrySetResult(err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.SetAgentDeliveryModeRequest, rid, flush: false);
                });
            return tcs.Task;
        }

        public void PrefillKvCache(Action<TryllKvCachePrefillResult, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.PrefillAgentKvCache(
                _client.Connection, this, agentId,
                (body, err) =>
                {
                    FinishLifecycle();
                    TryllKvCachePrefillResult result = null;
                    if (err.IsOk)
                    {
                        result = new TryllKvCachePrefillResult
                        {
                            AgentId = body.AgentId,
                            Applicable = body.Applicable,
                            Residency = (TryllKvCacheResidency)body.Residency,
                            PrefixStatus = (TryllKvCachePrefixStatus)body.PrefixStatus,
                            EligibleNodeCount = body.EligibleNodeCount,
                            DebugInfo = body.DebugInfo,
                            ContextsCreated = body.ContextsCreated,
                            TokensDecoded = body.TokensDecoded,
                            TokensReused = body.TokensReused,
                        };
                    }
                    NoteReply(MessageBody.PrefillAgentKvCacheRequest, err.IsOk, err, notedId);
                    onComplete?.Invoke(result, err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.PrefillAgentKvCacheRequest, rid, flush: true);
                }));
        }

        public void EvictKvCache(Action<TryllKvCacheEvictResult, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.EvictAgentKvCache(
                _client.Connection, this, agentId,
                (body, err) =>
                {
                    FinishLifecycle();
                    TryllKvCacheEvictResult result = null;
                    if (err.IsOk)
                    {
                        result = new TryllKvCacheEvictResult
                        {
                            AgentId = body.AgentId,
                            Applicable = body.Applicable,
                            Residency = (TryllKvCacheResidency)body.Residency,
                            PrefixStatus = (TryllKvCachePrefixStatus)body.PrefixStatus,
                            EligibleNodeCount = body.EligibleNodeCount,
                            DebugInfo = body.DebugInfo,
                            ContextsEvicted = body.ContextsEvicted,
                        };
                    }
                    NoteReply(MessageBody.EvictAgentKvCacheRequest, err.IsOk, err, notedId);
                    onComplete?.Invoke(result, err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.EvictAgentKvCacheRequest, rid, flush: true);
                }));
        }

        public void GetKvCacheStatus(Action<TryllKvCacheStatus, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.GetAgentKvCacheStatus(
                _client.Connection, this, agentId,
                (body, err) =>
                {
                    FinishLifecycle();
                    TryllKvCacheStatus result = null;
                    if (err.IsOk)
                    {
                        result = new TryllKvCacheStatus
                        {
                            AgentId = body.AgentId,
                            Applicable = body.Applicable,
                            Residency = (TryllKvCacheResidency)body.Residency,
                            PrefixStatus = (TryllKvCachePrefixStatus)body.PrefixStatus,
                            EligibleNodeCount = body.EligibleNodeCount,
                            DebugInfo = body.DebugInfo,
                        };
                    }
                    NoteReply(MessageBody.GetAgentKvCacheStatusRequest, err.IsOk, err, notedId);
                    onComplete?.Invoke(result, err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.GetAgentKvCacheStatusRequest, rid, flush: true);
                }));
        }

        /// <summary>
        /// Append scripted user/assistant interactions without running the graph.
        /// Empty sides are omitted; both-empty pairs are skipped. Idle-only.
        /// </summary>
        /// <returns>Applied count via <paramref name="onComplete"/>.</returns>
        public void AppendInteractions(
            IReadOnlyList<TryllDialogInteraction> interactions,
            Action<uint, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(0, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var batch = interactions == null
                ? new List<TryllDialogInteraction>()
                : new List<TryllDialogInteraction>(interactions);
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.AppendInteractions(
                _client.Connection, this,
                rid => ClientCodec.BuildAppendInteractionsRequest(rid, agentId, batch),
                (body, err) =>
                {
                    FinishLifecycle();
                    var count = err.IsOk ? body.AppendedCount : 0u;
                    NoteReply(MessageBody.AppendInteractionsRequest, err.IsOk, err, notedId);
                    onComplete?.Invoke(count, err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.AppendInteractionsRequest, rid, flush: true,
                        fill: n => n.DialogInteractionCount = batch?.Count ?? 0);
                }));
        }

        /// <summary>
        /// Remove whole interactions from the end of the dialog. count=0 is a no-op;
        /// count &gt;= dialog size clears all. Idle-only.
        /// </summary>
        /// <returns>Removed count via <paramref name="onComplete"/>.</returns>
        public void RemoveInteractionsFromEnd(int count, Action<uint, TryllError> onComplete = null)
        {
            if (count < 0)
            {
                var rejected = new TryllError(TryllErrorCode.InvalidParamValue, "count must be >= 0");
                _client?.Connection.Post(() => onComplete?.Invoke(0, rejected));
                return;
            }
            if (!IsValid) { onComplete?.Invoke(0, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var agentId = _agentId;
            var removeCount = (uint)count;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.RemoveInteractionsFromEnd(
                _client.Connection, this, agentId, removeCount,
                (body, err) =>
                {
                    FinishLifecycle();
                    var removed = err.IsOk ? body.RemovedCount : 0u;
                    NoteReply(MessageBody.RemoveInteractionsFromEndRequest, err.IsOk, err, notedId);
                    onComplete?.Invoke(removed, err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.RemoveInteractionsFromEndRequest, rid, flush: true,
                        fill: n => n.RemoveInteractionCount = removeCount);
                }));
        }

        /// <summary>
        /// Export the completed dialog as an owned snapshot. Idle-only.
        /// Does not write a file — the caller owns storage.
        /// </summary>
        public void ExportDialog(Action<TryllDialogSnapshot, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.ExportDialog(
                _client.Connection, this, agentId,
                (body, err) =>
                {
                    FinishLifecycle();
                    if (!err.IsOk)
                    {
                        NoteReply(MessageBody.ExportDialogRequest, false, err, notedId);
                        onComplete?.Invoke(null, err);
                        return;
                    }
                    TryllDialogSnapshot snapshot;
                    try
                    {
                        snapshot = UnpackDialog(body.Snapshot);
                    }
                    catch (Exception ex)
                    {
                        var invalid = new TryllError(TryllErrorCode.DialogSnapshotInvalid, ex.Message);
                        NoteReply(MessageBody.ExportDialogRequest, false, invalid, notedId);
                        onComplete?.Invoke(null, invalid);
                        return;
                    }
                    NoteReply(MessageBody.ExportDialogRequest, true, TryllError.Ok(), notedId);
                    onComplete?.Invoke(snapshot, TryllError.Ok());
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.ExportDialogRequest, rid, flush: true);
                }));
        }

        /// <summary>
        /// Replace the agent's dialog with <paramref name="snapshot"/>. Idle-only.
        /// Never appends. Failures leave the current dialog unchanged.
        /// </summary>
        public void ImportDialog(
            TryllDialogSnapshot snapshot,
            TryllDialogCompatibility compatibility = TryllDialogCompatibility.Strict,
            Action<TryllDialogImportResult, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            snapshot ??= new TryllDialogSnapshot();
            var encoded = snapshot.ToFlatBufferBytes();
            if (encoded != null && (uint)encoded.Length > ClientWire.MaxDialogSnapshotBytes)
            {
                var tooLarge = new TryllError(
                    TryllErrorCode.DialogSnapshotTooLarge,
                    "Encoded dialog snapshot exceeds MaxDialogSnapshotBytes");
                _client.Connection.Post(() => onComplete?.Invoke(null, tooLarge));
                return;
            }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.ImportDialog(
                _client.Connection, this,
                rid => ClientCodec.BuildImportDialogRequest(rid, agentId, snapshot, compatibility),
                (body, err) =>
                {
                    FinishLifecycle();
                    if (!err.IsOk)
                    {
                        NoteReply(MessageBody.ImportDialogRequest, false, err, notedId);
                        onComplete?.Invoke(null, err);
                        return;
                    }
                    var result = new TryllDialogImportResult
                    {
                        InteractionCount = body.InteractionCount,
                        Warnings = body.Warnings ?? new List<string>(),
                    };
                    NoteReply(MessageBody.ImportDialogRequest, true, TryllError.Ok(), notedId);
                    onComplete?.Invoke(result, TryllError.Ok());
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.ImportDialogRequest, rid, flush: true, fill: n =>
                    {
                        n.DialogInteractionCount = snapshot?.Interactions?.Count ?? 0;
                        n.Code = (uint)compatibility;
                    });
                }));
        }

        /// <summary>
        /// Persist the completed dialog to a storage-root-relative file. Idle-only.
        /// </summary>
        public void SaveDialog(string path, string saveId, Action<TryllDialogFileSaveResult, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.SaveDialog(
                _client.Connection, this, agentId, path, saveId,
                (body, err) =>
                {
                    FinishLifecycle();
                    TryllDialogFileSaveResult result = null;
                    if (err.IsOk)
                        result = new TryllDialogFileSaveResult { SavedAtMs = body.SavedAtMs };
                    NoteReply(MessageBody.SaveDialogRequest, err.IsOk, err, notedId);
                    onComplete?.Invoke(result, err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.SaveDialogRequest, rid, flush: true, fill: n => n.Message = path);
                }));
        }

        /// <summary>
        /// Replace the agent's dialog from a storage-root-relative file. Idle-only.
        /// </summary>
        public void LoadDialog(
            string path,
            TryllDialogCompatibility compatibility = TryllDialogCompatibility.Strict,
            Action<TryllDialogFileLoadResult, TryllError> onComplete = null)
        {
            if (!IsValid) { onComplete?.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            var agentId = _agentId;
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.LoadDialog(
                _client.Connection, this, agentId, path, (DialogCompatibility)compatibility,
                (body, err) =>
                {
                    FinishLifecycle();
                    TryllDialogFileLoadResult result = null;
                    if (err.IsOk)
                    {
                        result = new TryllDialogFileLoadResult
                        {
                            InteractionCount = body.InteractionCount,
                            Warnings = body.Warnings ?? new List<string>(),
                        };
                    }
                    NoteReply(MessageBody.LoadDialogRequest, err.IsOk, err, notedId);
                    onComplete?.Invoke(result, err);
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.LoadDialogRequest, rid, flush: true, fill: n =>
                    {
                        n.Message = path;
                        n.Code = (uint)compatibility;
                    });
                }));
        }

        /// <summary>
        /// Return a deep copy of the last authored params for a named node.
        ///
        /// Use as the starting point for a clone-set-send mutation:
        /// structural fields are automatically propagated from the baseline.
        /// </summary>
        public TryllNodeParamsBase GetNodeParamsBaseline(string nodeName)
        {
            if (_paramsBaseline.TryGetValue(nodeName, out var p))
                return p.Clone();
            return null;
        }

        /// <summary>Store/update the authored params baseline for a node.</summary>
        internal void SetNodeParamsBaseline(string nodeName, TryllNodeParamsBase @params)
        {
            _paramsBaseline[nodeName] = @params;
        }

        /// <summary>Where ticked Text IDs on this agent are looked up.</summary>
        internal void SetTextSource(string table, string assetName)
        {
            _textTable = table ?? "";
            _textAssetName = string.IsNullOrEmpty(assetName) ? "workflow" : assetName;
        }

        string ITryllLocalizedTarget.TextTable => _textTable;
        string ITryllLocalizedTarget.TextAssetName => _textAssetName;
        IReadOnlyList<string> ITryllLocalizedTarget.BaselineNodeNames
            => new List<string>(_paramsBaseline.Keys);

        // Already resolved by the tracker. AgentBusy is expected here (the
        // tracker retries at turn end), so it is not raised as a client error.
        void ITryllLocalizedTarget.SendResolvedParams(
            string nodeName, TryllNodeParamsBase @params, Action<TryllError> onComplete)
            => SendChangeParams(nodeName, @params, onComplete, globalError: false);

        // ── Internal wire calls (called by TryllAgentVariables) ───────────────

        internal void SendUpdateVariables(List<Tryll.VariableUpdateT> updates, Action<TryllError> onComplete,
            ulong carrierRequestId = 0)
        {
            var agentId = _agentId;
            var batch = updates == null ? new List<VariableUpdateT>() : new List<VariableUpdateT>(updates);
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.UpdateAgentVariables(
                _client.Connection, this,
                rid => ClientCodec.BuildUpdateAgentVariablesRequest(rid, agentId, batch),
                err => FinishAck(err, onComplete, globalError: true, MessageBody.UpdateAgentVariablesRequest, notedId),
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.UpdateAgentVariablesRequest, rid, flush: false, fill: n =>
                    {
                        n.VariableUpdateCount = batch?.Count ?? 0;
                        n.PayloadSummary = TryllObservationSnapshot.SummarizeVariableUpdates(batch);
                        n.CarrierRequestId = carrierRequestId;
                    });
                }));
        }

        internal void SendGetVariables(List<string> names, Action<List<Tryll.AgentVariableT>, TryllError> onComplete)
        {
            var agentId = _agentId;
            var copied = names == null ? null : new List<string>(names);
            ulong notedId = 0;
            IssueLifecycle(() => TryllCalls.GetAgentVariables(
                _client.Connection, this, agentId, copied,
                (body, err) =>
                {
                    FinishLifecycle();
                    if (!err.IsOk)
                    {
                        NoteReply(MessageBody.GetAgentVariablesRequest, false, err, notedId);
                        onComplete?.Invoke(null, err);
                        if (!IsLocalTerminal(err))
                            _client.RaiseError(err);
                        return;
                    }
                    NoteReply(MessageBody.GetAgentVariablesRequest, true, TryllError.Ok(), notedId);
                    onComplete?.Invoke(body.Variables ?? new List<AgentVariableT>(), TryllError.Ok());
                },
                rid =>
                {
                    notedId = rid;
                    NoteAgent(MessageBody.GetAgentVariablesRequest, rid, flush: false,
                        fill: n => n.VariableUpdateCount = copied?.Count ?? 0);
                }));
        }

        // ── Internal dispatch (called by TryllClient.Update on main thread) ───

        internal void HandleAnswerText(string nodeName, string text, bool isDelta, bool isFinal)
            => OnAnswerText?.Invoke(nodeName, text, isDelta, isFinal);

        internal void HandleTurnComplete(TryllTurnStatus status, string debugInfo, int tokensGenerated)
        {
            _pausedBatchGeneration++;
            _pendingToolCalls.Clear();
            _pendingBatchObserved = false;
            _pausedToolCalls.Clear();
            _pausedToolResults.Clear();
            PausedNode = "";
            // Before OnTurnComplete: a handler that sends the next message at
            // once would otherwise keep a queued language update busy forever.
            _client?.NoteLocalizedTurnEnded(this);
            OnTurnComplete?.Invoke(status, debugInfo, tokensGenerated);
        }

        internal void HandleError(TryllError error)
            => OnError?.Invoke(error);

        /// <summary>
        /// Agent-scope half of NodeEvent dispatch. The caller has already raised
        /// session-level events. Records tool calls, raises the per-agent events,
        /// then (for a pause) runs RegisterTool auto-resume.
        /// </summary>
        /// <param name="sessionListener">
        /// True when the <see cref="TryllClient"/> event for this event's kind has a
        /// subscriber: <see cref="TryllClient.Paused"/> for a pause, any of its
        /// tool-call events for a tool call. Agent-scope subscribers are counted
        /// here as well. Used only to decide whether a pause is resumable.
        /// </param>
        internal void HandleNodeEvent(TryllDecodedNodeEvent ev, bool sessionListener)
        {
            if (ev == null) return;
            switch (ev.Kind)
            {
                case TryllNodeEventKind.ToolCall:
                    HandleToolCall(
                        ev.ToolCall.CallId,
                        ev.ToolCall.NodeName,
                        ev.ToolCall.ToolName,
                        ev.ToolCall.ArgumentsJson,
                        ev.ToolCall.Disposition);
                    bool detailed = ToolCallDetailed != null;
                    bool withId = ToolCallNotificationWithId != null;
                    bool legacy = ToolCallNotification != null;
                    _pendingBatchObserved |= sessionListener || detailed || withId || legacy;
                    if (detailed)
                        ToolCallDetailed(ev.ToolCall);
                    if (withId)
                        ToolCallNotificationWithId(ev.ToolCall.CallId, ev.ToolCall.ToolName, ev.ToolCall.ArgumentsJson);
                    if (legacy)
                        ToolCallNotification(ev.ToolCall.ToolName, ev.ToolCall.ArgumentsJson);
                    if (!detailed && !withId && !legacy)
                        NodeEvent?.Invoke(ev.NodeName, ev.EventType, ev.KvPairs);
                    break;

                case TryllNodeEventKind.IntentClassified:
                    if (IntentClassified != null)
                        IntentClassified(ev.Intent);
                    else
                        NodeEvent?.Invoke(ev.NodeName, ev.EventType, ev.KvPairs);
                    break;

                case TryllNodeEventKind.IntentLlmClassified:
                    if (IntentLlmClassified != null)
                        IntentLlmClassified(ev.IntentLlm);
                    else
                        NodeEvent?.Invoke(ev.NodeName, ev.EventType, ev.KvPairs);
                    break;

                case TryllNodeEventKind.Paused:
                    bool agentPaused = Paused != null;
                    bool agentGeneric = NodeEvent != null;
                    if (agentPaused)
                        Paused(ev.NodeName, ev.PendingExit);
                    else
                        NodeEvent?.Invoke(ev.NodeName, ev.EventType, ev.KvPairs);
                    HandlePaused(ev.NodeName, sessionListener || agentPaused || agentGeneric);
                    break;

                default:
                    NodeEvent?.Invoke(ev.NodeName, ev.EventType, ev.KvPairs);
                    break;
            }
        }

        internal void HandleToolCall(
            string callId,
            string nodeName,
            string toolName,
            string argumentsJson,
            string disposition = "")
            => _pendingToolCalls.Add(
                new TryllToolCall(callId, nodeName, toolName, argumentsJson, disposition));

        /// <param name="externalListenerBound">
        /// True when a client event (<c>TryllClient.Paused</c>) is listening, so a
        /// pause this agent cannot auto-resume may still be resumed by game code.
        /// When false and no registered tool handler covers the batch, nothing can
        /// resume the turn and a warning is logged — a judgement only the client can
        /// make, since the server has no idea what the client subscribed to. A batch
        /// that reached a tool-call listener also counts as resumable: that listener
        /// can answer with ResumeWithToolResult(s).
        /// </param>
        internal void HandlePaused(string nodeName, bool externalListenerBound)
        {
            PausedNode = nodeName ?? "";

            bool batchObserved = _pendingBatchObserved;
            _pendingBatchObserved = false;

            if (_pendingToolCalls.Count > 0)
            {
                if (_pausedToolCalls.Count > 0)
                {
                    // Defensive: a fresh batch should never surface while a previous
                    // one is still retained (unresolved handler failure or resume
                    // rejection) — discard the stale batch rather than silently drop
                    // the new calls.
                    _pausedToolCalls.Clear();
                    _pausedToolResults.Clear();
                }
                _pausedToolCalls.AddRange(_pendingToolCalls);
                _pendingToolCalls.Clear();
            }

            if (_pausedToolCalls.Count == 0)
            {
                // A Pause node (or a pausing disposition with no calls): only game
                // code can resume this one.
                WarnIfUnresumable(nodeName, externalListenerBound);
                return;
            }

            foreach (var call in _pausedToolCalls)
            {
                if (call.NodeName != (nodeName ?? ""))
                {
                    WarnIfUnresumable(nodeName, externalListenerBound || batchObserved);
                    return; // mixed/foreign batch — stays entirely manual
                }
            }

            if (!RunPausedBatch())
                WarnIfUnresumable(nodeName, externalListenerBound || batchObserved);
        }

        private static void WarnIfUnresumable(string nodeName, bool externalListenerBound)
        {
            if (externalListenerBound) return;
            Debug.LogWarning(
                $"[Tryll] Turn paused at node '{nodeName}' and nothing can resume it: no " +
                "Paused handler is subscribed, no tool-call handler received a call for " +
                "this pause, and no registered tool handler covers it. The turn stays open " +
                "and further SendMessage calls fail with AgentBusy. Subscribe to " +
                "TryllAgent.Paused or TryllClient.Paused and call Resume, handle the tool " +
                "call and call ResumeWithToolResult(s), or RegisterTool a handler for " +
                "every tool in the batch.");
        }

        /// <summary>
        /// Run registered handlers for the retained <see cref="_pausedToolCalls"/>
        /// batch and, on full success, send the resume. Skips calls that already
        /// have a cached result from a previous attempt (see
        /// <see cref="RetryPausedToolBatch"/>) so retry never re-runs a handler
        /// that already succeeded. The batch itself is only cleared by the caller
        /// once the resume Ack reports success.
        /// </summary>
        /// <returns>True when the batch fully resolved and a resume was sent.</returns>
        private bool RunPausedBatch()
        {
            // A mixed registered/unregistered batch stays entirely manual. This
            // avoids executing side effects that cannot yet be atomically resumed.
            foreach (var call in _pausedToolCalls)
            {
                if (!_toolHandlers.ContainsKey(call.ToolName))
                    return false;
            }

            var pending = new List<(TryllToolCall call, Task<string> task)>();
            foreach (var call in _pausedToolCalls)
            {
                if (_pausedToolResults.ContainsKey(call.CallId))
                    continue; // already produced a result on a previous attempt

                Task<string> task;
                try
                {
                    task = _toolHandlers[call.ToolName](call);
                }
                catch (Exception ex)
                {
                    ReportToolHandlerError(new TryllError(
                        TryllErrorCode.InvalidToolResults,
                        $"Tool handler failed; turn remains paused: {ex.Message}"));
                    return true;
                }
                if (task == null)
                {
                    ReportToolHandlerError(new TryllError(
                        TryllErrorCode.InvalidToolResults,
                        "Tool handler returned a null task; turn remains paused"));
                    return true;
                }
                if (task.IsCompleted)
                {
                    if (!TryCacheCompletedToolTask(call, task))
                        return true;
                }
                else
                {
                    pending.Add((call, task));
                }
            }

            if (pending.Count == 0)
            {
                SendPausedBatchResume();
                return true;
            }

            int generation = ++_pausedBatchGeneration;
            ContinuePausedBatchWhenReady(generation, pending);
            return true;
        }

        private void ContinuePausedBatchWhenReady(
            int generation, List<(TryllToolCall call, Task<string> task)> pending)
        {
            // ExecuteSynchronously posts during SetResult/SetException on the
            // completing thread so EditMode tests can PumpEvents immediately.
            // A thread-pool completion still hops to the main thread via Post.
            Task.WhenAll(pending.ConvertAll(p => p.task)).ContinueWith(whenAll =>
            {
                _ = whenAll.Exception; // observe WhenAll so a faulted handler is not unobserved
                _client.PostToMainThread(() =>
                {
                    if (generation != _pausedBatchGeneration) return;
                    foreach (var (call, task) in pending)
                    {
                        if (_pausedToolResults.ContainsKey(call.CallId))
                            continue;
                        if (!TryCacheCompletedToolTask(call, task))
                            return;
                    }
                    SendPausedBatchResume();
                });
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private bool TryCacheCompletedToolTask(TryllToolCall call, Task<string> task)
        {
            if (task.IsFaulted)
            {
                var ex = task.Exception?.GetBaseException() ?? task.Exception;
                ReportToolHandlerError(new TryllError(
                    TryllErrorCode.InvalidToolResults,
                    $"Tool handler failed; turn remains paused: {ex?.Message}"));
                return false;
            }
            if (task.IsCanceled)
            {
                ReportToolHandlerError(new TryllError(
                    TryllErrorCode.InvalidToolResults,
                    "Tool handler was canceled; turn remains paused"));
                return false;
            }
            _pausedToolResults[call.CallId] = new TryllToolResult(call.CallId, task.Result);
            return true;
        }

        private void SendPausedBatchResume()
        {
            var results = new List<TryllToolResult>(_pausedToolCalls.Count);
            // AwaitResult needs the result batch. Acknowledgement / omit
            // dispositions already have synthetic/no pending results —
            // sending tool_results would 3016.
            bool needsNativeResults = false;
            foreach (var call in _pausedToolCalls)
            {
                if (_pausedToolResults.TryGetValue(call.CallId, out var r))
                    results.Add(r);
                if (call.RequiresNativeResults)
                    needsNativeResults = true;
            }

            Action<TryllError> onComplete = error =>
            {
                if (error.IsOk)
                {
                    // Clear only on a successful Ack — a rejected/disconnected resume
                    // keeps the batch (with any cached results) for RetryPausedToolBatch.
                    _pausedToolCalls.Clear();
                    _pausedToolResults.Clear();
                    PausedNode = "";
                }
                else
                {
                    ReportToolHandlerError(error);
                }
            };
            if (needsNativeResults)
                ResumeWithToolResults(results, onComplete: onComplete);
            else
                Resume(onComplete: onComplete);
        }

        private void ReportToolHandlerError(TryllError error)
        {
            ToolHandlerError?.Invoke(error);
            HandleError(error);
        }

        internal void HandleTtsAudioFormat(string nodeName, int sampleRate, int channels, int bitsPerSample)
            => OnTtsAudioFormat?.Invoke(nodeName, sampleRate, channels, bitsPerSample);

        internal void HandleTtsAudio(string nodeName, short[] buffer, int length)
        {
            if (buffer == null) return;
            try
            {
                if (length > 0)
                    OnTtsAudio?.Invoke(nodeName, new ArraySegment<short>(buffer, 0, length));
            }
            finally
            {
                ArrayPool<short>.Shared.Return(buffer);
            }
        }

        internal void HandleTtsVisemes(string nodeName, TryllVisemeEvent[] events)
        {
            if (events == null || events.Length == 0) return;
            OnTtsVisemes?.Invoke(nodeName, events);
        }

        private void IssueLifecycle(Action call)
        {
            _inflightLifecycle++;
            try
            {
                call();
            }
            catch
            {
                _inflightLifecycle--;
                throw;
            }
        }

        private void FinishLifecycle()
        {
            if (_inflightLifecycle > 0)
                _inflightLifecycle--;
        }

        private void FinishAck(
            TryllError err, Action<TryllError> onComplete, bool globalError,
            MessageBody request,
            ulong requestId,
            Action onSuccess = null)
        {
            FinishLifecycle();
            if (err.IsOk)
                onSuccess?.Invoke();
            NoteReply(request, err.IsOk, err, requestId);
            onComplete?.Invoke(err);
            if (!err.IsOk && globalError && !IsLocalTerminal(err))
                _client.RaiseError(err);
        }

        private static bool IsLocalTerminal(TryllError err)
            => err.Code == (uint)TryllErrorCode.ConnectionDropped
            || err.Code == (uint)TryllErrorCode.ConnectionFailed
            || (err.Code == (uint)TryllErrorCode.InvalidAgentId && err.Message == "Agent destroyed");

        private void NoteAgent(MessageBody body, ulong requestId, bool flush, Action<TryllObservationNote> fill = null)
        {
            if (flush)
                Variables.FlushIfDirty(requestId);
            var note = TryllObservationNote.Request(body, requestId);
            note.AgentId = _agentId;
            fill?.Invoke(note);
            _client.NoteOutbound(note);
        }

        private void NoteReply(MessageBody request, bool ok, TryllError err, ulong requestId)
        {
            var note = TryllObservationNote.Reply(request, ok, requestId);
            note.AgentId = _agentId;
            note.Code = (uint)err.Code;
            if (!ok)
                note.Message = err.Message;
            _client.NoteInbound(note);
        }

        private static void Deliver(TryllConnection connection, Action action)
        {
            if (connection.OnCallerThread)
                action();
            else
                connection.Post(action);
        }

        private static TryllDialogSnapshot UnpackDialog(PersistedDialogSnapshotT snapshot)
        {
            if (snapshot == null)
                throw new ArgumentException("ExportDialogResponse missing snapshot");
            var fbb = new FlatBufferBuilder(256);
            fbb.Finish(PersistedDialogSnapshot.Pack(fbb, snapshot).Value);
            return TryllDialogSnapshot.Unpack(
                PersistedDialogSnapshot.GetRootAsPersistedDialogSnapshot(new ByteBuffer(fbb.SizedByteArray())));
        }

        /// <summary>
        /// Fail any in-flight per-request callbacks (e.g. on destroy or disconnect)
        /// so callers are not left waiting for responses that will never arrive.
        /// </summary>
        internal void DrainPendingCallbacks(TryllError terminalError)
        {
            _client?.Connection.FailOwnedBy(this, terminalError);
            _pausedBatchGeneration++;
            _pausedToolCalls.Clear();
            _pausedToolResults.Clear();
            PausedNode = "";
        }

        internal void ClearAfterDestroyRequest() => _agentId = 0;
    }
}
