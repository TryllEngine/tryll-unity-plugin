// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;

namespace Tryll.Client.Editor
{
    /// <summary>Terminal outcome shared by Chat and Dialog Lab without either status model leaking across.</summary>
    internal enum TryllTurnTerminalStatus
    {
        Success,
        Cancelled,
        Error,
        Skipped,
    }

    /// <summary>
    /// Why a turn carries no usable schema-v2 diagnostics. Explicit so chat and Lab can
    /// explain absence without inferring it from Dialog Lab dataset state (R4.6).
    /// </summary>
    internal enum TryllDiagnosticsAbsenceReason
    {
        None,
        /// <summary>Injected with AppendInteractions — the graph never ran.</summary>
        Seeded,
        /// <summary>Earlier failure skipped this turn.</summary>
        Skipped,
        /// <summary>Agent was created without enable_diagnostics, or payload is empty.</summary>
        Unavailable,
        /// <summary>Payload present but schema_version is missing or not 2.</summary>
        UnsupportedSchema,
        /// <summary>Diagnostics expected but empty/malformed in a non-schema way.</summary>
        Empty,
        /// <summary>Agent existed before Agent Log/diagnostics demand began capturing.</summary>
        CreatedBeforeDiagnosticsCapture,
    }

    /// <summary>One node on the turn's route breadcrumb.</summary>
    internal sealed class TryllTurnRouteNode
    {
        public string Name;
        public string Type;
        public string Exit;
        public int    Ms;
    }

    /// <summary>
    /// Neutral per-turn input for the shared diagnostics inspector. Usable by Dialog Lab
    /// and Chat without either importing the other's dataset types.
    /// </summary>
    internal sealed class TryllTurnInspection
    {
        /// <summary>Local stable key (Lab cell key, or chat turn id). Used for node-scope reset.</summary>
        public string TurnKey;

        /// <summary>Hint under the title, e.g. "A1 · turn 3 · seed 42" or "turn 2".</summary>
        public string Hint;

        /// <summary>Route section trailing when unscoped — variant label or empty.</summary>
        public string RouteTrailing;

        public string AnswerText;
        public TryllTurnTerminalStatus Status = TryllTurnTerminalStatus.Success;
        public string ErrorText;
        public TryllTurnRouteNode[] Route = System.Array.Empty<TryllTurnRouteNode>();
        public int DurationMs;
        public int TtftMs;

        /// <summary>Raw schema-v2 debug_info, or Null when absent.</summary>
        public TryllJsonValue DebugInfo = TryllJsonValue.Null;

        /// <summary>Exact wire payload, including malformed JSON. Empty when the source did not retain it.</summary>
        public string RawDiagnosticsText;

        public TryllDiagnosticsAbsenceReason AbsenceReason = TryllDiagnosticsAbsenceReason.Unavailable;

        /// <summary>Null outside Dialog Lab — pins, checks, and A/B peer are omitted.</summary>
        public TryllTurnLabContext Lab;
    }

    /// <summary>Lab-only overlay. Omit entirely for chat (R1.2, R3.1).</summary>
    internal sealed class TryllTurnLabContext
    {
        public string PinA;
        public string PinB;
        public System.Action<string> OnClearPin;

        public IReadOnlyList<LabCheck> AuthoredChecks = System.Array.Empty<LabCheck>();
        public IReadOnlyList<TryllLabCheckOutcome> Failed = System.Array.Empty<TryllLabCheckOutcome>();

        /// <summary>Peer turn for A/B prompt diff; null when not diffing or peer has no data.</summary>
        public TryllTurnInspection DiffPeer;

        /// <summary>Empty-state copy when nothing is selected but a run exists.</summary>
        public bool ShowLabEmptyPlaceholder = true;
    }

    /// <summary>Builds and validates <see cref="TryllTurnInspection"/> payloads.</summary>
    internal static class TryllTurnInspectionFactory
    {
        public const int SupportedSchemaVersion = 2;

        public static TryllTurnInspection Empty(string hint = "no run yet") => new TryllTurnInspection
        {
            Hint = hint,
            AbsenceReason = TryllDiagnosticsAbsenceReason.Unavailable,
        };

        public static TryllTurnInspection FromLab(
            TryllDialogLabDataset dataset,
            IReadOnlyDictionary<string, LabCell> cells,
            string selectedKey,
            string pinA,
            string pinB,
            System.Action<string> onClearPin)
        {
            if (dataset == null || cells == null)
            {
                return new TryllTurnInspection
                {
                    Hint = "no run yet",
                    AbsenceReason = TryllDiagnosticsAbsenceReason.Unavailable,
                    Lab = new TryllTurnLabContext
                    {
                        PinA = pinA,
                        PinB = pinB,
                        OnClearPin = onClearPin,
                        ShowLabEmptyPlaceholder = true,
                    },
                };
            }

            if (string.IsNullOrEmpty(selectedKey) || !cells.TryGetValue(selectedKey, out var cell))
            {
                return new TryllTurnInspection
                {
                    Hint = "click a cell",
                    AbsenceReason = TryllDiagnosticsAbsenceReason.Unavailable,
                    Lab = new TryllTurnLabContext
                    {
                        PinA = pinA,
                        PinB = pinB,
                        OnClearPin = onClearPin,
                        ShowLabEmptyPlaceholder = true,
                    },
                };
            }

            var sample = System.Array.Find(dataset.Samples, s => s.Id == cell.SampleId);
            var variant = sample != null
                ? System.Array.Find(dataset.Variants, v => v.Id == sample.VariantId)
                : null;

            var turn = FromCellCore(cell);
            turn.TurnKey = cell.Key;
            string dialogue = dataset.Dialogues != null && dataset.Dialogues.Length > 1
                              && cell.DialogueIndex < dataset.Dialogues.Length
                ? $"{dataset.Dialogues[cell.DialogueIndex].Name} · "
                : "";
            turn.Hint = sample != null
                ? $"{cell.SampleId} · {dialogue}turn {cell.StepIndex + 1} · seed {sample.Seed}"
                : $"{cell.SampleId} · {dialogue}turn {cell.StepIndex + 1}";
            turn.RouteTrailing = variant != null ? variant.Label : "";

            TryllTurnInspection peer = null;
            bool diffing = !string.IsNullOrEmpty(pinA) && !string.IsNullOrEmpty(pinB);
            if (diffing)
            {
                string otherSample = cell.SampleId == pinA ? pinB : pinA;
                if (cells.TryGetValue(TryllDialogLabDataset.CellKey(otherSample, cell.TurnIndex), out var otherCell))
                    peer = FromCellCore(otherCell);
            }

            var authored = (dataset.Turns != null
                            && cell.TurnIndex >= 0
                            && cell.TurnIndex < dataset.Turns.Length
                            && dataset.Turns[cell.TurnIndex].Checks != null)
                ? dataset.Turns[cell.TurnIndex].Checks
                : System.Array.Empty<LabCheck>();

            turn.Lab = new TryllTurnLabContext
            {
                PinA = pinA,
                PinB = pinB,
                OnClearPin = onClearPin,
                AuthoredChecks = authored,
                Failed = cell.Failed ?? new List<TryllLabCheckOutcome>(),
                DiffPeer = peer,
                ShowLabEmptyPlaceholder = false,
            };
            return turn;
        }

        /// <summary>
        /// Chat / generic path: attach raw debug_info and classify schema / absence.
        /// </summary>
        public static TryllTurnInspection FromChatTurn(
            string turnKey,
            string hint,
            string answerText,
            TryllTurnTerminalStatus status,
            string errorText,
            TryllTurnRouteNode[] route,
            int durationMs,
            int ttftMs,
            TryllJsonValue debugInfo,
            string rawDiagnosticsText)
        {
            var turn = new TryllTurnInspection
            {
                TurnKey = turnKey,
                Hint = hint ?? "",
                AnswerText = answerText ?? "",
                Status = status,
                ErrorText = errorText ?? "",
                Route = route ?? System.Array.Empty<TryllTurnRouteNode>(),
                DurationMs = durationMs,
                TtftMs = ttftMs,
                DebugInfo = debugInfo ?? TryllJsonValue.Null,
                RawDiagnosticsText = rawDiagnosticsText ?? "",
            };

            // Chat requests diagnostics for every create. Empty and malformed wire text are
            // therefore transport/payload absence, while any valid JSON document proceeds
            // through the strict schema gate (including JSON null and non-object roots).
            if (string.IsNullOrEmpty(turn.RawDiagnosticsText)
                || !TryllJson.TryParse(turn.RawDiagnosticsText, out var parsedRaw, out _))
            {
                turn.DebugInfo = TryllJsonValue.Null;
                turn.AbsenceReason = TryllDiagnosticsAbsenceReason.Empty;
                return turn;
            }
            turn.DebugInfo = parsedRaw;
            turn.AbsenceReason = ClassifySchema(turn.DebugInfo);
            return turn;
        }

        public static TryllDiagnosticsAbsenceReason AbsenceFromLabCell(LabCell c)
        {
            if (c == null) return TryllDiagnosticsAbsenceReason.Unavailable;
            if (c.Seeded) return TryllDiagnosticsAbsenceReason.Seeded;
            if (c.Status == LabCellStatus.Skipped) return TryllDiagnosticsAbsenceReason.Skipped;
            if (c.RawDebugInfo == null || c.RawDebugInfo.IsNull)
                return TryllDiagnosticsAbsenceReason.Unavailable;
            return ClassifySchema(c.RawDebugInfo);
        }

        public static TryllDiagnosticsAbsenceReason ClassifySchema(TryllJsonValue debugInfo)
        {
            if (debugInfo == null || debugInfo.IsNull)
                return TryllDiagnosticsAbsenceReason.UnsupportedSchema;

            if (debugInfo.Kind != TryllJsonKind.Object)
                return TryllDiagnosticsAbsenceReason.UnsupportedSchema;

            var ver = debugInfo.Get("schema_version");
            if (ver.Kind != TryllJsonKind.Number
                || double.IsNaN(ver.Number)
                || double.IsInfinity(ver.Number)
                || ver.Number != SupportedSchemaVersion)
                return TryllDiagnosticsAbsenceReason.UnsupportedSchema;

            return TryllDiagnosticsAbsenceReason.None;
        }

        public static void ApplySchemaGate(TryllTurnInspection turn, TryllDiagnosticsAbsenceReason fallbackAbsence)
        {
            if (turn.DebugInfo == null || turn.DebugInfo.IsNull)
            {
                turn.AbsenceReason = fallbackAbsence;
                return;
            }

            var schema = ClassifySchema(turn.DebugInfo);
            if (schema != TryllDiagnosticsAbsenceReason.None)
            {
                turn.AbsenceReason = schema;
                // Keep raw payload for the escape hatch, but mark diagnostics unusable.
                return;
            }

            turn.AbsenceReason = TryllDiagnosticsAbsenceReason.None;
        }

        public static bool HasUsableDiagnostics(TryllTurnInspection turn) =>
            turn != null
            && turn.AbsenceReason == TryllDiagnosticsAbsenceReason.None
            && turn.DebugInfo != null
            && !turn.DebugInfo.IsNull;

        private static TryllTurnInspection FromCellCore(LabCell cell)
        {
            var route = cell.Route ?? System.Array.Empty<LabRouteNode>();
            var mapped = new TryllTurnRouteNode[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                mapped[i] = new TryllTurnRouteNode
                {
                    Name = route[i].Name,
                    Type = route[i].Type,
                    Exit = route[i].Exit,
                    Ms = route[i].Ms,
                };
            }

            var turn = new TryllTurnInspection
            {
                AnswerText = cell.Text,
                Status = MapLabStatus(cell.Status),
                ErrorText = cell.Status == LabCellStatus.Error || cell.Status == LabCellStatus.Skipped
                    ? (cell.Text ?? "")
                    : "",
                Route = mapped,
                DurationMs = cell.Ms,
                TtftMs = cell.Ttft,
                DebugInfo = cell.RawDebugInfo ?? TryllJsonValue.Null,
            };

            var absence = AbsenceFromLabCell(cell);
            if (absence == TryllDiagnosticsAbsenceReason.None
                || absence == TryllDiagnosticsAbsenceReason.UnsupportedSchema
                || absence == TryllDiagnosticsAbsenceReason.Empty)
            {
                ApplySchemaGate(turn, absence == TryllDiagnosticsAbsenceReason.None
                    ? TryllDiagnosticsAbsenceReason.Empty
                    : absence);
            }
            else
            {
                turn.AbsenceReason = absence;
            }

            return turn;
        }

        private static TryllTurnTerminalStatus MapLabStatus(LabCellStatus status) => status switch
        {
            LabCellStatus.Error => TryllTurnTerminalStatus.Error,
            LabCellStatus.Skipped => TryllTurnTerminalStatus.Skipped,
            _ => TryllTurnTerminalStatus.Success,
        };
    }
}
