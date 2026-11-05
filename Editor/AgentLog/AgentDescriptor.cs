// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using Tryll;
using Tryll.Client;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor.AgentLog
{
    internal sealed class AgentDescriptor
    {
        public ulong AgentId;
        public string Label = "";
        public string CreationSummary = "";
        public bool DiagnosticsAvailable;
        public bool CreatedBeforeDiagnosticsCapture;
        public bool Destroyed;
        public DateTime FirstSeenUtc;
        public DateTime? DestroyedUtc;
    }

    internal sealed class AgentDescriptorRegistry
    {
        readonly Dictionary<ulong, AgentDescriptor> _agents = new Dictionary<ulong, AgentDescriptor>();

        public IReadOnlyDictionary<ulong, AgentDescriptor> All => _agents;

        public bool TryGet(ulong agentId, out AgentDescriptor descriptor) =>
            _agents.TryGetValue(agentId, out descriptor);

        public void Clear() => _agents.Clear();

        public void UpsertFromSnapshot(TryllAgentSnapshot snapshot, bool fromAttachSnapshot)
        {
            if (snapshot.AgentId == 0) return;

            if (!_agents.TryGetValue(snapshot.AgentId, out var descriptor))
            {
                descriptor = new AgentDescriptor
                {
                    AgentId = snapshot.AgentId,
                    FirstSeenUtc = DateTime.UtcNow,
                };
            }

            if (!string.IsNullOrEmpty(snapshot.LabelHint))
                descriptor.Label = snapshot.LabelHint;
            else if (string.IsNullOrEmpty(descriptor.Label) || IsPendingLabel(descriptor.Label))
                descriptor.Label = FormatDefaultLabel(snapshot.AgentId);

            descriptor.DiagnosticsAvailable = snapshot.CreatedWithDiagnostics;
            if (fromAttachSnapshot && !snapshot.CreatedWithDiagnostics)
                descriptor.CreatedBeforeDiagnosticsCapture = true;
            else if (snapshot.CreatedBeforeDiagnosticsCapture)
                descriptor.CreatedBeforeDiagnosticsCapture = true;

            _agents[snapshot.AgentId] = descriptor;
        }

        public void UpsertFromObservation(TryllObservationEnvelope env)
        {
            if (env == null) return;

            if (env.IsRequest(MessageBody.CreateAgentRequest))
            {
                UpsertPendingCreate(env);
                return;
            }

            if (env.IsRequest(MessageBody.DestroyAgentRequest))
            {
                if (env.AgentId != 0)
                    MarkDestroyed(env.AgentId);
                return;
            }

            if (env.IsReplyTo(MessageBody.CreateAgentRequest, true))
            {
                UpsertFromCreateOk(env);
                return;
            }

            if (env.IsReplyTo(MessageBody.CreateAgentRequest, false))
            {
                UpsertFromCreateErr(env);
                return;
            }

            if ((env.IsReplyTo(MessageBody.DestroyAgentRequest, true)
                    || env.IsReplyTo(MessageBody.DestroyAgentRequest, false))
                && env.AgentId != 0)
            {
                MarkDestroyed(env.AgentId);
            }
        }

        void UpsertPendingCreate(TryllObservationEnvelope env)
        {
            var key = PendingCreateKey(env.ConnectionEpoch, env.RequestId);
            if (!_pendingCreates.TryGetValue(key, out var descriptor))
            {
                descriptor = new AgentDescriptor
                {
                    AgentId = env.AgentId,
                    FirstSeenUtc = env.WallTimeUtc != default ? env.WallTimeUtc : DateTime.UtcNow,
                };
            }

            if (env.AgentId != 0)
                descriptor.AgentId = env.AgentId;

            descriptor.DiagnosticsAvailable = env.EnableDiagnostics;
            if (!string.IsNullOrEmpty(env.GraphDefaultModelName))
                descriptor.CreationSummary = env.GraphDefaultModelName;

            if (string.IsNullOrEmpty(descriptor.Label))
                descriptor.Label = env.AgentId != 0
                    ? FormatDefaultLabel(env.AgentId)
                    : $"Pending agent (req {env.RequestId})";

            _pendingCreates[key] = descriptor;
        }

        void UpsertFromCreateOk(TryllObservationEnvelope env)
        {
            if (env.AgentId == 0) return;

            var key = PendingCreateKey(env.ConnectionEpoch, env.RequestId);
            AgentDescriptor descriptor;
            if (_pendingCreates.TryGetValue(key, out var pending))
            {
                descriptor = pending;
                _pendingCreates.Remove(key);
            }
            else if (!_agents.TryGetValue(env.AgentId, out descriptor))
            {
                descriptor = new AgentDescriptor
                {
                    AgentId = env.AgentId,
                    FirstSeenUtc = env.WallTimeUtc != default ? env.WallTimeUtc : DateTime.UtcNow,
                };
            }

            descriptor.AgentId = env.AgentId;
            // CreateAgent is observed with AgentId=0, so the pending label is
            // "Pending agent (req N)". Always promote to a real label on Ok.
            if (string.IsNullOrEmpty(descriptor.Label) || IsPendingLabel(descriptor.Label))
                descriptor.Label = FormatDefaultLabel(env.AgentId);

            _agents[env.AgentId] = descriptor;
        }

        internal void ApplyLabelHint(ulong agentId, string labelHint)
        {
            if (agentId == 0 || string.IsNullOrEmpty(labelHint)) return;
            if (!_agents.TryGetValue(agentId, out var descriptor))
            {
                descriptor = new AgentDescriptor
                {
                    AgentId = agentId,
                    FirstSeenUtc = DateTime.UtcNow,
                };
            }
            descriptor.Label = labelHint;
            _agents[agentId] = descriptor;
        }

        /// <summary>Replace a leftover "Pending agent (req N)" label after CreateAgentOk.</summary>
        internal void PromotePendingLabel(ulong agentId)
        {
            if (agentId == 0) return;
            if (!_agents.TryGetValue(agentId, out var descriptor)) return;
            if (!IsPendingLabel(descriptor.Label)) return;
            descriptor.Label = FormatDefaultLabel(agentId);
            _agents[agentId] = descriptor;
        }

        static bool IsPendingLabel(string label) =>
            !string.IsNullOrEmpty(label) && label.StartsWith("Pending agent (req ", StringComparison.Ordinal);

        void UpsertFromCreateErr(TryllObservationEnvelope env)
        {
            _pendingCreates.Remove(PendingCreateKey(env.ConnectionEpoch, env.RequestId));
        }

        void MarkDestroyed(ulong agentId)
        {
            if (!_agents.TryGetValue(agentId, out var descriptor))
            {
                descriptor = new AgentDescriptor
                {
                    AgentId = agentId,
                    Label = FormatDefaultLabel(agentId),
                    FirstSeenUtc = DateTime.UtcNow,
                };
            }

            descriptor.Destroyed = true;
            descriptor.DestroyedUtc ??= DateTime.UtcNow;
            _agents[agentId] = descriptor;
        }

        static string FormatDefaultLabel(ulong agentId) => $"Agent {agentId}";

        static (ulong Epoch, ulong RequestId) PendingCreateKey(ulong epoch, ulong requestId)
            => (epoch, requestId);

        readonly Dictionary<(ulong Epoch, ulong RequestId), AgentDescriptor> _pendingCreates
            = new Dictionary<(ulong, ulong), AgentDescriptor>();
    }
}
