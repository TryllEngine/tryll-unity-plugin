// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Google.FlatBuffers;
using Tryll;
using Tryll.Client;

namespace Tryll.Client.Internal
{
    internal interface ITryllClientObserver
    {
        void OnObservation(TryllObservationEnvelope envelope); // must not throw
    }

    internal sealed class TryllNullClientObserver : ITryllClientObserver
    {
        public static readonly TryllNullClientObserver Instance = new TryllNullClientObserver();

        private TryllNullClientObserver() { }

        public void OnObservation(TryllObservationEnvelope envelope) { }
    }

    internal sealed class TryllCompositeClientObserver : ITryllClientObserver
    {
        private readonly List<ITryllClientObserver> _observers = new List<ITryllClientObserver>();

        public void Add(ITryllClientObserver observer)
        {
            if (observer != null) _observers.Add(observer);
        }

        public void OnObservation(TryllObservationEnvelope envelope)
        {
            for (int i = 0; i < _observers.Count; ++i)
            {
                try
                {
                    _observers[i].OnObservation(envelope);
                }
                catch (Exception ex)
                {
                    TryllLog.Warning($"TryllCompositeClientObserver consumer failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Hub: O(1) enqueue on wire path; <see cref="Pump"/> delivers to subscribers
    /// after client dispatch.
    /// </summary>
    internal sealed class TryllClientObserverHub
    {
        private readonly ConcurrentQueue<TryllObservationEnvelope> _queue = new ConcurrentQueue<TryllObservationEnvelope>();
        private readonly object _gate = new object();
        private readonly List<ITryllClientObserver> _subscribers = new List<ITryllClientObserver>();
        private long _nextEventId = 1;
        private readonly Dictionary<ulong, ulong> _carriers = new Dictionary<ulong, ulong>();

        public ulong ConnectionEpoch { get; private set; }

        public bool HasSubscribers
        {
            get
            {
                lock (_gate) return _subscribers.Count > 0;
            }
        }

        public void BeginConnectionEpoch()
        {
            ConnectionEpoch++;
            lock (_gate) _carriers.Clear();
        }

        public ulong NextEventId() => (ulong)Interlocked.Increment(ref _nextEventId);

        public IDisposable Subscribe(ITryllClientObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            lock (_gate) _subscribers.Add(observer);
            return new Subscription(this, observer);
        }

        /// <summary>
        /// Capture one finished frame. Copies and strips only when someone is subscribed.
        /// </summary>
        public void AnnotateCarrier(ulong requestId, ulong carrierRequestId)
        {
            if (requestId == 0 || carrierRequestId == 0 || !HasSubscribers) return;
            lock (_gate) _carriers[requestId] = carrierRequestId;
        }

        public void ObserveWire(byte[] frame, TryllObservationDirection direction)
        {
            if (!HasSubscribers || frame == null || frame.Length == 0) return;
            try
            {
                var copy = new byte[frame.Length];
                Buffer.BlockCopy(frame, 0, copy, 0, frame.Length);
                var omitted = new List<TryllOmittedField>();
                var stripped = TryllAgentLogJson.StripMessage(copy, omitted);
                TryllAgentLogJson.ReadScope(copy, out var body, out var requestId, out var agentId, out var voiceId);
                MessageBody requestBody = body;
                if (direction == TryllObservationDirection.Inbound
                    && TryllMessageDescriptors.TryGet(body, out var descriptor)
                    && descriptor.Request != MessageBody.NONE)
                {
                    requestBody = descriptor.Request;
                }
                var note = new TryllObservationNote
                {
                    Direction = direction,
                    Body = body,
                    RequestBody = requestBody,
                    RequestId = requestId,
                    AgentId = agentId,
                    VoiceInputId = voiceId,
                    Text = TryllAgentLogJson.Label(body, copy),
                };
                Hydrate(note, copy);
                lock (_gate)
                {
                    if (requestId != 0 && _carriers.TryGetValue(requestId, out var carrier))
                    {
                        note.CarrierRequestId = carrier;
                        _carriers.Remove(requestId);
                    }
                }
                var envelope = TryllObservationSnapshot.FromNote(note, NextEventId(), ConnectionEpoch);
                envelope.Frame = stripped ?? copy;
                envelope.Omitted = omitted;
                _queue.Enqueue(envelope);
            }
            catch (Exception)
            {
                // A buffer that does not parse is not a journal row. The socket
                // worker reports malformed input on its own path.
            }
        }

        /// <summary>
        /// Journal one local event. <paramref name="frame"/> is a finished LogLocalEnvelope.
        /// </summary>
        public void ObserveLocal(
            byte[] frame,
            ulong requestId,
            ulong agentId,
            bool isError,
            string message,
            uint code,
            MessageBody requestBody = MessageBody.NONE)
        {
            if (!HasSubscribers || frame == null || frame.Length == 0) return;
            var envelope = new TryllObservationEnvelope
            {
                EventId = NextEventId(),
                ConnectionEpoch = ConnectionEpoch,
                Direction = TryllObservationDirection.Inbound,
                Scope = TryllObservationScope.RawSession,
                WallTimeUtc = DateTime.UtcNow,
                MonotonicTicks = Stopwatch.GetTimestamp(),
                RequestId = requestId,
                AgentId = agentId,
                RequestBody = requestBody,
                IsError = isError,
                Message = message,
                Code = code,
                Frame = frame,
                IsLocal = true,
            };
            _queue.Enqueue(envelope);
        }

        /// <summary>Hot path: if !HasSubscribers return; else enqueue snapshot. Never call observer here.</summary>
        public void Observe(TryllObservationNote note)
        {
            if (!HasSubscribers || note == null) return;
            _queue.Enqueue(TryllObservationSnapshot.FromNote(note, NextEventId(), ConnectionEpoch));
        }

        /// <summary>
        /// Connection-state records and pushes that still arrive on the event queue.
        /// Completions are noted by the wrapper before they are posted.
        /// </summary>
        public void ObserveQueuedEvent(TryllConnEvent evt)
        {
            if (!HasSubscribers || evt == null) return;
            var envelope = TryllObservationSnapshot.FromQueuedEvent(evt, NextEventId(), ConnectionEpoch);
            AttachConnectionFrame(evt, envelope);
            _queue.Enqueue(envelope);
        }

        private static void AttachConnectionFrame(TryllConnEvent evt, TryllObservationEnvelope envelope)
        {
            LogConnState state;
            string reason;
            switch (evt.Type)
            {
            case TryllConnEventType.Connected:
                state = LogConnState.connected;
                reason = evt.ServerVersion;
                break;
            case TryllConnEventType.ConnectFailed:
                state = LogConnState.failed;
                reason = evt.Message;
                break;
            case TryllConnEventType.Disconnected:
                state = LogConnState.dropped;
                reason = evt.Message;
                break;
            default:
                return;
            }
            envelope.Frame = TryllAgentLogLocal.Connection(state, reason);
            envelope.IsLocal = true;
        }

        /// <summary>
        /// Fill the fields the turn inspector still reads from the frame, so a tap
        /// can replace the per-call note without emptying the current window.
        /// </summary>
        private static void Hydrate(TryllObservationNote note, byte[] frame)
        {
            var msg = Message.GetRootAsMessage(new ByteBuffer(frame));
            switch (note.Body)
            {
            case MessageBody.AnswerText:
            {
                var answer = msg.BodyAsAnswerText();
                note.Text = answer.Text;
                note.NodeName = answer.NodeName;
                note.Flag = answer.IsDelta;
                note.Flag2 = answer.IsFinal;
                break;
            }
            case MessageBody.SendMessageRequest:
                note.Text = msg.BodyAsSendMessageRequest().Text;
                break;
            case MessageBody.ResumeAgentRequest:
                note.Text = msg.BodyAsResumeAgentRequest().ResumeNode;
                break;
            case MessageBody.ChangeAgentParamRequest:
                note.NodeName = msg.BodyAsChangeAgentParamRequest().NodeName;
                break;
            case MessageBody.TurnComplete:
            {
                var turn = msg.BodyAsTurnComplete();
                note.Text = turn.DebugInfo;
                note.TurnStatus = (byte)turn.Status;
                note.TokensGenerated = turn.TokensGenerated;
                break;
            }
            case MessageBody.ErrorResponse:
            {
                var error = msg.BodyAsErrorResponse();
                note.Message = error.Message;
                note.Code = error.Code;
                note.IsError = true;
                break;
            }
            case MessageBody.NodeEvent:
                note.NodeName = msg.BodyAsNodeEvent().NodeName;
                break;
            case MessageBody.TtsAudioFormatFrame:
                note.NodeName = msg.BodyAsTtsAudioFormatFrame().NodeName;
                break;
            case MessageBody.TtsAudioFrame:
                note.NodeName = msg.BodyAsTtsAudioFrame().NodeName;
                break;
            case MessageBody.TtsVisemeFrame:
                note.NodeName = msg.BodyAsTtsVisemeFrame().NodeName;
                break;
            case MessageBody.CreateAgentRequest:
            {
                var create = msg.BodyAsCreateAgentRequest();
                note.GraphDefaultModelName = create.DefaultModelName;
                note.EnableDiagnostics = create.EnableDiagnostics;
                note.MaintainDialogueHistory = create.MaintainDialogueHistory;
                break;
            }
            case MessageBody.UpdateAgentVariablesRequest:
                note.VariableUpdateCount = msg.BodyAsUpdateAgentVariablesRequest().UpdatesLength;
                break;
            }
        }

        /// <summary>Deliver up to <paramref name="maxCount"/> queued observations to all subscribers.</summary>
        public int Pump(int maxCount = 64)
        {
            if (maxCount <= 0) return 0;

            ITryllClientObserver[] snapshot;
            lock (_gate)
            {
                if (_subscribers.Count == 0) return 0;
                snapshot = _subscribers.ToArray();
            }

            int delivered = 0;
            while (delivered < maxCount && _queue.TryDequeue(out var envelope))
            {
                for (int i = 0; i < snapshot.Length; ++i)
                {
                    try
                    {
                        snapshot[i].OnObservation(envelope);
                    }
                    catch (Exception ex)
                    {
                        TryllLog.Warning($"TryllClientObserverHub consumer failed: {ex.Message}");
                    }
                }
                delivered++;
            }

            return delivered;
        }

        private sealed class Subscription : IDisposable
        {
            private TryllClientObserverHub _hub;
            private ITryllClientObserver _observer;

            public Subscription(TryllClientObserverHub hub, ITryllClientObserver observer)
            {
                _hub = hub;
                _observer = observer;
            }

            public void Dispose()
            {
                var hub = _hub;
                var observer = _observer;
                _hub = null;
                _observer = null;
                if (hub == null || observer == null) return;
                lock (hub._gate) hub._subscribers.Remove(observer);
            }
        }
    }
}
