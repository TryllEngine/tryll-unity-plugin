// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;

namespace Tryll.Client
{
    public enum TryllMutableKbCompatibility : byte
    {
        Strict = 0,
        AllowSchemaDrift = 1,
    }

    public enum TryllMutableKbEmbedTiming : byte
    {
        Unspecified = 0,
        Now = 1,
        Lazy = 2,
    }

    [Serializable]
    public sealed class TryllMutableKbUpsert
    {
        public string Id;
        public string Text;

        public TryllMutableKbUpsert() { }

        public TryllMutableKbUpsert(string id, string text)
        {
            Id = id;
            Text = text;
        }
    }

    [Serializable]
    public sealed class TryllMutableKbWriteResult
    {
        public string Id;
        public bool Inserted;
        public bool Accepted;
        public string Error;
    }

    [Serializable]
    public sealed class TryllMutableKbHit
    {
        public string Id;
        public string Text;
        public float Distance;
    }

    [Serializable]
    public sealed class TryllMutableKbRecord
    {
        public string Id;
        public string Text;
        public ulong CreatedAtMs;
        public ulong UpdatedAtMs;
    }

    [Serializable]
    public sealed class TryllMutableKbListPage
    {
        public List<TryllMutableKbRecord> Records = new List<TryllMutableKbRecord>();
        public uint NextOffset;
        public bool HasMore;
    }

    [Serializable]
    public sealed class TryllMutableKbStats
    {
        public uint LiveCount;
        public uint TombstonedCount;
        public uint SlotCount;
        public ulong WriteGeneration;
        public ulong BytesEstimate;
        public bool Materialized;
    }

    [Serializable]
    public sealed class TryllMutableKbInfo
    {
        public string Name;
        public string Schema;
        public uint LiveCount;
        public ulong WriteGeneration;
        public bool Materialized;
        public ulong BytesEstimate;
    }

    [Serializable]
    public sealed class TryllMutableKbImportResult
    {
        public string Name;
        public List<string> Warnings = new List<string>();
    }

    [Serializable]
    public sealed class TryllGameMemorySaveResult
    {
        public ulong SavedAtMs;
        public uint StoreCount;
    }

    [Serializable]
    public sealed class TryllGameMemoryLoadResult
    {
        public uint StoreCount;
        public List<string> Warnings = new List<string>();
    }
}
