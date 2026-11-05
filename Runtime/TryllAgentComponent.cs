// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Tryll.Client
{
    /// <summary>
    /// Component that manages one <see cref="TryllAgent"/> lifetime on a GameObject.
    /// Mirrors UTryllAgentComponent (UActorComponent) from the Unreal plugin.
    ///
    /// Ownership contract
    /// ------------------
    /// This component OWNS the <see cref="Agent"/> it creates and is responsible for
    /// calling <see cref="TryllAgent.Dispose"/> in <see cref="OnDisable"/> /
    /// <see cref="OnDestroy"/>. Do not share the returned agent reference beyond the
    /// lifetime of this component.
    ///
    /// If you need an agent that outlives the component, create it directly via
    /// <see cref="TryllClient.RequestCreateAgentAsync"/> and manage its lifetime yourself.
    ///
    /// Threading
    /// ---------
    /// All <see cref="UnityEvent"/> callbacks fire on the Unity main thread.
    /// </summary>
    [AddComponentMenu("Tryll/Tryll Agent Component")]
    [DisallowMultipleComponent]
    [HelpURL(TryllDocs.TryllAgentComponent)]
    public sealed class TryllAgentComponent : MonoBehaviour
    {
        // ── Inspector fields ───────────────────────────────────────────────────

        [Tooltip("Workflow graph asset to use when creating the agent. Required.")]
        public TryllWorkflowAsset WorkflowAsset;

        [Tooltip("Per-instance value overrides for the workflow's declared variables (Material-Instance " +
                 "pattern — name/type come from the declaration; only the initial value can be overridden). " +
                 "The Inspector shows one optional row per declared variable. Stale entries (renamed/removed " +
                 "declarations) are skipped with a warning at CreateAgent.")]
        public List<TryllVariableOverride> VariableOverrides = new List<TryllVariableOverride>();

        /// <summary>
        /// When true, CreateAgent requests server diagnostics. Authoring UI retired —
        /// Chat and Agent Log OR this on via transient override / diagnostics-demand lease.
        /// Kept for runtime API and serialized asset compatibility; default remains false.
        /// </summary>
        [HideInInspector]
        public bool EnableDiagnostics;

        [Tooltip("Keep the agent's dialogue history across turns. Disable for a " +
                 "stateless agent (e.g. classification/routing) that discards its " +
                 "history after every turn.")]
        public bool MaintainDialogueHistory = true;

        [Tooltip("How language-model KV contexts are materialised at agent creation.")]
        public TryllKvCacheInitialization KvCacheInitialization = TryllKvCacheInitialization.AllocateOnly;

        [Tooltip("Whether a human ever waits on this agent's turns. Set Background for " +
                 "world-logic/evaluator agents so the inference scheduler paces or defers " +
                 "their work first under GPU pressure. Purely a scheduling hint.")]
        public TryllWorkload Workload = TryllWorkload.Interactive;

        [Tooltip("Create the agent automatically when TryllClient connects and this component is enabled.")]
        public bool AutoCreateOnConnect = true;

        [Tooltip("Optional. Plays streaming TTS audio frames for this agent. Leave null to ignore TTS audio.")]
        [SerializeField] private TryllSpeaker speaker;

        /// <summary>
        /// The <see cref="TryllSpeaker"/> that plays this agent's streaming TTS audio.
        /// Assignable from code (e.g. when spawning agents dynamically); null means
        /// TTS audio is ignored. Must be set before <see cref="CreateAgent"/> runs.
        /// </summary>
        public TryllSpeaker Speaker
        {
            get => speaker;
            set => speaker = value;
        }

        [Tooltip("The agent's voice: TTS model, identity, visemes and delivery modes. Required " +
                 "when the workflow has a Speak or GenerateAndSpeak node (agent creation fails " +
                 "with AgentVoiceRequired otherwise). Ignored by workflows that do not speak.")]
        [SerializeField] private TryllVoiceProfile voice;

        /// <summary>
        /// The agent's voice profile (protocol v27). Read when <see cref="CreateAgent"/> runs,
        /// so set it before creation when spawning characters at runtime. To change the voice
        /// of a live agent use <see cref="SetVoiceAsync"/>, which also updates this property.
        /// </summary>
        public TryllVoiceProfile Voice
        {
            get => voice;
            set => voice = value;
        }

        [Tooltip("Override this agent instance's initial voice delivery without changing the shared " +
                 "Voice profile. Empty means neutral. The Inspector applies changes to a live agent.")]
        [SerializeField] private bool overrideVoiceDelivery;

        [SerializeField] private string voiceDelivery = string.Empty;

        /// <summary>Whether <see cref="VoiceDelivery"/> overrides the profile's default mode.</summary>
        public bool OverrideVoiceDelivery
        {
            get => overrideVoiceDelivery;
            set => overrideVoiceDelivery = value;
        }

        /// <summary>
        /// Per-agent initial delivery mode. Empty and <c>"neutral"</c> both mean the built-in
        /// neutral delivery. Used only when <see cref="OverrideVoiceDelivery"/> is true.
        /// </summary>
        public string VoiceDelivery
        {
            get => voiceDelivery;
            set => voiceDelivery = NormalizeVoiceDelivery(value);
        }

        // ── Events ─────────────────────────────────────────────────────────────

        [Serializable] public sealed class TextEvent         : UnityEvent<string>           { }
        [Serializable] public sealed class AnswerTextEvent   : UnityEvent<string, string, bool, bool> { }
        [Serializable] public sealed class TurnCompleteEvent : UnityEvent<TryllTurnStatus, string, int> { }
        [Serializable] public sealed class ErrorEvent        : UnityEvent<TryllError>        { }
        [Serializable] public sealed class ParamChangedEvent : UnityEvent<TryllError>        { }
        [Serializable] public sealed class ResumedEvent      : UnityEvent<TryllError>        { }
        [Serializable] public sealed class KvCachePrefillEvent : UnityEvent<TryllKvCachePrefillResult, TryllError> { }
        [Serializable] public sealed class KvCacheEvictEvent   : UnityEvent<TryllKvCacheEvictResult, TryllError> { }
        [Serializable] public sealed class KvCacheStatusEvent  : UnityEvent<TryllKvCacheStatus, TryllError> { }
        [Serializable] public sealed class MessageQueuedEvent  : UnityEvent<string> { }
        [Serializable] public sealed class InteractionCountEvent : UnityEvent<uint, TryllError> { }

        /// <summary>
        /// Fired when any answer text arrives (nodeName, text, isDelta, isFinal). `nodeName` is
        /// the workflow node that produced this chunk — attributes chunks when a turn routes
        /// through more than one text-producing node in the same turn.
        /// </summary>
        public AnswerTextEvent   OnAnswerText    = new AnswerTextEvent();

        /// <summary>Fired when a full turn completes.</summary>
        public TurnCompleteEvent OnTurnComplete  = new TurnCompleteEvent();

        /// <summary>Fired when an agent-level error arrives.</summary>
        public ErrorEvent        OnError         = new ErrorEvent();

        /// <summary>Fired when this component successfully creates its agent.</summary>
        public UnityEvent        OnAgentCreated  = new UnityEvent();

        /// <summary>Fired when this component's agent is destroyed.</summary>
        public UnityEvent        OnAgentDestroyed = new UnityEvent();

        /// <summary>
        /// Fired when a <see cref="ChangeParams"/> request completes.
        /// Check <see cref="TryllError.IsOk"/> — success is not a null error
        /// (<see cref="TryllError"/> is a value type).
        /// </summary>
        public ParamChangedEvent OnParamChanged = new ParamChangedEvent();

        /// <summary>
        /// Fired when a <see cref="Resume"/> request completes.
        /// Check <see cref="TryllError.IsOk"/> — success is not a null error
        /// (<see cref="TryllError"/> is a value type).
        /// </summary>
        public ResumedEvent OnResumed = new ResumedEvent();

        // Per-agent NodeEvent surfaces. C# events, not UnityEvents: UnityEvent cannot
        // report listeners added with AddListener, and suppression plus the
        // unresumable-pause warning need an exact subscriber check. Handlers are
        // subscribed on the agent itself so the agent's null check is the listener check.
        // TryllClient keeps the all-agents versions.

        private Action<string, string> _toolCallNotification;
        /// <summary>This agent's tool call. Args: (toolName, argumentsJson).</summary>
        public event Action<string, string> ToolCallNotification
        {
            add => AddNodeHandler(ref _toolCallNotification, value,
                (agent, handler) => agent.ToolCallNotification += handler);
            remove => RemoveNodeHandler(ref _toolCallNotification, value,
                (agent, handler) => agent.ToolCallNotification -= handler);
        }

        private Action<string, string, string> _toolCallNotificationWithId;
        /// <summary>This agent's tool call with its call id. Args: (callId, toolName, argumentsJson).</summary>
        public event Action<string, string, string> ToolCallNotificationWithId
        {
            add => AddNodeHandler(ref _toolCallNotificationWithId, value,
                (agent, handler) => agent.ToolCallNotificationWithId += handler);
            remove => RemoveNodeHandler(ref _toolCallNotificationWithId, value,
                (agent, handler) => agent.ToolCallNotificationWithId -= handler);
        }

        private Action<TryllToolCall> _toolCallDetailed;
        /// <summary>This agent's detailed tool call. A subscriber suppresses <see cref="NodeEvent"/> for that event.</summary>
        public event Action<TryllToolCall> ToolCallDetailed
        {
            add => AddNodeHandler(ref _toolCallDetailed, value,
                (agent, handler) => agent.ToolCallDetailed += handler);
            remove => RemoveNodeHandler(ref _toolCallDetailed, value,
                (agent, handler) => agent.ToolCallDetailed -= handler);
        }

        private Action<TryllIntentClassification> _intentClassified;
        /// <summary>Embedding ClassifyIntent result for this agent. The payload has no agent id.</summary>
        public event Action<TryllIntentClassification> IntentClassified
        {
            add => AddNodeHandler(ref _intentClassified, value,
                (agent, handler) => agent.IntentClassified += handler);
            remove => RemoveNodeHandler(ref _intentClassified, value,
                (agent, handler) => agent.IntentClassified -= handler);
        }

        private Action<TryllIntentLlmClassification> _intentLlmClassified;
        /// <summary>ClassifyIntentLLM result for this agent. The payload has no agent id.</summary>
        public event Action<TryllIntentLlmClassification> IntentLlmClassified
        {
            add => AddNodeHandler(ref _intentLlmClassified, value,
                (agent, handler) => agent.IntentLlmClassified += handler);
            remove => RemoveNodeHandler(ref _intentLlmClassified, value,
                (agent, handler) => agent.IntentLlmClassified -= handler);
        }

        private Action<string, string> _paused;
        /// <summary>
        /// This agent's turn paused. Args: (nodeName, pendingExit).
        /// <see cref="TryllClient.Paused"/> remains the all-agents surface.
        /// </summary>
        public event Action<string, string> Paused
        {
            add => AddNodeHandler(ref _paused, value,
                (agent, handler) => agent.Paused += handler);
            remove => RemoveNodeHandler(ref _paused, value,
                (agent, handler) => agent.Paused -= handler);
        }

        private Action<string, string, IReadOnlyList<KeyValuePair<string, string>>> _nodeEvent;
        /// <summary>
        /// Generic NodeEvent for this agent when no typed listener is bound here.
        /// Args: (nodeName, eventType, kvPairs).
        /// </summary>
        public event Action<string, string, IReadOnlyList<KeyValuePair<string, string>>> NodeEvent
        {
            add => AddNodeHandler(ref _nodeEvent, value,
                (agent, handler) => agent.NodeEvent += handler);
            remove => RemoveNodeHandler(ref _nodeEvent, value,
                (agent, handler) => agent.NodeEvent -= handler);
        }
        public KvCachePrefillEvent OnKvCachePrefilled = new KvCachePrefillEvent();
        public KvCacheEvictEvent   OnKvCacheEvicted  = new KvCacheEvictEvent();
        public KvCacheStatusEvent  OnKvCacheStatus   = new KvCacheStatusEvent();
        /// <summary>
        /// Fired when <see cref="SendMessage"/> could not hand the message over immediately and queued
        /// it instead. Carries the queued text. A game can use this to show that it is waiting rather
        /// than leaving the player wondering whether their line registered.
        /// </summary>
        public MessageQueuedEvent OnMessageQueued = new MessageQueuedEvent();

        /// <summary>
        /// Fired when the latest queued message is handed to the agent. The server can still reject
        /// it with <see cref="TryllErrorCode.AgentBusy"/>, in which case <see cref="OnMessageQueued"/>
        /// fires again after the message is put back.
        /// </summary>
        public MessageQueuedEvent OnMessageDequeued = new MessageQueuedEvent();

        public InteractionCountEvent OnInteractionsAppended = new InteractionCountEvent();
        public InteractionCountEvent OnInteractionsRemoved  = new InteractionCountEvent();
        [Serializable] public sealed class DialogExportedEvent : UnityEvent<TryllDialogSnapshot, TryllError> { }
        [Serializable] public sealed class DialogImportedEvent : UnityEvent<TryllDialogImportResult, TryllError> { }
        public DialogExportedEvent OnDialogExported = new DialogExportedEvent();
        public DialogImportedEvent OnDialogImported = new DialogImportedEvent();
        [Serializable] public sealed class DialogFileSavedEvent : UnityEvent<TryllDialogFileSaveResult, TryllError> { }
        [Serializable] public sealed class DialogFileLoadedEvent : UnityEvent<TryllDialogFileLoadResult, TryllError> { }
        public DialogFileSavedEvent OnDialogSaved = new DialogFileSavedEvent();
        public DialogFileLoadedEvent OnDialogLoaded = new DialogFileLoadedEvent();

        // ── Runtime state ──────────────────────────────────────────────────────

        /// <summary>The live agent. Null until <see cref="CreateAgent"/> has completed successfully.</summary>
        public TryllAgent Agent { get; private set; }

        public bool HasAgent => Agent != null && Agent.IsValid;
        /// <summary>Typed but not yet accepted by the agent — see <see cref="SendMessage"/>.</summary>
        [NonSerialized] private string _pendingMessage;

        /// <summary>Handed to the agent but not yet confirmed by a turn, so it can be re-queued.</summary>
        [NonSerialized] private string _inFlightMessage;

        /// <summary>
        /// A turn this component started and has not seen complete. Deliberately conservative: it
        /// tracks only the busy windows this component can see, and <see cref="HandleError"/>
        /// re-queues if the server refuses a send anyway.
        /// </summary>
        private bool _turnInFlight;

        /// <summary>True after a message was handed to the agent and before completion or re-queue.</summary>
        public bool HasTurnInFlight => _turnInFlight;

        private bool _createInFlight;
        private bool _transientDiagnosticsStaged;
        private bool _activeCreateUsesTransientDiagnostics;
        private int _createGeneration;
        private readonly Dictionary<string, Func<TryllToolCall, string>> _toolHandlers
            = new Dictionary<string, Func<TryllToolCall, string>>();
        private readonly Dictionary<string, Func<TryllToolCall, Task<string>>> _asyncToolHandlers
            = new Dictionary<string, Func<TryllToolCall, Task<string>>>();

        internal bool IsAgentCreationInFlight => _createInFlight;
        internal bool HasStagedTransientDiagnostics => _transientDiagnosticsStaged;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            var client = TryllClient.Instance;
            if (client == null) return;

            client.ConnectionChanged += OnConnectionChanged;
            client.CreateSessionComplete += OnCreateSessionComplete;

            // Agent creation requires a session, not just a TCP connection
            // (protocol v4): create only once CreateSession has succeeded.
            if (AutoCreateOnConnect && client.IsSessionReady && !HasAgent)
                CreateAgent();
        }

        private void OnDisable()
        {
            var client = TryllClient.Instance;
            if (client != null)
            {
                client.ConnectionChanged -= OnConnectionChanged;
                client.CreateSessionComplete -= OnCreateSessionComplete;
            }

            DestroyAgent();
        }

        private void OnDestroy()
        {
            DestroyAgent();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Manually request agent creation. Ignored when already running or creating.
        /// Populates <see cref="Agent"/> and fires <see cref="OnAgentCreated"/> on success.
        /// </summary>
        public void CreateAgent()
        {
            TryStartCreateAgent();
        }

        /// <summary>
        /// Stage Chat's editor-only one-shot diagnostics request without starting it. This
        /// is called before the editor session starts so AutoCreateOnConnect cannot race a
        /// separate diagnostics-disabled create.
        /// </summary>
        internal bool StageAgentCreationWithTransientDiagnostics()
        {
            if (HasAgent) return false;
            if (_createInFlight) return _activeCreateUsesTransientDiagnostics;

            _transientDiagnosticsStaged = true;
            return true;
        }

        /// <summary>Start or join the staged editor-only diagnostics create.</summary>
        internal bool CreateAgentWithTransientDiagnostics()
        {
            if (!StageAgentCreationWithTransientDiagnostics()) return false;
            TryStartCreateAgent();
            return true;
        }

        private async void TryStartCreateAgent()
        {
            if (HasAgent || _createInFlight) return;

            var client = TryllClient.Instance;
            if (client == null)
            {
                // A transient editor request may be staged before the shared editor client
                // exists. OnSessionReady retries this same one-shot request.
                if (!_transientDiagnosticsStaged)
                    TryllLog.Error("TryllAgentComponent.CreateAgent: TryllClient.Instance is null");
                return;
            }

            // The session must exist first (protocol v4): CreateAgent before
            // CreateSession is rejected by the server with SessionNotReady.  Wait for
            // OnCreateSessionComplete to drive creation instead.
            if (!client.IsSessionReady)
            {
                TryllLog.Verbose("TryllAgentComponent: session not ready; deferring CreateAgent until CreateSession completes");
                return;
            }

            bool useTransientDiagnostics = _transientDiagnosticsStaged;
            _transientDiagnosticsStaged = false;
            _activeCreateUsesTransientDiagnostics = useTransientDiagnostics;
            _createInFlight = true;
            int generation = ++_createGeneration;

            if (WorkflowAsset == null)
            {
                ClearCreateState();
                var missing = new TryllError(TryllErrorCode.GraphCompilationFailed,
                    "TryllAgentComponent.CreateAgent: WorkflowAsset is not assigned");
                TryllLog.Error(missing.Message);
                OnError.Invoke(missing);
                return;
            }

            var graph = WorkflowAsset.Graph;
            var declaredVariables = WorkflowAsset.Variables;
            var mergedVariables = MergeVariableOverrides(declaredVariables, VariableOverrides);

            // If this agent's workflow speaks (has a GenerateAndSpeak or Speak node)
            // and no speaker was assigned, use the one on this GameObject, else add
            // one, so TTS audio is actually played. Reusing matters: TryllLipSync
            // requires a TryllSpeaker, and a second one would get the audio while
            // lip sync followed the first. Matches Unreal's auto-discovery.
            // TryllSpeaker auto-provisions its own AudioSource.
            if (speaker == null && GraphUsesTts(graph))
            {
                speaker = GetComponent<TryllSpeaker>();
                if (speaker == null)
                    speaker = gameObject.AddComponent<TryllSpeaker>();
            }

            // The voice is sent whenever one is assigned; the server ignores it (and loads
            // no TTS model) when no node speaks. A speaking graph without one is rejected
            // by the server with AgentVoiceRequired — say why locally as well.
            var voiceData = ResolveCreateVoice();
            if (voiceData == null && GraphUsesTts(graph))
                TryllLog.Warning(
                    $"TryllAgentComponent '{name}': workflow '{WorkflowAsset.name}' speaks but no " +
                    "Voice profile is assigned; the server will reject CreateAgent (AgentVoiceRequired).");

            bool enableDiagnostics = useTransientDiagnostics || EnableDiagnostics;
            // The chat window's diagnostics create is an editor call. A game
            // component's create stays on the play-session token.
            Task<(TryllAgent agent, TryllError error)> createTask;
            if (useTransientDiagnostics)
            {
                using (client.AsEditorCall())
                    createTask = client.RequestCreateAgentAsync(
                        graph, enableDiagnostics, MaintainDialogueHistory, mergedVariables,
                        (Tryll.AgentKvCacheInitialization)KvCacheInitialization,
                        (Tryll.AgentWorkload)Workload,
                        WorkflowAsset.TextTable,
                        WorkflowAsset.name,
                        voiceData);
            }
            else
            {
                createTask = client.RequestCreateAgentAsync(
                    graph, enableDiagnostics, MaintainDialogueHistory, mergedVariables,
                    (Tryll.AgentKvCacheInitialization)KvCacheInitialization,
                    (Tryll.AgentWorkload)Workload,
                    WorkflowAsset.TextTable,
                    WorkflowAsset.name,
                    voiceData);
            }
            var (agent, error) = await createTask;

            if (generation != _createGeneration)
            {
                agent?.Dispose();
                return;
            }

            if (!error.IsOk)
            {
                ClearCreateState();
                TryllLog.Error($"TryllAgentComponent: CreateAgent failed: {error}");
                OnError.Invoke(error);
                return;
            }

            Agent = agent;
            Agent.DisplayName    = gameObject != null ? gameObject.name : null;
            AttachNodeEventHandlers();
            Agent.SetOnAnswerText(HandleAnswerText);
            Agent.SetOnTurnComplete(HandleTurnComplete);
            Agent.SetOnError(HandleError);
            foreach (var pair in _toolHandlers)
                Agent.RegisterTool(pair.Key, pair.Value);
            foreach (var pair in _asyncToolHandlers)
                Agent.RegisterTool(pair.Key, pair.Value);
            if (speaker != null)
            {
                Agent.SetOnTtsAudioFormat(speaker.HandleFormat);
                Agent.SetOnTtsAudio(speaker.HandleAudio);
            }

            ClearCreateState();
            OnAgentCreated.Invoke();
            TryFlushPendingMessage(); // a message sent before the agent existed goes out now
        }

        /// <summary>
        /// Returns the voice sent by the next CreateAgent. A delivery override is applied to
        /// a clone so the shared Voice profile asset is never mutated.
        /// </summary>
        internal TryllVoiceProfileData ResolveCreateVoice()
        {
            var profile = voice != null ? voice.Data : null;
            if (profile == null || !overrideVoiceDelivery)
                return profile;

            var resolved = profile.Clone();
            resolved.DefaultDeliveryMode = NormalizeVoiceDelivery(voiceDelivery);
            return resolved;
        }

        internal string EffectiveVoiceDelivery()
        {
            if (overrideVoiceDelivery)
                return NormalizeVoiceDelivery(voiceDelivery);
            return NormalizeVoiceDelivery(voice != null && voice.Data != null
                ? voice.Data.DefaultDeliveryMode
                : string.Empty);
        }

        internal static string NormalizeVoiceDelivery(string mode)
            => string.Equals(mode, TryllVoiceProfileData.NeutralMode, StringComparison.Ordinal)
                ? string.Empty
                : mode ?? string.Empty;

        /// <summary>
        /// Merge a workflow's declared variables with this component's enabled value
        /// overrides (Material-Instance pattern: the override may only replace the
        /// initial value of a declaration with the same name AND type — never
        /// introduce a new variable or change its type). Overrides that don't match
        /// any declared variable, or whose type disagrees, are skipped with a
        /// warning (stale — e.g. after the workflow asset's declaration changed).
        /// </summary>
        /// <summary>Visible to Edit Mode tests via InternalsVisibleTo.</summary>
        internal static List<TryllVariableDecl> MergeVariableOverrides(
            List<TryllVariableDecl> declared, List<TryllVariableOverride> overrides)
        {
            var result = new List<TryllVariableDecl>();
            if (declared == null) return result;

            // First serialized entry per name is canonical (matches CollectLegacyIndices /
            // Inspector). Later duplicates are ignored even if enabled; a disabled first
            // entry means the declaration default is used.
            var firstByName = new Dictionary<string, TryllVariableOverride>();
            if (overrides != null)
            {
                foreach (var o in overrides)
                {
                    if (o == null || string.IsNullOrEmpty(o.Name)) continue;
                    if (!firstByName.ContainsKey(o.Name))
                        firstByName[o.Name] = o;
                }
            }

            var declaredNames = new HashSet<string>();
            foreach (var d in declared)
            {
                if (d == null || string.IsNullOrEmpty(d.Name)) continue;
                declaredNames.Add(d.Name);

                if (firstByName.TryGetValue(d.Name, out var ov) && ov.Enabled && ov.Value != null)
                {
                    if (ov.Value.Type == d.Type)
                    {
                        result.Add(new TryllVariableDecl
                        {
                            Name           = d.Name,
                            Type           = d.Type,
                            IntValue       = ov.Value.IntValue,
                            FloatValue     = ov.Value.FloatValue,
                            StringValue    = ov.Value.StringValue,
                            BoolValue      = ov.Value.BoolValue,
                            StringSetValue = new List<string>(ov.Value.StringSetValue ?? new List<string>()),
                            AllowOutputSubstitution =
                                d.Type != TryllVariableType.StringSet && d.AllowOutputSubstitution,
                        });
                        continue;
                    }
                    TryllLog.Warning(
                        $"TryllAgentComponent: variable override '{d.Name}' type mismatch " +
                        $"(declared {d.Type}, override {ov.Value.Type}) — using the declared initial value");
                }
                result.Add(d);
            }

            if (overrides != null)
            {
                foreach (var o in overrides)
                {
                    if (o == null || !o.Enabled || string.IsNullOrEmpty(o.Name)) continue;
                    if (!declaredNames.Contains(o.Name))
                        TryllLog.Warning(
                            $"TryllAgentComponent: variable override '{o.Name}' does not match any " +
                            "declared variable — ignoring (stale?)");
                }
            }

            return result;
        }

        /// <summary>
        /// True when <paramref name="graph"/> has a speaking node (schema <c>tts_consumer</c>:
        /// Speak, GenerateAndSpeak), i.e. this agent produces TTS audio and needs a voice.
        /// </summary>
        internal static bool GraphUsesTts(TryllGraphDescription graph)
        {
            if (graph?.Nodes == null) return false;
            foreach (var node in graph.Nodes)
                if (node?.Params != null && node.Params.IsTtsConsumer)
                    return true;
            return false;
        }

        private void AddNodeHandler<T>(ref T backing, T handler, Action<TryllAgent, T> subscribe)
            where T : Delegate
        {
            if (handler == null) return;
            backing = (T)Delegate.Combine(backing, handler);
            if (Agent != null)
                subscribe(Agent, handler);
        }

        private void RemoveNodeHandler<T>(ref T backing, T handler, Action<TryllAgent, T> unsubscribe)
            where T : Delegate
        {
            if (handler == null) return;
            backing = (T)Delegate.Remove(backing, handler);
            if (Agent != null)
                unsubscribe(Agent, handler);
        }

        private void AttachNodeEventHandlers()
        {
            if (Agent == null) return;
            if (_toolCallNotification != null) Agent.ToolCallNotification += _toolCallNotification;
            if (_toolCallNotificationWithId != null) Agent.ToolCallNotificationWithId += _toolCallNotificationWithId;
            if (_toolCallDetailed != null) Agent.ToolCallDetailed += _toolCallDetailed;
            if (_intentClassified != null) Agent.IntentClassified += _intentClassified;
            if (_intentLlmClassified != null) Agent.IntentLlmClassified += _intentLlmClassified;
            if (_paused != null) Agent.Paused += _paused;
            if (_nodeEvent != null) Agent.NodeEvent += _nodeEvent;
        }

        private void DetachNodeEventHandlers()
        {
            if (Agent == null) return;
            // Remove each handler on its own. Delegate.Remove only strips a
            // combined list when that exact run is contiguous, and a direct
            // subscriber on Agent can sit between two component handlers.
            UnsubscribeEach(_toolCallNotification, handler => Agent.ToolCallNotification -= handler);
            UnsubscribeEach(_toolCallNotificationWithId, handler => Agent.ToolCallNotificationWithId -= handler);
            UnsubscribeEach(_toolCallDetailed, handler => Agent.ToolCallDetailed -= handler);
            UnsubscribeEach(_intentClassified, handler => Agent.IntentClassified -= handler);
            UnsubscribeEach(_intentLlmClassified, handler => Agent.IntentLlmClassified -= handler);
            UnsubscribeEach(_paused, handler => Agent.Paused -= handler);
            UnsubscribeEach(_nodeEvent, handler => Agent.NodeEvent -= handler);
        }

        private static void UnsubscribeEach<T>(T combined, Action<T> unsubscribe) where T : Delegate
        {
            if (combined == null) return;
            foreach (Delegate entry in combined.GetInvocationList())
                unsubscribe((T)entry);
        }

        /// <summary>
        /// Dispose the current agent and send DestroyAgent to the server.
        /// Safe to call when no agent exists.
        /// </summary>
        public void DestroyAgent()
        {
            InvalidateCreateState();
            ClearSendQueue();
            if (Agent == null) return;
            DetachNodeEventHandlers();
            Agent.SetOnAnswerText(null);
            Agent.SetOnTurnComplete(null);
            Agent.SetOnError(null);
            Agent.SetOnTtsAudioFormat(null);
            Agent.SetOnTtsAudio(null);
            Agent.Dispose();
            Agent = null;
            OnAgentDestroyed.Invoke();
        }

        /// <summary>
        /// Send a turn message to the agent.
        ///
        /// <para><b>The message is queued, not dropped, when the agent cannot accept it yet.</b> An
        /// agent refuses a send with <see cref="TryllErrorCode.AgentBusy"/> while a turn is running or
        /// a KV-cache operation is in flight — and a cache operation is exactly what happens when a
        /// game prefills the agent the player has just selected. Without queueing, a message typed
        /// during that window is silently lost and the player must retype it, which is the worst
        /// available outcome. The queue is flushed automatically when the agent becomes ready, when a
        /// turn completes, and when a cache operation finishes.</para>
        ///
        /// <para>Only the most recent message is held: if two are sent before either reaches the
        /// agent, the newer line is the one the player meant. <see cref="OnMessageQueued"/> fires when
        /// a send is deferred, so a game can show that it is waiting.</para>
        ///
        /// <para>Still reports <see cref="TryllErrorCode.InvalidAgentId"/> when there is no agent at
        /// all — that is a programming error rather than a busy window, and queueing it would hide the
        /// mistake.</para>
        /// </summary>
        public new void SendMessage(string text)
        {
            // An empty turn is not a message — the server rejects it with MalformedMessage.
            if (string.IsNullOrEmpty(text)) return;

            if (Agent == null || !Agent.IsValid)
            {
                TryllLog.Warning("TryllAgentComponent.SendMessage: no agent ready");
                OnError.Invoke(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return;
            }

            _pendingMessage = text;
            if (!TryFlushPendingMessage())
                OnMessageQueued.Invoke(text);
        }

        /// <summary>
        /// Hands the queued message to the agent if it can accept one now. Safe and cheap to call
        /// repeatedly; returns true only when a message was actually handed over.
        ///
        /// <para>Called automatically on agent-ready, turn-complete and cache-operation-complete. It
        /// is public because a KV-residency policy that drives prefills through
        /// <see cref="TryllAgent"/> directly — rather than through this component — closes a busy
        /// window this component never observed, and should nudge the queue when it does.</para>
        /// </summary>
        public bool TryFlushPendingMessage()
        {
            // Empty means "nothing queued": Unity's assembly-reload backup can revive a
            // null private string as "", and flushing that on agent-ready sent a bogus
            // empty turn the server rejected as MalformedMessage.
            if (string.IsNullOrEmpty(_pendingMessage))
            {
                _pendingMessage = null;
                return false;
            }
            if (Agent == null || !Agent.IsValid || _turnInFlight) return false;

            var text = _pendingMessage;
            _pendingMessage = null;
            _inFlightMessage = text;
            _turnInFlight = true;
            Agent.SendMessage(text);
            OnMessageDequeued.Invoke(text);
            return true;
        }

        /// <summary>True while a message is waiting for the agent to become available.</summary>
        public bool HasPendingMessage => !string.IsNullOrEmpty(_pendingMessage);

        /// <summary>True while a turn this component started has not yet completed.</summary>
        public bool IsTurnInFlight => _turnInFlight;

        /// <summary>The queued message, or null. For a "waiting to send" indicator.</summary>
        public string PendingMessage => string.IsNullOrEmpty(_pendingMessage) ? null : _pendingMessage;

        /// <summary>
        /// Cooperatively cancel the in-flight turn. The turn ends with
        /// <see cref="TryllTurnStatus.Cancelled"/> via <see cref="OnTurnComplete"/>.
        /// No-op if no agent/turn is active.
        /// </summary>
        public void Cancel(TryllCancelMode mode = TryllCancelMode.StopAndKeep)
        {
            if (!HasAgent) return;
            Agent.Cancel(mode);
        }

        /// <summary>
        /// Apply typed node parameters to a workflow node at runtime.
        /// Allowed while the agent is idle or paused (rejected with
        /// <see cref="TryllErrorCode.AgentBusy"/> only while a turn is actively
        /// running between pauses).
        ///
        /// Clone-set-send idiom:
        /// <code>
        ///   var p = (TryllGenerateParams)GetNodeParamsBaseline("gen");
        ///   p.SystemPrompt = "New system prompt";
        ///   ChangeParams("gen", p, err => { if (!err.IsOk) Debug.LogError(err); });
        /// </code>
        ///
        /// Completes via <see cref="OnParamChanged"/> and the optional
        /// <paramref name="onComplete"/> callback (exactly once per call).
        /// When no agent is ready, reports <see cref="TryllErrorCode.InvalidAgentId"/>
        /// through both channels immediately.
        /// </summary>
        public void ChangeParams(
            string nodeName,
            TryllNodeParamsBase @params,
            Action<TryllError> onComplete = null)
        {
            if (!HasAgent)
            {
                var err = new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready");
                TryllLog.Warning("TryllAgentComponent.ChangeParams: no agent ready");
                ReportParamChanged(err, onComplete);
                return;
            }
            Agent.ChangeParams(nodeName, @params, err => ReportParamChanged(err, onComplete));
        }

        /// <summary>
        /// Resume a turn that is currently paused (see <see cref="TryllClient.Paused"/>).
        /// Empty <paramref name="resumeNode"/> continues via the paused node's pending
        /// exit route; non-empty jumps to that node by name.
        ///
        /// Completes via <see cref="OnResumed"/> and the optional
        /// <paramref name="onComplete"/> callback (exactly once per call).
        /// When no agent is ready, reports <see cref="TryllErrorCode.InvalidAgentId"/>
        /// through both channels immediately.
        /// </summary>
        public void Resume(string resumeNode = "", Action<TryllError> onComplete = null)
        {
            if (!HasAgent)
            {
                var err = new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready");
                TryllLog.Warning("TryllAgentComponent.Resume: no agent ready");
                ReportResumed(err, onComplete);
                return;
            }
            Agent.Resume(resumeNode, err => ReportResumed(err, onComplete));
        }

        /// <summary>Resume with one result correlated to a tool-call ID.</summary>
        public void ResumeWithToolResult(
            TryllToolResult toolResult,
            string resumeNode = "",
            Action<TryllError> onComplete = null)
        {
            if (!HasAgent)
            {
                var err = new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready");
                ReportResumed(err, onComplete);
                return;
            }
            Agent.ResumeWithToolResult(
                toolResult, resumeNode, err => ReportResumed(err, onComplete));
        }

        /// <summary>Resume with the complete result batch for the paused pass.</summary>
        public void ResumeWithToolResults(
            IReadOnlyList<TryllToolResult> toolResults,
            string resumeNode = "",
            Action<TryllError> onComplete = null)
        {
            if (!HasAgent)
            {
                var err = new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready");
                ReportResumed(err, onComplete);
                return;
            }
            Agent.ResumeWithToolResults(
                toolResults, resumeNode, err => ReportResumed(err, onComplete));
        }

        /// <summary>
        /// Register main-thread auto-resume sugar. Registrations made before
        /// agent creation are applied when the agent becomes ready.
        /// </summary>
        public void RegisterTool(string toolName, Func<TryllToolCall, string> handler)
        {
            if (string.IsNullOrEmpty(toolName))
                throw new ArgumentException("Tool name must not be empty", nameof(toolName));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            _toolHandlers[toolName] = handler;
            _asyncToolHandlers.Remove(toolName);
            Agent?.RegisterTool(toolName, handler);
        }

        public void RegisterTool(string toolName, Func<TryllToolCall, Task<string>> handler)
        {
            if (string.IsNullOrEmpty(toolName))
                throw new ArgumentException("Tool name must not be empty", nameof(toolName));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            _asyncToolHandlers[toolName] = handler;
            _toolHandlers.Remove(toolName);
            Agent?.RegisterTool(toolName, handler);
        }

        public bool UnregisterTool(string toolName)
        {
            Agent?.UnregisterTool(toolName);
            _asyncToolHandlers.Remove(toolName ?? "");
            return _toolHandlers.Remove(toolName ?? "");
        }

        /// <summary>
        /// Look up a registered tool handler without going through an agent. Lets an
        /// external driver that runs its own agent — the Dialog Lab — call the same
        /// game-side handler this component would have used.
        ///
        /// <para>A handler that keeps per-conversation state must be told when the
        /// dialogue restarts, and such a driver's agent is not this component's, so none
        /// of the events here fire for it. The convention is a <c>ResetToolState()</c>
        /// method on the handler's component, which the driver reaches with
        /// <c>gameObject.SendMessage</c>. In-game and Chat-window callers need nothing:
        /// the handler subscribes to <see cref="OnAgentCreated"/> and
        /// <see cref="OnInteractionsRemoved"/> itself.</para>
        /// </summary>
        public bool TryGetToolHandler(string toolName, out Func<TryllToolCall, string> handler)
        {
            handler = null;
            if (string.IsNullOrEmpty(toolName)) return false;
            return _toolHandlers.TryGetValue(toolName, out handler);
        }

        /// <summary>See <see cref="TryllAgent.HasPausedToolBatch"/>. False when no agent is ready.</summary>
        public bool HasPausedToolBatch => HasAgent && Agent.HasPausedToolBatch;

        /// <summary>See <see cref="TryllAgent.RetryPausedToolBatch"/>. No-op when no agent is ready.</summary>
        public void RetryPausedToolBatch()
        {
            if (!HasAgent) return;
            Agent.RetryPausedToolBatch();
        }

        public void PrefillKvCache()
        {
            if (!HasAgent) { OnKvCachePrefilled.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.PrefillKvCache((result, error) =>
            {
                OnKvCachePrefilled.Invoke(result, error);
                TryFlushPendingMessage(); // the busy window this opened has closed
            });
        }

        public void EvictKvCache()
        {
            if (!HasAgent) { OnKvCacheEvicted.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.EvictKvCache((result, error) =>
            {
                OnKvCacheEvicted.Invoke(result, error);
                TryFlushPendingMessage(); // the busy window this opened has closed
            });
        }

        /// <summary>
        /// Change this agent's voice to <paramref name="profile"/> and, on success, make it
        /// the component's <see cref="Voice"/> (so a re-created agent keeps it). See
        /// <see cref="TryllAgent.SetVoiceAsync(TryllVoiceProfileData)"/> for admission rules.
        /// With no agent yet, the profile is only stored and applies at the next
        /// <see cref="CreateAgent"/> (returns Ok); while a create is in flight it is refused
        /// with <see cref="TryllErrorCode.AgentBusy"/> because that create already captured
        /// the previous voice.
        /// </summary>
        public async Task<TryllError> SetVoiceAsync(TryllVoiceProfile profile)
        {
            if (profile == null || profile.Data == null)
                return new TryllError(TryllErrorCode.InvalidVoiceProfile, "Voice profile is null");
            if (!HasAgent)
            {
                if (_createInFlight)
                    return new TryllError(TryllErrorCode.AgentBusy,
                        "Agent creation in flight; set the voice after OnAgentCreated");
                voice = profile;
                return TryllError.Ok();
            }
            var agent = Agent;
            var err = await agent.SetVoiceAsync(profile.Data);
            if (err.IsOk && ReferenceEquals(agent, Agent))
                voice = profile;
            TryFlushPendingMessage(); // the busy window this opened has closed
            return err;
        }

        /// <summary>
        /// Switch the live agent's delivery mode. See
        /// <see cref="TryllAgent.SetDeliveryModeAsync"/>. Reports
        /// <see cref="TryllErrorCode.InvalidAgentId"/> when no agent is ready.
        /// </summary>
        public Task<TryllError> SetDeliveryModeAsync(string mode, float intensity = 1f)
        {
            if (!HasAgent)
                return Task.FromResult(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
            return Agent.SetDeliveryModeAsync(mode, intensity);
        }

        public void GetKvCacheStatus()
        {
            if (!HasAgent) { OnKvCacheStatus.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.GetKvCacheStatus((result, error) => OnKvCacheStatus.Invoke(result, error));
        }

        public void AppendInteractions(IReadOnlyList<TryllDialogInteraction> interactions)
        {
            if (!HasAgent) { OnInteractionsAppended.Invoke(0, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.AppendInteractions(interactions, (count, error) => OnInteractionsAppended.Invoke(count, error));
        }

        public void RemoveInteractionsFromEnd(int count)
        {
            if (!HasAgent) { OnInteractionsRemoved.Invoke(0, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.RemoveInteractionsFromEnd(count, (removed, error) => OnInteractionsRemoved.Invoke(removed, error));
        }

        public void ExportDialog()
        {
            if (!HasAgent) { OnDialogExported.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.ExportDialog((snapshot, error) => OnDialogExported.Invoke(snapshot, error));
        }

        public void ImportDialog(
            TryllDialogSnapshot snapshot,
            TryllDialogCompatibility compatibility = TryllDialogCompatibility.Strict)
        {
            if (!HasAgent) { OnDialogImported.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.ImportDialog(snapshot, compatibility, (result, error) => OnDialogImported.Invoke(result, error));
        }

        public void SaveDialog(string path, string saveId)
        {
            if (!HasAgent) { OnDialogSaved.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.SaveDialog(path, saveId, (result, error) => OnDialogSaved.Invoke(result, error));
        }

        public void LoadDialog(
            string path,
            TryllDialogCompatibility compatibility = TryllDialogCompatibility.Strict)
        {
            if (!HasAgent) { OnDialogLoaded.Invoke(null, new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready")); return; }
            Agent.LoadDialog(path, compatibility, (result, error) => OnDialogLoaded.Invoke(result, error));
        }

        /// <summary>
        /// Return a deep copy of the last authored params for a named node, or null
        /// if no agent is ready / the node name is unknown.
        /// Use as the starting point for a clone-set-send mutation — the returned
        /// object is already a clone, so mutate it in place then send via
        /// <see cref="ChangeParams"/>.
        /// </summary>
        public TryllNodeParamsBase GetNodeParamsBaseline(string nodeName)
        {
            if (!HasAgent) return null;
            return Agent.GetNodeParamsBaseline(nodeName);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void ReportParamChanged(TryllError error, Action<TryllError> onComplete)
        {
            OnParamChanged.Invoke(error);
            onComplete?.Invoke(error);
        }

        private void ReportResumed(TryllError error, Action<TryllError> onComplete)
        {
            OnResumed.Invoke(error);
            onComplete?.Invoke(error);
        }

        private void OnCreateSessionComplete(TryllError error)
        {
            // Session is now live — safe to create the agent.
            if (error.IsOk && AutoCreateOnConnect && !HasAgent)
                CreateAgent();
        }

        private void OnConnectionChanged(bool isConnected)
        {
            // Do NOT create on raw connect: the session does not exist yet (protocol
            // v4).  Agent creation is driven by OnCreateSessionComplete.
            if (!isConnected)
            {
                InvalidateCreateState();
                ClearSendQueue();
            }
            if (!isConnected && HasAgent)
            {
                // Server disconnected — clear local state without sending DestroyAgent
                // (the TCP connection is gone, so there is nothing to send).
                DetachNodeEventHandlers();
                Agent.SetOnAnswerText(null);
                Agent.SetOnTurnComplete(null);
                Agent.SetOnError(null);
                Agent.SetOnTtsAudioFormat(null);
                Agent.SetOnTtsAudio(null);
                Agent = null;
                OnAgentDestroyed.Invoke();
            }
        }

        private void HandleAnswerText(string nodeName, string text, bool isDelta, bool isFinal)
            => OnAnswerText.Invoke(nodeName, text, isDelta, isFinal);

        private void HandleTurnComplete(TryllTurnStatus status, string debugInfo, int tokensGenerated)
        {
            _turnInFlight = false;
            _inFlightMessage = null;
            OnTurnComplete.Invoke(status, debugInfo, tokensGenerated);

            // A line sent during the turn goes out now rather than waiting for anything else.
            TryFlushPendingMessage();
        }

        private void HandleError(TryllError error)
        {
            // A send refused because the agent was busy must not be lost. A rejection is
            // ErrorResponse only — no TurnComplete follows. Re-queue the line and wait for
            // TryllKvResidencyPolicy to call TryFlushPendingMessage when its prefill finishes,
            // or for the next real TurnComplete. Do not resend from here.
            if (error.Code == (int)TryllErrorCode.AgentBusy && !string.IsNullOrEmpty(_inFlightMessage))
            {
                // Keep a newer queued line if there is one — it is the more recent intent.
                if (string.IsNullOrEmpty(_pendingMessage))
                    _pendingMessage = _inFlightMessage;
                _inFlightMessage = null;
                _turnInFlight = false;
                OnMessageQueued.Invoke(_pendingMessage);
            }
            else if (error.Code == (int)TryllErrorCode.FrameTooLarge && _turnInFlight)
            {
                // Local size reject: no TurnComplete follows. Drop the oversized
                // line and release the busy window so a newer queued message can go.
                _inFlightMessage = null;
                _turnInFlight = false;
                TryFlushPendingMessage();
            }

            OnError.Invoke(error);
        }

        private void ClearCreateState()
        {
            _createInFlight = false;
            _activeCreateUsesTransientDiagnostics = false;
            _transientDiagnosticsStaged = false;
        }

        private void InvalidateCreateState()
        {
            _createGeneration++;
            ClearCreateState();
        }

        private void ClearSendQueue()
        {
            _pendingMessage = null;
            _inFlightMessage = null;
            _turnInFlight = false;
        }
    }
}
