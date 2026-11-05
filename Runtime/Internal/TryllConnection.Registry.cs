// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
//
// One pending registry for the connection (R-A1). A caller registers before it
// enqueues the frame (R-A2). The socket worker runs the handler; the handler
// posts plain completions onto the same queue as push events (R-A5).

using System;
using System.Collections.Generic;
using System.Threading;
using Google.FlatBuffers;
using Tryll;

namespace Tryll.Client.Internal
{
    internal delegate bool TryllPendingHandler(Message frame, TryllError error);

    internal sealed class TryllPendingEntry
    {
        public TryllPendingHandler Handler;
        public object Owner;
        public MessageBody RequestBody;
        // 0 live, 1 claimed. A terminal reply and a teardown share one winner.
        public int Done;
    }

    internal sealed partial class TryllConnection
    {
        private readonly int _callerThreadId = Thread.CurrentThread.ManagedThreadId;
        private readonly object _registryLock = new object();
        private readonly Dictionary<ulong, TryllPendingEntry> _registry = new Dictionary<ulong, TryllPendingEntry>();

        internal bool OnCallerThread => Thread.CurrentThread.ManagedThreadId == _callerThreadId;

        internal ulong NextRequestId() => AllocateRequestId();

        internal ulong Generation => unchecked((ulong)Interlocked.Read(ref _workerGeneration));

        /// <summary>
        /// Register <paramref name="handler"/> then queue <paramref name="frame"/>.
        /// The frame is already encoded on the caller.
        /// </summary>
        internal void Call(
            ulong requestId,
            byte[] frame,
            object owner,
            TryllPendingHandler handler,
            MessageBody requestBody,
            Action<ulong> note = null)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            lock (_registryLock)
            {
                _registry[requestId] = new TryllPendingEntry
                {
                    Handler = handler,
                    Owner = owner,
                    RequestBody = requestBody,
                };
            }
            // Before the frame is queued, so a fast reply cannot be logged first.
            note?.Invoke(requestId);
            PushCmd(new TryllOutboundFrame(requestId, frame));
        }

        /// <summary>Fire-and-forget. No registry entry.</summary>
        internal void Send(byte[] frame)
        {
            if (frame == null)
                return;
            PushCmd(new TryllOutboundFrame(0, frame));
        }

        /// <summary>Worker (or caller) to main-thread FIFO. Same queue as connection events.</summary>
        internal void Post(Action completion)
        {
            if (completion == null)
                return;
            _eventQueue.Enqueue(new TryllConnEvent
            {
                Type = TryllConnEventType.Completion,
                Completion = completion,
            });
        }

        /// <summary>
        /// Fail every entry owned by <paramref name="owner"/>. Handlers run before this returns.
        /// </summary>
        internal void FailOwnedBy(object owner, TryllError error)
        {
            if (owner == null)
                return;
            List<TryllPendingEntry> doomed = null;
            List<ulong> keys = null;
            lock (_registryLock)
            {
                foreach (var kv in _registry)
                {
                    if (!ReferenceEquals(kv.Value.Owner, owner))
                        continue;
                    if (Interlocked.CompareExchange(ref kv.Value.Done, 1, 0) != 0)
                        continue;
                    keys ??= new List<ulong>();
                    doomed ??= new List<TryllPendingEntry>();
                    keys.Add(kv.Key);
                    doomed.Add(kv.Value);
                }
                if (keys != null)
                {
                    foreach (var key in keys)
                        _registry.Remove(key);
                }
            }
            if (doomed == null)
                return;
            if (keys != null)
            {
                for (int i = 0; i < keys.Count; ++i)
                    NoteTransportFailure(keys[i], doomed[i].RequestBody, error);
            }
            foreach (var entry in doomed)
                InvokeHandler(entry, default, error);
        }

        internal void NoteUnexpectedReply(ulong requestId)
        {
            if (!ObserverHub.HasSubscribers) return;
            ObserverHub.ObserveLocal(
                TryllAgentLogLocal.Error(
                    (uint)TryllErrorCode.MalformedMessage,
                    "MalformedMessage",
                    "unexpected reply",
                    LogErrorOrigin.unexpected_reply),
                requestId, 0, true, "unexpected reply", (uint)TryllErrorCode.MalformedMessage);
        }

        private void NoteTransportFailure(ulong requestId, MessageBody requestBody, TryllError error)
        {
            if (!ObserverHub.HasSubscribers) return;
            var origin = error.Code == (int)TryllErrorCode.MalformedMessage
                ? LogErrorOrigin.unexpected_reply
                : LogErrorOrigin.transport;
            var codeName = Enum.IsDefined(typeof(TryllErrorCode), error.Code)
                ? ((TryllErrorCode)error.Code).ToString()
                : error.Code.ToString();
            ObserverHub.ObserveLocal(
                TryllAgentLogLocal.Error((uint)error.Code, codeName, error.Message, origin),
                requestId, 0, true, error.Message, (uint)error.Code, requestBody);
        }

        internal void FailOneRegistered(ulong requestId, TryllError error)
        {
            TryllPendingEntry entry;
            lock (_registryLock)
            {
                if (!_registry.TryGetValue(requestId, out entry))
                    return;
                if (Interlocked.CompareExchange(ref entry.Done, 1, 0) != 0)
                    return;
                _registry.Remove(requestId);
            }
            NoteTransportFailure(requestId, entry.RequestBody, error);
            InvokeHandler(entry, default, error);
        }

        internal void FailRegistered(TryllError error)
        {
            List<TryllPendingEntry> doomed;
            List<ulong> requestIds;
            lock (_registryLock)
            {
                if (_registry.Count == 0)
                    return;
                doomed = new List<TryllPendingEntry>();
                requestIds = new List<ulong>();
                foreach (var pair in _registry)
                {
                    if (Interlocked.CompareExchange(ref pair.Value.Done, 1, 0) == 0)
                    {
                        doomed.Add(pair.Value);
                        requestIds.Add(pair.Key);
                    }
                }
                _registry.Clear();
            }
            for (int i = 0; i < requestIds.Count; ++i)
                NoteTransportFailure(requestIds[i], doomed[i].RequestBody, error);
            foreach (var entry in doomed)
                InvokeHandler(entry, default, error);
        }

        /// <summary>
        /// If <paramref name="payload"/> answers a registered request, run its handler
        /// and return true. Otherwise leave the legacy decode path to handle it.
        /// </summary>
        internal bool TryDispatchRegistered(byte[] payload)
        {
            Message msg;
            try
            {
                msg = Message.GetRootAsMessage(new ByteBuffer(payload));
            }
            catch (Exception)
            {
                return false;
            }

            var requestId = ClientCodec.RequestIdOf(msg);
            if (requestId == 0)
                return false;

            TryllPendingEntry entry;
            lock (_registryLock)
            {
                if (!_registry.TryGetValue(requestId, out entry))
                    return false;
                if (Volatile.Read(ref entry.Done) != 0)
                    return true;
            }

            // TTS frames share SendMessage's request id but are pushes, not the
            // reply. Leave the entry registered and let the legacy path deliver them.
            if (IsUnrelatedPush(msg.BodyType, entry))
                return false;

            // A progress frame must not take the entry. A SendMessage error
            // other than AgentBusy must not either: TurnComplete still arrives.
            // Anything else claims the entry first, so teardown cannot complete
            // it as well.
            var progress = IsProgressFrame(msg.BodyType, entry);
            // Only a SendMessage error that is still followed by TurnComplete
            // leaves the entry in place. AgentBusy, and every other request's
            // error, ends the call, so teardown must not complete it as well.
            var streamingError = false;
            if (entry.RequestBody == MessageBody.SendMessageRequest
                && msg.BodyType == MessageBody.ErrorResponse)
            {
                streamingError = msg.BodyAsErrorResponse().Code != (uint)TryllErrorCode.AgentBusy;
            }
            if (!progress && !streamingError)
            {
                if (Interlocked.CompareExchange(ref entry.Done, 1, 0) != 0)
                    return true;
                lock (_registryLock)
                {
                    if (_registry.TryGetValue(requestId, out var current) && ReferenceEquals(current, entry))
                        _registry.Remove(requestId);
                }
            }
            else if (Volatile.Read(ref entry.Done) != 0)
            {
                return true;
            }

            var terminal = true;
            try
            {
                terminal = entry.Handler(msg, TryllError.Ok());
            }
            catch (Exception ex)
            {
                TryllLog.Warning("Tryll pending handler threw: " + ex.Message);
                terminal = true;
            }

            if ((progress || streamingError) && terminal
                && Interlocked.CompareExchange(ref entry.Done, 1, 0) == 0)
            {
                lock (_registryLock)
                {
                    if (_registry.TryGetValue(requestId, out var current) && ReferenceEquals(current, entry))
                        _registry.Remove(requestId);
                }
            }
            return true;
        }

        private static bool IsProgressFrame(MessageBody body, TryllPendingEntry entry)
        {
            var progress = ClientCodec.RpcProgressOf(entry.RequestBody);
            return progress != MessageBody.NONE && body == progress;
        }

        private static bool IsUnrelatedPush(MessageBody body, TryllPendingEntry entry)
        {
            if (body == MessageBody.ErrorResponse)
                return false;
            var terminal = ClientCodec.RpcTerminalOf(entry.RequestBody);
            var progress = ClientCodec.RpcProgressOf(entry.RequestBody);
            if (body == terminal || (progress != MessageBody.NONE && body == progress))
                return false;
            return ClientCodec.IsRequestScopedPush(body);
        }

        private static void InvokeHandler(TryllPendingEntry entry, Message frame, TryllError error)
        {
            try
            {
                entry.Handler?.Invoke(frame, error);
            }
            catch (Exception ex)
            {
                TryllLog.Warning("Tryll pending teardown threw: " + ex.Message);
            }
        }
    }

    internal static class TryllConnectionCalls
    {
        public static void CallUnary<R>(
            this TryllConnection conn,
            object owner,
            ulong requestId,
            byte[] frame,
            MessageBody requestBody,
            MessageBody expected,
            Func<Message, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            if (frame == null)
            {
                conn.Post(() => done(default, new TryllError(
                    TryllErrorCode.MalformedMessage, "empty frame")));
                return;
            }

            conn.Call(requestId, frame, owner, (msg, err) =>
            {
                void Finish(R value, TryllError error)
                {
                    if (conn.OnCallerThread)
                        done(value, error);
                    else
                        conn.Post(() => done(value, error));
                }

                if (!err.IsOk)
                {
                    Finish(default, err);
                    return true;
                }
                if (msg.ByteBuffer != null && msg.BodyType == MessageBody.ErrorResponse)
                {
                    var body = msg.BodyAsErrorResponse();
                    Finish(default, new TryllError(body.Code, body.Message));
                    return true;
                }
                if (msg.ByteBuffer != null && msg.BodyType == expected)
                {
                    R value = default;
                    var projected = TryllError.Ok();
                    try
                    {
                        value = project(msg);
                    }
                    catch (Exception ex)
                    {
                        projected = new TryllError(TryllErrorCode.MalformedMessage, ex.Message);
                    }
                    Finish(value, projected);
                    return true;
                }
                conn.NoteUnexpectedReply(requestId);
                Finish(default, new TryllError(TryllErrorCode.MalformedMessage, "unexpected reply"));
                return true;
            }, requestBody, note);
        }

        public static void CallAck(
            this TryllConnection conn,
            object owner,
            ulong requestId,
            byte[] frame,
            MessageBody requestBody,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            conn.CallUnary(
                owner, requestId, frame, requestBody, MessageBody.Ack,
                (Message msg) => msg.BodyAsAck().UnPack().Warning,
                (string warning, TryllError err) =>
                {
                    if (err.IsOk && !string.IsNullOrEmpty(warning))
                        err = new TryllError(0u, warning);
                    done(err);
                },
                note);
        }
    }
}
