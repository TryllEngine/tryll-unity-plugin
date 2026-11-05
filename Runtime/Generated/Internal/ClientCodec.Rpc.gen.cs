// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/messages.fbs  +  tools/codegen/templates/protocol_rpc_csharp.cs.j2
// See: docs/dev/codegen.md

using Google.FlatBuffers;
using Tryll;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_ClientCodec_Rpc_gen {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client.Internal
{
    internal static partial class ClientCodec
    {
        /// <summary>
        /// The <c>request_id</c> on <paramref name="msg"/>, or 0 when the body
        /// does not carry one (notifications and fire-and-forget).
        /// </summary>
        public static ulong RequestIdOf(Message msg)
        {
            switch (msg.BodyType)
            {
            case MessageBody.CreateSessionRequest:
                return msg.BodyAsCreateSessionRequest().RequestId;
            case MessageBody.CreateSessionResponse:
                return msg.BodyAsCreateSessionResponse().RequestId;
            case MessageBody.CreateAgentRequest:
                return msg.BodyAsCreateAgentRequest().RequestId;
            case MessageBody.CreateAgentResponse:
                return msg.BodyAsCreateAgentResponse().RequestId;
            case MessageBody.SendMessageRequest:
                return msg.BodyAsSendMessageRequest().RequestId;
            case MessageBody.DestroyAgentRequest:
                return msg.BodyAsDestroyAgentRequest().RequestId;
            case MessageBody.AnswerText:
                return msg.BodyAsAnswerText().RequestId;
            case MessageBody.TurnComplete:
                return msg.BodyAsTurnComplete().RequestId;
            case MessageBody.ErrorResponse:
                return msg.BodyAsErrorResponse().RequestId;
            case MessageBody.Ack:
                return msg.BodyAsAck().RequestId;
            case MessageBody.ListModelsRequest:
                return msg.BodyAsListModelsRequest().RequestId;
            case MessageBody.ListModelsResponse:
                return msg.BodyAsListModelsResponse().RequestId;
            case MessageBody.DownloadModelRequest:
                return msg.BodyAsDownloadModelRequest().RequestId;
            case MessageBody.DownloadProgress:
                return msg.BodyAsDownloadProgress().RequestId;
            case MessageBody.DownloadComplete:
                return msg.BodyAsDownloadComplete().RequestId;
            case MessageBody.CreateStringStorageRequest:
                return msg.BodyAsCreateStringStorageRequest().RequestId;
            case MessageBody.CreateStringStorageResponse:
                return msg.BodyAsCreateStringStorageResponse().RequestId;
            case MessageBody.DestroyStringStorageRequest:
                return msg.BodyAsDestroyStringStorageRequest().RequestId;
            case MessageBody.LoadModelRequest:
                return msg.BodyAsLoadModelRequest().RequestId;
            case MessageBody.LoadModelResponse:
                return msg.BodyAsLoadModelResponse().RequestId;
            case MessageBody.UnloadModelRequest:
                return msg.BodyAsUnloadModelRequest().RequestId;
            case MessageBody.CreateEmbeddedStringStorageRequest:
                return msg.BodyAsCreateEmbeddedStringStorageRequest().RequestId;
            case MessageBody.CreateEmbeddedStringStorageResponse:
                return msg.BodyAsCreateEmbeddedStringStorageResponse().RequestId;
            case MessageBody.DestroyEmbeddedStringStorageRequest:
                return msg.BodyAsDestroyEmbeddedStringStorageRequest().RequestId;
            case MessageBody.ChangeAgentParamRequest:
                return msg.BodyAsChangeAgentParamRequest().RequestId;
            case MessageBody.CreateVoiceInputRequest:
                return msg.BodyAsCreateVoiceInputRequest().RequestId;
            case MessageBody.CreateVoiceInputResponse:
                return msg.BodyAsCreateVoiceInputResponse().RequestId;
            case MessageBody.BeginUtteranceRequest:
                return msg.BodyAsBeginUtteranceRequest().RequestId;
            case MessageBody.EndUtteranceRequest:
                return msg.BodyAsEndUtteranceRequest().RequestId;
            case MessageBody.CancelUtteranceRequest:
                return msg.BodyAsCancelUtteranceRequest().RequestId;
            case MessageBody.DestroyVoiceInputRequest:
                return msg.BodyAsDestroyVoiceInputRequest().RequestId;
            case MessageBody.TtsAudioFormatFrame:
                return msg.BodyAsTtsAudioFormatFrame().RequestId;
            case MessageBody.TtsAudioFrame:
                return msg.BodyAsTtsAudioFrame().RequestId;
            case MessageBody.DeleteModelRequest:
                return msg.BodyAsDeleteModelRequest().RequestId;
            case MessageBody.DebugCommandRequest:
                return msg.BodyAsDebugCommandRequest().RequestId;
            case MessageBody.DebugCommandResponse:
                return msg.BodyAsDebugCommandResponse().RequestId;
            case MessageBody.CancelRequest:
                return msg.BodyAsCancelRequest().RequestId;
            case MessageBody.ResumeAgentRequest:
                return msg.BodyAsResumeAgentRequest().RequestId;
            case MessageBody.UpdateAgentVariablesRequest:
                return msg.BodyAsUpdateAgentVariablesRequest().RequestId;
            case MessageBody.GetAgentVariablesRequest:
                return msg.BodyAsGetAgentVariablesRequest().RequestId;
            case MessageBody.GetAgentVariablesResponse:
                return msg.BodyAsGetAgentVariablesResponse().RequestId;
            case MessageBody.PrefillAgentKvCacheRequest:
                return msg.BodyAsPrefillAgentKvCacheRequest().RequestId;
            case MessageBody.PrefillAgentKvCacheResponse:
                return msg.BodyAsPrefillAgentKvCacheResponse().RequestId;
            case MessageBody.EvictAgentKvCacheRequest:
                return msg.BodyAsEvictAgentKvCacheRequest().RequestId;
            case MessageBody.EvictAgentKvCacheResponse:
                return msg.BodyAsEvictAgentKvCacheResponse().RequestId;
            case MessageBody.GetAgentKvCacheStatusRequest:
                return msg.BodyAsGetAgentKvCacheStatusRequest().RequestId;
            case MessageBody.GetAgentKvCacheStatusResponse:
                return msg.BodyAsGetAgentKvCacheStatusResponse().RequestId;
            case MessageBody.AppendInteractionsRequest:
                return msg.BodyAsAppendInteractionsRequest().RequestId;
            case MessageBody.AppendInteractionsResponse:
                return msg.BodyAsAppendInteractionsResponse().RequestId;
            case MessageBody.RemoveInteractionsFromEndRequest:
                return msg.BodyAsRemoveInteractionsFromEndRequest().RequestId;
            case MessageBody.RemoveInteractionsFromEndResponse:
                return msg.BodyAsRemoveInteractionsFromEndResponse().RequestId;
            case MessageBody.SetSessionLocaleRequest:
                return msg.BodyAsSetSessionLocaleRequest().RequestId;
            case MessageBody.ExportDialogRequest:
                return msg.BodyAsExportDialogRequest().RequestId;
            case MessageBody.ExportDialogResponse:
                return msg.BodyAsExportDialogResponse().RequestId;
            case MessageBody.ImportDialogRequest:
                return msg.BodyAsImportDialogRequest().RequestId;
            case MessageBody.ImportDialogResponse:
                return msg.BodyAsImportDialogResponse().RequestId;
            case MessageBody.GetMemoryConsumptionRequest:
                return msg.BodyAsGetMemoryConsumptionRequest().RequestId;
            case MessageBody.GetMemoryConsumptionResponse:
                return msg.BodyAsGetMemoryConsumptionResponse().RequestId;
            case MessageBody.EnsureMutableKbRequest:
                return msg.BodyAsEnsureMutableKbRequest().RequestId;
            case MessageBody.EnsureMutableKbResponse:
                return msg.BodyAsEnsureMutableKbResponse().RequestId;
            case MessageBody.EnumerateMutableKbsRequest:
                return msg.BodyAsEnumerateMutableKbsRequest().RequestId;
            case MessageBody.EnumerateMutableKbsResponse:
                return msg.BodyAsEnumerateMutableKbsResponse().RequestId;
            case MessageBody.MutableKbWriteRequest:
                return msg.BodyAsMutableKbWriteRequest().RequestId;
            case MessageBody.MutableKbWriteResponse:
                return msg.BodyAsMutableKbWriteResponse().RequestId;
            case MessageBody.MutableKbDeleteRequest:
                return msg.BodyAsMutableKbDeleteRequest().RequestId;
            case MessageBody.MutableKbDeleteResponse:
                return msg.BodyAsMutableKbDeleteResponse().RequestId;
            case MessageBody.MutableKbClearRequest:
                return msg.BodyAsMutableKbClearRequest().RequestId;
            case MessageBody.MutableKbClearResponse:
                return msg.BodyAsMutableKbClearResponse().RequestId;
            case MessageBody.MutableKbQueryRequest:
                return msg.BodyAsMutableKbQueryRequest().RequestId;
            case MessageBody.MutableKbQueryResponse:
                return msg.BodyAsMutableKbQueryResponse().RequestId;
            case MessageBody.MutableKbGetRequest:
                return msg.BodyAsMutableKbGetRequest().RequestId;
            case MessageBody.MutableKbGetResponse:
                return msg.BodyAsMutableKbGetResponse().RequestId;
            case MessageBody.MutableKbListRequest:
                return msg.BodyAsMutableKbListRequest().RequestId;
            case MessageBody.MutableKbListResponse:
                return msg.BodyAsMutableKbListResponse().RequestId;
            case MessageBody.MutableKbStatsRequest:
                return msg.BodyAsMutableKbStatsRequest().RequestId;
            case MessageBody.MutableKbStatsResponse:
                return msg.BodyAsMutableKbStatsResponse().RequestId;
            case MessageBody.ExportMutableKbRequest:
                return msg.BodyAsExportMutableKbRequest().RequestId;
            case MessageBody.ExportMutableKbResponse:
                return msg.BodyAsExportMutableKbResponse().RequestId;
            case MessageBody.ImportMutableKbRequest:
                return msg.BodyAsImportMutableKbRequest().RequestId;
            case MessageBody.ImportMutableKbResponse:
                return msg.BodyAsImportMutableKbResponse().RequestId;
            case MessageBody.SaveGameMemoryRequest:
                return msg.BodyAsSaveGameMemoryRequest().RequestId;
            case MessageBody.SaveGameMemoryResponse:
                return msg.BodyAsSaveGameMemoryResponse().RequestId;
            case MessageBody.LoadGameMemoryRequest:
                return msg.BodyAsLoadGameMemoryRequest().RequestId;
            case MessageBody.LoadGameMemoryResponse:
                return msg.BodyAsLoadGameMemoryResponse().RequestId;
            case MessageBody.SaveDialogRequest:
                return msg.BodyAsSaveDialogRequest().RequestId;
            case MessageBody.SaveDialogResponse:
                return msg.BodyAsSaveDialogResponse().RequestId;
            case MessageBody.LoadDialogRequest:
                return msg.BodyAsLoadDialogRequest().RequestId;
            case MessageBody.LoadDialogResponse:
                return msg.BodyAsLoadDialogResponse().RequestId;
            case MessageBody.TtsVisemeFrame:
                return msg.BodyAsTtsVisemeFrame().RequestId;
            case MessageBody.SetAgentVoiceRequest:
                return msg.BodyAsSetAgentVoiceRequest().RequestId;
            case MessageBody.SetAgentDeliveryModeRequest:
                return msg.BodyAsSetAgentDeliveryModeRequest().RequestId;
            default:
                return 0;
            }
        }

        /// <summary>True for client-to-server messages that expect no reply.</summary>
        public static bool IsFireAndForget(MessageBody body)
        {
            switch (body)
            {
            case MessageBody.AudioBuffer:
            case MessageBody.SetInferenceThrottle:
                return true;
            default:
                return false;
            }
        }

        /// <summary>Declared terminal reply, or <see cref="MessageBody.NONE"/>.</summary>
        public static MessageBody RpcTerminalOf(MessageBody request)
        {
            switch (request)
            {
            case MessageBody.CreateSessionRequest:
                return MessageBody.CreateSessionResponse;
            case MessageBody.CreateAgentRequest:
                return MessageBody.CreateAgentResponse;
            case MessageBody.SendMessageRequest:
                return MessageBody.TurnComplete;
            case MessageBody.DestroyAgentRequest:
                return MessageBody.Ack;
            case MessageBody.ListModelsRequest:
                return MessageBody.ListModelsResponse;
            case MessageBody.DownloadModelRequest:
                return MessageBody.DownloadComplete;
            case MessageBody.CreateStringStorageRequest:
                return MessageBody.CreateStringStorageResponse;
            case MessageBody.DestroyStringStorageRequest:
                return MessageBody.Ack;
            case MessageBody.LoadModelRequest:
                return MessageBody.LoadModelResponse;
            case MessageBody.UnloadModelRequest:
                return MessageBody.Ack;
            case MessageBody.CreateEmbeddedStringStorageRequest:
                return MessageBody.CreateEmbeddedStringStorageResponse;
            case MessageBody.DestroyEmbeddedStringStorageRequest:
                return MessageBody.Ack;
            case MessageBody.ChangeAgentParamRequest:
                return MessageBody.Ack;
            case MessageBody.CreateVoiceInputRequest:
                return MessageBody.CreateVoiceInputResponse;
            case MessageBody.BeginUtteranceRequest:
                return MessageBody.Ack;
            case MessageBody.EndUtteranceRequest:
                return MessageBody.Ack;
            case MessageBody.CancelUtteranceRequest:
                return MessageBody.Ack;
            case MessageBody.DestroyVoiceInputRequest:
                return MessageBody.Ack;
            case MessageBody.DeleteModelRequest:
                return MessageBody.Ack;
            case MessageBody.DebugCommandRequest:
                return MessageBody.DebugCommandResponse;
            case MessageBody.CancelRequest:
                return MessageBody.Ack;
            case MessageBody.ResumeAgentRequest:
                return MessageBody.Ack;
            case MessageBody.UpdateAgentVariablesRequest:
                return MessageBody.Ack;
            case MessageBody.GetAgentVariablesRequest:
                return MessageBody.GetAgentVariablesResponse;
            case MessageBody.PrefillAgentKvCacheRequest:
                return MessageBody.PrefillAgentKvCacheResponse;
            case MessageBody.EvictAgentKvCacheRequest:
                return MessageBody.EvictAgentKvCacheResponse;
            case MessageBody.GetAgentKvCacheStatusRequest:
                return MessageBody.GetAgentKvCacheStatusResponse;
            case MessageBody.AppendInteractionsRequest:
                return MessageBody.AppendInteractionsResponse;
            case MessageBody.RemoveInteractionsFromEndRequest:
                return MessageBody.RemoveInteractionsFromEndResponse;
            case MessageBody.SetSessionLocaleRequest:
                return MessageBody.Ack;
            case MessageBody.ExportDialogRequest:
                return MessageBody.ExportDialogResponse;
            case MessageBody.ImportDialogRequest:
                return MessageBody.ImportDialogResponse;
            case MessageBody.GetMemoryConsumptionRequest:
                return MessageBody.GetMemoryConsumptionResponse;
            case MessageBody.EnsureMutableKbRequest:
                return MessageBody.EnsureMutableKbResponse;
            case MessageBody.EnumerateMutableKbsRequest:
                return MessageBody.EnumerateMutableKbsResponse;
            case MessageBody.MutableKbWriteRequest:
                return MessageBody.MutableKbWriteResponse;
            case MessageBody.MutableKbDeleteRequest:
                return MessageBody.MutableKbDeleteResponse;
            case MessageBody.MutableKbClearRequest:
                return MessageBody.MutableKbClearResponse;
            case MessageBody.MutableKbQueryRequest:
                return MessageBody.MutableKbQueryResponse;
            case MessageBody.MutableKbGetRequest:
                return MessageBody.MutableKbGetResponse;
            case MessageBody.MutableKbListRequest:
                return MessageBody.MutableKbListResponse;
            case MessageBody.MutableKbStatsRequest:
                return MessageBody.MutableKbStatsResponse;
            case MessageBody.ExportMutableKbRequest:
                return MessageBody.ExportMutableKbResponse;
            case MessageBody.ImportMutableKbRequest:
                return MessageBody.ImportMutableKbResponse;
            case MessageBody.SaveGameMemoryRequest:
                return MessageBody.SaveGameMemoryResponse;
            case MessageBody.LoadGameMemoryRequest:
                return MessageBody.LoadGameMemoryResponse;
            case MessageBody.SaveDialogRequest:
                return MessageBody.SaveDialogResponse;
            case MessageBody.LoadDialogRequest:
                return MessageBody.LoadDialogResponse;
            case MessageBody.SetAgentVoiceRequest:
                return MessageBody.Ack;
            case MessageBody.SetAgentDeliveryModeRequest:
                return MessageBody.Ack;
            default:
                return MessageBody.NONE;
            }
        }

        /// <summary>Declared progress frame, or <see cref="MessageBody.NONE"/>.</summary>
        public static MessageBody RpcProgressOf(MessageBody request)
        {
            switch (request)
            {
            case MessageBody.SendMessageRequest:
                return MessageBody.AnswerText;
            case MessageBody.DownloadModelRequest:
                return MessageBody.DownloadProgress;
            default:
                return MessageBody.NONE;
            }
        }

        /// <summary>A Response or Ack terminal and no progress frame.</summary>
        public static bool RpcIsUnary(MessageBody request)
        {
            switch (request)
            {
            case MessageBody.CreateSessionRequest:
            case MessageBody.CreateAgentRequest:
            case MessageBody.DestroyAgentRequest:
            case MessageBody.ListModelsRequest:
            case MessageBody.CreateStringStorageRequest:
            case MessageBody.DestroyStringStorageRequest:
            case MessageBody.LoadModelRequest:
            case MessageBody.UnloadModelRequest:
            case MessageBody.CreateEmbeddedStringStorageRequest:
            case MessageBody.DestroyEmbeddedStringStorageRequest:
            case MessageBody.ChangeAgentParamRequest:
            case MessageBody.CreateVoiceInputRequest:
            case MessageBody.BeginUtteranceRequest:
            case MessageBody.EndUtteranceRequest:
            case MessageBody.CancelUtteranceRequest:
            case MessageBody.DestroyVoiceInputRequest:
            case MessageBody.DeleteModelRequest:
            case MessageBody.DebugCommandRequest:
            case MessageBody.CancelRequest:
            case MessageBody.ResumeAgentRequest:
            case MessageBody.UpdateAgentVariablesRequest:
            case MessageBody.GetAgentVariablesRequest:
            case MessageBody.PrefillAgentKvCacheRequest:
            case MessageBody.EvictAgentKvCacheRequest:
            case MessageBody.GetAgentKvCacheStatusRequest:
            case MessageBody.AppendInteractionsRequest:
            case MessageBody.RemoveInteractionsFromEndRequest:
            case MessageBody.SetSessionLocaleRequest:
            case MessageBody.ExportDialogRequest:
            case MessageBody.ImportDialogRequest:
            case MessageBody.GetMemoryConsumptionRequest:
            case MessageBody.EnsureMutableKbRequest:
            case MessageBody.EnumerateMutableKbsRequest:
            case MessageBody.MutableKbWriteRequest:
            case MessageBody.MutableKbDeleteRequest:
            case MessageBody.MutableKbClearRequest:
            case MessageBody.MutableKbQueryRequest:
            case MessageBody.MutableKbGetRequest:
            case MessageBody.MutableKbListRequest:
            case MessageBody.MutableKbStatsRequest:
            case MessageBody.ExportMutableKbRequest:
            case MessageBody.ImportMutableKbRequest:
            case MessageBody.SaveGameMemoryRequest:
            case MessageBody.LoadGameMemoryRequest:
            case MessageBody.SaveDialogRequest:
            case MessageBody.LoadDialogRequest:
            case MessageBody.SetAgentVoiceRequest:
            case MessageBody.SetAgentDeliveryModeRequest:
                return true;
            default:
                return false;
            }
        }

        /// <summary>
        /// A streaming frame that carries a request id but is not that
        /// request's terminal or progress reply. The registry leaves it for
        /// the push path.
        /// </summary>
        public static bool IsRequestScopedPush(MessageBody body)
        {
            switch (body)
            {
            case MessageBody.TtsAudioFormatFrame:
            case MessageBody.TtsAudioFrame:
            case MessageBody.TtsVisemeFrame:
                return true;
            default:
                return false;
            }
        }
    }
}
