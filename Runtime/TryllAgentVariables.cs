// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
//
// Local mirror of one agent's declared per-agent Variables, with deferred-flush
// staging. Mirrors Tryll::Client::AgentVariables (C++,
// tryll/clients/cpp/include/tryll/AgentVariables.h) and tryll_client.variables.AgentVariables
// (Python, tryll/clients/python/tryll_client/variables.py): seeded from the
// declaration passed to CreateAgent, mutations validated locally (unknown name /
// type mismatch return a TryllError immediately — no wire round-trip) and staged.
//
// Staged writes are coalesced per name (last value wins for scalars; a pure
// Reset is preserved; set edits accumulate a minimal add/remove delta, or a
// whole-set assign, whichever is smaller) so rapid repeated writes never grow
// the batch unboundedly. The batch is flushed as one UpdateAgentVariablesRequest
// immediately before the agent's next SendMessage/Resume/ChangeParams call
// (never before Cancel), and retained in-flight until acknowledged: a transient
// AgentBusy re-stages behind newer writes, a hard rejection re-syncs the
// affected names from the server, and either way the error is surfaced through
// the agent's OnError callback (auto-flush) or the caller's completion callback.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Tryll.Client
{
    /// <summary>
    /// Local mirror of one agent's declared per-agent Variables.
    /// Obtained via <see cref="TryllAgent.Variables"/>.
    /// </summary>
    public sealed class TryllAgentVariables
    {
        private readonly TryllAgent _agent;

        private readonly Dictionary<string, TryllVariableType> _declaredTypes = new Dictionary<string, TryllVariableType>();
        private readonly Dictionary<string, object>             _initial       = new Dictionary<string, object>();
        private readonly Dictionary<string, object>             _values        = new Dictionary<string, object>();

        // Per-name coalesced staging entry. Mode is null (pure set delta),
        // "assign" (whole-value replacement), or "reset".
        private sealed class PendingEntry
        {
            public string Mode;
            public readonly HashSet<string> Adds = new HashSet<string>();
            public readonly HashSet<string> Removes = new HashSet<string>();
        }

        private List<string> _pendingOrder = new List<string>();
        private Dictionary<string, PendingEntry> _pending = new Dictionary<string, PendingEntry>();
        // Per-name count of outstanding in-flight batches that still include
        // the name. Each flush captures its own batch snapshot in the response
        // callback so overlapping flushes cannot overwrite each other's recovery
        // state. MutationSeq advances on every staged write so an older batch's
        // AgentBusy/hard-error recovery never re-applies a superseded value.
        private readonly Dictionary<string, int> _inFlightRefCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _mutationSeq = new Dictionary<string, int>();

        /// <summary>
        /// Test hook: when set, <see cref="FlushIfDirty"/> invokes this instead of
        /// the live agent wire send. Signature is (wireBatch, onComplete, carrierRequestId).
        /// </summary>
        internal Action<List<Tryll.VariableUpdateT>, Action<TryllError>, ulong> TestSendUpdate;

        internal TryllAgentVariables(TryllAgent agent, List<TryllVariableDecl> declared)
        {
            _agent = agent;
            if (declared == null) return;
            foreach (var d in declared)
            {
                if (d == null || string.IsNullOrEmpty(d.Name)) continue;
                var value = d.GetBoxedValue();
                if (d.Type == TryllVariableType.StringSet)
                    value = NormalizeSet(value as IEnumerable<string>);
                _declaredTypes[d.Name] = d.Type;
                _initial[d.Name]       = value;
                _values[d.Name]        = CopyValue(value);
            }
        }

        // ── Introspection ────────────────────────────────────────────────────

        public bool IsDeclared(string name) => _declaredTypes.ContainsKey(name);

        public TryllVariableType? DeclaredType(string name)
            => _declaredTypes.TryGetValue(name, out var t) ? t : (TryllVariableType?)null;

        /// <summary>True when one or more mutations are staged and not yet flushed.</summary>
        public bool IsDirty => _pendingOrder.Count > 0;

        /// <summary>Deep copy of every declared variable's current local value, keyed by name.</summary>
        public Dictionary<string, object> Snapshot()
        {
            var outMap = new Dictionary<string, object>(_values.Count);
            foreach (var kv in _values) outMap[kv.Key] = CopyValue(kv.Value);
            return outMap;
        }

        public long GetInt(string name)
            => _values.TryGetValue(name, out var v) && v is long l ? l : 0L;

        public double GetFloat(string name)
            => _values.TryGetValue(name, out var v) && v is double d ? d : 0.0;

        public string GetString(string name)
            => _values.TryGetValue(name, out var v) ? (v as string ?? "") : "";

        public bool GetBool(string name)
            => _values.TryGetValue(name, out var v) && v is bool b && b;

        /// <summary>Returns a defensive copy — mutating it does not affect the mirror.</summary>
        public IReadOnlyList<string> GetStringSet(string name)
            => _values.TryGetValue(name, out var v) && v is List<string> l
                ? new List<string>(l)
                : (IReadOnlyList<string>)Array.Empty<string>();

        // ── Mutation (local pre-validation; no wire round-trip on failure) ────

        public TryllError SetInt(string name, long value)
        {
            var err = RequireType(name, TryllVariableType.Int);
            if (!err.IsOk) return err;
            _values[name] = value;
            MarkWholesale(name, "assign");
            return TryllError.Ok();
        }

        public TryllError SetFloat(string name, double value)
        {
            var err = RequireType(name, TryllVariableType.Float);
            if (!err.IsOk) return err;
            _values[name] = value;
            MarkWholesale(name, "assign");
            return TryllError.Ok();
        }

        public TryllError SetString(string name, string value)
        {
            var err = RequireType(name, TryllVariableType.String);
            if (!err.IsOk) return err;
            _values[name] = value ?? "";
            MarkWholesale(name, "assign");
            return TryllError.Ok();
        }

        public TryllError SetBool(string name, bool value)
        {
            var err = RequireType(name, TryllVariableType.Bool);
            if (!err.IsOk) return err;
            _values[name] = value;
            MarkWholesale(name, "assign");
            return TryllError.Ok();
        }

        public TryllError SetStringSet(string name, IEnumerable<string> values)
        {
            var err = RequireType(name, TryllVariableType.StringSet);
            if (!err.IsOk) return err;
            _values[name] = NormalizeSet(values);
            MarkWholesale(name, "assign");
            return TryllError.Ok();
        }

        /// <summary>Idempotent — a no-op success if <paramref name="element"/> is already present.</summary>
        public TryllError AddToSet(string name, string element)
        {
            var err = RequireType(name, TryllVariableType.StringSet);
            if (!err.IsOk) return err;
            if (element == null)
                return new TryllError(TryllErrorCode.InvalidVariableValue, "set element must not be null");
            var current = new SortedSet<string>(CurrentSet(name), StringComparer.Ordinal) { element };
            _values[name] = current.ToList();
            var entry = Touch(name);
            if (entry.Mode == null) { entry.Removes.Remove(element); entry.Adds.Add(element); }
            else                    { entry.Mode = "assign"; }
            return TryllError.Ok();
        }

        /// <summary>Idempotent — a no-op success if <paramref name="element"/> is already absent.</summary>
        public TryllError RemoveFromSet(string name, string element)
        {
            var err = RequireType(name, TryllVariableType.StringSet);
            if (!err.IsOk) return err;
            if (element == null)
                return new TryllError(TryllErrorCode.InvalidVariableValue, "set element must not be null");
            var current = new SortedSet<string>(CurrentSet(name), StringComparer.Ordinal);
            current.Remove(element);
            _values[name] = current.ToList();
            var entry = Touch(name);
            if (entry.Mode == null) { entry.Adds.Remove(element); entry.Removes.Add(element); }
            else                    { entry.Mode = "assign"; }
            return TryllError.Ok();
        }

        /// <summary>Restores the CreateAgent-time initial value for <paramref name="name"/>.</summary>
        public TryllError Reset(string name)
        {
            if (!_declaredTypes.ContainsKey(name))
                return Unknown(name);
            _values[name] = _initial.TryGetValue(name, out var iv) ? CopyValue(iv) : null;
            MarkWholesale(name, "reset");
            return TryllError.Ok();
        }

        // ── Flush / sync ─────────────────────────────────────────────────────

        public void FlushIfDirty(Action<TryllError> onComplete = null)
            => FlushIfDirty(carrierRequestId: 0, onComplete);

        /// <summary>
        /// Send every staged mutation as one batched UpdateAgentVariablesRequest.
        /// Called automatically at the top of carrier operations (SendMessage, Resume, …)
        /// — deliberately NOT before <see cref="TryllAgent.Cancel"/>. No-op when
        /// nothing is staged (invokes <paramref name="onComplete"/> with Ok).
        /// </summary>
        /// <param name="carrierRequestId">
        /// Observation-only link to the subsequent carrier command's request id (§7.5).
        /// </param>
        public void FlushIfDirty(ulong carrierRequestId, Action<TryllError> onComplete = null)
        {
            if (_pendingOrder.Count == 0)
            {
                onComplete?.Invoke(TryllError.Ok());
                return;
            }
            if (TestSendUpdate == null && (_agent == null || !_agent.IsValid))
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return;
            }
            var wire = BuildWireBatch();
            // Capture this request's coalesced batch for recovery. Leave pending
            // clear so later writes stage independently; overlapping flushes each
            // own their snapshot instead of sharing one mutable in-flight slot.
            var batchOrder = _pendingOrder;
            var batchByName = _pending;
            var batchSeq = new Dictionary<string, int>(batchOrder.Count);
            foreach (var name in batchOrder)
            {
                batchSeq[name] = _mutationSeq.TryGetValue(name, out var seq) ? seq : 0;
                if (_inFlightRefCounts.TryGetValue(name, out var count))
                    _inFlightRefCounts[name] = count + 1;
                else
                    _inFlightRefCounts[name] = 1;
            }
            _pendingOrder = new List<string>();
            _pending = new Dictionary<string, PendingEntry>();

            ulong agentId = _agent != null ? _agent.AgentId : 0;
            TryllLog.Info(
                $"AgentVariables: flushing {wire.Count} update(s) for agent {agentId}: " +
                string.Join(", ", wire.ConvertAll(u =>
                    u == null ? "?" : $"{u.Name}:{u.Op}")));

            Action<TryllError> onWireComplete = error =>
            {
                HandleFlushResult(error, batchOrder, batchByName, batchSeq);
                if (error.IsOk)
                    TryllLog.Info($"AgentVariables: flush ack OK (agent {agentId})");
                else
                    TryllLog.Warning(
                        $"AgentVariables: flush rejected (agent {agentId}): " +
                        $"{error.Code} {error.Message}");
                // Auto-flush (no caller callback): surface failures on the
                // agent's OnError so a TryllAgentComponent.OnError listener sees
                // them, not only the global TryllClient.Error channel.
                if (!error.IsOk && onComplete == null && _agent != null)
                    _agent.HandleError(error);
                onComplete?.Invoke(error);
            };

            if (TestSendUpdate != null)
                TestSendUpdate(wire, onWireComplete, carrierRequestId);
            else
                _agent.SendUpdateVariables(wire, onWireComplete, carrierRequestId);
        }

        private void HandleFlushResult(
            TryllError error,
            List<string> batchOrder,
            Dictionary<string, PendingEntry> batchByName,
            Dictionary<string, int> batchSeq)
        {
            foreach (var name in batchOrder)
            {
                if (!_inFlightRefCounts.TryGetValue(name, out var count)) continue;
                if (count <= 1) _inFlightRefCounts.Remove(name);
                else _inFlightRefCounts[name] = count - 1;
            }

            if (error.IsOk)
                return;

            if (error.Code == (int)TryllErrorCode.AgentBusy)
            {
                // Transient: re-stage entries not superseded by a newer local
                // mutation (pending or a later flush), to retry next flush.
                foreach (var name in batchOrder)
                {
                    if (!IsBatchCurrent(name, batchSeq)) continue;
                    if (_pending.ContainsKey(name)) continue;
                    _pendingOrder.Add(name);
                    _pending[name] = batchByName[name];
                }
                return;
            }

            // Hard rejection: re-sync names still matching this batch's mutation
            // generation so the optimistic mirror is reconciled.
            var resync = batchOrder.Where(n => IsBatchCurrent(n, batchSeq) && !_pending.ContainsKey(n)).ToList();
            if (resync.Count > 0)
                SyncFromServerAsync(resync);
        }

        private bool IsBatchCurrent(string name, Dictionary<string, int> batchSeq)
        {
            int batch = batchSeq.TryGetValue(name, out var s) ? s : 0;
            int current = _mutationSeq.TryGetValue(name, out var c) ? c : 0;
            return current == batch;
        }

        /// <summary>
        /// Refresh the local mirror from the server's authoritative current values.
        /// Names with a locally newer (staged or in-flight) write are skipped.
        /// </summary>
        /// <param name="names">Variable names to fetch; empty/null fetches every declared variable.</param>
        public void SyncFromServerAsync(List<string> names = null, Action<TryllError> onComplete = null)
        {
            if (!_agent.IsValid)
            {
                onComplete?.Invoke(new TryllError(TryllErrorCode.InvalidAgentId, "Agent not ready"));
                return;
            }
            _agent.SendGetVariables(names ?? new List<string>(), (variables, error) =>
            {
                if (error.IsOk && variables != null)
                {
                    foreach (var v in variables)
                    {
                        if (v == null || string.IsNullOrEmpty(v.Name)) continue;
                        if (_pending.ContainsKey(v.Name) || _inFlightRefCounts.ContainsKey(v.Name)) continue;
                        _values[v.Name] = ConvertFromUnion(v.Name, v.Value);
                    }
                }
                onComplete?.Invoke(error);
            });
        }

        // ── Internals ────────────────────────────────────────────────────────

        private PendingEntry Touch(string name)
        {
            if (!_pending.TryGetValue(name, out var e))
            {
                e = new PendingEntry();
                _pending[name] = e;
                _pendingOrder.Add(name);
            }
            _mutationSeq[name] = _mutationSeq.TryGetValue(name, out var seq) ? seq + 1 : 1;
            return e;
        }

        private void MarkWholesale(string name, string mode)
        {
            var e = Touch(name);
            e.Mode = mode;
            e.Adds.Clear();
            e.Removes.Clear();
        }

        private List<string> CurrentSet(string name)
            => _values.TryGetValue(name, out var v) && v is List<string> l ? l : new List<string>();

        private static object CopyValue(object value)
            => value is List<string> l ? new List<string>(l) : value;

        private static List<string> NormalizeSet(IEnumerable<string> values)
        {
            if (values == null) return new List<string>();
            var set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var s in values)
                if (s != null) set.Add(s);
            return set.ToList();
        }

        private List<Tryll.VariableUpdateT> BuildWireBatch()
        {
            var wire = new List<Tryll.VariableUpdateT>();
            foreach (var name in _pendingOrder)
            {
                if (!_pending.TryGetValue(name, out var e)) continue;
                if (e.Mode == "reset")
                {
                    // Preserve the explicit Reset — smaller wire than assigning a
                    // large initial set, and clearer intent.
                    wire.Add(new Tryll.VariableUpdateT { Name = name, Op = Tryll.VariableOp.Reset, Value = null });
                    continue;
                }
                if (e.Mode == "assign")
                {
                    wire.Add(new Tryll.VariableUpdateT { Name = name, Op = Tryll.VariableOp.Assign, Value = UnionFor(name, _values[name]) });
                    continue;
                }
                // Pure set delta — incremental ops, unless a whole-set assign
                // encodes smaller (fewer elements): §6.3 "smaller encoding".
                var adds = e.Adds.OrderBy(x => x, StringComparer.Ordinal).ToList();
                var removes = e.Removes.OrderBy(x => x, StringComparer.Ordinal).ToList();
                var current = CurrentSet(name);
                if (adds.Count + removes.Count <= current.Count)
                {
                    foreach (var el in adds)
                        wire.Add(new Tryll.VariableUpdateT { Name = name, Op = Tryll.VariableOp.AddToSet, Value = Tryll.VariableValueUnion.FromVarString(new Tryll.VarStringT { V = el }) });
                    foreach (var el in removes)
                        wire.Add(new Tryll.VariableUpdateT { Name = name, Op = Tryll.VariableOp.RemoveFromSet, Value = Tryll.VariableValueUnion.FromVarString(new Tryll.VarStringT { V = el }) });
                }
                else
                {
                    wire.Add(new Tryll.VariableUpdateT { Name = name, Op = Tryll.VariableOp.Assign, Value = UnionFor(name, current) });
                }
            }
            return wire;
        }

        private Tryll.VariableValueUnion UnionFor(string name, object value)
        {
            switch (_declaredTypes[name])
            {
                case TryllVariableType.Int:       return Tryll.VariableValueUnion.FromVarInt(new Tryll.VarIntT { V = (long)value });
                case TryllVariableType.Float:     return Tryll.VariableValueUnion.FromVarFloat(new Tryll.VarFloatT { V = (double)value });
                case TryllVariableType.String:    return Tryll.VariableValueUnion.FromVarString(new Tryll.VarStringT { V = (string)value ?? "" });
                case TryllVariableType.Bool:      return Tryll.VariableValueUnion.FromVarBool(new Tryll.VarBoolT { V = (bool)value });
                case TryllVariableType.StringSet: return Tryll.VariableValueUnion.FromVarStringSet(new Tryll.VarStringSetT { V = new List<string>((List<string>)value) });
                default:                          return null;
            }
        }

        private TryllError RequireType(string name, TryllVariableType expected)
        {
            if (!_declaredTypes.TryGetValue(name, out var actual))
                return Unknown(name);
            if (actual != expected)
                return new TryllError(TryllErrorCode.VariableTypeMismatch,
                    $"variable '{name}' is declared {actual} — cannot use as {expected}");
            return TryllError.Ok();
        }

        private static TryllError Unknown(string name)
            => new TryllError(TryllErrorCode.UnknownVariable, $"unknown agent variable '{name}'");

        private object ConvertFromUnion(string name, Tryll.VariableValueUnion u)
        {
            if (u == null) return null;
            switch (u.Type)
            {
                case Tryll.VariableValue.VarInt:       return u.AsVarInt()?.V ?? 0L;
                case Tryll.VariableValue.VarFloat:      return u.AsVarFloat()?.V ?? 0.0;
                case Tryll.VariableValue.VarString:     return u.AsVarString()?.V ?? "";
                case Tryll.VariableValue.VarBool:       return u.AsVarBool()?.V ?? false;
                case Tryll.VariableValue.VarStringSet:  return NormalizeSet(u.AsVarStringSet()?.V);
                default:                                 return null;
            }
        }
    }
}
