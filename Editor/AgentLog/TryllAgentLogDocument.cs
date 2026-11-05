// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor.AgentLog
{
    /// <summary>
    /// Fills an export snapshot from the editor journal. The document writer lives in the runtime.
    /// </summary>
    internal static class TryllAgentLogExport
    {
        public static TryllAgentLogExportSnapshot FromStore(
            AgentLogStore store, ulong selectedAgentId, Tryll.Client.TryllClient client)
        {
            var snap = new TryllAgentLogExportSnapshot
            {
                HostVersion = UnityEngine.Application.unityVersion ?? "",
                ProjectName = UnityEditor.PlayerSettings.productName ?? "",
                Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription ?? "",
                CaptureState = CaptureName(store.State),
                HistoryDiscarded = store.HistoryWasDiscardedByReload,
                SelectedAgentId = selectedAgentId,
                MaxTurns = store.Config.MaxCompletedTurnsPerAgent,
                MaxObservations = store.Config.MaxObservationsPerAgent,
                MaxRaw = store.Config.MaxRawSessionObservations,
                EvictedTurns = store.EvictedTurns,
                EvictedObservations = store.EvictedObservations,
            };
            if (client != null)
            {
                snap.ServerVersion = client.ServerVersion ?? "";
                snap.ServerRunId = client.ServerRunId ?? "";
                snap.ServerBuildConfig = client.ServerBuildConfig ?? "";
                snap.ServerMode = client.ServerMode ?? "";
            }
            foreach (var env in store.Journal)
                snap.Events.Add(env);
            snap.Agents = AgentsFrom(store, snap.Events);
            return snap;
        }

        static List<TryllAgentLogExportAgent> AgentsFrom(
            AgentLogStore store, List<TryllObservationEnvelope> events)
        {
            var agents = new List<TryllAgentLogExportAgent>();
            var seen = new HashSet<ulong>();
            foreach (var pair in store.Descriptors.All)
            {
                if (pair.Key == 0) continue;
                seen.Add(pair.Key);
                agents.Add(Describe(pair.Value, events, store.GetCompletedTurnCount(pair.Key)));
            }
            foreach (var env in events)
            {
                if (env.AgentId == 0 || !seen.Add(env.AgentId)) continue;
                agents.Add(Describe(null, events, store.GetCompletedTurnCount(env.AgentId), env.AgentId));
            }
            return agents;
        }

        static TryllAgentLogExportAgent Describe(
            AgentDescriptor descriptor, List<TryllObservationEnvelope> events, int completed, ulong fallbackId = 0)
        {
            var id = descriptor != null ? descriptor.AgentId : fallbackId;
            var agent = new TryllAgentLogExportAgent
            {
                AgentId = id,
                Label = descriptor != null ? descriptor.Label ?? "" : "",
                Diagnostics = descriptor != null && descriptor.DiagnosticsAvailable,
                CreatedBeforeDiagnosticsCapture = descriptor != null && descriptor.CreatedBeforeDiagnosticsCapture,
                CompletedTurns = completed,
            };
            foreach (var env in events)
            {
                if (env.AgentId != id) continue;
                if (agent.Epoch == 0) agent.Epoch = env.ConnectionEpoch;
                if (!agent.CreatedBeforeDiagnosticsCapture
                    && !agent.HasCreatedSeq
                    && env.IsRequest(Tryll.MessageBody.CreateAgentRequest))
                {
                    agent.HasCreatedSeq = true;
                    agent.CreatedSeq = env.EventId;
                }
                if (env.IsRequest(Tryll.MessageBody.DestroyAgentRequest))
                {
                    agent.HasDestroyedSeq = true;
                    agent.DestroyedSeq = env.EventId;
                }
            }
            if (!agent.CreatedBeforeDiagnosticsCapture && !agent.HasCreatedSeq)
            {
                foreach (var env in events)
                {
                    if (env.AgentId != id) continue;
                    agent.HasCreatedSeq = true;
                    agent.CreatedSeq = env.EventId;
                    break;
                }
            }
            return agent;
        }

        static string CaptureName(AgentLogStore.CaptureState state)
        {
            switch (state)
            {
            case AgentLogStore.CaptureState.Capturing: return "capturing";
            case AgentLogStore.CaptureState.PausedUnsubscribed: return "paused";
            case AgentLogStore.CaptureState.HistoryDiscardedByReload: return "discarded";
            default: return "idle";
            }
        }
    }
}
