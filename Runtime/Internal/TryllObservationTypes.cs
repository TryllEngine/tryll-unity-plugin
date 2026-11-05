// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using Tryll;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// A connection-state journal record. Wire frames use <see cref="TryllConnectionObservation.None"/>.
    /// </summary>
    internal enum TryllConnectionObservation : byte
    {
        None = 0,
        Connected,
        ConnectFailed,
        Disconnected,
    }

    internal enum TryllObservationDirection : byte
    {
        Outbound,
        Inbound,
    }

    internal enum TryllObservationScope : byte
    {
        CuratedAgent,
        RawSession,
        MetadataOnly,
    }

    internal readonly struct TryllTurnKey : IEquatable<TryllTurnKey>
    {
        public readonly ulong ConnectionEpoch;
        public readonly ulong AgentId;
        public readonly ulong RequestId; // 0 means synthetic
        public readonly ulong SyntheticSequence; // used when RequestId == 0
        public readonly bool HasValue;

        private TryllTurnKey(ulong connectionEpoch, ulong agentId, ulong requestId,
            ulong syntheticSequence, bool hasValue)
        {
            ConnectionEpoch = connectionEpoch;
            AgentId = agentId;
            RequestId = requestId;
            SyntheticSequence = syntheticSequence;
            HasValue = hasValue;
        }

        public static TryllTurnKey FromRequest(ulong connectionEpoch, ulong agentId, ulong requestId)
            => new TryllTurnKey(connectionEpoch, agentId, requestId, 0, true);

        public static TryllTurnKey FromSynthetic(ulong connectionEpoch, ulong agentId, ulong syntheticSequence)
            => new TryllTurnKey(connectionEpoch, agentId, 0, syntheticSequence, true);

        public static TryllTurnKey None => default;

        public bool Equals(TryllTurnKey other)
            => HasValue == other.HasValue
               && ConnectionEpoch == other.ConnectionEpoch
               && AgentId == other.AgentId
               && RequestId == other.RequestId
               && SyntheticSequence == other.SyntheticSequence;

        public override bool Equals(object obj) => obj is TryllTurnKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HasValue.GetHashCode();
                hash = (hash * 397) ^ ConnectionEpoch.GetHashCode();
                hash = (hash * 397) ^ AgentId.GetHashCode();
                hash = (hash * 397) ^ RequestId.GetHashCode();
                hash = (hash * 397) ^ SyntheticSequence.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(TryllTurnKey left, TryllTurnKey right) => left.Equals(right);
        public static bool operator !=(TryllTurnKey left, TryllTurnKey right) => !left.Equals(right);
    }

    /// <summary>
    /// Immutable observation envelope. Payload fields are selectively populated;
    /// binary buffers are never copied.
    /// </summary>
    internal sealed class TryllObservationEnvelope
    {
        public ulong EventId;
        public ulong ConnectionEpoch;
        public TryllObservationDirection Direction;
        public TryllObservationScope Scope;
        public DateTime WallTimeUtc;
        public long MonotonicTicks; // Stopwatch.GetTimestamp()
        public MessageBody Body;
        public MessageBody RequestBody;
        public bool IsError;
        public TryllConnectionObservation Connection;
        public ulong RequestId;
        public ulong AgentId;
        public ulong VoiceInputId;
        public TryllTurnKey TurnKey; // optional; HasValue = false if unset
        public ulong CarrierRequestId; // observation-only; 0 if none

        /// <summary>Stripped wire bytes, or a local LogLocalEnvelope. Null for legacy slot notes.</summary>
        public byte[] Frame;
        public bool IsLocal;
        public System.Collections.Generic.List<TryllOmittedField> Omitted;

        // Semantic payload (selective):
        public string Text;
        public string Text2;
        public string Message;
        public bool Flag;
        public bool Flag2;
        public byte TurnStatus;
        public uint TokensGenerated;
        public uint Code;
        public uint BinaryByteCount; // for PCM etc — never store the bytes
        public string BinaryKind; // e.g. "pcm", "tts_pcm"
        public bool EnableDiagnostics;
        public bool MaintainDialogueHistory;
        public uint SampleRate;
        public ushort Channels;
        public ushort BitsPerSample;
        public float ThrottleLevel;
        public uint RemoveInteractionCount;
        public int VariableUpdateCount;
        public int DialogInteractionCount;
        public string NodeName; // Message field for node events / change params
        public byte TranscriptKind;
        public uint AudioMsConsumed;
        public string GraphDefaultModelName; // lightweight create summary only
        /// <summary>Compact serializable payload (e.g. deferred variable batch summary).</summary>
        public string PayloadSummary;

        /// <summary>Editor-only synthetic journal markers (capture gaps, etc.).</summary>
        public bool IsSyntheticMarker;
        public bool IsCaptureGap;

        /// <summary>Outbound frame whose wire type is <paramref name="request"/>.</summary>
        public bool IsRequest(MessageBody request)
            => Direction == TryllObservationDirection.Outbound
               && Connection == TryllConnectionObservation.None
               && Body == request
               && RequestBody == request;

        /// <summary>
        /// Inbound reply to <paramref name="request"/>. <paramref name="ok"/> is false when
        /// the reply was an <c>ErrorResponse</c> or a local failure.
        /// </summary>
        public bool IsReplyTo(MessageBody request, bool ok)
            => Direction == TryllObservationDirection.Inbound
               && Connection == TryllConnectionObservation.None
               && RequestBody == request
               && IsError == !ok;

        /// <summary>Inbound push whose wire type is <paramref name="body"/>.</summary>
        public bool IsPush(MessageBody body)
            => Direction == TryllObservationDirection.Inbound
               && Connection == TryllConnectionObservation.None
               && Body == body;

        /// <summary>
        /// Shallow field copy for journal ownership. Callers may mutate their envelope after append;
        /// the store keeps an independent snapshot (strings are immutable references).
        /// </summary>
        public TryllObservationEnvelope Clone()
        {
            return (TryllObservationEnvelope)MemberwiseClone();
        }
    }

    /// <summary>
    /// Semantic extras a wrapper attaches when it records one outbound frame or one
    /// inbound completion/push. History stays in memory; binary buffers are counts only.
    /// </summary>
    internal sealed class TryllObservationNote
    {
        public TryllObservationDirection Direction;
        public MessageBody Body;
        public MessageBody RequestBody;
        public bool IsError;
        public TryllConnectionObservation Connection;

        public ulong RequestId;
        public ulong AgentId;
        public ulong VoiceInputId;
        public ulong CarrierRequestId;

        public string Text;
        public string Text2;
        public string Message;
        public bool Flag;
        public bool Flag2;
        public byte TurnStatus;
        public uint TokensGenerated;
        public uint Code;
        public uint BinaryByteCount;
        public string BinaryKind;
        public bool EnableDiagnostics;
        public bool MaintainDialogueHistory;
        public uint SampleRate;
        public ushort Channels;
        public ushort BitsPerSample;
        public float ThrottleLevel;
        public uint RemoveInteractionCount;
        public int VariableUpdateCount;
        public int DialogInteractionCount;
        public string NodeName;
        public byte TranscriptKind;
        public uint AudioMsConsumed;
        public string GraphDefaultModelName;
        public string PayloadSummary;

        public static TryllObservationNote Request(MessageBody body, ulong requestId = 0)
        {
            return new TryllObservationNote
            {
                Direction = TryllObservationDirection.Outbound,
                Body = body,
                RequestBody = body,
                RequestId = requestId,
            };
        }

        public static TryllObservationNote Reply(MessageBody request, bool ok, ulong requestId = 0)
        {
            var body = MessageBody.ErrorResponse;
            if (ok)
            {
                body = MessageBody.Ack;
                if (TryllMessageDescriptors.TryGet(request, out var descriptor)
                    && descriptor.Terminal != MessageBody.NONE)
                {
                    body = descriptor.Terminal;
                }
            }

            return new TryllObservationNote
            {
                Direction = TryllObservationDirection.Inbound,
                Body = body,
                RequestBody = request,
                IsError = !ok,
                RequestId = requestId,
            };
        }

        public static TryllObservationNote Push(MessageBody body, MessageBody requestBody = MessageBody.NONE)
        {
            return new TryllObservationNote
            {
                Direction = TryllObservationDirection.Inbound,
                Body = body,
                RequestBody = requestBody,
            };
        }

        public TryllObservationNote Fill(Action<TryllObservationNote> fill)
        {
            fill?.Invoke(this);
            return this;
        }

        public static TryllObservationNote ConnectionState(TryllConnectionObservation state)
        {
            return new TryllObservationNote
            {
                Direction = TryllObservationDirection.Inbound,
                Connection = state,
                Body = MessageBody.NONE,
                RequestBody = MessageBody.NONE,
            };
        }
    }

    internal static class TryllObservationLabels
    {
        public static string Name(MessageBody body)
        {
            if (body == MessageBody.NONE)
                return "NONE";
            if (TryllMessageDescriptors.TryGet(body, out var descriptor)
                && !string.IsNullOrEmpty(descriptor.Name))
            {
                return descriptor.Name;
            }
            return body.ToString();
        }

        public static string Of(TryllObservationEnvelope env)
        {
            if (env == null)
                return "";
            if (env.Connection != TryllConnectionObservation.None)
                return env.Connection.ToString();
            return Name(env.Body != MessageBody.NONE ? env.Body : env.RequestBody);
        }
    }
}
