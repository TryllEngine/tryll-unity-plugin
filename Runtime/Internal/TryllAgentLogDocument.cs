// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Tryll.Generated;

namespace Tryll.Client.Internal
{
    internal sealed class TryllAgentLogExportAgent
    {
        public ulong AgentId;
        public ulong Epoch;
        public string Label = "";
        public bool HasCreatedSeq;
        public ulong CreatedSeq;
        public bool HasDestroyedSeq;
        public ulong DestroyedSeq;
        public bool Diagnostics;
        public bool CreatedBeforeDiagnosticsCapture;
        public int CompletedTurns;
    }

    internal sealed class TryllAgentLogExportSnapshot
    {
        public string Host = "unity";
        public string HostVersion = "";
        public string PluginVersion = "0.9.0-DEV";
        public string ProjectName = "";
        public string Os = "";
        public uint ProtocolVersion = Tryll.Client.TryllProtocol.Version;
        public string CodegenFingerprint = CodegenFingerprintInfo.Value;
        public string ServerVersion = "";
        public string ServerRunId = "";
        public string ServerBuildConfig = "";
        public string ServerMode = "";
        public string CaptureState = "idle";
        public bool HistoryDiscarded;
        public ulong SelectedAgentId;
        public int MaxTurns = 200;
        public int MaxObservations = 4000;
        public int MaxRaw = 4000;
        public int EvictedTurns;
        public int EvictedObservations;
        public List<TryllAgentLogExportAgent> Agents = new List<TryllAgentLogExportAgent>();
        public List<TryllObservationEnvelope> Events = new List<TryllObservationEnvelope>();
    }

    /// <summary>
    /// Writes one <c>tryll.agent_log</c> document from a main-thread journal snapshot.
    /// Coalescing happens here. The window keeps every event.
    /// </summary>
    internal static class TryllAgentLogDocument
    {
        sealed class Run
        {
            public TryllObservationEnvelope First;
            public TryllObservationEnvelope Last;
            public string Type;
            public string Payload;
            public int Chunks = 1;
            public string NodeName = "";
            public TryllLogTable Table;
            public ulong FirstSeq;
            public ulong LastSeq;
        }

        sealed class TurnAccum
        {
            public string Id;
            public ulong AgentId;
            public ulong FirstSeq;
            public ulong LastSeq;
            public double FirstMs;
            public double LastMs;
            public bool SawAnswer;
            public double FirstAnswerMs;
            public bool Complete;
            public string Status;
            public bool HasTokens;
            public ulong Tokens;
            public bool FromDebug;
            public double DurationMs;
            public double TtftMs;
            public bool HasDuration;
            public bool HasTtft;
        }

        public static string Write(TryllAgentLogExportSnapshot snapshot)
        {
            snapshot ??= new TryllAgentLogExportSnapshot();
            var events = Coalesce(snapshot.Events);
            var origin = Origin(snapshot.Events);
            var turns = IndexTurns(events, origin);

            var w = new TryllJsonWriter(true);
            w.BeginObject();
            w.Key("format"); w.String("tryll.agent_log");
            w.Key("format_version"); w.Int64(1);
            w.Key("exported_at"); w.String(FormatUtc(DateTime.UtcNow));
            w.Key("exporter");
            w.BeginObject();
            w.Key("host"); w.String(snapshot.Host ?? "");
            w.Key("host_version"); w.String(snapshot.HostVersion ?? "");
            w.Key("plugin_version"); w.String(snapshot.PluginVersion ?? "");
            w.Key("project_name"); w.String(snapshot.ProjectName ?? "");
            w.Key("os"); w.String(snapshot.Os ?? "");
            w.EndObject();
            w.Key("protocol");
            w.BeginObject();
            w.Key("version"); w.UInt64(snapshot.ProtocolVersion);
            w.Key("codegen_fingerprint"); w.String(snapshot.CodegenFingerprint ?? "");
            w.EndObject();
            w.Key("server");
            w.BeginObject();
            WriteOptional(w, "version", snapshot.ServerVersion);
            WriteOptional(w, "run_id", snapshot.ServerRunId);
            WriteOptional(w, "build_config", snapshot.ServerBuildConfig);
            WriteOptional(w, "mode", snapshot.ServerMode);
            w.EndObject();
            w.Key("capture");
            w.BeginObject();
            w.Key("state"); w.String(string.IsNullOrEmpty(snapshot.CaptureState) ? "idle" : snapshot.CaptureState);
            w.Key("history_discarded_by_reload"); w.Bool(snapshot.HistoryDiscarded);
            if (snapshot.SelectedAgentId != 0)
            {
                w.Key("selected_agent_id"); w.UInt64(snapshot.SelectedAgentId);
            }
            w.Key("retention");
            w.BeginObject();
            w.Key("max_completed_turns_per_agent"); w.Int64(snapshot.MaxTurns);
            w.Key("max_observations_per_agent"); w.Int64(snapshot.MaxObservations);
            w.Key("max_raw_session_observations"); w.Int64(snapshot.MaxRaw);
            w.EndObject();
            w.Key("evicted");
            w.BeginObject();
            w.Key("turns"); w.Int64(snapshot.EvictedTurns);
            w.Key("observations"); w.Int64(snapshot.EvictedObservations);
            w.EndObject();
            if (snapshot.Events != null && snapshot.Events.Count > 0)
            {
                w.Key("oldest_retained_utc");
                w.String(FormatUtc(snapshot.Events[0].WallTimeUtc));
            }
            w.Key("coalesced"); w.Bool(true);
            w.EndObject();
            w.Key("agents");
            w.BeginArray();
            if (snapshot.Agents != null)
            {
                foreach (var agent in snapshot.Agents)
                    WriteAgent(w, agent);
            }
            w.EndArray();
            w.Key("events");
            w.BeginArray();
            foreach (var run in events)
                WriteEvent(w, run, origin);
            w.EndArray();
            w.Key("turns");
            w.BeginArray();
            foreach (var turn in turns)
                WriteTurn(w, turn);
            w.EndArray();
            w.EndObject();
            return w.ToString();
        }

        public static string FileName(string projectName, DateTime localNow)
        {
            var project = string.IsNullOrEmpty(projectName) ? "project" : projectName;
            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                project = project.Replace(ch, '_');
            project = project.Replace(' ', '_');
            return "TryllAgentLog_" + project + "_" + localNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".json";
        }

        static List<Run> Coalesce(List<TryllObservationEnvelope> events)
        {
            var runs = new List<Run>();
            var open = new Dictionary<string, Run>();
            if (events == null) return runs;
            ulong fallback = 1;
            foreach (var env in events)
            {
                if (env == null) continue;
                var type = TypeName(env);
                TryllAgentLogJson.TryGetTable(type, out var table);
                var seq = env.EventId != 0 ? env.EventId : fallback;
                fallback = seq + 1;
                var key = StreamKey(env);
                if (table != null
                    && !string.IsNullOrEmpty(table.Coalesce)
                    && open.TryGetValue(key, out var run)
                    && CanMerge(run, env, table))
                {
                    Merge(run, env, table, seq);
                    continue;
                }

                var started = Start(env, type, table, seq);
                runs.Add(started);
                if (table != null && !string.IsNullOrEmpty(table.Coalesce))
                    open[key] = started;
                else
                    open.Remove(key);
            }
            return runs;
        }

        static Run Start(TryllObservationEnvelope env, string type, TryllLogTable table, ulong seq)
        {
            return new Run
            {
                First = env,
                Last = env,
                Type = type,
                Payload = PayloadJson(env),
                Table = table,
                NodeName = NodeName(env, table),
                FirstSeq = seq,
                LastSeq = seq,
            };
        }

        static bool CanMerge(Run run, TryllObservationEnvelope env, TryllLogTable table)
        {
            if (run.Type != TypeName(env)) return false;
            if (table.HasNodeName && run.NodeName != NodeName(env, table)) return false;
            return true;
        }

        static void Merge(Run run, TryllObservationEnvelope env, TryllLogTable table, ulong seq)
        {
            var incoming = PayloadJson(env);
            var replace = table.HasIsDelta && !env.Flag;
            if (table.Coalesce == "last")
                run.Payload = incoming;
            else if (table.Coalesce == "concat" && !string.IsNullOrEmpty(table.CoalesceField))
            {
                var field = table.CoalesceField;
                var left = ReadString(run.Payload, field) ?? "";
                var right = ReadString(incoming, field) ?? "";
                run.Payload = ReplaceString(run.Payload, field, replace ? right : left + right);
                run.Payload = CopyBool(run.Payload, incoming, "is_delta");
                run.Payload = CopyBool(run.Payload, incoming, "is_final");
            }
            else if (table.Coalesce == "sum")
            {
                if (!string.IsNullOrEmpty(table.CoalesceField))
                    run.Payload = SumKey(run.Payload, incoming, table.CoalesceField);
                else
                {
                    run.Payload = SumKey(run.Payload, incoming, "count");
                    run.Payload = SumKey(run.Payload, incoming, "bytes");
                }
            }
            run.Last = env;
            run.Chunks++;
            run.LastSeq = seq;
            run.NodeName = NodeName(env, table);
        }

        static string StreamKey(TryllObservationEnvelope env)
        {
            if (env.TurnKey.HasValue) return "t:" + TurnId(env);
            var epoch = env.ConnectionEpoch.ToString(CultureInfo.InvariantCulture);
            if (env.RequestId != 0) return "r:" + epoch + ":" + env.RequestId.ToString(CultureInfo.InvariantCulture);
            if (env.VoiceInputId != 0) return "v:" + epoch + ":" + env.VoiceInputId.ToString(CultureInfo.InvariantCulture);
            return "e:" + env.EventId.ToString(CultureInfo.InvariantCulture);
        }

        static string NodeName(TryllObservationEnvelope env, TryllLogTable table)
        {
            if (table == null || !table.HasNodeName) return "";
            if (!string.IsNullOrEmpty(env.NodeName)) return env.NodeName;
            return ReadString(PayloadJson(env), "node_name") ?? "";
        }

        static string TypeName(TryllObservationEnvelope env)
        {
            var name = TryllAgentLogFrames.TableName(env.Frame, env.IsLocal);
            if (!string.IsNullOrEmpty(name)) return name;
            if (env.IsCaptureGap) return "LogCaptureGap";
            if (env.Body != Tryll.MessageBody.NONE) return env.Body.ToString();
            return "";
        }

        static string PayloadJson(TryllObservationEnvelope env)
        {
            if (env.Frame == null || env.Frame.Length == 0) return "{}";
            var writer = new TryllJsonWriter(false);
            if (env.IsLocal)
                TryllAgentLogJson.WriteLocal(writer, env.Frame, env.Omitted);
            else
                TryllAgentLogJson.WriteMessage(writer, env.Frame, env.Omitted);
            var text = writer.ToString();
            return string.IsNullOrEmpty(text) ? "{}" : text;
        }

        static void WriteEvent(TryllJsonWriter w, Run run, TimeOrigin origin)
        {
            var env = run.First;
            var kind = Kind(env, run.Type);
            w.BeginObject();
            w.Key("seq"); w.UInt64(run.FirstSeq);
            w.Key("t_ms"); w.Double(Millis(env, origin));
            if (run.Chunks > 1)
            {
                w.Key("t_end_ms"); w.Double(Millis(run.Last, origin));
                w.Key("chunks"); w.Int64(run.Chunks);
            }
            w.Key("utc"); w.String(FormatUtc(env.WallTimeUtc));
            if (env.ConnectionEpoch != 0)
            {
                w.Key("epoch"); w.UInt64(env.ConnectionEpoch);
            }
            if (!env.IsLocal && !env.IsCaptureGap)
            {
                w.Key("dir");
                w.String(env.Direction == TryllObservationDirection.Outbound ? "out" : "in");
            }
            w.Key("kind"); w.String(kind);
            w.Key("scope"); w.String(ScopeName(env.Scope));
            if (!string.IsNullOrEmpty(run.Type))
            {
                w.Key("type"); w.String(run.Type);
            }
            if (env.RequestId != 0)
            {
                w.Key("request_id"); w.UInt64(env.RequestId);
            }
            if (env.AgentId != 0)
            {
                w.Key("agent_id"); w.UInt64(env.AgentId);
            }
            if (env.VoiceInputId != 0)
            {
                w.Key("voice_input_id"); w.UInt64(env.VoiceInputId);
            }
            var turn = TurnId(env);
            if (!string.IsNullOrEmpty(turn))
            {
                w.Key("turn"); w.String(turn);
            }
            if (env.CarrierRequestId != 0)
            {
                w.Key("carrier_request_id"); w.UInt64(env.CarrierRequestId);
            }
            var originName = OriginName(env, run.Type, kind);
            if (!string.IsNullOrEmpty(originName))
            {
                w.Key("origin"); w.String(originName);
            }
            w.Key("payload");
            w.Raw(string.IsNullOrEmpty(run.Payload) ? "{}" : run.Payload);
            w.EndObject();
        }

        static string Kind(TryllObservationEnvelope env, string type)
        {
            if (env.IsCaptureGap || type == "LogCaptureGap") return "marker";
            if (env.IsLocal && type == "LogConnectionState") return "connection";
            if (env.IsLocal && type == "LogLocalError") return "local_error";
            if (env.Body == Tryll.MessageBody.ErrorResponse || (env.IsError && !env.IsLocal)) return "error";
            if (env.IsLocal && env.IsError) return "local_error";
            if (env.Direction == TryllObservationDirection.Outbound)
                return "request";
            if (TryllMessageDescriptors.TryGet(env.RequestBody, out var request)
                && request.Terminal == env.Body)
                return "reply";
            if (env.Body == Tryll.MessageBody.Ack)
                return "reply";
            return "push";
        }

        static string OriginName(TryllObservationEnvelope env, string type, string kind)
        {
            if (kind == "error" && env.Body == Tryll.MessageBody.ErrorResponse) return "server";
            if (kind != "local_error" && kind != "error") return "";
            var fromPayload = ReadString(PayloadJson(env), "origin");
            return fromPayload ?? "";
        }

        static string ScopeName(TryllObservationScope scope)
        {
            switch (scope)
            {
            case TryllObservationScope.CuratedAgent: return "agent";
            case TryllObservationScope.MetadataOnly: return "metadata";
            default: return "session";
            }
        }

        static string TurnId(TryllObservationEnvelope env)
        {
            if (env == null || !env.TurnKey.HasValue) return "";
            var key = env.TurnKey;
            if (key.RequestId != 0)
                return key.ConnectionEpoch + ":" + key.AgentId + ":" + key.RequestId;
            return key.ConnectionEpoch + ":" + key.AgentId + ":s" + key.SyntheticSequence;
        }

        static List<TurnAccum> IndexTurns(List<Run> runs, TimeOrigin origin)
        {
            var order = new List<TurnAccum>();
            var byId = new Dictionary<string, TurnAccum>();
            foreach (var run in runs)
            {
                var id = TurnId(run.First);
                if (string.IsNullOrEmpty(id)) continue;
                if (!byId.TryGetValue(id, out var turn))
                {
                    turn = new TurnAccum
                    {
                        Id = id,
                        AgentId = run.First.AgentId,
                        FirstSeq = run.FirstSeq,
                        FirstMs = Millis(run.First, origin),
                    };
                    byId[id] = turn;
                    order.Add(turn);
                }
                turn.LastSeq = run.LastSeq;
                turn.LastMs = Millis(run.Last, origin);
                if (run.Type == "AnswerText" && !turn.SawAnswer)
                {
                    turn.SawAnswer = true;
                    turn.FirstAnswerMs = Millis(run.First, origin);
                }
                if (run.Type == "TurnComplete" || Kind(run.First, run.Type) == "error")
                    turn.Complete = true;
                ApplyTurnPayload(turn, run);
            }
            foreach (var turn in order)
            {
                if (!turn.HasDuration)
                {
                    turn.DurationMs = turn.LastMs - turn.FirstMs;
                    turn.HasDuration = true;
                }
                if (!turn.HasTtft && turn.SawAnswer)
                {
                    turn.TtftMs = turn.FirstAnswerMs - turn.FirstMs;
                    turn.HasTtft = true;
                }
                if (turn.Complete && string.IsNullOrEmpty(turn.Status))
                    turn.Status = "success";
            }
            return order;
        }

        static void ApplyTurnPayload(TurnAccum turn, Run run)
        {
            if (run.Type == "TurnComplete")
            {
                var status = ReadTopString(run.Payload, "status");
                if (!string.IsNullOrEmpty(status))
                    turn.Status = status.ToLowerInvariant();
                if (TryReadTopDouble(run.Payload, "tokens_generated", out var tokens))
                {
                    turn.HasTokens = true;
                    turn.Tokens = (ulong)tokens;
                }
                ApplyDebugTiming(turn, run.Payload);
            }
            if (Kind(run.First, run.Type) == "error" || Kind(run.Last, run.Type) == "error")
                turn.Status = "error";
            if (run.Last.TokensGenerated != 0 && !turn.HasTokens)
            {
                turn.HasTokens = true;
                turn.Tokens = run.Last.TokensGenerated;
            }
        }

        static void ApplyDebugTiming(TurnAccum turn, string payload)
        {
            if (string.IsNullOrEmpty(payload) || !TryLocate(payload, "debug_info", out var at))
                return;
            SkipWs(payload, ref at);
            string slice = null;
            if (at < payload.Length && payload[at] == '"')
                slice = ParseString(payload, ref at);
            else if (at < payload.Length && payload[at] == '{')
                slice = SliceObject(payload, at);
            if (string.IsNullOrEmpty(slice))
                return;
            if (TryReadDouble(slice, "duration_s", out var duration))
            {
                turn.DurationMs = duration * 1000.0;
                turn.HasDuration = true;
                turn.FromDebug = true;
            }
            if (TryReadDouble(slice, "time_to_first_token_ms", out var ttft))
            {
                turn.TtftMs = ttft;
                turn.HasTtft = true;
                turn.FromDebug = true;
            }
        }

        static string SliceObject(string json, int at)
        {
            if (at >= json.Length || json[at] != '{') return null;
            var depth = 0;
            var inString = false;
            var escape = false;
            for (var i = at; i < json.Length; ++i)
            {
                var c = json[i];
                if (inString)
                {
                    if (escape) escape = false;
                    else if (c == '\\') escape = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') { inString = true; continue; }
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(at, i - at + 1);
                }
            }
            return null;
        }

        static string ReadTopString(string json, string key)
        {
            if (!TryLocateTop(json, key, out var at)) return null;
            SkipWs(json, ref at);
            if (at >= json.Length || json[at] != '"') return null;
            return ParseString(json, ref at);
        }

        static bool TryReadTopDouble(string json, string key, out double value)
        {
            value = 0;
            if (!TryLocateTop(json, key, out var at)) return false;
            return TryReadDoubleAt(json, ref at, out value);
        }

        static bool TryLocateTop(string json, string key, out int valueAt)
        {
            valueAt = 0;
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return false;
            var needle = "\"" + key + "\"";
            var inString = false;
            var escape = false;
            var depth = 0;
            for (var i = 0; i < json.Length; ++i)
            {
                var c = json[i];
                if (inString)
                {
                    if (escape) escape = false;
                    else if (c == '\\') escape = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"')
                {
                    if (depth == 1 && string.CompareOrdinal(json, i, needle, 0, needle.Length) == 0)
                    {
                        var after = i + needle.Length;
                        SkipWs(json, ref after);
                        if (after < json.Length && json[after] == ':')
                        {
                            valueAt = after + 1;
                            return true;
                        }
                    }
                    inString = true;
                    continue;
                }
                if (c == '{' || c == '[') depth++;
                else if ((c == '}' || c == ']') && depth > 0) depth--;
            }
            return false;
        }

        static bool TryReadDouble(string json, string key, out double value)
        {
            value = 0;
            if (!TryLocate(json, key, out var at)) return false;
            return TryReadDoubleAt(json, ref at, out value);
        }

        static bool TryReadDoubleAt(string json, ref int at, out double value)
        {
            value = 0;
            SkipWs(json, ref at);
            var start = at;
            if (at < json.Length && json[at] == '-') at++;
            var sawDigit = false;
            while (at < json.Length && json[at] >= '0' && json[at] <= '9') { sawDigit = true; at++; }
            if (at < json.Length && json[at] == '.')
            {
                at++;
                while (at < json.Length && json[at] >= '0' && json[at] <= '9') { sawDigit = true; at++; }
            }
            if (at < json.Length && (json[at] == 'e' || json[at] == 'E'))
            {
                var exponent = at + 1;
                if (exponent < json.Length && (json[exponent] == '+' || json[exponent] == '-')) exponent++;
                var digits = exponent;
                while (digits < json.Length && json[digits] >= '0' && json[digits] <= '9') digits++;
                if (digits > exponent) at = digits;
            }
            if (!sawDigit) return false;
            return double.TryParse(
                json.Substring(start, at - start),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        static void WriteTurn(TryllJsonWriter w, TurnAccum turn)
        {
            w.BeginObject();
            w.Key("turn"); w.String(turn.Id);
            if (turn.AgentId != 0)
            {
                w.Key("agent_id"); w.UInt64(turn.AgentId);
            }
            w.Key("first_seq"); w.UInt64(turn.FirstSeq);
            w.Key("last_seq"); w.UInt64(turn.LastSeq);
            w.Key("complete"); w.Bool(turn.Complete);
            if (turn.Complete && !string.IsNullOrEmpty(turn.Status))
            {
                w.Key("status"); w.String(turn.Status);
            }
            if (turn.HasDuration)
            {
                w.Key("duration_ms"); w.Double(turn.DurationMs);
            }
            if (turn.HasTtft)
            {
                w.Key("ttft_ms"); w.Double(turn.TtftMs);
            }
            if (turn.HasTokens)
            {
                w.Key("tokens_generated"); w.UInt64(turn.Tokens);
            }
            if (!turn.FromDebug)
            {
                w.Key("timing_source"); w.String("client");
            }
            w.EndObject();
        }

        static void WriteAgent(TryllJsonWriter w, TryllAgentLogExportAgent agent)
        {
            w.BeginObject();
            w.Key("agent_id"); w.UInt64(agent.AgentId);
            if (agent.Epoch != 0)
            {
                w.Key("epoch"); w.UInt64(agent.Epoch);
            }
            w.Key("label"); w.String(agent.Label ?? "");
            w.Key("created_seq");
            if (agent.HasCreatedSeq) w.UInt64(agent.CreatedSeq);
            else w.Null();
            w.Key("destroyed_seq");
            if (agent.HasDestroyedSeq) w.UInt64(agent.DestroyedSeq);
            else w.Null();
            w.Key("diagnostics"); w.Bool(agent.Diagnostics);
            w.Key("created_before_diagnostics_capture"); w.Bool(agent.CreatedBeforeDiagnosticsCapture);
            w.Key("completed_turns"); w.Int64(agent.CompletedTurns);
            w.EndObject();
        }

        struct TimeOrigin
        {
            public bool UseTicks;
            public long Ticks;
            public DateTime Utc;
        }

        static TimeOrigin Origin(List<TryllObservationEnvelope> events)
        {
            var origin = new TimeOrigin { Utc = DateTime.UtcNow };
            if (events == null) return origin;
            foreach (var env in events)
            {
                if (env == null) continue;
                origin.Utc = env.WallTimeUtc;
                origin.UseTicks = env.MonotonicTicks != 0;
                origin.Ticks = env.MonotonicTicks;
                break;
            }
            return origin;
        }

        static double Millis(TryllObservationEnvelope env, TimeOrigin origin)
        {
            if (env == null) return 0;
            if (origin.UseTicks && env.MonotonicTicks != 0)
                return (env.MonotonicTicks - origin.Ticks) * 1000.0 / Stopwatch.Frequency;
            return (env.WallTimeUtc - origin.Utc).TotalMilliseconds;
        }

        static string FormatUtc(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return utc.ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        }

        static void WriteOptional(TryllJsonWriter w, string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            w.Key(key);
            w.String(value);
        }

        static string SumKey(string kept, string incoming, string key)
        {
            var left = ReadNumbers(kept, key);
            var right = ReadNumbers(incoming, key);
            var count = left.Count < right.Count ? left.Count : right.Count;
            for (var i = count - 1; i >= 0; --i)
                kept = ReplaceNthNumber(kept, key, i, left[i] + right[i]);
            return kept;
        }

        static string CopyBool(string kept, string incoming, string key)
        {
            if (!TryReadBool(incoming, key, out var value)) return kept;
            return ReplaceBool(kept, key, value);
        }

        static string ReadString(string json, string key)
        {
            if (!TryLocate(json, key, out var at)) return null;
            SkipWs(json, ref at);
            if (at >= json.Length || json[at] != '"') return null;
            return ParseString(json, ref at);
        }

        static string ReplaceString(string json, string key, string value)
        {
            if (!TryLocate(json, key, out var at)) return json;
            SkipWs(json, ref at);
            if (at >= json.Length || json[at] != '"') return json;
            var start = at;
            ParseString(json, ref at);
            return json.Substring(0, start) + TryllJsonWriter.Quote(value ?? "") + json.Substring(at);
        }

        static bool TryReadBool(string json, string key, out bool value)
        {
            value = false;
            if (!TryLocate(json, key, out var at)) return false;
            SkipWs(json, ref at);
            if (Starts(json, at, "true")) { value = true; return true; }
            if (Starts(json, at, "false")) { value = false; return true; }
            return false;
        }

        static string ReplaceBool(string json, string key, bool value)
        {
            if (!TryLocate(json, key, out var at)) return json;
            SkipWs(json, ref at);
            var token = value ? "true" : "false";
            if (Starts(json, at, "true"))
                return json.Substring(0, at) + token + json.Substring(at + 4);
            if (Starts(json, at, "false"))
                return json.Substring(0, at) + token + json.Substring(at + 5);
            return json;
        }

        static List<long> ReadNumbers(string json, string key)
        {
            var values = new List<long>();
            var from = 0;
            while (TryLocate(json, key, out var at, from))
            {
                SkipWs(json, ref at);
                if (TryReadLong(json, ref at, out var number))
                    values.Add(number);
                from = at;
            }
            return values;
        }

        static string ReplaceNthNumber(string json, string key, int index, long value)
        {
            var from = 0;
            var seen = 0;
            while (TryLocate(json, key, out var at, from))
            {
                SkipWs(json, ref at);
                var start = at;
                if (!TryReadLong(json, ref at, out _))
                {
                    from = at + 1;
                    continue;
                }
                if (seen == index)
                {
                    return json.Substring(0, start)
                        + value.ToString(CultureInfo.InvariantCulture)
                        + json.Substring(at);
                }
                seen++;
                from = at;
            }
            return json;
        }

        static bool TryLocate(string json, string key, out int valueAt, int from = 0)
        {
            valueAt = 0;
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return false;
            var needle = "\"" + key + "\"";
            var inString = false;
            var escape = false;
            for (var i = from; i < json.Length; ++i)
            {
                var c = json[i];
                if (inString)
                {
                    if (escape) escape = false;
                    else if (c == '\\') escape = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c != '"') continue;
                if (string.CompareOrdinal(json, i, needle, 0, needle.Length) != 0)
                {
                    inString = true;
                    continue;
                }
                var after = i + needle.Length;
                SkipWs(json, ref after);
                if (after >= json.Length || json[after] != ':')
                {
                    inString = true;
                    continue;
                }
                valueAt = after + 1;
                return true;
            }
            return false;
        }

        static string ParseString(string json, ref int i)
        {
            if (i >= json.Length || json[i] != '"') return "";
            i++;
            var sb = new StringBuilder();
            while (i < json.Length)
            {
                var c = json[i++];
                if (c == '"') break;
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= json.Length) break;
                var esc = json[i++];
                switch (esc)
                {
                case '"':
                case '\\':
                case '/': sb.Append(esc); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u':
                    if (i + 4 <= json.Length
                        && int.TryParse(json.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
                    {
                        sb.Append((char)cp);
                        i += 4;
                    }
                    break;
                default:
                    sb.Append(esc);
                    break;
                }
            }
            return sb.ToString();
        }

        static bool TryReadLong(string json, ref int i, out long value)
        {
            value = 0;
            var start = i;
            if (i < json.Length && json[i] == '-') i++;
            if (i >= json.Length || json[i] < '0' || json[i] > '9')
            {
                i = start;
                return false;
            }
            while (i < json.Length && json[i] >= '0' && json[i] <= '9') i++;
            var end = i;
            if (i < json.Length && (json[i] == '.' || json[i] == 'e' || json[i] == 'E'))
            {
                if (json[i] == '.')
                {
                    i++;
                    while (i < json.Length && json[i] >= '0' && json[i] <= '9') i++;
                }
                if (i < json.Length && (json[i] == 'e' || json[i] == 'E'))
                {
                    i++;
                    if (i < json.Length && (json[i] == '+' || json[i] == '-')) i++;
                    while (i < json.Length && json[i] >= '0' && json[i] <= '9') i++;
                }
                return long.TryParse(json.Substring(start, end - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
            }
            return long.TryParse(json.Substring(start, i - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        static void SkipWs(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        }

        static bool Starts(string json, int at, string token)
        {
            return at + token.Length <= json.Length
                && string.CompareOrdinal(json, at, token, 0, token.Length) == 0;
        }
    }
}
