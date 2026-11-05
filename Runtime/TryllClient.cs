// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.FlatBuffers;
using UnityEngine;
using Tryll;
using Tryll.Client.Internal;

namespace Tryll.Client
{
    /// <summary>Compact agent descriptor for Agent Log registration snapshots (§7.5).</summary>
    internal readonly struct TryllAgentSnapshot
    {
        public ulong AgentId { get; }
        public string LabelHint { get; }
        public bool CreatedWithDiagnostics { get; }
        public bool CreatedBeforeDiagnosticsCapture { get; }

        public TryllAgentSnapshot(ulong agentId, string labelHint, bool createdWithDiagnostics,
            bool createdBeforeDiagnosticsCapture)
        {
            AgentId = agentId;
            LabelHint = labelHint;
            CreatedWithDiagnostics = createdWithDiagnostics;
            CreatedBeforeDiagnosticsCapture = createdBeforeDiagnosticsCapture;
        }
    }

    /// <summary>
    /// Singleton <see cref="MonoBehaviour"/> that manages the TCP session to the Tryll server.
    /// Mirrors UTryllSubsystem (UGameInstanceSubsystem + FTickableGameObject) from the Unreal plugin.
    ///
    /// Lifecycle:
    ///   1. The singleton is created automatically by <see cref="TryllClientModule"/> at game start.
    ///   2. Call <see cref="StartSession"/> to connect and configure the session in one step
    ///      (done automatically when <see cref="AutoStartSession"/> is on),
    ///      or do it manually: <see cref="Connect"/>, then <see cref="CreateSession"/>.
    ///   3. Hook <see cref="CreateSessionComplete"/> / <see cref="ConnectionChanged"/>, or poll
    ///      <see cref="IsSessionReady"/> / <see cref="IsConnected"/>.
    ///   4. Use <see cref="RequestCreateAgentAsync"/> to create agents, or attach a
    ///      <see cref="TryllAgentComponent"/> which does this automatically.
    ///   5. Call <see cref="Disconnect"/> when done.
    ///
    /// All events and <see cref="Task"/> completions fire on the Unity main thread.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [HelpURL(TryllDocs.TryllClient)]
    public sealed class TryllClient : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static TryllClient Instance { get; private set; }

        // ── Connection settings ────────────────────────────────────────────────

        public string ServerHost = "127.0.0.1";
        public int    ServerPort = 9100;

        /// <summary>
        /// When true, the client starts the session automatically on startup (in Awake):
        /// it connects and sends <c>CreateSession</c> from <see cref="Settings"/>,
        /// retrying failed connection attempts. Changing this at runtime has no effect —
        /// call <see cref="StartSession"/> directly instead.
        /// </summary>
        [Tooltip("Automatically start the session (connect + CreateSession from the settings asset) " +
                 "on startup.")]
        public bool AutoStartSession;

        [Tooltip("Optional settings asset override. Leave empty to load the default from Resources.")]
        [SerializeField] private TryllRuntimeSettings settings;

        /// <summary>
        /// Settings asset used for connection defaults and <see cref="StartSession"/>'s
        /// CreateSession parameters. Never returns null: when no override is assigned,
        /// the default asset is loaded via <see cref="TryllRuntimeSettings.Load"/>.
        /// Assigning while disconnected re-applies <see cref="ServerHost"/>/<see cref="ServerPort"/>.
        /// </summary>
        public TryllRuntimeSettings Settings
        {
            get => settings != null ? settings : TryllRuntimeSettings.Load();
            set
            {
                settings = value;
                if (!IsConnected) ApplyConnectionSettings();
                BindTextProvider();
            }
        }

        ITryllTextProvider _textProviderOverride;
        bool _textProviderOverridden;
        ITryllTextProvider _boundTextProvider;
        readonly TryllLocalizedAgentTracker _localizedAgents = new TryllLocalizedAgentTracker();
        bool _localizedRefreshRunning;
        bool _localizedRefreshQueued;
        // CreateSession was given a locale. A provider language change then
        // leaves the session language alone.
        bool _sessionLocaleExplicit;

        /// <summary>
        /// Resolves Text IDs before <see cref="RequestCreateAgentAsync"/> and again
        /// when its language changes. A value set here, including null, wins over
        /// <see cref="TryllRuntimeSettings.TextProvider"/>. Leave it unset to use
        /// the settings asset.
        /// </summary>
        public ITryllTextProvider TextProvider
        {
            get => _textProviderOverridden ? _textProviderOverride : Settings.TextProvider;
            set
            {
                _textProviderOverride = value;
                _textProviderOverridden = true;
                BindTextProvider();
            }
        }

        // ── Internal state ─────────────────────────────────────────────────────

        internal TryllConnection Connection { get; private set; }

        internal TryllDiagnosticsDemand DiagnosticsDemand { get; } = new TryllDiagnosticsDemand();

        [Flags]
        private enum SessionState
        {
            None              = 0,
            /// <summary>StartSession() owns bring-up (configure on connect, retry) until Disconnect().</summary>
            StartRequested    = 1 << 0,
            /// <summary>A CreateSession request is in flight (sent, no response yet).</summary>
            ConfigureInFlight = 1 << 1,
            /// <summary>A CreateSession request succeeded on the live connection.</summary>
            Configured        = 1 << 2,
        }

        private SessionState _sessionState = SessionState.None;
        private bool _announcedConnected;

        private bool HasState(SessionState flag)   => (_sessionState & flag) != 0;
        private void SetState(SessionState flag)   => _sessionState |= flag;
        private void ClearState(SessionState flag) => _sessionState &= ~flag;

        private Coroutine _connectRetryCoroutine;

        /// <summary>Seconds between reconnect attempts while <see cref="StartSession"/> is pending.</summary>
        private const float ConnectRetrySeconds = 2f;

        // Editor-held cold-start retry (not Play Mode StartSession). Scheduled
        // from PumpEvents so Edit Mode does not depend on coroutines.
        private int _remainingConnectAttempts;
        private bool _editorConnectRetryPending;
        private float _connectRetryDueRealtime;

        private readonly Queue<Action> _mainThreadContinuations = new Queue<Action>();
        private readonly object _mainThreadContinuationsLock = new object();

        // Live agents keyed by agent_id.
        private readonly Dictionary<ulong, TryllAgent> _agentsById = new Dictionary<ulong, TryllAgent>();

        // Live VoiceInput handles keyed by voice_input_id.
        private readonly Dictionary<ulong, TryllVoiceInput> _voiceInputsById = new Dictionary<ulong, TryllVoiceInput>();

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired when the connection state changes. Parameter: isConnected.</summary>
        public event Action<bool> ConnectionChanged;

        /// <summary>Fired on session-level or unroutable errors.</summary>
        public event Action<TryllError> Error;

        /// <summary>
        /// Fired on the main thread, once per agent and language change, when the
        /// change could not update that agent's Localized prompts. If a Text ID is
        /// missing in the new language, nothing was sent: the agent keeps its
        /// previous prompts in full. If the server rejected an update, the error
        /// is the first rejection; other nodes may already run the new language. The
        /// session language has changed either way. Recreate the agent or switch
        /// the language back.
        /// </summary>
        public event Action<TryllAgent, TryllError> LocalizedTextFailed;

        /// <summary>
        /// Fired on the main thread when a language change reached an agent: the
        /// server acknowledged every Localized prompt that changed, or none needed
        /// to change. Wait for this before work that must see the new prompts. An
        /// <see cref="TryllErrorCode.AgentBusy"/> reply delays it until the running
        /// turn ends.
        /// </summary>
        public event Action<TryllAgent> LocalizedTextApplied;

        /// <summary>
        /// Fired when a CreateSession response arrives. On success (<c>error.IsOk</c>)
        /// the session is ready (<see cref="IsSessionReady"/>).
        /// </summary>
        public event Action<TryllError> CreateSessionComplete;

        /// <summary>Fired when a model download progress update arrives (name, bytesDownloaded, totalBytes, percent).</summary>
        public event Action<string, ulong, ulong, float> DownloadProgress;

        /// <summary>Fired when a model download completes (name, success, error).</summary>
        public event Action<string, bool, string> DownloadComplete;

        /// <summary>Fired when LoadModel completes.</summary>
        public event Action<string, bool> LoadModelComplete;

        /// <summary>Fired when UnloadModel completes.</summary>
        public event Action<string, bool> UnloadModelComplete;

        /// <summary>
        /// Fired when <see cref="RequestSetSessionLocale"/> completes. The string
        /// is the requested tag (empty when clearing); the bool is success.
        /// A failure means the tag was malformed — the previous locale is
        /// untouched.
        /// </summary>
        public event Action<string, bool> SetSessionLocaleComplete;

        /// <summary>Fired when DeleteModel completes (name, success). Editor Model Manager only.</summary>
        internal event Action<string, bool> DeleteModelComplete;

        /// <summary>Fired when an agent is destroyed by the server.</summary>
        public event Action<ulong> AgentDestroyed;

        /// <summary>Fired when a NodeEvent with event_type="tool_call" arrives for any agent.</summary>
        /// <remarks>Args: (agentId, toolName, argumentsJson).</remarks>
        public event Action<ulong, string, string> ToolCallNotification;

        /// <summary>Call-ID-aware tool-call notification.</summary>
        /// <remarks>Args: (agentId, callId, toolName, argumentsJson).</remarks>
        public event Action<ulong, string, string, string> ToolCallNotificationWithId;

        /// <summary>
        /// Detailed tool-call notification carrying the full owning <see cref="TryllToolCall"/>
        /// value object (node name and disposition included) alongside the agent id. Fired
        /// in addition to — never instead of — <see cref="ToolCallNotification"/> and
        /// <see cref="ToolCallNotificationWithId"/>, so a manual handler can tell whether to
        /// send results for <c>await_result</c> or plain-resume for other pausing dispositions.
        /// </summary>
        /// <remarks>Args: (agentId, call).</remarks>
        public event Action<ulong, TryllToolCall> ToolCallDetailed;

        /// <summary>
        /// Embedding ClassifyIntent notification (<c>event_type="intent_classified"</c>).
        /// Subscribing swallows the generic <see cref="NodeEvent"/> fallback.
        /// The agent id is the first argument; the payload does not repeat it.
        /// Per-agent games should subscribe on <see cref="TryllAgent"/> instead.
        /// </summary>
        /// <remarks>Args: (agentId, classification).</remarks>
        public event Action<ulong, TryllIntentClassification> IntentClassified;

        /// <summary>
        /// ClassifyIntentLLM notification (<c>event_type="intent_llm_classified"</c>).
        /// Subscribing swallows the generic <see cref="NodeEvent"/> fallback.
        /// </summary>
        /// <remarks>Args: (agentId, classification).</remarks>
        public event Action<ulong, TryllIntentLlmClassification> IntentLlmClassified;

        /// <summary>
        /// Fired when a NodeEvent with event_type="paused" arrives for any agent — the
        /// executor paused the turn between nodes (see the Pause node and
        /// ToolCallDisposition Pause / PauseAndAcknowledge / AwaitResult).
        /// ChangeParams is allowed while paused.
        /// </summary>
        /// <remarks>Args: (agentId, nodeName, pendingExit).</remarks>
        public event Action<ulong, string, string> Paused;

        /// <summary>
        /// Generic NodeEvent fallback — fired when no typed handler exists for the
        /// arriving event_type, or when the typed event has no subscriber.
        /// </summary>
        /// <remarks>Args: (agentId, nodeName, eventType, kvPairs).</remarks>
        public event Action<ulong, string, string, IReadOnlyList<KeyValuePair<string, string>>> NodeEvent;

        /// <summary>Fired when a named StringStorage is created or destroyed.</summary>
        public event Action<string, bool> StringStorageChanged;

        /// <summary>Fired when a named EmbeddedStringStorage is created or destroyed.</summary>
        public event Action<string, int, bool> EmbeddedStringStorageChanged;

        // ── Info struct (EmbeddedStorage) ──────────────────────────────────────

        public struct EmbeddedStorageInfo
        {
            public string Name;
            public uint   RecordCount;
            public uint   EmbeddingDim;
        }

        // ── Unity lifecycle ────────────────────────────────────────────────────

        /// <summary>
        /// Set by <see cref="TryllClientModule"/> on the auto-created singleton. A
        /// module-created client carries default configuration only, so a scene-placed
        /// duplicate may transfer its inspector configuration onto it (see Awake).
        /// </summary>
        internal bool CreatedByModule;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Outside Play the singleton is usually the hidden editor client of a
                // Tryll window. DestroyImmediate here would delete the scene's
                // TryllClient from the open scene, and its inspector configuration with
                // it, before Play ever starts.
                if (!Application.isPlaying)
                    return;

                // The module auto-creates the singleton at BeforeSceneLoad, so a
                // scene-placed TryllClient always loses this race — but it is the one
                // carrying the user's inspector configuration (AutoStartSession,
                // Settings override). Transfer it to the default singleton, unless a
                // session is already live or starting.
                if (Instance.CreatedByModule && !Instance.IsConnected && !Instance.IsSessionStartPending)
                    Instance.AdoptSceneConfiguration(this);
                else
                    TryllLog.Warning($"TryllClient on '{gameObject.name}' is a duplicate of the live '{Instance.gameObject.name}' " +
                                     "and was destroyed without applying its inspector configuration.");

                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            Connection = new TryllConnection();

            ApplyConnectionSettings();

#if UNITY_EDITOR
            // [ExecuteAlways] Update() only ticks when the editor decides to run
            // the player loop (mouse over editor, window move, asset import, …).
            // EditorApplication.update fires every editor frame regardless, so
            // it's the reliable pump for events arriving from the socket worker
            // outside Play mode.
            UnityEditor.EditorApplication.update += PumpEvents;
#endif

            if (Application.isPlaying && AutoStartSession)
                StartSession();

            BindTextProvider();
        }

        private void ApplyConnectionSettings()
        {
            var s = Settings;
            ServerHost = s.ServerHost;
            ServerPort = s.ServerPort;
        }

        /// <summary>
        /// Take over the inspector configuration of a scene-placed duplicate that is
        /// about to be destroyed, then auto-start if it asked for that.
        /// </summary>
        internal void AdoptSceneConfiguration(TryllClient sceneClient)
        {
            settings         = sceneClient.settings;
            AutoStartSession = sceneClient.AutoStartSession;
            // Without an override, host/port already hold the module's values, including
            // the port TryLaunch picked; re-reading the default asset would reset a
            // player build's ephemeral port to the asset's fixed one.
            if (settings != null)
            {
                ServerHost = settings.ServerHost;
                ServerPort = settings.AutoLaunchServer && TryllServerProcess.ActualPort > 0
                    ? TryllServerProcess.ActualPort
                    : settings.ServerPort;
            }
            BindTextProvider();

            if (Application.isPlaying && AutoStartSession)
                StartSession();
        }

        /// <summary>
        /// Ensure a <see cref="TryllClient"/> singleton exists outside Play mode (e.g. for
        /// editor-only tools). In Play mode the singleton is created by
        /// <see cref="TryllClientModule"/> at <c>BeforeSceneLoad</c>; this helper exists for
        /// editor windows that need to drive the client without entering Play mode.
        /// </summary>
        public static TryllClient EnsureEditorInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[TryllClient (Editor)]")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            go.AddComponent<TryllClient>();
            return Instance;
        }

        private void OnDestroy()
        {
            UnbindTextProvider();
            if (Instance == this) Instance = null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= PumpEvents;
#endif
            TeardownLiveSession(
                new TryllError(TryllErrorCode.ConnectionDropped, "Connection dropped"),
                clearStartRequested: true,
                notifyDisconnected: true);
            Connection?.Stop();
            Connection?.DiscardQueuedTransport();
        }

        private void Update()
        {
            if (Instance == this)
                PumpEvents();
        }

        /// <summary>
        /// Drain queued connection events on the calling thread.  Normally invoked by
        /// Unity via <c>Update()</c>; exposed for EditMode tests where MonoBehaviour
        /// updates are not delivered while a test enumerator is running.
        /// </summary>
        internal void PumpEvents()
        {
            BindTextProvider();
            if (Connection != null)
            {
                while (Connection.TryDequeueEvent(out var evt))
                {
                    try
                    {
                        DispatchEvent(evt);
                    }
                    catch (System.Exception ex)
                    {
                        // One throwing completion must not stall the rest of the queue.
                        UnityEngine.Debug.LogException(ex);
                    }
                }
            }
            DrainMainThreadContinuations();
            Connection?.ObserverHub?.Pump(64);
            MaybeFireEditorConnectRetry();
        }

        internal void PostToMainThread(Action action)
        {
            if (action == null) return;
            lock (_mainThreadContinuationsLock)
                _mainThreadContinuations.Enqueue(action);
        }

        private void DrainMainThreadContinuations()
        {
            while (true)
            {
                Action action;
                lock (_mainThreadContinuationsLock)
                {
                    if (_mainThreadContinuations.Count == 0) return;
                    action = _mainThreadContinuations.Dequeue();
                }
                action();
            }
        }

        /// <summary>EditMode helper: raise CreateSessionComplete without a wire reply.</summary>
        internal void DebugRaiseCreateSessionCompleteForTests(TryllError error)
        {
            CreateSessionComplete?.Invoke(error);
        }

        /// <summary>EditMode helper: set session bits without opening a socket.</summary>
        internal void DebugForceSessionFlagsForTests(bool startRequested, bool configured = false)
        {
            _sessionState = SessionState.None;
            if (startRequested) SetState(SessionState.StartRequested);
            if (configured) SetState(SessionState.Configured);
        }

        // ── Connection management ──────────────────────────────────────────────

        /// <summary>
        /// True while an editor connect attempt still has retry budget after the
        /// latest <c>ConnectFailed</c> (a retry is scheduled or about to be), or
        /// after <see cref="Connect"/> armed the budget and the first attempt has
        /// not yet exhausted it. False on success, after the budget is exhausted,
        /// after an established drop, or when Connect was never started.
        /// </summary>
        public bool HasConnectRetriesRemaining => _remainingConnectAttempts > 0;

        /// <summary>Open a TCP session to <see cref="ServerHost"/>:<see cref="ServerPort"/>.</summary>
        public void Connect()
        {
            if (IsConnected) return;
            // A second consumer joining a live attempt must not replace the worker
            // or reset the shared retry budget. A dying worker after a terminal
            // miss (budget already 0) must not swallow an explicit reconnect.
            if (_editorConnectRetryPending) return;
            if (Connection != null && Connection.IsWorkerAlive && _remainingConnectAttempts > 0)
                return;
            StartConnectAttempt(resetRetryBudget: true);
        }

        private void StartConnectAttempt(bool resetRetryBudget)
        {
            // A live socket already has (or is about to get) its own session epoch.
            // Replacing a dead/connecting worker must not inherit Configured from a
            // previous socket that never produced Disconnected.
            TeardownLiveSession(
                new TryllError(TryllErrorCode.ConnectionDropped, "Connection replaced"),
                clearStartRequested: false,
                notifyDisconnected: true);
            if (resetRetryBudget)
                ArmEditorConnectRetryBudget();
            var timeout = Settings.ConnectionReadyTimeoutSeconds;
            if (timeout <= 0.0) timeout = 10.0;
            Connection.Start(ServerHost, ServerPort, timeout);
        }

        /// <summary>
        /// Start the session in one call: connect to <see cref="ServerHost"/>:<see cref="ServerPort"/>
        /// and, once connected, send <c>CreateSession</c> using the engines from
        /// <see cref="Settings"/>. Failed connection attempts are retried every
        /// <see cref="ConnectRetrySeconds"/> seconds (Play mode only) until <see cref="Disconnect"/>
        /// is called. Invoked automatically at startup when <see cref="AutoStartSession"/> is enabled.
        /// Hook <see cref="CreateSessionComplete"/> or poll <see cref="IsSessionReady"/> to know
        /// when the session is ready for <see cref="RequestCreateAgentAsync"/>.
        /// </summary>
        public void StartSession()
        {
            SetState(SessionState.StartRequested);
            if (!IsConnected)
                Connect();
            else if (!HasState(SessionState.Configured) && !HasState(SessionState.ConfigureInFlight))
                CreateSessionFromSettings();
        }

        /// <summary>True when the session is connected and successfully configured.</summary>
        public bool IsSessionReady => IsConnected && HasState(SessionState.Configured);

        // ── Server identity (from ConnectionReady) ─────────────────────────────

        /// <summary>Canonical path of the connected server executable, or empty if unknown.</summary>
        public string ServerExePath { get; private set; } = string.Empty;
        /// <summary>Server build configuration ("Debug" | "Release" | "ClientDev" | "ClientProd").</summary>
        public string ServerBuildConfig { get; private set; } = string.Empty;
        /// <summary>Deployment mode ("tryll-dev" | "client-dev" | "client-prod").</summary>
        public string ServerMode { get; private set; } = string.Empty;
        /// <summary>Server version string.</summary>
        public string ServerVersion { get; private set; } = string.Empty;
        /// <summary>Per-process server run id (UUID).</summary>
        public string ServerRunId { get; private set; } = string.Empty;
        /// <summary>True when the server runs in managed (idle-self-exit) mode.</summary>
        public bool ServerManaged { get; private set; } = false;
        /// <summary>Server idle-shutdown grace period in seconds (0 when standalone).</summary>
        public uint ServerIdleTimeoutSeconds { get; private set; } = 0;

        /// <summary>
        /// Record the server identity advertised in ConnectionReady and verify it
        /// matches the build we intended to launch.
        ///
        /// When <see cref="TryllRuntimeSettings.AutoLaunchServer"/> is enabled we
        /// are the ones responsible for this server's lifecycle, so a mismatch is
        /// treated as fail-closed: log an error and disconnect rather than create
        /// a session against a stranger. This is a defense-in-depth backstop —
        /// <see cref="Internal.TryllServerProcess"/>'s known-port reuse probe
        /// already verifies identity before deciding to reuse a warm server, so
        /// this should only trip on a genuine race. Explicit connections (
        /// <see cref="Connect"/> called directly, <c>AutoLaunchServer=false</c>)
        /// remain advisory-only: the caller chose that target on purpose.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the mismatch caused a fail-closed disconnect (the
        /// caller must not proceed with session creation); <c>true</c> otherwise.
        /// </returns>
        private bool CaptureAndVerifyServerIdentity(TryllConnEvent e)
        {
            ServerExePath            = e.ServerExePath ?? string.Empty;
            ServerBuildConfig        = e.ServerBuildConfig ?? string.Empty;
            ServerMode               = e.ServerMode ?? string.Empty;
            ServerVersion            = e.ServerVersion ?? string.Empty;
            ServerRunId              = e.ServerRunId ?? string.Empty;
            ServerManaged            = e.ServerManaged;
            ServerIdleTimeoutSeconds = e.ServerIdleTimeoutSeconds;

            if (string.IsNullOrEmpty(ServerExePath))
                return true; // server predates the identity fields

            TryllLog.Info(
                $"Server identity: exe='{ServerExePath}' config={ServerBuildConfig} " +
                $"version={ServerVersion} mode={ServerMode} run_id={ServerRunId} managed={ServerManaged} " +
                $"idle_timeout={ServerIdleTimeoutSeconds}s");

            // exe_path check: did we connect to the build we intended to launch?
            // Relevant when auto-launch reused a server found on the known port.
            // The server reports a *canonical* path (symlinks/junctions resolved),
            // while our bundled .Server path is usually a symlink to the dev build.
            // Canonicalize both before comparing, or the symlink trips a false alarm.
            string intended = Internal.TryllServerProcess.ResolveExePath(Settings);
            if (string.IsNullOrEmpty(intended))
                return true; // nothing to compare against (e.g. no bundled server payload)

            string a = Internal.TryllPathUtil.Canonicalize(intended);
            string b = Internal.TryllPathUtil.Canonicalize(ServerExePath);
            if (string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (Settings.AutoLaunchServer)
            {
                TryllLog.Error(
                    $"Connected server exe '{ServerExePath}' does not match the intended " +
                    $"exe '{intended}' — a different server instance is holding the port. " +
                    "Disconnecting (AutoLaunchServer is enabled, so this connection must be ours).");
                Disconnect();
                return false;
            }

            TryllLog.Warning(
                $"Connected server exe '{ServerExePath}' does not match the " +
                $"intended exe '{intended}' — reusing a different server instance.");
            return true;
        }

        /// <summary>
        /// True while a <see cref="StartSession"/> bring-up is in progress: requested
        /// (incl. <see cref="AutoStartSession"/>) but not yet connected and configured.
        /// </summary>
        public bool IsSessionStartPending => HasState(SessionState.StartRequested) && !IsSessionReady;

        private void CreateSessionFromSettings()
        {
            var s = Settings;
            // One resolve for all three hint lists: a missing manifest logs an
            // error, and three identical errors would read like three faults.
            var manifest = TryllModelManifest.LoadOrNull();
            CreateSession(
                s.BuildEngineDeclarations(manifest),
                s.GameName,
                locale: null,
                placementHints: s.BuildPlacementHints(manifest),
                hotwordHints: s.BuildHotwordHints(manifest));
        }

        private System.Collections.IEnumerator ConnectRetry()
        {
            yield return new WaitForSeconds(ConnectRetrySeconds);
            _connectRetryCoroutine = null;
            if (!HasState(SessionState.StartRequested) || IsConnected) yield break;
            Connection.Stop();   // ensure the previous worker thread is gone
            Connect();
        }

        private void StopConnectRetry()
        {
            if (_connectRetryCoroutine != null)
            {
                StopCoroutine(_connectRetryCoroutine);
                _connectRetryCoroutine = null;
            }
        }

        private void ArmEditorConnectRetryBudget()
        {
            ClearEditorConnectRetry();
            int max = Settings != null ? Settings.ConnectMaxAttempts : 20;
            _remainingConnectAttempts = max < 1 ? 1 : max;
        }

        private void ClearEditorConnectRetry()
        {
            _editorConnectRetryPending = false;
            _connectRetryDueRealtime = 0f;
        }

        private void OnEditorConnectRetryStateChanged(bool connected)
        {
            if (connected)
            {
                ClearEditorConnectRetry();
                _remainingConnectAttempts = 0;
                return;
            }

            _remainingConnectAttempts--;
            if (_remainingConnectAttempts <= 0 || Connection == null)
            {
                ClearEditorConnectRetry();
                return;
            }

            float delay = Settings != null ? Settings.ConnectRetryDelaySeconds : 1f;
            if (delay < 0f) delay = 0f;
            _editorConnectRetryPending = true;
            _connectRetryDueRealtime = Time.realtimeSinceStartup + delay;
        }

        private void MaybeFireEditorConnectRetry()
        {
            if (!_editorConnectRetryPending) return;
            if (Time.realtimeSinceStartup < _connectRetryDueRealtime) return;
            if (IsConnected)
            {
                ClearEditorConnectRetry();
                return;
            }
            // Keep the scheduled retry until the previous worker actually exits.
            // Clearing pending first would lose the attempt while consumers still
            // wait on HasConnectRetriesRemaining (zero-delay or thread-exit race).
            if (Connection != null && Connection.IsWorkerAlive) return;
            if (_remainingConnectAttempts <= 0)
            {
                ClearEditorConnectRetry();
                return;
            }
            if (_connectionRefCount <= 0 && !HasState(SessionState.StartRequested))
            {
                ClearEditorConnectRetry();
                return;
            }
            _editorConnectRetryPending = false;
            Connection.Stop();
            StartConnectAttempt(resetRetryBudget: false);
        }

        private bool HasEditorConnectConsumers
            => _connectionRefCount > 0 || HasState(SessionState.StartRequested);

        /// <summary>Close the active session.</summary>
        public void Disconnect()
        {
            StopConnectRetry();
            ClearEditorConnectRetry();
            _remainingConnectAttempts = 0;
            TeardownLiveSession(
                new TryllError(TryllErrorCode.ConnectionDropped, "Connection dropped"),
                clearStartRequested: true,
                notifyDisconnected: true);
            Connection.Stop();
            Connection.DiscardQueuedTransport();
        }

        private int _connectionRefCount = 0;

        internal int DebugConnectionRefCountForTests => _connectionRefCount;
        internal int DebugRemainingConnectAttemptsForTests => _remainingConnectAttempts;
        internal bool DebugEditorConnectRetryPendingForTests => _editorConnectRetryPending;

        /// <summary>Arm editor retry budget without opening a socket.</summary>
        internal void DebugArmConnectRetryForTests(int attempts)
        {
            ClearEditorConnectRetry();
            _remainingConnectAttempts = attempts < 1 ? 1 : attempts;
        }

        /// <summary>Fire a pending editor retry immediately (no delay).</summary>
        internal void DebugFireScheduledConnectRetryForTests()
        {
            if (!_editorConnectRetryPending) return;
            _connectRetryDueRealtime = 0f;
            MaybeFireEditorConnectRetry();
        }

        /// <summary>
        /// Registers a consumer of this shared instance (PIE, an editor window, ...).
        /// Pair with <see cref="ReleaseConnection"/> so <see cref="Disconnect"/> only
        /// runs once every consumer has released it — one window closing (or Play
        /// mode stopping) must not disconnect another window still using it.
        /// </summary>
        public void AcquireConnection() => _connectionRefCount++;

        private object _playSessionOwner;
        private int _editorCallDepth;

        /// <summary>
        /// A game <see cref="RequestCreateAgentAsync"/> whose wire call is owned
        /// by the client, so a play-exit drain cannot drop the reply. Play exit
        /// fails the caller and marks the entry abandoned; a late success sends
        /// DestroyAgent and does not register the agent.
        /// </summary>
        private sealed class PlayCreate
        {
            public object Token;
            public TaskCompletionSource<(TryllAgent, TryllError)> Tcs;
            public bool Abandoned;
        }

        private readonly List<PlayCreate> _playCreates = new List<PlayCreate>();

        /// <summary>
        /// Play-mode calls use the play-session token. Editor windows call
        /// <see cref="AsEditorCall"/> so exiting play mode leaves them pending.
        /// </summary>
        internal object RequestOwner =>
            _editorCallDepth == 0 && _playSessionOwner != null ? _playSessionOwner : this;

        internal void BeginPlaySession()
        {
            if (_playSessionOwner == null)
                _playSessionOwner = new object();
        }

        internal void EndPlaySession()
        {
            var token = _playSessionOwner;
            _playSessionOwner = null;
            if (token != null)
            {
                Connection?.FailOwnedBy(
                    token,
                    new TryllError(TryllErrorCode.ConnectionDropped, "Play session ended"));
                AbandonPlayCreates(
                    token,
                    new TryllError(TryllErrorCode.ConnectionDropped, "Play session ended"));
            }
        }

        /// <summary>
        /// <paramref name="token"/> null abandons every tracked create. Play exit
        /// passes its token; connection teardown passes null so a reply that has
        /// already left the registry cannot register into the next epoch.
        /// </summary>
        private void AbandonPlayCreates(object token, TryllError error)
        {
            for (var i = 0; i < _playCreates.Count; i++)
            {
                var create = _playCreates[i];
                if (create.Abandoned)
                    continue;
                if (token != null && !ReferenceEquals(create.Token, token))
                    continue;
                create.Abandoned = true;
                create.Tcs.TrySetResult((null, error));
            }
        }

        /// <summary>
        /// A CreateAgent that finished after its play session ended. The agent
        /// is not registered; the server copy is destroyed on this client.
        /// </summary>
        private void DestroyAbandonedAgent(ulong agentId)
        {
            // Stop() clears IsConnected before a queued completion runs. Sending
            // then would ride the next socket, which is a different session.
            if (agentId == 0 || Connection == null || !Connection.IsConnected)
                return;
            ulong notedId = 0;
            TryllCalls.DestroyAgent(
                Connection, this, agentId,
                err =>
                {
                    NoteInbound(TryllObservationNote.Reply(MessageBody.DestroyAgentRequest, err.IsOk, notedId).Fill(n =>
                    {
                        n.AgentId = agentId;
                        if (!err.IsOk)
                        {
                            n.Message = err.Message;
                            n.Code = (uint)err.Code;
                        }
                    }));
                },
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.DestroyAgentRequest, rid).Fill(n => { n.AgentId = agentId; }));
                });
        }

        internal System.IDisposable AsEditorCall() => new EditorCallScope(this);

        private sealed class EditorCallScope : System.IDisposable
        {
            private TryllClient _client;

            public EditorCallScope(TryllClient client)
            {
                _client = client;
                _client._editorCallDepth++;
            }

            public void Dispose()
            {
                if (_client == null)
                    return;
                _client._editorCallDepth--;
                _client = null;
            }
        }

        /// <summary>Releases this caller's use of the shared instance; see <see cref="AcquireConnection"/>.</summary>
        public void ReleaseConnection()
        {
            if (_connectionRefCount == 0)
            {
                // A release without a matching acquire must be inert — hitting
                // Disconnect() here would tear down another consumer's live
                // connection (e.g. from a duplicated quitting-handler subscription).
                TryllLog.Warning("ReleaseConnection called without a matching AcquireConnection");
                return;
            }
            _connectionRefCount--;
            if (_connectionRefCount == 0) Disconnect();
        }

        public bool  IsConnected => Connection?.IsConnected ?? false;
        public ulong SessionId   => Connection?.SessionId   ?? 0;

        // ── Session configuration ──────────────────────────────────────────────

        /// <summary>
        /// Select the session's inference engine.
        /// </summary>
        /// <param name="locale">
        /// Session language as a BCP-47 tag (e.g. <c>"de"</c>, <c>"pt-BR"</c>) — see
        /// <see cref="TryllRuntimeSettings.Locale"/> for everything it drives.
        ///
        /// <c>null</c> (the default) means <b>use the project setting</b>, so a game
        /// that builds its own CreateSession call still honours Project Settings ▸
        /// Tryll Client without having to thread the value through. Pass an explicit
        /// <c>""</c> to force "no session language" and ignore the setting, or a tag
        /// to override it. A malformed tag is rejected with <c>InvalidLocale</c> (2004).
        /// </param>
        /// <param name="engines">
        /// Advisory hint: the engines this session expects to touch, as
        /// <c>(role, engine)</c> pairs, derived from the Model Manager's manifest.
        /// An empty array is legal and is what a fresh project sends.
        ///
        /// A model's engine comes from the server's catalog, so a load works
        /// whether or not this named the engine it needs. What the hint buys is
        /// timing: the server initialises those engines' backends at
        /// CreateSession instead of at first use.
        ///
        /// Only an out-of-range ordinal is rejected, with
        /// <c>InvalidEngineSet</c> (2005).
        /// </param>
        /// <param name="hotwordHints">
        /// Optional per-model hotword-bias overrides, sparse: a model left out
        /// follows the server catalog. Unlike placement this is not downgradable —
        /// biasing needs beam-search decoding, which a recognizer fixes when it is
        /// built, so it selects which recognizer this session is served.
        /// </param>
        /// <param name="placementHints">
        /// Optional per-model memory placement. Sparse: a model not listed means
        /// <c>Auto</c>. These are hints — a host that cannot honour one downgrades
        /// it and reports what it actually did.
        /// </param>
        public void CreateSession(
            (byte Role, byte Engine)[] engines,
            string gameName = "",
            string locale = null,
            (string Model, byte Placement)[] placementHints = null,
            (string Model, byte Hotwords)[] hotwordHints = null)
        {
            // CreateSession is one-shot (protocol v4). Guard against duplicate calls
            // (e.g. AutoStartSession auto-create + an editor tool / component also
            // calling CreateSession on connect) — a second request would be rejected
            // by the server with SessionAlreadyExists (2003). If the session already
            // exists, re-notify the caller so it can proceed; if one is in flight,
            // the pending request will notify all subscribers when it completes.
            if (HasState(SessionState.Configured))
            {
                CreateSessionComplete?.Invoke(TryllError.Ok());
                return;
            }
            if (HasState(SessionState.ConfigureInFlight))
                return;

            // null = provider language, then the project setting; "" = explicitly none.
            // An explicit tag always wins over both, also on later provider changes.
            _sessionLocaleExplicit = locale != null;
            if (locale == null)
                locale = ProviderOrSettingsLocale();

            SetState(SessionState.ConfigureInFlight);
            var config = new SessionModelConfigT
            {
                Engines = BuildEngineSelection(engines),
                PlacementHints = BuildSessionHints(placementHints, hotwordHints),
            };
            var notedName = gameName ?? "";
            // The session belongs to the connection, not the play token: a
            // reply that lands after play exits still configures the client.
            TryllCalls.CreateSession<bool>(
                Connection, this, notedName, locale, config,
                body =>
                {
                    Connection.SetSessionId(body.SessionId);
                    return true;
                },
                (_, err) => FinishCreateSession(err),
                rid => NoteOutbound(TryllObservationNote.Request(MessageBody.CreateSessionRequest, rid).Fill(n => { n.Text = notedName; })));
        }

        private void FinishCreateSession(TryllError err)
        {
            if (err.IsOk || err.Code == (uint)TryllErrorCode.SessionAlreadyExists)
            {
                ClearState(SessionState.ConfigureInFlight);
                SetState(SessionState.Configured);
                CreateSessionComplete?.Invoke(TryllError.Ok());
                return;
            }
            TryllLog.Warning($"CreateSession error: {err.Message}");
            ClearState(SessionState.ConfigureInFlight);
            CreateSessionComplete?.Invoke(err);
            Error?.Invoke(err);
        }

        internal void NoteOutbound(TryllObservationNote note)
        {
            // The frame tap in PushCmd journals the bytes. The note only carries
            // facts that are not on the wire, such as the deferred-flush carrier.
            if (note != null && note.CarrierRequestId != 0)
                Connection.ObserverHub.AnnotateCarrier(note.RequestId, note.CarrierRequestId);
        }

        internal void NoteInbound(TryllObservationNote note)
        {
            // The inbound tap journals the frame. Handlers still call this.
            _ = note;
        }

        internal void NoteLocalError(ulong requestId, ulong agentId, uint code, string codeName, string message, LogErrorOrigin origin)
        {
            var hub = Connection?.ObserverHub;
            if (hub == null || !hub.HasSubscribers) return;
            hub.ObserveLocal(
                TryllAgentLogLocal.Error(code, codeName, message, origin),
                requestId, agentId, true, message, code);
        }

        private static List<EngineSelectionT> BuildEngineSelection((byte Role, byte Engine)[] engines)
        {
            var list = new List<EngineSelectionT>();
            if (engines == null)
                return list;
            foreach (var e in engines)
            {
                list.Add(new EngineSelectionT
                {
                    Role = (ModelRole)e.Role,
                    Engine = (InferenceEngine)e.Engine,
                });
            }
            return list;
        }

        private static List<ModelPlacementHintT> BuildSessionHints(
            (string Model, byte Placement)[] placementHints,
            (string Model, byte Hotwords)[] hotwordHints)
        {
            var byModel = new Dictionary<string, ModelPlacementHintT>();
            if (placementHints != null)
            {
                foreach (var h in placementHints)
                {
                    if (!byModel.TryGetValue(h.Model, out var entry))
                        entry = new ModelPlacementHintT { ModelName = h.Model };
                    entry.Placement = (MemoryPlacement)h.Placement;
                    byModel[h.Model] = entry;
                }
            }
            if (hotwordHints != null)
            {
                foreach (var h in hotwordHints)
                {
                    if (!byModel.TryGetValue(h.Model, out var entry))
                        entry = new ModelPlacementHintT { ModelName = h.Model };
                    entry.Hotwords = (HotwordBias)h.Hotwords;
                    byModel[h.Model] = entry;
                }
            }
            return new List<ModelPlacementHintT>(byModel.Values);
        }

        /// <summary>
        /// Report how hard the server should yield the GPU back to the game
        /// (the manual inference throttle knob). Fire-and-forget: no response,
        /// no error surfaced locally. Never sent automatically — a session
        /// that never calls this runs unthrottled.
        /// </summary>
        /// <param name="level">0 = full speed (default), 1 = maximum yielding.
        /// Clamped server-side; out-of-range values do not throw here.</param>
        public void SetInferenceThrottle(float level)
        {
            NoteOutbound(TryllObservationNote.Request(MessageBody.SetInferenceThrottle, 0).Fill(n => { n.ThrottleLevel = level; }));
            TryllCalls.SetInferenceThrottle(Connection, level);
        }

        // ── Agent lifecycle — Task<T> APIs (Option A: return tuple, no throw) ──

        /// <summary>
        /// Create a server-side agent from <paramref name="graph"/>.
        /// Returns <c>(agent, error)</c>; always check <c>error.IsOk</c> before using the agent.
        /// </summary>
        /// <param name="textTable">
        /// Table name for Localized fields. Null keeps <see cref="TryllGraphDescription.TextTable"/>
        /// on the graph (the workflow asset copies its table there).
        /// </param>
        /// <param name="assetName">
        /// Name included in Text ID errors. Null reads as "workflow".
        /// </param>
        /// <param name="voice">
        /// The agent's voice (protocol v27). Required when any node in <paramref name="graph"/>
        /// speaks (Speak, GenerateAndSpeak): creation fails with
        /// <see cref="TryllErrorCode.AgentVoiceRequired"/> without it, and with
        /// <see cref="TryllErrorCode.InvalidVoiceProfile"/> when the loaded model cannot use a
        /// field. Copied when the call is made; later edits do not affect this agent (use
        /// <see cref="TryllAgent.SetVoiceAsync(TryllVoiceProfileData)"/>).
        /// </param>
        public async Task<(TryllAgent agent, TryllError error)> RequestCreateAgentAsync(
            TryllGraphDescription graph,
            bool enableDiagnostics = false,
            bool maintainDialogueHistory = true,
            List<TryllVariableDecl> variables = null,
            Tryll.AgentKvCacheInitialization kvCacheInitialization = Tryll.AgentKvCacheInitialization.AllocateOnly,
            Tryll.AgentWorkload workload = Tryll.AgentWorkload.Interactive,
            string textTable = null,
            string assetName = null,
            TryllVoiceProfileData voice = null)
        {
            // Snapshot the voice before any await so a caller editing the asset
            // afterwards cannot change what this create sends.
            var voiceWire = voice?.ToWire();
            var voiceSnapshot = TryllVoiceProfileData.FromWire(voiceWire);

            // Read the call scope before any await: AsEditorCall() ends when
            // this method first returns, and play mode can end while a provider
            // is still loading its tables.
            var playToken = _editorCallDepth == 0 ? _playSessionOwner : null;

            var toSend = graph;
            bool track = false;
            string resolvedLocale = null;
            if (TryllTextResolver.HasTickedFields(graph))
            {
                // The asset graph is shared. Resolve on a clone so the authored
                // Text IDs stay in the asset and the wire sees plain text.
                toSend = graph.Clone();
                if (textTable != null)
                    toSend.TextTable = textTable;
                var provider = TextProvider;
                resolvedLocale = provider?.CurrentLocale;
                TryllTextResolveResult resolved;
                try
                {
                    resolved = await TryllTextResolver.ResolveAsync(
                        toSend, provider, assetName, liveOnly: false);
                }
                catch (Exception ex)
                {
                    resolved = new TryllTextResolveResult(
                        new TryllError(TryllErrorCode.InvalidParamValue,
                            $"Localized text: provider failed: {ex.GetBaseException().Message}"),
                        true, null);
                }
                if (!resolved.Error.IsOk)
                    return (null, resolved.Error);
                track = resolved.AnyTicked;
                if (playToken != null && !ReferenceEquals(playToken, _playSessionOwner))
                    return (null, new TryllError(TryllErrorCode.ConnectionDropped, "Play session ended"));
            }

            enableDiagnostics = enableDiagnostics || DiagnosticsDemand.IsActive;
            var variableCopy = variables == null ? null : new List<TryllVariableDecl>(variables);
            var tcs = new TaskCompletionSource<(TryllAgent, TryllError)>();
            PlayCreate tracked = null;
            if (playToken != null)
            {
                tracked = new PlayCreate { Token = playToken, Tcs = tcs };
                _playCreates.Add(tracked);
            }
            ulong notedId = 0;
            var trackedGraph = toSend;
            var trackedAsset = assetName;
            var trackLocalized = track;
            var trackedLocale = resolvedLocale;
            // Owner is the client so EndPlaySession cannot drop the reply.
            // A play-token create is tracked separately and abandoned there.
            // toSend is the clone whose Text IDs were resolved to plain text.
            TryllCalls.CreateAgent(
                Connection, this,
                rid => ClientCodec.BuildCreateAgentRequest(
                    rid, toSend, enableDiagnostics, maintainDialogueHistory, variableCopy,
                    kvCacheInitialization, workload, voiceWire),
                (body, err) =>
                {
                    if (tracked != null)
                        _playCreates.Remove(tracked);
                    if (tracked != null && tracked.Abandoned)
                    {
                        if (!err.IsOk)
                        {
                            NoteInbound(TryllObservationNote.Reply(MessageBody.CreateAgentRequest, false, notedId).Fill(n => { n.Message = err.Message; n.Code = (uint)err.Code; }));
                            return;
                        }
                        var lateId = body.AgentId;
                        NoteInbound(TryllObservationNote.Reply(MessageBody.CreateAgentRequest, true, notedId).Fill(n => { n.AgentId = lateId; }));
                        DestroyAbandonedAgent(lateId);
                        return;
                    }
                    if (!err.IsOk)
                    {
                        NoteInbound(TryllObservationNote.Reply(MessageBody.CreateAgentRequest, false, notedId).Fill(n => { n.Message = err.Message; n.Code = (uint)err.Code; }));
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var agentId = body.AgentId;
                    var agent = new TryllAgent(
                        this,
                        agentId,
                        variableCopy,
                        enableDiagnostics,
                        voiceSnapshot);
                    RegisterAgent(agentId, agent);
                    SeedParamsBaseline(agent, trackedGraph);
                    agent.SetTextSource(trackedGraph?.TextTable, trackedAsset);
                    if (trackLocalized)
                        TrackLocalizedAgent(agent, trackedLocale);
                    NoteInbound(TryllObservationNote.Reply(MessageBody.CreateAgentRequest, true, notedId).Fill(n => { n.AgentId = agentId; }));
                    tcs.TrySetResult((agent, TryllError.Ok()));
                },
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.CreateAgentRequest, rid).Fill(n => { n.GraphDefaultModelName = toSend?.DefaultModelName; n.EnableDiagnostics = enableDiagnostics; n.MaintainDialogueHistory = maintainDialogueHistory; }));
                });
            return await tcs.Task;
        }

        internal bool TryGetAgent(ulong agentId, out TryllAgent agent)
            => _agentsById.TryGetValue(agentId, out agent);

        /// <summary>Snapshot currently registered agents for Agent Log registration (§7.5).</summary>
        internal IReadOnlyList<TryllAgentSnapshot> SnapshotRegisteredAgents()
        {
            var snapshots = new List<TryllAgentSnapshot>(_agentsById.Count);
            foreach (var kv in _agentsById)
            {
                var agent = kv.Value;
                snapshots.Add(new TryllAgentSnapshot(
                    agent.AgentId,
                    labelHint: string.IsNullOrEmpty(agent.DisplayName) ? null : agent.DisplayName,
                    agent.CreatedWithDiagnostics,
                    agent.CreatedBeforeDiagnosticsCapture));
            }
            return snapshots;
        }

        /// <summary>
        /// A copy of the live agents for the Disconnected drain.
        ///
        /// The drain hands control to consumer callbacks, and a callback may legitimately
        /// dispose its agent — the Dialog Lab does exactly that when <c>ChangeParams</c>
        /// comes back <c>ParamNotMutable</c> and it falls back to recreating the agent. That
        /// reaches <see cref="RequestDestroyAgentInternal"/> → <c>_agentsById.Remove</c>
        /// while the enumerator is still live, and the next <c>MoveNext</c> throws
        /// "Collection was modified".
        /// </summary>
        private List<TryllAgent> AgentsSnapshot() => new List<TryllAgent>(_agentsById.Values);

        private List<TryllVoiceInput> VoiceInputsSnapshot()
            => new List<TryllVoiceInput>(_voiceInputsById.Values);

        /// <summary>
        /// Tear down the current session epoch: complete leftover work, invalidate
        /// retained handles, and optionally notify <see cref="ConnectionChanged"/>.
        /// <see cref="SessionState.StartRequested"/> is kept across automatic reconnect.
        /// </summary>
        private void TeardownLiveSession(TryllError reason, bool clearStartRequested, bool notifyDisconnected)
        {
            bool hadEpoch = _announcedConnected
                || IsConnected
                || HasState(SessionState.Configured | SessionState.ConfigureInFlight)
                || _agentsById.Count > 0
                || _voiceInputsById.Count > 0;

            if (clearStartRequested)
                _sessionState = SessionState.None;
            else
                ClearState(SessionState.Configured | SessionState.ConfigureInFlight);

            DrainPendingClientRequests(reason);
            _localizedAgents.ForgetAll();
            foreach (var agent in AgentsSnapshot())
            {
                agent.DrainPendingCallbacks(reason);
                agent.ClearAfterDestroyRequest();
            }
            foreach (var voice in VoiceInputsSnapshot())
            {
                voice.DrainPendingAcks(reason);
                voice.ClearAfterDestroyRequest();
            }
            _agentsById.Clear();
            _voiceInputsById.Clear();

            if (notifyDisconnected && hadEpoch)
            {
                _announcedConnected = false;
                ConnectionChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Take-and-clear every client-level pending task so a drop or explicit
        /// <see cref="Disconnect"/> cannot leave callers awaiting forever.
        /// Agent maps are drained separately via <see cref="TryllAgent.DrainPendingCallbacks"/>.
        /// </summary>
        private void DrainPendingClientRequests(TryllError terminalError)
        {
            // A CreateAgent reply may already have left the registry and be
            // sitting on the event queue. Abandon first, so that completion
            // does not register the agent after the maps are cleared.
            AbandonPlayCreates(null, terminalError);
            // Keep the play-session token. Connect tears the previous socket
            // down through this method, and clearing the token there made
            // EndPlaySession fail nothing. The token's entries are already
            // failed above; EndPlaySession is what drops the token.
            var token = _playSessionOwner;
            if (token != null)
                Connection?.FailOwnedBy(token, terminalError);
            Connection?.FailOwnedBy(this, terminalError);
        }

        internal void RaiseError(TryllError error) => Error?.Invoke(error);

        internal void RequestDestroyAgentInternal(TryllAgent agent)
        {
            var aid = agent.AgentId;
            if (aid == 0) return;
            agent.DrainPendingCallbacks(new TryllError(TryllErrorCode.InvalidAgentId, "Agent destroyed"));
            ulong notedId = 0;
            TryllCalls.DestroyAgent(
                Connection, agent, aid,
                err =>
                {
                    if (!err.IsOk)
                    {
                        NoteInbound(TryllObservationNote.Reply(MessageBody.DestroyAgentRequest, false, notedId).Fill(n => { n.Message = err.Message; n.AgentId = aid; n.Code = (uint)err.Code; }));
                        Error?.Invoke(err);
                        return;
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.DestroyAgentRequest, true, notedId).Fill(n => { n.AgentId = aid; }));
                    AgentDestroyed?.Invoke(aid);
                },
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.DestroyAgentRequest, rid).Fill(n => { n.AgentId = aid; }));
                });
            agent.ClearAfterDestroyRequest();
            _agentsById.Remove(aid);
            _localizedAgents.Forget(agent);
        }

        // ── Model management ───────────────────────────────────────────────────

        /// <summary>List models available for the session's inference engine.</summary>
        public Task<(List<TryllModelInfo> models, TryllError error)> RequestListModelsAsync()
        {
            var tcs = new TaskCompletionSource<(List<TryllModelInfo>, TryllError)>();
            TryllCalls.ListModels<List<TryllModelInfo>>(
                Connection, RequestOwner,
                ClientCodec.ModelsFrom,
                (models, err) => tcs.TrySetResult((err.IsOk ? models : null, err)),
                rid => NoteOutbound(TryllObservationNote.Request(MessageBody.ListModelsRequest, rid)));
            return tcs.Task;
        }

        /// <summary>
        /// Pull a server-wide RAM/VRAM snapshot. Session must exist; the numbers
        /// cover every session on that process (same view as the server monitor).
        /// Never fails with AgentBusy.
        /// </summary>
        public Task<(TryllMemorySnapshot snapshot, TryllError error)> RequestGetMemoryConsumptionAsync()
        {
            var tcs = new TaskCompletionSource<(TryllMemorySnapshot, TryllError)>();
            TryllCalls.GetMemoryConsumption<TryllMemorySnapshot>(
                Connection, RequestOwner,
                TryllMemorySnapshot.FromWire,
                (snapshot, err) => tcs.TrySetResult((
                    err.IsOk ? snapshot ?? new TryllMemorySnapshot() : null, err)),
                rid => NoteOutbound(TryllObservationNote.Request(MessageBody.GetMemoryConsumptionRequest, rid)));
            return tcs.Task;
        }

        public Task<(TryllMutableKb store, TryllError error)> EnsureMutableKbAsync(string name, string schema)
        {
            var tcs = new TaskCompletionSource<(TryllMutableKb, TryllError)>();
            ulong noted = 0;
            TryllCalls.EnsureMutableKb(
                Connection, RequestOwner, name, schema,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.EnsureMutableKbRequest, false, noted, err, name);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var storeName = body.Name ?? name ?? "";
                    var storeSchema = body.Schema ?? schema ?? "";
                    NoteInbound(TryllObservationNote.Reply(MessageBody.EnsureMutableKbRequest, true, noted).Fill(n => { n.Text = storeName; n.Message = storeSchema; n.Flag = body.Created; }));
                    tcs.TrySetResult((new TryllMutableKb(this, storeName, storeSchema), TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.EnsureMutableKbRequest, rid).Fill(n => { n.Text = name; n.Text2 = schema; }));
                });
            return tcs.Task;
        }

        public Task<(List<TryllMutableKbInfo> stores, TryllError error)> EnumerateMutableKbsAsync()
        {
            var tcs = new TaskCompletionSource<(List<TryllMutableKbInfo>, TryllError)>();
            ulong noted = 0;
            TryllCalls.EnumerateMutableKbs(
                Connection, RequestOwner,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.EnumerateMutableKbsRequest, false, noted, err, null);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var stores = MapStores(body.Stores);
                    NoteInbound(TryllObservationNote.Reply(MessageBody.EnumerateMutableKbsRequest, true, noted));
                    tcs.TrySetResult((stores, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.EnumerateMutableKbsRequest, rid));
                });
            return tcs.Task;
        }

        public Task<(TryllGameMemorySaveResult result, TryllError error)> SaveGameMemoryAsync(string path, string saveId)
        {
            var tcs = new TaskCompletionSource<(TryllGameMemorySaveResult, TryllError)>();
            ulong noted = 0;
            TryllCalls.SaveGameMemory(
                Connection, RequestOwner, path, saveId,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.SaveGameMemoryRequest, false, noted, err, null);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var result = new TryllGameMemorySaveResult
                    {
                        SavedAtMs = body.SavedAtMs,
                        StoreCount = body.StoreCount,
                    };
                    NoteInbound(TryllObservationNote.Reply(MessageBody.SaveGameMemoryRequest, true, noted));
                    tcs.TrySetResult((result, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.SaveGameMemoryRequest, rid).Fill(n => { n.Message = path; }));
                });
            return tcs.Task;
        }

        public Task<(TryllGameMemoryLoadResult result, TryllError error)> LoadGameMemoryAsync(
            string path,
            TryllMutableKbEmbedTiming embedTiming,
            TryllMutableKbCompatibility compatibility = TryllMutableKbCompatibility.Strict)
        {
            var tcs = new TaskCompletionSource<(TryllGameMemoryLoadResult, TryllError)>();
            if (embedTiming != TryllMutableKbEmbedTiming.Now && embedTiming != TryllMutableKbEmbedTiming.Lazy)
            {
                var rejected = new TryllError(TryllErrorCode.InvalidEmbedTiming,
                    "LoadGameMemory requires embed_timing Now or Lazy");
                Connection.Post(() => tcs.TrySetResult((null, rejected)));
                return tcs.Task;
            }
            ulong noted = 0;
            TryllCalls.LoadGameMemory(
                Connection, RequestOwner, path,
                (MutableKbCompatibility)compatibility,
                (MutableKbEmbedTiming)embedTiming,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.LoadGameMemoryRequest, false, noted, err, null);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var result = new TryllGameMemoryLoadResult
                    {
                        StoreCount = body.StoreCount,
                        Warnings = body.Warnings ?? new List<string>(),
                    };
                    NoteInbound(TryllObservationNote.Reply(MessageBody.LoadGameMemoryRequest, true, noted));
                    tcs.TrySetResult((result, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.LoadGameMemoryRequest, rid).Fill(n => { n.Message = path; }));
                });
            return tcs.Task;
        }

        internal Task<(List<TryllMutableKbWriteResult> results, TryllError error)> MutableKbWriteAsync(
            string name, string schema, IReadOnlyList<TryllMutableKbUpsert> records)
        {
            var copied = records == null ? null : new List<TryllMutableKbUpsert>(records);
            var wire = ClientCodec.ToMutableKbUpsertT(copied);
            var tcs = new TaskCompletionSource<(List<TryllMutableKbWriteResult>, TryllError)>();
            ulong noted = 0;
            TryllCalls.MutableKbWrite(
                Connection, RequestOwner, name, schema, wire,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.MutableKbWriteRequest, false, noted, err, name);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var results = MapWriteResults(body.Results);
                    NoteInbound(TryllObservationNote.Reply(MessageBody.MutableKbWriteRequest, true, noted));
                    tcs.TrySetResult((results, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.MutableKbWriteRequest, rid).Fill(n => { n.Text = name; n.Text2 = schema; }));
                });
            return tcs.Task;
        }

        internal Task<(uint deletedCount, TryllError error)> MutableKbDeleteAsync(string name, IReadOnlyList<string> ids)
        {
            var copied = ids == null ? null : new List<string>(ids);
            var tcs = new TaskCompletionSource<(uint, TryllError)>();
            ulong noted = 0;
            TryllCalls.MutableKbDelete(
                Connection, RequestOwner, name, copied,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.MutableKbDeleteRequest, false, noted, err, name);
                        tcs.TrySetResult((0, err));
                        return;
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.MutableKbDeleteRequest, true, noted));
                    tcs.TrySetResult((body.DeletedCount, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.MutableKbDeleteRequest, rid).Fill(n => { n.Text = name; }));
                });
            return tcs.Task;
        }

        internal Task<(uint clearedCount, TryllError error)> MutableKbClearAsync(string name)
        {
            var tcs = new TaskCompletionSource<(uint, TryllError)>();
            ulong noted = 0;
            TryllCalls.MutableKbClear(
                Connection, RequestOwner, name,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.MutableKbClearRequest, false, noted, err, name);
                        tcs.TrySetResult((0, err));
                        return;
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.MutableKbClearRequest, true, noted));
                    tcs.TrySetResult((body.ClearedCount, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.MutableKbClearRequest, rid).Fill(n => { n.Text = name; }));
                });
            return tcs.Task;
        }

        internal Task<(List<TryllMutableKbHit> hits, TryllError error)> MutableKbQueryAsync(
            string name, string queryText, uint topK, RetrievalMode retrievalMode)
        {
            var tcs = new TaskCompletionSource<(List<TryllMutableKbHit>, TryllError)>();
            ulong noted = 0;
            TryllCalls.MutableKbQuery(
                Connection, RequestOwner, name, queryText, topK, retrievalMode,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.MutableKbQueryRequest, false, noted, err, name);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var hits = MapHits(body.Hits);
                    NoteInbound(TryllObservationNote.Reply(MessageBody.MutableKbQueryRequest, true, noted));
                    tcs.TrySetResult((hits, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.MutableKbQueryRequest, rid).Fill(n => { n.Text = name; n.Message = queryText; }));
                });
            return tcs.Task;
        }

        internal Task<(TryllMutableKbRecord record, TryllError error)> MutableKbGetAsync(string name, string id)
        {
            var tcs = new TaskCompletionSource<(TryllMutableKbRecord, TryllError)>();
            ulong noted = 0;
            TryllCalls.MutableKbGet(
                Connection, RequestOwner, name, id,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.MutableKbGetRequest, false, noted, err, name);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    TryllMutableKbRecord record = null;
                    if (body.Found)
                    {
                        record = new TryllMutableKbRecord
                        {
                            Id = body.Id,
                            Text = body.Text,
                            CreatedAtMs = body.CreatedAtMs,
                            UpdatedAtMs = body.UpdatedAtMs,
                        };
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.MutableKbGetRequest, true, noted).Fill(n => { n.Flag = body.Found; }));
                    tcs.TrySetResult((record, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.MutableKbGetRequest, rid).Fill(n => { n.Text = name; n.Message = id; }));
                });
            return tcs.Task;
        }

        internal Task<(TryllMutableKbListPage page, TryllError error)> MutableKbListAsync(
            string name, uint offset, uint limit)
        {
            var tcs = new TaskCompletionSource<(TryllMutableKbListPage, TryllError)>();
            ulong noted = 0;
            TryllCalls.MutableKbList(
                Connection, RequestOwner, name, offset, limit,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.MutableKbListRequest, false, noted, err, name);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var page = new TryllMutableKbListPage
                    {
                        NextOffset = body.NextOffset,
                        HasMore = body.HasMore,
                    };
                    if (body.Records != null)
                    {
                        foreach (var rec in body.Records)
                        {
                            page.Records.Add(new TryllMutableKbRecord
                            {
                                Id = rec.Id,
                                Text = rec.Text,
                                CreatedAtMs = rec.CreatedAtMs,
                                UpdatedAtMs = rec.UpdatedAtMs,
                            });
                        }
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.MutableKbListRequest, true, noted));
                    tcs.TrySetResult((page, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.MutableKbListRequest, rid).Fill(n => { n.Text = name; }));
                });
            return tcs.Task;
        }

        internal Task<(TryllMutableKbStats stats, TryllError error)> MutableKbStatsAsync(string name)
        {
            var tcs = new TaskCompletionSource<(TryllMutableKbStats, TryllError)>();
            ulong noted = 0;
            TryllCalls.MutableKbStats(
                Connection, RequestOwner, name,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.MutableKbStatsRequest, false, noted, err, name);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var stats = new TryllMutableKbStats
                    {
                        LiveCount = body.LiveCount,
                        TombstonedCount = body.TombstonedCount,
                        SlotCount = body.SlotCount,
                        WriteGeneration = body.WriteGeneration,
                        BytesEstimate = body.BytesEstimate,
                        Materialized = body.Materialized,
                    };
                    NoteInbound(TryllObservationNote.Reply(MessageBody.MutableKbStatsRequest, true, noted));
                    tcs.TrySetResult((stats, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.MutableKbStatsRequest, rid).Fill(n => { n.Text = name; }));
                });
            return tcs.Task;
        }

        internal Task<(TryllMutableKbSnapshot snapshot, TryllError error)> ExportMutableKbAsync(string name)
        {
            var tcs = new TaskCompletionSource<(TryllMutableKbSnapshot, TryllError)>();
            ulong noted = 0;
            TryllCalls.ExportMutableKb(
                Connection, RequestOwner, name,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.ExportMutableKbRequest, false, noted, err, name);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    TryllMutableKbSnapshot snapshot;
                    try
                    {
                        snapshot = UnpackMutableKb(body.Snapshot);
                    }
                    catch (Exception ex)
                    {
                        var invalid = new TryllError(TryllErrorCode.MutableKbSnapshotInvalid, ex.Message);
                        NoteStorage(MessageBody.ExportMutableKbRequest, false, noted, invalid, name);
                        tcs.TrySetResult((null, invalid));
                        return;
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.ExportMutableKbRequest, true, noted));
                    tcs.TrySetResult((snapshot, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.ExportMutableKbRequest, rid).Fill(n => { n.Text = name; }));
                });
            return tcs.Task;
        }

        internal Task<(TryllMutableKbImportResult result, TryllError error)> ImportMutableKbAsync(
            TryllMutableKbSnapshot snapshot, TryllMutableKbCompatibility compatibility)
        {
            var tcs = new TaskCompletionSource<(TryllMutableKbImportResult, TryllError)>();
            try
            {
                var encoded = snapshot.ToFlatBufferBytes();
                if (encoded != null && (uint)encoded.Length > ClientWire.MaxMutableKbSnapshotBytes)
                {
                    Connection.Post(() => tcs.TrySetResult((
                        null,
                        new TryllError(TryllErrorCode.StorageSnapshotTooLarge,
                            "Encoded mutable KB snapshot exceeds MaxMutableKbSnapshotBytes"))));
                    return tcs.Task;
                }
            }
            catch (ArgumentException ex)
            {
                var code = ex.Message != null && ex.Message.IndexOf("exceeds", StringComparison.Ordinal) >= 0
                    ? TryllErrorCode.StorageSnapshotTooLarge
                    : TryllErrorCode.MutableKbSnapshotInvalid;
                var invalid = new TryllError(code, ex.Message);
                Connection.Post(() => tcs.TrySetResult((null, invalid)));
                return tcs.Task;
            }
            ulong noted = 0;
            TryllCalls.ImportMutableKb(
                Connection, RequestOwner,
                rid => ClientCodec.BuildImportMutableKbRequest(rid, snapshot, compatibility),
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteStorage(MessageBody.ImportMutableKbRequest, false, noted, err, null);
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var result = new TryllMutableKbImportResult
                    {
                        Name = body.Name,
                        Warnings = body.Warnings ?? new List<string>(),
                    };
                    NoteInbound(TryllObservationNote.Reply(MessageBody.ImportMutableKbRequest, true, noted));
                    tcs.TrySetResult((result, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.ImportMutableKbRequest, rid));
                });
            return tcs.Task;
        }

        public void RequestDownloadModel(string modelName)
        {
            var rid = Connection.NextRequestId();
            var frame = ClientCodec.BuildDownloadModelRequest(rid, modelName);
            Connection.Call(rid, frame, this, (msg, err) =>
            {
                void Finish(string name, bool success, string message, TryllError error, bool failed)
                {
                    void Run()
                    {
                        NoteInbound(TryllObservationNote.Reply(
                            MessageBody.DownloadModelRequest, !failed, rid).Fill(n =>
                        {
                            n.Text = name;
                            n.Flag = success;
                            n.Message = message;
                            n.Code = (uint)error.Code;
                        }));
                        DownloadComplete?.Invoke(name, success, message);
                        if (failed && !IsDrainedTransportError(error))
                            Error?.Invoke(error);
                    }
                    if (Connection.OnCallerThread)
                        Run();
                    else
                        Connection.Post(Run);
                }

                if (!err.IsOk)
                {
                    Finish(modelName, false, err.Message, err, true);
                    return true;
                }
                if (msg.ByteBuffer != null && msg.BodyType == MessageBody.DownloadProgress)
                {
                    var progress = msg.BodyAsDownloadProgress();
                    var progressName = progress.ModelName;
                    var downloaded = progress.BytesDownloaded;
                    var total = progress.TotalBytes;
                    var percent = progress.Percent;
                    void Run()
                    {
                        NoteInbound(TryllObservationNote.Push(MessageBody.DownloadProgress, MessageBody.DownloadModelRequest).Fill(n => { n.RequestId = rid; n.Code = (uint)downloaded; n.Text = progressName; }));
                        DownloadProgress?.Invoke(progressName, downloaded, total, percent);
                    }
                    if (Connection.OnCallerThread)
                        Run();
                    else
                        Connection.Post(Run);
                    return false;
                }
                if (msg.ByteBuffer != null && msg.BodyType == MessageBody.DownloadComplete)
                {
                    var done = msg.BodyAsDownloadComplete();
                    // success=false arrives with code 0. It is still a failure,
                    // and it is not a drain, so Error is raised once.
                    if (!done.Success)
                    {
                        Finish(done.ModelName, false, done.Error,
                            new TryllError(0u, done.Error ?? ""), true);
                        return true;
                    }
                    Finish(done.ModelName, true, done.Error, TryllError.Ok(), false);
                    return true;
                }
                if (msg.ByteBuffer != null && msg.BodyType == MessageBody.ErrorResponse)
                {
                    var body = msg.BodyAsErrorResponse();
                    Finish(modelName, false, body.Message, new TryllError(body.Code, body.Message), true);
                    return true;
                }
                Finish(modelName, false, "unexpected reply",
                    new TryllError(TryllErrorCode.MalformedMessage, "unexpected reply"), true);
                return true;
            }, MessageBody.DownloadModelRequest, requestId => NoteOutbound(TryllObservationNote.Request(MessageBody.DownloadModelRequest, requestId).Fill(n => { n.Text = modelName; })));
        }

        /// <param name="placement">
        /// Where to put this model, for this load only, overriding any per-model hint
        /// the session declared. <c>null</c> (the default) means "no opinion" — the
        /// session hint applies. Passing <c>Auto</c> is therefore a real override
        /// meaning "host policy", which is distinct from not asking: without that
        /// distinction a stale Ram/Vram hint could never be undone for the session.
        /// </param>
        public void RequestLoadModel(string modelName, TryllMemoryPlacement? placement = null)
        {
            var requested = (byte)(placement ?? TryllMemoryPlacement.Auto);
            var wasRequested = placement.HasValue;
            ulong notedId = 0;
            TryllCalls.LoadModel(
                Connection, RequestOwner, modelName,
                wasRequested ? (MemoryPlacement)requested : MemoryPlacement.Auto,
                wasRequested,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        TryllLog.Warning($"LoadModel error: {err.Message}");
                        NoteInbound(TryllObservationNote.Reply(MessageBody.LoadModelRequest, false, notedId).Fill(n => { n.Text = modelName; n.Message = err.Message; n.Code = (uint)err.Code; }));
                        LoadModelComplete?.Invoke(modelName, false);
                        if (!IsDrainedTransportError(err))
                            Error?.Invoke(err);
                        return;
                    }
                    ReportLoadPlacement(modelName, wasRequested, requested,
                        (byte)body.PlacementResolved, body.Backend ?? string.Empty);
                    NoteInbound(TryllObservationNote.Reply(MessageBody.LoadModelRequest, true, notedId).Fill(n => { n.Text = modelName; }));
                    LoadModelComplete?.Invoke(modelName, true);
                },
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.LoadModelRequest, rid).Fill(n => { n.Text = modelName; }));
                });
        }

        private static void ReportLoadPlacement(
            string modelName, bool wasRequested, byte requested, byte resolved, string backend)
        {
            if (wasRequested
                && (TryllMemoryPlacement)requested != TryllMemoryPlacement.Auto
                && requested != resolved)
            {
                TryllLog.Warning(
                    $"LoadModel '{modelName}': asked for " +
                    $"{(TryllMemoryPlacement)requested}, got " +
                    $"{(TryllMemoryPlacement)resolved} " +
                    $"(backend '{backend}'). An unsatisfiable request is " +
                    "downgraded rather than failed, and a model another session " +
                    "already loaded keeps its resident placement.");
            }
            else
            {
                TryllLog.Verbose(
                    $"LoadModel '{modelName}': {(TryllMemoryPlacement)resolved} " +
                    $"(backend '{backend}')");
            }
        }

        /// <summary>
        /// Change the session language without restarting the session. Pass an
        /// empty string to clear it.
        ///
        /// Applies to every agent, existing and future, from its next turn: TTS
        /// language and the locale template keys are read per turn, not captured
        /// at agent creation. A new voice input picks up the new STT language
        /// hint; one already open keeps its recognizer. Storage paths re-resolve
        /// on the next load, but content already bound to a node (a Retrieve KB,
        /// canned lines) does NOT swap — those params are structural, so
        /// switching authored content still means recreating agents.
        ///
        /// Not turn-atomic: a turn already running may render its prompt under
        /// the old locale and synthesize under the new one.
        ///
        /// Completion arrives on <see cref="SetSessionLocaleComplete"/>; a
        /// malformed tag fails with <c>InvalidLocale</c> (2004).
        /// </summary>
        public void RequestSetSessionLocale(string locale)
        {
            var tag = locale ?? "";
            ulong notedId = 0;
            TryllCalls.SetSessionLocale(
                Connection, RequestOwner, tag,
                err =>
                {
                    if (!err.IsOk)
                    {
                        TryllLog.Warning($"SetSessionLocale error: {err.Message}");
                        NoteInbound(TryllObservationNote.Reply(MessageBody.SetSessionLocaleRequest, false, notedId).Fill(n => { n.Text = tag; n.Message = err.Message; n.Code = (uint)err.Code; }));
                        SetSessionLocaleComplete?.Invoke(tag, false);
                        Error?.Invoke(err);
                        return;
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.SetSessionLocaleRequest, true, notedId).Fill(n => { n.Text = tag; }));
                    SetSessionLocaleComplete?.Invoke(tag, true);
                },
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.SetSessionLocaleRequest, rid));
                });
        }

        void BindTextProvider()
        {
            var current = TextProvider;
            if (ReferenceEquals(current, _boundTextProvider)) return;
            UnbindTextProvider();
            _boundTextProvider = current;
            if (_boundTextProvider != null)
                _boundTextProvider.LocaleChanged += OnTextLocaleChanged;
        }

        void UnbindTextProvider()
        {
            if (_boundTextProvider == null) return;
            _boundTextProvider.LocaleChanged -= OnTextLocaleChanged;
            _boundTextProvider = null;
        }

        /// <summary>The provider's language, else <see cref="TryllRuntimeSettings.Locale"/>.</summary>
        string ProviderOrSettingsLocale()
        {
            string fromProvider = TextProvider?.CurrentLocale;
            if (!string.IsNullOrEmpty(fromProvider))
                return fromProvider;
            return Settings.Locale ?? string.Empty;
        }

        void OnTextLocaleChanged()
        {
            var provider = _boundTextProvider;
            if (provider == null) return;
            if (IsSessionReady && !_sessionLocaleExplicit)
                RequestSetSessionLocale(ProviderOrSettingsLocale());
            _ = RefreshLocalizedTextsAsync(provider);
        }

        async Task RefreshLocalizedTextsAsync(ITryllTextProvider provider)
        {
            if (_localizedRefreshRunning)
            {
                _localizedRefreshQueued = true;
                return;
            }

            _localizedRefreshRunning = true;
            try
            {
                do
                {
                    _localizedRefreshQueued = false;
                    var current = _boundTextProvider ?? provider;
                    if (current == null) break;
                    await _localizedAgents.RefreshAllAsync(current, RaiseLocalizedTextFailed, RaiseLocalizedTextApplied);
                }
                while (_localizedRefreshQueued);
            }
            catch (Exception ex)
            {
                TryllLog.Warning($"Localized text refresh failed: {ex.Message}");
            }
            finally
            {
                _localizedRefreshRunning = false;
            }
        }

        void RaiseLocalizedTextFailed(ITryllLocalizedTarget target, TryllError error)
        {
            if (target is TryllAgent agent)
                LocalizedTextFailed?.Invoke(agent, error);
        }

        void RaiseLocalizedTextApplied(ITryllLocalizedTarget target)
        {
            if (target is TryllAgent agent)
                LocalizedTextApplied?.Invoke(agent);
        }

        /// <summary>
        /// Track an agent whose prompts came from Text IDs. If the language
        /// changed while it was being created, refresh it now.
        /// </summary>
        void TrackLocalizedAgent(TryllAgent agent, string resolvedLocale)
        {
            _localizedAgents.Track(agent);
            var provider = TextProvider;
            if (provider == null) return;
            if (!string.Equals(provider.CurrentLocale, resolvedLocale, StringComparison.Ordinal))
                _ = RefreshOneLocalizedAsync(agent, provider);
        }

        async Task RefreshOneLocalizedAsync(TryllAgent agent, ITryllTextProvider provider)
        {
            try
            {
                await _localizedAgents.RefreshAsync(agent, provider, RaiseLocalizedTextFailed, RaiseLocalizedTextApplied);
            }
            catch (Exception ex)
            {
                TryllLog.Warning($"Localized text refresh failed: {ex.Message}");
            }
        }

        internal TryllLocalizedAgentTracker LocalizedAgents => _localizedAgents;

        /// <summary>Tests: drop a code-set provider and use the settings asset again.</summary>
        internal void ResetTextProviderOverride()
        {
            _textProviderOverride = null;
            _textProviderOverridden = false;
            BindTextProvider();
        }

        /// <summary>
        /// A turn ended. Re-send localized updates that came back
        /// <c>AgentBusy</c> while the turn was running, from the current baseline.
        /// </summary>
        internal void NoteLocalizedTurnEnded(TryllAgent agent)
        {
            if (!_localizedAgents.HasPending(agent)) return;
            var provider = TextProvider;
            if (provider == null) return;
            _ = RefreshOneLocalizedAsync(agent, provider);
        }

        public void RequestUnloadModel(string modelName)
        {
            ulong notedId = 0;
            TryllCalls.UnloadModel(
                Connection, RequestOwner, modelName,
                err => FinishNamedAck(
                    modelName, err, "UnloadModel",
                    MessageBody.UnloadModelRequest,
                    UnloadModelComplete, notedId),
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.UnloadModelRequest, rid).Fill(n => { n.Text = modelName; }));
                });
        }

        /// <summary>Editor Model Manager only — not a shipping runtime API.</summary>
        internal void RequestDeleteModel(string modelName)
        {
            ulong notedId = 0;
            TryllCalls.DeleteModel(
                Connection, RequestOwner, modelName,
                err => FinishNamedAck(
                    modelName, err, "DeleteModel",
                    MessageBody.DeleteModelRequest,
                    DeleteModelComplete, notedId),
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.DeleteModelRequest, rid).Fill(n => { n.Text = modelName; }));
                });
        }

        /// <summary>
        /// A registry drain already reported the drop or the failed connect.
        /// The call's own completion must not raise <see cref="Error"/> again.
        /// </summary>
        private static bool IsDrainedTransportError(TryllError err)
            => err.Code == (uint)TryllErrorCode.ConnectionDropped
            || err.Code == (uint)TryllErrorCode.ConnectionFailed;

        private void FinishNamedAck(
            string name, TryllError err, string label,
            MessageBody request,
            Action<string, bool> complete,
            ulong requestId)
        {
            if (!err.IsOk)
            {
                TryllLog.Warning($"{label} error: {err.Message}");
                NoteInbound(TryllObservationNote.Reply(request, false, requestId).Fill(n =>
                {
                    n.Text = name;
                    n.Code = (uint)err.Code;
                    n.Message = err.Message;
                }));
                complete?.Invoke(name, false);
                if (!IsDrainedTransportError(err))
                    Error?.Invoke(err);
                return;
            }
            NoteInbound(TryllObservationNote.Reply(request, true, requestId).Fill(n => n.Text = name));
            complete?.Invoke(name, true);
        }

        // ── StringStorage management ───────────────────────────────────────────

        public Task<TryllError> RequestCreateStringStorageAsync(string name, List<string> strings)
        {
            return CreateStringStorage(
                name, StringStorageKind.List, CopyStrings(strings), null, null);
        }

        public Task<TryllError> RequestCreateStringStorageFromFileAsync(
            string name, string filePath,
            Tryll.StringStorageKind kind = Tryll.StringStorageKind.List)
        {
            return CreateStringStorage(name, kind, null, null, filePath);
        }

        public Task<TryllError> RequestCreateKeyedStringStorageAsync(
            string name, List<string> keys, List<string> values,
            Tryll.StringStorageKind kind = Tryll.StringStorageKind.Map)
        {
            var copiedKeys = CopyStrings(keys);
            var copiedValues = CopyStrings(values);
            return CreateStringStorage(name, kind, copiedValues, copiedKeys, null);
        }

        public Task<TryllError> RequestDestroyStringStorageAsync(string name)
        {
            var tcs = new TaskCompletionSource<TryllError>();
            ulong noted = 0;
            TryllCalls.DestroyStringStorage(
                Connection, RequestOwner, name,
                err => FinishStringStorage(tcs, noted, name, err, destroying: true),
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.DestroyStringStorageRequest, rid).Fill(n => { n.Text = name; }));
                });
            return tcs.Task;
        }

        private Task<TryllError> CreateStringStorage(
            string name, StringStorageKind kind, List<string> strings, List<string> keys, string filePath)
        {
            var tcs = new TaskCompletionSource<TryllError>();
            ulong noted = 0;
            TryllCalls.CreateStringStorage(
                Connection, RequestOwner, name, kind, strings, keys, filePath,
                (body, err) =>
                {
                    var reported = err.IsOk && !string.IsNullOrEmpty(body?.Name) ? body.Name : name;
                    FinishStringStorage(tcs, noted, reported, err, destroying: false);
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.CreateStringStorageRequest, rid).Fill(n =>
                    {
                        n.Text = name;
                        if (!string.IsNullOrEmpty(filePath))
                            n.Message = filePath;
                        else if (keys != null && keys.Count > 0)
                            n.Message = string.Join(", ", keys);
                    }));
                });
            return tcs.Task;
        }

        private void FinishStringStorage(
            TaskCompletionSource<TryllError> tcs, ulong requestId, string name, TryllError err, bool destroying)
        {
            var request = destroying
                ? MessageBody.DestroyStringStorageRequest
                : MessageBody.CreateStringStorageRequest;
            if (!err.IsOk)
            {
                StringStorageChanged?.Invoke(name, false);
                NoteStorage(request, false, requestId, err, name);
                tcs.TrySetResult(err);
                Error?.Invoke(err);
                return;
            }
            StringStorageChanged?.Invoke(name, true);
            NoteStorage(request, true, requestId, TryllError.Ok(), name);
            tcs.TrySetResult(TryllError.Ok());
        }

        // ── EmbeddedStringStorage management ──────────────────────────────────

        public Task<(EmbeddedStorageInfo info, TryllError error)> RequestCreateEmbeddedStringStorageAsync(
            string name, string configPath, string embeddingModel = "")
        {
            return CreateEmbedded(name, configPath, null, embeddingModel, fromStrings: false);
        }

        public Task<(EmbeddedStorageInfo info, TryllError error)> RequestCreateEmbeddedStringStorageFromStringsAsync(
            string name, List<string> strings, string embeddingModel)
        {
            return CreateEmbedded(name, null, CopyStrings(strings), embeddingModel, fromStrings: true);
        }

        public Task<TryllError> RequestDestroyEmbeddedStringStorageAsync(string name)
        {
            var tcs = new TaskCompletionSource<TryllError>();
            ulong noted = 0;
            TryllCalls.DestroyEmbeddedStringStorage(
                Connection, RequestOwner, name,
                err =>
                {
                    if (!err.IsOk)
                    {
                        EmbeddedStringStorageChanged?.Invoke(name, 0, false);
                        NoteStorage(MessageBody.DestroyEmbeddedStringStorageRequest, false, noted, err, name);
                        tcs.TrySetResult(err);
                        Error?.Invoke(err);
                        return;
                    }
                    EmbeddedStringStorageChanged?.Invoke(name, 0, true);
                    NoteInbound(TryllObservationNote.Reply(MessageBody.DestroyEmbeddedStringStorageRequest, true, noted).Fill(n => { n.Text = name; }));
                    tcs.TrySetResult(TryllError.Ok());
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.DestroyEmbeddedStringStorageRequest, rid).Fill(n => { n.Text = name; }));
                });
            return tcs.Task;
        }

        private Task<(EmbeddedStorageInfo info, TryllError error)> CreateEmbedded(
            string name, string configPath, List<string> strings, string embeddingModel, bool fromStrings)
        {
            var tcs = new TaskCompletionSource<(EmbeddedStorageInfo, TryllError)>();
            ulong noted = 0;
            TryllCalls.CreateEmbeddedStringStorage(
                Connection, RequestOwner, name, configPath, strings, embeddingModel,
                (body, err) =>
                {
                    var reported = err.IsOk && !string.IsNullOrEmpty(body?.Name) ? body.Name : name;
                    if (!err.IsOk)
                    {
                        EmbeddedStringStorageChanged?.Invoke(reported, 0, false);
                        NoteStorage(MessageBody.CreateEmbeddedStringStorageRequest, false, noted, err, reported);
                        tcs.TrySetResult((default, err));
                        Error?.Invoke(err);
                        return;
                    }
                    var info = new EmbeddedStorageInfo
                    {
                        Name = reported,
                        RecordCount = body.RecordCount,
                        EmbeddingDim = body.EmbeddingDim,
                    };
                    EmbeddedStringStorageChanged?.Invoke(reported, (int)body.RecordCount, true);
                    NoteInbound(TryllObservationNote.Reply(MessageBody.CreateEmbeddedStringStorageRequest, true, noted).Fill(n => { n.Text = reported; }));
                    tcs.TrySetResult((info, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(
                        MessageBody.CreateEmbeddedStringStorageRequest, rid).Fill(n =>
                    {
                        n.Text = name;
                        if (fromStrings)
                            n.Text2 = embeddingModel;
                    }));
                });
            return tcs.Task;
        }

        // ── VoiceInput lifecycle ───────────────────────────────────────────────

        /// <summary>
        /// Create a server-side VoiceInput handle for streaming speech-to-text.
        /// Returns <c>(voice, error)</c>; always check <c>error.IsOk</c> before using the handle.
        /// The caller owns the returned handle and must call <see cref="TryllVoiceInput.Dispose"/>.
        /// </summary>
        public Task<(TryllVoiceInput voice, TryllError error)> CreateVoiceInputAsync(VoiceInputConfig cfg)
        {
            var format = new WireAudioFormatT
            {
                SampleRate = cfg.InputFormat.SampleRate,
                Channels = cfg.InputFormat.Channels,
                BitsPerSample = cfg.InputFormat.BitsPerSample,
            };
            var hotwords = cfg.HotwordsStoragePath ?? "";
            var tcs = new TaskCompletionSource<(TryllVoiceInput, TryllError)>();
            ulong noted = 0;
            TryllCalls.CreateVoiceInput(
                Connection, RequestOwner, cfg.ModelName, format,
                cfg.VadThreshold, cfg.VadMinSilenceMs, cfg.VadSpeechPadMs,
                hotwords, cfg.HotwordsScore,
                (body, err) =>
                {
                    if (!err.IsOk)
                    {
                        NoteInbound(TryllObservationNote.Reply(MessageBody.CreateVoiceInputRequest, false, noted).Fill(n => { n.Message = err.Message; n.Code = (uint)err.Code; }));
                        tcs.TrySetResult((null, err));
                        return;
                    }
                    var voice = new TryllVoiceInput(this, body.VoiceInputId);
                    _voiceInputsById[body.VoiceInputId] = voice;
                    NoteInbound(TryllObservationNote.Reply(MessageBody.CreateVoiceInputRequest, true, noted).Fill(n => { n.VoiceInputId = body.VoiceInputId; }));
                    tcs.TrySetResult((voice, TryllError.Ok()));
                },
                rid =>
                {
                    noted = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.CreateVoiceInputRequest, rid).Fill(n => { n.Text = cfg.ModelName; n.SampleRate = format.SampleRate; n.Channels = format.Channels; n.BitsPerSample = format.BitsPerSample; }));
                });
            return tcs.Task;
        }

        internal void RequestDestroyVoiceInputInternal(TryllVoiceInput voice)
        {
            var vid = voice.VoiceInputId;
            if (vid == 0) return;
            _voiceInputsById.Remove(vid);
            ulong notedId = 0;
            TryllCalls.DestroyVoiceInput(
                Connection, voice, vid,
                err =>
                {
                    if (!err.IsOk)
                    {
                        NoteInbound(TryllObservationNote.Reply(MessageBody.DestroyVoiceInputRequest, false, notedId).Fill(n => { n.Message = err.Message; n.VoiceInputId = vid; n.Code = (uint)err.Code; }));
                        Error?.Invoke(err);
                        return;
                    }
                    NoteInbound(TryllObservationNote.Reply(MessageBody.DestroyVoiceInputRequest, true, notedId).Fill(n => { n.VoiceInputId = vid; }));
                },
                rid =>
                {
                    notedId = rid;
                    NoteOutbound(TryllObservationNote.Request(MessageBody.DestroyVoiceInputRequest, rid).Fill(n => { n.VoiceInputId = vid; }));
                });
        }

        // ── Internal registration (used by TryllAgent) ─────────────────────────

        private static void SeedParamsBaseline(TryllAgent agent, TryllGraphDescription graph)
        {
            if (graph?.Nodes == null) return;
            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null) continue;
                agent.SetNodeParamsBaseline(node.Name, node.Params.Clone());
            }
        }

        internal void RegisterAgent(ulong agentId, TryllAgent agent)
        {
            if (agentId != 0) _agentsById[agentId] = agent;
        }

        internal void RegisterVoiceInput(ulong voiceInputId, TryllVoiceInput voice)
        {
            if (voiceInputId != 0) _voiceInputsById[voiceInputId] = voice;
        }

        // ── Event dispatch (main thread) ───────────────────────────────────────

        private void DispatchEvent(TryllConnEvent e)
        {
            if (e.Type == TryllConnEventType.Completion)
            {
                e.Completion?.Invoke();
                return;
            }
            switch (e.Type)
            {
            // ── Connection ─────────────────────────────────────────────────────
            case TryllConnEventType.Connected:
                TryllLog.Info("Connection ready (no session yet — call CreateSession)");
                // Do not clear ConfigureInFlight. IsConnected becomes true on the
                // socket thread before this event is dispatched, so a restored
                // editor window can already have sent the one-shot CreateSession.
                // Connect() already clears leftover flags from a previous socket.
                if (!HasState(SessionState.ConfigureInFlight))
                    ClearState(SessionState.Configured);
                if (!CaptureAndVerifyServerIdentity(e))
                    break; // fail-closed identity mismatch — already disconnected, no session
                if (HasState(SessionState.StartRequested))
                    // Create the session BEFORE notifying listeners: anything they send
                    // from ConnectionChanged is then ordered after CreateSession on the wire.
                    CreateSessionFromSettings();
                _announcedConnected = true;
                OnEditorConnectRetryStateChanged(true);
                ConnectionChanged?.Invoke(true);
                break;

            case TryllConnEventType.ConnectFailed:
                TryllLog.Warning($"Connect failed: {e.Message}");
                ClearState(SessionState.Configured | SessionState.ConfigureInFlight);
                DrainPendingClientRequests(new TryllError(TryllErrorCode.ConnectionFailed, e.Message));
                _announcedConnected = false;
                // Drive retry budget before broadcasting false so editor consumers
                // can tell a retryable accept-late miss from a terminal failure.
                if (HasState(SessionState.StartRequested) && Application.isPlaying)
                {
                    if (_connectRetryCoroutine == null)
                        _connectRetryCoroutine = StartCoroutine(ConnectRetry());
                }
                else if (HasEditorConnectConsumers)
                    OnEditorConnectRetryStateChanged(false);
                else
                    _remainingConnectAttempts = 0;
                ConnectionChanged?.Invoke(false);
                Error?.Invoke(new TryllError(TryllErrorCode.ConnectionFailed, e.Message));
                break;

            case TryllConnEventType.Disconnected:
                TryllLog.Info("Disconnected");
                // Established-session death is terminal for editor consumers.
                ClearEditorConnectRetry();
                _remainingConnectAttempts = 0;
                TeardownLiveSession(
                    new TryllError(TryllErrorCode.ConnectionDropped, e.Message ?? "Connection dropped"),
                    clearStartRequested: false,
                    notifyDisconnected: true);
                // StartSession owns bring-up until Disconnect(): re-establish after a drop.
                if (HasState(SessionState.StartRequested) && Application.isPlaying && _connectRetryCoroutine == null)
                    _connectRetryCoroutine = StartCoroutine(ConnectRetry());
                break;

            case TryllConnEventType.TtsAudioFormat:
                if (_agentsById.TryGetValue(e.AgentId, out var fmtAgent))
                    fmtAgent.HandleTtsAudioFormat(e.Message, (int)e.Code, e.TtsChannels, e.TtsBitsPerSample);
                break;

            case TryllConnEventType.TtsAudio:
                if (_agentsById.TryGetValue(e.AgentId, out var ttsAgent))
                    ttsAgent.HandleTtsAudio(e.Message, e.PcmBuffer, e.PcmLength);
                else if (e.PcmBuffer != null)
                    System.Buffers.ArrayPool<short>.Shared.Return(e.PcmBuffer);
                break;

            case TryllConnEventType.TtsViseme:
                if (_agentsById.TryGetValue(e.AgentId, out var visAgent))
                    visAgent.HandleTtsVisemes(e.Message, e.VisemeEvents);
                break;

            // ── Node event ─────────────────────────────────────────────────────
            case TryllConnEventType.NodeEvent:
            {
                var decoded = TryllNodeEventDecoder.Decode(e.Text, e.Message, e.KvPairs);
                bool dispatchedTyped = false;

                switch (decoded.Kind)
                {
                    case TryllNodeEventKind.ToolCall:
                        // Detailed is additive and does not swallow NodeEvent.
                        ToolCallDetailed?.Invoke(e.AgentId, decoded.ToolCall);
                        if (ToolCallNotificationWithId != null)
                        {
                            ToolCallNotificationWithId.Invoke(
                                e.AgentId,
                                decoded.ToolCall.CallId,
                                decoded.ToolCall.ToolName,
                                decoded.ToolCall.ArgumentsJson);
                            dispatchedTyped = true;
                        }
                        if (ToolCallNotification != null)
                        {
                            ToolCallNotification.Invoke(
                                e.AgentId, decoded.ToolCall.ToolName, decoded.ToolCall.ArgumentsJson);
                            dispatchedTyped = true;
                        }
                        break;

                    case TryllNodeEventKind.IntentClassified:
                        if (IntentClassified != null)
                        {
                            IntentClassified.Invoke(e.AgentId, decoded.Intent);
                            dispatchedTyped = true;
                        }
                        break;

                    case TryllNodeEventKind.IntentLlmClassified:
                        if (IntentLlmClassified != null)
                        {
                            IntentLlmClassified.Invoke(e.AgentId, decoded.IntentLlm);
                            dispatchedTyped = true;
                        }
                        break;

                    case TryllNodeEventKind.Paused:
                        // Public listeners run first, before the agent's auto-resume.
                        if (Paused != null)
                        {
                            Paused.Invoke(e.AgentId, decoded.NodeName, decoded.PendingExit);
                            dispatchedTyped = true;
                        }
                        break;
                }

                if (!dispatchedTyped)
                    NodeEvent?.Invoke(e.AgentId, decoded.NodeName, decoded.EventType, decoded.KvPairs);

                if (_agentsById.TryGetValue(e.AgentId, out var nodeAgent))
                {
                    bool sessionListener =
                        decoded.Kind == TryllNodeEventKind.Paused ? Paused != null
                        : decoded.Kind == TryllNodeEventKind.ToolCall
                            ? ToolCallDetailed != null
                              || ToolCallNotificationWithId != null
                              || ToolCallNotification != null
                        : false;
                    nodeAgent.HandleNodeEvent(decoded, sessionListener);
                }
                break;
            }




            case TryllConnEventType.TranscriptUpdate:
                if (_voiceInputsById.TryGetValue(e.VoiceInputId, out var tuVoice))
                    tuVoice.HandleTranscriptUpdate(e.Text, (TranscriptUpdateKind)e.TranscriptKind, e.AudioMsConsumed);
                break;

            }
        }

        private void NoteStorage(MessageBody request, bool ok, ulong requestId, TryllError err, string name)
        {
            NoteInbound(TryllObservationNote.Reply(request, ok, requestId).Fill(n =>
            {
                n.Text = name;
                if (!ok)
                {
                    n.Code = (uint)err.Code;
                    n.Message = err.Message;
                }
            }));
        }

        private static List<string> CopyStrings(List<string> source)
            => source == null ? null : new List<string>(source);

        private static List<TryllMutableKbInfo> MapStores(List<MutableKbInfoT> stores)
        {
            var mapped = new List<TryllMutableKbInfo>(stores?.Count ?? 0);
            if (stores == null) return mapped;
            foreach (var store in stores)
            {
                mapped.Add(new TryllMutableKbInfo
                {
                    Name = store.Name,
                    Schema = store.Schema,
                    LiveCount = store.LiveCount,
                    WriteGeneration = store.WriteGeneration,
                    Materialized = store.Materialized,
                    BytesEstimate = store.BytesEstimate,
                });
            }
            return mapped;
        }

        private static List<TryllMutableKbWriteResult> MapWriteResults(List<MutableKbWriteResultT> rows)
        {
            var mapped = new List<TryllMutableKbWriteResult>(rows?.Count ?? 0);
            if (rows == null) return mapped;
            foreach (var row in rows)
            {
                mapped.Add(new TryllMutableKbWriteResult
                {
                    Id = row.Id,
                    Inserted = row.Inserted,
                    Accepted = row.Accepted,
                    Error = row.Error,
                });
            }
            return mapped;
        }

        private static List<TryllMutableKbHit> MapHits(List<MutableKbHitT> hits)
        {
            var mapped = new List<TryllMutableKbHit>(hits?.Count ?? 0);
            if (hits == null) return mapped;
            foreach (var hit in hits)
            {
                mapped.Add(new TryllMutableKbHit
                {
                    Id = hit.Id,
                    Text = hit.Text,
                    Distance = hit.Distance,
                });
            }
            return mapped;
        }

        private static TryllMutableKbSnapshot UnpackMutableKb(PersistedMutableKbT snapshot)
        {
            if (snapshot == null)
                throw new ArgumentException("ExportMutableKbResponse missing snapshot");
            var fbb = new FlatBufferBuilder(256);
            fbb.Finish(PersistedMutableKb.Pack(fbb, snapshot).Value);
            return TryllMutableKbSnapshot.Unpack(
                PersistedMutableKb.GetRootAsPersistedMutableKb(new ByteBuffer(fbb.SizedByteArray())));
        }
    }
}
