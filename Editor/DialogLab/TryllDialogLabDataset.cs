// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tryll.Client.Editor
{
    internal enum LabCellStatus { Ok, CheckFail, Error, Skipped }

    internal sealed class LabVariant
    {
        public string Id;
        public string Label;
        public Color  Color;
        public string DiffLabel;
        public string AgentName;
        public bool   Mutable;
    }

    internal sealed class LabSample
    {
        public string Id;
        public string VariantId;
        public int    Seed;
        public Color  Color;
        public bool   FirstOfVariant;
    }

    /// <summary>One authored check row; matched to outcomes by <see cref="Scope"/> +
    /// <see cref="Index"/> (<see cref="TryllDialogLabChecks.Matches"/>).</summary>
    internal sealed class LabCheck
    {
        public string Label;
        public string Kind;
        public string Arg;
        public TryllLabCheckScope Scope;
        public int Index;
    }

    /// <summary>One step row. <c>Turns</c> is flat across dialogues; these say where it came from.</summary>
    internal sealed class LabTurn
    {
        public string     User;
        public bool       Seeded;
        public LabCheck[] Checks;
        public int        DialogueIndex;
        /// <summary>Step index within its dialogue (what the UI numbers from).</summary>
        public int        StepIndex;
    }

    /// <summary>One dialogue's contiguous slice of <see cref="TryllDialogLabDataset.Turns"/>.</summary>
    internal sealed class LabDialogue
    {
        public string Name;
        public int    FirstTurn;
        public int    TurnCount;
        public int    SeededTurns;
    }

    internal sealed class LabRouteNode
    {
        public string Name;
        public string Type;
        public string Exit;
        public int    Ms;
    }

    internal sealed class LabCell
    {
        public string          Key;
        public string          SampleId;
        /// <summary>Index into the flat <see cref="TryllDialogLabDataset.Turns"/>; also the cell key's turn part.</summary>
        public int             TurnIndex;
        public int             DialogueIndex;
        public int             StepIndex;
        public bool            Seeded;
        public string          Text;
        public int             Words;
        public LabCellStatus   Status;
        public LabRouteNode[]  Route;
        public int             Ms;
        public int             Ttft;
        public List<TryllLabCheckOutcome> Failed = new List<TryllLabCheckOutcome>();
        /// <summary>Every evaluated check, passes included.</summary>
        public List<TryllLabCheckOutcome> Outcomes = new List<TryllLabCheckOutcome>();
        public TryllJsonValue RawDebugInfo = TryllJsonValue.Null;
    }

    /// <summary>
    /// The Grid/Summary renderer's data source: a completed or imported run reshaped
    /// into the flat per-cell form the views iterate.
    /// </summary>
    internal sealed class TryllDialogLabDataset
    {
        public LabVariant[] Variants;
        public LabSample[] Samples;
        /// <summary>Every shown dialogue's steps, back to back.</summary>
        public LabTurn[] Turns;
        public LabDialogue[] Dialogues;
        /// <summary>How many dialogues the run has in total; more than <see cref="Dialogues"/>
        /// while a run is still publishing dialogue by dialogue.</summary>
        public int TotalDialogues;
        public int Repeats;
        public string AgentName;
        public bool Imported;

        public static string CellKey(string sampleId, int turnIndex) => $"{sampleId}:{turnIndex}";

        private System.Func<Dictionary<string, LabCell>> _buildCells;

        public Dictionary<string, LabCell> BuildCells() => _buildCells();

        public LabCellStatus StatusOf(LabCell c)
        {
            if (c.Status == LabCellStatus.Error || c.Status == LabCellStatus.Skipped) return c.Status;
            return c.Failed.Count > 0 ? LabCellStatus.CheckFail : LabCellStatus.Ok;
        }

        /// <param name="visibleDialogues">Show only the first N dialogues (a run still in
        /// progress); null shows all of them.</param>
        public static TryllDialogLabDataset FromRunResult(
            TryllDialogLabRunResult run, bool imported = false, int? visibleDialogues = null)
        {
            var snapshot = run.Snapshot;
            int shown = Mathf.Clamp(visibleDialogues ?? snapshot.Dialogues.Count, 0, snapshot.Dialogues.Count);
            var variants = new LabVariant[snapshot.Variants.Count];
            for (int i = 0; i < snapshot.Variants.Count; i++)
            {
                var v = snapshot.Variants[i];
                var parts = new List<string>();
                if (v.AgentIsOverride) parts.Add("agent " + v.AgentName);
                // The headline difference when variants compare models — put it first
                // among the parameter-ish parts, not buried after the overrides.
                if (!string.IsNullOrEmpty(v.ModelOverride)) parts.Add("model " + v.ModelOverride);
                // Worth a chip: it changes what every turn after the first one sees,
                // and it is invisible in the graph and the overrides.
                if (!v.MaintainDialogueHistory) parts.Add("stateless");
                if (v.UseLiveTools) parts.Add("live tools");
                if (v.Overrides.Count > 0)
                    parts.AddRange(v.Overrides.Select(o => $"{o.NodeName}.{o.FieldPath}"));

                variants[i] = new LabVariant
                {
                    Id = VariantId(i),
                    Label = v.Label,
                    Color = v.Swatch,
                    DiffLabel = parts.Count > 0 ? string.Join(", ", parts) : "baseline",
                    AgentName = v.AgentName,
                    Mutable = true,
                };
            }

            var turns = new List<LabTurn>();
            var dialogues = new LabDialogue[shown];
            for (int d = 0; d < shown; d++)
            {
                var script = snapshot.Dialogues[d];
                dialogues[d] = new LabDialogue
                {
                    Name = script.Name,
                    FirstTurn = turns.Count,
                    TurnCount = script.Steps.Count,
                    SeededTurns = script.SeedTurnCount,
                };
                for (int i = 0; i < script.Steps.Count; i++)
                {
                    var step = script.Steps[i];
                    bool seeded = i < script.SeedTurnCount;
                    turns.Add(new LabTurn
                    {
                        User = step.User,
                        Seeded = seeded,
                        Checks = TryllDialogLabChecks.AuthoredChecks(script, step, seeded).ToArray(),
                        DialogueIndex = d,
                        StepIndex = i,
                    });
                }
            }
            int shownTurns = turns.Count;

            var samples = run.Samples.Select(r => new LabSample
            {
                Id = r.SampleId,
                VariantId = VariantId(r.VariantIndex),
                Seed = r.Seed,
                Color = variants.Length > r.VariantIndex ? variants[r.VariantIndex].Color : Color.white,
                FirstOfVariant = r.Repeat == 0,
            }).ToArray();

            Dictionary<string, LabCell> BuildCells()
            {
                var cells = new Dictionary<string, LabCell>();
                foreach (var sample in run.Samples)
                {
                    // A sample's records are flat in dialogue order, like Turns.
                    for (int g = 0; g < sample.Turns.Count && g < shownTurns; g++)
                    {
                        var record = sample.Turns[g];
                        var key = CellKey(sample.SampleId, g);
                        cells[key] = new LabCell
                        {
                            Key = key,
                            SampleId = sample.SampleId,
                            TurnIndex = g,
                            DialogueIndex = record.DialogueIndex,
                            StepIndex = record.TurnIndex,
                            Seeded = record.Seeded,
                            Text = record.Status == TryllLabCellStatus.Error
                                ? (record.ErrorMessage ?? "Error")
                                : record.AnswerText,
                            Words = record.Words,
                            Status = MapStatus(record.Status),
                            Route = ExtractRoute(record.DebugInfo),
                            Ms = record.DurationMs,
                            Ttft = record.TtftMs,
                            Failed = record.FailedChecks,
                            Outcomes = new List<TryllLabCheckOutcome>(record.CheckOutcomes ?? new List<TryllLabCheckOutcome>()),
                            RawDebugInfo = record.DebugInfo ?? TryllJsonValue.Null,
                        };
                    }
                }
                return cells;
            }

            return new TryllDialogLabDataset
            {
                Variants = variants,
                Samples = samples,
                Turns = turns.ToArray(),
                Dialogues = dialogues,
                TotalDialogues = snapshot.Dialogues.Count,
                Repeats = snapshot.Repeats,
                AgentName = snapshot.DefaultAgentName,
                Imported = imported,
                _buildCells = BuildCells,
            };
        }

        private static string VariantId(int index) => ((char)('A' + index)).ToString();

        private static LabCellStatus MapStatus(TryllLabCellStatus s) => s switch
        {
            TryllLabCellStatus.Ok => LabCellStatus.Ok,
            TryllLabCellStatus.CheckFail => LabCellStatus.CheckFail,
            TryllLabCellStatus.Error => LabCellStatus.Error,
            _ => LabCellStatus.Skipped,
        };

        private static LabRouteNode[] ExtractRoute(TryllJsonValue debugInfo)
        {
            if (debugInfo == null || debugInfo.IsNull) return System.Array.Empty<LabRouteNode>();
            var nodes = debugInfo.Get("nodes").AsArray();
            var route = new LabRouteNode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                route[i] = new LabRouteNode
                {
                    Name = n.Get("name").AsString(""),
                    Type = n.Get("_type").AsString(""),
                    Exit = n.Get("exit_route").AsString(""),
                    Ms = (int)(n.Get("duration_s").AsDouble() * 1000),
                };
            }
            return route;
        }
    }
}
