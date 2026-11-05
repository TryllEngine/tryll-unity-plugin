// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/messages.fbs  +  tools/codegen/templates/protocol_observation_csharp.cs.j2
// See: docs/dev/codegen.md
//
// One row per MessageBody member: name, direction, inferred scope, and the
// request↔terminal link. Curated-vs-raw exceptions live in the hand-written
// TryllObservationCurated override, not here.

using System.Collections.Generic;
using Tryll;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllMessageDescriptors_gen {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client.Internal
{
    internal readonly struct TryllMessageDescriptor
    {
        public readonly MessageBody Body;
        public readonly string Name;
        public readonly bool ClientToServer;
        public readonly string ScopeKey;
        public readonly MessageBody Terminal;
        public readonly MessageBody Progress;
        public readonly MessageBody Request;
        public readonly string Coalesce;
        public readonly string CoalesceField;

        public TryllMessageDescriptor(
            MessageBody body, string name, bool clientToServer, string scopeKey,
            MessageBody terminal, MessageBody progress, MessageBody request,
            string coalesce, string coalesceField)
        {
            Body = body;
            Name = name;
            ClientToServer = clientToServer;
            ScopeKey = scopeKey;
            Terminal = terminal;
            Progress = progress;
            Request = request;
            Coalesce = coalesce;
            CoalesceField = coalesceField;
        }
    }

    internal static class TryllMessageDescriptors
    {
        public static bool TryGet(MessageBody body, out TryllMessageDescriptor descriptor)
            => ByBody.TryGetValue(body, out descriptor);

        /// <summary>
        /// A unary request whose fields name an agent or a voice input.
        /// Unary means a terminal and no progress frame, so SendMessage is not
        /// included. CreateAgent is not included either: it has no scope key.
        /// Reply rows for these join the outbound request instead of standing alone.
        /// </summary>
        public static bool IsScopedAck(MessageBody request)
        {
            if (!TryGet(request, out var descriptor))
                return false;
            return descriptor.Terminal != MessageBody.NONE
                && descriptor.Progress == MessageBody.NONE
                && !string.IsNullOrEmpty(descriptor.ScopeKey);
        }

        /// <summary>
        /// An agent-scoped request marked (turn: "standalone"). It opens its
        /// own Agent Log turn instead of joining an in-flight send.
        /// </summary>
        public static bool IsStandaloneTurnRequest(MessageBody body)
        {
            switch (body)
            {
                case MessageBody.ChangeAgentParamRequest:
                case MessageBody.PrefillAgentKvCacheRequest:
                case MessageBody.EvictAgentKvCacheRequest:
                case MessageBody.GetAgentKvCacheStatusRequest:
                case MessageBody.AppendInteractionsRequest:
                case MessageBody.RemoveInteractionsFromEndRequest:
                case MessageBody.ExportDialogRequest:
                case MessageBody.ImportDialogRequest:
                case MessageBody.SaveDialogRequest:
                case MessageBody.LoadDialogRequest:
                case MessageBody.SetAgentVoiceRequest:
                    return true;
                default:
                    return false;
            }
        }

        private static readonly Dictionary<MessageBody, TryllMessageDescriptor> ByBody
            = new Dictionary<MessageBody, TryllMessageDescriptor>
        {
            {
                MessageBody.ConnectionReady,
                new TryllMessageDescriptor(
                    MessageBody.ConnectionReady,
                    "ConnectionReady",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateSessionRequest,
                new TryllMessageDescriptor(
                    MessageBody.CreateSessionRequest,
                    "CreateSessionRequest",
                    true,
                    null,
                    MessageBody.CreateSessionResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateSessionResponse,
                new TryllMessageDescriptor(
                    MessageBody.CreateSessionResponse,
                    "CreateSessionResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.CreateSessionRequest,
                    "",
                    "")
            },
            {
                MessageBody.CreateAgentRequest,
                new TryllMessageDescriptor(
                    MessageBody.CreateAgentRequest,
                    "CreateAgentRequest",
                    true,
                    null,
                    MessageBody.CreateAgentResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateAgentResponse,
                new TryllMessageDescriptor(
                    MessageBody.CreateAgentResponse,
                    "CreateAgentResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.CreateAgentRequest,
                    "",
                    "")
            },
            {
                MessageBody.SendMessageRequest,
                new TryllMessageDescriptor(
                    MessageBody.SendMessageRequest,
                    "SendMessageRequest",
                    true,
                    "agent_id",
                    MessageBody.TurnComplete,
                    MessageBody.AnswerText,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.DestroyAgentRequest,
                new TryllMessageDescriptor(
                    MessageBody.DestroyAgentRequest,
                    "DestroyAgentRequest",
                    true,
                    "agent_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.AnswerText,
                new TryllMessageDescriptor(
                    MessageBody.AnswerText,
                    "AnswerText",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.SendMessageRequest,
                    "concat",
                    "text")
            },
            {
                MessageBody.TurnComplete,
                new TryllMessageDescriptor(
                    MessageBody.TurnComplete,
                    "TurnComplete",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.SendMessageRequest,
                    "",
                    "")
            },
            {
                MessageBody.ErrorResponse,
                new TryllMessageDescriptor(
                    MessageBody.ErrorResponse,
                    "ErrorResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.Ack,
                new TryllMessageDescriptor(
                    MessageBody.Ack,
                    "Ack",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ListModelsRequest,
                new TryllMessageDescriptor(
                    MessageBody.ListModelsRequest,
                    "ListModelsRequest",
                    true,
                    null,
                    MessageBody.ListModelsResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ListModelsResponse,
                new TryllMessageDescriptor(
                    MessageBody.ListModelsResponse,
                    "ListModelsResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.ListModelsRequest,
                    "",
                    "")
            },
            {
                MessageBody.DownloadModelRequest,
                new TryllMessageDescriptor(
                    MessageBody.DownloadModelRequest,
                    "DownloadModelRequest",
                    true,
                    null,
                    MessageBody.DownloadComplete,
                    MessageBody.DownloadProgress,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.DownloadProgress,
                new TryllMessageDescriptor(
                    MessageBody.DownloadProgress,
                    "DownloadProgress",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.DownloadModelRequest,
                    "last",
                    "")
            },
            {
                MessageBody.DownloadComplete,
                new TryllMessageDescriptor(
                    MessageBody.DownloadComplete,
                    "DownloadComplete",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.DownloadModelRequest,
                    "",
                    "")
            },
            {
                MessageBody.CreateStringStorageRequest,
                new TryllMessageDescriptor(
                    MessageBody.CreateStringStorageRequest,
                    "CreateStringStorageRequest",
                    true,
                    null,
                    MessageBody.CreateStringStorageResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateStringStorageResponse,
                new TryllMessageDescriptor(
                    MessageBody.CreateStringStorageResponse,
                    "CreateStringStorageResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.CreateStringStorageRequest,
                    "",
                    "")
            },
            {
                MessageBody.DestroyStringStorageRequest,
                new TryllMessageDescriptor(
                    MessageBody.DestroyStringStorageRequest,
                    "DestroyStringStorageRequest",
                    true,
                    null,
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.NodeEvent,
                new TryllMessageDescriptor(
                    MessageBody.NodeEvent,
                    "NodeEvent",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.LoadModelRequest,
                new TryllMessageDescriptor(
                    MessageBody.LoadModelRequest,
                    "LoadModelRequest",
                    true,
                    null,
                    MessageBody.LoadModelResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.LoadModelResponse,
                new TryllMessageDescriptor(
                    MessageBody.LoadModelResponse,
                    "LoadModelResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.LoadModelRequest,
                    "",
                    "")
            },
            {
                MessageBody.UnloadModelRequest,
                new TryllMessageDescriptor(
                    MessageBody.UnloadModelRequest,
                    "UnloadModelRequest",
                    true,
                    null,
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateEmbeddedStringStorageRequest,
                new TryllMessageDescriptor(
                    MessageBody.CreateEmbeddedStringStorageRequest,
                    "CreateEmbeddedStringStorageRequest",
                    true,
                    null,
                    MessageBody.CreateEmbeddedStringStorageResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateEmbeddedStringStorageResponse,
                new TryllMessageDescriptor(
                    MessageBody.CreateEmbeddedStringStorageResponse,
                    "CreateEmbeddedStringStorageResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.CreateEmbeddedStringStorageRequest,
                    "",
                    "")
            },
            {
                MessageBody.DestroyEmbeddedStringStorageRequest,
                new TryllMessageDescriptor(
                    MessageBody.DestroyEmbeddedStringStorageRequest,
                    "DestroyEmbeddedStringStorageRequest",
                    true,
                    null,
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ChangeAgentParamRequest,
                new TryllMessageDescriptor(
                    MessageBody.ChangeAgentParamRequest,
                    "ChangeAgentParamRequest",
                    true,
                    "agent_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateVoiceInputRequest,
                new TryllMessageDescriptor(
                    MessageBody.CreateVoiceInputRequest,
                    "CreateVoiceInputRequest",
                    true,
                    null,
                    MessageBody.CreateVoiceInputResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CreateVoiceInputResponse,
                new TryllMessageDescriptor(
                    MessageBody.CreateVoiceInputResponse,
                    "CreateVoiceInputResponse",
                    false,
                    "voice_input_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.CreateVoiceInputRequest,
                    "",
                    "")
            },
            {
                MessageBody.BeginUtteranceRequest,
                new TryllMessageDescriptor(
                    MessageBody.BeginUtteranceRequest,
                    "BeginUtteranceRequest",
                    true,
                    "voice_input_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.AudioBuffer,
                new TryllMessageDescriptor(
                    MessageBody.AudioBuffer,
                    "AudioBuffer",
                    true,
                    "voice_input_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "sum",
                    "")
            },
            {
                MessageBody.EndUtteranceRequest,
                new TryllMessageDescriptor(
                    MessageBody.EndUtteranceRequest,
                    "EndUtteranceRequest",
                    true,
                    "voice_input_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.CancelUtteranceRequest,
                new TryllMessageDescriptor(
                    MessageBody.CancelUtteranceRequest,
                    "CancelUtteranceRequest",
                    true,
                    "voice_input_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.WireTranscriptUpdate,
                new TryllMessageDescriptor(
                    MessageBody.WireTranscriptUpdate,
                    "WireTranscriptUpdate",
                    false,
                    "voice_input_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.DestroyVoiceInputRequest,
                new TryllMessageDescriptor(
                    MessageBody.DestroyVoiceInputRequest,
                    "DestroyVoiceInputRequest",
                    true,
                    "voice_input_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.TtsAudioFormatFrame,
                new TryllMessageDescriptor(
                    MessageBody.TtsAudioFormatFrame,
                    "TtsAudioFormatFrame",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.TtsAudioFrame,
                new TryllMessageDescriptor(
                    MessageBody.TtsAudioFrame,
                    "TtsAudioFrame",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "sum",
                    "")
            },
            {
                MessageBody.DeleteModelRequest,
                new TryllMessageDescriptor(
                    MessageBody.DeleteModelRequest,
                    "DeleteModelRequest",
                    true,
                    null,
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.DebugCommandRequest,
                new TryllMessageDescriptor(
                    MessageBody.DebugCommandRequest,
                    "DebugCommandRequest",
                    true,
                    null,
                    MessageBody.DebugCommandResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.DebugCommandResponse,
                new TryllMessageDescriptor(
                    MessageBody.DebugCommandResponse,
                    "DebugCommandResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.DebugCommandRequest,
                    "",
                    "")
            },
            {
                MessageBody.CancelRequest,
                new TryllMessageDescriptor(
                    MessageBody.CancelRequest,
                    "CancelRequest",
                    true,
                    "agent_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ResumeAgentRequest,
                new TryllMessageDescriptor(
                    MessageBody.ResumeAgentRequest,
                    "ResumeAgentRequest",
                    true,
                    "agent_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.UpdateAgentVariablesRequest,
                new TryllMessageDescriptor(
                    MessageBody.UpdateAgentVariablesRequest,
                    "UpdateAgentVariablesRequest",
                    true,
                    "agent_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.GetAgentVariablesRequest,
                new TryllMessageDescriptor(
                    MessageBody.GetAgentVariablesRequest,
                    "GetAgentVariablesRequest",
                    true,
                    "agent_id",
                    MessageBody.GetAgentVariablesResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.GetAgentVariablesResponse,
                new TryllMessageDescriptor(
                    MessageBody.GetAgentVariablesResponse,
                    "GetAgentVariablesResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.GetAgentVariablesRequest,
                    "",
                    "")
            },
            {
                MessageBody.PrefillAgentKvCacheRequest,
                new TryllMessageDescriptor(
                    MessageBody.PrefillAgentKvCacheRequest,
                    "PrefillAgentKvCacheRequest",
                    true,
                    "agent_id",
                    MessageBody.PrefillAgentKvCacheResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.PrefillAgentKvCacheResponse,
                new TryllMessageDescriptor(
                    MessageBody.PrefillAgentKvCacheResponse,
                    "PrefillAgentKvCacheResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.PrefillAgentKvCacheRequest,
                    "",
                    "")
            },
            {
                MessageBody.EvictAgentKvCacheRequest,
                new TryllMessageDescriptor(
                    MessageBody.EvictAgentKvCacheRequest,
                    "EvictAgentKvCacheRequest",
                    true,
                    "agent_id",
                    MessageBody.EvictAgentKvCacheResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.EvictAgentKvCacheResponse,
                new TryllMessageDescriptor(
                    MessageBody.EvictAgentKvCacheResponse,
                    "EvictAgentKvCacheResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.EvictAgentKvCacheRequest,
                    "",
                    "")
            },
            {
                MessageBody.GetAgentKvCacheStatusRequest,
                new TryllMessageDescriptor(
                    MessageBody.GetAgentKvCacheStatusRequest,
                    "GetAgentKvCacheStatusRequest",
                    true,
                    "agent_id",
                    MessageBody.GetAgentKvCacheStatusResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.GetAgentKvCacheStatusResponse,
                new TryllMessageDescriptor(
                    MessageBody.GetAgentKvCacheStatusResponse,
                    "GetAgentKvCacheStatusResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.GetAgentKvCacheStatusRequest,
                    "",
                    "")
            },
            {
                MessageBody.AppendInteractionsRequest,
                new TryllMessageDescriptor(
                    MessageBody.AppendInteractionsRequest,
                    "AppendInteractionsRequest",
                    true,
                    "agent_id",
                    MessageBody.AppendInteractionsResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.AppendInteractionsResponse,
                new TryllMessageDescriptor(
                    MessageBody.AppendInteractionsResponse,
                    "AppendInteractionsResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.AppendInteractionsRequest,
                    "",
                    "")
            },
            {
                MessageBody.RemoveInteractionsFromEndRequest,
                new TryllMessageDescriptor(
                    MessageBody.RemoveInteractionsFromEndRequest,
                    "RemoveInteractionsFromEndRequest",
                    true,
                    "agent_id",
                    MessageBody.RemoveInteractionsFromEndResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.RemoveInteractionsFromEndResponse,
                new TryllMessageDescriptor(
                    MessageBody.RemoveInteractionsFromEndResponse,
                    "RemoveInteractionsFromEndResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.RemoveInteractionsFromEndRequest,
                    "",
                    "")
            },
            {
                MessageBody.SetInferenceThrottle,
                new TryllMessageDescriptor(
                    MessageBody.SetInferenceThrottle,
                    "SetInferenceThrottle",
                    true,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.SetSessionLocaleRequest,
                new TryllMessageDescriptor(
                    MessageBody.SetSessionLocaleRequest,
                    "SetSessionLocaleRequest",
                    true,
                    null,
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ExportDialogRequest,
                new TryllMessageDescriptor(
                    MessageBody.ExportDialogRequest,
                    "ExportDialogRequest",
                    true,
                    "agent_id",
                    MessageBody.ExportDialogResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ExportDialogResponse,
                new TryllMessageDescriptor(
                    MessageBody.ExportDialogResponse,
                    "ExportDialogResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.ExportDialogRequest,
                    "",
                    "")
            },
            {
                MessageBody.ImportDialogRequest,
                new TryllMessageDescriptor(
                    MessageBody.ImportDialogRequest,
                    "ImportDialogRequest",
                    true,
                    "agent_id",
                    MessageBody.ImportDialogResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ImportDialogResponse,
                new TryllMessageDescriptor(
                    MessageBody.ImportDialogResponse,
                    "ImportDialogResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.ImportDialogRequest,
                    "",
                    "")
            },
            {
                MessageBody.GetMemoryConsumptionRequest,
                new TryllMessageDescriptor(
                    MessageBody.GetMemoryConsumptionRequest,
                    "GetMemoryConsumptionRequest",
                    true,
                    null,
                    MessageBody.GetMemoryConsumptionResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.GetMemoryConsumptionResponse,
                new TryllMessageDescriptor(
                    MessageBody.GetMemoryConsumptionResponse,
                    "GetMemoryConsumptionResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.GetMemoryConsumptionRequest,
                    "",
                    "")
            },
            {
                MessageBody.EnsureMutableKbRequest,
                new TryllMessageDescriptor(
                    MessageBody.EnsureMutableKbRequest,
                    "EnsureMutableKbRequest",
                    true,
                    null,
                    MessageBody.EnsureMutableKbResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.EnsureMutableKbResponse,
                new TryllMessageDescriptor(
                    MessageBody.EnsureMutableKbResponse,
                    "EnsureMutableKbResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.EnsureMutableKbRequest,
                    "",
                    "")
            },
            {
                MessageBody.EnumerateMutableKbsRequest,
                new TryllMessageDescriptor(
                    MessageBody.EnumerateMutableKbsRequest,
                    "EnumerateMutableKbsRequest",
                    true,
                    null,
                    MessageBody.EnumerateMutableKbsResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.EnumerateMutableKbsResponse,
                new TryllMessageDescriptor(
                    MessageBody.EnumerateMutableKbsResponse,
                    "EnumerateMutableKbsResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.EnumerateMutableKbsRequest,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbWriteRequest,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbWriteRequest,
                    "MutableKbWriteRequest",
                    true,
                    null,
                    MessageBody.MutableKbWriteResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbWriteResponse,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbWriteResponse,
                    "MutableKbWriteResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.MutableKbWriteRequest,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbDeleteRequest,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbDeleteRequest,
                    "MutableKbDeleteRequest",
                    true,
                    null,
                    MessageBody.MutableKbDeleteResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbDeleteResponse,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbDeleteResponse,
                    "MutableKbDeleteResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.MutableKbDeleteRequest,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbClearRequest,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbClearRequest,
                    "MutableKbClearRequest",
                    true,
                    null,
                    MessageBody.MutableKbClearResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbClearResponse,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbClearResponse,
                    "MutableKbClearResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.MutableKbClearRequest,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbQueryRequest,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbQueryRequest,
                    "MutableKbQueryRequest",
                    true,
                    null,
                    MessageBody.MutableKbQueryResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbQueryResponse,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbQueryResponse,
                    "MutableKbQueryResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.MutableKbQueryRequest,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbGetRequest,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbGetRequest,
                    "MutableKbGetRequest",
                    true,
                    null,
                    MessageBody.MutableKbGetResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbGetResponse,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbGetResponse,
                    "MutableKbGetResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.MutableKbGetRequest,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbListRequest,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbListRequest,
                    "MutableKbListRequest",
                    true,
                    null,
                    MessageBody.MutableKbListResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbListResponse,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbListResponse,
                    "MutableKbListResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.MutableKbListRequest,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbStatsRequest,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbStatsRequest,
                    "MutableKbStatsRequest",
                    true,
                    null,
                    MessageBody.MutableKbStatsResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.MutableKbStatsResponse,
                new TryllMessageDescriptor(
                    MessageBody.MutableKbStatsResponse,
                    "MutableKbStatsResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.MutableKbStatsRequest,
                    "",
                    "")
            },
            {
                MessageBody.ExportMutableKbRequest,
                new TryllMessageDescriptor(
                    MessageBody.ExportMutableKbRequest,
                    "ExportMutableKbRequest",
                    true,
                    null,
                    MessageBody.ExportMutableKbResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ExportMutableKbResponse,
                new TryllMessageDescriptor(
                    MessageBody.ExportMutableKbResponse,
                    "ExportMutableKbResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.ExportMutableKbRequest,
                    "",
                    "")
            },
            {
                MessageBody.ImportMutableKbRequest,
                new TryllMessageDescriptor(
                    MessageBody.ImportMutableKbRequest,
                    "ImportMutableKbRequest",
                    true,
                    null,
                    MessageBody.ImportMutableKbResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.ImportMutableKbResponse,
                new TryllMessageDescriptor(
                    MessageBody.ImportMutableKbResponse,
                    "ImportMutableKbResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.ImportMutableKbRequest,
                    "",
                    "")
            },
            {
                MessageBody.SaveGameMemoryRequest,
                new TryllMessageDescriptor(
                    MessageBody.SaveGameMemoryRequest,
                    "SaveGameMemoryRequest",
                    true,
                    null,
                    MessageBody.SaveGameMemoryResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.SaveGameMemoryResponse,
                new TryllMessageDescriptor(
                    MessageBody.SaveGameMemoryResponse,
                    "SaveGameMemoryResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.SaveGameMemoryRequest,
                    "",
                    "")
            },
            {
                MessageBody.LoadGameMemoryRequest,
                new TryllMessageDescriptor(
                    MessageBody.LoadGameMemoryRequest,
                    "LoadGameMemoryRequest",
                    true,
                    null,
                    MessageBody.LoadGameMemoryResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.LoadGameMemoryResponse,
                new TryllMessageDescriptor(
                    MessageBody.LoadGameMemoryResponse,
                    "LoadGameMemoryResponse",
                    false,
                    null,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.LoadGameMemoryRequest,
                    "",
                    "")
            },
            {
                MessageBody.SaveDialogRequest,
                new TryllMessageDescriptor(
                    MessageBody.SaveDialogRequest,
                    "SaveDialogRequest",
                    true,
                    "agent_id",
                    MessageBody.SaveDialogResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.SaveDialogResponse,
                new TryllMessageDescriptor(
                    MessageBody.SaveDialogResponse,
                    "SaveDialogResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.SaveDialogRequest,
                    "",
                    "")
            },
            {
                MessageBody.LoadDialogRequest,
                new TryllMessageDescriptor(
                    MessageBody.LoadDialogRequest,
                    "LoadDialogRequest",
                    true,
                    "agent_id",
                    MessageBody.LoadDialogResponse,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.LoadDialogResponse,
                new TryllMessageDescriptor(
                    MessageBody.LoadDialogResponse,
                    "LoadDialogResponse",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.LoadDialogRequest,
                    "",
                    "")
            },
            {
                MessageBody.TtsVisemeFrame,
                new TryllMessageDescriptor(
                    MessageBody.TtsVisemeFrame,
                    "TtsVisemeFrame",
                    false,
                    "agent_id",
                    MessageBody.NONE,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "sum",
                    "")
            },
            {
                MessageBody.SetAgentVoiceRequest,
                new TryllMessageDescriptor(
                    MessageBody.SetAgentVoiceRequest,
                    "SetAgentVoiceRequest",
                    true,
                    "agent_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
            {
                MessageBody.SetAgentDeliveryModeRequest,
                new TryllMessageDescriptor(
                    MessageBody.SetAgentDeliveryModeRequest,
                    "SetAgentDeliveryModeRequest",
                    true,
                    "agent_id",
                    MessageBody.Ack,
                    MessageBody.NONE,
                    MessageBody.NONE,
                    "",
                    "")
            },
        };
    }
}
