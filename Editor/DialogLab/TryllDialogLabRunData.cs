// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Tryll.Client.Editor
{
    internal enum TryllLabCellStatus { Ok, CheckFail, Error, Skipped }

    internal enum TryllDialogLabRunStatus { Completed, Stopped, Failed }

    /// <summary>Pure script projection — no ScriptableObject references.</summary>
    internal sealed class TryllDialogScriptData
    {
        public string Name = "";
        public string Description = "";
        public int SeedTurnCount;

        /// <summary>
        /// Checks applied to every <b>generated</b> turn, on top of each step's own
        /// <see cref="TryllDialogStepData.Checks"/>. Seeded turns skip them.
        /// </summary>
        public List<TryllDialogCheck> GlobalChecks = new List<TryllDialogCheck>();

        public List<TryllDialogStepData> Steps = new List<TryllDialogStepData>();

        public int GeneratedStepCount => Mathf.Max(0, Steps.Count - SeedTurnCount);

        public static TryllDialogScriptData FromAsset(TryllDialogScriptAsset asset)
        {
            if (asset == null) return null;
            var data = new TryllDialogScriptData
            {
                Name = asset.name ?? "",
                Description = asset.Description ?? "",
                SeedTurnCount = asset.SeedTurnCount,
                GlobalChecks = CloneChecks(asset.GlobalChecks),
            };
            foreach (var step in asset.Steps ?? new List<TryllDialogScriptAsset.Step>())
                data.Steps.Add(TryllDialogStepData.FromAssetStep(step));
            return data;
        }

        public TryllDialogScriptData Clone()
        {
            var c = new TryllDialogScriptData
            {
                Name = Name,
                Description = Description,
                SeedTurnCount = SeedTurnCount,
                GlobalChecks = CloneChecks(GlobalChecks),
            };
            foreach (var s in Steps) c.Steps.Add(s.Clone());
            return c;
        }

        /// <summary>Deep copy that drops null slots (an inspector list element with no
        /// type picked yet), so nothing downstream has to null-check a check.</summary>
        internal static List<TryllDialogCheck> CloneChecks(IEnumerable<TryllDialogCheck> checks) =>
            (checks ?? Enumerable.Empty<TryllDialogCheck>())
                .Where(c => c != null)
                .Select(c => c.Clone())
                .ToList();
    }

    internal sealed class TryllDialogStepData
    {
        public string User = "";
        public string Assistant = "";
        public List<TryllDialogCheck> Checks = new List<TryllDialogCheck>();
        public List<TryllCannedToolResultData> ToolResults = new List<TryllCannedToolResultData>();

        public static TryllDialogStepData FromAssetStep(TryllDialogScriptAsset.Step step)
        {
            if (step == null) return new TryllDialogStepData();
            return new TryllDialogStepData
            {
                User = step.User ?? "",
                Assistant = step.Assistant ?? "",
                Checks = TryllDialogScriptData.CloneChecks(step.Checks),
                ToolResults = (step.ToolResults ?? new List<TryllDialogScriptAsset.CannedToolResult>())
                    .Select(t => new TryllCannedToolResultData { ToolName = t.ToolName ?? "", Result = t.Result ?? "" })
                    .ToList(),
            };
        }

        public TryllDialogStepData Clone() =>
            new TryllDialogStepData
            {
                User = User,
                Assistant = Assistant,
                Checks = TryllDialogScriptData.CloneChecks(Checks),
                ToolResults = ToolResults.Select(t => new TryllCannedToolResultData
                {
                    ToolName = t.ToolName,
                    Result = t.Result,
                }).ToList(),
            };
    }

    internal sealed class TryllCannedToolResultData
    {
        public string ToolName = "";
        public string Result = "";
    }

    internal sealed class TryllParamOverrideData
    {
        public string NodeName = "";
        public string FieldPath = "";
        public string Value = "";

        public TryllParamOverrideData Clone() =>
            new TryllParamOverrideData { NodeName = NodeName, FieldPath = FieldPath, Value = Value };

        public static TryllParamOverrideData From(TryllParamOverride o) =>
            new TryllParamOverrideData
            {
                NodeName = o?.NodeName ?? "",
                FieldPath = o?.FieldPath ?? "",
                Value = o?.Value ?? "",
            };
    }

    internal sealed class TryllLabNodeSummary
    {
        public string Name = "";
        public string Type = "";
    }

    /// <summary>Execution + provenance for one variant, captured before connect.</summary>
    internal sealed class TryllLabVariantSnapshot
    {
        public int Index;
        public string Label = "";
        public Color Swatch;
        public string AgentName = "";
        public bool AgentIsOverride;
        public string WorkflowAssetName = "";
        public string GraphStartNode = "";
        public string DefaultModelName = "";
        public List<string> ModelNames = new List<string>();
        public List<TryllLabNodeSummary> Nodes = new List<TryllLabNodeSummary>();
        public List<TryllParamOverrideData> Overrides = new List<TryllParamOverrideData>();

        /// <summary>
        /// Mirrors the agent component's <see cref="TryllAgentComponent.MaintainDialogueHistory"/>.
        /// Provenance *and* execution input: the Lab used to hardcode <c>true</c> at
        /// CreateAgent, which silently ran a stateless agent — one whose dialogue the
        /// server clears after every turn — as a stateful one, so the Lab was not
        /// testing the configuration that ships.
        /// </summary>
        public bool MaintainDialogueHistory = true;

        /// <summary>
        /// The variant's per-variant model, or empty when it runs the workflow's own
        /// <c>DefaultModelName</c>. <see cref="DefaultModelName"/> already holds the
        /// *effective* value; this records whether that value was overridden, which is
        /// what a reader of the export needs to interpret a model comparison.
        /// </summary>
        public string ModelOverride = "";

        /// <summary>
        /// When false (default), an uncanned tool call fails the cell. When true, the
        /// runner may invoke the variant's scene-agent handler. Provenance *and*
        /// execution input — the scene object itself stays out of this snapshot.
        /// </summary>
        public bool UseLiveTools;

        // Execution-only — not serialized to JSONL.
        public TryllGraphDescription Graph;
        public List<TryllVariableDecl> Variables;
        /// <summary>The agent component's voice (protocol v27), or null. A speaking graph needs it.</summary>
        public TryllVoiceProfileData Voice;
    }

    /// <summary>Immutable run input captured at Run, before connection/model awaits.</summary>
    internal sealed class TryllDialogLabRunSnapshot
    {
        public string RunId = "";
        public DateTimeOffset CapturedAtUtc;
        /// <summary>
        /// The dialogues this run covers, in order — one for a Dialog Script, several for
        /// a Dialog Suite. Every sample (one variant x one seed column) runs all of them, and
        /// its <see cref="TryllLabSampleResult.Turns"/> hold their steps back to back.
        /// </summary>
        public List<TryllDialogScriptData> Dialogues = new List<TryllDialogScriptData>();
        public int Repeats = 1;
        public int BaseSeed = 1;
        public string DefaultAgentName = "";
        public string SettingsAssetName = "";
        public List<TryllLabVariantSnapshot> Variants = new List<TryllLabVariantSnapshot>();

        public int DeriveSeed(int variantIndex, int repeat) => BaseSeed + variantIndex * 1000 + repeat;

        public int PlannedCells => Variants.Count * Mathf.Max(1, Repeats);
        public int GeneratedStepsPerSample => Dialogues.Sum(d => d.GeneratedStepCount);
        public int PlannedGeneratedTurns => PlannedCells * GeneratedStepsPerSample;
        public int TotalSteps => Dialogues.Sum(d => d.Steps.Count);

        /// <summary>Index in a sample's flat <c>Turns</c> of dialogue <paramref name="dialogue"/>'s first step.</summary>
        public int TurnOffset(int dialogue)
        {
            int offset = 0;
            for (int d = 0; d < dialogue && d < Dialogues.Count; d++) offset += Dialogues[d].Steps.Count;
            return offset;
        }

        public static TryllDialogLabRunSnapshot Capture(
            TryllRuntimeSettings settings,
            TryllAgentComponent defaultAgent,
            IReadOnlyList<TryllDialogScriptAsset> scriptAssets,
            TryllDialogLabExperiment experiment,
            Func<TryllDialogLabVariant, TryllAgentComponent> effectiveAgent)
        {
            var snap = new TryllDialogLabRunSnapshot
            {
                RunId = Guid.NewGuid().ToString("N"),
                CapturedAtUtc = DateTimeOffset.UtcNow,
                Dialogues = (scriptAssets ?? Array.Empty<TryllDialogScriptAsset>())
                    .Select(TryllDialogScriptData.FromAsset)
                    .Where(d => d != null)
                    .ToList(),
                Repeats = Mathf.Max(1, experiment.Repeats),
                BaseSeed = Mathf.Max(1, experiment.BaseSeed),
                DefaultAgentName = defaultAgent != null ? defaultAgent.name : "",
                SettingsAssetName = settings != null ? settings.name : "",
            };

            for (int i = 0; i < experiment.Variants.Count; i++)
            {
                var v = experiment.Variants[i];
                var agent = effectiveAgent(v);
                snap.Variants.Add(BuildVariant(i, v,
                    agentName: agent != null ? agent.name : "",
                    workflow: agent != null ? agent.WorkflowAsset : null,
                    variables: VariablesOf(agent),
                    voice: agent?.ResolveCreateVoice()?.Clone(),
                    // No agent is a blocked run (the window refuses before Capture),
                    // so the fallback only matters to defensive callers.
                    maintainDialogueHistory: agent == null || agent.MaintainDialogueHistory));
            }
            return snap;
        }

        /// <summary>
        /// Headless capture: one variant built from a workflow asset instead of a scene
        /// component, through the same per-variant path as <see cref="Capture"/>. The voice
        /// is the workflow default.
        /// </summary>
        public static TryllDialogLabRunSnapshot CaptureFromSpec(
            TryllRuntimeSettings settings,
            TryllLabAgentSpec agent,
            IReadOnlyList<TryllDialogScriptAsset> scriptAssets,
            int seeds,
            int baseSeed)
        {
            var snap = new TryllDialogLabRunSnapshot
            {
                RunId = Guid.NewGuid().ToString("N"),
                CapturedAtUtc = DateTimeOffset.UtcNow,
                Dialogues = (scriptAssets ?? Array.Empty<TryllDialogScriptAsset>())
                    .Select(TryllDialogScriptData.FromAsset)
                    .Where(d => d != null)
                    .ToList(),
                Repeats = Mathf.Max(1, seeds),
                BaseSeed = Mathf.Max(1, baseSeed),
                DefaultAgentName = agent.Name ?? "",
                SettingsAssetName = settings != null ? settings.name : "",
            };
            var variant = new TryllDialogLabVariant { Label = agent.Name ?? "", ModelOverride = agent.Model ?? "" };
            var declared = agent.Workflow != null ? agent.Workflow.Variables : new List<TryllVariableDecl>();
            snap.Variants.Add(BuildVariant(0, variant,
                agentName: agent.Name ?? "",
                workflow: agent.Workflow,
                variables: TryllAgentComponent.MergeVariableOverrides(declared, agent.VariableOverrides),
                voice: null,
                maintainDialogueHistory: agent.MaintainDialogueHistory));
            return snap;
        }

        private static TryllLabVariantSnapshot BuildVariant(
            int i, TryllDialogLabVariant v, string agentName, TryllWorkflowAsset workflow,
            List<TryllVariableDecl> variables, TryllVoiceProfileData voice, bool maintainDialogueHistory)
        {
            var graph = workflow != null ? workflow.Graph : null;
            string modelOverride = (v.ModelOverride ?? "").Trim();
            var variant = new TryllLabVariantSnapshot
            {
                Index = i,
                Label = v.Label ?? $"Variant {i + 1}",
                // By position, not the stored field: swatches are not user-editable,
                // and older prefs hold the same accent colour for every variant.
                Swatch = TryllEditorTheme.VariantSwatch(i),
                AgentName = agentName,
                AgentIsOverride = v.AgentOverride != null,
                WorkflowAssetName = workflow != null ? workflow.name : "",
                GraphStartNode = graph?.StartNode ?? "",
                // The EFFECTIVE default, so provenance and FillNodeProvenance's model
                // list describe the run rather than the asset it came from.
                DefaultModelName = modelOverride.Length > 0
                    ? modelOverride
                    : (graph?.DefaultModelName ?? ""),
                ModelOverride = modelOverride,
                Graph = CloneGraph(graph),
                Variables = CloneVariables(variables),
                Voice = voice,
                MaintainDialogueHistory = maintainDialogueHistory,
                UseLiveTools = v.UseLiveTools,
            };
            foreach (var o in v.Overrides ?? new List<TryllParamOverride>())
                variant.Overrides.Add(TryllParamOverrideData.From(o));
            FillNodeProvenance(variant, graph);
            return variant;
        }

        private static void FillNodeProvenance(TryllLabVariantSnapshot variant, TryllGraphDescription graph)
        {
            var models = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(variant.DefaultModelName))
                models.Add(variant.DefaultModelName);
            if (graph?.Nodes == null) return;
            foreach (var node in graph.Nodes)
            {
                if (node == null) continue;
                string typeName = node.Params != null ? node.Params.GetType().Name : "";
                variant.Nodes.Add(new TryllLabNodeSummary { Name = node.Name ?? "", Type = typeName });
                if (node.Params == null) continue;
                var modelField = node.Params.GetType().GetField("ModelName", BindingFlags.Public | BindingFlags.Instance);
                if (modelField?.FieldType == typeof(string))
                {
                    string name = modelField.GetValue(node.Params) as string;
                    if (!string.IsNullOrEmpty(name)) models.Add(name);
                }
            }
            variant.ModelNames = models.OrderBy(m => m, StringComparer.Ordinal).ToList();
        }

        private static TryllGraphDescription GraphOf(TryllAgentComponent a) =>
            a == null ? null : a.WorkflowAsset != null ? a.WorkflowAsset.Graph : null;

        private static List<TryllVariableDecl> VariablesOf(TryllAgentComponent a)
        {
            if (a == null) return new List<TryllVariableDecl>();
            var declared = a.WorkflowAsset != null ? a.WorkflowAsset.Variables : new List<TryllVariableDecl>();
            return TryllAgentComponent.MergeVariableOverrides(declared, a.VariableOverrides);
        }

        /// <summary>Deep-clone a graph (params included) for CreateAgent baking.</summary>
        internal static TryllGraphDescription CloneGraph(TryllGraphDescription graph)
        {
            if (graph == null) return null;
            var clone = new TryllGraphDescription
            {
                StartNode = graph.StartNode,
                DefaultModelName = graph.DefaultModelName,
                Nodes = new List<TryllNodeDescription>(),
            };
            if (graph.Nodes == null) return clone;
            foreach (var n in graph.Nodes)
            {
                if (n == null) continue;
                clone.Nodes.Add(new TryllNodeDescription
                {
                    Name = n.Name,
                    Params = n.Params?.Clone(),
                });
            }
            return clone;
        }

        private static List<TryllVariableDecl> CloneVariables(List<TryllVariableDecl> vars)
        {
            if (vars == null) return new List<TryllVariableDecl>();
            // Variable decls are value-like; MergeVariableOverrides already returns a fresh list.
            return new List<TryllVariableDecl>(vars);
        }
    }

    /// <summary>
    /// A headless run's agent: what <see cref="TryllDialogLabRunSnapshot.Capture"/> reads off a
    /// scene component, given directly. Variable overrides follow the component's rules
    /// (<see cref="TryllAgentComponent.MergeVariableOverrides"/>).
    /// </summary>
    internal sealed class TryllLabAgentSpec
    {
        public TryllWorkflowAsset Workflow;
        public string Name = "";
        public List<TryllVariableOverride> VariableOverrides = new List<TryllVariableOverride>();
        public bool MaintainDialogueHistory = true;
        /// <summary>Model override; empty = the workflow default.</summary>
        public string Model = "";
    }

    internal sealed class TryllLabTurnRecord
    {
        /// <summary>Which of the snapshot's dialogues this turn belongs to.</summary>
        public int DialogueIndex;
        /// <summary>Step index within that dialogue.</summary>
        public int TurnIndex;
        public bool Seeded;
        public string UserText = "";
        public string AnswerText = "";
        public int Words;
        public TryllLabCellStatus Status;

        /// <summary>
        /// Every evaluated check on this turn, passes included — statistics need the
        /// denominator, not only the failures. Empty for turns that never ran.
        /// </summary>
        public List<TryllLabCheckOutcome> CheckOutcomes = new List<TryllLabCheckOutcome>();

        public List<TryllLabCheckOutcome> FailedChecks => CheckOutcomes.Where(o => !o.Passed).ToList();
        public string ErrorMessage;
        public int DurationMs;
        public int TtftMs;
        public int TokensGenerated;
        public string DebugInfoRaw;
        public TryllJsonValue DebugInfo = TryllJsonValue.Null;
    }

    internal sealed class TryllLabSampleResult
    {
        public string SampleId = "";
        public int VariantIndex;
        public string VariantLabel = "";
        public Color VariantSwatch;
        public int Repeat;
        public int Seed;
        public List<TryllLabTurnRecord> Turns = new List<TryllLabTurnRecord>();
    }

    /// <summary>Immutable completed (or imported) run — the sole source for dataset/export.</summary>
    internal sealed class TryllDialogLabRunResult
    {
        public TryllDialogLabRunSnapshot Snapshot;
        public TryllDialogLabExport.ServerIdentity Server;
        public DateTimeOffset StartedAtUtc;
        public DateTimeOffset CompletedAtUtc;
        public TryllDialogLabRunStatus Status = TryllDialogLabRunStatus.Completed;
        public List<string> Issues = new List<string>();
        public List<TryllLabSampleResult> Samples = new List<TryllLabSampleResult>();
    }
}
