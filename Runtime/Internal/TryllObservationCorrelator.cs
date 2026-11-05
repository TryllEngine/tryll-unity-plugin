// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using Tryll;
using Tryll.Client;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Pure correlation layer: assigns <see cref="TryllTurnKey"/>
    /// on fresh observation envelopes before they are journaled (§7.5).
    /// </summary>
    internal sealed class TryllObservationCorrelator
    {
        private readonly struct AgentEpochKey
        {
            public readonly ulong Epoch;
            public readonly ulong AgentId;

            public AgentEpochKey(ulong epoch, ulong agentId)
            {
                Epoch = epoch;
                AgentId = agentId;
            }
        }

        // Per (epoch, agentId) → next synthetic sequence (assigned on turn close).
        private readonly Dictionary<AgentEpochKey, ulong> _nextSyntheticSeq = new Dictionary<AgentEpochKey, ulong>();

        // Per (epoch, agentId) → open synthetic sequence while a request-zero turn is in flight.
        private readonly Dictionary<AgentEpochKey, ulong> _openSynthetic = new Dictionary<AgentEpochKey, ulong>();

        // Per (epoch, agentId) → open logical conversation turn (Send / continued by Resume).
        private readonly Dictionary<AgentEpochKey, TryllTurnKey> _openLogicalTurn =
            new Dictionary<AgentEpochKey, TryllTurnKey>();

        // requestId → turn key (Resume request ids map onto the open Send turn).
        private readonly Dictionary<(ulong Epoch, ulong RequestId), TryllTurnKey> _requestTurnKeys =
            new Dictionary<(ulong, ulong), TryllTurnKey>();

        // requestId → the outbound body, so Ack and ErrorResponse join that request.
        private readonly Dictionary<(ulong Epoch, ulong RequestId), (MessageBody Body, ulong AgentId)> _requestStamps =
            new Dictionary<(ulong, ulong), (MessageBody, ulong)>();

        /// <summary>Drop per-epoch mutable state when a new connection epoch begins.</summary>
        public void ResetForEpoch(ulong epoch)
        {
            _nextSyntheticSeq.Clear();
            _openSynthetic.Clear();
            _openLogicalTurn.Clear();
            _requestTurnKeys.Clear();
            _requestStamps.Clear();
        }

        /// <summary>
        /// Annotate a fresh envelope in-place before it is journaled. Does not mutate
        /// previously delivered envelopes.
        /// </summary>
        public void Correlate(TryllObservationEnvelope env)
        {
            if (env == null) return;

            var epoch = env.ConnectionEpoch;

            if (env.IsCaptureGap || env.IsSyntheticMarker)
                return;
            StampRequest(env);
            BindReply(env, epoch);
            if (env.Direction == TryllObservationDirection.Outbound)
                CorrelateCmd(env, epoch);
            else
                CorrelateEvent(env, epoch);
        }

        private void StampRequest(TryllObservationEnvelope env)
        {
            if (env.Direction != TryllObservationDirection.Outbound || env.RequestId == 0 || env.IsLocal)
                return;
            if (env.Body == MessageBody.NONE || env.Body != env.RequestBody)
                return;
            _requestStamps[(env.ConnectionEpoch, env.RequestId)] = (env.Body, env.AgentId);
        }

        private void BindReply(TryllObservationEnvelope env, ulong epoch)
        {
            if (env.Direction != TryllObservationDirection.Inbound || env.RequestId == 0)
                return;
            if (env.Connection != TryllConnectionObservation.None)
                return;
            var generic = env.Body == MessageBody.Ack
                || env.Body == MessageBody.ErrorResponse
                || env.IsLocal;
            if (!generic)
                return;
            if (!_requestStamps.TryGetValue((epoch, env.RequestId), out var stamp))
                return;
            if (env.RequestBody == MessageBody.NONE || env.RequestBody == env.Body)
                env.RequestBody = stamp.Body;
            if (env.AgentId == 0)
                env.AgentId = stamp.AgentId;
            if (env.Body == MessageBody.ErrorResponse || env.IsLocal)
                env.IsError = true;
        }

        private void CorrelateCmd(TryllObservationEnvelope env, ulong epoch)
        {
            if (env.IsRequest(MessageBody.SendMessageRequest))
            {
                OpenLogicalTurn(env, epoch);
                return;
            }

            if (env.IsRequest(MessageBody.ResumeAgentRequest))
            {
                // Tool-call Resume continues the in-flight Send turn so NodeEvent → Resume →
                // AnswerText → TurnComplete stay one narrative group (and one collapse unit).
                ContinueOrOpenLogicalTurn(env, epoch);
                return;
            }

            if (env.Direction == TryllObservationDirection.Outbound
                && env.Connection == TryllConnectionObservation.None
                && env.Body == env.RequestBody
                && TryllMessageDescriptors.IsStandaloneTurnRequest(env.Body))
            {
                RecordRequestTurn(env, epoch);
            }
        }

        private void CorrelateEvent(TryllObservationEnvelope env, ulong epoch)
        {
            if (TryllMessageDescriptors.IsScopedAck(env.RequestBody)
                && env.Direction == TryllObservationDirection.Inbound)
            {
                ApplyStoredOrRequestTurnKey(env, epoch);
                return;
            }

            if (IsRequestZeroTurnEvent(env) && env.AgentId != 0)
            {
                AssignTurnEventKey(env, epoch);
                if (IsSyntheticTerminal(env))
                    CloseOpenTurns(epoch, env.AgentId, env.TurnKey);
            }
        }

        private void OpenLogicalTurn(TryllObservationEnvelope env, ulong epoch)
        {
            if (env.RequestId == 0 || env.AgentId == 0) return;
            var key = TryllTurnKey.FromRequest(epoch, env.AgentId, env.RequestId);
            env.TurnKey = key;
            _requestTurnKeys[(epoch, env.RequestId)] = key;
            _openLogicalTurn[new AgentEpochKey(epoch, env.AgentId)] = key;
        }

        private void ContinueOrOpenLogicalTurn(TryllObservationEnvelope env, ulong epoch)
        {
            if (env.RequestId == 0 || env.AgentId == 0) return;
            var agentKey = new AgentEpochKey(epoch, env.AgentId);
            if (_openLogicalTurn.TryGetValue(agentKey, out var open) && open.HasValue)
            {
                env.TurnKey = open;
                _requestTurnKeys[(epoch, env.RequestId)] = open;
                return;
            }
            OpenLogicalTurn(env, epoch);
        }

        private void RecordRequestTurn(TryllObservationEnvelope env, ulong epoch)
        {
            if (env.RequestId == 0 || env.AgentId == 0) return;
            var key = TryllTurnKey.FromRequest(epoch, env.AgentId, env.RequestId);
            env.TurnKey = key;
            _requestTurnKeys[(epoch, env.RequestId)] = key;
        }

        private void ApplyStoredOrRequestTurnKey(TryllObservationEnvelope env, ulong epoch)
        {
            if (env.RequestId == 0 || env.AgentId == 0) return;
            if (_requestTurnKeys.TryGetValue((epoch, env.RequestId), out var mapped))
            {
                env.TurnKey = mapped;
                return;
            }
            env.TurnKey = TryllTurnKey.FromRequest(epoch, env.AgentId, env.RequestId);
        }

        private void AssignTurnEventKey(TryllObservationEnvelope env, ulong epoch)
        {
            if (env.RequestId != 0)
            {
                ApplyStoredOrRequestTurnKey(env, epoch);
                return;
            }

            // Request-zero streaming after Send/Resume joins the open logical turn.
            var agentKey = new AgentEpochKey(epoch, env.AgentId);
            if (_openLogicalTurn.TryGetValue(agentKey, out var open) && open.HasValue)
            {
                env.TurnKey = open;
                return;
            }

            env.TurnKey = GetOrOpenSynthetic(epoch, env.AgentId);
        }

        private void CloseOpenTurns(ulong epoch, ulong agentId, TryllTurnKey closedKey)
        {
            var agentKey = new AgentEpochKey(epoch, agentId);
            if (_openLogicalTurn.TryGetValue(agentKey, out var open)
                && open.HasValue
                && open.Equals(closedKey))
            {
                _openLogicalTurn.Remove(agentKey);
            }
            CloseSynthetic(epoch, agentId);
        }

        private static bool IsRequestZeroTurnEvent(TryllObservationEnvelope env)
        {
            return env.IsPush(MessageBody.AnswerText)
                || env.IsPush(MessageBody.TurnComplete)
                || env.IsReplyTo(MessageBody.SendMessageRequest, false)
                || env.IsPush(MessageBody.TtsAudioFormatFrame)
                || env.IsPush(MessageBody.TtsAudioFrame)
                || env.IsPush(MessageBody.TtsVisemeFrame)
                || env.IsPush(MessageBody.NodeEvent);
        }

        private static bool IsSyntheticTerminal(TryllObservationEnvelope env)
        {
            return env.IsPush(MessageBody.TurnComplete)
                || env.IsReplyTo(MessageBody.SendMessageRequest, false);
        }

        private TryllTurnKey ReserveSyntheticTurn(ulong epoch, ulong agentId)
        {
            var key = new AgentEpochKey(epoch, agentId);
            var seq = _nextSyntheticSeq.TryGetValue(key, out var next) ? next : 1;
            _nextSyntheticSeq[key] = seq + 1;
            _openSynthetic[key] = seq;
            return TryllTurnKey.FromSynthetic(epoch, agentId, seq);
        }

        private TryllTurnKey GetOrOpenSynthetic(ulong epoch, ulong agentId)
        {
            var key = new AgentEpochKey(epoch, agentId);
            if (!_openSynthetic.TryGetValue(key, out var seq))
            {
                seq = _nextSyntheticSeq.TryGetValue(key, out var next) ? next : 1;
                _openSynthetic[key] = seq;
            }
            return TryllTurnKey.FromSynthetic(epoch, agentId, seq);
        }

        private void CloseSynthetic(ulong epoch, ulong agentId)
        {
            var key = new AgentEpochKey(epoch, agentId);
            if (!_openSynthetic.TryGetValue(key, out var seq)) return;
            _openSynthetic.Remove(key);
            _nextSyntheticSeq[key] = seq + 1;
        }
    }
}
