// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/*.fbs + tools/codegen/agent_log_emit.py
// See: docs/dev/codegen.md

using System;
using System.Collections.Generic;
using System.Text;
using Google.FlatBuffers;
using Tryll;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllAgentLogJson_gen {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client.Internal
{
    internal sealed class TryllLogField
    {
        public string Name;
        public string Kind;
        public string Doc;
        public string DefaultText;
        public string[] EnumNames;
        public string Log;
        public string Hint;
        public string TypeName;
        public string ElementKind;
        public string ElementType;
    }

    internal sealed class TryllLogTable
    {
        public string Name;
        public string Label;
        public string Coalesce;
        public string CoalesceField;
        public bool HasNodeName;
        public bool HasIsDelta;
        public TryllLogField[] Fields;
    }

    internal static class TryllAgentLogJson
    {
        public static bool TryGetTable(string name, out TryllLogTable table)
            => Tables.TryGetValue(name, out table);

        public static void WriteMessage(TryllJsonWriter w, byte[] frame, IReadOnlyList<TryllOmittedField> omitted)
        {
            var map = Index(omitted);
            var bb = new ByteBuffer(frame);
            var msg = Message.GetRootAsMessage(bb);
            var pos = TryllFbNav.Root(bb);
            var bodyPos = TryllFbNav.Child(bb, pos, 6);
            switch (msg.BodyType)
            {
                case MessageBody.ConnectionReady:
                    Write_Tryll_ConnectionReady(w, msg.BodyAsConnectionReady(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateSessionRequest:
                    Write_Tryll_CreateSessionRequest(w, msg.BodyAsCreateSessionRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateSessionResponse:
                    Write_Tryll_CreateSessionResponse(w, msg.BodyAsCreateSessionResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateAgentRequest:
                    Write_Tryll_CreateAgentRequest(w, msg.BodyAsCreateAgentRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateAgentResponse:
                    Write_Tryll_CreateAgentResponse(w, msg.BodyAsCreateAgentResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SendMessageRequest:
                    Write_Tryll_SendMessageRequest(w, msg.BodyAsSendMessageRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DestroyAgentRequest:
                    Write_Tryll_DestroyAgentRequest(w, msg.BodyAsDestroyAgentRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.AnswerText:
                    Write_Tryll_AnswerText(w, msg.BodyAsAnswerText(), bodyPos, bb, "", map);
                    break;
                case MessageBody.TurnComplete:
                    Write_Tryll_TurnComplete(w, msg.BodyAsTurnComplete(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ErrorResponse:
                    Write_Tryll_ErrorResponse(w, msg.BodyAsErrorResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.Ack:
                    Write_Tryll_Ack(w, msg.BodyAsAck(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ListModelsRequest:
                    Write_Tryll_ListModelsRequest(w, msg.BodyAsListModelsRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ListModelsResponse:
                    Write_Tryll_ListModelsResponse(w, msg.BodyAsListModelsResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DownloadModelRequest:
                    Write_Tryll_DownloadModelRequest(w, msg.BodyAsDownloadModelRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DownloadProgress:
                    Write_Tryll_DownloadProgress(w, msg.BodyAsDownloadProgress(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DownloadComplete:
                    Write_Tryll_DownloadComplete(w, msg.BodyAsDownloadComplete(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateStringStorageRequest:
                    Write_Tryll_CreateStringStorageRequest(w, msg.BodyAsCreateStringStorageRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateStringStorageResponse:
                    Write_Tryll_CreateStringStorageResponse(w, msg.BodyAsCreateStringStorageResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DestroyStringStorageRequest:
                    Write_Tryll_DestroyStringStorageRequest(w, msg.BodyAsDestroyStringStorageRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.NodeEvent:
                    Write_Tryll_NodeEvent(w, msg.BodyAsNodeEvent(), bodyPos, bb, "", map);
                    break;
                case MessageBody.LoadModelRequest:
                    Write_Tryll_LoadModelRequest(w, msg.BodyAsLoadModelRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.LoadModelResponse:
                    Write_Tryll_LoadModelResponse(w, msg.BodyAsLoadModelResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.UnloadModelRequest:
                    Write_Tryll_UnloadModelRequest(w, msg.BodyAsUnloadModelRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateEmbeddedStringStorageRequest:
                    Write_Tryll_CreateEmbeddedStringStorageRequest(w, msg.BodyAsCreateEmbeddedStringStorageRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateEmbeddedStringStorageResponse:
                    Write_Tryll_CreateEmbeddedStringStorageResponse(w, msg.BodyAsCreateEmbeddedStringStorageResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DestroyEmbeddedStringStorageRequest:
                    Write_Tryll_DestroyEmbeddedStringStorageRequest(w, msg.BodyAsDestroyEmbeddedStringStorageRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ChangeAgentParamRequest:
                    Write_Tryll_ChangeAgentParamRequest(w, msg.BodyAsChangeAgentParamRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateVoiceInputRequest:
                    Write_Tryll_CreateVoiceInputRequest(w, msg.BodyAsCreateVoiceInputRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CreateVoiceInputResponse:
                    Write_Tryll_CreateVoiceInputResponse(w, msg.BodyAsCreateVoiceInputResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.BeginUtteranceRequest:
                    Write_Tryll_BeginUtteranceRequest(w, msg.BodyAsBeginUtteranceRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.AudioBuffer:
                    Write_Tryll_AudioBuffer(w, msg.BodyAsAudioBuffer(), bodyPos, bb, "", map);
                    break;
                case MessageBody.EndUtteranceRequest:
                    Write_Tryll_EndUtteranceRequest(w, msg.BodyAsEndUtteranceRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CancelUtteranceRequest:
                    Write_Tryll_CancelUtteranceRequest(w, msg.BodyAsCancelUtteranceRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.WireTranscriptUpdate:
                    Write_Tryll_WireTranscriptUpdate(w, msg.BodyAsWireTranscriptUpdate(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DestroyVoiceInputRequest:
                    Write_Tryll_DestroyVoiceInputRequest(w, msg.BodyAsDestroyVoiceInputRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.TtsAudioFormatFrame:
                    Write_Tryll_TtsAudioFormatFrame(w, msg.BodyAsTtsAudioFormatFrame(), bodyPos, bb, "", map);
                    break;
                case MessageBody.TtsAudioFrame:
                    Write_Tryll_TtsAudioFrame(w, msg.BodyAsTtsAudioFrame(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DeleteModelRequest:
                    Write_Tryll_DeleteModelRequest(w, msg.BodyAsDeleteModelRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DebugCommandRequest:
                    Write_Tryll_DebugCommandRequest(w, msg.BodyAsDebugCommandRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.DebugCommandResponse:
                    Write_Tryll_DebugCommandResponse(w, msg.BodyAsDebugCommandResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.CancelRequest:
                    Write_Tryll_CancelRequest(w, msg.BodyAsCancelRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ResumeAgentRequest:
                    Write_Tryll_ResumeAgentRequest(w, msg.BodyAsResumeAgentRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.UpdateAgentVariablesRequest:
                    Write_Tryll_UpdateAgentVariablesRequest(w, msg.BodyAsUpdateAgentVariablesRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.GetAgentVariablesRequest:
                    Write_Tryll_GetAgentVariablesRequest(w, msg.BodyAsGetAgentVariablesRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.GetAgentVariablesResponse:
                    Write_Tryll_GetAgentVariablesResponse(w, msg.BodyAsGetAgentVariablesResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.PrefillAgentKvCacheRequest:
                    Write_Tryll_PrefillAgentKvCacheRequest(w, msg.BodyAsPrefillAgentKvCacheRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.PrefillAgentKvCacheResponse:
                    Write_Tryll_PrefillAgentKvCacheResponse(w, msg.BodyAsPrefillAgentKvCacheResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.EvictAgentKvCacheRequest:
                    Write_Tryll_EvictAgentKvCacheRequest(w, msg.BodyAsEvictAgentKvCacheRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.EvictAgentKvCacheResponse:
                    Write_Tryll_EvictAgentKvCacheResponse(w, msg.BodyAsEvictAgentKvCacheResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.GetAgentKvCacheStatusRequest:
                    Write_Tryll_GetAgentKvCacheStatusRequest(w, msg.BodyAsGetAgentKvCacheStatusRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.GetAgentKvCacheStatusResponse:
                    Write_Tryll_GetAgentKvCacheStatusResponse(w, msg.BodyAsGetAgentKvCacheStatusResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.AppendInteractionsRequest:
                    Write_Tryll_AppendInteractionsRequest(w, msg.BodyAsAppendInteractionsRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.AppendInteractionsResponse:
                    Write_Tryll_AppendInteractionsResponse(w, msg.BodyAsAppendInteractionsResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.RemoveInteractionsFromEndRequest:
                    Write_Tryll_RemoveInteractionsFromEndRequest(w, msg.BodyAsRemoveInteractionsFromEndRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.RemoveInteractionsFromEndResponse:
                    Write_Tryll_RemoveInteractionsFromEndResponse(w, msg.BodyAsRemoveInteractionsFromEndResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SetInferenceThrottle:
                    Write_Tryll_SetInferenceThrottle(w, msg.BodyAsSetInferenceThrottle(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SetSessionLocaleRequest:
                    Write_Tryll_SetSessionLocaleRequest(w, msg.BodyAsSetSessionLocaleRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ExportDialogRequest:
                    Write_Tryll_ExportDialogRequest(w, msg.BodyAsExportDialogRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ExportDialogResponse:
                    Write_Tryll_ExportDialogResponse(w, msg.BodyAsExportDialogResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ImportDialogRequest:
                    Write_Tryll_ImportDialogRequest(w, msg.BodyAsImportDialogRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ImportDialogResponse:
                    Write_Tryll_ImportDialogResponse(w, msg.BodyAsImportDialogResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.GetMemoryConsumptionRequest:
                    Write_Tryll_GetMemoryConsumptionRequest(w, msg.BodyAsGetMemoryConsumptionRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.GetMemoryConsumptionResponse:
                    Write_Tryll_GetMemoryConsumptionResponse(w, msg.BodyAsGetMemoryConsumptionResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.EnsureMutableKbRequest:
                    Write_Tryll_EnsureMutableKbRequest(w, msg.BodyAsEnsureMutableKbRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.EnsureMutableKbResponse:
                    Write_Tryll_EnsureMutableKbResponse(w, msg.BodyAsEnsureMutableKbResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.EnumerateMutableKbsRequest:
                    Write_Tryll_EnumerateMutableKbsRequest(w, msg.BodyAsEnumerateMutableKbsRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.EnumerateMutableKbsResponse:
                    Write_Tryll_EnumerateMutableKbsResponse(w, msg.BodyAsEnumerateMutableKbsResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbWriteRequest:
                    Write_Tryll_MutableKbWriteRequest(w, msg.BodyAsMutableKbWriteRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbWriteResponse:
                    Write_Tryll_MutableKbWriteResponse(w, msg.BodyAsMutableKbWriteResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbDeleteRequest:
                    Write_Tryll_MutableKbDeleteRequest(w, msg.BodyAsMutableKbDeleteRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbDeleteResponse:
                    Write_Tryll_MutableKbDeleteResponse(w, msg.BodyAsMutableKbDeleteResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbClearRequest:
                    Write_Tryll_MutableKbClearRequest(w, msg.BodyAsMutableKbClearRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbClearResponse:
                    Write_Tryll_MutableKbClearResponse(w, msg.BodyAsMutableKbClearResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbQueryRequest:
                    Write_Tryll_MutableKbQueryRequest(w, msg.BodyAsMutableKbQueryRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbQueryResponse:
                    Write_Tryll_MutableKbQueryResponse(w, msg.BodyAsMutableKbQueryResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbGetRequest:
                    Write_Tryll_MutableKbGetRequest(w, msg.BodyAsMutableKbGetRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbGetResponse:
                    Write_Tryll_MutableKbGetResponse(w, msg.BodyAsMutableKbGetResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbListRequest:
                    Write_Tryll_MutableKbListRequest(w, msg.BodyAsMutableKbListRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbListResponse:
                    Write_Tryll_MutableKbListResponse(w, msg.BodyAsMutableKbListResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbStatsRequest:
                    Write_Tryll_MutableKbStatsRequest(w, msg.BodyAsMutableKbStatsRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.MutableKbStatsResponse:
                    Write_Tryll_MutableKbStatsResponse(w, msg.BodyAsMutableKbStatsResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ExportMutableKbRequest:
                    Write_Tryll_ExportMutableKbRequest(w, msg.BodyAsExportMutableKbRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ExportMutableKbResponse:
                    Write_Tryll_ExportMutableKbResponse(w, msg.BodyAsExportMutableKbResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ImportMutableKbRequest:
                    Write_Tryll_ImportMutableKbRequest(w, msg.BodyAsImportMutableKbRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.ImportMutableKbResponse:
                    Write_Tryll_ImportMutableKbResponse(w, msg.BodyAsImportMutableKbResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SaveGameMemoryRequest:
                    Write_Tryll_SaveGameMemoryRequest(w, msg.BodyAsSaveGameMemoryRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SaveGameMemoryResponse:
                    Write_Tryll_SaveGameMemoryResponse(w, msg.BodyAsSaveGameMemoryResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.LoadGameMemoryRequest:
                    Write_Tryll_LoadGameMemoryRequest(w, msg.BodyAsLoadGameMemoryRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.LoadGameMemoryResponse:
                    Write_Tryll_LoadGameMemoryResponse(w, msg.BodyAsLoadGameMemoryResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SaveDialogRequest:
                    Write_Tryll_SaveDialogRequest(w, msg.BodyAsSaveDialogRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SaveDialogResponse:
                    Write_Tryll_SaveDialogResponse(w, msg.BodyAsSaveDialogResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.LoadDialogRequest:
                    Write_Tryll_LoadDialogRequest(w, msg.BodyAsLoadDialogRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.LoadDialogResponse:
                    Write_Tryll_LoadDialogResponse(w, msg.BodyAsLoadDialogResponse(), bodyPos, bb, "", map);
                    break;
                case MessageBody.TtsVisemeFrame:
                    Write_Tryll_TtsVisemeFrame(w, msg.BodyAsTtsVisemeFrame(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SetAgentVoiceRequest:
                    Write_Tryll_SetAgentVoiceRequest(w, msg.BodyAsSetAgentVoiceRequest(), bodyPos, bb, "", map);
                    break;
                case MessageBody.SetAgentDeliveryModeRequest:
                    Write_Tryll_SetAgentDeliveryModeRequest(w, msg.BodyAsSetAgentDeliveryModeRequest(), bodyPos, bb, "", map);
                    break;
                default:
                    w.BeginObject();
                    w.EndObject();
                    break;
            }
        }

        public static void WriteLocal(TryllJsonWriter w, byte[] frame, IReadOnlyList<TryllOmittedField> omitted)
        {
            var map = Index(omitted);
            var bb = new ByteBuffer(frame);
            var msg = LogLocalEnvelope.GetRootAsLogLocalEnvelope(bb);
            var pos = TryllFbNav.Root(bb);
            var bodyPos = TryllFbNav.Child(bb, pos, 6);
            switch (msg.BodyType)
            {
                case LogLocalBody.LogConnectionState:
                    Write_Tryll_LogConnectionState(w, msg.BodyAsLogConnectionState(), bodyPos, bb, "", map);
                    break;
                case LogLocalBody.LogLocalError:
                    Write_Tryll_LogLocalError(w, msg.BodyAsLogLocalError(), bodyPos, bb, "", map);
                    break;
                case LogLocalBody.LogCaptureGap:
                    Write_Tryll_LogCaptureGap(w, msg.BodyAsLogCaptureGap(), bodyPos, bb, "", map);
                    break;
                default:
                    w.BeginObject();
                    w.EndObject();
                    break;
            }
        }

        public static byte[] StripMessage(byte[] frame, List<TryllOmittedField> omitted)
        {
            if (frame == null || frame.Length == 0) return frame;
            var bb = new ByteBuffer(frame);
            var msg = Message.GetRootAsMessage(bb);
            switch (msg.BodyType)
            {
                case MessageBody.CreateStringStorageRequest:
                    return Finish(bb, MessageBody.CreateStringStorageRequest, (fbb, bodyPos) => Pack_Tryll_CreateStringStorageRequest(fbb, msg.BodyAsCreateStringStorageRequest(), bodyPos, bb, "", omitted));

                case MessageBody.CreateEmbeddedStringStorageRequest:
                    return Finish(bb, MessageBody.CreateEmbeddedStringStorageRequest, (fbb, bodyPos) => Pack_Tryll_CreateEmbeddedStringStorageRequest(fbb, msg.BodyAsCreateEmbeddedStringStorageRequest(), bodyPos, bb, "", omitted));

                case MessageBody.AudioBuffer:
                    return Finish(bb, MessageBody.AudioBuffer, (fbb, bodyPos) => Pack_Tryll_AudioBuffer(fbb, msg.BodyAsAudioBuffer(), bodyPos, bb, "", omitted));

                case MessageBody.TtsAudioFrame:
                    return Finish(bb, MessageBody.TtsAudioFrame, (fbb, bodyPos) => Pack_Tryll_TtsAudioFrame(fbb, msg.BodyAsTtsAudioFrame(), bodyPos, bb, "", omitted));

                case MessageBody.ExportDialogResponse:
                    return Finish(bb, MessageBody.ExportDialogResponse, (fbb, bodyPos) => Pack_Tryll_ExportDialogResponse(fbb, msg.BodyAsExportDialogResponse(), bodyPos, bb, "", omitted));

                case MessageBody.ImportDialogRequest:
                    return Finish(bb, MessageBody.ImportDialogRequest, (fbb, bodyPos) => Pack_Tryll_ImportDialogRequest(fbb, msg.BodyAsImportDialogRequest(), bodyPos, bb, "", omitted));

                case MessageBody.ExportMutableKbResponse:
                    return Finish(bb, MessageBody.ExportMutableKbResponse, (fbb, bodyPos) => Pack_Tryll_ExportMutableKbResponse(fbb, msg.BodyAsExportMutableKbResponse(), bodyPos, bb, "", omitted));

                case MessageBody.ImportMutableKbRequest:
                    return Finish(bb, MessageBody.ImportMutableKbRequest, (fbb, bodyPos) => Pack_Tryll_ImportMutableKbRequest(fbb, msg.BodyAsImportMutableKbRequest(), bodyPos, bb, "", omitted));

                case MessageBody.TtsVisemeFrame:
                    return Finish(bb, MessageBody.TtsVisemeFrame, (fbb, bodyPos) => Pack_Tryll_TtsVisemeFrame(fbb, msg.BodyAsTtsVisemeFrame(), bodyPos, bb, "", omitted));

                default:
                    return frame;
            }
        }

        public static void ReadScope(byte[] frame, out MessageBody body, out ulong requestId, out ulong agentId, out ulong voiceInputId)
        {
            requestId = 0;
            agentId = 0;
            voiceInputId = 0;
            body = MessageBody.NONE;
            if (frame == null || frame.Length == 0) return;
            var msg = Message.GetRootAsMessage(new ByteBuffer(frame));
            body = msg.BodyType;
            switch (body)
            {
                case MessageBody.CreateSessionRequest:
                {
                    var row = msg.BodyAsCreateSessionRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateSessionResponse:
                {
                    var row = msg.BodyAsCreateSessionResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateAgentRequest:
                {
                    var row = msg.BodyAsCreateAgentRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateAgentResponse:
                {
                    var row = msg.BodyAsCreateAgentResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.SendMessageRequest:
                {
                    var row = msg.BodyAsSendMessageRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.DestroyAgentRequest:
                {
                    var row = msg.BodyAsDestroyAgentRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.AnswerText:
                {
                    var row = msg.BodyAsAnswerText();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.TurnComplete:
                {
                    var row = msg.BodyAsTurnComplete();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.ErrorResponse:
                {
                    var row = msg.BodyAsErrorResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.Ack:
                {
                    var row = msg.BodyAsAck();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.ListModelsRequest:
                {
                    var row = msg.BodyAsListModelsRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.ListModelsResponse:
                {
                    var row = msg.BodyAsListModelsResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.DownloadModelRequest:
                {
                    var row = msg.BodyAsDownloadModelRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.DownloadProgress:
                {
                    var row = msg.BodyAsDownloadProgress();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.DownloadComplete:
                {
                    var row = msg.BodyAsDownloadComplete();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateStringStorageRequest:
                {
                    var row = msg.BodyAsCreateStringStorageRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateStringStorageResponse:
                {
                    var row = msg.BodyAsCreateStringStorageResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.DestroyStringStorageRequest:
                {
                    var row = msg.BodyAsDestroyStringStorageRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.NodeEvent:
                {
                    var row = msg.BodyAsNodeEvent();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.LoadModelRequest:
                {
                    var row = msg.BodyAsLoadModelRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.LoadModelResponse:
                {
                    var row = msg.BodyAsLoadModelResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.UnloadModelRequest:
                {
                    var row = msg.BodyAsUnloadModelRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateEmbeddedStringStorageRequest:
                {
                    var row = msg.BodyAsCreateEmbeddedStringStorageRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateEmbeddedStringStorageResponse:
                {
                    var row = msg.BodyAsCreateEmbeddedStringStorageResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.DestroyEmbeddedStringStorageRequest:
                {
                    var row = msg.BodyAsDestroyEmbeddedStringStorageRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.ChangeAgentParamRequest:
                {
                    var row = msg.BodyAsChangeAgentParamRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.CreateVoiceInputRequest:
                {
                    var row = msg.BodyAsCreateVoiceInputRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CreateVoiceInputResponse:
                {
                    var row = msg.BodyAsCreateVoiceInputResponse();
                    requestId = (ulong)row.RequestId;
                    voiceInputId = (ulong)row.VoiceInputId;
                    break;
                }
                case MessageBody.BeginUtteranceRequest:
                {
                    var row = msg.BodyAsBeginUtteranceRequest();
                    requestId = (ulong)row.RequestId;
                    voiceInputId = (ulong)row.VoiceInputId;
                    break;
                }
                case MessageBody.AudioBuffer:
                {
                    var row = msg.BodyAsAudioBuffer();
                    voiceInputId = (ulong)row.VoiceInputId;
                    break;
                }
                case MessageBody.EndUtteranceRequest:
                {
                    var row = msg.BodyAsEndUtteranceRequest();
                    requestId = (ulong)row.RequestId;
                    voiceInputId = (ulong)row.VoiceInputId;
                    break;
                }
                case MessageBody.CancelUtteranceRequest:
                {
                    var row = msg.BodyAsCancelUtteranceRequest();
                    requestId = (ulong)row.RequestId;
                    voiceInputId = (ulong)row.VoiceInputId;
                    break;
                }
                case MessageBody.WireTranscriptUpdate:
                {
                    var row = msg.BodyAsWireTranscriptUpdate();
                    voiceInputId = (ulong)row.VoiceInputId;
                    break;
                }
                case MessageBody.DestroyVoiceInputRequest:
                {
                    var row = msg.BodyAsDestroyVoiceInputRequest();
                    requestId = (ulong)row.RequestId;
                    voiceInputId = (ulong)row.VoiceInputId;
                    break;
                }
                case MessageBody.TtsAudioFormatFrame:
                {
                    var row = msg.BodyAsTtsAudioFormatFrame();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.TtsAudioFrame:
                {
                    var row = msg.BodyAsTtsAudioFrame();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.DeleteModelRequest:
                {
                    var row = msg.BodyAsDeleteModelRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.DebugCommandRequest:
                {
                    var row = msg.BodyAsDebugCommandRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.DebugCommandResponse:
                {
                    var row = msg.BodyAsDebugCommandResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.CancelRequest:
                {
                    var row = msg.BodyAsCancelRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.ResumeAgentRequest:
                {
                    var row = msg.BodyAsResumeAgentRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.UpdateAgentVariablesRequest:
                {
                    var row = msg.BodyAsUpdateAgentVariablesRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.GetAgentVariablesRequest:
                {
                    var row = msg.BodyAsGetAgentVariablesRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.GetAgentVariablesResponse:
                {
                    var row = msg.BodyAsGetAgentVariablesResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.PrefillAgentKvCacheRequest:
                {
                    var row = msg.BodyAsPrefillAgentKvCacheRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.PrefillAgentKvCacheResponse:
                {
                    var row = msg.BodyAsPrefillAgentKvCacheResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.EvictAgentKvCacheRequest:
                {
                    var row = msg.BodyAsEvictAgentKvCacheRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.EvictAgentKvCacheResponse:
                {
                    var row = msg.BodyAsEvictAgentKvCacheResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.GetAgentKvCacheStatusRequest:
                {
                    var row = msg.BodyAsGetAgentKvCacheStatusRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.GetAgentKvCacheStatusResponse:
                {
                    var row = msg.BodyAsGetAgentKvCacheStatusResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.AppendInteractionsRequest:
                {
                    var row = msg.BodyAsAppendInteractionsRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.AppendInteractionsResponse:
                {
                    var row = msg.BodyAsAppendInteractionsResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.RemoveInteractionsFromEndRequest:
                {
                    var row = msg.BodyAsRemoveInteractionsFromEndRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.RemoveInteractionsFromEndResponse:
                {
                    var row = msg.BodyAsRemoveInteractionsFromEndResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.SetSessionLocaleRequest:
                {
                    var row = msg.BodyAsSetSessionLocaleRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.ExportDialogRequest:
                {
                    var row = msg.BodyAsExportDialogRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.ExportDialogResponse:
                {
                    var row = msg.BodyAsExportDialogResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.ImportDialogRequest:
                {
                    var row = msg.BodyAsImportDialogRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.ImportDialogResponse:
                {
                    var row = msg.BodyAsImportDialogResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.GetMemoryConsumptionRequest:
                {
                    var row = msg.BodyAsGetMemoryConsumptionRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.GetMemoryConsumptionResponse:
                {
                    var row = msg.BodyAsGetMemoryConsumptionResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.EnsureMutableKbRequest:
                {
                    var row = msg.BodyAsEnsureMutableKbRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.EnsureMutableKbResponse:
                {
                    var row = msg.BodyAsEnsureMutableKbResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.EnumerateMutableKbsRequest:
                {
                    var row = msg.BodyAsEnumerateMutableKbsRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.EnumerateMutableKbsResponse:
                {
                    var row = msg.BodyAsEnumerateMutableKbsResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbWriteRequest:
                {
                    var row = msg.BodyAsMutableKbWriteRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbWriteResponse:
                {
                    var row = msg.BodyAsMutableKbWriteResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbDeleteRequest:
                {
                    var row = msg.BodyAsMutableKbDeleteRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbDeleteResponse:
                {
                    var row = msg.BodyAsMutableKbDeleteResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbClearRequest:
                {
                    var row = msg.BodyAsMutableKbClearRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbClearResponse:
                {
                    var row = msg.BodyAsMutableKbClearResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbQueryRequest:
                {
                    var row = msg.BodyAsMutableKbQueryRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbQueryResponse:
                {
                    var row = msg.BodyAsMutableKbQueryResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbGetRequest:
                {
                    var row = msg.BodyAsMutableKbGetRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbGetResponse:
                {
                    var row = msg.BodyAsMutableKbGetResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbListRequest:
                {
                    var row = msg.BodyAsMutableKbListRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbListResponse:
                {
                    var row = msg.BodyAsMutableKbListResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbStatsRequest:
                {
                    var row = msg.BodyAsMutableKbStatsRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.MutableKbStatsResponse:
                {
                    var row = msg.BodyAsMutableKbStatsResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.ExportMutableKbRequest:
                {
                    var row = msg.BodyAsExportMutableKbRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.ExportMutableKbResponse:
                {
                    var row = msg.BodyAsExportMutableKbResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.ImportMutableKbRequest:
                {
                    var row = msg.BodyAsImportMutableKbRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.ImportMutableKbResponse:
                {
                    var row = msg.BodyAsImportMutableKbResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.SaveGameMemoryRequest:
                {
                    var row = msg.BodyAsSaveGameMemoryRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.SaveGameMemoryResponse:
                {
                    var row = msg.BodyAsSaveGameMemoryResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.LoadGameMemoryRequest:
                {
                    var row = msg.BodyAsLoadGameMemoryRequest();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.LoadGameMemoryResponse:
                {
                    var row = msg.BodyAsLoadGameMemoryResponse();
                    requestId = (ulong)row.RequestId;
                    break;
                }
                case MessageBody.SaveDialogRequest:
                {
                    var row = msg.BodyAsSaveDialogRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.SaveDialogResponse:
                {
                    var row = msg.BodyAsSaveDialogResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.LoadDialogRequest:
                {
                    var row = msg.BodyAsLoadDialogRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.LoadDialogResponse:
                {
                    var row = msg.BodyAsLoadDialogResponse();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.TtsVisemeFrame:
                {
                    var row = msg.BodyAsTtsVisemeFrame();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.SetAgentVoiceRequest:
                {
                    var row = msg.BodyAsSetAgentVoiceRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
                case MessageBody.SetAgentDeliveryModeRequest:
                {
                    var row = msg.BodyAsSetAgentDeliveryModeRequest();
                    requestId = (ulong)row.RequestId;
                    agentId = (ulong)row.AgentId;
                    break;
                }
            }
        }

        public static string Label(MessageBody body, byte[] frame)
        {
            if (frame == null || frame.Length == 0) return "";
            var bb = new ByteBuffer(frame);
            var msg = Message.GetRootAsMessage(bb);
            switch (body)
            {
                case MessageBody.SendMessageRequest:
                    return Label_Tryll_SendMessageRequest(msg.BodyAsSendMessageRequest());
                case MessageBody.AnswerText:
                    return Label_Tryll_AnswerText(msg.BodyAsAnswerText());
                case MessageBody.DownloadProgress:
                    return Label_Tryll_DownloadProgress(msg.BodyAsDownloadProgress());
                case MessageBody.NodeEvent:
                    return Label_Tryll_NodeEvent(msg.BodyAsNodeEvent());
                case MessageBody.ChangeAgentParamRequest:
                    return Label_Tryll_ChangeAgentParamRequest(msg.BodyAsChangeAgentParamRequest());
                case MessageBody.TtsAudioFrame:
                    return Label_Tryll_TtsAudioFrame(msg.BodyAsTtsAudioFrame());
                case MessageBody.TtsVisemeFrame:
                    return Label_Tryll_TtsVisemeFrame(msg.BodyAsTtsVisemeFrame());
                default:
                    return "";
            }
        }

        private static void Write_Tryll_Ack(TryllJsonWriter w, Tryll.Ack t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Warning != null)
            {
                w.Key("warning");
                w.String(t.Warning);
            }
            w.EndObject();
        }

        private static void Write_Tryll_AgentVariable(TryllJsonWriter w, Tryll.AgentVariable t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.ValueType != Tryll.VariableValue.NONE)
            {
                w.Key("value_type");
                w.String(t.ValueType.ToString());
                w.Key("value");
                switch (t.ValueType)
                {
                    case Tryll.VariableValue.VarInt:
                        Write_Tryll_VarInt(w, t.ValueAsVarInt(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarFloat:
                        Write_Tryll_VarFloat(w, t.ValueAsVarFloat(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarString:
                        Write_Tryll_VarString(w, t.ValueAsVarString(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarBool:
                        Write_Tryll_VarBool(w, t.ValueAsVarBool(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarStringSet:
                        Write_Tryll_VarStringSet(w, t.ValueAsVarStringSet(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "value.", omitted);
                        break;
                    default:
                        w.BeginObject(); w.EndObject();
                        break;
                }
            }
            w.Key("allow_output_substitution");
            w.Bool(t.AllowOutputSubstitution);
            w.EndObject();
        }

        private static void Write_Tryll_AnswerText(TryllJsonWriter w, Tryll.AnswerText t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("is_delta");
            w.Bool(t.IsDelta);
            w.Key("is_final");
            w.Bool(t.IsFinal);
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            w.EndObject();
        }

        private static string Label_Tryll_AnswerText(Tryll.AnswerText t)
        {
            var node_name = (t.NodeName ?? "");
            return TryllLogLabel.Format("{node_name}", ("node_name", node_name));
        }

        private static void Write_Tryll_AppendInteractionsRequest(TryllJsonWriter w, Tryll.AppendInteractionsRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("interactions");
                w.BeginArray();
                for (int i = 0; i < t.InteractionsLength; i++)
                {
                    var elem = t.Interactions(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 8);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_DialogInteraction(w, elem, elemPos, bb, prefix + "interactions.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_AppendInteractionsResponse(TryllJsonWriter w, Tryll.AppendInteractionsResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("appended_count");
            w.UInt64(t.AppendedCount);
            w.EndObject();
        }

        private static void Write_Tryll_AudioBuffer(TryllJsonWriter w, Tryll.AudioBuffer t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("voice_input_id");
            w.UInt64(t.VoiceInputId);
            {
                var path = (string.IsNullOrEmpty(prefix) ? "pcm" : prefix + "pcm");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 6) || inMap)
                {
                    w.Key("pcm");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.PcmLength, (long)t.PcmLength * 1L);
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            w.EndObject();
        }

        private static int Pack_Tryll_AudioBuffer(FlatBufferBuilder fbb, Tryll.AudioBuffer t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _pcm = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "pcm" : prefix + "pcm");
                omitted.Add(new TryllOmittedField(path, t.PcmLength, (long)t.PcmLength * 1L));
                _pcm = Tryll.AudioBuffer.CreatePcmVector(fbb, new byte[0]);
            }
            return Tryll.AudioBuffer.CreateAudioBuffer(fbb, t.VoiceInputId, _pcm).Value;
        }

        private static void Write_Tryll_BeginUtteranceRequest(TryllJsonWriter w, Tryll.BeginUtteranceRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("voice_input_id");
            w.UInt64(t.VoiceInputId);
            w.Key("auto_finish_on_silence");
            w.Bool(t.AutoFinishOnSilence);
            w.Key("max_utterance_ms");
            w.UInt64(t.MaxUtteranceMs);
            w.EndObject();
        }

        private static void Write_Tryll_CancelRequest(TryllJsonWriter w, Tryll.CancelRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("mode");
            if (Enum.IsDefined(typeof(Tryll.CancelMode), t.Mode)) w.String(t.Mode.ToString());
            else w.Int64(Convert.ToInt64(t.Mode));
            w.EndObject();
        }

        private static void Write_Tryll_CancelUtteranceRequest(TryllJsonWriter w, Tryll.CancelUtteranceRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("voice_input_id");
            w.UInt64(t.VoiceInputId);
            w.EndObject();
        }

        private static void Write_Tryll_ChangeAgentParamRequest(TryllJsonWriter w, Tryll.ChangeAgentParamRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            if (t.ParamsType != Tryll.NodeParams.NodeParams.NONE)
            {
                w.Key("params_type");
                w.String(t.ParamsType.ToString());
                w.Key("params");
                switch (t.ParamsType)
                {
                    case Tryll.NodeParams.NodeParams.GenerateParams:
                        Write_Tryll_NodeParams_GenerateParams(w, t.ParamsAsGenerateParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.RegexGuardrailParams:
                        Write_Tryll_NodeParams_RegexGuardrailParams(w, t.ParamsAsRegexGuardrailParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.CannedResponseParams:
                        Write_Tryll_NodeParams_CannedResponseParams(w, t.ParamsAsCannedResponseParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ToolCallParams:
                        Write_Tryll_NodeParams_ToolCallParams(w, t.ParamsAsToolCallParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.RetrieveParams:
                        Write_Tryll_NodeParams_RetrieveParams(w, t.ParamsAsRetrieveParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.InstructionParams:
                        Write_Tryll_NodeParams_InstructionParams(w, t.ParamsAsInstructionParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ClassifyIntentParams:
                        Write_Tryll_NodeParams_ClassifyIntentParams(w, t.ParamsAsClassifyIntentParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ClassifyIntentLLMParams:
                        Write_Tryll_NodeParams_ClassifyIntentLLMParams(w, t.ParamsAsClassifyIntentLLMParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.IntentToInstructionParams:
                        Write_Tryll_NodeParams_IntentToInstructionParams(w, t.ParamsAsIntentToInstructionParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.GenerateAndSpeakParams:
                        Write_Tryll_NodeParams_GenerateAndSpeakParams(w, t.ParamsAsGenerateAndSpeakParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.SpeakParams:
                        Write_Tryll_NodeParams_SpeakParams(w, t.ParamsAsSpeakParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.PauseParams:
                        Write_Tryll_NodeParams_PauseParams(w, t.ParamsAsPauseParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.TransformParams:
                        Write_Tryll_NodeParams_TransformParams(w, t.ParamsAsTransformParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.BranchParams:
                        Write_Tryll_NodeParams_BranchParams(w, t.ParamsAsBranchParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.WriteToMemoryParams:
                        Write_Tryll_NodeParams_WriteToMemoryParams(w, t.ParamsAsWriteToMemoryParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.SummarizeParams:
                        Write_Tryll_NodeParams_SummarizeParams(w, t.ParamsAsSummarizeParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ClassifyAttributesLLMParams:
                        Write_Tryll_NodeParams_ClassifyAttributesLLMParams(w, t.ParamsAsClassifyAttributesLLMParams(), TryllFbNav.Child(bb, pos, 12), bb, prefix + "params.", omitted);
                        break;
                    default:
                        w.BeginObject(); w.EndObject();
                        break;
                }
            }
            w.EndObject();
        }

        private static string Label_Tryll_ChangeAgentParamRequest(Tryll.ChangeAgentParamRequest t)
        {
            var node_name = (t.NodeName ?? "");
            var params_type = (t.ParamsType == Tryll.NodeParams.NodeParams.NONE ? "" : t.ParamsType.ToString());
            return TryllLogLabel.Format("{node_name} · {params_type}", ("node_name", node_name), ("params_type", params_type));
        }

        private static void Write_Tryll_ConnectionReady(TryllJsonWriter w, Tryll.ConnectionReady t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("protocol_version");
            w.UInt64(t.ProtocolVersion);
            if (t.CodegenFingerprint != null)
            {
                w.Key("codegen_fingerprint");
                w.String(t.CodegenFingerprint);
            }
            if (t.ExePath != null)
            {
                w.Key("exe_path");
                w.String(t.ExePath);
            }
            if (t.BuildConfig != null)
            {
                w.Key("build_config");
                w.String(t.BuildConfig);
            }
            if (t.Version != null)
            {
                w.Key("version");
                w.String(t.Version);
            }
            if (t.RunId != null)
            {
                w.Key("run_id");
                w.String(t.RunId);
            }
            w.Key("managed");
            w.Bool(t.Managed);
            w.Key("idle_timeout_seconds");
            w.UInt64(t.IdleTimeoutSeconds);
            if (t.Mode != null)
            {
                w.Key("mode");
                w.String(t.Mode);
            }
            w.EndObject();
        }

        private static void Write_Tryll_CreateAgentRequest(TryllJsonWriter w, Tryll.CreateAgentRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Graph.HasValue)
            {
                w.Key("graph");
                var childPos = TryllFbNav.Child(bb, pos, 6);
                Write_Tryll_GraphDescription(w, t.Graph.Value, childPos, bb, prefix + "graph.", omitted);
            }
            if (t.DefaultModelName != null)
            {
                w.Key("default_model_name");
                w.String(t.DefaultModelName);
            }
            w.Key("enable_diagnostics");
            w.Bool(t.EnableDiagnostics);
            w.Key("maintain_dialogue_history");
            w.Bool(t.MaintainDialogueHistory);
            if (TryllFbNav.Present(bb, pos, 14))
            {
                w.Key("variables");
                w.BeginArray();
                for (int i = 0; i < t.VariablesLength; i++)
                {
                    var elem = t.Variables(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 14);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_AgentVariable(w, elem, elemPos, bb, prefix + "variables.", omitted);
                }
                w.EndArray();
            }
            w.Key("kv_cache_initialization");
            if (Enum.IsDefined(typeof(Tryll.AgentKvCacheInitialization), t.KvCacheInitialization)) w.String(t.KvCacheInitialization.ToString());
            else w.Int64(Convert.ToInt64(t.KvCacheInitialization));
            w.Key("workload");
            if (Enum.IsDefined(typeof(Tryll.AgentWorkload), t.Workload)) w.String(t.Workload.ToString());
            else w.Int64(Convert.ToInt64(t.Workload));
            if (t.Voice.HasValue)
            {
                w.Key("voice");
                var childPos = TryllFbNav.Child(bb, pos, 20);
                Write_Tryll_VoiceProfile(w, t.Voice.Value, childPos, bb, prefix + "voice.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_CreateAgentResponse(TryllJsonWriter w, Tryll.CreateAgentResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.EndObject();
        }

        private static void Write_Tryll_CreateEmbeddedStringStorageRequest(TryllJsonWriter w, Tryll.CreateEmbeddedStringStorageRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.ConfigPath != null)
            {
                w.Key("config_path");
                w.String(t.ConfigPath);
            }
            {
                var path = (string.IsNullOrEmpty(prefix) ? "strings" : prefix + "strings");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 10) || inMap)
                {
                    w.Key("strings");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.StringsLength, StringVectorBytes(t.StringsLength, i => t.Strings(i)));
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            if (t.EmbeddingModel != null)
            {
                w.Key("embedding_model");
                w.String(t.EmbeddingModel);
            }
            w.EndObject();
        }

        private static int Pack_Tryll_CreateEmbeddedStringStorageRequest(FlatBufferBuilder fbb, Tryll.CreateEmbeddedStringStorageRequest t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _name = t.Name == null ? default(StringOffset) : fbb.CreateString(t.Name);
            var _config_path = t.ConfigPath == null ? default(StringOffset) : fbb.CreateString(t.ConfigPath);
            var _strings = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 10))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "strings" : prefix + "strings");
                omitted.Add(new TryllOmittedField(path, t.StringsLength, StringVectorBytes(t.StringsLength, i => t.Strings(i))));
                _strings = Tryll.CreateEmbeddedStringStorageRequest.CreateStringsVector(fbb, new StringOffset[0]);
            }
            var _embedding_model = t.EmbeddingModel == null ? default(StringOffset) : fbb.CreateString(t.EmbeddingModel);
            return Tryll.CreateEmbeddedStringStorageRequest.CreateCreateEmbeddedStringStorageRequest(fbb, t.RequestId, _name, _config_path, _strings, _embedding_model).Value;
        }

        private static void Write_Tryll_CreateEmbeddedStringStorageResponse(TryllJsonWriter w, Tryll.CreateEmbeddedStringStorageResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.Key("record_count");
            w.UInt64(t.RecordCount);
            w.Key("embedding_dim");
            w.UInt64(t.EmbeddingDim);
            w.EndObject();
        }

        private static void Write_Tryll_CreateSessionRequest(TryllJsonWriter w, Tryll.CreateSessionRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.GameName != null)
            {
                w.Key("game_name");
                w.String(t.GameName);
            }
            if (t.Locale != null)
            {
                w.Key("locale");
                w.String(t.Locale);
            }
            if (t.ModelConfig.HasValue)
            {
                w.Key("model_config");
                var childPos = TryllFbNav.Child(bb, pos, 10);
                Write_Tryll_SessionModelConfig(w, t.ModelConfig.Value, childPos, bb, prefix + "model_config.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_CreateSessionResponse(TryllJsonWriter w, Tryll.CreateSessionResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("session_id");
            w.UInt64(t.SessionId);
            w.EndObject();
        }

        private static void Write_Tryll_CreateStringStorageRequest(TryllJsonWriter w, Tryll.CreateStringStorageRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.Key("kind");
            if (Enum.IsDefined(typeof(Tryll.StringStorageKind), t.Kind)) w.String(t.Kind.ToString());
            else w.Int64(Convert.ToInt64(t.Kind));
            {
                var path = (string.IsNullOrEmpty(prefix) ? "strings" : prefix + "strings");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 10) || inMap)
                {
                    w.Key("strings");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.StringsLength, StringVectorBytes(t.StringsLength, i => t.Strings(i)));
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            {
                var path = (string.IsNullOrEmpty(prefix) ? "keys" : prefix + "keys");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 12) || inMap)
                {
                    w.Key("keys");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.KeysLength, StringVectorBytes(t.KeysLength, i => t.Keys(i)));
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            if (t.FilePath != null)
            {
                w.Key("file_path");
                w.String(t.FilePath);
            }
            w.EndObject();
        }

        private static int Pack_Tryll_CreateStringStorageRequest(FlatBufferBuilder fbb, Tryll.CreateStringStorageRequest t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _name = t.Name == null ? default(StringOffset) : fbb.CreateString(t.Name);
            var _strings = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 10))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "strings" : prefix + "strings");
                omitted.Add(new TryllOmittedField(path, t.StringsLength, StringVectorBytes(t.StringsLength, i => t.Strings(i))));
                _strings = Tryll.CreateStringStorageRequest.CreateStringsVector(fbb, new StringOffset[0]);
            }
            var _keys = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 12))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "keys" : prefix + "keys");
                omitted.Add(new TryllOmittedField(path, t.KeysLength, StringVectorBytes(t.KeysLength, i => t.Keys(i))));
                _keys = Tryll.CreateStringStorageRequest.CreateKeysVector(fbb, new StringOffset[0]);
            }
            var _file_path = t.FilePath == null ? default(StringOffset) : fbb.CreateString(t.FilePath);
            return Tryll.CreateStringStorageRequest.CreateCreateStringStorageRequest(fbb, t.RequestId, _name, t.Kind, _strings, _keys, _file_path).Value;
        }

        private static void Write_Tryll_CreateStringStorageResponse(TryllJsonWriter w, Tryll.CreateStringStorageResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.EndObject();
        }

        private static void Write_Tryll_CreateVoiceInputRequest(TryllJsonWriter w, Tryll.CreateVoiceInputRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            if (t.InputFormat.HasValue)
            {
                w.Key("input_format");
                var childPos = TryllFbNav.Child(bb, pos, 8);
                Write_Tryll_WireAudioFormat(w, t.InputFormat.Value, childPos, bb, prefix + "input_format.", omitted);
            }
            w.Key("vad_threshold");
            w.Float(t.VadThreshold);
            w.Key("vad_min_silence_ms");
            w.UInt64(t.VadMinSilenceMs);
            w.Key("vad_speech_pad_ms");
            w.UInt64(t.VadSpeechPadMs);
            if (t.HotwordsStoragePath != null)
            {
                w.Key("hotwords_storage_path");
                w.String(t.HotwordsStoragePath);
            }
            w.Key("hotwords_score");
            w.Float(t.HotwordsScore);
            w.EndObject();
        }

        private static void Write_Tryll_CreateVoiceInputResponse(TryllJsonWriter w, Tryll.CreateVoiceInputResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("voice_input_id");
            w.UInt64(t.VoiceInputId);
            w.EndObject();
        }

        private static void Write_Tryll_DebugCommandRequest(TryllJsonWriter w, Tryll.DebugCommandRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Command != null)
            {
                w.Key("command");
                w.String(t.Command);
            }
            if (t.Payload != null)
            {
                w.Key("payload");
                w.String(t.Payload);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DebugCommandResponse(TryllJsonWriter w, Tryll.DebugCommandResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Result != null)
            {
                w.Key("result");
                w.String(t.Result);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DeleteModelRequest(TryllJsonWriter w, Tryll.DeleteModelRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DeliveryMode(TryllJsonWriter w, Tryll.DeliveryMode t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.ReferenceVoice != null)
            {
                w.Key("reference_voice");
                w.String(t.ReferenceVoice);
            }
            if (t.Instruction != null)
            {
                w.Key("instruction");
                w.String(t.Instruction);
            }
            w.Key("expressiveness");
            w.Float(t.Expressiveness);
            w.Key("speed_scale");
            w.Float(t.SpeedScale);
            w.EndObject();
        }

        private static void Write_Tryll_DestroyAgentRequest(TryllJsonWriter w, Tryll.DestroyAgentRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.EndObject();
        }

        private static void Write_Tryll_DestroyEmbeddedStringStorageRequest(TryllJsonWriter w, Tryll.DestroyEmbeddedStringStorageRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DestroyStringStorageRequest(TryllJsonWriter w, Tryll.DestroyStringStorageRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DestroyVoiceInputRequest(TryllJsonWriter w, Tryll.DestroyVoiceInputRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("voice_input_id");
            w.UInt64(t.VoiceInputId);
            w.EndObject();
        }

        private static void Write_Tryll_DialogInteraction(TryllJsonWriter w, Tryll.DialogInteraction t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.UserMessage != null)
            {
                w.Key("user_message");
                w.String(t.UserMessage);
            }
            if (t.AssistantMessage != null)
            {
                w.Key("assistant_message");
                w.String(t.AssistantMessage);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DownloadComplete(TryllJsonWriter w, Tryll.DownloadComplete t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("success");
            w.Bool(t.Success);
            if (t.Error != null)
            {
                w.Key("error");
                w.String(t.Error);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DownloadModelRequest(TryllJsonWriter w, Tryll.DownloadModelRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.EndObject();
        }

        private static void Write_Tryll_DownloadProgress(TryllJsonWriter w, Tryll.DownloadProgress t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("bytes_downloaded");
            w.UInt64(t.BytesDownloaded);
            w.Key("total_bytes");
            w.UInt64(t.TotalBytes);
            w.Key("percent");
            w.Float(t.Percent);
            w.EndObject();
        }

        private static string Label_Tryll_DownloadProgress(Tryll.DownloadProgress t)
        {
            var model_name = (t.ModelName ?? "");
            return TryllLogLabel.Format("{model_name}", ("model_name", model_name));
        }

        private static void Write_Tryll_EndUtteranceRequest(TryllJsonWriter w, Tryll.EndUtteranceRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("voice_input_id");
            w.UInt64(t.VoiceInputId);
            w.EndObject();
        }

        private static void Write_Tryll_EngineSelection(TryllJsonWriter w, Tryll.EngineSelection t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("role");
            if (Enum.IsDefined(typeof(Tryll.ModelRole), t.Role)) w.String(t.Role.ToString());
            else w.Int64(Convert.ToInt64(t.Role));
            w.Key("engine");
            if (Enum.IsDefined(typeof(Tryll.InferenceEngine), t.Engine)) w.String(t.Engine.ToString());
            else w.Int64(Convert.ToInt64(t.Engine));
            w.EndObject();
        }

        private static void Write_Tryll_EnsureMutableKbRequest(TryllJsonWriter w, Tryll.EnsureMutableKbRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Schema != null)
            {
                w.Key("schema");
                w.String(t.Schema);
            }
            w.EndObject();
        }

        private static void Write_Tryll_EnsureMutableKbResponse(TryllJsonWriter w, Tryll.EnsureMutableKbResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Schema != null)
            {
                w.Key("schema");
                w.String(t.Schema);
            }
            w.Key("created");
            w.Bool(t.Created);
            w.EndObject();
        }

        private static void Write_Tryll_EnumerateMutableKbsRequest(TryllJsonWriter w, Tryll.EnumerateMutableKbsRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.EndObject();
        }

        private static void Write_Tryll_EnumerateMutableKbsResponse(TryllJsonWriter w, Tryll.EnumerateMutableKbsResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("stores");
                w.BeginArray();
                for (int i = 0; i < t.StoresLength; i++)
                {
                    var elem = t.Stores(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MutableKbInfo(w, elem, elemPos, bb, prefix + "stores.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_ErrorResponse(TryllJsonWriter w, Tryll.ErrorResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("code");
            w.UInt64(t.Code);
            if (t.Message != null)
            {
                w.Key("message");
                w.String(t.Message);
            }
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.EndObject();
        }

        private static void Write_Tryll_EvictAgentKvCacheRequest(TryllJsonWriter w, Tryll.EvictAgentKvCacheRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.EndObject();
        }

        private static void Write_Tryll_EvictAgentKvCacheResponse(TryllJsonWriter w, Tryll.EvictAgentKvCacheResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("applicable");
            w.Bool(t.Applicable);
            w.Key("residency");
            if (Enum.IsDefined(typeof(Tryll.AgentKvCacheResidency), t.Residency)) w.String(t.Residency.ToString());
            else w.Int64(Convert.ToInt64(t.Residency));
            w.Key("prefix_status");
            if (Enum.IsDefined(typeof(Tryll.AgentKvCachePrefixStatus), t.PrefixStatus)) w.String(t.PrefixStatus.ToString());
            else w.Int64(Convert.ToInt64(t.PrefixStatus));
            w.Key("eligible_node_count");
            w.UInt64(t.EligibleNodeCount);
            w.Key("contexts_evicted");
            w.UInt64(t.ContextsEvicted);
            if (t.DebugInfo != null)
            {
                w.Key("debug_info");
                w.String(t.DebugInfo);
            }
            w.EndObject();
        }

        private static void Write_Tryll_ExportDialogRequest(TryllJsonWriter w, Tryll.ExportDialogRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.EndObject();
        }

        private static void Write_Tryll_ExportDialogResponse(TryllJsonWriter w, Tryll.ExportDialogResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Snapshot.HasValue)
            {
                w.Key("snapshot");
                var childPos = TryllFbNav.Child(bb, pos, 8);
                Write_Tryll_PersistedDialogSnapshot(w, t.Snapshot.Value, childPos, bb, prefix + "snapshot.", omitted);
            }
            w.EndObject();
        }

        private static int Pack_Tryll_ExportDialogResponse(FlatBufferBuilder fbb, Tryll.ExportDialogResponse t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _snapshot = default(Offset<Tryll.PersistedDialogSnapshot>);
            if (t.Snapshot.HasValue)
                _snapshot = new Offset<Tryll.PersistedDialogSnapshot>(Pack_Tryll_PersistedDialogSnapshot(fbb, t.Snapshot.Value, TryllFbNav.Child(bb, pos, 8), bb, prefix + "snapshot.", omitted));
            return Tryll.ExportDialogResponse.CreateExportDialogResponse(fbb, t.RequestId, t.AgentId, _snapshot).Value;
        }

        private static void Write_Tryll_ExportMutableKbRequest(TryllJsonWriter w, Tryll.ExportMutableKbRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.EndObject();
        }

        private static void Write_Tryll_ExportMutableKbResponse(TryllJsonWriter w, Tryll.ExportMutableKbResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Snapshot.HasValue)
            {
                w.Key("snapshot");
                var childPos = TryllFbNav.Child(bb, pos, 6);
                Write_Tryll_PersistedMutableKb(w, t.Snapshot.Value, childPos, bb, prefix + "snapshot.", omitted);
            }
            w.EndObject();
        }

        private static int Pack_Tryll_ExportMutableKbResponse(FlatBufferBuilder fbb, Tryll.ExportMutableKbResponse t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _snapshot = default(Offset<Tryll.PersistedMutableKb>);
            if (t.Snapshot.HasValue)
                _snapshot = new Offset<Tryll.PersistedMutableKb>(Pack_Tryll_PersistedMutableKb(fbb, t.Snapshot.Value, TryllFbNav.Child(bb, pos, 6), bb, prefix + "snapshot.", omitted));
            return Tryll.ExportMutableKbResponse.CreateExportMutableKbResponse(fbb, t.RequestId, _snapshot).Value;
        }

        private static void Write_Tryll_GameMemory(TryllJsonWriter w, Tryll.GameMemory t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("format_version");
            w.UInt64(t.FormatVersion);
            w.Key("saved_at_ms");
            w.UInt64(t.SavedAtMs);
            if (t.SaveId != null)
            {
                w.Key("save_id");
                w.String(t.SaveId);
            }
            {
                var path = (string.IsNullOrEmpty(prefix) ? "stores" : prefix + "stores");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 10) || inMap)
                {
                    w.Key("stores");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.StoresLength, (long)t.StoresLength * 4L);
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            w.EndObject();
        }

        private static int Pack_Tryll_GameMemory(FlatBufferBuilder fbb, Tryll.GameMemory t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _save_id = t.SaveId == null ? default(StringOffset) : fbb.CreateString(t.SaveId);
            var _stores = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 10))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "stores" : prefix + "stores");
                omitted.Add(new TryllOmittedField(path, t.StoresLength, (long)t.StoresLength * 4L));
                _stores = Tryll.GameMemory.CreateStoresVector(fbb, new Offset<Tryll.PersistedMutableKb>[0]);
            }
            return Tryll.GameMemory.CreateGameMemory(fbb, t.FormatVersion, t.SavedAtMs, _save_id, _stores).Value;
        }

        private static void Write_Tryll_GetAgentKvCacheStatusRequest(TryllJsonWriter w, Tryll.GetAgentKvCacheStatusRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.EndObject();
        }

        private static void Write_Tryll_GetAgentKvCacheStatusResponse(TryllJsonWriter w, Tryll.GetAgentKvCacheStatusResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("applicable");
            w.Bool(t.Applicable);
            w.Key("residency");
            if (Enum.IsDefined(typeof(Tryll.AgentKvCacheResidency), t.Residency)) w.String(t.Residency.ToString());
            else w.Int64(Convert.ToInt64(t.Residency));
            w.Key("prefix_status");
            if (Enum.IsDefined(typeof(Tryll.AgentKvCachePrefixStatus), t.PrefixStatus)) w.String(t.PrefixStatus.ToString());
            else w.Int64(Convert.ToInt64(t.PrefixStatus));
            w.Key("eligible_node_count");
            w.UInt64(t.EligibleNodeCount);
            if (t.DebugInfo != null)
            {
                w.Key("debug_info");
                w.String(t.DebugInfo);
            }
            w.EndObject();
        }

        private static void Write_Tryll_GetAgentVariablesRequest(TryllJsonWriter w, Tryll.GetAgentVariablesRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("names");
                w.BeginArray();
                for (int i = 0; i < t.NamesLength; i++)
                {
                    w.String(t.Names(i) ?? "");
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_GetAgentVariablesResponse(TryllJsonWriter w, Tryll.GetAgentVariablesResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("variables");
                w.BeginArray();
                for (int i = 0; i < t.VariablesLength; i++)
                {
                    var elem = t.Variables(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_AgentVariable(w, elem, elemPos, bb, prefix + "variables.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_GetMemoryConsumptionRequest(TryllJsonWriter w, Tryll.GetMemoryConsumptionRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.EndObject();
        }

        private static void Write_Tryll_GetMemoryConsumptionResponse(TryllJsonWriter w, Tryll.GetMemoryConsumptionResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Snapshot.HasValue)
            {
                w.Key("snapshot");
                var childPos = TryllFbNav.Child(bb, pos, 6);
                Write_Tryll_MemorySnapshot(w, t.Snapshot.Value, childPos, bb, prefix + "snapshot.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_GraphDescription(TryllJsonWriter w, Tryll.GraphDescription t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (TryllFbNav.Present(bb, pos, 4))
            {
                w.Key("nodes");
                w.BeginArray();
                for (int i = 0; i < t.NodesLength; i++)
                {
                    var elem = t.Nodes(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 4);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeDescription(w, elem, elemPos, bb, prefix + "nodes.", omitted);
                }
                w.EndArray();
            }
            if (t.StartNode != null)
            {
                w.Key("start_node");
                w.String(t.StartNode);
            }
            w.EndObject();
        }

        private static void Write_Tryll_ImportDialogRequest(TryllJsonWriter w, Tryll.ImportDialogRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Snapshot.HasValue)
            {
                w.Key("snapshot");
                var childPos = TryllFbNav.Child(bb, pos, 8);
                Write_Tryll_PersistedDialogSnapshot(w, t.Snapshot.Value, childPos, bb, prefix + "snapshot.", omitted);
            }
            w.Key("compatibility");
            if (Enum.IsDefined(typeof(Tryll.DialogCompatibility), t.Compatibility)) w.String(t.Compatibility.ToString());
            else w.Int64(Convert.ToInt64(t.Compatibility));
            w.EndObject();
        }

        private static int Pack_Tryll_ImportDialogRequest(FlatBufferBuilder fbb, Tryll.ImportDialogRequest t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _snapshot = default(Offset<Tryll.PersistedDialogSnapshot>);
            if (t.Snapshot.HasValue)
                _snapshot = new Offset<Tryll.PersistedDialogSnapshot>(Pack_Tryll_PersistedDialogSnapshot(fbb, t.Snapshot.Value, TryllFbNav.Child(bb, pos, 8), bb, prefix + "snapshot.", omitted));
            return Tryll.ImportDialogRequest.CreateImportDialogRequest(fbb, t.RequestId, t.AgentId, _snapshot, t.Compatibility).Value;
        }

        private static void Write_Tryll_ImportDialogResponse(TryllJsonWriter w, Tryll.ImportDialogResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("interaction_count");
            w.UInt64(t.InteractionCount);
            if (TryllFbNav.Present(bb, pos, 10))
            {
                w.Key("warnings");
                w.BeginArray();
                for (int i = 0; i < t.WarningsLength; i++)
                {
                    w.String(t.Warnings(i) ?? "");
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_ImportMutableKbRequest(TryllJsonWriter w, Tryll.ImportMutableKbRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Snapshot.HasValue)
            {
                w.Key("snapshot");
                var childPos = TryllFbNav.Child(bb, pos, 6);
                Write_Tryll_PersistedMutableKb(w, t.Snapshot.Value, childPos, bb, prefix + "snapshot.", omitted);
            }
            w.Key("compatibility");
            if (Enum.IsDefined(typeof(Tryll.MutableKbCompatibility), t.Compatibility)) w.String(t.Compatibility.ToString());
            else w.Int64(Convert.ToInt64(t.Compatibility));
            w.EndObject();
        }

        private static int Pack_Tryll_ImportMutableKbRequest(FlatBufferBuilder fbb, Tryll.ImportMutableKbRequest t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _snapshot = default(Offset<Tryll.PersistedMutableKb>);
            if (t.Snapshot.HasValue)
                _snapshot = new Offset<Tryll.PersistedMutableKb>(Pack_Tryll_PersistedMutableKb(fbb, t.Snapshot.Value, TryllFbNav.Child(bb, pos, 6), bb, prefix + "snapshot.", omitted));
            return Tryll.ImportMutableKbRequest.CreateImportMutableKbRequest(fbb, t.RequestId, _snapshot, t.Compatibility).Value;
        }

        private static void Write_Tryll_ImportMutableKbResponse(TryllJsonWriter w, Tryll.ImportMutableKbResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("warnings");
                w.BeginArray();
                for (int i = 0; i < t.WarningsLength; i++)
                {
                    w.String(t.Warnings(i) ?? "");
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_ListModelsRequest(TryllJsonWriter w, Tryll.ListModelsRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.EndObject();
        }

        private static void Write_Tryll_ListModelsResponse(TryllJsonWriter w, Tryll.ListModelsResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("models");
                w.BeginArray();
                for (int i = 0; i < t.ModelsLength; i++)
                {
                    var elem = t.Models(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_ModelInfo(w, elem, elemPos, bb, prefix + "models.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_LoadDialogRequest(TryllJsonWriter w, Tryll.LoadDialogRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Path != null)
            {
                w.Key("path");
                w.String(t.Path);
            }
            w.Key("compatibility");
            if (Enum.IsDefined(typeof(Tryll.DialogCompatibility), t.Compatibility)) w.String(t.Compatibility.ToString());
            else w.Int64(Convert.ToInt64(t.Compatibility));
            w.EndObject();
        }

        private static void Write_Tryll_LoadDialogResponse(TryllJsonWriter w, Tryll.LoadDialogResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("interaction_count");
            w.UInt64(t.InteractionCount);
            if (TryllFbNav.Present(bb, pos, 10))
            {
                w.Key("warnings");
                w.BeginArray();
                for (int i = 0; i < t.WarningsLength; i++)
                {
                    w.String(t.Warnings(i) ?? "");
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_LoadGameMemoryRequest(TryllJsonWriter w, Tryll.LoadGameMemoryRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Path != null)
            {
                w.Key("path");
                w.String(t.Path);
            }
            w.Key("compatibility");
            if (Enum.IsDefined(typeof(Tryll.MutableKbCompatibility), t.Compatibility)) w.String(t.Compatibility.ToString());
            else w.Int64(Convert.ToInt64(t.Compatibility));
            w.Key("embed_timing");
            if (Enum.IsDefined(typeof(Tryll.MutableKbEmbedTiming), t.EmbedTiming)) w.String(t.EmbedTiming.ToString());
            else w.Int64(Convert.ToInt64(t.EmbedTiming));
            w.EndObject();
        }

        private static void Write_Tryll_LoadGameMemoryResponse(TryllJsonWriter w, Tryll.LoadGameMemoryResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("store_count");
            w.UInt64(t.StoreCount);
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("warnings");
                w.BeginArray();
                for (int i = 0; i < t.WarningsLength; i++)
                {
                    w.String(t.Warnings(i) ?? "");
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_LoadModelRequest(TryllJsonWriter w, Tryll.LoadModelRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("placement");
            if (Enum.IsDefined(typeof(Tryll.MemoryPlacement), t.Placement)) w.String(t.Placement.ToString());
            else w.Int64(Convert.ToInt64(t.Placement));
            w.Key("placement_is_override");
            w.Bool(t.PlacementIsOverride);
            w.EndObject();
        }

        private static void Write_Tryll_LoadModelResponse(TryllJsonWriter w, Tryll.LoadModelResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("placement_resolved");
            if (Enum.IsDefined(typeof(Tryll.MemoryPlacement), t.PlacementResolved)) w.String(t.PlacementResolved.ToString());
            else w.Int64(Convert.ToInt64(t.PlacementResolved));
            if (t.Backend != null)
            {
                w.Key("backend");
                w.String(t.Backend);
            }
            w.EndObject();
        }

        private static void Write_Tryll_LogCaptureGap(TryllJsonWriter w, Tryll.LogCaptureGap t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("reason");
            if (Enum.IsDefined(typeof(Tryll.LogGapReason), t.Reason)) w.String(t.Reason.ToString());
            else w.Int64(Convert.ToInt64(t.Reason));
            w.EndObject();
        }

        private static void Write_Tryll_LogConnectionState(TryllJsonWriter w, Tryll.LogConnectionState t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("state");
            if (Enum.IsDefined(typeof(Tryll.LogConnState), t.State)) w.String(t.State.ToString());
            else w.Int64(Convert.ToInt64(t.State));
            if (t.Reason != null)
            {
                w.Key("reason");
                w.String(t.Reason);
            }
            w.EndObject();
        }

        private static void Write_Tryll_LogLocalError(TryllJsonWriter w, Tryll.LogLocalError t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("code");
            w.UInt64(t.Code);
            if (t.CodeName != null)
            {
                w.Key("code_name");
                w.String(t.CodeName);
            }
            if (t.Message != null)
            {
                w.Key("message");
                w.String(t.Message);
            }
            w.Key("origin");
            if (Enum.IsDefined(typeof(Tryll.LogErrorOrigin), t.Origin)) w.String(t.Origin.ToString());
            else w.Int64(Convert.ToInt64(t.Origin));
            w.EndObject();
        }

        private static void Write_Tryll_MemoryComponent(TryllJsonWriter w, Tryll.MemoryComponent t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Kind != null)
            {
                w.Key("kind");
                w.String(t.Kind);
            }
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.Key("owner");
            if (Enum.IsDefined(typeof(Tryll.MemoryOwnerKind), t.Owner)) w.String(t.Owner.ToString());
            else w.Int64(Convert.ToInt64(t.Owner));
            w.Key("tier");
            if (Enum.IsDefined(typeof(Tryll.MemoryTier), t.Tier)) w.String(t.Tier.ToString());
            else w.Int64(Convert.ToInt64(t.Tier));
            if (t.EstimateBasis != null)
            {
                w.Key("estimate_basis");
                w.String(t.EstimateBasis);
            }
            w.Key("ram_bytes");
            w.UInt64(t.RamBytes);
            w.Key("vram_bytes");
            w.UInt64(t.VramBytes);
            w.Key("ram_bytes_high");
            w.UInt64(t.RamBytesHigh);
            w.Key("device_index");
            w.Int64(t.DeviceIndex);
            if (TryllFbNav.Present(bb, pos, 22))
            {
                w.Key("per_device");
                w.BeginArray();
                for (int i = 0; i < t.PerDeviceLength; i++)
                {
                    var elem = t.PerDevice(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 22);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MemoryComponentDevice(w, elem, elemPos, bb, prefix + "per_device.", omitted);
                }
                w.EndArray();
            }
            w.Key("session_id");
            w.UInt64(t.SessionId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            w.Key("used_tokens");
            w.UInt64(t.UsedTokens);
            w.Key("capacity_tokens");
            w.UInt64(t.CapacityTokens);
            w.Key("resident");
            w.Bool(t.Resident);
            w.Key("occupancy_stale");
            w.Bool(t.OccupancyStale);
            w.Key("compute_bytes");
            w.UInt64(t.ComputeBytes);
            w.Key("consumer_class");
            if (Enum.IsDefined(typeof(Tryll.InferenceConsumerClass), t.ConsumerClass)) w.String(t.ConsumerClass.ToString());
            else w.Int64(Convert.ToInt64(t.ConsumerClass));
            w.Key("last_turn_at_ms");
            w.Int64(t.LastTurnAtMs);
            w.EndObject();
        }

        private static void Write_Tryll_MemoryComponentDevice(TryllJsonWriter w, Tryll.MemoryComponentDevice t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("device_index");
            w.Int64(t.DeviceIndex);
            w.Key("bytes");
            w.UInt64(t.Bytes);
            w.Key("compute_bytes");
            w.UInt64(t.ComputeBytes);
            w.EndObject();
        }

        private static void Write_Tryll_MemoryDevice(TryllJsonWriter w, Tryll.MemoryDevice t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("index");
            w.Int64(t.Index);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Description != null)
            {
                w.Key("description");
                w.String(t.Description);
            }
            if (t.Kind != null)
            {
                w.Key("kind");
                w.String(t.Kind);
            }
            w.Key("total_bytes");
            w.UInt64(t.TotalBytes);
            w.Key("free_bytes");
            w.UInt64(t.FreeBytes);
            w.Key("self_model_bytes");
            w.UInt64(t.SelfModelBytes);
            w.Key("self_context_bytes");
            w.UInt64(t.SelfContextBytes);
            w.Key("self_compute_bytes");
            w.UInt64(t.SelfComputeBytes);
            w.Key("other_bytes");
            w.UInt64(t.OtherBytes);
            w.EndObject();
        }

        private static void Write_Tryll_MemorySnapshot(TryllJsonWriter w, Tryll.MemorySnapshot t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("captured_at_ms");
            w.Int64(t.CapturedAtMs);
            w.Key("process_working_set_bytes");
            w.UInt64(t.ProcessWorkingSetBytes);
            w.Key("process_private_bytes");
            w.UInt64(t.ProcessPrivateBytes);
            w.Key("process_available");
            w.Bool(t.ProcessAvailable);
            w.Key("backend_initialized");
            w.Bool(t.BackendInitialized);
            w.Key("unattributed_ram_bytes");
            w.UInt64(t.UnattributedRamBytes);
            if (TryllFbNav.Present(bb, pos, 16))
            {
                w.Key("devices");
                w.BeginArray();
                for (int i = 0; i < t.DevicesLength; i++)
                {
                    var elem = t.Devices(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 16);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MemoryDevice(w, elem, elemPos, bb, prefix + "devices.", omitted);
                }
                w.EndArray();
            }
            if (TryllFbNav.Present(bb, pos, 18))
            {
                w.Key("components");
                w.BeginArray();
                for (int i = 0; i < t.ComponentsLength; i++)
                {
                    var elem = t.Components(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 18);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MemoryComponent(w, elem, elemPos, bb, prefix + "components.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_ModelDependency(TryllJsonWriter w, Tryll.ModelDependency t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Role != null)
            {
                w.Key("role");
                w.String(t.Role);
            }
            w.Key("status");
            if (Enum.IsDefined(typeof(Tryll.ModelStatus), t.Status)) w.String(t.Status.ToString());
            else w.Int64(Convert.ToInt64(t.Status));
            w.Key("size_bytes");
            w.UInt64(t.SizeBytes);
            if (t.Folder != null)
            {
                w.Key("folder");
                w.String(t.Folder);
            }
            if (t.LocalPath != null)
            {
                w.Key("local_path");
                w.String(t.LocalPath);
            }
            if (t.FailureReason != null)
            {
                w.Key("failure_reason");
                w.String(t.FailureReason);
            }
            w.EndObject();
        }

        private static void Write_Tryll_ModelInfo(TryllJsonWriter w, Tryll.ModelInfo t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.Key("status");
            if (Enum.IsDefined(typeof(Tryll.ModelStatus), t.Status)) w.String(t.Status.ToString());
            else w.Int64(Convert.ToInt64(t.Status));
            if (t.HuggingfaceRepo != null)
            {
                w.Key("huggingface_repo");
                w.String(t.HuggingfaceRepo);
            }
            w.Key("size_bytes");
            w.UInt64(t.SizeBytes);
            if (t.ModelType != null)
            {
                w.Key("model_type");
                w.String(t.ModelType);
            }
            if (t.Engine != null)
            {
                w.Key("engine");
                w.String(t.Engine);
            }
            if (t.Audience != null)
            {
                w.Key("audience");
                w.String(t.Audience);
            }
            if (t.ToolCallFormat != null)
            {
                w.Key("tool_call_format");
                w.String(t.ToolCallFormat);
            }
            w.Key("context_size");
            w.Int64(t.ContextSize);
            if (TryllFbNav.Present(bb, pos, 22))
            {
                w.Key("files");
                w.BeginArray();
                for (int i = 0; i < t.FilesLength; i++)
                {
                    w.String(t.Files(i) ?? "");
                }
                w.EndArray();
            }
            if (t.DefaultSampling.HasValue)
            {
                w.Key("default_sampling");
                var childPos = TryllFbNav.Child(bb, pos, 24);
                Write_Tryll_ModelSampling(w, t.DefaultSampling.Value, childPos, bb, prefix + "default_sampling.", omitted);
            }
            if (t.LocalPath != null)
            {
                w.Key("local_path");
                w.String(t.LocalPath);
            }
            if (t.ToolCallSupport != null)
            {
                w.Key("tool_call_support");
                w.String(t.ToolCallSupport);
            }
            if (TryllFbNav.Present(bb, pos, 30))
            {
                w.Key("languages");
                w.BeginArray();
                for (int i = 0; i < t.LanguagesLength; i++)
                {
                    w.String(t.Languages(i) ?? "");
                }
                w.EndArray();
            }
            if (TryllFbNav.Present(bb, pos, 32))
            {
                w.Key("dependencies");
                w.BeginArray();
                for (int i = 0; i < t.DependenciesLength; i++)
                {
                    var elem = t.Dependencies(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 32);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_ModelDependency(w, elem, elemPos, bb, prefix + "dependencies.", omitted);
                }
                w.EndArray();
            }
            if (t.TtsCapabilities.HasValue)
            {
                w.Key("tts_capabilities");
                var childPos = TryllFbNav.Child(bb, pos, 34);
                Write_Tryll_TtsCapabilitiesInfo(w, t.TtsCapabilities.Value, childPos, bb, prefix + "tts_capabilities.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_ModelPlacementHint(TryllJsonWriter w, Tryll.ModelPlacementHint t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("placement");
            if (Enum.IsDefined(typeof(Tryll.MemoryPlacement), t.Placement)) w.String(t.Placement.ToString());
            else w.Int64(Convert.ToInt64(t.Placement));
            w.Key("hotwords");
            if (Enum.IsDefined(typeof(Tryll.HotwordBias), t.Hotwords)) w.String(t.Hotwords.ToString());
            else w.Int64(Convert.ToInt64(t.Hotwords));
            w.EndObject();
        }

        private static void Write_Tryll_ModelSampling(TryllJsonWriter w, Tryll.ModelSampling t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("temperature");
            w.Float(t.Temperature);
            w.Key("top_p");
            w.Float(t.TopP);
            w.Key("top_k");
            w.Int64(t.TopK);
            w.Key("min_p");
            w.Float(t.MinP);
            w.Key("max_tokens");
            w.Int64(t.MaxTokens);
            w.Key("seed");
            w.UInt64(t.Seed);
            w.Key("repeat_penalty");
            w.Float(t.RepeatPenalty);
            w.Key("presence_penalty");
            w.Float(t.PresencePenalty);
            w.Key("frequency_penalty");
            w.Float(t.FrequencyPenalty);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbClearRequest(TryllJsonWriter w, Tryll.MutableKbClearRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbClearResponse(TryllJsonWriter w, Tryll.MutableKbClearResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("cleared_count");
            w.UInt64(t.ClearedCount);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbDeleteRequest(TryllJsonWriter w, Tryll.MutableKbDeleteRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("ids");
                w.BeginArray();
                for (int i = 0; i < t.IdsLength; i++)
                {
                    w.String(t.Ids(i) ?? "");
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbDeleteResponse(TryllJsonWriter w, Tryll.MutableKbDeleteResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("deleted_count");
            w.UInt64(t.DeletedCount);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbGetRequest(TryllJsonWriter w, Tryll.MutableKbGetRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbGetResponse(TryllJsonWriter w, Tryll.MutableKbGetResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("found");
            w.Bool(t.Found);
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("created_at_ms");
            w.UInt64(t.CreatedAtMs);
            w.Key("updated_at_ms");
            w.UInt64(t.UpdatedAtMs);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbHit(TryllJsonWriter w, Tryll.MutableKbHit t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("distance");
            w.Float(t.Distance);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbInfo(TryllJsonWriter w, Tryll.MutableKbInfo t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Schema != null)
            {
                w.Key("schema");
                w.String(t.Schema);
            }
            w.Key("live_count");
            w.UInt64(t.LiveCount);
            w.Key("write_generation");
            w.UInt64(t.WriteGeneration);
            w.Key("materialized");
            w.Bool(t.Materialized);
            w.Key("bytes_estimate");
            w.UInt64(t.BytesEstimate);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbListRequest(TryllJsonWriter w, Tryll.MutableKbListRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.Key("offset");
            w.UInt64(t.Offset);
            w.Key("limit");
            w.UInt64(t.Limit);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbListResponse(TryllJsonWriter w, Tryll.MutableKbListResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("records");
                w.BeginArray();
                for (int i = 0; i < t.RecordsLength; i++)
                {
                    var elem = t.Records(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MutableKbRecord(w, elem, elemPos, bb, prefix + "records.", omitted);
                }
                w.EndArray();
            }
            w.Key("next_offset");
            w.UInt64(t.NextOffset);
            w.Key("has_more");
            w.Bool(t.HasMore);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbQueryRequest(TryllJsonWriter w, Tryll.MutableKbQueryRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.QueryText != null)
            {
                w.Key("query_text");
                w.String(t.QueryText);
            }
            w.Key("top_k");
            w.UInt64(t.TopK);
            w.Key("retrieval_mode");
            if (Enum.IsDefined(typeof(Tryll.RetrievalMode), t.RetrievalMode)) w.String(t.RetrievalMode.ToString());
            else w.Int64(Convert.ToInt64(t.RetrievalMode));
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbQueryResponse(TryllJsonWriter w, Tryll.MutableKbQueryResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("hits");
                w.BeginArray();
                for (int i = 0; i < t.HitsLength; i++)
                {
                    var elem = t.Hits(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MutableKbHit(w, elem, elemPos, bb, prefix + "hits.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbRecord(TryllJsonWriter w, Tryll.MutableKbRecord t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("created_at_ms");
            w.UInt64(t.CreatedAtMs);
            w.Key("updated_at_ms");
            w.UInt64(t.UpdatedAtMs);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbStatsRequest(TryllJsonWriter w, Tryll.MutableKbStatsRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbStatsResponse(TryllJsonWriter w, Tryll.MutableKbStatsResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("live_count");
            w.UInt64(t.LiveCount);
            w.Key("tombstoned_count");
            w.UInt64(t.TombstonedCount);
            w.Key("slot_count");
            w.UInt64(t.SlotCount);
            w.Key("write_generation");
            w.UInt64(t.WriteGeneration);
            w.Key("bytes_estimate");
            w.UInt64(t.BytesEstimate);
            w.Key("materialized");
            w.Bool(t.Materialized);
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbUpsert(TryllJsonWriter w, Tryll.MutableKbUpsert t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbWriteRequest(TryllJsonWriter w, Tryll.MutableKbWriteRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Schema != null)
            {
                w.Key("schema");
                w.String(t.Schema);
            }
            if (TryllFbNav.Present(bb, pos, 10))
            {
                w.Key("records");
                w.BeginArray();
                for (int i = 0; i < t.RecordsLength; i++)
                {
                    var elem = t.Records(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 10);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MutableKbUpsert(w, elem, elemPos, bb, prefix + "records.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbWriteResponse(TryllJsonWriter w, Tryll.MutableKbWriteResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("results");
                w.BeginArray();
                for (int i = 0; i < t.ResultsLength; i++)
                {
                    var elem = t.Results(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_MutableKbWriteResult(w, elem, elemPos, bb, prefix + "results.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_MutableKbWriteResult(TryllJsonWriter w, Tryll.MutableKbWriteResult t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            w.Key("inserted");
            w.Bool(t.Inserted);
            w.Key("accepted");
            w.Bool(t.Accepted);
            if (t.Error != null)
            {
                w.Key("error");
                w.String(t.Error);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeDescription(TryllJsonWriter w, Tryll.NodeDescription t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.ParamsType != Tryll.NodeParams.NodeParams.NONE)
            {
                w.Key("params_type");
                w.String(t.ParamsType.ToString());
                w.Key("params");
                switch (t.ParamsType)
                {
                    case Tryll.NodeParams.NodeParams.GenerateParams:
                        Write_Tryll_NodeParams_GenerateParams(w, t.ParamsAsGenerateParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.RegexGuardrailParams:
                        Write_Tryll_NodeParams_RegexGuardrailParams(w, t.ParamsAsRegexGuardrailParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.CannedResponseParams:
                        Write_Tryll_NodeParams_CannedResponseParams(w, t.ParamsAsCannedResponseParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ToolCallParams:
                        Write_Tryll_NodeParams_ToolCallParams(w, t.ParamsAsToolCallParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.RetrieveParams:
                        Write_Tryll_NodeParams_RetrieveParams(w, t.ParamsAsRetrieveParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.InstructionParams:
                        Write_Tryll_NodeParams_InstructionParams(w, t.ParamsAsInstructionParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ClassifyIntentParams:
                        Write_Tryll_NodeParams_ClassifyIntentParams(w, t.ParamsAsClassifyIntentParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ClassifyIntentLLMParams:
                        Write_Tryll_NodeParams_ClassifyIntentLLMParams(w, t.ParamsAsClassifyIntentLLMParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.IntentToInstructionParams:
                        Write_Tryll_NodeParams_IntentToInstructionParams(w, t.ParamsAsIntentToInstructionParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.GenerateAndSpeakParams:
                        Write_Tryll_NodeParams_GenerateAndSpeakParams(w, t.ParamsAsGenerateAndSpeakParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.SpeakParams:
                        Write_Tryll_NodeParams_SpeakParams(w, t.ParamsAsSpeakParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.PauseParams:
                        Write_Tryll_NodeParams_PauseParams(w, t.ParamsAsPauseParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.TransformParams:
                        Write_Tryll_NodeParams_TransformParams(w, t.ParamsAsTransformParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.BranchParams:
                        Write_Tryll_NodeParams_BranchParams(w, t.ParamsAsBranchParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.WriteToMemoryParams:
                        Write_Tryll_NodeParams_WriteToMemoryParams(w, t.ParamsAsWriteToMemoryParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.SummarizeParams:
                        Write_Tryll_NodeParams_SummarizeParams(w, t.ParamsAsSummarizeParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    case Tryll.NodeParams.NodeParams.ClassifyAttributesLLMParams:
                        Write_Tryll_NodeParams_ClassifyAttributesLLMParams(w, t.ParamsAsClassifyAttributesLLMParams(), TryllFbNav.Child(bb, pos, 8), bb, prefix + "params.", omitted);
                        break;
                    default:
                        w.BeginObject(); w.EndObject();
                        break;
                }
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeEvent(TryllJsonWriter w, Tryll.NodeEvent t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            if (t.EventType != null)
            {
                w.Key("event_type");
                w.String(t.EventType);
            }
            if (TryllFbNav.Present(bb, pos, 12))
            {
                w.Key("kv_pairs");
                w.BeginArray();
                for (int i = 0; i < t.KvPairsLength; i++)
                {
                    var elem = t.KvPairs(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 12);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParam(w, elem, elemPos, bb, prefix + "kv_pairs.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static string Label_Tryll_NodeEvent(Tryll.NodeEvent t)
        {
            var event_type = (t.EventType ?? "");
            var node_name = (t.NodeName ?? "");
            return TryllLogLabel.Format("{event_type} · {node_name}", ("event_type", event_type), ("node_name", node_name));
        }

        private static void Write_Tryll_NodeParam(TryllJsonWriter w, Tryll.NodeParam t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Key != null)
            {
                w.Key("key");
                w.String(t.Key);
            }
            if (t.Value != null)
            {
                w.Key("value");
                w.String(t.Value);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_BranchParams(TryllJsonWriter w, Tryll.NodeParams.BranchParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("test");
            if (Enum.IsDefined(typeof(Tryll.BranchTest), t.Test)) w.String(t.Test.ToString());
            else w.Int64(Convert.ToInt64(t.Test));
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.Variable != null)
            {
                w.Key("variable");
                w.String(t.Variable);
            }
            if (TryllFbNav.Present(bb, pos, 10))
            {
                w.Key("values");
                w.BeginArray();
                for (int i = 0; i < t.ValuesLength; i++)
                {
                    w.String(t.Values(i) ?? "");
                }
                w.EndArray();
            }
            if (t.Condition != null)
            {
                w.Key("condition");
                w.String(t.Condition);
            }
            w.Key("notify_client");
            w.Bool(t.NotifyClient);
            if (t.ThenExit != null)
            {
                w.Key("then_exit");
                w.String(t.ThenExit);
            }
            if (t.ElseExit != null)
            {
                w.Key("else_exit");
                w.String(t.ElseExit);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_CannedResponseParams(TryllJsonWriter w, Tryll.NodeParams.CannedResponseParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.StringStorage != null)
            {
                w.Key("string_storage");
                w.String(t.StringStorage);
            }
            w.Key("selection_strategy");
            if (Enum.IsDefined(typeof(Tryll.CannedResponseSelectionStrategy), t.SelectionStrategy)) w.String(t.SelectionStrategy.ToString());
            else w.Int64(Convert.ToInt64(t.SelectionStrategy));
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            w.Key("send");
            if (Enum.IsDefined(typeof(Tryll.SendAnswer), t.Send)) w.String(t.Send.ToString());
            else w.Int64(Convert.ToInt64(t.Send));
            w.Key("history_role");
            if (Enum.IsDefined(typeof(Tryll.HistoryRole), t.HistoryRole)) w.String(t.HistoryRole.ToString());
            else w.Int64(Convert.ToInt64(t.HistoryRole));
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            if (TryllFbNav.Present(bb, pos, 18))
            {
                w.Key("lines");
                w.BeginArray();
                for (int i = 0; i < t.LinesLength; i++)
                {
                    var elem = t.Lines(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 18);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_LocalizableText(w, elem, elemPos, bb, prefix + "lines.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ClassifyAttributeChoice(TryllJsonWriter w, Tryll.NodeParams.ClassifyAttributeChoice t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Prompt != null)
            {
                w.Key("prompt");
                w.String(t.Prompt);
            }
            w.Key("triggers");
            w.Bool(t.Triggers);
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ClassifyAttributeDef(TryllJsonWriter w, Tryll.NodeParams.ClassifyAttributeDef t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.Key("kind");
            if (Enum.IsDefined(typeof(Tryll.AttributeKind), t.Kind)) w.String(t.Kind.ToString());
            else w.Int64(Convert.ToInt64(t.Kind));
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("choices");
                w.BeginArray();
                for (int i = 0; i < t.ChoicesLength; i++)
                {
                    var elem = t.Choices(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 8);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_ClassifyAttributeChoice(w, elem, elemPos, bb, prefix + "choices.", omitted);
                }
                w.EndArray();
            }
            if (t.Statement != null)
            {
                w.Key("statement");
                w.String(t.Statement);
            }
            if (t.SystemPrompt != null)
            {
                w.Key("system_prompt");
                w.String(t.SystemPrompt);
            }
            w.Key("threshold");
            w.Float(t.Threshold);
            w.Key("margin");
            w.Float(t.Margin);
            if (t.UncertainChoice != null)
            {
                w.Key("uncertain_choice");
                w.String(t.UncertainChoice);
            }
            w.Key("boolean_trigger");
            if (Enum.IsDefined(typeof(Tryll.BooleanTrigger), t.BooleanTrigger)) w.String(t.BooleanTrigger.ToString());
            else w.Int64(Convert.ToInt64(t.BooleanTrigger));
            w.Key("score_trigger");
            if (Enum.IsDefined(typeof(Tryll.ScoreTrigger), t.ScoreTrigger)) w.String(t.ScoreTrigger.ToString());
            else w.Int64(Convert.ToInt64(t.ScoreTrigger));
            w.Key("score_threshold");
            w.Float(t.ScoreThreshold);
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ClassifyAttributesLLMParams(TryllJsonWriter w, Tryll.NodeParams.ClassifyAttributesLLMParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("context_size");
            w.Int64(t.ContextSize);
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("attributes");
                w.BeginArray();
                for (int i = 0; i < t.AttributesLength; i++)
                {
                    var elem = t.Attributes(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 8);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_ClassifyAttributeDef(w, elem, elemPos, bb, prefix + "attributes.", omitted);
                }
                w.EndArray();
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            w.Key("history_turns");
            w.Int64(t.HistoryTurns);
            w.Key("temperature");
            w.Float(t.Temperature);
            w.Key("on_failure");
            if (Enum.IsDefined(typeof(Tryll.AttributeFailureRoute), t.OnFailure)) w.String(t.OnFailure.ToString());
            else w.Int64(Convert.ToInt64(t.OnFailure));
            w.Key("notify_client");
            if (Enum.IsDefined(typeof(Tryll.NotifyClient), t.NotifyClient)) w.String(t.NotifyClient.ToString());
            else w.Int64(Convert.ToInt64(t.NotifyClient));
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            if (t.TriggeredExit != null)
            {
                w.Key("triggered_exit");
                w.String(t.TriggeredExit);
            }
            if (t.Context.HasValue)
            {
                w.Key("context");
                var childPos = TryllFbNav.Child(bb, pos, 26);
                Write_Tryll_NodeParams_ContextOverrides(w, t.Context.Value, childPos, bb, prefix + "context.", omitted);
            }
            if (t.Adaptation.HasValue)
            {
                w.Key("adaptation");
                var childPos = TryllFbNav.Child(bb, pos, 28);
                Write_Tryll_NodeParams_ModelAdaptation(w, t.Adaptation.Value, childPos, bb, prefix + "adaptation.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ClassifyIntentEntry(TryllJsonWriter w, Tryll.NodeParams.ClassifyIntentEntry t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Prompt != null)
            {
                w.Key("prompt");
                w.String(t.Prompt);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ClassifyIntentLLMParams(TryllJsonWriter w, Tryll.NodeParams.ClassifyIntentLLMParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("context_size");
            w.Int64(t.ContextSize);
            if (t.SystemPrompt != null)
            {
                w.Key("system_prompt");
                w.String(t.SystemPrompt);
            }
            if (TryllFbNav.Present(bb, pos, 10))
            {
                w.Key("intents");
                w.BeginArray();
                for (int i = 0; i < t.IntentsLength; i++)
                {
                    var elem = t.Intents(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 10);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_ClassifyIntentEntry(w, elem, elemPos, bb, prefix + "intents.", omitted);
                }
                w.EndArray();
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            w.Key("history_turns");
            w.Int64(t.HistoryTurns);
            w.Key("threshold");
            w.Float(t.Threshold);
            w.Key("margin");
            w.Float(t.Margin);
            w.Key("temperature");
            w.Float(t.Temperature);
            w.Key("notify_client");
            if (Enum.IsDefined(typeof(Tryll.NotifyClient), t.NotifyClient)) w.String(t.NotifyClient.ToString());
            else w.Int64(Convert.ToInt64(t.NotifyClient));
            if (t.NotFoundIntent != null)
            {
                w.Key("not_found_intent");
                w.String(t.NotFoundIntent);
            }
            if (t.FoundExit != null)
            {
                w.Key("found_exit");
                w.String(t.FoundExit);
            }
            if (t.NotFoundExit != null)
            {
                w.Key("not_found_exit");
                w.String(t.NotFoundExit);
            }
            if (t.Context.HasValue)
            {
                w.Key("context");
                var childPos = TryllFbNav.Child(bb, pos, 32);
                Write_Tryll_NodeParams_ContextOverrides(w, t.Context.Value, childPos, bb, prefix + "context.", omitted);
            }
            if (t.Adaptation.HasValue)
            {
                w.Key("adaptation");
                var childPos = TryllFbNav.Child(bb, pos, 38);
                Write_Tryll_NodeParams_ModelAdaptation(w, t.Adaptation.Value, childPos, bb, prefix + "adaptation.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ClassifyIntentParams(TryllJsonWriter w, Tryll.NodeParams.ClassifyIntentParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.EmbeddedStringStorage != null)
            {
                w.Key("embedded_string_storage");
                w.String(t.EmbeddedStringStorage);
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.MetadataField != null)
            {
                w.Key("metadata_field");
                w.String(t.MetadataField);
            }
            w.Key("threshold");
            w.Float(t.Threshold);
            if (t.Filter != null)
            {
                w.Key("filter");
                w.String(t.Filter);
            }
            w.Key("notify_client");
            if (Enum.IsDefined(typeof(Tryll.NotifyClient), t.NotifyClient)) w.String(t.NotifyClient.ToString());
            else w.Int64(Convert.ToInt64(t.NotifyClient));
            w.Key("diagnostic_topk");
            w.Int64(t.DiagnosticTopk);
            if (t.FoundExit != null)
            {
                w.Key("found_exit");
                w.String(t.FoundExit);
            }
            if (t.NotFoundExit != null)
            {
                w.Key("not_found_exit");
                w.String(t.NotFoundExit);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ContextOverrides(TryllJsonWriter w, Tryll.NodeParams.ContextOverrides t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("kv_cache_type");
            if (Enum.IsDefined(typeof(Tryll.KvCacheType), t.KvCacheType)) w.String(t.KvCacheType.ToString());
            else w.Int64(Convert.ToInt64(t.KvCacheType));
            w.Key("offload_kqv");
            if (t.OffloadKqv.HasValue)
                w.Bool(t.OffloadKqv.Value);
            else w.Null();
            w.Key("n_ubatch");
            if (t.NUbatch.HasValue)
                w.Int64(t.NUbatch.Value);
            else w.Null();
            w.Key("n_outputs_max");
            if (t.NOutputsMax.HasValue)
                w.Int64(t.NOutputsMax.Value);
            else w.Null();
            w.Key("n_batch");
            if (t.NBatch.HasValue)
                w.Int64(t.NBatch.Value);
            else w.Null();
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_GenerateAndSpeakParams(TryllJsonWriter w, Tryll.NodeParams.GenerateAndSpeakParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("context_size");
            w.Int64(t.ContextSize);
            if (t.SystemPrompt != null)
            {
                w.Key("system_prompt");
                w.String(t.SystemPrompt);
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.Template != null)
            {
                w.Key("template");
                w.String(t.Template);
            }
            w.Key("placement");
            if (Enum.IsDefined(typeof(Tryll.Placement), t.Placement)) w.String(t.Placement.ToString());
            else w.Int64(Convert.ToInt64(t.Placement));
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            w.Key("history_role");
            if (Enum.IsDefined(typeof(Tryll.HistoryRole), t.HistoryRole)) w.String(t.HistoryRole.ToString());
            else w.Int64(Convert.ToInt64(t.HistoryRole));
            if (t.Sampling.HasValue)
            {
                w.Key("sampling");
                var childPos = TryllFbNav.Child(bb, pos, 20);
                Write_Tryll_NodeParams_SamplingOverrides(w, t.Sampling.Value, childPos, bb, prefix + "sampling.", omitted);
            }
            if (t.Grammar != null)
            {
                w.Key("grammar");
                w.String(t.Grammar);
            }
            w.Key("min_sentence_chars");
            w.Int64(t.MinSentenceChars);
            w.Key("first_chunk_max_chars");
            w.Int64(t.FirstChunkMaxChars);
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            w.Key("substitute_agent_variables");
            w.Bool(t.SubstituteAgentVariables);
            if (t.OutputFilter.HasValue)
            {
                w.Key("output_filter");
                var childPos = TryllFbNav.Child(bb, pos, 32);
                Write_Tryll_NodeParams_OutputFilterOverrides(w, t.OutputFilter.Value, childPos, bb, prefix + "output_filter.", omitted);
            }
            if (t.Context.HasValue)
            {
                w.Key("context");
                var childPos = TryllFbNav.Child(bb, pos, 34);
                Write_Tryll_NodeParams_ContextOverrides(w, t.Context.Value, childPos, bb, prefix + "context.", omitted);
            }
            if (t.Adaptation.HasValue)
            {
                w.Key("adaptation");
                var childPos = TryllFbNav.Child(bb, pos, 36);
                Write_Tryll_NodeParams_ModelAdaptation(w, t.Adaptation.Value, childPos, bb, prefix + "adaptation.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_GenerateParams(TryllJsonWriter w, Tryll.NodeParams.GenerateParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("context_size");
            w.Int64(t.ContextSize);
            if (t.SystemPrompt != null)
            {
                w.Key("system_prompt");
                w.String(t.SystemPrompt);
            }
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.Template != null)
            {
                w.Key("template");
                w.String(t.Template);
            }
            w.Key("placement");
            if (Enum.IsDefined(typeof(Tryll.Placement), t.Placement)) w.String(t.Placement.ToString());
            else w.Int64(Convert.ToInt64(t.Placement));
            w.Key("send");
            if (Enum.IsDefined(typeof(Tryll.SendAnswer), t.Send)) w.String(t.Send.ToString());
            else w.Int64(Convert.ToInt64(t.Send));
            w.Key("history_role");
            if (Enum.IsDefined(typeof(Tryll.HistoryRole), t.HistoryRole)) w.String(t.HistoryRole.ToString());
            else w.Int64(Convert.ToInt64(t.HistoryRole));
            if (t.Sampling.HasValue)
            {
                w.Key("sampling");
                var childPos = TryllFbNav.Child(bb, pos, 22);
                Write_Tryll_NodeParams_SamplingOverrides(w, t.Sampling.Value, childPos, bb, prefix + "sampling.", omitted);
            }
            if (t.Grammar != null)
            {
                w.Key("grammar");
                w.String(t.Grammar);
            }
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            w.Key("substitute_agent_variables");
            w.Bool(t.SubstituteAgentVariables);
            if (t.OutputFilter.HasValue)
            {
                w.Key("output_filter");
                var childPos = TryllFbNav.Child(bb, pos, 30);
                Write_Tryll_NodeParams_OutputFilterOverrides(w, t.OutputFilter.Value, childPos, bb, prefix + "output_filter.", omitted);
            }
            if (t.Context.HasValue)
            {
                w.Key("context");
                var childPos = TryllFbNav.Child(bb, pos, 32);
                Write_Tryll_NodeParams_ContextOverrides(w, t.Context.Value, childPos, bb, prefix + "context.", omitted);
            }
            if (t.Adaptation.HasValue)
            {
                w.Key("adaptation");
                var childPos = TryllFbNav.Child(bb, pos, 34);
                Write_Tryll_NodeParams_ModelAdaptation(w, t.Adaptation.Value, childPos, bb, prefix + "adaptation.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_InstructionParams(TryllJsonWriter w, Tryll.NodeParams.InstructionParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Instruction != null)
            {
                w.Key("instruction");
                w.String(t.Instruction);
            }
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_IntentToInstructionParams(TryllJsonWriter w, Tryll.NodeParams.IntentToInstructionParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.StringStorage != null)
            {
                w.Key("string_storage");
                w.String(t.StringStorage);
            }
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("inline_keys");
                w.BeginArray();
                for (int i = 0; i < t.InlineKeysLength; i++)
                {
                    w.String(t.InlineKeys(i) ?? "");
                }
                w.EndArray();
            }
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            if (TryllFbNav.Present(bb, pos, 14))
            {
                w.Key("instructions");
                w.BeginArray();
                for (int i = 0; i < t.InstructionsLength; i++)
                {
                    var elem = t.Instructions(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 14);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_LocalizableText(w, elem, elemPos, bb, prefix + "instructions.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_LocalizableText(TryllJsonWriter w, Tryll.NodeParams.LocalizableText t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_LoraAdapter(TryllJsonWriter w, Tryll.NodeParams.LoraAdapter t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Path != null)
            {
                w.Key("path");
                w.String(t.Path);
            }
            w.Key("scale");
            w.Float(t.Scale);
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ModelAdaptation(TryllJsonWriter w, Tryll.NodeParams.ModelAdaptation t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (TryllFbNav.Present(bb, pos, 4))
            {
                w.Key("loras");
                w.BeginArray();
                for (int i = 0; i < t.LorasLength; i++)
                {
                    var elem = t.Loras(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 4);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_LoraAdapter(w, elem, elemPos, bb, prefix + "loras.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_OutputFilterOverrides(TryllJsonWriter w, Tryll.NodeParams.OutputFilterOverrides t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("strip_speaker_prefix");
            w.Bool(t.StripSpeakerPrefix);
            w.Key("max_prefix_bytes");
            w.Int64(t.MaxPrefixBytes);
            w.Key("strip_asterisk_spans");
            w.Bool(t.StripAsteriskSpans);
            w.Key("strip_paren_spans");
            w.Bool(t.StripParenSpans);
            w.Key("strip_bracket_spans");
            w.Bool(t.StripBracketSpans);
            w.Key("max_span_bytes");
            w.Int64(t.MaxSpanBytes);
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_PauseParams(TryllJsonWriter w, Tryll.NodeParams.PauseParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            w.Key("pause_timeout_ms");
            w.Int64(t.PauseTimeoutMs);
            if (t.TimeoutExit != null)
            {
                w.Key("timeout_exit");
                w.String(t.TimeoutExit);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_RegexGuardrailParams(TryllJsonWriter w, Tryll.NodeParams.RegexGuardrailParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.StringStorage != null)
            {
                w.Key("string_storage");
                w.String(t.StringStorage);
            }
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("inline_strings");
                w.BeginArray();
                for (int i = 0; i < t.InlineStringsLength; i++)
                {
                    w.String(t.InlineStrings(i) ?? "");
                }
                w.EndArray();
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.TriggeredExit != null)
            {
                w.Key("triggered_exit");
                w.String(t.TriggeredExit);
            }
            if (t.NotTriggeredExit != null)
            {
                w.Key("not_triggered_exit");
                w.String(t.NotTriggeredExit);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_RetrieveParams(TryllJsonWriter w, Tryll.NodeParams.RetrieveParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.EmbeddedStringStorage != null)
            {
                w.Key("embedded_string_storage");
                w.String(t.EmbeddedStringStorage);
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            w.Key("top_k");
            w.Int64(t.TopK);
            w.Key("threshold");
            w.Float(t.Threshold);
            w.Key("retrieval_mode");
            if (Enum.IsDefined(typeof(Tryll.RetrievalMode), t.RetrievalMode)) w.String(t.RetrievalMode.ToString());
            else w.Int64(Convert.ToInt64(t.RetrievalMode));
            w.Key("rrf_k");
            w.Int64(t.RrfK);
            if (t.Source != null)
            {
                w.Key("source");
                w.String(t.Source);
            }
            if (t.Filter != null)
            {
                w.Key("filter");
                w.String(t.Filter);
            }
            if (t.FoundExit != null)
            {
                w.Key("found_exit");
                w.String(t.FoundExit);
            }
            if (t.NotFoundExit != null)
            {
                w.Key("not_found_exit");
                w.String(t.NotFoundExit);
            }
            if (t.StorageName != null)
            {
                w.Key("storage_name");
                w.String(t.StorageName);
            }
            if (t.Schema != null)
            {
                w.Key("schema");
                w.String(t.Schema);
            }
            w.Key("source_kind");
            if (Enum.IsDefined(typeof(Tryll.KnowledgeSourceKind), t.SourceKind)) w.String(t.SourceKind.ToString());
            else w.Int64(Convert.ToInt64(t.SourceKind));
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_SamplingOverrides(TryllJsonWriter w, Tryll.NodeParams.SamplingOverrides t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("temperature");
            if (t.Temperature.HasValue)
                w.Float(t.Temperature.Value);
            else w.Null();
            w.Key("top_p");
            if (t.TopP.HasValue)
                w.Float(t.TopP.Value);
            else w.Null();
            w.Key("top_k");
            if (t.TopK.HasValue)
                w.Int64(t.TopK.Value);
            else w.Null();
            w.Key("min_p");
            if (t.MinP.HasValue)
                w.Float(t.MinP.Value);
            else w.Null();
            w.Key("max_tokens");
            if (t.MaxTokens.HasValue)
                w.Int64(t.MaxTokens.Value);
            else w.Null();
            w.Key("seed");
            if (t.Seed.HasValue)
                w.Int64(t.Seed.Value);
            else w.Null();
            w.Key("repeat_penalty");
            if (t.RepeatPenalty.HasValue)
                w.Float(t.RepeatPenalty.Value);
            else w.Null();
            w.Key("presence_penalty");
            if (t.PresencePenalty.HasValue)
                w.Float(t.PresencePenalty.Value);
            else w.Null();
            w.Key("frequency_penalty");
            if (t.FrequencyPenalty.HasValue)
                w.Float(t.FrequencyPenalty.Value);
            else w.Null();
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_SpeakParams(TryllJsonWriter w, Tryll.NodeParams.SpeakParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            w.Key("min_sentence_chars");
            w.Int64(t.MinSentenceChars);
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            w.Key("first_chunk_max_chars");
            w.Int64(t.FirstChunkMaxChars);
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_SummarizeParams(TryllJsonWriter w, Tryll.NodeParams.SummarizeParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("context_size");
            w.Int64(t.ContextSize);
            if (t.SystemPrompt != null)
            {
                w.Key("system_prompt");
                w.String(t.SystemPrompt);
            }
            if (t.Template != null)
            {
                w.Key("template");
                w.String(t.Template);
            }
            w.Key("trigger_tokens");
            w.Int64(t.TriggerTokens);
            w.Key("trigger_turns");
            w.Int64(t.TriggerTurns);
            w.Key("keep_recent_turns");
            w.Int64(t.KeepRecentTurns);
            w.Key("max_summary_tokens");
            w.Int64(t.MaxSummaryTokens);
            if (t.Sampling.HasValue)
            {
                w.Key("sampling");
                var childPos = TryllFbNav.Child(bb, pos, 20);
                Write_Tryll_NodeParams_SamplingOverrides(w, t.Sampling.Value, childPos, bb, prefix + "sampling.", omitted);
            }
            w.Key("notify_client");
            w.Bool(t.NotifyClient);
            w.Key("run_inline");
            w.Bool(t.RunInline);
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            if (t.Context.HasValue)
            {
                w.Key("context");
                var childPos = TryllFbNav.Child(bb, pos, 28);
                Write_Tryll_NodeParams_ContextOverrides(w, t.Context.Value, childPos, bb, prefix + "context.", omitted);
            }
            if (t.Adaptation.HasValue)
            {
                w.Key("adaptation");
                var childPos = TryllFbNav.Child(bb, pos, 30);
                Write_Tryll_NodeParams_ModelAdaptation(w, t.Adaptation.Value, childPos, bb, prefix + "adaptation.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ToolCallParams(TryllJsonWriter w, Tryll.NodeParams.ToolCallParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.Key("context_size");
            w.Int64(t.ContextSize);
            if (t.SystemPrompt != null)
            {
                w.Key("system_prompt");
                w.String(t.SystemPrompt);
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.ToolCallFormat != null)
            {
                w.Key("tool_call_format");
                w.String(t.ToolCallFormat);
            }
            if (TryllFbNav.Present(bb, pos, 14))
            {
                w.Key("tools");
                w.BeginArray();
                for (int i = 0; i < t.ToolsLength; i++)
                {
                    var elem = t.Tools(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 14);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_ToolDefinition(w, elem, elemPos, bb, prefix + "tools.", omitted);
                }
                w.EndArray();
            }
            w.Key("mode");
            if (Enum.IsDefined(typeof(Tryll.ToolCallMode), t.Mode)) w.String(t.Mode.ToString());
            else w.Int64(Convert.ToInt64(t.Mode));
            w.Key("parallel_tool_calls");
            w.Bool(t.ParallelToolCalls);
            if (t.Sampling.HasValue)
            {
                w.Key("sampling");
                var childPos = TryllFbNav.Child(bb, pos, 20);
                Write_Tryll_NodeParams_SamplingOverrides(w, t.Sampling.Value, childPos, bb, prefix + "sampling.", omitted);
            }
            if (t.ToolCalledExit != null)
            {
                w.Key("tool_called_exit");
                w.String(t.ToolCalledExit);
            }
            if (t.NoToolCalledExit != null)
            {
                w.Key("no_tool_called_exit");
                w.String(t.NoToolCalledExit);
            }
            w.Key("disposition");
            if (Enum.IsDefined(typeof(Tryll.ToolCallDisposition), t.Disposition)) w.String(t.Disposition.ToString());
            else w.Int64(Convert.ToInt64(t.Disposition));
            if (t.AcknowledgeText != null)
            {
                w.Key("acknowledge_text");
                w.String(t.AcknowledgeText);
            }
            w.Key("pause_timeout_ms");
            w.Int64(t.PauseTimeoutMs);
            if (t.Context.HasValue)
            {
                w.Key("context");
                var childPos = TryllFbNav.Child(bb, pos, 32);
                Write_Tryll_NodeParams_ContextOverrides(w, t.Context.Value, childPos, bb, prefix + "context.", omitted);
            }
            if (t.Adaptation.HasValue)
            {
                w.Key("adaptation");
                var childPos = TryllFbNav.Child(bb, pos, 34);
                Write_Tryll_NodeParams_ModelAdaptation(w, t.Adaptation.Value, childPos, bb, prefix + "adaptation.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ToolDefinition(TryllJsonWriter w, Tryll.NodeParams.ToolDefinition t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Description != null)
            {
                w.Key("description");
                w.String(t.Description);
            }
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("parameters");
                w.BeginArray();
                for (int i = 0; i < t.ParametersLength; i++)
                {
                    var elem = t.Parameters(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 8);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_NodeParams_ToolParamDefinition(w, elem, elemPos, bb, prefix + "parameters.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_ToolParamDefinition(TryllJsonWriter w, Tryll.NodeParams.ToolParamDefinition t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Type != null)
            {
                w.Key("type");
                w.String(t.Type);
            }
            if (t.Description != null)
            {
                w.Key("description");
                w.String(t.Description);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_TransformParams(TryllJsonWriter w, Tryll.NodeParams.TransformParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Template != null)
            {
                w.Key("template");
                w.String(t.Template);
            }
            if (t.OutputName != null)
            {
                w.Key("output_name");
                w.String(t.OutputName);
            }
            if (t.DefaultExit != null)
            {
                w.Key("default_exit");
                w.String(t.DefaultExit);
            }
            w.EndObject();
        }

        private static void Write_Tryll_NodeParams_WriteToMemoryParams(TryllJsonWriter w, Tryll.NodeParams.WriteToMemoryParams t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.StorageName != null)
            {
                w.Key("storage_name");
                w.String(t.StorageName);
            }
            if (t.Schema != null)
            {
                w.Key("schema");
                w.String(t.Schema);
            }
            if (t.Input != null)
            {
                w.Key("input");
                w.String(t.Input);
            }
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.WrittenExit != null)
            {
                w.Key("written_exit");
                w.String(t.WrittenExit);
            }
            if (t.FailedExit != null)
            {
                w.Key("failed_exit");
                w.String(t.FailedExit);
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedComponentEntry(TryllJsonWriter w, Tryll.PersistedComponentEntry t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ValueType != Tryll.PersistedComponent.NONE)
            {
                w.Key("value_type");
                w.String(t.ValueType.ToString());
                w.Key("value");
                switch (t.ValueType)
                {
                    case Tryll.PersistedComponent.PersistedSlot:
                        Write_Tryll_PersistedSlot(w, t.ValueAsPersistedSlot(), TryllFbNav.Child(bb, pos, 6), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.PersistedComponent.PersistedKnowledge:
                        Write_Tryll_PersistedKnowledge(w, t.ValueAsPersistedKnowledge(), TryllFbNav.Child(bb, pos, 6), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.PersistedComponent.PersistedIntention:
                        Write_Tryll_PersistedIntention(w, t.ValueAsPersistedIntention(), TryllFbNav.Child(bb, pos, 6), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.PersistedComponent.PersistedToolCall:
                        Write_Tryll_PersistedToolCall(w, t.ValueAsPersistedToolCall(), TryllFbNav.Child(bb, pos, 6), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.PersistedComponent.PersistedToolResult:
                        Write_Tryll_PersistedToolResult(w, t.ValueAsPersistedToolResult(), TryllFbNav.Child(bb, pos, 6), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.PersistedComponent.PersistedSummary:
                        Write_Tryll_PersistedSummary(w, t.ValueAsPersistedSummary(), TryllFbNav.Child(bb, pos, 6), bb, prefix + "value.", omitted);
                        break;
                    default:
                        w.BeginObject(); w.EndObject();
                        break;
                }
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedDialogSnapshot(TryllJsonWriter w, Tryll.PersistedDialogSnapshot t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("format_version");
            w.UInt64(t.FormatVersion);
            if (t.GraphSignature != null)
            {
                w.Key("graph_signature");
                w.String(t.GraphSignature);
            }
            {
                var path = (string.IsNullOrEmpty(prefix) ? "interactions" : prefix + "interactions");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 8) || inMap)
                {
                    w.Key("interactions");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.InteractionsLength, (long)t.InteractionsLength * 4L);
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            w.Key("next_tool_call_seq");
            w.UInt64(t.NextToolCallSeq);
            w.Key("next_exchange_id");
            w.UInt64(t.NextExchangeId);
            {
                var path = (string.IsNullOrEmpty(prefix) ? "projection_windows" : prefix + "projection_windows");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 14) || inMap)
                {
                    w.Key("projection_windows");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.ProjectionWindowsLength, (long)t.ProjectionWindowsLength * 4L);
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            if (t.SaveId != null)
            {
                w.Key("save_id");
                w.String(t.SaveId);
            }
            w.Key("saved_at_ms");
            w.UInt64(t.SavedAtMs);
            w.EndObject();
        }

        private static int Pack_Tryll_PersistedDialogSnapshot(FlatBufferBuilder fbb, Tryll.PersistedDialogSnapshot t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _graph_signature = t.GraphSignature == null ? default(StringOffset) : fbb.CreateString(t.GraphSignature);
            var _interactions = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 8))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "interactions" : prefix + "interactions");
                omitted.Add(new TryllOmittedField(path, t.InteractionsLength, (long)t.InteractionsLength * 4L));
                _interactions = Tryll.PersistedDialogSnapshot.CreateInteractionsVector(fbb, new Offset<Tryll.PersistedInteraction>[0]);
            }
            var _projection_windows = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 14))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "projection_windows" : prefix + "projection_windows");
                omitted.Add(new TryllOmittedField(path, t.ProjectionWindowsLength, (long)t.ProjectionWindowsLength * 4L));
                _projection_windows = Tryll.PersistedDialogSnapshot.CreateProjectionWindowsVector(fbb, new Offset<Tryll.PersistedProjectionWindow>[0]);
            }
            var _save_id = t.SaveId == null ? default(StringOffset) : fbb.CreateString(t.SaveId);
            return Tryll.PersistedDialogSnapshot.CreatePersistedDialogSnapshot(fbb, t.FormatVersion, _graph_signature, _interactions, t.NextToolCallSeq, t.NextExchangeId, _projection_windows, _save_id, t.SavedAtMs).Value;
        }

        private static void Write_Tryll_PersistedIntention(TryllJsonWriter w, Tryll.PersistedIntention t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Intent != null)
            {
                w.Key("intent");
                w.String(t.Intent);
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedInteraction(TryllJsonWriter w, Tryll.PersistedInteraction t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("interrupted");
            w.Bool(t.Interrupted);
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("components");
                w.BeginArray();
                for (int i = 0; i < t.ComponentsLength; i++)
                {
                    var elem = t.Components(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_PersistedComponentEntry(w, elem, elemPos, bb, prefix + "components.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedKbRecord(TryllJsonWriter w, Tryll.PersistedKbRecord t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("created_at_ms");
            w.UInt64(t.CreatedAtMs);
            w.Key("updated_at_ms");
            w.UInt64(t.UpdatedAtMs);
            w.EndObject();
        }

        private static void Write_Tryll_PersistedKnowledge(TryllJsonWriter w, Tryll.PersistedKnowledge t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Source != null)
            {
                w.Key("source");
                w.String(t.Source);
            }
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("chunks");
                w.BeginArray();
                for (int i = 0; i < t.ChunksLength; i++)
                {
                    var elem = t.Chunks(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_PersistedKnowledgeChunk(w, elem, elemPos, bb, prefix + "chunks.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedKnowledgeChunk(TryllJsonWriter w, Tryll.PersistedKnowledgeChunk t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("distance");
            w.Float(t.Distance);
            w.Key("distance_is_nan");
            w.Bool(t.DistanceIsNan);
            w.Key("bm25_score");
            if (t.Bm25Score.HasValue)
                w.Float(t.Bm25Score.Value);
            else w.Null();
            w.Key("rank_dense");
            if (t.RankDense.HasValue)
                w.UInt64(t.RankDense.Value);
            else w.Null();
            w.Key("rank_sparse");
            if (t.RankSparse.HasValue)
                w.UInt64(t.RankSparse.Value);
            else w.Null();
            w.EndObject();
        }

        private static void Write_Tryll_PersistedMutableKb(TryllJsonWriter w, Tryll.PersistedMutableKb t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.SchemaName != null)
            {
                w.Key("schema_name");
                w.String(t.SchemaName);
            }
            if (t.SchemaFingerprint != null)
            {
                w.Key("schema_fingerprint");
                w.String(t.SchemaFingerprint);
            }
            if (t.EmbeddingModel != null)
            {
                w.Key("embedding_model");
                w.String(t.EmbeddingModel);
            }
            w.Key("write_generation");
            w.UInt64(t.WriteGeneration);
            w.Key("next_generated_id");
            w.UInt64(t.NextGeneratedId);
            {
                var path = (string.IsNullOrEmpty(prefix) ? "records" : prefix + "records");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 16) || inMap)
                {
                    w.Key("records");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.RecordsLength, (long)t.RecordsLength * 4L);
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            w.EndObject();
        }

        private static int Pack_Tryll_PersistedMutableKb(FlatBufferBuilder fbb, Tryll.PersistedMutableKb t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _name = t.Name == null ? default(StringOffset) : fbb.CreateString(t.Name);
            var _schema_name = t.SchemaName == null ? default(StringOffset) : fbb.CreateString(t.SchemaName);
            var _schema_fingerprint = t.SchemaFingerprint == null ? default(StringOffset) : fbb.CreateString(t.SchemaFingerprint);
            var _embedding_model = t.EmbeddingModel == null ? default(StringOffset) : fbb.CreateString(t.EmbeddingModel);
            var _records = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 16))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "records" : prefix + "records");
                omitted.Add(new TryllOmittedField(path, t.RecordsLength, (long)t.RecordsLength * 4L));
                _records = Tryll.PersistedMutableKb.CreateRecordsVector(fbb, new Offset<Tryll.PersistedKbRecord>[0]);
            }
            return Tryll.PersistedMutableKb.CreatePersistedMutableKb(fbb, _name, _schema_name, _schema_fingerprint, _embedding_model, t.WriteGeneration, t.NextGeneratedId, _records).Value;
        }

        private static void Write_Tryll_PersistedProjectionWindow(TryllJsonWriter w, Tryll.PersistedProjectionWindow t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            w.Key("window_start");
            w.UInt64(t.WindowStart);
            w.EndObject();
        }

        private static void Write_Tryll_PersistedSlot(TryllJsonWriter w, Tryll.PersistedSlot t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("kind");
            if (Enum.IsDefined(typeof(Tryll.PersistedSlotKind), t.Kind)) w.String(t.Kind.ToString());
            else w.Int64(Convert.ToInt64(t.Kind));
            w.Key("history_role");
            if (Enum.IsDefined(typeof(Tryll.HistoryRole), t.HistoryRole)) w.String(t.HistoryRole.ToString());
            else w.Int64(Convert.ToInt64(t.HistoryRole));
            if (t.Producer != null)
            {
                w.Key("producer");
                w.String(t.Producer);
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedStringPair(TryllJsonWriter w, Tryll.PersistedStringPair t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Key != null)
            {
                w.Key("key");
                w.String(t.Key);
            }
            if (t.Value != null)
            {
                w.Key("value");
                w.String(t.Value);
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedSummary(TryllJsonWriter w, Tryll.PersistedSummary t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("covered_through");
            w.UInt64(t.CoveredThrough);
            if (t.Producer != null)
            {
                w.Key("producer");
                w.String(t.Producer);
            }
            w.EndObject();
        }

        private static void Write_Tryll_PersistedToolCall(TryllJsonWriter w, Tryll.PersistedToolCall t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.ToolName != null)
            {
                w.Key("tool_name");
                w.String(t.ToolName);
            }
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("arguments");
                w.BeginArray();
                for (int i = 0; i < t.ArgumentsLength; i++)
                {
                    var elem = t.Arguments(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_PersistedStringPair(w, elem, elemPos, bb, prefix + "arguments.", omitted);
                }
                w.EndArray();
            }
            if (t.ArgumentsJson != null)
            {
                w.Key("arguments_json");
                w.String(t.ArgumentsJson);
            }
            w.Key("dispatched");
            w.Bool(t.Dispatched);
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            w.Key("disposition");
            if (Enum.IsDefined(typeof(Tryll.ToolCallDisposition), t.Disposition)) w.String(t.Disposition.ToString());
            else w.Int64(Convert.ToInt64(t.Disposition));
            if (t.ModelId != null)
            {
                w.Key("model_id");
                w.String(t.ModelId);
            }
            w.Key("exchange_id");
            w.UInt64(t.ExchangeId);
            w.EndObject();
        }

        private static void Write_Tryll_PersistedToolResult(TryllJsonWriter w, Tryll.PersistedToolResult t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.CallId != null)
            {
                w.Key("call_id");
                w.String(t.CallId);
            }
            if (t.ToolName != null)
            {
                w.Key("tool_name");
                w.String(t.ToolName);
            }
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("exchange_id");
            w.UInt64(t.ExchangeId);
            w.EndObject();
        }

        private static void Write_Tryll_PrefillAgentKvCacheRequest(TryllJsonWriter w, Tryll.PrefillAgentKvCacheRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.EndObject();
        }

        private static void Write_Tryll_PrefillAgentKvCacheResponse(TryllJsonWriter w, Tryll.PrefillAgentKvCacheResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("applicable");
            w.Bool(t.Applicable);
            w.Key("residency");
            if (Enum.IsDefined(typeof(Tryll.AgentKvCacheResidency), t.Residency)) w.String(t.Residency.ToString());
            else w.Int64(Convert.ToInt64(t.Residency));
            w.Key("prefix_status");
            if (Enum.IsDefined(typeof(Tryll.AgentKvCachePrefixStatus), t.PrefixStatus)) w.String(t.PrefixStatus.ToString());
            else w.Int64(Convert.ToInt64(t.PrefixStatus));
            w.Key("eligible_node_count");
            w.UInt64(t.EligibleNodeCount);
            w.Key("contexts_created");
            w.UInt64(t.ContextsCreated);
            w.Key("tokens_decoded");
            w.UInt64(t.TokensDecoded);
            w.Key("tokens_reused");
            w.UInt64(t.TokensReused);
            if (t.DebugInfo != null)
            {
                w.Key("debug_info");
                w.String(t.DebugInfo);
            }
            w.EndObject();
        }

        private static void Write_Tryll_RemoveInteractionsFromEndRequest(TryllJsonWriter w, Tryll.RemoveInteractionsFromEndRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("count");
            w.UInt64(t.Count);
            w.EndObject();
        }

        private static void Write_Tryll_RemoveInteractionsFromEndResponse(TryllJsonWriter w, Tryll.RemoveInteractionsFromEndResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("removed_count");
            w.UInt64(t.RemovedCount);
            w.EndObject();
        }

        private static void Write_Tryll_ResumeAgentRequest(TryllJsonWriter w, Tryll.ResumeAgentRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.ResumeNode != null)
            {
                w.Key("resume_node");
                w.String(t.ResumeNode);
            }
            if (TryllFbNav.Present(bb, pos, 10))
            {
                w.Key("tool_results");
                w.BeginArray();
                for (int i = 0; i < t.ToolResultsLength; i++)
                {
                    var elem = t.ToolResults(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 10);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_ToolResult(w, elem, elemPos, bb, prefix + "tool_results.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_SaveDialogRequest(TryllJsonWriter w, Tryll.SaveDialogRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Path != null)
            {
                w.Key("path");
                w.String(t.Path);
            }
            if (t.SaveId != null)
            {
                w.Key("save_id");
                w.String(t.SaveId);
            }
            w.EndObject();
        }

        private static void Write_Tryll_SaveDialogResponse(TryllJsonWriter w, Tryll.SaveDialogResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("saved_at_ms");
            w.UInt64(t.SavedAtMs);
            w.EndObject();
        }

        private static void Write_Tryll_SaveGameMemoryRequest(TryllJsonWriter w, Tryll.SaveGameMemoryRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Path != null)
            {
                w.Key("path");
                w.String(t.Path);
            }
            if (t.SaveId != null)
            {
                w.Key("save_id");
                w.String(t.SaveId);
            }
            w.EndObject();
        }

        private static void Write_Tryll_SaveGameMemoryResponse(TryllJsonWriter w, Tryll.SaveGameMemoryResponse t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("saved_at_ms");
            w.UInt64(t.SavedAtMs);
            w.Key("store_count");
            w.UInt64(t.StoreCount);
            w.EndObject();
        }

        private static void Write_Tryll_SendMessageRequest(TryllJsonWriter w, Tryll.SendMessageRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.EndObject();
        }

        private static string Label_Tryll_SendMessageRequest(Tryll.SendMessageRequest t)
        {
            var text = (t.Text ?? "");
            return TryllLogLabel.Format("{text}", ("text", text));
        }

        private static void Write_Tryll_SessionModelConfig(TryllJsonWriter w, Tryll.SessionModelConfig t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (TryllFbNav.Present(bb, pos, 4))
            {
                w.Key("engines");
                w.BeginArray();
                for (int i = 0; i < t.EnginesLength; i++)
                {
                    var elem = t.Engines(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 4);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_EngineSelection(w, elem, elemPos, bb, prefix + "engines.", omitted);
                }
                w.EndArray();
            }
            if (TryllFbNav.Present(bb, pos, 6))
            {
                w.Key("placement_hints");
                w.BeginArray();
                for (int i = 0; i < t.PlacementHintsLength; i++)
                {
                    var elem = t.PlacementHints(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 6);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_ModelPlacementHint(w, elem, elemPos, bb, prefix + "placement_hints.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_SetAgentDeliveryModeRequest(TryllJsonWriter w, Tryll.SetAgentDeliveryModeRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Mode != null)
            {
                w.Key("mode");
                w.String(t.Mode);
            }
            w.Key("intensity");
            w.Float(t.Intensity);
            w.EndObject();
        }

        private static void Write_Tryll_SetAgentVoiceRequest(TryllJsonWriter w, Tryll.SetAgentVoiceRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Voice.HasValue)
            {
                w.Key("voice");
                var childPos = TryllFbNav.Child(bb, pos, 8);
                Write_Tryll_VoiceProfile(w, t.Voice.Value, childPos, bb, prefix + "voice.", omitted);
            }
            w.EndObject();
        }

        private static void Write_Tryll_SetInferenceThrottle(TryllJsonWriter w, Tryll.SetInferenceThrottle t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("throttle");
            w.Float(t.Throttle);
            w.EndObject();
        }

        private static void Write_Tryll_SetSessionLocaleRequest(TryllJsonWriter w, Tryll.SetSessionLocaleRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.Locale != null)
            {
                w.Key("locale");
                w.String(t.Locale);
            }
            w.EndObject();
        }

        private static void Write_Tryll_ToolResult(TryllJsonWriter w, Tryll.ToolResult t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.CallId != null)
            {
                w.Key("call_id");
                w.String(t.CallId);
            }
            if (t.Result != null)
            {
                w.Key("result");
                w.String(t.Result);
            }
            w.EndObject();
        }

        private static void Write_Tryll_TtsAudioFormatFrame(TryllJsonWriter w, Tryll.TtsAudioFormatFrame t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.Format.HasValue)
            {
                w.Key("format");
                var childPos = TryllFbNav.Child(bb, pos, 8);
                Write_Tryll_WireAudioFormat(w, t.Format.Value, childPos, bb, prefix + "format.", omitted);
            }
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            w.EndObject();
        }

        private static void Write_Tryll_TtsAudioFrame(TryllJsonWriter w, Tryll.TtsAudioFrame t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            {
                var path = (string.IsNullOrEmpty(prefix) ? "pcm" : prefix + "pcm");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 8) || inMap)
                {
                    w.Key("pcm");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.PcmLength, (long)t.PcmLength * 1L);
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            w.EndObject();
        }

        private static int Pack_Tryll_TtsAudioFrame(FlatBufferBuilder fbb, Tryll.TtsAudioFrame t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _pcm = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 8))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "pcm" : prefix + "pcm");
                omitted.Add(new TryllOmittedField(path, t.PcmLength, (long)t.PcmLength * 1L));
                _pcm = Tryll.TtsAudioFrame.CreatePcmVector(fbb, new byte[0]);
            }
            var _node_name = t.NodeName == null ? default(StringOffset) : fbb.CreateString(t.NodeName);
            return Tryll.TtsAudioFrame.CreateTtsAudioFrame(fbb, t.RequestId, t.AgentId, _pcm, _node_name).Value;
        }

        private static string Label_Tryll_TtsAudioFrame(Tryll.TtsAudioFrame t)
        {
            var node_name = (t.NodeName ?? "");
            return TryllLogLabel.Format("{node_name}", ("node_name", node_name));
        }

        private static void Write_Tryll_TtsCapabilitiesInfo(TryllJsonWriter w, Tryll.TtsCapabilitiesInfo t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("voice_description");
            w.Bool(t.VoiceDescription);
            w.Key("delivery_instruction");
            w.Bool(t.DeliveryInstruction);
            w.Key("reference_voice");
            w.Bool(t.ReferenceVoice);
            w.Key("speed");
            w.Bool(t.Speed);
            w.Key("language_per_call");
            w.Bool(t.LanguagePerCall);
            if (TryllFbNav.Present(bb, pos, 14))
            {
                w.Key("voice_presets");
                w.BeginArray();
                for (int i = 0; i < t.VoicePresetsLength; i++)
                {
                    var elem = t.VoicePresets(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 14);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_TtsVoicePresetInfo(w, elem, elemPos, bb, prefix + "voice_presets.", omitted);
                }
                w.EndArray();
            }
            if (TryllFbNav.Present(bb, pos, 16))
            {
                w.Key("inline_tags");
                w.BeginArray();
                for (int i = 0; i < t.InlineTagsLength; i++)
                {
                    var elem = t.InlineTags(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 16);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_TtsInlineTagInfo(w, elem, elemPos, bb, prefix + "inline_tags.", omitted);
                }
                w.EndArray();
            }
            w.Key("expressiveness");
            w.Bool(t.Expressiveness);
            w.Key("num_speakers");
            w.Int64(t.NumSpeakers);
            w.EndObject();
        }

        private static void Write_Tryll_TtsInlineTagInfo(TryllJsonWriter w, Tryll.TtsInlineTagInfo t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Token != null)
            {
                w.Key("token");
                w.String(t.Token);
            }
            if (t.Kind != null)
            {
                w.Key("kind");
                w.String(t.Kind);
            }
            if (t.Verification != null)
            {
                w.Key("verification");
                w.String(t.Verification);
            }
            w.EndObject();
        }

        private static void Write_Tryll_TtsVisemeFrame(TryllJsonWriter w, Tryll.TtsVisemeFrame t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (t.NodeName != null)
            {
                w.Key("node_name");
                w.String(t.NodeName);
            }
            if (t.Events.HasValue)
            {
                w.Key("events");
                var childPos = TryllFbNav.Child(bb, pos, 10);
                Write_Tryll_VisemeEventList(w, t.Events.Value, childPos, bb, prefix + "events.", omitted);
            }
            w.EndObject();
        }

        private static int Pack_Tryll_TtsVisemeFrame(FlatBufferBuilder fbb, Tryll.TtsVisemeFrame t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _node_name = t.NodeName == null ? default(StringOffset) : fbb.CreateString(t.NodeName);
            var _events = default(Offset<Tryll.VisemeEventList>);
            if (t.Events.HasValue)
                _events = new Offset<Tryll.VisemeEventList>(Pack_Tryll_VisemeEventList(fbb, t.Events.Value, TryllFbNav.Child(bb, pos, 10), bb, prefix + "events.", omitted));
            return Tryll.TtsVisemeFrame.CreateTtsVisemeFrame(fbb, t.RequestId, t.AgentId, _node_name, _events).Value;
        }

        private static string Label_Tryll_TtsVisemeFrame(Tryll.TtsVisemeFrame t)
        {
            var node_name = (t.NodeName ?? "");
            return TryllLogLabel.Format("{node_name}", ("node_name", node_name));
        }

        private static void Write_Tryll_TtsVoicePresetInfo(TryllJsonWriter w, Tryll.TtsVoicePresetInfo t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Id != null)
            {
                w.Key("id");
                w.String(t.Id);
            }
            if (t.Label != null)
            {
                w.Key("label");
                w.String(t.Label);
            }
            if (t.Description != null)
            {
                w.Key("description");
                w.String(t.Description);
            }
            w.EndObject();
        }

        private static void Write_Tryll_TurnComplete(TryllJsonWriter w, Tryll.TurnComplete t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            w.Key("status");
            if (Enum.IsDefined(typeof(Tryll.TurnStatus), t.Status)) w.String(t.Status.ToString());
            else w.Int64(Convert.ToInt64(t.Status));
            if (t.DebugInfo != null)
            {
                w.Key("debug_info");
                if (TryllJsonWriter.TryParseJson(t.DebugInfo, out var parseError)) w.Raw(t.DebugInfo);
                else
                {
                    w.String(t.DebugInfo);
                    w.Key("debug_info__parse_error");
                    w.String(parseError ?? "invalid");
                }
            }
            w.Key("tokens_generated");
            w.UInt64(t.TokensGenerated);
            w.EndObject();
        }

        private static void Write_Tryll_UnloadModelRequest(TryllJsonWriter w, Tryll.UnloadModelRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            if (t.ModelName != null)
            {
                w.Key("model_name");
                w.String(t.ModelName);
            }
            w.EndObject();
        }

        private static void Write_Tryll_UpdateAgentVariablesRequest(TryllJsonWriter w, Tryll.UpdateAgentVariablesRequest t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("request_id");
            w.UInt64(t.RequestId);
            w.Key("agent_id");
            w.UInt64(t.AgentId);
            if (TryllFbNav.Present(bb, pos, 8))
            {
                w.Key("updates");
                w.BeginArray();
                for (int i = 0; i < t.UpdatesLength; i++)
                {
                    var elem = t.Updates(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 8);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_VariableUpdate(w, elem, elemPos, bb, prefix + "updates.", omitted);
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_VarBool(TryllJsonWriter w, Tryll.VarBool t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("v");
            w.Bool(t.V);
            w.EndObject();
        }

        private static void Write_Tryll_VarFloat(TryllJsonWriter w, Tryll.VarFloat t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("v");
            w.Double(t.V);
            w.EndObject();
        }

        private static void Write_Tryll_VarInt(TryllJsonWriter w, Tryll.VarInt t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("v");
            w.Int64(t.V);
            w.EndObject();
        }

        private static void Write_Tryll_VarString(TryllJsonWriter w, Tryll.VarString t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.V != null)
            {
                w.Key("v");
                w.String(t.V);
            }
            w.EndObject();
        }

        private static void Write_Tryll_VarStringSet(TryllJsonWriter w, Tryll.VarStringSet t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (TryllFbNav.Present(bb, pos, 4))
            {
                w.Key("v");
                w.BeginArray();
                for (int i = 0; i < t.VLength; i++)
                {
                    w.String(t.V(i) ?? "");
                }
                w.EndArray();
            }
            w.EndObject();
        }

        private static void Write_Tryll_VariableUpdate(TryllJsonWriter w, Tryll.VariableUpdate t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.Name != null)
            {
                w.Key("name");
                w.String(t.Name);
            }
            w.Key("op");
            if (Enum.IsDefined(typeof(Tryll.VariableOp), t.Op)) w.String(t.Op.ToString());
            else w.Int64(Convert.ToInt64(t.Op));
            if (t.ValueType != Tryll.VariableValue.NONE)
            {
                w.Key("value_type");
                w.String(t.ValueType.ToString());
                w.Key("value");
                switch (t.ValueType)
                {
                    case Tryll.VariableValue.VarInt:
                        Write_Tryll_VarInt(w, t.ValueAsVarInt(), TryllFbNav.Child(bb, pos, 10), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarFloat:
                        Write_Tryll_VarFloat(w, t.ValueAsVarFloat(), TryllFbNav.Child(bb, pos, 10), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarString:
                        Write_Tryll_VarString(w, t.ValueAsVarString(), TryllFbNav.Child(bb, pos, 10), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarBool:
                        Write_Tryll_VarBool(w, t.ValueAsVarBool(), TryllFbNav.Child(bb, pos, 10), bb, prefix + "value.", omitted);
                        break;
                    case Tryll.VariableValue.VarStringSet:
                        Write_Tryll_VarStringSet(w, t.ValueAsVarStringSet(), TryllFbNav.Child(bb, pos, 10), bb, prefix + "value.", omitted);
                        break;
                    default:
                        w.BeginObject(); w.EndObject();
                        break;
                }
            }
            w.EndObject();
        }

        private static void Write_Tryll_VisemeEvent(TryllJsonWriter w, Tryll.VisemeEvent t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("sample_offset");
            w.UInt64(t.SampleOffset);
            w.Key("viseme");
            if (Enum.IsDefined(typeof(Tryll.WireViseme), t.Viseme)) w.String(t.Viseme.ToString());
            else w.Int64(Convert.ToInt64(t.Viseme));
            w.Key("weight");
            w.Float(t.Weight);
            w.EndObject();
        }

        private static void Write_Tryll_VisemeEventList(TryllJsonWriter w, Tryll.VisemeEventList t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            {
                var path = (string.IsNullOrEmpty(prefix) ? "events" : prefix + "events");
                var inMap = omitted.TryGetValue(path, out var sized);
                if (TryllFbNav.Present(bb, pos, 4) || inMap)
                {
                    w.Key("events");
                    if (!inMap)
                        sized = new TryllOmittedField(path, t.EventsLength, (long)t.EventsLength * 4L);
                    w.BeginObject();
                    w.Key("omitted"); w.Bool(true);
                    w.Key("count"); w.Int64(sized.Count);
                    w.Key("bytes"); w.Int64(sized.Bytes);
                    w.EndObject();
                }
            }
            w.EndObject();
        }

        private static int Pack_Tryll_VisemeEventList(FlatBufferBuilder fbb, Tryll.VisemeEventList t, int pos, ByteBuffer bb, string prefix, List<TryllOmittedField> omitted)
        {
            var _events = default(VectorOffset);
            if (TryllFbNav.Present(bb, pos, 4))
            {
                var path = (string.IsNullOrEmpty(prefix) ? "events" : prefix + "events");
                omitted.Add(new TryllOmittedField(path, t.EventsLength, (long)t.EventsLength * 4L));
                _events = Tryll.VisemeEventList.CreateEventsVector(fbb, new Offset<Tryll.VisemeEvent>[0]);
            }
            return Tryll.VisemeEventList.CreateVisemeEventList(fbb, _events).Value;
        }

        private static void Write_Tryll_VoiceProfile(TryllJsonWriter w, Tryll.VoiceProfile t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            if (t.TtsModelName != null)
            {
                w.Key("tts_model_name");
                w.String(t.TtsModelName);
            }
            w.Key("speaker_id");
            w.Int64(t.SpeakerId);
            if (t.ReferenceVoice != null)
            {
                w.Key("reference_voice");
                w.String(t.ReferenceVoice);
            }
            if (t.VoiceDescription != null)
            {
                w.Key("voice_description");
                w.String(t.VoiceDescription);
            }
            w.Key("speed");
            w.Float(t.Speed);
            if (t.Lang != null)
            {
                w.Key("lang");
                w.String(t.Lang);
            }
            w.Key("expressiveness");
            w.Float(t.Expressiveness);
            w.Key("prepare");
            if (Enum.IsDefined(typeof(Tryll.VoicePreparation), t.Prepare)) w.String(t.Prepare.ToString());
            else w.Int64(Convert.ToInt64(t.Prepare));
            w.Key("emit_visemes");
            w.Bool(t.EmitVisemes);
            if (TryllFbNav.Present(bb, pos, 22))
            {
                w.Key("delivery_modes");
                w.BeginArray();
                for (int i = 0; i < t.DeliveryModesLength; i++)
                {
                    var elem = t.DeliveryModes(i).Value;
                    var rel = TryllFbNav.Field(bb, pos, 22);
                    var vec = TryllFbNav.Indirect(bb, pos + rel);
                    var elemPos = TryllFbNav.Indirect(bb, vec + 4 + i * 4);
                    Write_Tryll_DeliveryMode(w, elem, elemPos, bb, prefix + "delivery_modes.", omitted);
                }
                w.EndArray();
            }
            if (t.DefaultDeliveryMode != null)
            {
                w.Key("default_delivery_mode");
                w.String(t.DefaultDeliveryMode);
            }
            w.EndObject();
        }

        private static void Write_Tryll_WireAudioFormat(TryllJsonWriter w, Tryll.WireAudioFormat t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("sample_rate");
            w.UInt64(t.SampleRate);
            w.Key("channels");
            w.Int64(t.Channels);
            w.Key("bits_per_sample");
            w.Int64(t.BitsPerSample);
            w.EndObject();
        }

        private static void Write_Tryll_WireTranscriptUpdate(TryllJsonWriter w, Tryll.WireTranscriptUpdate t, int pos, ByteBuffer bb, string prefix, Dictionary<string, TryllOmittedField> omitted)
        {
            w.BeginObject();
            w.Key("voice_input_id");
            w.UInt64(t.VoiceInputId);
            if (t.Text != null)
            {
                w.Key("text");
                w.String(t.Text);
            }
            w.Key("kind");
            if (Enum.IsDefined(typeof(Tryll.TranscriptUpdateKind), t.Kind)) w.String(t.Kind.ToString());
            else w.Int64(Convert.ToInt64(t.Kind));
            w.Key("audio_ms_consumed");
            w.UInt64(t.AudioMsConsumed);
            w.EndObject();
        }

        private static byte[] Finish(ByteBuffer bb, MessageBody body, Func<FlatBufferBuilder, int, int> pack)
        {
            var messagePos = TryllFbNav.Root(bb);
            var bodyPos = TryllFbNav.Child(bb, messagePos, 6);
            var fbb = new FlatBufferBuilder(256);
            var off = pack(fbb, bodyPos);
            Message.FinishMessageBuffer(fbb, Message.CreateMessage(fbb, body, off));
            return fbb.SizedByteArray();
        }

        private static long StringVectorBytes(int count, Func<int, string> at)
        {
            long total = 0;
            for (int i = 0; i < count; i++)
            {
                var text = at(i);
                if (text != null) total += Encoding.UTF8.GetByteCount(text);
            }
            return total;
        }

        private static Dictionary<string, TryllOmittedField> Index(IReadOnlyList<TryllOmittedField> items)
        {
            var map = new Dictionary<string, TryllOmittedField>();
            if (items == null) return map;
            foreach (var item in items) map[item.Path] = item;
            return map;
        }

        private static readonly Dictionary<string, TryllLogTable> Tables = new Dictionary<string, TryllLogTable>
        {
            {
                "Ack",
                new TryllLogTable
                {
                    Name = "Ack",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "warning",
                            Kind = "string",
                            Doc = "Set when a successful call changed something the caller did not ask for. SetAgentVoice fills this when the active delivery mode is not on the new profile and the server falls back to that profile's default mode. Empty on every other ack.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "AgentVariable",
                new TryllLogTable
                {
                    Name = "AgentVariable",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "value",
                            Kind = "union",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = new string[] { "NONE", "VarInt", "VarFloat", "VarString", "VarBool", "VarStringSet" },
                            Log = "",
                            Hint = "",
                            TypeName = "VariableValue",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "allow_output_substitution",
                            Kind = "scalar",
                            Doc = "When true, this variable may appear as `__NAME__` in Generate / GenerateAndSpeak output and be replaced with its rendered value. Immutable declaration metadata (not overridable per-component). Default false — opt-in. Forbidden for StringSet; enabled strings are capped at 2048 UTF-8 bytes (see AgentVariables validation).",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "AnswerText",
                new TryllLogTable
                {
                    Name = "AnswerText",
                    Label = "{node_name}",
                    Coalesce = "concat",
                    CoalesceField = "text",
                    HasNodeName = true,
                    HasIsDelta = true,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "multiline",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "is_delta",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "is_final",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "Name of the node that produced this text (multi-sender attribution).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "AppendInteractionsRequest",
                new TryllLogTable
                {
                    Name = "AppendInteractionsRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "interactions",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "DialogInteraction",
                            ElementKind = "table",
                            ElementType = "DialogInteraction",
                        },
                    },
                }
            },
            {
                "AppendInteractionsResponse",
                new TryllLogTable
                {
                    Name = "AppendInteractionsResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "appended_count",
                            Kind = "scalar",
                            Doc = "Number of interactions actually appended (after skipping all-empty pairs).",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "AudioBuffer",
                new TryllLogTable
                {
                    Name = "AudioBuffer",
                    Label = "",
                    Coalesce = "sum",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "voice_input_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "pcm",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "scalar",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "BeginUtteranceRequest",
                new TryllLogTable
                {
                    Name = "BeginUtteranceRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice_input_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "auto_finish_on_silence",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "true",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "max_utterance_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "60000",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CancelRequest",
                new TryllLogTable
                {
                    Name = "CancelRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "mode",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "StopAndKeep",
                            EnumNames = new string[] { "StopAndKeep", "StopAndDiscard" },
                            Log = "",
                            Hint = "",
                            TypeName = "CancelMode",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CancelUtteranceRequest",
                new TryllLogTable
                {
                    Name = "CancelUtteranceRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice_input_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ChangeAgentParamRequest",
                new TryllLogTable
                {
                    Name = "ChangeAgentParamRequest",
                    Label = "{node_name} · {params_type}",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = true,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "Identifies the target node by name (addressing data, kept on envelope).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "params",
                            Kind = "union",
                            Doc = "Whole authored typed params for the node.  Must match the node's concrete type.",
                            DefaultText = "null",
                            EnumNames = new string[] { "NONE", "GenerateParams", "RegexGuardrailParams", "CannedResponseParams", "ToolCallParams", "RetrieveParams", "InstructionParams", "ClassifyIntentParams", "ClassifyIntentLLMParams", "IntentToInstructionParams", "GenerateAndSpeakParams", "SpeakParams", "PauseParams", "TransformParams", "BranchParams", "WriteToMemoryParams", "SummarizeParams", "ClassifyAttributesLLMParams" },
                            Log = "",
                            Hint = "",
                            TypeName = "NodeParams",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ConnectionReady",
                new TryllLogTable
                {
                    Name = "ConnectionReady",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "protocol_version",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "codegen_fingerprint",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "exe_path",
                            Kind = "string",
                            Doc = "Canonical path of the server executable (symlinks resolved). Clients use this to verify they connected to the intended build.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "build_config",
                            Kind = "string",
                            Doc = "\"Debug\" | \"Release\" | \"ClientDev\" | \"ClientProd\"",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "version",
                            Kind = "string",
                            Doc = "Tryll::kVersion string (e.g. \"1.2.3\").",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "run_id",
                            Kind = "string",
                            Doc = "UUID for this server process run — correlates logs and telemetry.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "managed",
                            Kind = "scalar",
                            Doc = "True when the server was launched in managed mode (--idle-shutdown-timeout > 0). A managed server self-exits after the grace period with zero sessions.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "idle_timeout_seconds",
                            Kind = "scalar",
                            Doc = "Seconds the server will wait after the last session disconnects before self-exiting.  0 when not managed (unmanaged launch).",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "mode",
                            Kind = "string",
                            Doc = "Deployment mode this process is running: \"tryll-dev\", \"client-dev\", or \"client-prod\". Added in protocol v24, appended so earlier fields keep their positions.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateAgentRequest",
                new TryllLogTable
                {
                    Name = "CreateAgentRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "graph",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "GraphDescription",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_model_name",
                            Kind = "string",
                            Doc = "Fallback model name for nodes that do not specify their own \"model_name\" param. If empty, the session has no default and every Generate node must supply its own.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "enable_diagnostics",
                            Kind = "scalar",
                            Doc = "When true, the server serializes workflow execution data into TurnComplete.debug_info. Intended for QA/eval pipelines. Off by default for zero overhead in production.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "maintain_dialogue_history",
                            Kind = "scalar",
                            Doc = "When false, the agent's dialogue history is discarded after each turn completes (stateless agent: every turn starts with the system prompt + the new turn only). Useful for technical agents — classification, routing, tool-picking — that gain nothing from prior turns and should not pay to store or project them. Default true = accumulate the full history (previous behaviour).",
                            DefaultText = "true",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "variables",
                            Kind = "vector",
                            Doc = "Complete declaration of this agent's variables: every variable it will ever have, with its initial value (the value's union member fixes the type for the agent's lifetime). Post-create UpdateAgentVariablesRequest updates may only target these names (UnknownVariable otherwise).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "AgentVariable",
                            ElementKind = "table",
                            ElementType = "AgentVariable",
                        },
                        new TryllLogField
                        {
                            Name = "kv_cache_initialization",
                            Kind = "enum",
                            Doc = "How language-model KV contexts are materialised for this agent. Default AllocateOnly = previous behaviour (allocate, no explicit prefill).",
                            DefaultText = "AllocateOnly",
                            EnumNames = new string[] { "AllocateOnly", "Prefill", "DeferAllocation" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentKvCacheInitialization",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "workload",
                            Kind = "enum",
                            Doc = "Whether a human ever waits on this agent's turns. Default Interactive = previous behaviour. Set Background for world-logic/evaluator agents so the inference scheduler paces or defers their work first under load.",
                            DefaultText = "Interactive",
                            EnumNames = new string[] { "Interactive", "Background" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentWorkload",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice",
                            Kind = "table",
                            Doc = "The agent's voice (protocol v27). Required when any node in `graph` speaks (SpeakNode, GenerateAndSpeakNode): creation fails with AgentVoiceRequired (3025) without it, and with InvalidVoiceProfile (3023) when the profile does not fit its model. Ignored, and no TTS model is loaded, when no node speaks.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "VoiceProfile",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateAgentResponse",
                new TryllLogTable
                {
                    Name = "CreateAgentResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateEmbeddedStringStorageRequest",
                new TryllLogTable
                {
                    Name = "CreateEmbeddedStringStorageRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "config_path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "strings",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "embedding_model",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateEmbeddedStringStorageResponse",
                new TryllLogTable
                {
                    Name = "CreateEmbeddedStringStorageResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "record_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "embedding_dim",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateSessionRequest",
                new TryllLogTable
                {
                    Name = "CreateSessionRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "game_name",
                            Kind = "string",
                            Doc = "Identifies the integration (e.g. \"test-chat\", \"qa-and-eval\", \"<partner-game>\"). Used for telemetry grouping.  Nullable — omitting it is valid for backward compatibility; the server will default to \"unknown\" when telemetry is enabled.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "locale",
                            Kind = "string",
                            Doc = "Optional session language as a BCP-47 tag (\"de\", \"de-DE\", \"pt-BR\"). One setting the whole voice + text stack derives from, so a game does not have to push a language into every agent and every node: • TTS   — default for each GenerateAndSpeak/Speak node's `tts_lang` (an explicitly authored `tts_lang` still wins). • STT   — language hint for multilingual recognizers (Whisper, Canary); ignored by models whose language is fixed by the weights. • Files — relative storage paths prefer a translation in a `loc/<language>/` folder beside the linked file, falling back to the linked file itself, so per-language knowledge bases, canned lines and prompts need no code. • Text  — exposed to Mustache templates as the `locale`, `language` and `language_code` keys, for authoring a \"reply in X\" directive. (Written without braces here: flatc's own code writer treats a doubled brace in a doc comment as a template key.) Empty = unset: every one of the above keeps its pre-locale behaviour. Rejected with InvalidLocale (2004) when it is not a well-formed tag. Changeable later via SetSessionLocaleRequest (57), which reaches existing agents' voice + template keys but not content already bound to a node.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_config",
                            Kind = "table",
                            Doc = "What this session needs from the model layer: which engines, and where each model should live. Replaces the four `*_engine` fields this table carried through protocol v12 — the catalog already states each model's engine, so a client declares what it needs instead of choosing an implementation per model type. Nested one level rather than held as two vectors directly here; see SessionModelConfig for why that matters.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "SessionModelConfig",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateSessionResponse",
                new TryllLogTable
                {
                    Name = "CreateSessionResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "session_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateStringStorageRequest",
                new TryllLogTable
                {
                    Name = "CreateStringStorageRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "kind",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "List",
                            EnumNames = new string[] { "List", "Map", "Multimap" },
                            Log = "",
                            Hint = "",
                            TypeName = "StringStorageKind",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "strings",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "keys",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "file_path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateStringStorageResponse",
                new TryllLogTable
                {
                    Name = "CreateStringStorageResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateVoiceInputRequest",
                new TryllLogTable
                {
                    Name = "CreateVoiceInputRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input_format",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "WireAudioFormat",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "vad_threshold",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0.5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "vad_min_silence_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "1000",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "vad_speech_pad_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "250",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "hotwords_storage_path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "hotwords_score",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "1.5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CreateVoiceInputResponse",
                new TryllLogTable
                {
                    Name = "CreateVoiceInputResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice_input_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DebugCommandRequest",
                new TryllLogTable
                {
                    Name = "DebugCommandRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "command",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "payload",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DebugCommandResponse",
                new TryllLogTable
                {
                    Name = "DebugCommandResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "result",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DeleteModelRequest",
                new TryllLogTable
                {
                    Name = "DeleteModelRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DeliveryMode",
                new TryllLogTable
                {
                    Name = "DeliveryMode",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "Mode name used by SetAgentDeliveryModeRequest: [a-z0-9_]{1,32}, unique in the profile. \"neutral\" is built in and may not be defined.",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "reference_voice",
                            Kind = "string",
                            Doc = "Clone families (Pocket TTS, Chatterbox, Qwen3-TTS Base): a reference clip of this character speaking in this mood, relative to the storage root. Replaces the profile's reference_voice while the mode is active.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "instruction",
                            Kind = "string",
                            Doc = "Maya1: sustained delivery clause appended to the voice description. A known keyword (angry, sad, whisper, ...) is rewritten into its tuned clause; anything else is appended as written.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "expressiveness",
                            Kind = "scalar",
                            Doc = "Chatterbox: exaggeration for this mode, 0.0-2.0. Negative = use the profile's expressiveness.",
                            DefaultText = "-1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "speed_scale",
                            Kind = "scalar",
                            Doc = "Multiplies the profile speed while the mode is active, 0.5-2.0. Ignored (must stay 1.0) on models without speed control.",
                            DefaultText = "1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DestroyAgentRequest",
                new TryllLogTable
                {
                    Name = "DestroyAgentRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DestroyEmbeddedStringStorageRequest",
                new TryllLogTable
                {
                    Name = "DestroyEmbeddedStringStorageRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DestroyStringStorageRequest",
                new TryllLogTable
                {
                    Name = "DestroyStringStorageRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DestroyVoiceInputRequest",
                new TryllLogTable
                {
                    Name = "DestroyVoiceInputRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice_input_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DialogInteraction",
                new TryllLogTable
                {
                    Name = "DialogInteraction",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "user_message",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "multiline",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "assistant_message",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "multiline",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DownloadComplete",
                new TryllLogTable
                {
                    Name = "DownloadComplete",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "success",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "error",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DownloadModelRequest",
                new TryllLogTable
                {
                    Name = "DownloadModelRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "DownloadProgress",
                new TryllLogTable
                {
                    Name = "DownloadProgress",
                    Label = "{model_name}",
                    Coalesce = "last",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "bytes_downloaded",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "total_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "percent",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EndUtteranceRequest",
                new TryllLogTable
                {
                    Name = "EndUtteranceRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice_input_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EngineSelection",
                new TryllLogTable
                {
                    Name = "EngineSelection",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "role",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Language",
                            EnumNames = new string[] { "Language", "Embedding", "Stt", "Tts", "Vad" },
                            Log = "",
                            Hint = "",
                            TypeName = "ModelRole",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "engine",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Mock",
                            EnumNames = new string[] { "Mock", "LlamaCpp", "OnnxGenAI", "WindowsML", "OpenVino", "TensorRtLlm", "SherpaOnnx", "OnnxRuntime" },
                            Log = "",
                            Hint = "",
                            TypeName = "InferenceEngine",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EnsureMutableKbRequest",
                new TryllLogTable
                {
                    Name = "EnsureMutableKbRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EnsureMutableKbResponse",
                new TryllLogTable
                {
                    Name = "EnsureMutableKbResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "created",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EnumerateMutableKbsRequest",
                new TryllLogTable
                {
                    Name = "EnumerateMutableKbsRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EnumerateMutableKbsResponse",
                new TryllLogTable
                {
                    Name = "EnumerateMutableKbsResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "stores",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbInfo",
                            ElementKind = "table",
                            ElementType = "MutableKbInfo",
                        },
                    },
                }
            },
            {
                "ErrorResponse",
                new TryllLogTable
                {
                    Name = "ErrorResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "code",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "message",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EvictAgentKvCacheRequest",
                new TryllLogTable
                {
                    Name = "EvictAgentKvCacheRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "EvictAgentKvCacheResponse",
                new TryllLogTable
                {
                    Name = "EvictAgentKvCacheResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "applicable",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "true",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "residency",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Evicted",
                            EnumNames = new string[] { "Evicted", "Resident" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentKvCacheResidency",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "prefix_status",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "NotCurrent",
                            EnumNames = new string[] { "NotCurrent", "Current" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentKvCachePrefixStatus",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "eligible_node_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "contexts_evicted",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "debug_info",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ExportDialogRequest",
                new TryllLogTable
                {
                    Name = "ExportDialogRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ExportDialogResponse",
                new TryllLogTable
                {
                    Name = "ExportDialogResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "snapshot",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedDialogSnapshot",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ExportMutableKbRequest",
                new TryllLogTable
                {
                    Name = "ExportMutableKbRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ExportMutableKbResponse",
                new TryllLogTable
                {
                    Name = "ExportMutableKbResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "snapshot",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedMutableKb",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GameMemory",
                new TryllLogTable
                {
                    Name = "GameMemory",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "format_version",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "saved_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "save_id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "stores",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "PersistedMutableKb",
                            ElementKind = "table",
                            ElementType = "PersistedMutableKb",
                        },
                    },
                }
            },
            {
                "GetAgentKvCacheStatusRequest",
                new TryllLogTable
                {
                    Name = "GetAgentKvCacheStatusRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GetAgentKvCacheStatusResponse",
                new TryllLogTable
                {
                    Name = "GetAgentKvCacheStatusResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "applicable",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "true",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "residency",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Evicted",
                            EnumNames = new string[] { "Evicted", "Resident" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentKvCacheResidency",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "prefix_status",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "NotCurrent",
                            EnumNames = new string[] { "NotCurrent", "Current" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentKvCachePrefixStatus",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "eligible_node_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "debug_info",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GetAgentVariablesRequest",
                new TryllLogTable
                {
                    Name = "GetAgentVariablesRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "names",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GetAgentVariablesResponse",
                new TryllLogTable
                {
                    Name = "GetAgentVariablesResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "variables",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "AgentVariable",
                            ElementKind = "table",
                            ElementType = "AgentVariable",
                        },
                    },
                }
            },
            {
                "GetMemoryConsumptionRequest",
                new TryllLogTable
                {
                    Name = "GetMemoryConsumptionRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GetMemoryConsumptionResponse",
                new TryllLogTable
                {
                    Name = "GetMemoryConsumptionResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "snapshot",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MemorySnapshot",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GraphDescription",
                new TryllLogTable
                {
                    Name = "GraphDescription",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "nodes",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "NodeDescription",
                            ElementKind = "table",
                            ElementType = "NodeDescription",
                        },
                        new TryllLogField
                        {
                            Name = "start_node",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ImportDialogRequest",
                new TryllLogTable
                {
                    Name = "ImportDialogRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "snapshot",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedDialogSnapshot",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "compatibility",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Strict",
                            EnumNames = new string[] { "Strict", "AllowGraphMismatch" },
                            Log = "",
                            Hint = "",
                            TypeName = "DialogCompatibility",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ImportDialogResponse",
                new TryllLogTable
                {
                    Name = "ImportDialogResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "interaction_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "warnings",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ImportMutableKbRequest",
                new TryllLogTable
                {
                    Name = "ImportMutableKbRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "snapshot",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedMutableKb",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "compatibility",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Strict",
                            EnumNames = new string[] { "Strict", "AllowSchemaDrift" },
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbCompatibility",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ImportMutableKbResponse",
                new TryllLogTable
                {
                    Name = "ImportMutableKbResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "warnings",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ListModelsRequest",
                new TryllLogTable
                {
                    Name = "ListModelsRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ListModelsResponse",
                new TryllLogTable
                {
                    Name = "ListModelsResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "models",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelInfo",
                            ElementKind = "table",
                            ElementType = "ModelInfo",
                        },
                    },
                }
            },
            {
                "LoadDialogRequest",
                new TryllLogTable
                {
                    Name = "LoadDialogRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "compatibility",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Strict",
                            EnumNames = new string[] { "Strict", "AllowGraphMismatch" },
                            Log = "",
                            Hint = "",
                            TypeName = "DialogCompatibility",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LoadDialogResponse",
                new TryllLogTable
                {
                    Name = "LoadDialogResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "interaction_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "warnings",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LoadGameMemoryRequest",
                new TryllLogTable
                {
                    Name = "LoadGameMemoryRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "compatibility",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Strict",
                            EnumNames = new string[] { "Strict", "AllowSchemaDrift" },
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbCompatibility",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "embed_timing",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Unspecified",
                            EnumNames = new string[] { "Unspecified", "Now", "Lazy" },
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbEmbedTiming",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LoadGameMemoryResponse",
                new TryllLogTable
                {
                    Name = "LoadGameMemoryResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "store_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "warnings",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LoadModelRequest",
                new TryllLogTable
                {
                    Name = "LoadModelRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "placement",
                            Kind = "enum",
                            Doc = "Where to put this model, for this load only — the top of the placement precedence chain, above the session's per-model hint. This is what lets an editor's Placement dropdown work without tearing down a long-lived shared session that other panels are using. A hint: an unsatisfiable value is downgraded, never an error. Read the result from LoadModelResponse. Only consulted when `placement_is_override` is true. Without that flag `Auto` would have to mean both \"put it wherever the host prefers\" and \"use whatever the session declared\", and those are different requests — an editor could then never move a model back to host policy once the session had been created with a Ram or Vram hint for it.",
                            DefaultText = "Auto",
                            EnumNames = new string[] { "Auto", "Ram", "Vram" },
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryPlacement",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "placement_is_override",
                            Kind = "scalar",
                            Doc = "True when `placement` is a deliberate choice for this load. False (the default) means \"no opinion — use the session's per-model hint\", which is what a client that does not care about placement sends.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LoadModelResponse",
                new TryllLogTable
                {
                    Name = "LoadModelResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "placement_resolved",
                            Kind = "enum",
                            Doc = "Where the model actually ended up. **Not** an echo of the request: • `Auto` always resolves to a concrete `Ram` or `Vram` here; • an unsatisfiable `Vram` reports `Ram` (no GPU backend, or a CPU-only Sherpa ONNX Runtime build); • a model already resident under a different placement reports the resident value, because placement is fixed at load and deliberately is not part of the model cache key — first loader wins. Echoing the request instead would give a client a UI that confidently displays a device the model is not on.",
                            DefaultText = "Auto",
                            EnumNames = new string[] { "Auto", "Ram", "Vram" },
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryPlacement",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "backend",
                            Kind = "string",
                            Doc = "Backend that served the load: \"vulkan\" | \"cuda\" | \"rocm\" | \"cpu\" for llama.cpp, or the ONNX Runtime provider for Sherpa. Read-only — a client never chooses this. Which GPU API is available is a host decision: it is process-wide and one-shot for llama.cpp and compile-time for Sherpa. Empty when the load did not resolve a backend.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LogCaptureGap",
                new TryllLogTable
                {
                    Name = "LogCaptureGap",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "reason",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "paused",
                            EnumNames = new string[] { "paused", "epoch_change", "reload_discard" },
                            Log = "",
                            Hint = "",
                            TypeName = "LogGapReason",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LogConnectionState",
                new TryllLogTable
                {
                    Name = "LogConnectionState",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "state",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "connected",
                            EnumNames = new string[] { "connected", "failed", "dropped" },
                            Log = "",
                            Hint = "",
                            TypeName = "LogConnState",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "reason",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LogLocalError",
                new TryllLogTable
                {
                    Name = "LogLocalError",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "code",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "code_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "message",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "origin",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "transport",
                            EnumNames = new string[] { "transport", "unexpected_reply", "client_validation" },
                            Log = "",
                            Hint = "",
                            TypeName = "LogErrorOrigin",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MemoryComponent",
                new TryllLogTable
                {
                    Name = "MemoryComponent",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = true,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "kind",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "owner",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Model",
                            EnumNames = new string[] { "Process", "Device", "Model", "AgentNode", "Storage", "Unattributed" },
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryOwnerKind",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tier",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Measured",
                            EnumNames = new string[] { "Measured", "Derived", "Estimated" },
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryTier",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "estimate_basis",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "ram_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "vram_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "ram_bytes_high",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "device_index",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "-1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "per_device",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryComponentDevice",
                            ElementKind = "table",
                            ElementType = "MemoryComponentDevice",
                        },
                        new TryllLogField
                        {
                            Name = "session_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "used_tokens",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "capacity_tokens",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "resident",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "true",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "occupancy_stale",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "compute_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "consumer_class",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Unspecified",
                            EnumNames = new string[] { "Unspecified", "InteractiveVisible", "InteractiveBuffered", "Background" },
                            Log = "",
                            Hint = "",
                            TypeName = "InferenceConsumerClass",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "last_turn_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MemoryComponentDevice",
                new TryllLogTable
                {
                    Name = "MemoryComponentDevice",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "device_index",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "-1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "compute_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MemoryDevice",
                new TryllLogTable
                {
                    Name = "MemoryDevice",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "index",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "-1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "description",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "kind",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "total_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "free_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "self_model_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "self_context_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "self_compute_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "other_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MemorySnapshot",
                new TryllLogTable
                {
                    Name = "MemorySnapshot",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "captured_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "process_working_set_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "process_private_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "process_available",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "backend_initialized",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "unattributed_ram_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "devices",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryDevice",
                            ElementKind = "table",
                            ElementType = "MemoryDevice",
                        },
                        new TryllLogField
                        {
                            Name = "components",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryComponent",
                            ElementKind = "table",
                            ElementType = "MemoryComponent",
                        },
                    },
                }
            },
            {
                "ModelDependency",
                new TryllLogTable
                {
                    Name = "ModelDependency",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "role",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "status",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Absent",
                            EnumNames = new string[] { "Absent", "Local", "Downloading", "Loaded", "Downloaded", "Incomplete" },
                            Log = "",
                            Hint = "",
                            TypeName = "ModelStatus",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "size_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "folder",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "local_path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "failure_reason",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ModelInfo",
                new TryllLogTable
                {
                    Name = "ModelInfo",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "status",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Absent",
                            EnumNames = new string[] { "Absent", "Local", "Downloading", "Loaded", "Downloaded", "Incomplete" },
                            Log = "",
                            Hint = "",
                            TypeName = "ModelStatus",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "huggingface_repo",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "size_bytes",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_type",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "engine",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "audience",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tool_call_format",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context_size",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "files",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_sampling",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelSampling",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "local_path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tool_call_support",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "languages",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "dependencies",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelDependency",
                            ElementKind = "table",
                            ElementType = "ModelDependency",
                        },
                        new TryllLogField
                        {
                            Name = "tts_capabilities",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "TtsCapabilitiesInfo",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ModelPlacementHint",
                new TryllLogTable
                {
                    Name = "ModelPlacementHint",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "placement",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Auto",
                            EnumNames = new string[] { "Auto", "Ram", "Vram" },
                            Log = "",
                            Hint = "",
                            TypeName = "MemoryPlacement",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "hotwords",
                            Kind = "enum",
                            Doc = "Whether this session wants hotword biasing on this STT model. Overrides the catalog's `stt_hotwords_score`; `Default` follows it.",
                            DefaultText = "Default",
                            EnumNames = new string[] { "Default", "Off", "On" },
                            Log = "",
                            Hint = "",
                            TypeName = "HotwordBias",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ModelSampling",
                new TryllLogTable
                {
                    Name = "ModelSampling",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "temperature",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "top_p",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "top_k",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "min_p",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "max_tokens",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "seed",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "repeat_penalty",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "presence_penalty",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "frequency_penalty",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbClearRequest",
                new TryllLogTable
                {
                    Name = "MutableKbClearRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbClearResponse",
                new TryllLogTable
                {
                    Name = "MutableKbClearResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "cleared_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbDeleteRequest",
                new TryllLogTable
                {
                    Name = "MutableKbDeleteRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "ids",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbDeleteResponse",
                new TryllLogTable
                {
                    Name = "MutableKbDeleteResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "deleted_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbGetRequest",
                new TryllLogTable
                {
                    Name = "MutableKbGetRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbGetResponse",
                new TryllLogTable
                {
                    Name = "MutableKbGetResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "found",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "created_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "updated_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbHit",
                new TryllLogTable
                {
                    Name = "MutableKbHit",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "distance",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbInfo",
                new TryllLogTable
                {
                    Name = "MutableKbInfo",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "live_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "write_generation",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "materialized",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "bytes_estimate",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbListRequest",
                new TryllLogTable
                {
                    Name = "MutableKbListRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "offset",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "limit",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "50",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbListResponse",
                new TryllLogTable
                {
                    Name = "MutableKbListResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "records",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbRecord",
                            ElementKind = "table",
                            ElementType = "MutableKbRecord",
                        },
                        new TryllLogField
                        {
                            Name = "next_offset",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "has_more",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbQueryRequest",
                new TryllLogTable
                {
                    Name = "MutableKbQueryRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "query_text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "top_k",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "4",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "retrieval_mode",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Dense",
                            EnumNames = new string[] { "Dense", "Lexical", "Hybrid" },
                            Log = "",
                            Hint = "",
                            TypeName = "RetrievalMode",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbQueryResponse",
                new TryllLogTable
                {
                    Name = "MutableKbQueryResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "hits",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbHit",
                            ElementKind = "table",
                            ElementType = "MutableKbHit",
                        },
                    },
                }
            },
            {
                "MutableKbRecord",
                new TryllLogTable
                {
                    Name = "MutableKbRecord",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "created_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "updated_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbStatsRequest",
                new TryllLogTable
                {
                    Name = "MutableKbStatsRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbStatsResponse",
                new TryllLogTable
                {
                    Name = "MutableKbStatsResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "live_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tombstoned_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "slot_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "write_generation",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "bytes_estimate",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "materialized",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbUpsert",
                new TryllLogTable
                {
                    Name = "MutableKbUpsert",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "MutableKbWriteRequest",
                new TryllLogTable
                {
                    Name = "MutableKbWriteRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "records",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbUpsert",
                            ElementKind = "table",
                            ElementType = "MutableKbUpsert",
                        },
                    },
                }
            },
            {
                "MutableKbWriteResponse",
                new TryllLogTable
                {
                    Name = "MutableKbWriteResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "results",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "MutableKbWriteResult",
                            ElementKind = "table",
                            ElementType = "MutableKbWriteResult",
                        },
                    },
                }
            },
            {
                "MutableKbWriteResult",
                new TryllLogTable
                {
                    Name = "MutableKbWriteResult",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "inserted",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "accepted",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "error",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "NodeDescription",
                new TryllLogTable
                {
                    Name = "NodeDescription",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "params",
                            Kind = "union",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = new string[] { "NONE", "GenerateParams", "RegexGuardrailParams", "CannedResponseParams", "ToolCallParams", "RetrieveParams", "InstructionParams", "ClassifyIntentParams", "ClassifyIntentLLMParams", "IntentToInstructionParams", "GenerateAndSpeakParams", "SpeakParams", "PauseParams", "TransformParams", "BranchParams", "WriteToMemoryParams", "SummarizeParams", "ClassifyAttributesLLMParams" },
                            Log = "",
                            Hint = "",
                            TypeName = "NodeParams",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "NodeEvent",
                new TryllLogTable
                {
                    Name = "NodeEvent",
                    Label = "{event_type} · {node_name}",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = true,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "event_type",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "kv_pairs",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "NodeParam",
                            ElementKind = "table",
                            ElementType = "NodeParam",
                        },
                    },
                }
            },
            {
                "NodeParam",
                new TryllLogTable
                {
                    Name = "NodeParam",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "key",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "value",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "BranchParams",
                new TryllLogTable
                {
                    Name = "BranchParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "test",
                            Kind = "enum",
                            Doc = "What to check: NonEmpty, IsTrue (bool Variable), Equals, EqualsIgnoreCase, Contains, Regex (against Values), or Expression (Condition).",
                            DefaultText = "NonEmpty",
                            EnumNames = new string[] { "NonEmpty", "IsTrue", "Equals", "EqualsIgnoreCase", "Contains", "Regex", "Expression" },
                            Log = "",
                            Hint = "",
                            TypeName = "BranchTest",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text to test: the player's message or an earlier node's output. Empty = the player's message. Leave empty when Variable is set.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "variable",
                            Kind = "string",
                            Doc = "Agent variable to test instead of Input. Leave empty to test Input.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "values",
                            Kind = "vector",
                            Doc = "Values for Equals, EqualsIgnoreCase, Contains and Regex; the test passes if any one matches.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "condition",
                            Kind = "string",
                            Doc = "JSON condition for the Expression test, over agent variables and {\"slot\":\"<name>\"} values. Unused by other tests.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "notify_client",
                            Kind = "scalar",
                            Doc = "Send the game a branch_taken event with the exit taken.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "then_exit",
                            Kind = "string",
                            Doc = "Node to run when the test passes. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "else_exit",
                            Kind = "string",
                            Doc = "Node to run when the test fails or the value to test is missing. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "CannedResponseParams",
                new TryllLogTable
                {
                    Name = "CannedResponseParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "string_storage",
                            Kind = "string",
                            Doc = "File of responses, one per line. Used instead of Lines. Both empty = the server's default responses.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "selection_strategy",
                            Kind = "enum",
                            Doc = "Which response comes next: Random, First (always the first), or RoundRobin (each in turn).",
                            DefaultText = "RoundRobin",
                            EnumNames = new string[] { "Random", "First", "RoundRobin" },
                            Log = "",
                            Hint = "",
                            TypeName = "CannedResponseSelectionStrategy",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Name later nodes use to read the chosen response (in their Input, or as the slot.<name> tag). Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "send",
                            Kind = "enum",
                            Doc = "How the response reaches the game: Whole (in one piece), Streamed, or None (kept for later nodes only).",
                            DefaultText = "Whole",
                            EnumNames = new string[] { "None", "Whole", "Streamed" },
                            Log = "",
                            Hint = "",
                            TypeName = "SendAnswer",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "history_role",
                            Kind = "enum",
                            Doc = "Whether later turns remember this response as an assistant message. None = forgotten after this turn.",
                            DefaultText = "Assistant",
                            EnumNames = new string[] { "None", "Assistant", "User", "System" },
                            Log = "",
                            Hint = "",
                            TypeName = "HistoryRole",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run after the response. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "lines",
                            Kind = "vector",
                            Doc = "Responses typed here; used when String Storage is empty.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "LocalizableText",
                            ElementKind = "table",
                            ElementType = "LocalizableText",
                        },
                    },
                }
            },
            {
                "ClassifyAttributeChoice",
                new TryllLogTable
                {
                    Name = "ClassifyAttributeChoice",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "Value written to the output when this option wins. The model never sees it. Must be unique within the attribute.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "prompt",
                            Kind = "string",
                            Doc = "Description of the option the model reads, listed as \"A. <prompt>\".",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "triggers",
                            Kind = "scalar",
                            Doc = "Choice only. When this option is the answer, the node takes its Triggered exit. Leave off for Score attributes.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ClassifyAttributeDef",
                new TryllLogTable
                {
                    Name = "ClassifyAttributeDef",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "Attribute name; the answer is written to <Output Name>_<Name>. Must be unique within the node.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "kind",
                            Kind = "enum",
                            Doc = "Choice: picks one option and writes its id. Boolean: decides one Statement, writes true or false. Score: writes a 0–1 level.",
                            DefaultText = "Choice",
                            EnumNames = new string[] { "Choice", "Boolean", "Score" },
                            Log = "",
                            Hint = "",
                            TypeName = "AttributeKind",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "choices",
                            Kind = "vector",
                            Doc = "Choice and Score only: the options, 2–26. For Score, order them from lowest to highest.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ClassifyAttributeChoice",
                            ElementKind = "table",
                            ElementType = "ClassifyAttributeChoice",
                        },
                        new TryllLogField
                        {
                            Name = "statement",
                            Kind = "string",
                            Doc = "Boolean only. The statement judged true or false for the input.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "system_prompt",
                            Kind = "string",
                            Doc = "Instructions for this attribute. Empty = a built-in default. Choice and Score must include the intents_block tag; Boolean must include the statement tag.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "threshold",
                            Kind = "scalar",
                            Doc = "Choice: minimum probability of the winner. Boolean: probability of True needed to answer true.",
                            DefaultText = "0.5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "margin",
                            Kind = "scalar",
                            Doc = "Choice only. Minimum lead of the winner over the runner-up.",
                            DefaultText = "0.15",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "uncertain_choice",
                            Kind = "string",
                            Doc = "Choice only. Answer written when the winner is too weak: a choice id or #uncertain. A choice id fires Triggered if that choice does.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "boolean_trigger",
                            Kind = "enum",
                            Doc = "Boolean only. Which answer fires the node's Triggered exit: Never, True or False.",
                            DefaultText = "Never",
                            EnumNames = new string[] { "Never", "True", "False" },
                            Log = "",
                            Hint = "",
                            TypeName = "BooleanTrigger",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "score_trigger",
                            Kind = "enum",
                            Doc = "Score only. Fire the node's Triggered exit when the score is AtOrAbove or Below Score Threshold, or Never.",
                            DefaultText = "Never",
                            EnumNames = new string[] { "Never", "AtOrAbove", "Below" },
                            Log = "",
                            Hint = "",
                            TypeName = "ScoreTrigger",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "score_threshold",
                            Kind = "scalar",
                            Doc = "Score only. Level (0–1) that Score Trigger compares against.",
                            DefaultText = "0.5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ClassifyAttributesLLMParams",
                new TryllLogTable
                {
                    Name = "ClassifyAttributesLLMParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "Language model that scores the attributes. Empty = the workflow's Default Model Name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context_size",
                            Kind = "scalar",
                            Doc = "Context window for this node, in tokens. 0 = sized automatically from the attribute prompts.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "attributes",
                            Kind = "vector",
                            Doc = "Questions to classify, 1–16, all scored in one pass. Each writes its answer to <Output Name>_<attribute Name>.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ClassifyAttributeDef",
                            ElementKind = "table",
                            ElementType = "ClassifyAttributeDef",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text to classify: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "history_turns",
                            Kind = "scalar",
                            Doc = "How many earlier dialog turns the classifier sees. 0 = the current message only.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "temperature",
                            Kind = "scalar",
                            Doc = "Sharpens (below 1) or flattens (above 1) every attribute's probabilities before its thresholds. 0 = unchanged.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "on_failure",
                            Kind = "enum",
                            Doc = "Exit to take if scoring fails: Default (fail open) or Triggered (fail closed).",
                            DefaultText = "Default",
                            EnumNames = new string[] { "Default", "Triggered" },
                            Log = "",
                            Hint = "",
                            TypeName = "AttributeFailureRoute",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "notify_client",
                            Kind = "enum",
                            Doc = "When to send the game an attributes_classified event: Disabled, OnFound (Triggered exit only), or Always.",
                            DefaultText = "Disabled",
                            EnumNames = new string[] { "Disabled", "OnFound", "Always" },
                            Log = "",
                            Hint = "",
                            TypeName = "NotifyClient",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Prefix of this node's results: each attribute writes <Output Name>_<attribute Name>. Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run when no attribute fires. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "triggered_exit",
                            Kind = "string",
                            Doc = "Node to run when any attribute fires (or scoring fails and On Failure is Triggered). Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context",
                            Kind = "table",
                            Doc = "Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ContextOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "adaptation",
                            Kind = "table",
                            Doc = "LoRA adapters applied to this node's model. Empty = the base model.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelAdaptation",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ClassifyIntentEntry",
                new TryllLogTable
                {
                    Name = "ClassifyIntentEntry",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "Label written to the output when this intent wins. The model never sees it. Must be unique among the intents.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "prompt",
                            Kind = "string",
                            Doc = "Description of the intent the model reads, listed as \"A. <prompt>\", \"B. <prompt>\", … in Intents order.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ClassifyIntentLLMParams",
                new TryllLogTable
                {
                    Name = "ClassifyIntentLLMParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "Language model that scores the intents. Empty = the workflow's Default Model Name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context_size",
                            Kind = "scalar",
                            Doc = "Context window for this node, in tokens; must fit the model's trained maximum. 0 = 2048.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "system_prompt",
                            Kind = "string",
                            Doc = "Instructions for the classifier. Must include the intents_block tag (the lettered intent list); the letters tag (\"A, B, C\") is optional.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "intents",
                            Kind = "vector",
                            Doc = "The intents to choose from, 2–26. Each has an id (the label written to the output) and a description the model reads.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ClassifyIntentEntry",
                            ElementKind = "table",
                            ElementType = "ClassifyIntentEntry",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text to classify: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Name later nodes use to read the result: the winning intent id, or Not Found Intent. Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "history_turns",
                            Kind = "scalar",
                            Doc = "How many earlier dialog turns the classifier sees. 0 = the current message only.",
                            DefaultText = "2",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "threshold",
                            Kind = "scalar",
                            Doc = "Minimum probability (0–1) the winning intent needs; below it = Not Found.",
                            DefaultText = "0.5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "margin",
                            Kind = "scalar",
                            Doc = "Minimum lead (0–1) of the winner over the runner-up; less = Not Found.",
                            DefaultText = "0.15",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "temperature",
                            Kind = "scalar",
                            Doc = "Sharpens (below 1) or flattens (above 1) the probabilities before Threshold and Margin. 0 = unchanged.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "notify_client",
                            Kind = "enum",
                            Doc = "When to send the game an intent_llm_classified event: Disabled, OnFound (Found exit only), or Always.",
                            DefaultText = "Disabled",
                            EnumNames = new string[] { "Disabled", "OnFound", "Always" },
                            Log = "",
                            Hint = "",
                            TypeName = "NotifyClient",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "not_found_intent",
                            Kind = "string",
                            Doc = "Intent id that means \"none of these\": if it wins, take Not Found. Also the output on Not Found. May be #uncertain or empty.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "found_exit",
                            Kind = "string",
                            Doc = "Node to run when an intent wins clearly (passes Threshold and Margin). Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "not_found_exit",
                            Kind = "string",
                            Doc = "Node to run when no intent wins clearly. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context",
                            Kind = "table",
                            Doc = "Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ContextOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "adaptation",
                            Kind = "table",
                            Doc = "LoRA adapters applied to this node's model. Empty = the base model.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelAdaptation",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ClassifyIntentParams",
                new TryllLogTable
                {
                    Name = "ClassifyIntentParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "embedded_string_storage",
                            Kind = "string",
                            Doc = "Embedded file of example phrases, each labelled with its intent.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text to classify: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "metadata_field",
                            Kind = "string",
                            Doc = "Metadata field in that file that holds each example's intent label.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "threshold",
                            Kind = "scalar",
                            Doc = "Max cosine distance of the closest example (0 = identical, 2 = opposite). Farther = Not Found.",
                            DefaultText = "0.25",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "filter",
                            Kind = "string",
                            Doc = "JSON filter on example metadata; only matching examples are compared. Empty = no filter.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "notify_client",
                            Kind = "enum",
                            Doc = "When to send the game an intent_classified event: Disabled, OnFound (Found exit only), or Always.",
                            DefaultText = "Disabled",
                            EnumNames = new string[] { "Disabled", "OnFound", "Always" },
                            Log = "",
                            Hint = "",
                            TypeName = "NotifyClient",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "diagnostic_topk",
                            Kind = "scalar",
                            Doc = "How many closest examples to report in diagnostics. Routing always uses the closest one.",
                            DefaultText = "5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "found_exit",
                            Kind = "string",
                            Doc = "Node to run when the closest example is within Threshold. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "not_found_exit",
                            Kind = "string",
                            Doc = "Node to run when no example is close enough. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ContextOverrides",
                new TryllLogTable
                {
                    Name = "ContextOverrides",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "kv_cache_type",
                            Kind = "enum",
                            Doc = "Number format of the context memory (KV cache). Smaller types save memory at some quality cost. Inherit = the model's setting, else q8_0.",
                            DefaultText = "Inherit",
                            EnumNames = new string[] { "Inherit", "F16", "Q8_0", "Q4_0" },
                            Log = "",
                            Hint = "",
                            TypeName = "KvCacheType",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "offload_kqv",
                            Kind = "scalar",
                            Doc = "Keep the context memory on the GPU. Can only turn it off, not on for a model placed in RAM. Unset = server and model placement decide.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "n_ubatch",
                            Kind = "scalar",
                            Doc = "Tokens processed per GPU step; sizes the working buffers. Unset = the model's setting.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "n_outputs_max",
                            Kind = "scalar",
                            Doc = "Leave unset. Advanced: how many positions return scores per step.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "n_batch",
                            Kind = "scalar",
                            Doc = "Most tokens sent to the model in one call while reading the prompt. Unset = the model's setting.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GenerateAndSpeakParams",
                new TryllLogTable
                {
                    Name = "GenerateAndSpeakParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "Language model this node runs. Empty = the workflow's Default Model Name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context_size",
                            Kind = "scalar",
                            Doc = "Context window for this node, in tokens; must fit the model's trained maximum. 0 = the model's own size, else the server default.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "system_prompt",
                            Kind = "string",
                            Doc = "System message the model reads before the dialog. Empty = none.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text this node answers: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "template",
                            Kind = "string",
                            Doc = "Mustache text added next to the Input (where: see Placement), for example the slot.<name> tag or retrieved knowledge. Empty = none.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "placement",
                            Kind = "enum",
                            Doc = "Where Template goes: before or after the Input, as a system or a user message.",
                            DefaultText = "BeforeUserAsSystem",
                            EnumNames = new string[] { "BeforeUserAsUser", "BeforeUserAsSystem", "AfterUserAsUser", "AfterUserAsSystem" },
                            Log = "",
                            Hint = "",
                            TypeName = "Placement",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Name later nodes use to read this node's answer (in their Input, or as the slot.<name> tag). Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "history_role",
                            Kind = "enum",
                            Doc = "Whether later turns remember this answer as an assistant message. None = forgotten after this turn.",
                            DefaultText = "Assistant",
                            EnumNames = new string[] { "None", "Assistant", "User", "System" },
                            Log = "",
                            Hint = "",
                            TypeName = "HistoryRole",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "sampling",
                            Kind = "table",
                            Doc = "Overrides for the model's sampling settings (temperature, top-p, max tokens…). Unset values keep the model's own.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "SamplingOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "grammar",
                            Kind = "string",
                            Doc = "GBNF grammar the spoken answer must follow; needs a `root` rule. Empty = free text. Use it to limit speech to a fixed set of lines.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "min_sentence_chars",
                            Kind = "scalar",
                            Doc = "Minimum length of a spoken chunk before a sentence end cuts it (CJK characters count double). Lower = speech starts sooner, more TTS calls.",
                            DefaultText = "12",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "first_chunk_max_chars",
                            Kind = "scalar",
                            Doc = "Lets the first chunk end early, at a comma, dash or space once it is this long, so speech starts sooner. 0 = off; try 48.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run after the answer is generated and spoken. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "substitute_agent_variables",
                            Kind = "scalar",
                            Doc = "Replace __NAME__ markers in the answer with the values of agent variables that allow output substitution. Not case-sensitive.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_filter",
                            Kind = "table",
                            Doc = "Removes roleplay artifacts before they are spoken: \"Bob:\" prefixes, *actions*, (asides), [notes]. All off by default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "OutputFilterOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context",
                            Kind = "table",
                            Doc = "Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ContextOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "adaptation",
                            Kind = "table",
                            Doc = "LoRA adapters applied to this node's model. Empty = the base model.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelAdaptation",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "GenerateParams",
                new TryllLogTable
                {
                    Name = "GenerateParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "Language model this node runs. Empty = the workflow's Default Model Name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context_size",
                            Kind = "scalar",
                            Doc = "Context window for this node, in tokens; must fit the model's trained maximum. 0 = the model's own size, else the server default.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "system_prompt",
                            Kind = "string",
                            Doc = "System message the model reads before the dialog. Empty = none.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Name later nodes use to read this node's answer (in their Input, or as the slot.<name> tag). Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text this node answers: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "template",
                            Kind = "string",
                            Doc = "Mustache text added next to the Input (where: see Placement), for example the slot.<name> tag or retrieved knowledge. Empty = none.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "placement",
                            Kind = "enum",
                            Doc = "Where Template goes: before or after the Input, as a system or a user message.",
                            DefaultText = "BeforeUserAsSystem",
                            EnumNames = new string[] { "BeforeUserAsUser", "BeforeUserAsSystem", "AfterUserAsUser", "AfterUserAsSystem" },
                            Log = "",
                            Hint = "",
                            TypeName = "Placement",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "send",
                            Kind = "enum",
                            Doc = "How the answer reaches the game: Streamed (token by token), Whole (once, at the end), or None (kept for later nodes only).",
                            DefaultText = "Streamed",
                            EnumNames = new string[] { "None", "Whole", "Streamed" },
                            Log = "",
                            Hint = "",
                            TypeName = "SendAnswer",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "history_role",
                            Kind = "enum",
                            Doc = "Whether later turns remember this answer as an assistant message. None = forgotten after this turn.",
                            DefaultText = "Assistant",
                            EnumNames = new string[] { "None", "Assistant", "User", "System" },
                            Log = "",
                            Hint = "",
                            TypeName = "HistoryRole",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "sampling",
                            Kind = "table",
                            Doc = "Overrides for the model's sampling settings (temperature, top-p, max tokens…). Unset values keep the model's own.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "SamplingOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "grammar",
                            Kind = "string",
                            Doc = "GBNF grammar the answer must follow; needs a `root` rule. Empty = free text. Use it to force a format, such as JSON or a fixed set of lines.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run after the answer is generated. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "substitute_agent_variables",
                            Kind = "scalar",
                            Doc = "Replace __NAME__ markers in the answer with the values of agent variables that allow output substitution. Not case-sensitive.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_filter",
                            Kind = "table",
                            Doc = "Removes roleplay artifacts from the answer: \"Bob:\" prefixes, *actions*, (asides), [notes]. All off by default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "OutputFilterOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context",
                            Kind = "table",
                            Doc = "Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ContextOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "adaptation",
                            Kind = "table",
                            Doc = "LoRA adapters applied to this node's model. Empty = the base model.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelAdaptation",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "InstructionParams",
                new TryllLogTable
                {
                    Name = "InstructionParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "instruction",
                            Kind = "string",
                            Doc = "Text later nodes can insert into their prompts, as the slot.<Output Name> tag or through the #instructions list.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Name later nodes use to read the Instruction (in their Input, or as the slot.<name> tag). Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run next. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "IntentToInstructionParams",
                new TryllLogTable
                {
                    Name = "IntentToInstructionParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "string_storage",
                            Kind = "string",
                            Doc = "Storage file that maps intent labels to instruction text. Used instead of Inline Keys and Instructions.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "inline_keys",
                            Kind = "vector",
                            Doc = "Intent labels, paired by position with Instructions. Used when String Storage is empty.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Name later nodes use to read the chosen instruction (in their Input, or as the slot.<name> tag). Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run next. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "instructions",
                            Kind = "vector",
                            Doc = "Instruction text for each label in Inline Keys, in the same order.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "LocalizableText",
                            ElementKind = "table",
                            ElementType = "LocalizableText",
                        },
                    },
                }
            },
            {
                "LocalizableText",
                new TryllLogTable
                {
                    Name = "LocalizableText",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "The text. Tick Localized to store a Text ID from the workflow text table.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "LoraAdapter",
                new TryllLogTable
                {
                    Name = "LoraAdapter",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "path",
                            Kind = "string",
                            Doc = "LoRA adapter file (.gguf), relative to the storage root. Must be trained for the same base model as the node's model.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "scale",
                            Kind = "scalar",
                            Doc = "Adapter strength. 1 = as trained, 0 = off.",
                            DefaultText = "1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ModelAdaptation",
                new TryllLogTable
                {
                    Name = "ModelAdaptation",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "loras",
                            Kind = "vector",
                            Doc = "LoRA adapters applied together, up to 8, each file once. Order does not matter. Empty = none.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "LoraAdapter",
                            ElementKind = "table",
                            ElementType = "LoraAdapter",
                        },
                    },
                }
            },
            {
                "OutputFilterOverrides",
                new TryllLogTable
                {
                    Name = "OutputFilterOverrides",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "strip_speaker_prefix",
                            Kind = "scalar",
                            Doc = "Remove a leading speaker tag such as \"Bob: \" from the answer, once per turn.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "max_prefix_bytes",
                            Kind = "scalar",
                            Doc = "Longest speaker tag Strip Speaker Prefix looks for, in bytes.",
                            DefaultText = "16",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "strip_asterisk_spans",
                            Kind = "scalar",
                            Doc = "Remove text between asterisks, such as *smiles*.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "strip_paren_spans",
                            Kind = "scalar",
                            Doc = "Remove text in parentheses, such as (laughs).",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "strip_bracket_spans",
                            Kind = "scalar",
                            Doc = "Remove text in square brackets, such as [aside].",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "max_span_bytes",
                            Kind = "scalar",
                            Doc = "Longest span the strip options can remove, in bytes. Longer spans are kept as they are.",
                            DefaultText = "64",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PauseParams",
                new TryllLogTable
                {
                    Name = "PauseParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run when the game resumes without choosing one. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "pause_timeout_ms",
                            Kind = "scalar",
                            Doc = "How long to wait for the game to resume, in ms. 0 = forever. Set a limit unless the game always resumes.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "timeout_exit",
                            Kind = "string",
                            Doc = "Node to run if the wait times out, such as a fallback line. Empty = end the turn as timed out. Unused when Pause Timeout Ms is 0.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "RegexGuardrailParams",
                new TryllLogTable
                {
                    Name = "RegexGuardrailParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "string_storage",
                            Kind = "string",
                            Doc = "File of regex patterns, one per line. Used instead of Inline Strings. Both empty = the server's default patterns.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "inline_strings",
                            Kind = "vector",
                            Doc = "Regex patterns typed here; used when String Storage is empty.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text checked against the patterns: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "triggered_exit",
                            Kind = "string",
                            Doc = "Node to run when the Input matches a pattern. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "not_triggered_exit",
                            Kind = "string",
                            Doc = "Node to run when no pattern matches. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "RetrieveParams",
                new TryllLogTable
                {
                    Name = "RetrieveParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "embedded_string_storage",
                            Kind = "string",
                            Doc = "Embedded knowledge file to search. Used when Source Kind is From File; leave empty with From Name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text to search with: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "top_k",
                            Kind = "scalar",
                            Doc = "How many matching chunks to return.",
                            DefaultText = "2",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "threshold",
                            Kind = "scalar",
                            Doc = "Max cosine distance for a chunk to count (0 = identical, 2 = opposite). 0 = no limit. Ignored in Lexical mode.",
                            DefaultText = "0.5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "retrieval_mode",
                            Kind = "enum",
                            Doc = "Dense = match by meaning. Lexical = match by keywords (BM25). Hybrid = both, merged.",
                            DefaultText = "Dense",
                            EnumNames = new string[] { "Dense", "Lexical", "Hybrid" },
                            Log = "",
                            Hint = "",
                            TypeName = "RetrievalMode",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "rrf_k",
                            Kind = "scalar",
                            Doc = "Hybrid only: how strongly the merge favours top-ranked hits. Higher = flatter. 60 is the standard value.",
                            DefaultText = "60",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "source",
                            Kind = "string",
                            Doc = "Name templates use for these results: the knowledge_<source> tag. Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "filter",
                            Kind = "string",
                            Doc = "JSON filter on chunk metadata; only matching chunks are searched. Empty = no filter.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "found_exit",
                            Kind = "string",
                            Doc = "Node to run when at least one chunk is found. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "not_found_exit",
                            Kind = "string",
                            Doc = "Node to run when nothing is found. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "storage_name",
                            Kind = "string",
                            Doc = "Memory store to search, by name. Used when Source Kind is From Name. May include agent variables (var.*).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema",
                            Kind = "string",
                            Doc = "Record schema of the store. Required with From Name; with From File, checked against the file.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "source_kind",
                            Kind = "enum",
                            Doc = "From File = a prebuilt knowledge file. From Name = a memory store the game fills during play (Write To Memory).",
                            DefaultText = "FromFile",
                            EnumNames = new string[] { "FromFile", "FromName" },
                            Log = "",
                            Hint = "",
                            TypeName = "KnowledgeSourceKind",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SamplingOverrides",
                new TryllLogTable
                {
                    Name = "SamplingOverrides",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "temperature",
                            Kind = "scalar",
                            Doc = "Randomness of the output: 0 = always the most likely token, 2 = very random. Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "top_p",
                            Kind = "scalar",
                            Doc = "Nucleus sampling: pick only from the most likely tokens that together reach this probability (0–1). Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "top_k",
                            Kind = "scalar",
                            Doc = "Pick only from this many most likely tokens. Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "min_p",
                            Kind = "scalar",
                            Doc = "Drop tokens less likely than this fraction of the top token (0–1). Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "max_tokens",
                            Kind = "scalar",
                            Doc = "Longest answer this node may generate, in tokens. Must be smaller than Context Size. Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "seed",
                            Kind = "scalar",
                            Doc = "Random seed; the same seed and input give the same output. Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "repeat_penalty",
                            Kind = "scalar",
                            Doc = "Penalty for repeating recent tokens; 1 = none, higher = less repetition. Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "presence_penalty",
                            Kind = "scalar",
                            Doc = "Penalty for any token already used, pushing toward new topics. Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "frequency_penalty",
                            Kind = "scalar",
                            Doc = "Penalty that grows with how often a token was already used. Unset = the model's default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SpeakParams",
                new TryllLogTable
                {
                    Name = "SpeakParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text to speak: the player's message or an earlier node's output (for example a Canned Response). Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "min_sentence_chars",
                            Kind = "scalar",
                            Doc = "Minimum length of a spoken chunk before a sentence end cuts it (CJK characters count double). Lower = speech starts sooner, more TTS calls.",
                            DefaultText = "12",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run after the text is spoken. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "first_chunk_max_chars",
                            Kind = "scalar",
                            Doc = "Lets the first chunk end early, at a comma, dash or space once it is this long, so speech starts sooner. 0 = off; try 48.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SummarizeParams",
                new TryllLogTable
                {
                    Name = "SummarizeParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "Language model that writes the summaries. Empty = the workflow's Default Model Name. Prefer a plain instruct model over a roleplay fine-tune.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context_size",
                            Kind = "scalar",
                            Doc = "Context window for this node, in tokens; must fit the model's trained maximum. 0 = 2048.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "system_prompt",
                            Kind = "string",
                            Doc = "Instructions for the summarizer. Empty = the built-in default (third person, named speakers, no speculation).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "template",
                            Kind = "string",
                            Doc = "Request sent to the summarizer. Can use the tags previous_summary, #turns (index, user, assistant) and max_summary_tokens. Empty = built-in.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "trigger_tokens",
                            Kind = "scalar",
                            Doc = "Summarize when unsummarized history reaches this many tokens. Keep it below the point where the reply node trims history. 0 = off.",
                            DefaultText = "1024",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "trigger_turns",
                            Kind = "scalar",
                            Doc = "Summarize every N turns. 0 = off. With both triggers off, the node does nothing.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "keep_recent_turns",
                            Kind = "scalar",
                            Doc = "Latest turns never summarized; they are always replayed word for word. Max 512.",
                            DefaultText = "3",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "max_summary_tokens",
                            Kind = "scalar",
                            Doc = "Longest summary, in tokens. Must leave at least 64 tokens of Context Size free.",
                            DefaultText = "256",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "sampling",
                            Kind = "table",
                            Doc = "Overrides for the model's sampling settings (temperature, top-p, max tokens…). Unset values keep the model's own.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "SamplingOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "notify_client",
                            Kind = "scalar",
                            Doc = "Send the game a summarized event after each summary.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "run_inline",
                            Kind = "scalar",
                            Doc = "Must stay on: summarize inside the turn that triggered it, which adds the summary time to that turn. Background mode is not available yet.",
                            DefaultText = "true",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run next, whether or not a summary was written. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context",
                            Kind = "table",
                            Doc = "Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ContextOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "adaptation",
                            Kind = "table",
                            Doc = "LoRA adapters applied to this node's model. Empty = the base model.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelAdaptation",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ToolCallParams",
                new TryllLogTable
                {
                    Name = "ToolCallParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "Language model this node runs. Empty = the workflow's Default Model Name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context_size",
                            Kind = "scalar",
                            Doc = "Context window for this node, in tokens; must fit the model's trained maximum. 0 = 2048.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "system_prompt",
                            Kind = "string",
                            Doc = "System message the model reads before the dialog. Empty = none.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text the model reads to decide on a tool call: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tool_call_format",
                            Kind = "string",
                            Doc = "Not used. Kept so older workflows still load.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tools",
                            Kind = "vector",
                            Doc = "Tools the model may call, each with a name, description and parameters. The game carries out the call; the server only detects it.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ToolDefinition",
                            ElementKind = "table",
                            ElementType = "ToolDefinition",
                        },
                        new TryllLogField
                        {
                            Name = "mode",
                            Kind = "enum",
                            Doc = "DetectOnly: call a tool or say nothing. CallOrAnswer: call a tool or answer in text. RequireCall: must call a tool.",
                            DefaultText = "CallOrAnswer",
                            EnumNames = new string[] { "DetectOnly", "CallOrAnswer", "RequireCall" },
                            Log = "",
                            Hint = "",
                            TypeName = "ToolCallMode",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "parallel_tool_calls",
                            Kind = "scalar",
                            Doc = "Allow several tool calls in one turn, if the model's chat template supports it.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "sampling",
                            Kind = "table",
                            Doc = "Overrides for the model's sampling settings (temperature, top-p, max tokens…). Unset values keep the model's own.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "SamplingOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tool_called_exit",
                            Kind = "string",
                            Doc = "Node to run when the model calls a tool. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "no_tool_called_exit",
                            Kind = "string",
                            Doc = "Node to run when no tool is called (under CallOrAnswer, after the text answer). Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "disposition",
                            Kind = "enum",
                            Doc = "After a call: whether to tell the game (Notify), wait for it (Pause, AwaitResult), and record an acknowledgement in history (…Acknowledge).",
                            DefaultText = "Acknowledge",
                            EnumNames = new string[] { "RouteOnly", "Acknowledge", "Notify", "NotifyAndAcknowledge", "Pause", "PauseAndAcknowledge", "AwaitResult" },
                            Log = "",
                            Hint = "",
                            TypeName = "ToolCallDisposition",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "acknowledge_text",
                            Kind = "string",
                            Doc = "Tool result recorded in history by the …Acknowledge dispositions. Empty = \"ok\".",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "pause_timeout_ms",
                            Kind = "scalar",
                            Doc = "How long a pausing Disposition waits for the game, in ms. 0 = the server default (wait forever in released builds). On timeout the turn ends.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "context",
                            Kind = "table",
                            Doc = "Advanced memory and batching settings for this node's model (KV cache type, GPU offload, batch sizes). Unset = inherit.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ContextOverrides",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "adaptation",
                            Kind = "table",
                            Doc = "LoRA adapters applied to this node's model. Empty = the base model.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelAdaptation",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ToolDefinition",
                new TryllLogTable
                {
                    Name = "ToolDefinition",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "Tool name the model uses to call it, and the game receives with the call.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "description",
                            Kind = "string",
                            Doc = "What the tool does; the model reads this to decide when to call it.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "parameters",
                            Kind = "vector",
                            Doc = "Values the model must supply when it calls this tool.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ToolParamDefinition",
                            ElementKind = "table",
                            ElementType = "ToolParamDefinition",
                        },
                    },
                }
            },
            {
                "ToolParamDefinition",
                new TryllLogTable
                {
                    Name = "ToolParamDefinition",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "Parameter name the model fills in when it calls the tool.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "type",
                            Kind = "string",
                            Doc = "JSON Schema type of the value, such as \"string\", \"number\" or \"boolean\".",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "description",
                            Kind = "string",
                            Doc = "What the parameter means; the model reads this to fill it in.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TransformParams",
                new TryllLogTable
                {
                    Name = "TransformParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "template",
                            Kind = "string",
                            Doc = "Mustache text this node outputs. Can use the tags user_message, slot.<name>, knowledge_<source> and #instructions.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "output_name",
                            Kind = "string",
                            Doc = "Name later nodes use to read the rendered text (in their Input, or as the slot.<name> tag). Empty = this node's name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "default_exit",
                            Kind = "string",
                            Doc = "Node to run next. Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "WriteToMemoryParams",
                new TryllLogTable
                {
                    Name = "WriteToMemoryParams",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "storage_name",
                            Kind = "string",
                            Doc = "Memory store to write to, by name; it lasts for the session. May include agent variables (var.*).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema",
                            Kind = "string",
                            Doc = "Record schema of the store. Nodes that read the same store must use the same schema.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "input",
                            Kind = "string",
                            Doc = "Text to store: the player's message or an earlier node's output. Empty = the player's message.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "Record id; writing an existing id replaces that record. May include agent variables (var.*). Empty = add a new record each time.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "written_exit",
                            Kind = "string",
                            Doc = "Node to run after the write, or when the text to store is empty (nothing is written). Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "failed_exit",
                            Kind = "string",
                            Doc = "Node to run when the store rejects the write (for example, the store is full). Empty = end the turn.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedComponentEntry",
                new TryllLogTable
                {
                    Name = "PersistedComponentEntry",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "value",
                            Kind = "union",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = new string[] { "NONE", "PersistedSlot", "PersistedKnowledge", "PersistedIntention", "PersistedToolCall", "PersistedToolResult", "PersistedSummary" },
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedComponent",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedDialogSnapshot",
                new TryllLogTable
                {
                    Name = "PersistedDialogSnapshot",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "format_version",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "graph_signature",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "interactions",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "PersistedInteraction",
                            ElementKind = "table",
                            ElementType = "PersistedInteraction",
                        },
                        new TryllLogField
                        {
                            Name = "next_tool_call_seq",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "next_exchange_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "projection_windows",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "PersistedProjectionWindow",
                            ElementKind = "table",
                            ElementType = "PersistedProjectionWindow",
                        },
                        new TryllLogField
                        {
                            Name = "save_id",
                            Kind = "string",
                            Doc = "Client-supplied coherence stamp. Compared with game-memory save_id.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "saved_at_ms",
                            Kind = "scalar",
                            Doc = "Server-generated Unix epoch milliseconds at export/save time.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedIntention",
                new TryllLogTable
                {
                    Name = "PersistedIntention",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "intent",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedInteraction",
                new TryllLogTable
                {
                    Name = "PersistedInteraction",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "interrupted",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "components",
                            Kind = "vector",
                            Doc = "Insertion order is semantic (last-wins slots, tool replay).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedComponentEntry",
                            ElementKind = "table",
                            ElementType = "PersistedComponentEntry",
                        },
                    },
                }
            },
            {
                "PersistedKbRecord",
                new TryllLogTable
                {
                    Name = "PersistedKbRecord",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "created_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "updated_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedKnowledge",
                new TryllLogTable
                {
                    Name = "PersistedKnowledge",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "source",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "chunks",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedKnowledgeChunk",
                            ElementKind = "table",
                            ElementType = "PersistedKnowledgeChunk",
                        },
                    },
                }
            },
            {
                "PersistedKnowledgeChunk",
                new TryllLogTable
                {
                    Name = "PersistedKnowledgeChunk",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "distance",
                            Kind = "scalar",
                            Doc = "Cosine distance. Ignored when distance_is_nan is true.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "distance_is_nan",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "bm25_score",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "rank_dense",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "rank_sparse",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedMutableKb",
                new TryllLogTable
                {
                    Name = "PersistedMutableKb",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "schema_fingerprint",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "embedding_model",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "write_generation",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "next_generated_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "records",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "PersistedKbRecord",
                            ElementKind = "table",
                            ElementType = "PersistedKbRecord",
                        },
                    },
                }
            },
            {
                "PersistedProjectionWindow",
                new TryllLogTable
                {
                    Name = "PersistedProjectionWindow",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = true,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "window_start",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedSlot",
                new TryllLogTable
                {
                    Name = "PersistedSlot",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "kind",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "User",
                            EnumNames = new string[] { "User", "Instruction", "General" },
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedSlotKind",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "history_role",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "None",
                            EnumNames = new string[] { "None", "Assistant", "User", "System" },
                            Log = "",
                            Hint = "",
                            TypeName = "HistoryRole",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "producer",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedStringPair",
                new TryllLogTable
                {
                    Name = "PersistedStringPair",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "key",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "value",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedSummary",
                new TryllLogTable
                {
                    Name = "PersistedSummary",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "covered_through",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "producer",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedToolCall",
                new TryllLogTable
                {
                    Name = "PersistedToolCall",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "tool_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "arguments",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "PersistedStringPair",
                            ElementKind = "table",
                            ElementType = "PersistedStringPair",
                        },
                        new TryllLogField
                        {
                            Name = "arguments_json",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "dispatched",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "disposition",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "RouteOnly",
                            EnumNames = new string[] { "RouteOnly", "Acknowledge", "Notify", "NotifyAndAcknowledge", "Pause", "PauseAndAcknowledge", "AwaitResult" },
                            Log = "",
                            Hint = "",
                            TypeName = "ToolCallDisposition",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "exchange_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PersistedToolResult",
                new TryllLogTable
                {
                    Name = "PersistedToolResult",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "call_id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tool_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "exchange_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PrefillAgentKvCacheRequest",
                new TryllLogTable
                {
                    Name = "PrefillAgentKvCacheRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "PrefillAgentKvCacheResponse",
                new TryllLogTable
                {
                    Name = "PrefillAgentKvCacheResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "applicable",
                            Kind = "scalar",
                            Doc = "False when the graph has no language-model nodes (operation is a no-op).",
                            DefaultText = "true",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "residency",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Evicted",
                            EnumNames = new string[] { "Evicted", "Resident" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentKvCacheResidency",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "prefix_status",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "NotCurrent",
                            EnumNames = new string[] { "NotCurrent", "Current" },
                            Log = "",
                            Hint = "",
                            TypeName = "AgentKvCachePrefixStatus",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "eligible_node_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "contexts_created",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tokens_decoded",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tokens_reused",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "debug_info",
                            Kind = "string",
                            Doc = "JSON lifecycle diagnostics. Non-empty only when enable_diagnostics was set on CreateAgentRequest. Never contains prompts or token text.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "RemoveInteractionsFromEndRequest",
                new TryllLogTable
                {
                    Name = "RemoveInteractionsFromEndRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "RemoveInteractionsFromEndResponse",
                new TryllLogTable
                {
                    Name = "RemoveInteractionsFromEndResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "removed_count",
                            Kind = "scalar",
                            Doc = "Number of interactions actually removed.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ResumeAgentRequest",
                new TryllLogTable
                {
                    Name = "ResumeAgentRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "resume_node",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tool_results",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ToolResult",
                            ElementKind = "table",
                            ElementType = "ToolResult",
                        },
                    },
                }
            },
            {
                "SaveDialogRequest",
                new TryllLogTable
                {
                    Name = "SaveDialogRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "save_id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SaveDialogResponse",
                new TryllLogTable
                {
                    Name = "SaveDialogResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "saved_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SaveGameMemoryRequest",
                new TryllLogTable
                {
                    Name = "SaveGameMemoryRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "path",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "save_id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SaveGameMemoryResponse",
                new TryllLogTable
                {
                    Name = "SaveGameMemoryResponse",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "saved_at_ms",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "store_count",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SendMessageRequest",
                new TryllLogTable
                {
                    Name = "SendMessageRequest",
                    Label = "{text}",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "multiline",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SessionModelConfig",
                new TryllLogTable
                {
                    Name = "SessionModelConfig",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "engines",
                            Kind = "vector",
                            Doc = "ADVISORY. Engines this session expects to touch, one entry per (role, engine) pair. May be empty, absent, incomplete, or wrong: none of that prevents a session or a load, and nothing is inferred from an empty list — in particular it is never read as Mock. Its only job is to let the server initialise those engines' backends at CreateSession instead of at first use. A model's engine always comes from the server's own catalog entry, so loading a model this list never named is normal — a developer adding a model to a live session is the motivating case. Unity and Unreal derive it from the Model Manager's manifest or omit it, and expose no engine control. C++ and Python may state it explicitly.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "EngineSelection",
                            ElementKind = "table",
                            ElementType = "EngineSelection",
                        },
                        new TryllLogField
                        {
                            Name = "placement_hints",
                            Kind = "vector",
                            Doc = "Sparse per-model hints. A model absent from this list means MemoryPlacement.Auto and HotwordBias.Default.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "ModelPlacementHint",
                            ElementKind = "table",
                            ElementType = "ModelPlacementHint",
                        },
                    },
                }
            },
            {
                "SetAgentDeliveryModeRequest",
                new TryllLogTable
                {
                    Name = "SetAgentDeliveryModeRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "mode",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "intensity",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SetAgentVoiceRequest",
                new TryllLogTable
                {
                    Name = "SetAgentVoiceRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "VoiceProfile",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SetInferenceThrottle",
                new TryllLogTable
                {
                    Name = "SetInferenceThrottle",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "throttle",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "SetSessionLocaleRequest",
                new TryllLogTable
                {
                    Name = "SetSessionLocaleRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "locale",
                            Kind = "string",
                            Doc = "BCP-47 tag (\"de\", \"pt-BR\"), or empty to clear the session locale.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "ToolResult",
                new TryllLogTable
                {
                    Name = "ToolResult",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "call_id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "result",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TtsAudioFormatFrame",
                new TryllLogTable
                {
                    Name = "TtsAudioFormatFrame",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = true,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "format",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "WireAudioFormat",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "Name of the first speaking node of the turn. Per-chunk attribution is TtsAudioFrame.node_name.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TtsAudioFrame",
                new TryllLogTable
                {
                    Name = "TtsAudioFrame",
                    Label = "{node_name}",
                    Coalesce = "sum",
                    CoalesceField = "",
                    HasNodeName = true,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "pcm",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "scalar",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "Name of the node that produced this audio chunk (multi-sender attribution).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TtsCapabilitiesInfo",
                new TryllLogTable
                {
                    Name = "TtsCapabilitiesInfo",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "voice_description",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "delivery_instruction",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "reference_voice",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "speed",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "language_per_call",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice_presets",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "TtsVoicePresetInfo",
                            ElementKind = "table",
                            ElementType = "TtsVoicePresetInfo",
                        },
                        new TryllLogField
                        {
                            Name = "inline_tags",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "TtsInlineTagInfo",
                            ElementKind = "table",
                            ElementType = "TtsInlineTagInfo",
                        },
                        new TryllLogField
                        {
                            Name = "expressiveness",
                            Kind = "scalar",
                            Doc = "Chatterbox-style exaggeration knob (VoiceProfile.expressiveness).",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "num_speakers",
                            Kind = "scalar",
                            Doc = "Number of speaker ids (VoiceProfile.speaker_id is 0..num_speakers-1). 0 = not advertised by the catalog.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TtsInlineTagInfo",
                new TryllLogTable
                {
                    Name = "TtsInlineTagInfo",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "token",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "kind",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "verification",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TtsVisemeFrame",
                new TryllLogTable
                {
                    Name = "TtsVisemeFrame",
                    Label = "{node_name}",
                    Coalesce = "sum",
                    CoalesceField = "",
                    HasNodeName = true,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "node_name",
                            Kind = "string",
                            Doc = "Name of the node that produced this viseme track (multi-sender attribution — mirrors TtsAudioFrame.node_name).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "events",
                            Kind = "table",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "VisemeEventList",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TtsVoicePresetInfo",
                new TryllLogTable
                {
                    Name = "TtsVoicePresetInfo",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "id",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "label",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "description",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "TurnComplete",
                new TryllLogTable
                {
                    Name = "TurnComplete",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "status",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Success",
                            EnumNames = new string[] { "Success", "Error", "Cancelled", "PauseTimedOut" },
                            Log = "",
                            Hint = "",
                            TypeName = "TurnStatus",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "debug_info",
                            Kind = "string",
                            Doc = "JSON-encoded workflow execution trace. Populated only when enable_diagnostics was set on the CreateAgentRequest. Empty string otherwise.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "json",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "tokens_generated",
                            Kind = "scalar",
                            Doc = "Total tokens sampled across all generation nodes in this turn (prompt tokens excluded).",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "UnloadModelRequest",
                new TryllLogTable
                {
                    Name = "UnloadModelRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "model_name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "UpdateAgentVariablesRequest",
                new TryllLogTable
                {
                    Name = "UpdateAgentVariablesRequest",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "request_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "agent_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "hidden",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "updates",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "VariableUpdate",
                            ElementKind = "table",
                            ElementType = "VariableUpdate",
                        },
                    },
                }
            },
            {
                "VarBool",
                new TryllLogTable
                {
                    Name = "VarBool",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "v",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "VarFloat",
                new TryllLogTable
                {
                    Name = "VarFloat",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "v",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "VarInt",
                new TryllLogTable
                {
                    Name = "VarInt",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "v",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "VarString",
                new TryllLogTable
                {
                    Name = "VarString",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "v",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "VarStringSet",
                new TryllLogTable
                {
                    Name = "VarStringSet",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "v",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "string",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "VariableUpdate",
                new TryllLogTable
                {
                    Name = "VariableUpdate",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "name",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "op",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "Assign",
                            EnumNames = new string[] { "Assign", "AddToSet", "RemoveFromSet", "Reset" },
                            Log = "",
                            Hint = "",
                            TypeName = "VariableOp",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "value",
                            Kind = "union",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = new string[] { "NONE", "VarInt", "VarFloat", "VarString", "VarBool", "VarStringSet" },
                            Log = "",
                            Hint = "",
                            TypeName = "VariableValue",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "VisemeEvent",
                new TryllLogTable
                {
                    Name = "VisemeEvent",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "sample_offset",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "viseme",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "sil",
                            EnumNames = new string[] { "sil", "PP", "FF", "TH", "DD", "kk", "CH", "SS", "nn", "RR", "aa", "E", "ih", "oh", "ou" },
                            Log = "",
                            Hint = "",
                            TypeName = "WireViseme",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "weight",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "VisemeEventList",
                new TryllLogTable
                {
                    Name = "VisemeEventList",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "events",
                            Kind = "vector",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "size",
                            Hint = "",
                            TypeName = "VisemeEvent",
                            ElementKind = "table",
                            ElementType = "VisemeEvent",
                        },
                    },
                }
            },
            {
                "VoiceProfile",
                new TryllLogTable
                {
                    Name = "VoiceProfile",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "tts_model_name",
                            Kind = "string",
                            Doc = "TTS model catalog name. Required: there is no default model. An empty string is still present on the wire and is rejected in code (3023).",
                            DefaultText = "",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "speaker_id",
                            Kind = "scalar",
                            Doc = "Speaker / style index for multi-speaker models (Supertonic 3: 0-9, Orpheus and Maya1: preset index). Must be below the model's speaker count.",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "reference_voice",
                            Kind = "string",
                            Doc = "Reference clip for clone families (Pocket TTS, Chatterbox, Qwen3-TTS Base), relative to the storage root; locale-aware (voices/loc/<locale>/). Empty = the model's default voice (Qwen3-TTS Base requires a clip).",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "voice_description",
                            Kind = "string",
                            Doc = "Maya1: natural-language identity of the voice. Overrides speaker_id.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "speed",
                            Kind = "scalar",
                            Doc = "Speech-rate multiplier. 1.0 = native. Must stay 1.0 on models without speed control (Orpheus, Maya1, Qwen3-TTS).",
                            DefaultText = "1",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "lang",
                            Kind = "string",
                            Doc = "Language for multilingual models that pick it per call. Empty = session locale, then the model default. Rejected on models with a fixed language.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "expressiveness",
                            Kind = "scalar",
                            Doc = "Chatterbox exaggeration used by \"neutral\" and by modes that do not set their own, 0.0-2.0. Ignored by other families.",
                            DefaultText = "0.5",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "prepare",
                            Kind = "enum",
                            Doc = "When reference clips are prepared. Default Eager.",
                            DefaultText = "Eager",
                            EnumNames = new string[] { "Eager", "Lazy" },
                            Log = "",
                            Hint = "",
                            TypeName = "VoicePreparation",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "emit_visemes",
                            Kind = "scalar",
                            Doc = "Emit a viseme track (TtsVisemeFrame) for every sentence this agent speaks, from any speaking node. Turn on for lip-synced characters.",
                            DefaultText = "false",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "delivery_modes",
                            Kind = "vector",
                            Doc = "Named delivery modes. Game code switches between them with SetAgentDeliveryModeRequest.",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "DeliveryMode",
                            ElementKind = "table",
                            ElementType = "DeliveryMode",
                        },
                        new TryllLogField
                        {
                            Name = "default_delivery_mode",
                            Kind = "string",
                            Doc = "Mode active after the voice is set. Empty = \"neutral\".",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "WireAudioFormat",
                new TryllLogTable
                {
                    Name = "WireAudioFormat",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "sample_rate",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "channels",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "bits_per_sample",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
            {
                "WireTranscriptUpdate",
                new TryllLogTable
                {
                    Name = "WireTranscriptUpdate",
                    Label = "",
                    Coalesce = "",
                    CoalesceField = "",
                    HasNodeName = false,
                    HasIsDelta = false,
                    Fields = new TryllLogField[]
                    {
                        new TryllLogField
                        {
                            Name = "voice_input_id",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "text",
                            Kind = "string",
                            Doc = "",
                            DefaultText = "null",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "kind",
                            Kind = "enum",
                            Doc = "",
                            DefaultText = "SpeechStart",
                            EnumNames = new string[] { "SpeechStart", "Partial", "SegmentFinal", "UtteranceFinal" },
                            Log = "",
                            Hint = "",
                            TypeName = "TranscriptUpdateKind",
                            ElementKind = "",
                            ElementType = "",
                        },
                        new TryllLogField
                        {
                            Name = "audio_ms_consumed",
                            Kind = "scalar",
                            Doc = "",
                            DefaultText = "0",
                            EnumNames = null,
                            Log = "",
                            Hint = "",
                            TypeName = "",
                            ElementKind = "",
                            ElementType = "",
                        },
                    },
                }
            },
        };
    }
}

