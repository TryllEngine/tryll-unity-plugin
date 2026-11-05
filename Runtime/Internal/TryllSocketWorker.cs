// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
//
// Port of FTryllSocketWorker in TryllConnection.cpp.
// Runs on a background Thread; communicates with the main thread via two
// ConcurrentQueues owned by TryllConnection.

using System;
using System.Net.Sockets;
using System.Threading;
using Google.FlatBuffers;
using Tryll;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Background socket worker thread.
    /// Connects to the Tryll server, reads frames in a blocking loop, and decodes
    /// them into <see cref="TryllConnEvent"/> items pushed onto the event queue.
    /// Outbound commands are pulled from the command queue and encoded + written.
    ///
    /// Threading: all methods except <see cref="Run"/> and <see cref="Stop"/> are
    /// called from the main thread. <see cref="Run"/> executes on the worker thread.
    /// </summary>
    internal sealed class TryllSocketWorker
    {
        private readonly TryllConnection _owner;
        private readonly string          _host;
        private readonly int             _port;
        private volatile bool            _stop;

        private const int ConnectTimeoutMs = 5000;
        private const int PollIntervalMs   = 100;

        private readonly double _connectionReadyTimeoutSeconds;
        private readonly ulong  _generation;
        private bool _emittedTerminal;

        private readonly object _tcpLock = new object();
        private TcpClient _tcp;

        public TryllSocketWorker(TryllConnection owner, string host, int port,
            double connectionReadyTimeoutSeconds = 10.0, ulong generation = 0)
        {
            _owner = owner;
            _host  = host;
            _port  = port;
            _connectionReadyTimeoutSeconds = connectionReadyTimeoutSeconds > 0.0
                ? connectionReadyTimeoutSeconds
                : 10.0;
            _generation = generation;
        }

        public void Stop()
        {
            _stop = true;
            CloseTcp();
        }

        public void Run()
        {
            TcpClient tcp = null;
            try
            {
                // Connect with bounded timeout to avoid OS-default 21s hang.
                // ConnectAsync faults (e.g. connection refused) throw on Wait — that is a
                // normal ConnectFailed outcome, not an unexpected worker crash / Error log
                // (Unity EditMode LogAssert treats Error as test failure).
                tcp = new TcpClient();
                lock (_tcpLock)
                {
                    if (_stop)
                    {
                        try { tcp.Close(); } catch { }
                        return;
                    }
                    _tcp = tcp;
                }
                var connectTask = tcp.ConnectAsync(_host, _port);
                try
                {
                    if (!connectTask.Wait(ConnectTimeoutMs))
                    {
                        Enqueue(new TryllConnEvent
                        {
                            Type    = TryllConnEventType.ConnectFailed,
                            Message = $"Connect to {_host}:{_port} timed out",
                        });
                        return;
                    }
                }
                catch (AggregateException ex)
                {
                    var detail = ex.InnerException?.Message ?? ex.Message;
                    Enqueue(new TryllConnEvent
                    {
                        Type    = TryllConnEventType.ConnectFailed,
                        Message = $"Connect to {_host}:{_port} failed: {detail}",
                    });
                    return;
                }

                if (_stop)
                    return;

                if (!tcp.Connected)
                {
                    Enqueue(new TryllConnEvent
                    {
                        Type    = TryllConnEventType.ConnectFailed,
                        Message = $"Connect to {_host}:{_port} timed out or refused",
                    });
                    return;
                }

                var stream = tcp.GetStream();
                var socket = tcp.Client;

                // Bound the first-frame wait: a peer that accepts TCP but never
                // speaks Tryll (HTTP, a silent squat on 9100) would otherwise hang
                // this worker forever inside ReadFrame with no event emitted.
                int readyTimeoutMs = (int)(_connectionReadyTimeoutSeconds * 1000.0);
                if (readyTimeoutMs < 100) readyTimeoutMs = 100;

                byte[] firstFrame;
                try
                {
                    if (!WaitReadable(socket, readyTimeoutMs))
                    {
                        if (!_stop)
                            EnqueueConnectionReadyTimeout();
                        return;
                    }
                    // The hello has started arriving; the same budget bounds a
                    // peer that stalls mid-frame.
                    stream.ReadTimeout = readyTimeoutMs;
                    firstFrame = ClientWire.ReadFrame(stream);
                }
                catch (Exception ex) when (_stop || IsExpectedWorkerExit(ex))
                {
                    return;
                }
                catch (System.IO.IOException ex) when (IsReadTimeout(ex))
                {
                    EnqueueConnectionReadyTimeout();
                    return;
                }

                if (firstFrame == null)
                {
                    Enqueue(new TryllConnEvent
                    {
                        Type    = TryllConnEventType.ConnectFailed,
                        Message = "Connection closed before ConnectionReady",
                    });
                    return;
                }

                _owner.ObserverHub.ObserveWire(firstFrame, TryllObservationDirection.Inbound);
                var connectionReadyEvt = ClientCodec.Decode(firstFrame);
                if (connectionReadyEvt == null || connectionReadyEvt.Type != TryllConnEventType.Connected)
                {
                    // Decode already turns a version-mismatch hello into ConnectFailed;
                    // keep that message instead of a generic first-frame complaint.
                    if (connectionReadyEvt != null && connectionReadyEvt.Type == TryllConnEventType.ConnectFailed)
                    {
                        Enqueue(connectionReadyEvt);
                        return;
                    }
                    Enqueue(new TryllConnEvent
                    {
                        Type    = TryllConnEventType.ConnectFailed,
                        Message = "Expected ConnectionReady as first message",
                    });
                    return;
                }
                if (!_owner.IsActiveGeneration(_generation))
                    return;
                _owner.SetConnected(true);
                Enqueue(connectionReadyEvt);

                // Main loop: interleave command sends with frame reads.
                // Readiness comes from Socket.Poll, not a socket read timeout:
                // on Android a SO_RCVTIMEO expiry surfaces as WouldBlock instead
                // of TimedOut, and a timeout that fires mid-frame discards the
                // bytes ReadFrame already consumed. Once a frame starts arriving
                // it is read to the end; Stop() closes the socket to unblock it.
                stream.ReadTimeout = Timeout.Infinite;
                while (!_stop)
                {
                    // Drain outbound command queue first.
                    while (_owner.TryDequeueCmd(out var cmd))
                    {
                        if (!HandleCommand(stream, cmd))
                        {
                            _stop = true;
                            break;
                        }
                    }
                    if (_stop) break;

                    // Wait for the next server frame; an idle poll slice loops
                    // back to drain commands and check _stop.
                    byte[] frame;
                    try
                    {
                        if (!socket.Poll(PollIntervalMs * 1000, SelectMode.SelectRead))
                            continue;
                        frame = ClientWire.ReadFrame(stream);
                    }
                    catch (Exception ex) when (_stop || IsExpectedWorkerExit(ex))
                    {
                        break;
                    }
                    catch
                    {
                        frame = null;
                    }

                    if (frame == null)
                    {
                        EmitDisconnected("Connection dropped");
                        break;
                    }

                    HandleIncoming(frame);
                }

                // Intentional Stop() used to leave pending tasks stranded and skip
                // Disconnected, so session flags survived the next socket. Always
                // take the same terminal path as a peer drop.
                EmitDisconnected("Connection dropped");
            }
            catch (Exception ex) when (_stop || IsExpectedWorkerExit(ex))
            {
                if (IsDomainUnloadAbort(ex))
                    TryllLog.Warning(DomainUnloadDisconnectMessage);
                EmitDisconnected("Connection dropped");
            }
            catch (Exception ex)
            {
                TryllLog.Error($"TryllSocketWorker: unhandled exception: {ex}");
                EmitDisconnected(ex.Message);
            }
            finally
            {
                if (_owner.IsActiveGeneration(_generation))
                    _owner.SetConnected(false);
                lock (_tcpLock)
                {
                    if (ReferenceEquals(_tcp, tcp))
                        _tcp = null;
                }
                try { tcp?.Close(); } catch { }
            }
        }

        /// <summary>
        /// Polls in <see cref="PollIntervalMs"/> slices until the socket is
        /// readable (data or peer close), <paramref name="timeoutMs"/> elapses,
        /// or <see cref="Stop"/> is requested.
        /// </summary>
        private bool WaitReadable(Socket socket, int timeoutMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!_stop)
            {
                int remainingMs = timeoutMs - (int)sw.ElapsedMilliseconds;
                if (remainingMs <= 0)
                    return false;
                if (socket.Poll(Math.Min(remainingMs, PollIntervalMs) * 1000, SelectMode.SelectRead))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True when a stream read failed on its ReadTimeout. Windows reports
        /// TimedOut; Mono/IL2CPP on Android and iOS report the POSIX EAGAIN as
        /// WouldBlock even though the socket is in blocking mode.
        /// </summary>
        internal static bool IsReadTimeout(Exception ex)
            => ex?.InnerException is SocketException se &&
               (se.SocketErrorCode == SocketError.TimedOut ||
                se.SocketErrorCode == SocketError.WouldBlock);

        private void EnqueueConnectionReadyTimeout()
        {
            Enqueue(new TryllConnEvent
            {
                Type    = TryllConnEventType.ConnectFailed,
                Message = "Timed out waiting for ConnectionReady",
            });
        }

        // ── Outbound ──────────────────────────────────────────────────────────

        private bool HandleCommand(System.IO.Stream stream, TryllOutboundFrame cmd)
        {
            if (cmd.Frame == null)
                return true;
            switch (ClientWire.WriteFrame(stream, cmd.Frame))
            {
            case ClientWire.WriteFrameResult.Ok:
                return true;
            case ClientWire.WriteFrameResult.TooLarge:
                FailOneOversize(cmd);
                return true;
            default:
                return false;
            }
        }

        private void FailOneOversize(TryllOutboundFrame cmd)
        {
            if (cmd.RequestId != 0)
            {
                _owner.FailOneRegistered(
                    cmd.RequestId,
                    new TryllError(
                        TryllErrorCode.FrameTooLarge,
                        "Outbound frame exceeds MaxFramePayloadBytes"));
                return;
            }

            TryllLog.Warning(
                "TryllSocketWorker: dropped oversized fire-and-forget frame: Outbound frame exceeds MaxFramePayloadBytes");
        }

        // ── Inbound ───────────────────────────────────────────────────────────

        private void HandleIncoming(byte[] payload)
        {
            _owner.ObserverHub.ObserveWire(payload, TryllObservationDirection.Inbound);
            if (_owner.TryDispatchRegistered(payload))
                return;

            var evt = ClientCodec.Decode(payload);
            if (evt == null)
            {
                // Request-reply bodies are owned by the registry. A null mapper
                // means this frame is not a push we still route. A buffer that
                // does not parse is malformed and fails the socket.
                if (ParsesAsMessage(payload))
                    return;
                TryllLog.Warning("TryllSocketWorker: malformed server message");
                _owner.FailRegistered(new TryllError(TryllErrorCode.MalformedMessage, "Malformed server message"));
                _stop = true;
                return;
            }

            Enqueue(evt);
        }

        private static bool ParsesAsMessage(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return false;
            try
            {
                var msg = Message.GetRootAsMessage(new ByteBuffer(payload));
                _ = msg.BodyType;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void EmitDisconnected(string message)
        {
            if (_emittedTerminal) return;
            _emittedTerminal = true;
            _owner.FailRegistered(new TryllError(TryllErrorCode.ConnectionDropped, message ?? "Connection dropped"));
            Enqueue(new TryllConnEvent
            {
                Type    = TryllConnEventType.Disconnected,
                Message = message,
            });
        }

        internal const string DomainUnloadDisconnectMessage =
            "TryllSocketWorker: editor connection closed because Unity reloaded " +
            "the scripting domain (entering Play Mode or recompiling) and aborted " +
            "the client thread. Editor windows will reconnect.";

        /// <summary>
        /// True when the worker should exit without an Error log. Covers an
        /// intentional <see cref="Stop"/> (socket close) and Unity aborting the
        /// thread on domain reload.
        /// </summary>
        internal static bool IsExpectedWorkerExit(Exception ex)
            => IsStopInduced(ex) || IsDomainUnloadAbort(ex);

        /// <summary>
        /// Unity aborts leftover managed threads when the scripting domain
        /// unloads (Enter Play Mode with Reload Domain, or a script recompile).
        /// That is not a protocol failure — the editor client is being torn down.
        /// </summary>
        internal static bool IsDomainUnloadAbort(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is ThreadAbortException)
                    return true;
            }
            return false;
        }

        private static bool IsStopInduced(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is ObjectDisposedException)
                    return true;
                if (cur is SocketException se &&
                    (se.SocketErrorCode == SocketError.Interrupted ||
                     se.SocketErrorCode == SocketError.OperationAborted ||
                     se.SocketErrorCode == SocketError.ConnectionAborted ||
                     se.SocketErrorCode == SocketError.ConnectionReset ||
                     se.SocketErrorCode == SocketError.Shutdown))
                {
                    return true;
                }
            }
            return false;
        }

        private void CloseTcp()
        {
            lock (_tcpLock)
            {
                try { _tcp?.Close(); } catch { }
                _tcp = null;
            }
        }

        private void Enqueue(TryllConnEvent evt)
        {
            if (!_owner.IsActiveGeneration(_generation)) return;
            _owner.EnqueueEvent(evt);
        }

        /// <summary>EditMode seam: run one decoded server payload through <see cref="HandleIncoming"/>.</summary>
        internal void TestDeliver(byte[] payload) => HandleIncoming(payload);

        /// <summary>EditMode seam: the disconnect path that fails whatever is still pending.</summary>
        internal void TestDisconnect(string message) => EmitDisconnected(message);
    }
}
