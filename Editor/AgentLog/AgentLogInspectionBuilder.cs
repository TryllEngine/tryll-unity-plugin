// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Text;
using Tryll;
using Tryll.Client.Editor;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Builds polymorphic inspector models from projected rows and the observation journal.
    /// </summary>
    internal static class AgentLogInspectionBuilder
    {
        public static TryllSentInspection BuildSent(AgentLogRow row)
        {
            if (row?.Source == null)
            {
                return new TryllSentInspection
                {
                    Title = "Sent",
                    Hint = "no row selected",
                    ExtraNotes = "No additional detail",
                };
            }

            var env = row.Source;
            var cmd = TryllObservationLabels.Of(env);
            return new TryllSentInspection
            {
                Title = $"Sent · {cmd}",
                Hint = FormatHint(env.RequestId, env.AgentId, env.WallTimeUtc),
                Form = ClassifyForm(env),
                RequestId = env.RequestId,
                AgentId = env.AgentId,
                WallTimeUtc = env.WallTimeUtc,
                ExactPayloadText = BuildExactPayload(env),
                DeferredVariablesSummary = SummarizeDeferred(row.DeferredVariableUpdates),
                ExtraNotes = BuildSentExtraNotes(row),
            };
        }

        public static TryllSystemInspection BuildSystem(AgentLogRow row)
        {
            if (row == null)
            {
                return new TryllSystemInspection
                {
                    Title = "System",
                    Hint = "no row selected",
                    ExtraNotes = "No additional detail",
                };
            }

            if (row.Kind == AgentLogRowKind.CaptureGap)
            {
                return new TryllSystemInspection
                {
                    Title = "Capture gap",
                    Hint = FormatTime(row.WallTimeUtc),
                    Target = "session",
                    BeforeText = "—",
                    AfterText = "—",
                    AckOrError = "—",
                    ExtraNotes = string.IsNullOrEmpty(row.SummaryText)
                        ? "Observations may be missing while capture was detached."
                        : row.SummaryText,
                };
            }

            var env = row.Source;
            var title = $"System · {TryllObservationLabels.Of(env)}";

            return new TryllSystemInspection
            {
                Title = title,
                Hint = FormatHint(env.RequestId, env.AgentId, env.WallTimeUtc),
                Target = BuildSystemTarget(env),
                BeforeText = BuildSystemBefore(env),
                AfterText = BuildSystemAfter(env),
                AckOrError = FormatAck(row.CorrelatedAck),
                ExtraNotes = BuildSystemExtraNotes(row),
            };
        }

        public static TryllTurnInspection BuildTurn(
            AgentLogRow row,
            IEnumerable<TryllObservationEnvelope> journal,
            AgentDescriptorRegistry descriptors)
        {
            if (row == null)
                return PlaceholderTurn("select a row");

            if (row.Kind != AgentLogRowKind.Received || !row.TurnKey.HasValue)
            {
                return new TryllTurnInspection
                {
                    TurnKey = row.EventId.ToString(),
                    Hint = row.SummaryText ?? "",
                    AnswerText = row.SummaryText ?? "",
                    AbsenceReason = TryllDiagnosticsAbsenceReason.Unavailable,
                };
            }

            GatherTurnSiblings(journal, row.TurnKey, out var answerText, out var status, out var errorText,
                out var rawDebug, out var tokens, out var durationMs, out var ttftMs);

            string turnKeyStr = FormatTurnKey(row.TurnKey);
            string hint = row.AgentId != 0
                ? $"agent {row.AgentId} · req {row.TurnKey.RequestId}"
                : turnKeyStr;

            var inspection = TryllTurnInspectionFactory.FromChatTurn(
                turnKeyStr,
                hint,
                answerText,
                MapStatus(status),
                errorText,
                TryllChatTurnLogic.ExtractRoute(ParseDebugJson(rawDebug)),
                durationMs,
                ttftMs,
                ParseDebugJson(rawDebug),
                rawDebug ?? "");

            if (descriptors != null
                && descriptors.TryGet(row.AgentId, out var descriptor)
                && descriptor.CreatedBeforeDiagnosticsCapture
                && (inspection.AbsenceReason == TryllDiagnosticsAbsenceReason.Empty
                    || inspection.AbsenceReason == TryllDiagnosticsAbsenceReason.Unavailable))
            {
                inspection.AbsenceReason = TryllDiagnosticsAbsenceReason.CreatedBeforeDiagnosticsCapture;
            }

            return inspection;
        }

        static TryllTurnInspection PlaceholderTurn(string hint) =>
            TryllTurnInspectionFactory.Empty(hint);

        static void GatherTurnSiblings(
            IEnumerable<TryllObservationEnvelope> journal,
            TryllTurnKey turnKey,
            out string answerText,
            out TryllTurnStatus status,
            out string errorText,
            out string rawDebug,
            out int tokens,
            out int durationMs,
            out int ttftMs)
        {
            var answer = new StringBuilder();
            TryllObservationEnvelope turnComplete = null;
            TryllObservationEnvelope sendErr = null;

            if (journal != null)
            {
                foreach (var env in journal)
                {
                    if (!env.TurnKey.Equals(turnKey))
                        continue;

                    if (env.IsPush(MessageBody.AnswerText))
                    {
                        // Flag is is_delta. A full replacement must not keep the previous tokens.
                        if (env.Flag)
                        {
                            if (!string.IsNullOrEmpty(env.Text))
                                answer.Append(env.Text);
                        }
                        else
                        {
                            answer.Clear();
                            answer.Append(env.Text ?? "");
                        }
                    }
                    else if (env.IsPush(MessageBody.TurnComplete))
                    {
                        turnComplete = env;
                    }
                    else if (env.IsReplyTo(MessageBody.SendMessageRequest, false))
                    {
                        sendErr = env;
                    }
                }
            }

            answerText = answer.ToString();
            status = TryllTurnStatus.Success;
            errorText = "";
            rawDebug = "";
            tokens = 0;
            durationMs = 0;
            ttftMs = 0;

            if (sendErr != null)
            {
                status = TryllTurnStatus.Error;
                errorText = sendErr.Message ?? sendErr.Text ?? "SendErr";
            }
            else if (turnComplete != null)
            {
                status = (TryllTurnStatus)turnComplete.TurnStatus;
                rawDebug = turnComplete.Text ?? turnComplete.Message ?? "";
                tokens = (int)turnComplete.TokensGenerated;

                var debugJson = ParseDebugJson(rawDebug);
                if (debugJson != null && !debugJson.IsNull)
                {
                    var durS = debugJson.Get("duration_s");
                    if (!durS.IsNull) durationMs = (int)(durS.AsDouble() * 1000);
                    var ttft = debugJson.Get("time_to_first_token_ms");
                    if (!ttft.IsNull) ttftMs = (int)ttft.AsDouble();
                }
            }
        }

        static TryllJsonValue ParseDebugJson(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return TryllJsonValue.Null;
            return TryllJson.TryParse(raw, out var parsed, out _) ? parsed : TryllJsonValue.Null;
        }

        static TryllTurnTerminalStatus MapStatus(TryllTurnStatus status) => status switch
        {
            TryllTurnStatus.Cancelled => TryllTurnTerminalStatus.Cancelled,
            TryllTurnStatus.Error => TryllTurnTerminalStatus.Error,
            _ => TryllTurnTerminalStatus.Success,
        };

        static string FormatTurnKey(TryllTurnKey key)
        {
            if (!key.HasValue) return "";
            if (key.RequestId != 0)
                return $"turn:{key.ConnectionEpoch}:{key.AgentId}:{key.RequestId}";
            return $"turn:{key.ConnectionEpoch}:{key.AgentId}:s{key.SyntheticSequence}";
        }

        static string ClassifyForm(TryllObservationEnvelope env)
        {
            if (env.Direction != TryllObservationDirection.Outbound) return "inbound";
            if (env.IsRequest(MessageBody.SendMessageRequest)) return "typed";
            if (env.IsRequest(MessageBody.BeginUtteranceRequest)) return "voice";
            if (env.IsRequest(MessageBody.ResumeAgentRequest)) return "resume";
            if (env.IsRequest(MessageBody.ChangeAgentParamRequest)) return "params";
            if (env.IsRequest(MessageBody.AppendInteractionsRequest)) return "injected";
            return TryllObservationLabels.Of(env);
        }

        static string BuildExactPayload(TryllObservationEnvelope env)
        {
            if (env.Direction != TryllObservationDirection.Outbound)
                return env.Text ?? env.Message ?? TryllObservationLabels.Of(env);

            if (env.IsRequest(MessageBody.SendMessageRequest) || env.IsRequest(MessageBody.ResumeAgentRequest))
                return env.Text ?? "";
            if (env.IsRequest(MessageBody.ChangeAgentParamRequest))
                return string.IsNullOrEmpty(env.NodeName)
                    ? env.Text ?? ""
                    : $"{env.NodeName}: {env.Text ?? ""}";
            if (env.IsRequest(MessageBody.BeginUtteranceRequest))
                return $"auto_finish={env.Flag}, max_ms={env.Code}";
            if (env.IsRequest(MessageBody.CreateAgentRequest))
                return string.IsNullOrEmpty(env.GraphDefaultModelName)
                    ? $"diagnostics={env.EnableDiagnostics}"
                    : $"{env.GraphDefaultModelName} (diagnostics={env.EnableDiagnostics})";
            if (env.IsRequest(MessageBody.UpdateAgentVariablesRequest))
                return $"{env.VariableUpdateCount} variable update(s)";
            if (env.IsRequest(MessageBody.AppendInteractionsRequest))
                return $"{env.DialogInteractionCount} interaction(s)";
            if (env.IsRequest(MessageBody.CancelRequest))
                return $"mode={env.TurnStatus}";
            return env.Text ?? env.Message ?? env.NodeName ?? TryllObservationLabels.Of(env);
        }

        static string SummarizeDeferred(IReadOnlyList<TryllObservationEnvelope> deferred)
        {
            if (deferred == null || deferred.Count == 0) return "None";

            if (deferred.Count == 1)
                return SummarizeDeferredBatch(deferred[0]);

            var sb = new StringBuilder();
            for (int i = 0; i < deferred.Count; ++i)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(SummarizeDeferredBatch(deferred[i]));
            }
            return sb.ToString();
        }

        static string SummarizeDeferredBatch(TryllObservationEnvelope deferred)
        {
            var summary = string.IsNullOrEmpty(deferred.PayloadSummary)
                ? $"{deferred.VariableUpdateCount} update(s)"
                : deferred.PayloadSummary;
            return $"UpdateAgentVariables — {summary} (carrier req {deferred.CarrierRequestId})";
        }

        static string BuildSentExtraNotes(AgentLogRow row)
        {
            var notes = new List<string>(2);
            if (row.DeferredVariablesAck != null)
                notes.Add($"Deferred vars: {FormatAck(row.DeferredVariablesAck)}");
            if (row.CorrelatedAck != null)
                notes.Add(FormatAck(row.CorrelatedAck));
            if (notes.Count > 0)
                return string.Join(" · ", notes);
            return "No additional detail";
        }

        static string BuildSystemTarget(TryllObservationEnvelope env)
        {
            if (!string.IsNullOrEmpty(env.NodeName)) return env.NodeName;
            if (env.AgentId != 0) return $"agent {env.AgentId}";
            if (env.IsRequest(MessageBody.UpdateAgentVariablesRequest))
                return "variables";
            return TryllObservationLabels.Of(env);
        }

        static string BuildSystemBefore(TryllObservationEnvelope env)
        {
            if (env.IsRequest(MessageBody.ChangeAgentParamRequest))
                return env.NodeName ?? "—";
            return "—";
        }

        static string BuildSystemAfter(TryllObservationEnvelope env)
        {
            if (env.Direction == TryllObservationDirection.Outbound)
            {
                if (env.IsRequest(MessageBody.ChangeAgentParamRequest))
                    return env.Text ?? "—";
                if (env.IsRequest(MessageBody.UpdateAgentVariablesRequest))
                    return $"{env.VariableUpdateCount} update(s)";
                if (env.IsRequest(MessageBody.CreateAgentRequest))
                    return env.GraphDefaultModelName ?? "—";
                return env.Text ?? env.Message ?? "—";
            }

            if (env.IsPush(MessageBody.NodeEvent))
                return env.Message ?? env.Text ?? "—";

            return env.Text ?? env.Message ?? "—";
        }

        static string BuildSystemExtraNotes(AgentLogRow row)
        {
            if (row.Kind == AgentLogRowKind.CaptureGap)
                return row.SummaryText ?? "Observations may be missing while capture was detached.";

            if (!string.IsNullOrEmpty(row.SummaryText))
                return row.SummaryText;

            return "No additional detail";
        }

        static string FormatAck(TryllObservationEnvelope ack)
        {
            if (ack == null) return "—";
            if (ack.IsError)
                return $"Error: {ack.Message ?? ack.Text ?? TryllObservationLabels.Of(ack)}";
            return TryllObservationLabels.Of(ack);
        }

        static string FormatHint(ulong requestId, ulong agentId, System.DateTime wallTimeUtc)
        {
            var parts = new List<string>(3);
            if (requestId != 0) parts.Add($"req {requestId}");
            if (agentId != 0) parts.Add($"agent {agentId}");
            parts.Add(FormatTime(wallTimeUtc));
            return string.Join(" · ", parts);
        }

        static string FormatTime(System.DateTime wallTimeUtc)
        {
            if (wallTimeUtc == default) return "—";
            return wallTimeUtc.ToLocalTime().ToString("HH:mm:ss.fff");
        }

        /// <summary>All-Agents timeline gutter — live DisplayName / LabelHint, else "Agent N".</summary>
        public static string FormatAgentGutterLabel(ulong agentId, AgentDescriptorRegistry descriptors)
        {
            if (descriptors != null && descriptors.TryGet(agentId, out var descriptor)
                && !string.IsNullOrEmpty(descriptor.Label))
            {
                return descriptor.Label;
            }
            return $"Agent {agentId}";
        }

        /// <summary>Dim status line under an agent name in the Agents list.</summary>
        public static string FormatAgentListStatus(
            int completedTurns,
            bool destroyed,
            bool limitedDiagnostics,
            bool pending)
        {
            if (pending)
                return "creating…";

            var parts = new List<string>(3) { FormatTurnCount(completedTurns) };
            if (destroyed)
                parts.Add("destroyed");
            if (limitedDiagnostics)
                parts.Add("limited diag");
            return string.Join(" · ", parts);
        }

        /// <summary>Dim status line under the All Agents entry.</summary>
        public static string FormatAllAgentsStatus(int agentCount, int completedTurns)
        {
            if (agentCount <= 0)
                return "no agents";

            var agents = agentCount == 1 ? "1 agent" : $"{agentCount} agents";
            if (completedTurns <= 0)
                return agents;
            return $"{agents} · {FormatTurnCount(completedTurns)}";
        }

        static string FormatTurnCount(int completedTurns)
        {
            if (completedTurns <= 0) return "no turns";
            return completedTurns == 1 ? "1 turn" : $"{completedTurns} turns";
        }
    }
}
