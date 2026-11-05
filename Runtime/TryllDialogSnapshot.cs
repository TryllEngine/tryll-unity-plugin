// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using Tryll;

namespace Tryll.Client
{
    public enum TryllDialogCompatibility : byte
    {
        Strict = 0,
        AllowGraphMismatch = 1,
    }

    public enum TryllDialogSlotKind : byte
    {
        User = 0,
        Instruction = 1,
        General = 2,
    }

    [Serializable]
    public sealed class TryllDialogProjectionWindow
    {
        public string NodeName;
        public uint WindowStart;
    }

    [Serializable]
    public sealed class TryllDialogSlot
    {
        public string Name;
        public string Text;
        public TryllDialogSlotKind Kind = TryllDialogSlotKind.General;
        public HistoryRole HistoryRole = HistoryRole.None;
        public string Producer;
    }

    [Serializable]
    public sealed class TryllDialogKnowledgeChunk
    {
        public string Id;
        public string Text;
        public float Distance;
        public bool DistanceIsNan;
        public float Bm25Score;
        public bool HasBm25Score;
        public uint RankDense;
        public bool HasRankDense;
        public uint RankSparse;
        public bool HasRankSparse;
    }

    [Serializable]
    public sealed class TryllDialogKnowledge
    {
        public string Source;
        public List<TryllDialogKnowledgeChunk> Chunks = new List<TryllDialogKnowledgeChunk>();
    }

    [Serializable]
    public sealed class TryllDialogIntention
    {
        public string Intent;
    }

    [Serializable]
    public sealed class TryllDialogToolCall
    {
        public string ToolName;
        public List<KeyValuePair<string, string>> Arguments = new List<KeyValuePair<string, string>>();
        public string ArgumentsJson;
        public bool Dispatched;
        public string Id;
        public ToolCallDisposition Disposition = ToolCallDisposition.Acknowledge;
        public string ModelId;
        public ulong ExchangeId;
    }

    [Serializable]
    public sealed class TryllDialogToolResult
    {
        public string CallId;
        public string ToolName;
        public string Text;
        public ulong ExchangeId;
    }

    /// <summary>
    /// Rolling conversation summary written by a Summarize node.
    ///
    /// <para><see cref="CoveredThrough"/> is the index of the last interaction folded into
    /// <see cref="Text"/>, inclusive. The server rejects an import whose value is at or past the
    /// snapshot's interaction count.</para>
    /// </summary>
    [Serializable]
    public sealed class TryllDialogSummary
    {
        public string Text;
        public uint CoveredThrough;
        public string Producer;
    }

    [Serializable]
    public sealed class TryllDialogInteractionSnapshot
    {
        public bool Interrupted;
        public List<object> Components = new List<object>();
    }

    [Serializable]
    public sealed class TryllDialogSnapshot
    {
        public uint FormatVersion = 1;
        public string GraphSignature;
        public List<TryllDialogInteractionSnapshot> Interactions = new List<TryllDialogInteractionSnapshot>();
        public ulong NextToolCallSeq;
        public ulong NextExchangeId = 1;
        public List<TryllDialogProjectionWindow> ProjectionWindows = new List<TryllDialogProjectionWindow>();
        public string SaveId;
        public ulong SavedAtMs;

        public byte[] ToFlatBufferBytes()
        {
            var fbb = new FlatBufferBuilder(1024);
            var offset = Pack(fbb, this);
            PersistedDialogSnapshot.FinishPersistedDialogSnapshotBuffer(fbb, offset);
            return fbb.SizedByteArray();
        }

        public static TryllDialogSnapshot FromFlatBufferBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8)
                throw new ArgumentException("Dialog snapshot buffer is too short");
            var bb = new ByteBuffer(bytes);
            if (!PersistedDialogSnapshot.PersistedDialogSnapshotBufferHasIdentifier(bb))
                throw new ArgumentException("Dialog snapshot file identifier is not TDLG");
            if (!PersistedDialogSnapshot.VerifyPersistedDialogSnapshot(bb))
                throw new ArgumentException("Dialog snapshot failed FlatBuffers verification");
            var table = PersistedDialogSnapshot.GetRootAsPersistedDialogSnapshot(bb);
            return Unpack(table);
        }

        public static TryllDialogSnapshot Unpack(PersistedDialogSnapshot table)
        {
            var snapshot = new TryllDialogSnapshot
            {
                FormatVersion = table.FormatVersion,
                GraphSignature = table.GraphSignature,
                NextToolCallSeq = table.NextToolCallSeq,
                NextExchangeId = table.NextExchangeId,
                SaveId = table.SaveId,
                SavedAtMs = table.SavedAtMs,
            };
            if (snapshot.FormatVersion == 0)
                throw new ArgumentException("format_version must not be 0");
            if (snapshot.FormatVersion > 1)
                throw new ArgumentException("Unsupported dialog snapshot format_version");
            if (snapshot.NextExchangeId == 0)
                throw new ArgumentException("next_exchange_id 0 is reserved");
            for (int i = 0; i < table.InteractionsLength; i++)
            {
                var interaction = table.Interactions(i);
                if (!interaction.HasValue)
                    throw new ArgumentException("Null PersistedInteraction");
                snapshot.Interactions.Add(UnpackInteraction(interaction.Value));
            }
            for (int i = 0; i < table.ProjectionWindowsLength; i++)
            {
                var window = table.ProjectionWindows(i);
                if (!window.HasValue || string.IsNullOrEmpty(window.Value.NodeName))
                    throw new ArgumentException("Projection window missing node_name");
                snapshot.ProjectionWindows.Add(new TryllDialogProjectionWindow
                {
                    NodeName = window.Value.NodeName,
                    WindowStart = window.Value.WindowStart,
                });
            }
            return snapshot;
        }

        public static Offset<PersistedDialogSnapshot> Pack(FlatBufferBuilder fbb, TryllDialogSnapshot snapshot)
        {
            snapshot ??= new TryllDialogSnapshot();
            var interactionOffs = new Offset<PersistedInteraction>[snapshot.Interactions.Count];
            for (int i = 0; i < snapshot.Interactions.Count; i++)
                interactionOffs[i] = PackInteraction(fbb, snapshot.Interactions[i]);
            var interactionsVec = PersistedDialogSnapshot.CreateInteractionsVector(fbb, interactionOffs);

            var windowOffs = new Offset<PersistedProjectionWindow>[snapshot.ProjectionWindows.Count];
            for (int i = 0; i < snapshot.ProjectionWindows.Count; i++)
            {
                var window = snapshot.ProjectionWindows[i];
                windowOffs[i] = PersistedProjectionWindow.CreatePersistedProjectionWindow(
                    fbb, fbb.CreateString(window.NodeName ?? ""), window.WindowStart);
            }
            var windowsVec = PersistedDialogSnapshot.CreateProjectionWindowsVector(fbb, windowOffs);
            var signature = string.IsNullOrEmpty(snapshot.GraphSignature)
                ? default(StringOffset)
                : fbb.CreateString(snapshot.GraphSignature);
            var saveId = string.IsNullOrEmpty(snapshot.SaveId)
                ? default(StringOffset)
                : fbb.CreateString(snapshot.SaveId);
            return PersistedDialogSnapshot.CreatePersistedDialogSnapshot(
                fbb,
                snapshot.FormatVersion,
                signature,
                interactionsVec,
                snapshot.NextToolCallSeq,
                snapshot.NextExchangeId,
                windowsVec,
                saveId,
                snapshot.SavedAtMs);
        }

        static TryllDialogInteractionSnapshot UnpackInteraction(PersistedInteraction interaction)
        {
            var captured = new TryllDialogInteractionSnapshot { Interrupted = interaction.Interrupted };
            var count = interaction.ComponentsLength;
            for (int i = 0; i < count; i++)
            {
                var entry = interaction.Components(i);
                if (!entry.HasValue)
                    throw new ArgumentException("Null PersistedComponentEntry");
                switch (entry.Value.ValueType)
                {
                    case PersistedComponent.PersistedSlot:
                    {
                        var slot = entry.Value.Value<PersistedSlot>();
                        if (!slot.HasValue)
                            throw new ArgumentException("Null PersistedSlot");
                        captured.Components.Add(new TryllDialogSlot
                        {
                            Name = slot.Value.Name,
                            Text = slot.Value.Text,
                            Kind = (TryllDialogSlotKind)slot.Value.Kind,
                            HistoryRole = slot.Value.HistoryRole,
                            Producer = slot.Value.Producer,
                        });
                        break;
                    }
                    case PersistedComponent.PersistedKnowledge:
                    {
                        var knowledge = entry.Value.Value<PersistedKnowledge>();
                        if (!knowledge.HasValue)
                            throw new ArgumentException("Null PersistedKnowledge");
                        var payload = new TryllDialogKnowledge { Source = knowledge.Value.Source };
                        for (int c = 0; c < knowledge.Value.ChunksLength; c++)
                        {
                            var chunk = knowledge.Value.Chunks(c);
                            if (!chunk.HasValue)
                                throw new ArgumentException("Null PersistedKnowledgeChunk");
                            payload.Chunks.Add(new TryllDialogKnowledgeChunk
                            {
                                Id = chunk.Value.Id,
                                Text = chunk.Value.Text,
                                Distance = chunk.Value.Distance,
                                DistanceIsNan = chunk.Value.DistanceIsNan,
                                HasBm25Score = chunk.Value.Bm25Score.HasValue,
                                Bm25Score = chunk.Value.Bm25Score.HasValue ? chunk.Value.Bm25Score.Value : 0f,
                                HasRankDense = chunk.Value.RankDense.HasValue,
                                RankDense = chunk.Value.RankDense.HasValue ? chunk.Value.RankDense.Value : 0,
                                HasRankSparse = chunk.Value.RankSparse.HasValue,
                                RankSparse = chunk.Value.RankSparse.HasValue ? chunk.Value.RankSparse.Value : 0,
                            });
                        }
                        captured.Components.Add(payload);
                        break;
                    }
                    case PersistedComponent.PersistedIntention:
                    {
                        var intention = entry.Value.Value<PersistedIntention>();
                        if (!intention.HasValue)
                            throw new ArgumentException("Null PersistedIntention");
                        captured.Components.Add(new TryllDialogIntention { Intent = intention.Value.Intent });
                        break;
                    }
                    case PersistedComponent.PersistedToolCall:
                    {
                        var call = entry.Value.Value<PersistedToolCall>();
                        if (!call.HasValue)
                            throw new ArgumentException("Null PersistedToolCall");
                        var payload = new TryllDialogToolCall
                        {
                            ToolName = call.Value.ToolName,
                            ArgumentsJson = call.Value.ArgumentsJson,
                            Dispatched = call.Value.Dispatched,
                            Id = call.Value.Id,
                            Disposition = call.Value.Disposition,
                            ModelId = call.Value.ModelId,
                            ExchangeId = call.Value.ExchangeId,
                        };
                        for (int a = 0; a < call.Value.ArgumentsLength; a++)
                        {
                            var pair = call.Value.Arguments(a);
                            if (!pair.HasValue || string.IsNullOrEmpty(pair.Value.Key))
                                throw new ArgumentException("Tool argument missing key");
                            payload.Arguments.Add(new KeyValuePair<string, string>(pair.Value.Key, pair.Value.Value));
                        }
                        captured.Components.Add(payload);
                        break;
                    }
                    case PersistedComponent.PersistedToolResult:
                    {
                        var result = entry.Value.Value<PersistedToolResult>();
                        if (!result.HasValue)
                            throw new ArgumentException("Null PersistedToolResult");
                        captured.Components.Add(new TryllDialogToolResult
                        {
                            CallId = result.Value.CallId,
                            ToolName = result.Value.ToolName,
                            Text = result.Value.Text,
                            ExchangeId = result.Value.ExchangeId,
                        });
                        break;
                    }
                    case PersistedComponent.PersistedSummary:
                    {
                        var summary = entry.Value.Value<PersistedSummary>();
                        if (!summary.HasValue)
                            throw new ArgumentException("Null PersistedSummary");
                        captured.Components.Add(new TryllDialogSummary
                        {
                            Text = summary.Value.Text,
                            CoveredThrough = summary.Value.CoveredThrough,
                            Producer = summary.Value.Producer,
                        });
                        break;
                    }
                    case PersistedComponent.NONE:
                        throw new ArgumentException("PersistedComponent is NONE");
                    default:
                        throw new ArgumentException("Unknown PersistedComponent union arm");
                }
            }
            ValidateToolPairing(captured);
            return captured;
        }

        static void ValidateToolPairing(TryllDialogInteractionSnapshot interaction)
        {
            var callIds = new HashSet<string>();
            var callExchange = new Dictionary<string, ulong>();
            foreach (var component in interaction.Components)
            {
                if (component is TryllDialogToolCall call)
                {
                    if (!string.IsNullOrEmpty(call.Id))
                    {
                        callIds.Add(call.Id);
                        callExchange[call.Id] = call.ExchangeId;
                    }
                    continue;
                }
                if (component is TryllDialogToolResult result && !string.IsNullOrEmpty(result.CallId))
                {
                    if (!callIds.Contains(result.CallId))
                        throw new ArgumentException("Tool result call_id has no matching tool call in the same interaction");
                    if (callExchange.TryGetValue(result.CallId, out var expected)
                        && expected != 0 && result.ExchangeId != 0 && expected != result.ExchangeId)
                        throw new ArgumentException("Tool result exchange_id does not match its tool call");
                }
            }
        }

        static Offset<PersistedInteraction> PackInteraction(FlatBufferBuilder fbb, TryllDialogInteractionSnapshot interaction)
        {
            interaction ??= new TryllDialogInteractionSnapshot();
            var types = new PersistedComponent[interaction.Components.Count];
            var unionValues = new int[interaction.Components.Count];
            for (int i = 0; i < interaction.Components.Count; i++)
            {
                var component = interaction.Components[i];
                switch (component)
                {
                    case TryllDialogSlot slot:
                        types[i] = PersistedComponent.PersistedSlot;
                        unionValues[i] = PersistedSlot.CreatePersistedSlot(
                            fbb,
                            fbb.CreateString(slot.Name ?? ""),
                            fbb.CreateString(slot.Text ?? ""),
                            (PersistedSlotKind)slot.Kind,
                            slot.HistoryRole,
                            fbb.CreateString(slot.Producer ?? "")).Value;
                        break;
                    case TryllDialogKnowledge knowledge:
                    {
                        types[i] = PersistedComponent.PersistedKnowledge;
                        var chunkOffs = new Offset<PersistedKnowledgeChunk>[knowledge.Chunks.Count];
                        for (int c = 0; c < knowledge.Chunks.Count; c++)
                        {
                            var chunk = knowledge.Chunks[c];
                            var idOff = fbb.CreateString(chunk.Id ?? "");
                            var textOff = fbb.CreateString(chunk.Text ?? "");
                            PersistedKnowledgeChunk.StartPersistedKnowledgeChunk(fbb);
                            PersistedKnowledgeChunk.AddId(fbb, idOff);
                            PersistedKnowledgeChunk.AddText(fbb, textOff);
                            if (chunk.DistanceIsNan)
                                PersistedKnowledgeChunk.AddDistanceIsNan(fbb, true);
                            else
                                PersistedKnowledgeChunk.AddDistance(fbb, chunk.Distance);
                            if (chunk.HasBm25Score)
                                PersistedKnowledgeChunk.AddBm25Score(fbb, chunk.Bm25Score);
                            if (chunk.HasRankDense)
                                PersistedKnowledgeChunk.AddRankDense(fbb, chunk.RankDense);
                            if (chunk.HasRankSparse)
                                PersistedKnowledgeChunk.AddRankSparse(fbb, chunk.RankSparse);
                            chunkOffs[c] = PersistedKnowledgeChunk.EndPersistedKnowledgeChunk(fbb);
                        }
                        var chunksVec = PersistedKnowledge.CreateChunksVector(fbb, chunkOffs);
                        var sourceOff = fbb.CreateString(knowledge.Source ?? "");
                        unionValues[i] = PersistedKnowledge.CreatePersistedKnowledge(
                            fbb, sourceOff, chunksVec).Value;
                        break;
                    }
                    case TryllDialogIntention intention:
                        types[i] = PersistedComponent.PersistedIntention;
                        unionValues[i] = PersistedIntention.CreatePersistedIntention(
                            fbb, fbb.CreateString(intention.Intent ?? "")).Value;
                        break;
                    case TryllDialogToolCall call:
                    {
                        types[i] = PersistedComponent.PersistedToolCall;
                        var args = new List<KeyValuePair<string, string>>(call.Arguments);
                        args.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
                        var argOffs = new Offset<PersistedStringPair>[args.Count];
                        for (int a = 0; a < args.Count; a++)
                        {
                            argOffs[a] = PersistedStringPair.CreatePersistedStringPair(
                                fbb, fbb.CreateString(args[a].Key ?? ""), fbb.CreateString(args[a].Value ?? ""));
                        }
                        var argsVec = PersistedToolCall.CreateArgumentsVector(fbb, argOffs);
                        unionValues[i] = PersistedToolCall.CreatePersistedToolCall(
                            fbb,
                            fbb.CreateString(call.ToolName ?? ""),
                            argsVec,
                            fbb.CreateString(call.ArgumentsJson ?? ""),
                            call.Dispatched,
                            fbb.CreateString(call.Id ?? ""),
                            call.Disposition,
                            fbb.CreateString(call.ModelId ?? ""),
                            call.ExchangeId).Value;
                        break;
                    }
                    case TryllDialogToolResult result:
                        types[i] = PersistedComponent.PersistedToolResult;
                        unionValues[i] = PersistedToolResult.CreatePersistedToolResult(
                            fbb,
                            fbb.CreateString(result.CallId ?? ""),
                            fbb.CreateString(result.ToolName ?? ""),
                            fbb.CreateString(result.Text ?? ""),
                            result.ExchangeId).Value;
                        break;
                    case TryllDialogSummary summary:
                        types[i] = PersistedComponent.PersistedSummary;
                        unionValues[i] = PersistedSummary.CreatePersistedSummary(
                            fbb,
                            fbb.CreateString(summary.Text ?? ""),
                            summary.CoveredThrough,
                            fbb.CreateString(summary.Producer ?? "")).Value;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported dialog component type: {component?.GetType()}");
                }
            }
            var entryOffs = new Offset<PersistedComponentEntry>[unionValues.Length];
            for (int i = 0; i < unionValues.Length; i++)
            {
                entryOffs[i] = PersistedComponentEntry.CreatePersistedComponentEntry(
                    fbb, types[i], unionValues[i]);
            }
            var entriesVec = PersistedInteraction.CreateComponentsVector(fbb, entryOffs);
            return PersistedInteraction.CreatePersistedInteraction(
                fbb, interaction.Interrupted, entriesVec);
        }
    }

    [Serializable]
    public sealed class TryllDialogImportResult
    {
        public uint InteractionCount;
        public List<string> Warnings = new List<string>();
    }

    [Serializable]
    public sealed class TryllDialogFileSaveResult
    {
        public ulong SavedAtMs;
    }

    [Serializable]
    public sealed class TryllDialogFileLoadResult
    {
        public uint InteractionCount;
        public List<string> Warnings = new List<string>();
    }
}
