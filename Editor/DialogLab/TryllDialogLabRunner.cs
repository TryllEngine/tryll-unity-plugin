// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
        /// Executes one immutable <see cref="TryllDialogLabRunSnapshot"/>: for each dialogue,
        /// for each variant, a fresh agent; for each repeat within it, the same agent
        /// reseeded and rewound. Serial by design. Never wires TTS callbacks.
        ///
        /// It reaches a <see cref="TryllAgentComponent"/> only when a variant has
        /// <c>UseLiveTools</c> and a step authors no canned result for a tool: the
        /// scene component may still have a live handler, and those components come
        /// in through <see cref="RunAsync"/> rather than the snapshot. An uncanned
        /// call with live tools off resumes empty and CheckFails after the turn.
    /// </summary>
    internal sealed class TryllDialogLabRunner
    {
        public bool IsRunning { get; private set; }

        /// <summary>(completedTurnUnits, totalTurnUnits). A unit is one generated step.</summary>
        public event Action<int, int> Progress;

        private volatile bool _stopRequested;
        private TryllAgent _currentAgent;
        private TryllClient _client;
        private TryllGraphDescription _currentGraph;
        private TryllDialogStepData _currentStep;
        /// <summary>Scene component whose live tool handlers serve the variant being run, or null.</summary>
        private TryllAgentComponent _liveToolAgent;
        /// <summary>Snapshot flag for the variant being run. Live fallback is gated on this.</summary>
        private bool _currentVariantUsesLiveTools;
        private string _turnFailure;
        /// <summary>Tool names empty-resumed this turn (Missing source). CheckFail still applies.</summary>
        private readonly HashSet<string> _missingResumedTools = new HashSet<string>(StringComparer.Ordinal);
        private TaskCompletionSource<(TryllTurnStatus status, string debugInfo, int tokens)> _turnTcs;
        private System.Text.StringBuilder _answerBuffer;
        private Action<bool> _onConnectionChanged;

        /// <summary>
        /// Seconds a turn may take before the run gives up on it; 0 = wait forever (the
        /// interactive Lab). A timed-out turn is an Error cell and ends the run as Failed:
        /// a hung server or model would hang every later turn too.
        /// </summary>
        public double TurnTimeoutSeconds;

        /// <summary>True when the last run ended because a turn ran past <see cref="TurnTimeoutSeconds"/>.</summary>
        public bool TimedOut { get; private set; }

        public void Stop()
        {
            _stopRequested = true;
            if (_currentAgent != null && _currentAgent.IsValid)
                _currentAgent.Cancel(TryllCancelMode.StopAndDiscard);
            // Dead connection / disposed agent cannot produce TurnComplete — settle locally.
            SettleTurn(TryllTurnStatus.Cancelled, "", 0, "Cancelled");
        }

        /// <summary>
        /// Raised after every variant and seed has run dialogue <c>completedDialogues - 1</c>
        /// (main thread). The result is the live, still-growing one: dialogues before
        /// <c>completedDialogues</c> are final and may be shown, later ones are not.
        /// </summary>
        public event Action<TryllDialogLabRunResult, int> DialogueCompleted;

        public async Task<TryllDialogLabRunResult> RunAsync(
            TryllClient client,
            TryllDialogLabRunSnapshot snapshot,
            IReadOnlyList<TryllAgentComponent> liveToolAgents = null)
        {
            if (IsRunning)
            {
                return new TryllDialogLabRunResult
                {
                    Snapshot = snapshot,
                    Status = TryllDialogLabRunStatus.Failed,
                    Issues = { "A Dialog Lab run is already in progress." },
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    CompletedAtUtc = DateTimeOffset.UtcNow,
                };
            }

            IsRunning = true;
            _stopRequested = false;
            TimedOut = false;
            _client = client;

            var result = new TryllDialogLabRunResult
            {
                Snapshot = snapshot,
                Server = TryllDialogLabExport.ServerIdentity.FromClient(client),
                StartedAtUtc = DateTimeOffset.UtcNow,
                Status = TryllDialogLabRunStatus.Completed,
            };

            int repeats = Mathf.Max(1, snapshot.Repeats);
            int totalUnits = snapshot.PlannedGeneratedTurns;
            int doneUnits = 0;

            // One sample per variant x seed column, created up front with every dialogue's
            // turns as "not run": the dialogue loop fills its own slice of each column.
            var samples = new TryllLabSampleResult[snapshot.Variants.Count, repeats];
            for (int vi = 0; vi < snapshot.Variants.Count; vi++)
            {
                for (int r = 0; r < repeats; r++)
                {
                    var sample = NewSample(snapshot.Variants[vi], vi, r, snapshot.DeriveSeed(vi, r));
                    for (int d = 0; d < snapshot.Dialogues.Count; d++)
                        AppendDialogueTurns(sample, snapshot.Dialogues[d], d, "Not run");
                    samples[vi, r] = sample;
                    result.Samples.Add(sample);
                }
            }

            Action<ulong, string, string> onPaused = (agentId, nodeName, _) =>
            {
                if (_currentAgent == null || agentId != _currentAgent.AgentId) return;
                var node = _currentGraph?.Nodes?.FirstOrDefault(n => n.Name == nodeName);
                bool isToolCallPause = node?.Params is TryllToolCallParams;
                if (!isToolCallPause)
                    _currentAgent.Resume();
            };

            _onConnectionChanged = isConnected =>
            {
                if (!isConnected && _turnTcs != null && !_turnTcs.Task.IsCompleted)
                    SettleTurn(TryllTurnStatus.Error, "", 0, "Disconnected");
            };

            client.Paused += onPaused;
            client.ConnectionChanged += _onConnectionChanged;

            try
            {
                // Dialogue-major: a dialogue finishes across every column before the next
                // starts, so it can be shown as soon as it is done without showing a
                // biased subset of its own cells.
                for (int d = 0; d < snapshot.Dialogues.Count; d++)
                {
                    if (_stopRequested) break;
                    var script = snapshot.Dialogues[d];
                    int offset = snapshot.TurnOffset(d);
                    string prefix = TryllDialogLabDialogues.Prefix(d, script.Name, snapshot.Dialogues.Count);
                    // Script-level, so once per dialogue rather than once per variant.
                    foreach (var warn in TryllDialogScriptValidation.CollectSeededGlobalCheckWarnings(script))
                        result.Issues.Add(prefix + warn);

                    for (int vi = 0; vi < snapshot.Variants.Count; vi++)
                    {
                        if (_stopRequested) break;
                        var variant = snapshot.Variants[vi];
                        string who = $"{prefix}Variant '{variant.Label}'";
                        await RunVariantDialogueAsync(
                            client, snapshot, result, samples, script, d, offset, vi, who,
                            liveToolAgents, totalUnits, () => doneUnits, n => doneUnits = n);
                    }

                    if (!_stopRequested)
                        DialogueCompleted?.Invoke(result, d + 1);
                }

                if (TimedOut)
                {
                    result.Status = TryllDialogLabRunStatus.Failed;
                    result.Issues.Add($"Run stopped: {TimeoutMessage(TurnTimeoutSeconds)}.");
                }
                else if (_stopRequested)
                    result.Status = TryllDialogLabRunStatus.Stopped;
                else if (result.Issues.Count > 0 && result.Samples.All(s => s.Turns.All(t => t.Status == TryllLabCellStatus.Error)))
                    result.Status = TryllDialogLabRunStatus.Failed;
            }
            finally
            {
                client.Paused -= onPaused;
                if (_onConnectionChanged != null)
                    client.ConnectionChanged -= _onConnectionChanged;
                _onConnectionChanged = null;
                if (_currentAgent != null) { _currentAgent.Dispose(); _currentAgent = null; }
                _currentGraph = null;
                _currentStep = null;
                _liveToolAgent = null;
                _currentVariantUsesLiveTools = false;
                _turnTcs = null;
                IsRunning = false;
                result.CompletedAtUtc = DateTimeOffset.UtcNow;
            }

            return result;
        }

        /// <summary>
        /// One variant through one dialogue: a fresh agent (so nothing an earlier dialogue
        /// set leaks in), then every repeat on it, reseeded and rewound in between. Fills
        /// this dialogue's slice of each of the variant's columns.
        /// </summary>
        private async Task RunVariantDialogueAsync(
            TryllClient client,
            TryllDialogLabRunSnapshot snapshot,
            TryllDialogLabRunResult result,
            TryllLabSampleResult[,] samples,
            TryllDialogScriptData script,
            int d,
            int offset,
            int vi,
            string who,
            IReadOnlyList<TryllAgentComponent> liveToolAgents,
            int totalUnits,
            Func<int> getDone,
            Action<int> setDone)
        {
            var variant = snapshot.Variants[vi];
            int repeats = samples.GetLength(1);
            int generatedStepCount = script.GeneratedStepCount;

            void Advance(int units)
            {
                setDone(getDone() + units);
                Progress?.Invoke(getDone(), totalUnits);
            }

            void SkipVariant(string reason, TryllLabCellStatus status = TryllLabCellStatus.Error, int fromRepeat = 0)
            {
                for (int r = fromRepeat; r < repeats; r++)
                {
                    MarkDialogueTurns(samples[vi, r], script, d, offset, status, reason);
                    Advance(generatedStepCount);
                }
            }

            _liveToolAgent = liveToolAgents != null && vi < liveToolAgents.Count
                ? liveToolAgents[vi]
                : null;
            _currentVariantUsesLiveTools = variant.UseLiveTools;
            // Snapshot graph stays pristine. Bake overrides (and per-sample seeds)
            // onto clones so CreateAgent carries structural fields like tools —
            // ChangeParams rejects tools round-trips with ParamNotMutable (3006).
            if (variant.Graph == null || variant.Graph.Nodes == null || variant.Graph.Nodes.Count == 0)
            {
                result.Issues.Add($"{who}: no workflow graph.");
                result.Status = TryllDialogLabRunStatus.Failed;
                SkipVariant("No workflow graph");
                return;
            }

            // Defence in depth: the window refuses this combination before Run,
            // but an imported/synthesised snapshot could still carry it.
            if (!TryllDialogScriptValidation.TryValidateForAgent(
                    script, variant.MaintainDialogueHistory,
                    variant.AgentName, out var statelessError))
            {
                result.Issues.Add($"{who}: {statelessError}");
                result.Status = TryllDialogLabRunStatus.Failed;
                SkipVariant(statelessError);
                return;
            }

            var baseGraph = TryllDialogLabRunSnapshot.CloneGraph(variant.Graph);
            if (!TryBakeOverrides(baseGraph, variant, out var bakeError))
            {
                result.Issues.Add($"{who}: {bakeError}");
                SkipVariant(bakeError);
                return;
            }

            var workingGraph = TryllDialogLabRunSnapshot.CloneGraph(baseGraph);
            if (!TryBakeSeed(workingGraph, snapshot.DeriveSeed(vi, 0), out bakeError))
            {
                result.Issues.Add($"{who}: {bakeError}");
                SkipVariant(bakeError);
                return;
            }

            foreach (var warn in TryllDialogScriptValidation.CollectIntentCheckWarnings(script, baseGraph))
                result.Issues.Add($"{who}: {warn}");

            var (created, createError) = await CreateLabAgentAsync(
                client, workingGraph, variant.Variables, script,
                variant.MaintainDialogueHistory, variant.WorkflowAssetName, variant.Voice);
            TryllAgent agent = created;
            if (_stopRequested) { agent?.Dispose(); return; }
            if (!createError.IsOk)
            {
                string msg = $"CreateAgent failed — {createError.Message}";
                result.Issues.Add($"{who}: {msg}");
                SkipVariant(msg);
                return;
            }

            var samplingNodeNames = baseGraph.Nodes
                .Where(n => HasSamplingField(n.Params))
                .Select(n => n.Name)
                .ToList();

            try
            {
                for (int r = 0; r < repeats; r++)
                {
                    if (_stopRequested) return;
                    int seed = snapshot.DeriveSeed(vi, r);
                    var sample = samples[vi, r];
                    MarkDialogueTurns(sample, script, d, offset, TryllLabCellStatus.Skipped, null);

                    bool seedFailed = false;
                    string seedError = null;

                    if (r > 0)
                    {
                        // Prefer ChangeParams (keeps warm KV). ToolCall nodes can reject
                        // that with ParamNotMutable on structural `tools` when the wire
                        // round-trip is not byte-identical — fall back to a fresh agent
                        // with seed baked into the graph (same effective params, cold KV).
                        var changeErr = await TryChangeSeedAsync(agent, samplingNodeNames, seed);
                        if (_stopRequested) return;
                        if (!changeErr.IsOk)
                        {
                            if (changeErr.Code == (int)TryllErrorCode.ParamNotMutable)
                            {
                                agent.Dispose();
                                _currentAgent = null;
                                workingGraph = TryllDialogLabRunSnapshot.CloneGraph(baseGraph);
                                if (!TryBakeSeed(workingGraph, seed, out bakeError))
                                {
                                    seedFailed = true;
                                    seedError = bakeError;
                                }
                                else
                                {
                                    var (newAgent, recreateErr) = await CreateLabAgentAsync(
                                        client, workingGraph, variant.Variables, script,
                                        variant.MaintainDialogueHistory, variant.WorkflowAssetName, variant.Voice);
                                    if (_stopRequested)
                                    {
                                        newAgent?.Dispose();
                                        return;
                                    }
                                    if (!recreateErr.IsOk)
                                    {
                                        seedFailed = true;
                                        seedError = $"RecreateAgent failed — {recreateErr.Message}";
                                    }
                                    else
                                    {
                                        agent = newAgent;
                                    }
                                }
                            }
                            else
                            {
                                seedFailed = true;
                                seedError = changeErr.Message;
                            }
                        }
                    }

                    if (_stopRequested) return;

                    // One agent serves every repeat of a variant, so a live game-side
                    // handler that keeps per-conversation state must be told the dialogue
                    // restarted here - not at agent creation. Only when this variant
                    // opted into live tools: otherwise the scene handler is not consulted
                    // and resetting it would mutate game state the Lab is not using.
                    if (variant.UseLiveTools && _liveToolAgent != null)
                        _liveToolAgent.gameObject.SendMessage(
                            "ResetToolState", SendMessageOptions.DontRequireReceiver);

                    if (!seedFailed && script.SeedTurnCount > 0)
                    {
                        var seedInteractions = script.Steps.Take(script.SeedTurnCount)
                            .Select(s => new TryllDialogInteraction
                            {
                                UserMessage = s.User ?? "",
                                AssistantMessage = s.Assistant ?? "",
                            })
                            .ToList();
                        var (count, appendErr) = await AppendInteractionsAsync(agent, seedInteractions);
                        if (_stopRequested) return;
                        if (!appendErr.IsOk)
                        {
                            seedFailed = true;
                            seedError = $"AppendInteractions failed — {appendErr.Message}";
                        }
                        else if (count != (uint)script.SeedTurnCount)
                        {
                            seedFailed = true;
                            seedError =
                                $"AppendInteractions applied {count} of {script.SeedTurnCount} seeded turns";
                        }
                    }

                    if (seedFailed)
                    {
                        MarkDialogueTurns(sample, script, d, offset, TryllLabCellStatus.Error, seedError);
                        Advance(generatedStepCount);
                        // History may be partial — do not continue repeats on this agent.
                        result.Issues.Add($"{who} sample {sample.SampleId}: {seedError}");
                        SkipVariant("Prior seeding failed", TryllLabCellStatus.Skipped, r + 1);
                        return;
                    }

                    // Seeded cells from script + checks.
                    for (int ti = 0; ti < script.SeedTurnCount; ti++)
                    {
                        var step = script.Steps[ti];
                        var record = sample.Turns[offset + ti];
                        record.Seeded = true;
                        record.UserText = step.User ?? "";
                        record.AnswerText = step.Assistant ?? "";
                        record.Words = TryllDialogLabChecks.CountWords(record.AnswerText);
                        var (status, outcomes) = TryllDialogLabChecks.EvaluateStep(
                            script, step, record.AnswerText,
                            TryllJsonValue.Null, TryllDiagnosticsAbsenceReason.Seeded);
                        record.Status = status;
                        record.CheckOutcomes = outcomes;
                    }

                    bool columnFailed = false;
                    for (int ti = script.SeedTurnCount; ti < script.Steps.Count; ti++)
                    {
                        var step = script.Steps[ti];
                        var record = sample.Turns[offset + ti];
                        record.Seeded = false;
                        record.UserText = step.User ?? "";

                        if (_stopRequested || columnFailed)
                        {
                            record.Status = TryllLabCellStatus.Skipped;
                            if (_stopRequested && !columnFailed)
                                record.ErrorMessage = "Cancelled";
                            Advance(1);
                            continue;
                        }

                        await RunGeneratedStepAsync(agent, step, record);
                        if (record.Status == TryllLabCellStatus.Ok)
                        {
                            var (status, outcomes) = TryllDialogLabChecks.EvaluateStep(
                                script, step, record.AnswerText,
                                record.DebugInfo ?? TryllJsonValue.Null,
                                TryllDiagnosticsAbsenceReason.None,
                                variant.UseLiveTools,
                                _missingResumedTools);
                            record.Status = status;
                            record.CheckOutcomes = outcomes;
                        }
                        if (record.Status == TryllLabCellStatus.Error) columnFailed = true;

                        Advance(1);
                    }

                    // A stateless agent has nothing to rewind: the server cleared its
                    // dialogue at the end of every turn, so the next repeat already
                    // starts clean. The request would saturate to 0 removed rather
                    // than fail, but sending it would imply a rewind that never
                    // happened.
                    if (!variant.MaintainDialogueHistory) continue;
                    if (r == repeats - 1) continue; // the agent is disposed next anyway

                    var (_, removeErr) = await RemoveAllInteractionsAsync(agent);
                    if (_stopRequested) return;
                    if (!removeErr.IsOk)
                    {
                        result.Issues.Add(
                            $"{who} sample {sample.SampleId}: " +
                            $"RemoveInteractionsFromEnd failed — {removeErr.Message}");
                        // Remaining repeats would see contaminated history.
                        SkipVariant("Prior rewind failed", TryllLabCellStatus.Skipped, r + 1);
                        return;
                    }
                }
            }
            finally
            {
                agent?.Dispose();
                _currentAgent = null;
            }
        }

        private void OnAgentError(TryllError error)
        {
            if (_currentAgent != null && _currentAgent.HasPausedToolBatch)
                _currentAgent.Cancel(TryllCancelMode.StopAndDiscard);
            SettleTurn(TryllTurnStatus.Error, "", 0, error.Message ?? "Agent error");
        }

        private void OnToolHandlerError(TryllError error)
        {
            _turnFailure = error.Message ?? "Tool handler failed";
            if (_currentAgent != null && _currentAgent.IsValid)
                _currentAgent.Cancel(TryllCancelMode.StopAndDiscard);
            else
                SettleTurn(TryllTurnStatus.Error, "", 0, _turnFailure);
        }

        private void SettleTurn(TryllTurnStatus status, string debugInfo, int tokens, string failureMessage = null)
        {
            if (!string.IsNullOrEmpty(failureMessage))
                _turnFailure = failureMessage;
            _turnTcs?.TrySetResult((status, debugInfo ?? "", tokens));
        }

        private async Task RunGeneratedStepAsync(TryllAgent agent, TryllDialogStepData step, TryllLabTurnRecord record)
        {
            _currentStep = step;
            _turnFailure = null;
            _missingResumedTools.Clear();
            _answerBuffer = new System.Text.StringBuilder();
            _turnTcs = new TaskCompletionSource<(TryllTurnStatus, string, int)>();

            if (string.IsNullOrEmpty(step.User))
            {
                record.Status = TryllLabCellStatus.Error;
                record.ErrorMessage = "Empty user line";
                return;
            }

            var startTime = DateTime.UtcNow;
            agent.SendMessage(step.User);
            var turnTask = _turnTcs.Task;
            if (TurnTimeoutSeconds > 0)
            {
                var finished = await Task.WhenAny(turnTask, Task.Delay(TimeSpan.FromSeconds(TurnTimeoutSeconds)));
                if (finished != turnTask)
                {
                    TimedOut = true;
                    _stopRequested = true;
                    if (agent.IsValid) agent.Cancel(TryllCancelMode.StopAndDiscard);
                    SettleTurn(TryllTurnStatus.Error, "", 0, TimeoutMessage(TurnTimeoutSeconds));
                }
            }
            var (status, debugInfo, tokens) = await turnTask;
            var wallClockMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            record.AnswerText = _answerBuffer.ToString();
            record.Words = TryllDialogLabChecks.CountWords(record.AnswerText);
            record.TokensGenerated = tokens;
            record.DebugInfoRaw = debugInfo;
            record.DurationMs = wallClockMs;

            if (!string.IsNullOrEmpty(debugInfo) && TryllJson.TryParse(debugInfo, out var parsed, out _))
            {
                record.DebugInfo = parsed;
                var durS = parsed.Get("duration_s");
                if (!durS.IsNull) record.DurationMs = (int)(durS.AsDouble() * 1000);
                var ttft = parsed.Get("time_to_first_token_ms");
                if (!ttft.IsNull) record.TtftMs = (int)ttft.AsDouble();
            }

            if (_turnFailure != null)
            {
                record.Status = TryllLabCellStatus.Error;
                record.ErrorMessage = _turnFailure;
            }
            else if (status == TryllTurnStatus.Error)
            {
                record.Status = TryllLabCellStatus.Error;
                record.ErrorMessage = ExtractFirstError(record.DebugInfo) ?? "Turn failed";
            }
            else if (status == TryllTurnStatus.Cancelled)
            {
                record.Status = TryllLabCellStatus.Error;
                record.ErrorMessage = "Cancelled";
            }
            else
            {
                record.Status = TryllLabCellStatus.Ok;
            }
        }

        /// <summary>"Turn timed out after 120 s" — the same wording in both engines.</summary>
        internal static string TimeoutMessage(double seconds) =>
            $"Turn timed out after {seconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} s";

        private void HandleAnswerText(string nodeName, string text, bool isDelta, bool isFinal)
        {
            if (!string.IsNullOrEmpty(text)) _answerBuffer?.Append(text);
        }

        private void HandleTurnComplete(TryllTurnStatus status, string debugInfo, int tokensGenerated) =>
            SettleTurn(status, debugInfo, tokensGenerated);

        private static string ExtractFirstError(TryllJsonValue debugInfo)
        {
            var errors = debugInfo.Get("errors").AsArray();
            return errors.Count > 0 ? errors[0].AsString(errors[0].ToString()) : null;
        }

        private void RegisterCannedToolHandlers(
            TryllAgent agent, TryllGraphDescription graph, TryllDialogScriptData script)
        {
            var toolNames = script.Steps
                .SelectMany(s => (s.ToolResults ?? new List<TryllCannedToolResultData>()).Select(r => r.ToolName))
                .Concat(graph.Nodes.Select(n => n.Params).OfType<TryllToolCallParams>()
                    .SelectMany(p => p.Tools ?? Array.Empty<TryllToolDefinition>()).Select(t => t.Name))
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct();

            foreach (var toolName in toolNames)
            {
                string capturedName = toolName;
                agent.RegisterTool(capturedName, call =>
                {
                    var canned = (_currentStep?.ToolResults ?? new List<TryllCannedToolResultData>())
                        .FirstOrDefault(r => r.ToolName == capturedName);
                    bool hasCanned = canned != null && canned.ToolName != null;
                    Func<TryllToolCall, string> liveHandler = null;
                    bool hasLive = _liveToolAgent != null &&
                        _liveToolAgent.TryGetToolHandler(capturedName, out liveHandler);
                    var source = TryllDialogLabToolResolution.Resolve(
                        hasCanned, _currentVariantUsesLiveTools, hasLive);
                    if (source == TryllDialogLabToolSource.Canned)
                        return canned?.Result ?? "";
                    if (source == TryllDialogLabToolSource.Live)
                        return liveHandler != null ? liveHandler(call) ?? "" : "";
                    // Missing is a check, not a handler error: resume with an empty
                    // result so the turn completes and EvaluateStep can mark CheckFail.
                    _missingResumedTools.Add(capturedName);
                    return "";
                });
            }
        }

        private static bool HasSamplingField(TryllNodeParamsBase p) =>
            p?.GetType().GetField("Sampling", BindingFlags.Public | BindingFlags.Instance)?.FieldType
                == typeof(TryllSamplingOverrides);

        private static string VariantLetter(int index) => ((char)('A' + index)).ToString();

        private static TryllLabSampleResult NewSample(
            TryllLabVariantSnapshot variant, int vi, int r, int seed) =>
            new TryllLabSampleResult
            {
                SampleId = $"{VariantLetter(vi)}{r + 1}",
                VariantIndex = vi,
                VariantLabel = variant.Label,
                VariantSwatch = variant.Swatch,
                Repeat = r,
                Seed = seed,
            };

        /// <summary>Append one dialogue's steps to a column, every turn "not run" yet.</summary>
        private static void AppendDialogueTurns(
            TryllLabSampleResult sample, TryllDialogScriptData script, int dialogue, string reason)
        {
            for (int ti = 0; ti < script.Steps.Count; ti++)
            {
                var step = script.Steps[ti];
                sample.Turns.Add(new TryllLabTurnRecord
                {
                    DialogueIndex = dialogue,
                    TurnIndex = ti,
                    Seeded = ti < script.SeedTurnCount,
                    UserText = step.User ?? "",
                    AnswerText = ti < script.SeedTurnCount ? (step.Assistant ?? "") : "",
                    Status = TryllLabCellStatus.Skipped,
                    ErrorMessage = reason,
                });
            }
        }

        /// <summary>Reset one dialogue's slice of a column to <paramref name="status"/>.</summary>
        private static void MarkDialogueTurns(
            TryllLabSampleResult sample,
            TryllDialogScriptData script,
            int dialogue,
            int offset,
            TryllLabCellStatus status,
            string error)
        {
            for (int ti = 0; ti < script.Steps.Count; ti++)
            {
                var step = script.Steps[ti];
                bool seeded = ti < script.SeedTurnCount;
                sample.Turns[offset + ti] = new TryllLabTurnRecord
                {
                    DialogueIndex = dialogue,
                    TurnIndex = ti,
                    Seeded = seeded,
                    UserText = step.User ?? "",
                    AnswerText = seeded ? (step.Assistant ?? "") : "",
                    Status = status,
                    ErrorMessage = error,
                };
            }
        }

        /// <summary>
        /// Apply variant overrides onto a graph clone in memory. Avoids ChangeParams,
        /// which rejects structural fields like ToolCall.tools after construction.
        /// </summary>
        private static bool TryBakeOverrides(
            TryllGraphDescription graph, TryllLabVariantSnapshot variant, out string error)
        {
            error = null;

            // Per-variant model. On the wire this is CreateAgentRequest.default_model_name,
            // a sibling of `graph` — the client just carries it on the graph description —
            // so baking it here changes only what this run's CreateAgent asks for. Nodes
            // that pin their own ModelName are untouched by design; the Variants tab warns
            // when the graph has any.
            if (!string.IsNullOrEmpty(variant.ModelOverride))
                graph.DefaultModelName = variant.ModelOverride;

            if (variant.Overrides == null || variant.Overrides.Count == 0) return true;

            var byName = graph.Nodes
                .Where(n => n?.Params != null)
                .ToDictionary(n => n.Name, n => n.Params);

            foreach (var group in variant.Overrides.GroupBy(o => o.NodeName))
            {
                if (!byName.TryGetValue(group.Key, out var target))
                {
                    error = $"override targets node '{group.Key}', which is not in {variant.AgentName}'s workflow";
                    return false;
                }
                if (!TryllNodeFieldReflection.TryApply(
                        target,
                        group.Select(o => new TryllParamOverride
                        {
                            NodeName = o.NodeName,
                            FieldPath = o.FieldPath,
                            Value = o.Value,
                        }),
                        out var applyMessage))
                {
                    error = $"node '{group.Key}': {applyMessage}";
                    return false;
                }
                // An override on a Localized prompt is the text to test: it wins
                // over the Text ID, so the field is unticked in this run's copy.
                foreach (var o in group)
                    TryllTextResolver.UntickAt(target, o.FieldPath);
            }
            return true;
        }

        private static bool TryBakeSeed(TryllGraphDescription graph, int seed, out string error)
        {
            error = null;
            foreach (var node in graph.Nodes)
            {
                if (node?.Params == null || !HasSamplingField(node.Params)) continue;
                if (!TryllNodeFieldReflection.TryApply(
                        node.Params,
                        new[]
                        {
                            new TryllParamOverride
                            {
                                NodeName = node.Name,
                                FieldPath = "Sampling.Seed",
                                Value = seed.ToString(),
                            },
                        },
                        out var applyMessage))
                {
                    error = $"node '{node.Name}': {applyMessage}";
                    return false;
                }
            }
            return true;
        }

        /// <param name="maintainDialogueHistory">
        /// Taken from the variant's agent component, never assumed. Hardcoding <c>true</c>
        /// here used to make the Lab run every stateless agent as a stateful one.
        /// </param>
        private async Task<(TryllAgent agent, TryllError error)> CreateLabAgentAsync(
            TryllClient client,
            TryllGraphDescription graph,
            List<TryllVariableDecl> variables,
            TryllDialogScriptData script,
            bool maintainDialogueHistory,
            string assetName,
            TryllVoiceProfileData voice)
        {
            _currentGraph = graph;
            System.Threading.Tasks.Task<(TryllAgent, TryllError)> createTask;
            using (client.AsEditorCall())
            {
                createTask = client.RequestCreateAgentAsync(
                    graph, enableDiagnostics: true, maintainDialogueHistory, variables,
                    textTable: graph?.TextTable, assetName: assetName, voice: voice);
            }
            var (agent, createError) = await createTask;
            if (!createError.IsOk) return (null, createError);

            _currentAgent = agent;
            agent.SetOnAnswerText(HandleAnswerText);
            agent.SetOnTurnComplete(HandleTurnComplete);
            agent.SetOnError(OnAgentError);
            agent.ToolHandlerError += OnToolHandlerError;
            RegisterCannedToolHandlers(agent, graph, script);
            return (agent, TryllError.Ok());
        }

        private static async Task<TryllError> TryChangeSeedAsync(
            TryllAgent agent, List<string> samplingNodeNames, int seed)
        {
            foreach (var nodeName in samplingNodeNames)
            {
                var baseline = agent.GetNodeParamsBaseline(nodeName);
                if (baseline == null)
                    return new TryllError(TryllErrorCode.UnknownNode, $"no baseline for '{nodeName}'");

                if (!TryllNodeFieldReflection.TryApply(
                        baseline,
                        new[]
                        {
                            new TryllParamOverride
                            {
                                NodeName = nodeName,
                                FieldPath = "Sampling.Seed",
                                Value = seed.ToString(),
                            },
                        },
                        out var applyMessage))
                {
                    return new TryllError(TryllErrorCode.InvalidParamValue, applyMessage);
                }

                var err = await ChangeParamsAsync(agent, nodeName, baseline);
                if (!err.IsOk) return err;
            }
            return TryllError.Ok();
        }

        private static Task<TryllError> ChangeParamsAsync(TryllAgent agent, string nodeName, TryllNodeParamsBase p)
        {
            var tcs = new TaskCompletionSource<TryllError>();
            agent.ChangeParams(nodeName, p, err => tcs.TrySetResult(err));
            return tcs.Task;
        }

        private static Task<(uint count, TryllError error)> AppendInteractionsAsync(
            TryllAgent agent, List<TryllDialogInteraction> interactions)
        {
            var tcs = new TaskCompletionSource<(uint, TryllError)>();
            agent.AppendInteractions(interactions, (count, err) => tcs.TrySetResult((count, err)));
            return tcs.Task;
        }

        private static Task<(uint count, TryllError error)> RemoveAllInteractionsAsync(TryllAgent agent)
        {
            var tcs = new TaskCompletionSource<(uint, TryllError)>();
            agent.RemoveInteractionsFromEnd(int.MaxValue, (count, err) => tcs.TrySetResult((count, err)));
            return tcs.Task;
        }
    }
}
