// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tryll;
using Tryll.Client;

namespace Tryll.Client.Internal
{
    internal static class TryllObservationSnapshot
    {
        public static TryllObservationEnvelope FromNote(TryllObservationNote note, ulong eventId, ulong epoch)
        {
            if (note == null)
                note = new TryllObservationNote();

            return new TryllObservationEnvelope
            {
                EventId = eventId,
                ConnectionEpoch = epoch,
                Direction = note.Direction,
                Scope = TryllObservationClassifier.Classify(note),
                WallTimeUtc = DateTime.UtcNow,
                MonotonicTicks = Stopwatch.GetTimestamp(),
                Body = note.Body,
                RequestBody = note.RequestBody,
                IsError = note.IsError,
                Connection = note.Connection,
                RequestId = note.RequestId,
                AgentId = note.AgentId,
                VoiceInputId = note.VoiceInputId,
                TurnKey = TryllTurnKey.None,
                CarrierRequestId = note.CarrierRequestId,
                Text = note.Text,
                Text2 = note.Text2,
                Message = note.Message,
                Flag = note.Flag,
                Flag2 = note.Flag2,
                TurnStatus = note.TurnStatus,
                TokensGenerated = note.TokensGenerated,
                Code = note.Code,
                BinaryByteCount = note.BinaryByteCount,
                BinaryKind = note.BinaryKind,
                EnableDiagnostics = note.EnableDiagnostics,
                MaintainDialogueHistory = note.MaintainDialogueHistory,
                SampleRate = note.SampleRate,
                Channels = note.Channels,
                BitsPerSample = note.BitsPerSample,
                ThrottleLevel = note.ThrottleLevel,
                RemoveInteractionCount = note.RemoveInteractionCount,
                VariableUpdateCount = note.VariableUpdateCount,
                DialogInteractionCount = note.DialogInteractionCount,
                NodeName = note.NodeName,
                TranscriptKind = note.TranscriptKind,
                AudioMsConsumed = note.AudioMsConsumed,
                GraphDefaultModelName = note.GraphDefaultModelName,
                PayloadSummary = note.PayloadSummary,
            };
        }

        /// <summary>
        /// Pushes and connection-state records that still travel the event queue.
        /// Request-reply completions are noted by the wrapper, not here.
        /// </summary>
        public static TryllObservationEnvelope FromQueuedEvent(TryllConnEvent evt, ulong eventId, ulong epoch)
        {
            if (evt == null)
                return FromNote(null, eventId, epoch);

            TryllObservationNote note;
            switch (evt.Type)
            {
            case TryllConnEventType.Connected:
                note = TryllObservationNote.ConnectionState(TryllConnectionObservation.Connected);
                note.Message = evt.ServerExePath;
                note.Text = evt.ServerVersion;
                note.Text2 = evt.ServerRunId;
                break;

            case TryllConnEventType.ConnectFailed:
                note = TryllObservationNote.ConnectionState(TryllConnectionObservation.ConnectFailed);
                note.Message = evt.Message;
                break;

            case TryllConnEventType.Disconnected:
                note = TryllObservationNote.ConnectionState(TryllConnectionObservation.Disconnected);
                note.Message = evt.Message;
                break;

            case TryllConnEventType.TtsAudioFormat:
                note = TryllObservationNote.Push(MessageBody.TtsAudioFormatFrame);
                note.RequestId = evt.RequestId;
                note.AgentId = evt.AgentId;
                note.NodeName = evt.Message;
                note.SampleRate = evt.Code;
                note.Channels = evt.TtsChannels;
                note.BitsPerSample = evt.TtsBitsPerSample;
                break;

            case TryllConnEventType.TtsAudio:
                note = TryllObservationNote.Push(MessageBody.TtsAudioFrame);
                note.RequestId = evt.RequestId;
                note.AgentId = evt.AgentId;
                note.NodeName = evt.Message;
                note.BinaryByteCount = evt.PcmLength > 0 ? (uint)(evt.PcmLength * sizeof(short)) : 0;
                note.BinaryKind = "tts_pcm";
                break;

            case TryllConnEventType.TtsViseme:
                note = TryllObservationNote.Push(MessageBody.TtsVisemeFrame);
                note.RequestId = evt.RequestId;
                note.AgentId = evt.AgentId;
                note.NodeName = evt.Message;
                note.BinaryByteCount = evt.VisemeEvents != null ? (uint)evt.VisemeEvents.Length : 0;
                note.BinaryKind = "tts_visemes";
                break;

            case TryllConnEventType.NodeEvent:
                note = TryllObservationNote.Push(MessageBody.NodeEvent);
                note.AgentId = evt.AgentId;
                note.NodeName = evt.Text;
                note.Message = evt.Message;
                break;

            case TryllConnEventType.TranscriptUpdate:
                note = TryllObservationNote.Push(MessageBody.WireTranscriptUpdate);
                note.VoiceInputId = evt.VoiceInputId;
                note.Text = evt.Text;
                note.TranscriptKind = evt.TranscriptKind;
                note.AudioMsConsumed = evt.AudioMsConsumed;
                break;

            default:
                note = new TryllObservationNote
                {
                    Direction = TryllObservationDirection.Inbound,
                    Message = evt.Message,
                    Text = evt.Text,
                    AgentId = evt.AgentId,
                    RequestId = evt.RequestId,
                    VoiceInputId = evt.VoiceInputId,
                };
                break;
            }

            return FromNote(note, eventId, epoch);
        }

        internal static string SummarizeVariableUpdates(List<VariableUpdateT> updates)
        {
            if (updates == null || updates.Count == 0)
                return "";

            var parts = new List<string>(updates.Count);
            for (int i = 0; i < updates.Count; ++i)
            {
                var update = updates[i];
                if (update == null || string.IsNullOrEmpty(update.Name))
                    continue;

                var part = $"{update.Name}:{update.Op}";
                if (update.Op != VariableOp.Reset && update.Value != null)
                {
                    var value = FormatVariableValue(update.Value);
                    if (!string.IsNullOrEmpty(value))
                        part += $":{value}";
                }
                parts.Add(part);
            }
            return string.Join(", ", parts);
        }

        static string FormatVariableValue(VariableValueUnion value)
        {
            if (value == null) return "";
            if (value.AsVarInt() != null) return value.AsVarInt().V.ToString();
            if (value.AsVarFloat() != null) return value.AsVarFloat().V.ToString();
            if (value.AsVarString() != null) return value.AsVarString().V;
            if (value.AsVarBool() != null) return value.AsVarBool().V ? "true" : "false";
            if (value.AsVarStringSet() != null)
            {
                var set = value.AsVarStringSet().V;
                return set != null ? $"[{string.Join("|", set)}]" : "[]";
            }
            return "";
        }
    }
}
