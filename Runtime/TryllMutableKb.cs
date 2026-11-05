// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tryll;

namespace Tryll.Client
{
    /// <summary>
    /// Lightweight session-bound handle for one named mutable knowledge base.
    /// Operations fail with a terminal error after the owning session is torn down.
    /// </summary>
    public sealed class TryllMutableKb
    {
        private readonly TryllClient _client;
        private readonly string _name;
        private readonly string _schema;

        internal TryllMutableKb(TryllClient client, string name, string schema)
        {
            _client = client;
            _name = name ?? "";
            _schema = schema ?? "";
        }

        public bool IsValid => _client != null && _client.IsSessionReady;

        public string Name => _name;

        public string Schema => _schema;

        public Task<(List<TryllMutableKbWriteResult> results, TryllError error)> WriteAsync(
            IReadOnlyList<TryllMutableKbUpsert> records)
            => _client.MutableKbWriteAsync(_name, _schema, records);

        public Task<(uint deletedCount, TryllError error)> DeleteAsync(IReadOnlyList<string> ids)
            => _client.MutableKbDeleteAsync(_name, ids);

        public Task<(uint clearedCount, TryllError error)> ClearAsync()
            => _client.MutableKbClearAsync(_name);

        public Task<(List<TryllMutableKbHit> hits, TryllError error)> QueryAsync(
            string queryText,
            uint topK = 4,
            RetrievalMode retrievalMode = RetrievalMode.Dense)
            => _client.MutableKbQueryAsync(_name, queryText, topK, retrievalMode);

        public Task<(TryllMutableKbRecord record, TryllError error)> GetAsync(string id)
            => _client.MutableKbGetAsync(_name, id);

        public Task<(TryllMutableKbListPage page, TryllError error)> ListAsync(
            uint offset = 0, uint limit = 50)
            => _client.MutableKbListAsync(_name, offset, limit);

        public Task<(TryllMutableKbStats stats, TryllError error)> StatsAsync()
            => _client.MutableKbStatsAsync(_name);

        public Task<(TryllMutableKbSnapshot snapshot, TryllError error)> ExportAsync()
            => _client.ExportMutableKbAsync(_name);

        public Task<(TryllMutableKbImportResult result, TryllError error)> ImportAsync(
            TryllMutableKbSnapshot snapshot,
            TryllMutableKbCompatibility compatibility = TryllMutableKbCompatibility.Strict)
        {
            if (snapshot == null || snapshot.Name != _name)
            {
                var tcs = new TaskCompletionSource<(TryllMutableKbImportResult, TryllError)>();
                var err = new TryllError(
                    TryllErrorCode.MutableKbSnapshotInvalid,
                    $"snapshot.name must match store name '{_name}'");
                _client.Connection.Post(() => tcs.TrySetResult((null, err)));
                return tcs.Task;
            }
            return _client.ImportMutableKbAsync(snapshot, compatibility);
        }
    }
}
