// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using Tryll;
using Tryll.Client.Internal;

namespace Tryll.Client
{
    public static class TryllMkbSnapshotLimits
    {
        public const uint FormatVersion = 1;
        public const string FileIdentifier = "TMKB";
    }

    [Serializable]
    public sealed class TryllMutableKbSnapshot
    {
        public string Name;
        public string SchemaName;
        public string SchemaFingerprint;
        public string EmbeddingModel;
        public ulong WriteGeneration;
        public ulong NextGeneratedId = 1;
        public List<TryllMutableKbRecord> Records = new List<TryllMutableKbRecord>();

        public byte[] ToFlatBufferBytes()
        {
            var fbb = new FlatBufferBuilder(1024);
            var offset = Pack(fbb, this);
            fbb.Finish(offset.Value, TryllMkbSnapshotLimits.FileIdentifier);
            var bytes = fbb.SizedByteArray();
            if ((uint)bytes.Length > ClientWire.MaxMutableKbSnapshotBytes)
            {
                throw new ArgumentException(
                    "Mutable KB snapshot exceeds MaxMutableKbSnapshotBytes");
            }
            return bytes;
        }

        public static TryllMutableKbSnapshot FromFlatBufferBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8)
                throw new ArgumentException("Buffer is not a valid TMKB store snapshot");
            var bb = new ByteBuffer(bytes);
            if (!PersistedMutableKb.PersistedMutableKbBufferHasIdentifier(bb))
                throw new ArgumentException("Buffer is not a valid TMKB store snapshot");
            if (!PersistedMutableKb.VerifyPersistedMutableKb(bb))
                throw new ArgumentException("Buffer is not a valid TMKB store snapshot");
            var table = PersistedMutableKb.GetRootAsPersistedMutableKb(bb);
            return Unpack(table);
        }

        public static Offset<PersistedMutableKb> Pack(FlatBufferBuilder fbb, TryllMutableKbSnapshot snapshot)
        {
            snapshot ??= new TryllMutableKbSnapshot();
            if (string.IsNullOrEmpty(snapshot.Name))
                throw new ArgumentException("PersistedMutableKb.name is required");
            if (string.IsNullOrEmpty(snapshot.SchemaName))
                throw new ArgumentException("PersistedMutableKb.schema_name is required");

            var records = snapshot.Records ?? new List<TryllMutableKbRecord>();
            var recOffs = new Offset<PersistedKbRecord>[records.Count];
            var seenIds = new HashSet<string>();
            for (int i = 0; i < records.Count; i++)
            {
                var rec = records[i] ?? new TryllMutableKbRecord();
                if (string.IsNullOrEmpty(rec.Id))
                    throw new ArgumentException("PersistedKbRecord.id is required");
                if (!seenIds.Add(rec.Id))
                    throw new ArgumentException($"duplicate record id '{rec.Id}'");
                recOffs[i] = PersistedKbRecord.CreatePersistedKbRecord(
                    fbb,
                    fbb.CreateString(rec.Id),
                    string.IsNullOrEmpty(rec.Text) ? default : fbb.CreateString(rec.Text),
                    rec.CreatedAtMs,
                    rec.UpdatedAtMs);
            }

            return PersistedMutableKb.CreatePersistedMutableKb(
                fbb,
                fbb.CreateString(snapshot.Name),
                fbb.CreateString(snapshot.SchemaName),
                string.IsNullOrEmpty(snapshot.SchemaFingerprint)
                    ? default
                    : fbb.CreateString(snapshot.SchemaFingerprint),
                string.IsNullOrEmpty(snapshot.EmbeddingModel)
                    ? default
                    : fbb.CreateString(snapshot.EmbeddingModel),
                snapshot.WriteGeneration,
                snapshot.NextGeneratedId,
                PersistedMutableKb.CreateRecordsVector(fbb, recOffs));
        }

        public static TryllMutableKbSnapshot Unpack(PersistedMutableKb table)
        {
            if (string.IsNullOrEmpty(table.Name))
                throw new ArgumentException("PersistedMutableKb.name is required");
            if (string.IsNullOrEmpty(table.SchemaName))
                throw new ArgumentException("PersistedMutableKb.schema_name is required");

            var snapshot = new TryllMutableKbSnapshot
            {
                Name = table.Name,
                SchemaName = table.SchemaName,
                SchemaFingerprint = table.SchemaFingerprint,
                EmbeddingModel = table.EmbeddingModel,
                WriteGeneration = table.WriteGeneration,
                NextGeneratedId = table.NextGeneratedId,
            };
            var seenIds = new HashSet<string>();
            for (int i = 0; i < table.RecordsLength; i++)
            {
                var rec = table.Records(i);
                if (!rec.HasValue)
                    throw new ArgumentException("Null PersistedKbRecord");
                var id = rec.Value.Id;
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentException("PersistedKbRecord.id is required");
                if (!seenIds.Add(id))
                    throw new ArgumentException($"duplicate record id '{id}'");
                snapshot.Records.Add(new TryllMutableKbRecord
                {
                    Id = id,
                    Text = rec.Value.Text,
                    CreatedAtMs = rec.Value.CreatedAtMs,
                    UpdatedAtMs = rec.Value.UpdatedAtMs,
                });
            }
            return snapshot;
        }
    }

    [Serializable]
    public sealed class TryllGameMemorySnapshot
    {
        public uint FormatVersion = TryllMkbSnapshotLimits.FormatVersion;
        public ulong SavedAtMs;
        public string SaveId;
        public List<TryllMutableKbSnapshot> Stores = new List<TryllMutableKbSnapshot>();

        public byte[] ToFlatBufferBytes()
        {
            var fbb = new FlatBufferBuilder(1024);
            var offset = Pack(fbb, this);
            fbb.Finish(offset.Value, TryllMkbSnapshotLimits.FileIdentifier);
            var bytes = fbb.SizedByteArray();
            if ((uint)bytes.Length > ClientWire.MaxGameMemorySnapshotBytes)
            {
                throw new ArgumentException(
                    "GameMemory snapshot exceeds MaxGameMemorySnapshotBytes");
            }
            return bytes;
        }

        public static TryllGameMemorySnapshot FromFlatBufferBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 8)
                throw new ArgumentException("Buffer is not a valid TMKB GameMemory snapshot");
            var bb = new ByteBuffer(bytes);
            if (!PersistedMutableKb.PersistedMutableKbBufferHasIdentifier(bb))
                throw new ArgumentException("Buffer is not a valid TMKB GameMemory snapshot");
            var verifier = new Verifier(bb);
            if (!verifier.VerifyBuffer("TMKB", false, GameMemoryVerify.Verify))
                throw new ArgumentException("Buffer is not a valid TMKB GameMemory snapshot");
            var table = GameMemory.GetRootAsGameMemory(bb);
            return Unpack(table);
        }

        public static Offset<GameMemory> Pack(FlatBufferBuilder fbb, TryllGameMemorySnapshot snapshot)
        {
            snapshot ??= new TryllGameMemorySnapshot();
            if (snapshot.FormatVersion == 0)
                throw new ArgumentException("format_version must not be 0");
            var stores = snapshot.Stores ?? new List<TryllMutableKbSnapshot>();
            var storeOffs = new Offset<PersistedMutableKb>[stores.Count];
            var seenNames = new HashSet<string>();
            for (int i = 0; i < stores.Count; i++)
            {
                storeOffs[i] = TryllMutableKbSnapshot.Pack(fbb, stores[i]);
                var name = stores[i]?.Name;
                if (!string.IsNullOrEmpty(name) && !seenNames.Add(name))
                    throw new ArgumentException($"duplicate store name '{name}'");
            }
            return GameMemory.CreateGameMemory(
                fbb,
                snapshot.FormatVersion,
                snapshot.SavedAtMs,
                string.IsNullOrEmpty(snapshot.SaveId) ? default : fbb.CreateString(snapshot.SaveId),
                GameMemory.CreateStoresVector(fbb, storeOffs));
        }

        public static TryllGameMemorySnapshot Unpack(GameMemory table)
        {
            var snapshot = new TryllGameMemorySnapshot
            {
                FormatVersion = table.FormatVersion,
                SavedAtMs = table.SavedAtMs,
                SaveId = table.SaveId,
            };
            if (snapshot.FormatVersion == 0)
                throw new ArgumentException("format_version must not be 0");
            if (snapshot.FormatVersion > TryllMkbSnapshotLimits.FormatVersion)
                throw new ArgumentException("Unsupported GameMemory format_version");
            var seenNames = new HashSet<string>();
            for (int i = 0; i < table.StoresLength; i++)
            {
                var store = table.Stores(i);
                if (!store.HasValue)
                    throw new ArgumentException("Null PersistedMutableKb");
                var captured = TryllMutableKbSnapshot.Unpack(store.Value);
                if (!seenNames.Add(captured.Name))
                    throw new ArgumentException($"duplicate store name '{captured.Name}'");
                snapshot.Stores.Add(captured);
            }
            return snapshot;
        }
    }
}
