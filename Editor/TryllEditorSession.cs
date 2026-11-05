// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// The generic half of an editor window's connection lifecycle: launch the managed
    /// server if needed, acquire a <see cref="TryllClient"/> connection, create the
    /// session, and (optionally) gate on required models being present (feat-011) —
    /// everything up to "the session is ready", with no opinion about which agent.
    ///
    /// Extracted from <see cref="TryllChatWindow"/>: that window's Start/Stop mixed this
    /// generic sequence with single-agent and voice-input concerns. The Dialog Lab needs
    /// the same sequence with neither — it creates its own agents per variant via
    /// <see cref="TryllDialogLabRunner"/> — so sharing this class keeps the two windows
    /// from drifting on server launch, ephemeral-port adoption, and the "never hard-kill
    /// a server other windows may be using" invariant.
    ///
    /// Chat and Dialog Lab must keep <c>requireRegisteredModels: true</c> — they create
    /// agents. Memory, Model Manager, and Knowledge must pass <c>false</c>: none of
    /// those windows create agents, and Model Manager is the surface that inspects
    /// and repairs missing registrations. Gating them on the play-mode download
    /// guard would block the catalog.
    /// </summary>
    internal sealed class TryllEditorSession
    {
        public enum State { Idle, LaunchingServer, Connecting, Configuring, CheckingModels, Ready, Error }

        public State CurrentState { get; private set; } = State.Idle;
        public string StatusText { get; private set; } = TryllEditorUiContract.StatusIdle;
        public TryllClient Client { get; private set; }

        /// <summary>
        /// Shared chrome phase. <see cref="State.LaunchingServer"/> collapses to
        /// <see cref="TryllOwnerPhase.Connecting"/> with detail "Launching server…".
        /// <see cref="State.CheckingModels"/> stays consumer-local and is not an owner phase.
        /// </summary>
        public TryllOwnerPhase OwnerPhase { get; private set; } = TryllOwnerPhase.Idle;

        /// <summary>Optional owner caption (launch text, last error). Null when Compose should use the default.</summary>
        public string OwnerDetail { get; private set; }

        /// <summary>Fired whenever <see cref="CurrentState"/>/<see cref="StatusText"/> change.</summary>
        public event Action<State, string> StateChanged;

        /// <summary>Fired once the session is created. When
        /// <c>requireRegisteredModels</c> is true, registered models are also
        /// confirmed present — the point at which a caller may create agents.</summary>
        public event Action SessionReady;

        private readonly TryllRuntimeSettings _settings;
        private readonly bool _requireRegisteredModels;
        private bool _acquiredConnection;
        private bool _hooked;
        private int _generation;
        private bool _sessionReadyEmitted;
        private bool _createSessionRequested;

        /// <summary>When true, SessionReady waits on the play-mode registered-model guard.</summary>
        internal bool RequiresRegisteredModels => _requireRegisteredModels;

        /// <summary>Whether this session holds one shared-connection lease.</summary>
        internal bool HoldsLease => _acquiredConnection;

        public TryllEditorSession(TryllRuntimeSettings settings, bool requireRegisteredModels = true)
        {
            _settings = settings;
            _requireRegisteredModels = requireRegisteredModels;
        }

        public bool IsIdle => CurrentState == State.Idle;

        public void Start()
        {
            if (_settings == null) { Fail("Assign a TryllRuntimeSettings asset"); return; }
            if (CurrentState != State.Idle && CurrentState != State.Error)
            {
                Fail("Session already starting");
                return;
            }

            _generation++;
            int generation = _generation;
            _sessionReadyEmitted = false;
            _createSessionRequested = false;

            if (DebugInhibitLaunchForTests)
            {
                DebugEnterConnectingForTests(TryllClient.EnsureEditorInstance());
                return;
            }

            SetState(State.LaunchingServer, "Launching server…");
            // Always call TryLaunch: IsRunning is true for a hung idle-exit
            // zombie (PID alive, ports closed) and TryLaunch reaps that child.
            if (!TryllServerProcess.TryLaunch(_settings, out var err))
            {
                if (err == TryllEditorUiContract.ServerNeedsLicence)
                {
                    // The licence gate has already opened the Welcome window. No dialog: several
                    // windows can be blocked at once. Retry works once the licence is accepted.
                    Fail(err);
                    return;
                }
                Fail($"Server launch failed: {err}");
                // err already carries the fix: build the server (development package) or
                // download it (released package), see TryllServerProcess.EditorMissingServerHint.
                TryllEditorPrompts.Notify("Tryll — cannot start server", err, "OK");
                TryllWelcomeWindow.OpenIfServerMissing();
                return;
            }

            if (generation != _generation) return;

            Client = TryllClient.EnsureEditorInstance();
            if (Client == null) { Fail("Failed to create TryllClient"); return; }

            // Already-configured shared client: CreateSession is one-shot and will
            // synchronously re-fire CreateSessionComplete to every subscriber. Only
            // accept that when this Start owns the configuring generation, and refuse
            // a silent settings mismatch.
            if (Client.IsSessionReady)
            {
                if (!SettingsMatchConfiguredClient(Client, _settings, out var mismatch))
                {
                    Fail(mismatch);
                    return;
                }
            }

            // TryLaunch may have fallen back to a free ephemeral port (a stranger already
            // held the known port) — adopt the actual port before connecting, or this
            // window would connect to the stale known port instead of the server it just
            // launched.
            if (TryllServerProcess.ActualPort > 0)
                Client.ServerPort = TryllServerProcess.ActualPort;
            TryllEditorConnectionPolicy.AcquireOnce(Client, ref _acquiredConnection);

            HookEvents();

            SetState(State.Connecting, TryllEditorUiContract.StatusConnecting);
            if (Client.IsConnected) OnConnectionChanged(true);
            else                    Client.Connect();
        }

        public void Stop()
        {
            // Invalidate every in-flight async continuation (model guard, etc.).
            _generation++;
            _sessionReadyEmitted = false;
            _createSessionRequested = false;

            if (Client != null)
            {
                UnhookEvents();
                if (_acquiredConnection) Client.ReleaseConnection();
            }
            _acquiredConnection = false;
            Client = null;
            // Do NOT kill the server: it is launched in managed mode and self-exits after
            // its idle grace once no client is connected. Hard-killing here would tear
            // down a server other editor windows may still be using and skips the
            // graceful shutdown (no flushed logs / telemetry).
            SetState(State.Idle, TryllEditorUiContract.StatusIdle);
        }

        private void HookEvents()
        {
            if (_hooked || Client == null) return;
            Client.ConnectionChanged     += OnConnectionChanged;
            Client.CreateSessionComplete += OnCreateSessionComplete;
            _hooked = true;
        }

        private void UnhookEvents()
        {
            if (!_hooked || Client == null) return;
            Client.ConnectionChanged     -= OnConnectionChanged;
            Client.CreateSessionComplete -= OnCreateSessionComplete;
            _hooked = false;
        }

        private void OnConnectionChanged(bool isConnected)
        {
            if (CurrentState == State.Idle) return;

            if (!isConnected)
            {
                if (CurrentState == State.Idle || CurrentState == State.Error)
                    return;
                if (TryllEditorConnectionPolicy.ShouldWaitForOwnerRetry(
                        isStarting: CurrentState == State.Connecting || CurrentState == State.LaunchingServer,
                        retriesRemaining: Client != null && Client.HasConnectRetriesRemaining))
                {
                    SetState(State.Connecting, "Waiting for server…");
                    return;
                }
                Fail("Disconnected");
                return;
            }

            // Only the helper that is actively bringing a session up may call
            // CreateSession / accept the synchronous already-configured notify.
            if (CurrentState != State.Connecting && CurrentState != State.Configuring)
                return;
            if (_createSessionRequested) return;
            _createSessionRequested = true;

            SetState(State.Configuring, "Configuring session…");
            var manifest = TryllModelManifest.LoadOrNull();
            using (Client.AsEditorCall())
            {
                Client.CreateSession(
                    _settings.BuildEngineDeclarations(manifest),
                    gameName:       _settings.GameName,
                    placementHints: _settings.BuildPlacementHints(manifest),
                    hotwordHints:   _settings.BuildHotwordHints(manifest));
            }
        }

        private async void OnCreateSessionComplete(TryllError error)
        {
            int generation = _generation;
            // Ignore broadcasts for other helpers / stale Starts. We only accept a
            // completion while this Start is configuring or checking models.
            if (CurrentState != State.Configuring && CurrentState != State.CheckingModels)
                return;
            if (!_createSessionRequested) return;

            if (!error.IsOk)
            {
                if (generation == _generation) Fail($"CreateSession failed: {error.Message}");
                return;
            }

            if (Client != null && Client.IsSessionReady
                && !SettingsMatchConfiguredClient(Client, _settings, out var mismatch))
            {
                if (generation == _generation) Fail(mismatch);
                return;
            }

            // feat-011 guard: ensure every registered (Production/Experimental) model is
            // downloaded before creating any agent — same blocking treatment as Play.
            // Memory skips this: it creates no agent and must not block Refresh on a
            // catalog download (Unreal's panel has the same skip).
            if (_requireRegisteredModels)
            {
                SetState(State.CheckingModels, "Checking required models…");
                var (ready, guardError) = await TryllPlayModeModelGuard.EnsureRegisteredModelsReady();
                if (generation != _generation) return;

                if (!ready)
                {
                    Fail(string.IsNullOrEmpty(guardError) ? "Model download cancelled." : guardError);
                    return;
                }
            }

            if (_sessionReadyEmitted) return;
            _sessionReadyEmitted = true;
            SetState(State.Ready, TryllEditorUiContract.StatusReady);
            SessionReady?.Invoke();
        }

        private static bool SettingsMatchConfiguredClient(
            TryllClient client, TryllRuntimeSettings settings, out string error)
        {
            error = null;
            // The wire session is already created; we cannot observe its engines after
            // the fact. Best effort: if the client's Settings asset differs from ours
            // on the fields CreateSession uses, refuse rather than silently keep the
            // first window's configuration.
            var live = client.Settings;
            if (live == null) return true;
            if (live == settings) return true;

            // Engines are no longer a per-asset setting — they are derived from the
            // project's single model manifest, so two settings assets cannot
            // disagree about them. Only the fields a user can still set differ.
            if (!string.Equals(live.GameName ?? "", settings.GameName ?? "", StringComparison.Ordinal)
                || !string.Equals(live.StorageDataFolder ?? "", settings.StorageDataFolder ?? "", StringComparison.Ordinal))
            {
                error = "Shared Tryll client is already configured with different session settings. "
                      + "Stop the other Tryll editor window (or wait for its idle disconnect) and retry.";
                return false;
            }
            return true;
        }

        private void SetState(State state, string status)
        {
            CurrentState = state;
            StatusText = status;
            MapOwnerPhase(state, status);
            StateChanged?.Invoke(state, status);
        }

        private void MapOwnerPhase(State state, string status)
        {
            switch (state)
            {
                case State.Idle:
                    OwnerPhase = TryllOwnerPhase.Idle;
                    OwnerDetail = null;
                    break;
                case State.LaunchingServer:
                    OwnerPhase = TryllOwnerPhase.Connecting;
                    OwnerDetail = "Launching server…";
                    break;
                case State.Connecting:
                    OwnerPhase = TryllOwnerPhase.Connecting;
                    OwnerDetail = string.IsNullOrEmpty(status) || status == TryllEditorUiContract.StatusConnecting
                        ? null
                        : status;
                    break;
                case State.Configuring:
                    OwnerPhase = TryllOwnerPhase.Configuring;
                    OwnerDetail = string.IsNullOrEmpty(status) || status == TryllEditorUiContract.StatusConfiguring
                        ? null
                        : status;
                    break;
                case State.CheckingModels:
                    // Consumer-local (Chat). Owner chrome stays Configuring so
                    // other windows do not grow a CheckingModels phase.
                    OwnerPhase = TryllOwnerPhase.Configuring;
                    OwnerDetail = null;
                    break;
                case State.Ready:
                    OwnerPhase = TryllOwnerPhase.Ready;
                    OwnerDetail = null;
                    break;
                case State.Error:
                    OwnerPhase = TryllOwnerPhase.Error;
                    OwnerDetail = status;
                    break;
                default:
                    OwnerPhase = TryllOwnerPhase.Idle;
                    OwnerDetail = null;
                    break;
            }
        }

        private void Fail(string reason) => SetState(State.Error, reason);

        /// <summary>
        /// When true, <see cref="Start"/> skips the server launch and connect and
        /// binds the shared client in Connecting, as
        /// <see cref="DebugEnterConnectingForTests"/> does. Lets window tests
        /// drive owner phases with injected client events.
        /// </summary>
        internal static bool DebugInhibitLaunchForTests { get; set; }

        /// <summary>
        /// Bind an existing client and enter Connecting without launching a server.
        /// Used by owner-recovery tests that inject connection events.
        /// </summary>
        internal void DebugEnterConnectingForTests(TryllClient client)
        {
            Client = client;
            TryllEditorConnectionPolicy.AcquireOnce(Client, ref _acquiredConnection);
            HookEvents();
            SetState(State.Connecting, TryllEditorUiContract.StatusConnecting);
        }

        /// <summary>
        /// Bind an existing client and enter Configuring with CreateSession already
        /// requested. Used to inject <c>CreateSessionComplete</c> without a wire server.
        /// </summary>
        internal void DebugEnterConfiguringForTests(TryllClient client)
        {
            Client = client;
            TryllEditorConnectionPolicy.AcquireOnce(Client, ref _acquiredConnection);
            HookEvents();
            _createSessionRequested = true;
            SetState(State.Configuring, TryllEditorUiContract.StatusConfiguring);
        }
    }
}
