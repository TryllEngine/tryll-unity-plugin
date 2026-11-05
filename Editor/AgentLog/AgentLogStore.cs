// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections;
using System.Collections.Generic;
using Tryll.Client;
using Tryll.Client.Internal;
using UnityEditor;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Chronological journal view: Count + enumeration in append order.
    /// Node handles stay inside <see cref="AgentLogStore"/>; consumers never see them.
    /// </summary>
    internal sealed class AgentLogJournalView : IReadOnlyCollection<TryllObservationEnvelope>
    {
        readonly LinkedList<TryllObservationEnvelope> _list;

        public AgentLogJournalView(LinkedList<TryllObservationEnvelope> list)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
        }

        public int Count => _list.Count;

        public IEnumerator<TryllObservationEnvelope> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Editor-session Agent Log store: append-only observation journal with retention (§7.5).
    /// Eviction unlinks turn/raw nodes in O(turn size) without rebuilding indexes.
    /// </summary>
    internal sealed class AgentLogStore : ITryllClientObserver
    {
        public enum CaptureState
        {
            NeverAttached,
            Capturing,
            PausedUnsubscribed,
            HistoryDiscardedByReload,
        }

        const string ReloadDiscardSessionKey = "Tryll.AgentLog.HistoryDiscardedByReload";

        static AgentLogStore _instance;

        readonly LinkedList<TryllObservationEnvelope> _journal = new LinkedList<TryllObservationEnvelope>();
        readonly Dictionary<ulong, LinkedListNode<TryllObservationEnvelope>> _byEventId
            = new Dictionary<ulong, LinkedListNode<TryllObservationEnvelope>>();
        readonly Dictionary<ulong, int> _agentScopedCount = new Dictionary<ulong, int>();
        readonly Dictionary<TryllTurnKey, List<LinkedListNode<TryllObservationEnvelope>>> _byTurnKey
            = new Dictionary<TryllTurnKey, List<LinkedListNode<TryllObservationEnvelope>>>();
        readonly Dictionary<ulong, List<TryllTurnKey>> _completedTurnsByAgent
            = new Dictionary<ulong, List<TryllTurnKey>>();
        readonly Queue<LinkedListNode<TryllObservationEnvelope>> _rawSessionNodes
            = new Queue<LinkedListNode<TryllObservationEnvelope>>();

        readonly AgentLogJournalView _journalView;
        readonly AgentDescriptorRegistry _descriptors = new AgentDescriptorRegistry();
        readonly AgentLogConfig _config = AgentLogConfig.Load();
        readonly TryllObservationCorrelator _correlator = new TryllObservationCorrelator();

        TryllClient _client;
        IDisposable _hubSubscription;
        IDisposable _diagnosticsLease;
        ulong _correlatorEpoch;
        ulong _fallbackEventId = 1;
        // Store-wide watermark: a replacement client/connection owns a fresh observer
        // hub whose EventId counter may restart while this journal remains alive.
        ulong _lastJournalEventId;
        bool _changedPending;

        public AgentLogStore()
        {
            _journalView = new AgentLogJournalView(_journal);
        }

        public static AgentLogStore Instance => _instance ?? (_instance = CreateInstance());

        public CaptureState State { get; private set; } = CaptureState.NeverAttached;

        /// <summary>
        /// Sticky banner bit set across domain reload. Survives Attach/capture resume until Clear.
        /// </summary>
        public bool HistoryWasDiscardedByReload { get; private set; }

        public int EvictedTurns { get; private set; }

        public int EvictedObservations { get; private set; }

        public bool IsSubscribed => _hubSubscription != null;

        /// <summary>
        /// True when subscribed specifically to <paramref name="client"/>'s hub
        /// (detects stale subscription after Instance/connection replacement).
        /// </summary>
        public bool IsSubscribedTo(TryllClient client)
            => IsSubscribed && client != null && ReferenceEquals(_client, client);

        /// <summary>Chronological journal (append order). No random indexer — use EventId lookup.</summary>
        public IReadOnlyCollection<TryllObservationEnvelope> Journal => _journalView;

        public AgentDescriptorRegistry Descriptors => _descriptors;
        public AgentLogConfig Config => _config;

        /// <summary>Completed turns retained for an agent (retention FIFO length).</summary>
        public int GetCompletedTurnCount(ulong agentId) =>
            _completedTurnsByAgent.TryGetValue(agentId, out var turns) ? turns.Count : 0;

        /// <summary>Sum of completed turns across all agents (All Agents status line).</summary>
        public int GetTotalCompletedTurnCount()
        {
            var total = 0;
            foreach (var turns in _completedTurnsByAgent.Values)
                total += turns.Count;
            return total;
        }

        public event Action Changed;

        static AgentLogStore()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        static AgentLogStore CreateInstance()
        {
            var store = new AgentLogStore();
            if (SessionState.GetBool(ReloadDiscardSessionKey, false))
            {
                SessionState.SetBool(ReloadDiscardSessionKey, false);
                store.State = CaptureState.HistoryDiscardedByReload;
                store.HistoryWasDiscardedByReload = true;
                store.AppendCaptureGapMarker(0, Tryll.LogGapReason.reload_discard);
            }
            return store;
        }

        static void OnBeforeAssemblyReload()
        {
            if (_instance == null) return;
            if (_instance._journal.Count > 0
                || _instance.State == CaptureState.Capturing
                || _instance.State == CaptureState.PausedUnsubscribed)
            {
                SessionState.SetBool(ReloadDiscardSessionKey, true);
            }
            _instance.OnDomainReload();
        }

        public void Attach(TryllClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            // Pause resume, or live client/hub replacement while still Capturing.
            if (State == CaptureState.PausedUnsubscribed
                || (IsSubscribed && !IsSubscribedTo(client)))
            {
                AppendCaptureGapMarker(client.Connection?.ObserverHub?.ConnectionEpoch ?? 0, Tryll.LogGapReason.paused);
            }

            DetachInternal(releaseLease: true, retainData: true);

            _client = client;
            ResetCorrelatorForHub(client.Connection.ObserverHub);
            _hubSubscription = client.Connection.ObserverHub.Subscribe(this);
            _diagnosticsLease = client.DiagnosticsDemand.Acquire();

            SnapshotLiveAgents(client);

            State = CaptureState.Capturing;
            NotifyChanged();
        }

        public void Detach()
        {
            DetachInternal(releaseLease: true, retainData: true);
            if (State == CaptureState.Capturing)
                State = CaptureState.PausedUnsubscribed;
            NotifyChanged();
        }

        public void Clear()
        {
            DetachInternal(releaseLease: true, retainData: false);
            State = CaptureState.NeverAttached;
            HistoryWasDiscardedByReload = false;
            NotifyChanged();
        }

        internal void OnDomainReload()
        {
            DetachInternal(releaseLease: true, retainData: false);
            State = CaptureState.HistoryDiscardedByReload;
            HistoryWasDiscardedByReload = true;
            NotifyChanged();
        }

        public void OnObservation(TryllObservationEnvelope env)
        {
            if (env == null) return;
            CorrelateAndAppend(env);
        }

        internal void AttachForTests(TryllClient client = null)
        {
            if (State == CaptureState.PausedUnsubscribed)
                AppendCaptureGapMarker(client?.Connection?.ObserverHub?.ConnectionEpoch ?? 0, Tryll.LogGapReason.paused);

            _client = client;
            if (client != null)
            {
                ResetCorrelatorForHub(client.Connection.ObserverHub);
                _diagnosticsLease = client.DiagnosticsDemand.Acquire();
                SnapshotLiveAgents(client);
            }
            State = CaptureState.Capturing;
            NotifyChanged();
        }

        internal void AppendForTests(TryllObservationEnvelope envelope)
        {
            if (envelope != null && envelope.EventId == 0)
                envelope.EventId = AllocateEventId();
            CorrelateAndAppend(envelope);
        }

        public bool TryGetByEventId(ulong eventId, out TryllObservationEnvelope envelope)
        {
            if (_byEventId.TryGetValue(eventId, out var node) && node != null)
            {
                envelope = node.Value;
                return true;
            }
            envelope = null;
            return false;
        }

        void CorrelateAndAppend(TryllObservationEnvelope env)
        {
            if (env.ConnectionEpoch != _correlatorEpoch)
            {
                if (_correlatorEpoch != 0 && !env.IsCaptureGap)
                    AppendCaptureGapMarker(env.ConnectionEpoch, Tryll.LogGapReason.epoch_change);
                _correlator.ResetForEpoch(env.ConnectionEpoch);
                _correlatorEpoch = env.ConnectionEpoch;
            }
            _correlator.Correlate(env);
            AppendEnvelope(env);
        }

        void ResetCorrelatorForHub(TryllClientObserverHub hub)
        {
            if (hub == null) return;
            _correlator.ResetForEpoch(hub.ConnectionEpoch);
            _correlatorEpoch = hub.ConnectionEpoch;
        }

        ulong AllocateEventId()
        {
            var hub = _client?.Connection?.ObserverHub;
            if (hub != null)
                return hub.NextEventId();
            return _fallbackEventId++;
        }

        void AppendEnvelope(TryllObservationEnvelope env)
        {
            // Own a snapshot so later caller/UI mutations cannot rewrite journal history.
            var stored = env.Clone();
            // EventId is the stable UI-selection/index key, so normalize at the
            // persistence boundary before a fresh hub can overwrite an old lookup.
            if (stored.EventId == 0 || stored.EventId <= _lastJournalEventId)
                stored.EventId = _lastJournalEventId + 1;
            _lastJournalEventId = stored.EventId;
            if (_fallbackEventId <= _lastJournalEventId)
                _fallbackEventId = _lastJournalEventId + 1;

            var node = _journal.AddLast(stored);
            IndexNode(node);
            _descriptors.UpsertFromObservation(stored);

            if (stored.Direction != TryllObservationDirection.Outbound && stored.TurnKey.HasValue && stored.AgentId != 0
                && IsTurnTerminalEvent(stored))
            {
                RegisterCompletedTurn(stored.AgentId, stored.TurnKey);
            }

            ApplyRetention();
            NotifyChanged();
        }

        static bool IsTurnTerminalEvent(TryllObservationEnvelope env)
        {
            return env.IsPush(Tryll.MessageBody.TurnComplete)
                || env.IsReplyTo(Tryll.MessageBody.SendMessageRequest, false);
        }

        static bool CountsTowardAgentRetention(TryllObservationEnvelope env)
        {
            if (env == null || env.AgentId == 0 || env.IsCaptureGap)
                return false;
            return env.Scope == TryllObservationScope.CuratedAgent;
        }

        static bool CountsTowardRawRetention(TryllObservationEnvelope env)
        {
            if (env == null || env.IsCaptureGap)
                return false;
            return env.Scope == TryllObservationScope.RawSession;
        }

        void SnapshotLiveAgents(TryllClient client)
        {
            foreach (var snapshot in client.SnapshotRegisteredAgents())
                _descriptors.UpsertFromSnapshot(snapshot, fromAttachSnapshot: true);
        }

        void AppendCaptureGapMarker(ulong connectionEpoch, Tryll.LogGapReason reason)
        {
            var marker = new TryllObservationEnvelope
            {
                EventId = AllocateEventId(),
                ConnectionEpoch = connectionEpoch,
                Direction = TryllObservationDirection.Outbound,
                Scope = TryllObservationScope.RawSession,
                WallTimeUtc = DateTime.UtcNow,
                IsSyntheticMarker = true,
                IsCaptureGap = true,
                IsLocal = true,
                Message = "capture-gap",
                Frame = TryllAgentLogLocal.Gap(reason),
            };
            AppendEnvelope(marker);
        }

        void DetachInternal(bool releaseLease, bool retainData)
        {
            _hubSubscription?.Dispose();
            _hubSubscription = null;

            if (releaseLease)
            {
                _diagnosticsLease?.Dispose();
                _diagnosticsLease = null;
            }

            _client = null;

            if (!retainData)
            {
                ClearJournalStructures();
                _descriptors.Clear();
                _correlator.ResetForEpoch(0);
                _correlatorEpoch = 0;
            }
        }

        void ClearJournalStructures()
        {
            _byEventId.Clear();
            _agentScopedCount.Clear();
            _byTurnKey.Clear();
            _completedTurnsByAgent.Clear();
            _rawSessionNodes.Clear();
            _journal.Clear();
            _lastJournalEventId = 0;
            _fallbackEventId = 1;
            EvictedTurns = 0;
            EvictedObservations = 0;
        }

        void IndexNode(LinkedListNode<TryllObservationEnvelope> node)
        {
            var env = node.Value;
            if (env.EventId != 0)
                _byEventId[env.EventId] = node;

            if (CountsTowardAgentRetention(env))
            {
                if (!_agentScopedCount.TryGetValue(env.AgentId, out var count))
                    count = 0;
                _agentScopedCount[env.AgentId] = count + 1;
            }

            if (env.TurnKey.HasValue)
            {
                if (!_byTurnKey.TryGetValue(env.TurnKey, out var turnList))
                    _byTurnKey[env.TurnKey] = turnList = new List<LinkedListNode<TryllObservationEnvelope>>();
                turnList.Add(node);
            }

            if (CountsTowardRawRetention(env))
                _rawSessionNodes.Enqueue(node);
        }

        void RegisterCompletedTurn(ulong agentId, TryllTurnKey turnKey)
        {
            if (!_completedTurnsByAgent.TryGetValue(agentId, out var turns))
                _completedTurnsByAgent[agentId] = turns = new List<TryllTurnKey>();
            if (!turns.Contains(turnKey))
                turns.Add(turnKey);
        }

        void ApplyRetention()
        {
            EvictPerAgentTurns();
            EvictRawSessionObservations();
        }

        void EvictPerAgentTurns()
        {
            var agents = new List<ulong>(_completedTurnsByAgent.Keys);
            foreach (var agentId in agents)
            {
                while (ShouldEvictAgentTurn(agentId))
                {
                    if (!TryEvictOldestCompleteTurn(agentId))
                        break;
                }
            }
        }

        bool ShouldEvictAgentTurn(ulong agentId)
        {
            var completedCount = _completedTurnsByAgent.TryGetValue(agentId, out var turns)
                ? turns.Count
                : 0;
            if (completedCount > _config.MaxCompletedTurnsPerAgent)
                return true;

            var agentObs = _agentScopedCount.TryGetValue(agentId, out var count) ? count : 0;
            return agentObs > _config.MaxObservationsPerAgent;
        }

        bool TryEvictOldestCompleteTurn(ulong agentId)
        {
            if (!_completedTurnsByAgent.TryGetValue(agentId, out var turns) || turns.Count == 0)
                return false;

            var turnKey = turns[0];
            turns.RemoveAt(0);
            if (turns.Count == 0)
                _completedTurnsByAgent.Remove(agentId);

            if (_byTurnKey.TryGetValue(turnKey, out var members) && members != null)
                EvictedObservations += members.Count;
            EvictedTurns++;
            RemoveTurnNodes(turnKey);
            return true;
        }

        void RemoveTurnNodes(TryllTurnKey turnKey)
        {
            if (!_byTurnKey.TryGetValue(turnKey, out var nodes) || nodes == null)
                return;

            _byTurnKey.Remove(turnKey);
            for (int i = 0; i < nodes.Count; ++i)
                UnlinkNode(nodes[i]);
        }

        void EvictRawSessionObservations()
        {
            while (_rawSessionNodes.Count > _config.MaxRawSessionObservations)
            {
                var oldest = _rawSessionNodes.Dequeue();
                // Skip nodes already unlinked (e.g. if they were also part of a turn — raw
                // observations are typically session-scoped and not turn-keyed, but be safe).
                if (oldest == null || oldest.List != _journal)
                    continue;
                EvictedObservations++;
                UnlinkNode(oldest);
            }
        }

        void UnlinkNode(LinkedListNode<TryllObservationEnvelope> node)
        {
            if (node == null || node.List != _journal)
                return;

            var env = node.Value;
            if (env.EventId != 0)
                _byEventId.Remove(env.EventId);

            if (CountsTowardAgentRetention(env)
                && _agentScopedCount.TryGetValue(env.AgentId, out var count))
            {
                if (count <= 1)
                    _agentScopedCount.Remove(env.AgentId);
                else
                    _agentScopedCount[env.AgentId] = count - 1;
            }

            // Raw FIFO may still hold a stale handle; EvictRawSessionObservations skips those.
            // Turn membership was already removed by RemoveTurnNodes before UnlinkNode.

            _journal.Remove(node);
        }

        void NotifyChanged()
        {
            _changedPending = true;
            EditorApplication.delayCall -= FlushChanged;
            EditorApplication.delayCall += FlushChanged;
        }

        void FlushChanged()
        {
            if (!_changedPending) return;
            _changedPending = false;
            Changed?.Invoke();
        }
    }
}
