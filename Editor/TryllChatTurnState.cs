// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;

namespace Tryll.Client.Editor
{
    /// <summary>How an assistant-side chat turn should appear in the transcript.</summary>
    internal enum TryllChatTurnDisplay
    {
        Streaming,
        Success,
        Cancelled,
        Error,
        /// <summary>Completed successfully but produced no visible assistant text.</summary>
        Silent,
        /// <summary>
        /// A pause outlived its budget and the turn was abandoned. Presented like
        /// <see cref="Cancelled"/> (an interrupted turn) but labelled distinctly:
        /// "Cancelled" would imply somebody asked for it, when in fact Resume was
        /// simply never called.
        /// </summary>
        PauseTimedOut,
    }

    /// <summary>
    /// One retained chat turn. Package 1 keeps this chat-local — not a general event stream.
    /// </summary>
    internal sealed class TryllChatTurnRecord
    {
        public string TurnKey;
        public int Sequence;
        public string UserText = "";
        public bool FromVoice;
        public string AssistantText = "";
        public TryllTurnStatus Status = TryllTurnStatus.Success;
        public string ErrorText = "";
        public TryllJsonValue DebugInfo = TryllJsonValue.Null;
        public string DebugInfoRaw = "";
        public int TokensGenerated;
        public int DurationMs;
        public int TtftMs;
        public TryllChatTurnDisplay Display = TryllChatTurnDisplay.Streaming;
        public bool IsComplete;
        public DateTime StartedUtc;
    }

    internal sealed class TryllChatLabHistory
    {
        public readonly List<(bool IsUser, string Text)> Items =
            new List<(bool IsUser, string Text)>();
        public int SkippedTurnCount;
    }

    /// <summary>
    /// Pure selection / finalization helpers for editor chat. Extracted so Unity and tests
    /// share one set of follow rules (R2).
    /// </summary>
    internal static class TryllChatTurnLogic
    {
        public static string MakeTurnKey(int sequence) => $"chat:{sequence}";

        public static bool CanBeginTurn(TryllChatTurnRecord activeTurn, bool isTurnInFlight) =>
            activeTurn == null && !isTurnInFlight;

        public static TryllChatTurnRecord BeginTurn(int sequence, string userText, bool fromVoice)
        {
            return new TryllChatTurnRecord
            {
                TurnKey = MakeTurnKey(sequence),
                Sequence = sequence,
                UserText = userText ?? "",
                FromVoice = fromVoice,
                Display = TryllChatTurnDisplay.Streaming,
                IsComplete = false,
                StartedUtc = DateTime.UtcNow,
            };
        }

        public static void AppendAssistantDelta(TryllChatTurnRecord turn, string text, bool isDelta)
        {
            if (turn == null || turn.IsComplete) return;
            if (isDelta) turn.AssistantText += text ?? "";
            else         turn.AssistantText  = text ?? "";
        }

        public static void FinalizeTurn(
            TryllChatTurnRecord turn,
            TryllTurnStatus status,
            string debugInfoRaw,
            int tokensGenerated,
            string errorText = null)
        {
            if (turn == null) return;

            turn.Status = status;
            turn.ErrorText = errorText ?? "";
            if (status == TryllTurnStatus.PauseTimedOut && string.IsNullOrEmpty(turn.ErrorText))
            {
                // Say why the turn stopped. Mirrors the Unreal panel's ResolveErrorText:
                // the status label alone cannot carry the fix.
                turn.ErrorText =
                    "Pause timed out — the turn was abandoned because Resume was never "
                    + "called. Subscribe to TryllClient.Paused (or register a tool handler), "
                    + "or raise the node's pause_timeout_ms.";
            }
            turn.TokensGenerated = tokensGenerated;
            turn.DebugInfoRaw = debugInfoRaw ?? "";
            turn.IsComplete = true;

            int wallMs = (int)(DateTime.UtcNow - turn.StartedUtc).TotalMilliseconds;
            turn.DurationMs = wallMs;

            if (!string.IsNullOrEmpty(debugInfoRaw) && TryllJson.TryParse(debugInfoRaw, out var parsed, out _))
            {
                turn.DebugInfo = parsed;
                var durS = parsed.Get("duration_s");
                if (!durS.IsNull) turn.DurationMs = (int)(durS.AsDouble() * 1000);
                var ttft = parsed.Get("time_to_first_token_ms");
                if (!ttft.IsNull) turn.TtftMs = (int)ttft.AsDouble();
            }
            else
            {
                turn.DebugInfo = TryllJsonValue.Null;
            }

            turn.Display = ClassifyDisplay(status, turn.AssistantText);
        }

        public static TryllChatTurnDisplay ClassifyDisplay(TryllTurnStatus status, string assistantText)
        {
            // A pause that timed out is an *interrupted* turn — partial interaction
            // kept, exactly as with StopAndKeep — but it gets its own label, because
            // "Cancelled" would tell the reader somebody asked for it when nobody did.
            if (status == TryllTurnStatus.PauseTimedOut) return TryllChatTurnDisplay.PauseTimedOut;
            if (status == TryllTurnStatus.Cancelled) return TryllChatTurnDisplay.Cancelled;
            if (status == TryllTurnStatus.Error)     return TryllChatTurnDisplay.Error;
            if (string.IsNullOrEmpty(assistantText)) return TryllChatTurnDisplay.Silent;
            return TryllChatTurnDisplay.Success;
        }

        public static string StatusRowLabel(TryllChatTurnDisplay display) => display switch
        {
            TryllChatTurnDisplay.Cancelled     => "Cancelled",
            TryllChatTurnDisplay.PauseTimedOut => "Pause timed out",
            TryllChatTurnDisplay.Error         => "Error",
            TryllChatTurnDisplay.Silent        => "No visible output",
            _                              => "",
        };

        /// <summary>True when the assistant side should render a compact status row instead of answer text.</summary>
        public static bool UsesStatusRow(TryllChatTurnRecord turn) =>
            turn != null
            && turn.IsComplete
            && string.IsNullOrEmpty(turn.AssistantText);

        public static string TerminalBadgeLabel(TryllChatTurnRecord turn)
        {
            if (turn == null || !turn.IsComplete || string.IsNullOrEmpty(turn.AssistantText))
                return "";
            if (turn.Status == TryllTurnStatus.Cancelled) return "Cancelled";
            // Say why: "Cancelled" alone would imply someone asked for it.
            if (turn.Status == TryllTurnStatus.PauseTimedOut) return "Pause timed out";
            if (turn.Status == TryllTurnStatus.Error) return "Error";
            return "";
        }

        public static TryllTurnTerminalStatus ToTerminalStatus(TryllTurnStatus status) => status switch
        {
            TryllTurnStatus.Cancelled => TryllTurnTerminalStatus.Cancelled,
            TryllTurnStatus.PauseTimedOut => TryllTurnTerminalStatus.Cancelled,
            TryllTurnStatus.Error => TryllTurnTerminalStatus.Error,
            _ => TryllTurnTerminalStatus.Success,
        };

        public static string ExtractErrorText(TryllJsonValue debugInfo)
        {
            if (debugInfo == null || debugInfo.IsNull) return null;

            var errors = debugInfo.Get("errors").AsArray();
            if (errors.Count > 0)
            {
                string text = errors[0].AsString(null);
                if (!string.IsNullOrEmpty(text)) return text;
                text = errors[0].Get("message").AsString(null);
                if (!string.IsNullOrEmpty(text)) return text;
            }

            string direct = debugInfo.Get("error").AsString(null)
                ?? debugInfo.Get("error_message").AsString(null);
            return string.IsNullOrEmpty(direct) ? null : direct;
        }

        public static TryllTurnInspection ToInspection(TryllChatTurnRecord turn)
        {
            if (turn == null || !turn.IsComplete)
                return TryllTurnInspectionFactory.Empty("select an answer");

            var route = ExtractRoute(turn.DebugInfo);
            return TryllTurnInspectionFactory.FromChatTurn(
                turn.TurnKey,
                $"turn {turn.Sequence}" + (turn.FromVoice ? " · voice" : ""),
                turn.AssistantText ?? "",
                ToTerminalStatus(turn.Status),
                turn.ErrorText,
                route,
                turn.DurationMs,
                turn.TtftMs,
                turn.DebugInfo ?? TryllJsonValue.Null,
                turn.DebugInfoRaw);
        }

        public static TryllChatLabHistory BuildLabHistory(IReadOnlyList<TryllChatTurnRecord> turns)
        {
            var result = new TryllChatLabHistory();
            if (turns == null) return result;

            foreach (var turn in turns)
            {
                if (turn == null || !turn.IsComplete || string.IsNullOrEmpty(turn.UserText))
                    continue;
                if (string.IsNullOrEmpty(turn.AssistantText))
                {
                    result.SkippedTurnCount++;
                    continue;
                }
                result.Items.Add((true, turn.UserText));
                result.Items.Add((false, turn.AssistantText));
            }
            return result;
        }

        public static TryllTurnRouteNode[] ExtractRoute(TryllJsonValue debugInfo)
        {
            if (debugInfo == null || debugInfo.IsNull) return Array.Empty<TryllTurnRouteNode>();
            var nodes = debugInfo.Get("nodes").AsArray();
            if (nodes == null || nodes.Count == 0) return Array.Empty<TryllTurnRouteNode>();

            var route = new TryllTurnRouteNode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                route[i] = new TryllTurnRouteNode
                {
                    Name = n.Get("name").AsString(""),
                    Type = n.Get("_type").AsString(""),
                    Exit = n.Get("exit_route").AsString(""),
                    Ms   = (int)(n.Get("duration_s").AsDouble() * 1000),
                };
            }
            return route;
        }

        /// <summary>
        /// Selection-follow rules (call after <paramref name="completed"/> is already in
        /// <paramref name="turns"/> and finalized):
        /// 1. First completed answer becomes selected when nothing was selected.
        /// 2. If selection was the previous latest, advance to the new completion.
        /// 3. An older explicit selection is preserved.
        /// </summary>
        public static string OnTurnCompleted(
            string selectedKey,
            IReadOnlyList<TryllChatTurnRecord> turns,
            TryllChatTurnRecord completed)
        {
            if (completed == null || !completed.IsComplete) return selectedKey;
            if (string.IsNullOrEmpty(selectedKey)) return completed.TurnKey;

            string previousLatest = null;
            if (turns != null)
            {
                for (int i = turns.Count - 1; i >= 0; i--)
                {
                    var t = turns[i];
                    if (t == null || !t.IsComplete) continue;
                    if (t.TurnKey == completed.TurnKey) continue;
                    previousLatest = t.TurnKey;
                    break;
                }
            }

            // No prior completed answer, or still following that prior latest.
            if (previousLatest == null || selectedKey == previousLatest)
                return completed.TurnKey;

            return selectedKey;
        }

        /// <summary>True when <paramref name="selectedKey"/> is the newest completed turn (or empty).</summary>
        public static bool IsFollowingLatest(string selectedKey, IReadOnlyList<TryllChatTurnRecord> turns)
        {
            if (string.IsNullOrEmpty(selectedKey) || turns == null || turns.Count == 0)
                return true;

            for (int i = turns.Count - 1; i >= 0; i--)
            {
                if (turns[i] != null && turns[i].IsComplete)
                    return turns[i].TurnKey == selectedKey;
            }
            return true;
        }

        /// <summary>After reset/stop: clear selection when the selected record is gone.</summary>
        public static string SanitizeSelection(string selectedKey, IReadOnlyList<TryllChatTurnRecord> turns)
        {
            if (string.IsNullOrEmpty(selectedKey) || turns == null) return null;
            for (int i = 0; i < turns.Count; i++)
            {
                if (turns[i] != null && turns[i].TurnKey == selectedKey)
                    return selectedKey;
            }
            return null;
        }
    }
}
