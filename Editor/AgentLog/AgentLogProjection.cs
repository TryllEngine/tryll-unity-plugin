// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using Tryll;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor.AgentLog
{
    internal enum AgentLogRowKind
    {
        Sent,
        Received,
        System,
        CaptureGap,
    }

    internal sealed class AgentLogRow
    {
        public ulong EventId;
        public AgentLogRowKind Kind;
        public TryllTurnKey TurnKey;
        public ulong AgentId;
        public DateTime WallTimeUtc;
        public string GutterMarker = "";
        public string SummaryText = "";
        public bool IsFirstRowOfTurn;
        public bool IsTurnTerminal;
        public TryllObservationEnvelope Source;
        public TryllObservationEnvelope CorrelatedAck;
        /// <summary>All deferred UpdateAgentVariables batches folded onto this carrier (journal order).</summary>
        public List<TryllObservationEnvelope> DeferredVariableUpdates;
        public TryllObservationEnvelope DeferredVariablesAck;
        public bool ShowAgentIdentityInGutter;
        /// <summary>True when this is a turn header with at least one body row.</summary>
        public bool CanCollapseTurn;
        public bool IsTurnCollapsed;
        /// <summary>Body rows hidden while collapsed (excludes the header).</summary>
        public int HiddenBodyCount;
    }

    internal sealed class AgentLogFilter
    {
        public bool RawMode;
        public ulong? SingleAgentId;
        public bool AllAgents;

        /// <summary>
        /// Merge consecutive AnswerText observations on the same turn into one row.
        /// Any other observation kind breaks the run (e.g. TTS audio between tokens).
        /// </summary>
        public bool CoalesceAnswerText = true;
    }

    internal static class AgentLogProjection
    {
        public static List<AgentLogRow> Project(
            IEnumerable<TryllObservationEnvelope> journal,
            AgentLogFilter filter)
        {
            filter ??= new AgentLogFilter();
            var rows = new List<AgentLogRow>();
            if (journal == null)
                return rows;

            var ackByRequest = BuildAckIndex(journal);
            var originatingRequests = BuildOriginatingRequestIndex(journal);
            var deferredVars = BuildDeferredVariablesIndex(journal);

            foreach (var env in journal)
            {
                if (!PassesFilter(env, filter))
                    continue;

                if (ShouldSkipForProjection(env, originatingRequests))
                    continue;

                if (filter.CoalesceAnswerText
                    && TryCoalesceAnswerText(rows, env, filter))
                    continue;

                var row = BuildRow(env, filter);
                if (row == null)
                    continue;

                TryJoinAck(row, env, ackByRequest);
                TryAttachDeferredVariables(row, env, deferredVars, ackByRequest);

                rows.Add(row);
            }

            MarkFirstRowsOfTurn(rows);
            return rows;
        }

        /// <summary>
        /// Fold consecutive same-turn AnswerText into the previous row. Returns true when consumed.
        /// </summary>
        static bool TryCoalesceAnswerText(
            List<AgentLogRow> rows,
            TryllObservationEnvelope env,
            AgentLogFilter filter)
        {
            if (env == null || env.IsCaptureGap || !env.IsPush(MessageBody.AnswerText))
                return false;
            if (rows.Count == 0)
                return false;

            var last = rows[rows.Count - 1];
            if (last == null || last.Source == null || last.Kind != AgentLogRowKind.Received)
                return false;
            if (!last.Source.IsPush(MessageBody.AnswerText))
                return false;
            if (!SameAnswerStream(last, env))
                return false;

            last.SummaryText = AccumulateAnswerText(last.SummaryText, env);
            last.Source = env; // keep latest payload (status/final flags) on the row
            last.IsTurnTerminal = IsTurnTerminalEvent(env);
            last.ShowAgentIdentityInGutter = filter.AllAgents;
            return true;
        }

        static bool SameAnswerStream(AgentLogRow last, TryllObservationEnvelope env)
        {
            if (last.AgentId != env.AgentId)
                return false;
            if (last.TurnKey.HasValue && env.TurnKey.HasValue)
                return last.TurnKey.Equals(env.TurnKey);
            return last.Source != null
                && last.Source.ConnectionEpoch == env.ConnectionEpoch
                && last.Source.RequestId == env.RequestId;
        }

        static string AccumulateAnswerText(string existingSummary, TryllObservationEnvelope env)
        {
            var chunk = env.Text ?? "";
            // Flag = isDelta (see TryllAgent.HandleAnswerText).
            if (env.Flag)
                return (existingSummary ?? "") + chunk;
            return string.IsNullOrEmpty(chunk) ? (existingSummary ?? "AnswerText") : chunk;
        }

        static Dictionary<(ulong Epoch, ulong RequestId), TryllObservationEnvelope> BuildAckIndex(
            IEnumerable<TryllObservationEnvelope> journal)
        {
            var map = new Dictionary<(ulong, ulong), TryllObservationEnvelope>();
            foreach (var env in journal)
            {
                if (!IsCorrelatedReply(env))
                    continue;
                map[(env.ConnectionEpoch, env.RequestId)] = env;
            }
            return map;
        }

        static Dictionary<(ulong Epoch, ulong RequestId), List<TryllObservationEnvelope>> BuildDeferredVariablesIndex(
            IEnumerable<TryllObservationEnvelope> journal)
        {
            var map = new Dictionary<(ulong, ulong), List<TryllObservationEnvelope>>();
            foreach (var env in journal)
            {
                if (!env.IsRequest(MessageBody.UpdateAgentVariablesRequest))
                    continue;
                if (env.CarrierRequestId == 0)
                    continue;
                var key = (env.ConnectionEpoch, env.CarrierRequestId);
                if (!map.TryGetValue(key, out var list))
                {
                    list = new List<TryllObservationEnvelope>(2);
                    map[key] = list;
                }
                list.Add(env);
            }
            return map;
        }

        static bool PassesFilter(TryllObservationEnvelope env, AgentLogFilter filter)
        {
            if (env.IsCaptureGap)
                return true;

            if (filter.RawMode)
            {
                return env.Scope == TryllObservationScope.CuratedAgent
                    || env.Scope == TryllObservationScope.RawSession
                    || env.Scope == TryllObservationScope.MetadataOnly;
            }

            return env.Scope == TryllObservationScope.CuratedAgent;
        }

        static bool PassesAgentFilter(TryllObservationEnvelope env, AgentLogFilter filter)
        {
            if (filter.AllAgents)
                return true;

            if (filter.SingleAgentId.HasValue)
                return env.AgentId == filter.SingleAgentId.Value;

            return true;
        }

        static HashSet<(ulong Epoch, ulong RequestId)> BuildOriginatingRequestIndex(
            IEnumerable<TryllObservationEnvelope> journal)
        {
            var set = new HashSet<(ulong, ulong)>();
            foreach (var env in journal)
            {
                if (env.Direction != TryllObservationDirection.Outbound || env.RequestId == 0 || env.IsCaptureGap)
                    continue;
                set.Add((env.ConnectionEpoch, env.RequestId));
            }
            return set;
        }

        static bool ShouldSkipForProjection(
            TryllObservationEnvelope env,
            HashSet<(ulong Epoch, ulong RequestId)> originatingRequests)
        {
            if (env.IsCaptureGap)
                return false;

            if (env.Direction == TryllObservationDirection.Outbound)
            {
                if (env.IsRequest(MessageBody.UpdateAgentVariablesRequest) && env.CarrierRequestId != 0)
                    return true;
                return false;
            }

            if (IsCorrelatedReply(env))
                return originatingRequests.Contains((env.ConnectionEpoch, env.RequestId));

            return false;
        }

        static bool IsVoiceSentFinalTranscript(TryllObservationEnvelope env)
        {
            return env.IsPush(MessageBody.WireTranscriptUpdate)
                && env.TranscriptKind == (byte)TranscriptUpdateKind.UtteranceFinal
                && env.CarrierRequestId != 0;
        }

        static AgentLogRow BuildRow(TryllObservationEnvelope env, AgentLogFilter filter)
        {
            if (!PassesAgentFilter(env, filter))
                return null;

            if (env.IsCaptureGap)
            {
                return new AgentLogRow
                {
                    EventId = env.EventId,
                    Kind = AgentLogRowKind.CaptureGap,
                    WallTimeUtc = env.WallTimeUtc,
                    GutterMarker = "·",
                    SummaryText = "Capture paused — observations may be missing.",
                    Source = env,
                    ShowAgentIdentityInGutter = filter.AllAgents,
                };
            }

            var kind = ClassifyKind(env);
            var row = new AgentLogRow
            {
                EventId = env.EventId,
                Kind = kind,
                TurnKey = env.TurnKey,
                AgentId = env.AgentId,
                WallTimeUtc = env.WallTimeUtc,
                GutterMarker = GutterForEnvelope(env, kind),
                SummaryText = Summarize(env, kind),
                IsTurnTerminal = IsTurnTerminalEvent(env),
                Source = env,
                ShowAgentIdentityInGutter = filter.AllAgents,
            };
            return row;
        }

        static void TryJoinAck(
            AgentLogRow row,
            TryllObservationEnvelope env,
            Dictionary<(ulong Epoch, ulong RequestId), TryllObservationEnvelope> ackByRequest)
        {
            if (env.Direction != TryllObservationDirection.Outbound || env.RequestId == 0 || env.IsCaptureGap)
                return;

            if (!ackByRequest.TryGetValue((env.ConnectionEpoch, env.RequestId), out var ack))
                return;

            row.CorrelatedAck = ack;
        }

        static void TryAttachDeferredVariables(
            AgentLogRow row,
            TryllObservationEnvelope env,
            Dictionary<(ulong Epoch, ulong RequestId), List<TryllObservationEnvelope>> deferredVars,
            Dictionary<(ulong Epoch, ulong RequestId), TryllObservationEnvelope> ackByRequest)
        {
            if (env.Direction != TryllObservationDirection.Outbound || env.RequestId == 0 || env.IsCaptureGap)
                return;

            if (!IsCarrierCommand(env))
                return;

            if (!deferredVars.TryGetValue((env.ConnectionEpoch, env.RequestId), out var updates)
                || updates == null
                || updates.Count == 0)
            {
                return;
            }

            row.DeferredVariableUpdates = updates;
            // Unreal parity: join ack for the first deferred batch only.
            var first = updates[0];
            if (first.RequestId != 0
                && ackByRequest.TryGetValue((first.ConnectionEpoch, first.RequestId), out var deferredAck))
            {
                row.DeferredVariablesAck = deferredAck;
            }
        }

        static void MarkFirstRowsOfTurn(List<AgentLogRow> rows)
        {
            var seen = new HashSet<TryllTurnKey>();
            for (int i = 0; i < rows.Count; ++i)
            {
                var key = rows[i].TurnKey;
                if (!key.HasValue)
                    continue;
                if (seen.Add(key))
                    rows[i].IsFirstRowOfTurn = true;
            }
        }

        static AgentLogRowKind ClassifyKind(TryllObservationEnvelope env)
        {
            if (IsVoiceSentFinalTranscript(env))
                return AgentLogRowKind.Sent;

            if (env.IsRequest(MessageBody.SendMessageRequest)
                || env.IsRequest(MessageBody.ResumeAgentRequest)
                || env.IsRequest(MessageBody.ChangeAgentParamRequest)
                || env.IsRequest(MessageBody.BeginUtteranceRequest))
            {
                return AgentLogRowKind.Sent;
            }

            if (env.IsPush(MessageBody.AnswerText)
                || env.IsPush(MessageBody.TurnComplete)
                || env.IsReplyTo(MessageBody.SendMessageRequest, false)
                || env.IsPush(MessageBody.TtsAudioFormatFrame)
                || env.IsPush(MessageBody.TtsAudioFrame)
                || env.IsPush(MessageBody.TtsVisemeFrame))
            {
                return AgentLogRowKind.Received;
            }

            return AgentLogRowKind.System;
        }

        static bool IsCarrierCommand(TryllObservationEnvelope env)
        {
            if (env.Direction != TryllObservationDirection.Outbound)
                return false;
            if (env.IsRequest(MessageBody.SendMessageRequest))
                return true;
            return TryllMessageDescriptors.IsScopedAck(env.Body);
        }

        static bool IsCorrelatedReply(TryllObservationEnvelope env)
        {
            if (env.Direction != TryllObservationDirection.Inbound || env.RequestId == 0)
                return false;
            if (env.Connection != TryllConnectionObservation.None)
                return false;
            return TryllMessageDescriptors.IsScopedAck(env.RequestBody);
        }

        static bool IsTurnTerminalEvent(TryllObservationEnvelope env)
        {
            return env.IsPush(MessageBody.TurnComplete)
                || env.IsReplyTo(MessageBody.SendMessageRequest, false);
        }

        static string GutterForEnvelope(TryllObservationEnvelope env, AgentLogRowKind kind)
        {
            if (IsVoiceSentFinalTranscript(env))
                return TryllEditorUiContract.IconVoiceInput;
            return GutterForKind(kind);
        }

        static string GutterForKind(AgentLogRowKind kind)
        {
            switch (kind)
            {
            case AgentLogRowKind.Sent: return "→";
            case AgentLogRowKind.Received: return "←";
            default: return "·";
            }
        }

        static string Summarize(TryllObservationEnvelope env, AgentLogRowKind kind)
        {
            if (IsVoiceSentFinalTranscript(env))
                return string.IsNullOrEmpty(env.Text) ? "Voice sent" : env.Text;

            if (env.Direction == TryllObservationDirection.Outbound && !env.IsCaptureGap)
            {
                if (env.IsRequest(MessageBody.SendMessageRequest))
                    return string.IsNullOrEmpty(env.Text) ? "SendMessage" : $"Send: {env.Text}";
                if (env.IsRequest(MessageBody.ResumeAgentRequest))
                    return string.IsNullOrEmpty(env.Text) ? "Resume" : $"Resume: {env.Text}";
                if (env.IsRequest(MessageBody.ChangeAgentParamRequest))
                {
                    var changeLabel = FrameLabel(env);
                    if (!string.IsNullOrEmpty(changeLabel))
                        return changeLabel;
                    return string.IsNullOrEmpty(env.NodeName) ? "ChangeParams" : $"ChangeParams: {env.NodeName}";
                }
                if (env.IsRequest(MessageBody.BeginUtteranceRequest))
                    return "BeginUtterance";
                if (env.IsRequest(MessageBody.CreateAgentRequest))
                    return string.IsNullOrEmpty(env.GraphDefaultModelName)
                        ? "CreateAgent"
                        : $"CreateAgent ({env.GraphDefaultModelName})";
                if (env.IsRequest(MessageBody.DestroyAgentRequest))
                    return "DestroyAgent";
                if (env.IsRequest(MessageBody.CancelRequest))
                    return "Cancel";
                if (env.IsRequest(MessageBody.UpdateAgentVariablesRequest))
                    return string.IsNullOrEmpty(env.PayloadSummary)
                        ? $"UpdateVariables ({env.VariableUpdateCount})"
                        : $"UpdateVariables ({env.PayloadSummary})";
                return FrameLabel(env) ?? TryllObservationLabels.Of(env);
            }

            if (env.IsPush(MessageBody.AnswerText))
                return string.IsNullOrEmpty(env.Text) ? "AnswerText" : env.Text;
            if (env.IsPush(MessageBody.TurnComplete))
                return "TurnComplete";
            if (env.IsReplyTo(MessageBody.SendMessageRequest, false))
                return string.IsNullOrEmpty(env.Message) ? "SendErr" : env.Message;
            if (env.IsPush(MessageBody.TtsAudioFormatFrame))
                return string.IsNullOrEmpty(env.NodeName) ? "TTS format" : $"TTS format ({env.NodeName})";
            if (env.IsPush(MessageBody.TtsAudioFrame))
                return string.IsNullOrEmpty(env.NodeName) ? "TTS audio" : $"TTS audio ({env.NodeName})";
            if (env.IsPush(MessageBody.TtsVisemeFrame))
                return string.IsNullOrEmpty(env.NodeName) ? "TTS visemes" : $"TTS visemes ({env.NodeName})";
            if (env.IsPush(MessageBody.NodeEvent))
                return string.IsNullOrEmpty(env.NodeName) ? "NodeEvent" : $"NodeEvent: {env.NodeName}";
            return FrameLabel(env) ?? TryllObservationLabels.Of(env);
        }

        static string FrameLabel(TryllObservationEnvelope env)
        {
            if (env?.Frame == null || env.Frame.Length == 0 || env.IsLocal)
                return null;
            var label = TryllAgentLogJson.Label(env.Body, env.Frame);
            return string.IsNullOrEmpty(label) ? null : label;
        }
    }
}
