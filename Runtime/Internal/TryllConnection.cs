// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
//
// Port of FTryllConnection in TryllConnection.h/.cpp.
// Thread-safe bridge between the Unity main thread and the background socket worker.

using System.Collections.Concurrent;
using System.Threading;
using Tryll.Client;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Thread-safe bridge between the main thread and <see cref="TryllSocketWorker"/>.
    ///
    /// Main thread pushes commands; worker thread enqueues events.
    /// <see cref="TryllClient"/> drains events on its <c>Update()</c> call.
    ///
    /// Command enqueue is main-thread only.
    /// <see cref="TryDequeueEvent"/> is also main-thread-only.
    /// <see cref="TryDequeueCmd"/> and <see cref="EnqueueEvent"/> are worker-thread-only.
    /// </summary>
    internal sealed partial class TryllConnection
    {
        internal TryllClientObserverHub ObserverHub { get; } = new TryllClientObserverHub();

        private ulong _nextRequestId = 1;
        private volatile bool _connected = false;
        private long          _sessionId = 0;

        private readonly ConcurrentQueue<TryllConnEvent> _eventQueue = new ConcurrentQueue<TryllConnEvent>();
        private readonly ConcurrentQueue<TryllOutboundFrame> _cmdQueue = new ConcurrentQueue<TryllOutboundFrame>();

        private Thread            _workerThread;
        private TryllSocketWorker _worker;
        private long              _workerGeneration;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        public void Start(string host, int port)
            => Start(host, port, ResolveConnectionReadyTimeout());

        public void Start(string host, int port, double connectionReadyTimeoutSeconds)
        {
            // Retire the previous epoch first so a blocked ConnectionReady wait
            // cannot enqueue into the replacement socket.
            Interlocked.Increment(ref _workerGeneration);
            Stop();
            // Entries the retiring worker did not already fail (R-A12).
            FailRegistered(new TryllError(TryllErrorCode.ConnectionDropped, "Connection dropped"));

            _connected = false;
            _sessionId = 0;
            _nextRequestId = 1;
            ObserverHub.BeginConnectionEpoch();

            DiscardQueuedTransport();

            var generation = unchecked((ulong)Interlocked.Read(ref _workerGeneration));
            _worker = new TryllSocketWorker(this, host, port, connectionReadyTimeoutSeconds, generation);
            _workerThread = new Thread(_worker.Run)
            {
                IsBackground = true,
                Name         = "TryllSocketWorker",
            };
            _workerThread.Start();
        }

        public void Stop()
        {
            _worker?.Stop();
            if (_workerThread != null && _workerThread.IsAlive)
                _workerThread.Join();
            _workerThread = null;
            _worker       = null;
            _connected    = false;
        }

        internal void DiscardQueuedTransport()
        {
            while (_cmdQueue.TryDequeue(out _))  { }
            // A reply may already have left the registry and posted its completion.
            // Dropping that item loses the call. Run completions; drop pushes and
            // connection-state events, which belong to the retiring socket.
            while (_eventQueue.TryDequeue(out var evt))
            {
                if (evt.Type == TryllConnEventType.Completion)
                    evt.Completion?.Invoke();
            }
        }

        internal bool IsActiveGeneration(ulong generation)
            => unchecked((ulong)Interlocked.Read(ref _workerGeneration)) == generation;

        internal bool DebugForceWorkerAliveForTests { get; set; }

        internal bool IsWorkerAlive
            => DebugForceWorkerAliveForTests || (_workerThread != null && _workerThread.IsAlive);

        public bool IsConnected  => _connected;
        public ulong SessionId   => unchecked((ulong)Interlocked.Read(ref _sessionId));

        private static double ResolveConnectionReadyTimeout()
        {
            double readyTimeout = 10.0;
            var settings = TryllRuntimeSettings.Load();
            if (settings != null && settings.ConnectionReadyTimeoutSeconds > 0.0)
                readyTimeout = settings.ConnectionReadyTimeoutSeconds;
            return readyTimeout;
        }

        // Called from worker thread.
        internal void SetConnected(bool value) => _connected = value;
        internal void SetSessionId(ulong id)   => Interlocked.Exchange(ref _sessionId, unchecked((long)id));

        // ── Event queue (worker → main) ────────────────────────────────────────

        internal void EnqueueEvent(TryllConnEvent evt) => _eventQueue.Enqueue(evt);

        public bool TryDequeueEvent(out TryllConnEvent evt)
        {
            if (!_eventQueue.TryDequeue(out evt)) return false;
            // Wire pushes and replies are journaled by the inbound frame tap.
            // Connection state is not a frame.
            if (ObserverHub.HasSubscribers && IsConnectionEvent(evt.Type))
                ObserverHub.ObserveQueuedEvent(evt);
            return true;
        }

        private static bool IsConnectionEvent(TryllConnEventType type)
        {
            return type == TryllConnEventType.Connected
                || type == TryllConnEventType.ConnectFailed
                || type == TryllConnEventType.Disconnected;
        }

        // ── Command queue (main → worker) ──────────────────────────────────────

        internal bool TryDequeueCmd(out TryllOutboundFrame cmd) => _cmdQueue.TryDequeue(out cmd);

        private void PushCmd(TryllOutboundFrame cmd)
        {
            if (cmd.Frame != null)
                ObserverHub.ObserveWire(cmd.Frame, TryllObservationDirection.Outbound);
            _cmdQueue.Enqueue(cmd);
        }

        // ── Request ID allocation ──────────────────────────────────────────────

        // AllocateRequestId is main-thread-only — no atomics needed.
        public ulong AllocateRequestId() => _nextRequestId++;

    }

    /// <summary>One pre-encoded outbound frame plus the request id the registry already holds.</summary>
    internal readonly struct TryllOutboundFrame
    {
        public readonly ulong RequestId;
        public readonly byte[] Frame;

        public TryllOutboundFrame(ulong requestId, byte[] frame)
        {
            RequestId = requestId;
            Frame = frame;
        }
    }
}
