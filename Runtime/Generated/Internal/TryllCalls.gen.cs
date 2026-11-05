// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/messages.fbs  +  tools/codegen/templates/protocol_call_stubs_csharp.cs.j2
// See: docs/dev/codegen.md
//
// One stub per request except SendMessage and DownloadModel (streaming) and
// AudioBuffer (manual fire-and-forget). A stub encodes on the caller, registers
// the pending handler, and posts the unpacked reply. The wrapper projects that
// reply into the public type and does the fan-out.

using System;
using System.Collections.Generic;
using Tryll;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllCalls_gen {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}



namespace Tryll.Client.Internal
{
    internal static class TryllCalls
    {


        internal static void SetInferenceThrottle(
            TryllConnection conn, float throttle)
        {
            conn.Send(ClientCodec.BuildSetInferenceThrottle(throttle));
        }


        /// <summary>CreateSession.</summary>
        internal static void CreateSession(
            TryllConnection conn,
            object owner, string game_name, string locale, SessionModelConfigT model_config,
            Action<CreateSessionResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            CreateSession<CreateSessionResponseT>(
                conn, owner, game_name, locale, model_config,
                static body => body, done, note);
        }

        internal static void CreateSession<R>(
            TryllConnection conn,
            object owner, string game_name, string locale, SessionModelConfigT model_config,
            Func<CreateSessionResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildCreateSessionRequest(requestId, game_name, locale, model_config);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.CreateSessionRequest,
                MessageBody.CreateSessionResponse,
                msg => project(msg.BodyAsCreateSessionResponse().UnPack()),
                done,
                note);
        }


        /// <summary>CreateAgent.</summary>
        internal static void CreateAgent(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Action<CreateAgentResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            CreateAgent<CreateAgentResponseT>(
                conn, owner, encode,
                static body => body, done, note);
        }

        internal static void CreateAgent<R>(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Func<CreateAgentResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = encode(requestId);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.CreateAgentRequest,
                MessageBody.CreateAgentResponse,
                msg => project(msg.BodyAsCreateAgentResponse().UnPack()),
                done,
                note);
        }


        /// <summary>DestroyAgent.</summary>
        internal static void DestroyAgent(
            TryllConnection conn,
            object owner, ulong agent_id,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildDestroyAgentRequest(requestId, agent_id);
            conn.CallAck(owner, requestId, frame, MessageBody.DestroyAgentRequest, done, note);
        }


        /// <summary>ListModels.</summary>
        internal static void ListModels(
            TryllConnection conn,
            object owner,
            Action<ListModelsResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            ListModels<ListModelsResponseT>(
                conn, owner,
                static body => body, done, note);
        }

        internal static void ListModels<R>(
            TryllConnection conn,
            object owner,
            Func<ListModelsResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildListModelsRequest(requestId);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.ListModelsRequest,
                MessageBody.ListModelsResponse,
                msg => project(msg.BodyAsListModelsResponse().UnPack()),
                done,
                note);
        }


        /// <summary>CreateStringStorage.</summary>
        internal static void CreateStringStorage(
            TryllConnection conn,
            object owner, string name, StringStorageKind kind, IReadOnlyList<string> strings, IReadOnlyList<string> keys, string file_path,
            Action<CreateStringStorageResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            CreateStringStorage<CreateStringStorageResponseT>(
                conn, owner, name, kind, strings, keys, file_path,
                static body => body, done, note);
        }

        internal static void CreateStringStorage<R>(
            TryllConnection conn,
            object owner, string name, StringStorageKind kind, IReadOnlyList<string> strings, IReadOnlyList<string> keys, string file_path,
            Func<CreateStringStorageResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildCreateStringStorageRequest(requestId, name, kind, strings, keys, file_path);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.CreateStringStorageRequest,
                MessageBody.CreateStringStorageResponse,
                msg => project(msg.BodyAsCreateStringStorageResponse().UnPack()),
                done,
                note);
        }


        /// <summary>DestroyStringStorage.</summary>
        internal static void DestroyStringStorage(
            TryllConnection conn,
            object owner, string name,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildDestroyStringStorageRequest(requestId, name);
            conn.CallAck(owner, requestId, frame, MessageBody.DestroyStringStorageRequest, done, note);
        }


        /// <summary>LoadModel.</summary>
        internal static void LoadModel(
            TryllConnection conn,
            object owner, string model_name, MemoryPlacement placement, bool placement_is_override,
            Action<LoadModelResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            LoadModel<LoadModelResponseT>(
                conn, owner, model_name, placement, placement_is_override,
                static body => body, done, note);
        }

        internal static void LoadModel<R>(
            TryllConnection conn,
            object owner, string model_name, MemoryPlacement placement, bool placement_is_override,
            Func<LoadModelResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildLoadModelRequest(requestId, model_name, placement, placement_is_override);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.LoadModelRequest,
                MessageBody.LoadModelResponse,
                msg => project(msg.BodyAsLoadModelResponse().UnPack()),
                done,
                note);
        }


        /// <summary>UnloadModel.</summary>
        internal static void UnloadModel(
            TryllConnection conn,
            object owner, string model_name,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildUnloadModelRequest(requestId, model_name);
            conn.CallAck(owner, requestId, frame, MessageBody.UnloadModelRequest, done, note);
        }


        /// <summary>CreateEmbeddedStringStorage.</summary>
        internal static void CreateEmbeddedStringStorage(
            TryllConnection conn,
            object owner, string name, string config_path, IReadOnlyList<string> strings, string embedding_model,
            Action<CreateEmbeddedStringStorageResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            CreateEmbeddedStringStorage<CreateEmbeddedStringStorageResponseT>(
                conn, owner, name, config_path, strings, embedding_model,
                static body => body, done, note);
        }

        internal static void CreateEmbeddedStringStorage<R>(
            TryllConnection conn,
            object owner, string name, string config_path, IReadOnlyList<string> strings, string embedding_model,
            Func<CreateEmbeddedStringStorageResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildCreateEmbeddedStringStorageRequest(requestId, name, config_path, strings, embedding_model);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.CreateEmbeddedStringStorageRequest,
                MessageBody.CreateEmbeddedStringStorageResponse,
                msg => project(msg.BodyAsCreateEmbeddedStringStorageResponse().UnPack()),
                done,
                note);
        }


        /// <summary>DestroyEmbeddedStringStorage.</summary>
        internal static void DestroyEmbeddedStringStorage(
            TryllConnection conn,
            object owner, string name,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildDestroyEmbeddedStringStorageRequest(requestId, name);
            conn.CallAck(owner, requestId, frame, MessageBody.DestroyEmbeddedStringStorageRequest, done, note);
        }


        /// <summary>ChangeAgentParam.</summary>
        internal static void ChangeAgentParam(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = encode(requestId);
            conn.CallAck(owner, requestId, frame, MessageBody.ChangeAgentParamRequest, done, note);
        }


        /// <summary>CreateVoiceInput.</summary>
        internal static void CreateVoiceInput(
            TryllConnection conn,
            object owner, string model_name, WireAudioFormatT input_format, float vad_threshold, uint vad_min_silence_ms, uint vad_speech_pad_ms, string hotwords_storage_path, float hotwords_score,
            Action<CreateVoiceInputResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            CreateVoiceInput<CreateVoiceInputResponseT>(
                conn, owner, model_name, input_format, vad_threshold, vad_min_silence_ms, vad_speech_pad_ms, hotwords_storage_path, hotwords_score,
                static body => body, done, note);
        }

        internal static void CreateVoiceInput<R>(
            TryllConnection conn,
            object owner, string model_name, WireAudioFormatT input_format, float vad_threshold, uint vad_min_silence_ms, uint vad_speech_pad_ms, string hotwords_storage_path, float hotwords_score,
            Func<CreateVoiceInputResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildCreateVoiceInputRequest(requestId, model_name, input_format, vad_threshold, vad_min_silence_ms, vad_speech_pad_ms, hotwords_storage_path, hotwords_score);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.CreateVoiceInputRequest,
                MessageBody.CreateVoiceInputResponse,
                msg => project(msg.BodyAsCreateVoiceInputResponse().UnPack()),
                done,
                note);
        }


        /// <summary>BeginUtterance.</summary>
        internal static void BeginUtterance(
            TryllConnection conn,
            object owner, ulong voice_input_id, bool auto_finish_on_silence, uint max_utterance_ms,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildBeginUtteranceRequest(requestId, voice_input_id, auto_finish_on_silence, max_utterance_ms);
            conn.CallAck(owner, requestId, frame, MessageBody.BeginUtteranceRequest, done, note);
        }


        /// <summary>EndUtterance.</summary>
        internal static void EndUtterance(
            TryllConnection conn,
            object owner, ulong voice_input_id,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildEndUtteranceRequest(requestId, voice_input_id);
            conn.CallAck(owner, requestId, frame, MessageBody.EndUtteranceRequest, done, note);
        }


        /// <summary>CancelUtterance.</summary>
        internal static void CancelUtterance(
            TryllConnection conn,
            object owner, ulong voice_input_id,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildCancelUtteranceRequest(requestId, voice_input_id);
            conn.CallAck(owner, requestId, frame, MessageBody.CancelUtteranceRequest, done, note);
        }


        /// <summary>DestroyVoiceInput.</summary>
        internal static void DestroyVoiceInput(
            TryllConnection conn,
            object owner, ulong voice_input_id,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildDestroyVoiceInputRequest(requestId, voice_input_id);
            conn.CallAck(owner, requestId, frame, MessageBody.DestroyVoiceInputRequest, done, note);
        }


        /// <summary>DeleteModel.</summary>
        internal static void DeleteModel(
            TryllConnection conn,
            object owner, string model_name,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildDeleteModelRequest(requestId, model_name);
            conn.CallAck(owner, requestId, frame, MessageBody.DeleteModelRequest, done, note);
        }


        /// <summary>DebugCommand. May never reply.</summary>
        internal static void DebugCommand(
            TryllConnection conn,
            object owner, string command, string payload,
            Action<DebugCommandResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            DebugCommand<DebugCommandResponseT>(
                conn, owner, command, payload,
                static body => body, done, note);
        }

        internal static void DebugCommand<R>(
            TryllConnection conn,
            object owner, string command, string payload,
            Func<DebugCommandResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildDebugCommandRequest(requestId, command, payload);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.DebugCommandRequest,
                MessageBody.DebugCommandResponse,
                msg => project(msg.BodyAsDebugCommandResponse().UnPack()),
                done,
                note);
        }


        /// <summary>Cancel.</summary>
        internal static void Cancel(
            TryllConnection conn,
            object owner, ulong agent_id, CancelMode mode,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildCancelRequest(requestId, agent_id, mode);
            conn.CallAck(owner, requestId, frame, MessageBody.CancelRequest, done, note);
        }


        /// <summary>ResumeAgent.</summary>
        internal static void ResumeAgent(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = encode(requestId);
            conn.CallAck(owner, requestId, frame, MessageBody.ResumeAgentRequest, done, note);
        }


        /// <summary>UpdateAgentVariables.</summary>
        internal static void UpdateAgentVariables(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = encode(requestId);
            conn.CallAck(owner, requestId, frame, MessageBody.UpdateAgentVariablesRequest, done, note);
        }


        /// <summary>GetAgentVariables.</summary>
        internal static void GetAgentVariables(
            TryllConnection conn,
            object owner, ulong agent_id, IReadOnlyList<string> names,
            Action<GetAgentVariablesResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            GetAgentVariables<GetAgentVariablesResponseT>(
                conn, owner, agent_id, names,
                static body => body, done, note);
        }

        internal static void GetAgentVariables<R>(
            TryllConnection conn,
            object owner, ulong agent_id, IReadOnlyList<string> names,
            Func<GetAgentVariablesResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildGetAgentVariablesRequest(requestId, agent_id, names);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.GetAgentVariablesRequest,
                MessageBody.GetAgentVariablesResponse,
                msg => project(msg.BodyAsGetAgentVariablesResponse().UnPack()),
                done,
                note);
        }


        /// <summary>PrefillAgentKvCache.</summary>
        internal static void PrefillAgentKvCache(
            TryllConnection conn,
            object owner, ulong agent_id,
            Action<PrefillAgentKvCacheResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            PrefillAgentKvCache<PrefillAgentKvCacheResponseT>(
                conn, owner, agent_id,
                static body => body, done, note);
        }

        internal static void PrefillAgentKvCache<R>(
            TryllConnection conn,
            object owner, ulong agent_id,
            Func<PrefillAgentKvCacheResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildPrefillAgentKvCacheRequest(requestId, agent_id);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.PrefillAgentKvCacheRequest,
                MessageBody.PrefillAgentKvCacheResponse,
                msg => project(msg.BodyAsPrefillAgentKvCacheResponse().UnPack()),
                done,
                note);
        }


        /// <summary>EvictAgentKvCache.</summary>
        internal static void EvictAgentKvCache(
            TryllConnection conn,
            object owner, ulong agent_id,
            Action<EvictAgentKvCacheResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            EvictAgentKvCache<EvictAgentKvCacheResponseT>(
                conn, owner, agent_id,
                static body => body, done, note);
        }

        internal static void EvictAgentKvCache<R>(
            TryllConnection conn,
            object owner, ulong agent_id,
            Func<EvictAgentKvCacheResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildEvictAgentKvCacheRequest(requestId, agent_id);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.EvictAgentKvCacheRequest,
                MessageBody.EvictAgentKvCacheResponse,
                msg => project(msg.BodyAsEvictAgentKvCacheResponse().UnPack()),
                done,
                note);
        }


        /// <summary>GetAgentKvCacheStatus.</summary>
        internal static void GetAgentKvCacheStatus(
            TryllConnection conn,
            object owner, ulong agent_id,
            Action<GetAgentKvCacheStatusResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            GetAgentKvCacheStatus<GetAgentKvCacheStatusResponseT>(
                conn, owner, agent_id,
                static body => body, done, note);
        }

        internal static void GetAgentKvCacheStatus<R>(
            TryllConnection conn,
            object owner, ulong agent_id,
            Func<GetAgentKvCacheStatusResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildGetAgentKvCacheStatusRequest(requestId, agent_id);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.GetAgentKvCacheStatusRequest,
                MessageBody.GetAgentKvCacheStatusResponse,
                msg => project(msg.BodyAsGetAgentKvCacheStatusResponse().UnPack()),
                done,
                note);
        }


        /// <summary>AppendInteractions.</summary>
        internal static void AppendInteractions(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Action<AppendInteractionsResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            AppendInteractions<AppendInteractionsResponseT>(
                conn, owner, encode,
                static body => body, done, note);
        }

        internal static void AppendInteractions<R>(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Func<AppendInteractionsResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = encode(requestId);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.AppendInteractionsRequest,
                MessageBody.AppendInteractionsResponse,
                msg => project(msg.BodyAsAppendInteractionsResponse().UnPack()),
                done,
                note);
        }


        /// <summary>RemoveInteractionsFromEnd.</summary>
        internal static void RemoveInteractionsFromEnd(
            TryllConnection conn,
            object owner, ulong agent_id, uint count,
            Action<RemoveInteractionsFromEndResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            RemoveInteractionsFromEnd<RemoveInteractionsFromEndResponseT>(
                conn, owner, agent_id, count,
                static body => body, done, note);
        }

        internal static void RemoveInteractionsFromEnd<R>(
            TryllConnection conn,
            object owner, ulong agent_id, uint count,
            Func<RemoveInteractionsFromEndResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildRemoveInteractionsFromEndRequest(requestId, agent_id, count);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.RemoveInteractionsFromEndRequest,
                MessageBody.RemoveInteractionsFromEndResponse,
                msg => project(msg.BodyAsRemoveInteractionsFromEndResponse().UnPack()),
                done,
                note);
        }


        /// <summary>SetSessionLocale.</summary>
        internal static void SetSessionLocale(
            TryllConnection conn,
            object owner, string locale,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildSetSessionLocaleRequest(requestId, locale);
            conn.CallAck(owner, requestId, frame, MessageBody.SetSessionLocaleRequest, done, note);
        }


        /// <summary>ExportDialog.</summary>
        internal static void ExportDialog(
            TryllConnection conn,
            object owner, ulong agent_id,
            Action<ExportDialogResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            ExportDialog<ExportDialogResponseT>(
                conn, owner, agent_id,
                static body => body, done, note);
        }

        internal static void ExportDialog<R>(
            TryllConnection conn,
            object owner, ulong agent_id,
            Func<ExportDialogResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildExportDialogRequest(requestId, agent_id);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.ExportDialogRequest,
                MessageBody.ExportDialogResponse,
                msg => project(msg.BodyAsExportDialogResponse().UnPack()),
                done,
                note);
        }


        /// <summary>ImportDialog.</summary>
        internal static void ImportDialog(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Action<ImportDialogResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            ImportDialog<ImportDialogResponseT>(
                conn, owner, encode,
                static body => body, done, note);
        }

        internal static void ImportDialog<R>(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Func<ImportDialogResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = encode(requestId);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.ImportDialogRequest,
                MessageBody.ImportDialogResponse,
                msg => project(msg.BodyAsImportDialogResponse().UnPack()),
                done,
                note);
        }


        /// <summary>GetMemoryConsumption.</summary>
        internal static void GetMemoryConsumption(
            TryllConnection conn,
            object owner,
            Action<GetMemoryConsumptionResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            GetMemoryConsumption<GetMemoryConsumptionResponseT>(
                conn, owner,
                static body => body, done, note);
        }

        internal static void GetMemoryConsumption<R>(
            TryllConnection conn,
            object owner,
            Func<GetMemoryConsumptionResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildGetMemoryConsumptionRequest(requestId);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.GetMemoryConsumptionRequest,
                MessageBody.GetMemoryConsumptionResponse,
                msg => project(msg.BodyAsGetMemoryConsumptionResponse().UnPack()),
                done,
                note);
        }


        /// <summary>EnsureMutableKb.</summary>
        internal static void EnsureMutableKb(
            TryllConnection conn,
            object owner, string name, string schema,
            Action<EnsureMutableKbResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            EnsureMutableKb<EnsureMutableKbResponseT>(
                conn, owner, name, schema,
                static body => body, done, note);
        }

        internal static void EnsureMutableKb<R>(
            TryllConnection conn,
            object owner, string name, string schema,
            Func<EnsureMutableKbResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildEnsureMutableKbRequest(requestId, name, schema);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.EnsureMutableKbRequest,
                MessageBody.EnsureMutableKbResponse,
                msg => project(msg.BodyAsEnsureMutableKbResponse().UnPack()),
                done,
                note);
        }


        /// <summary>EnumerateMutableKbs.</summary>
        internal static void EnumerateMutableKbs(
            TryllConnection conn,
            object owner,
            Action<EnumerateMutableKbsResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            EnumerateMutableKbs<EnumerateMutableKbsResponseT>(
                conn, owner,
                static body => body, done, note);
        }

        internal static void EnumerateMutableKbs<R>(
            TryllConnection conn,
            object owner,
            Func<EnumerateMutableKbsResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildEnumerateMutableKbsRequest(requestId);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.EnumerateMutableKbsRequest,
                MessageBody.EnumerateMutableKbsResponse,
                msg => project(msg.BodyAsEnumerateMutableKbsResponse().UnPack()),
                done,
                note);
        }


        /// <summary>MutableKbWrite.</summary>
        internal static void MutableKbWrite(
            TryllConnection conn,
            object owner, string name, string schema, IReadOnlyList<MutableKbUpsertT> records,
            Action<MutableKbWriteResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            MutableKbWrite<MutableKbWriteResponseT>(
                conn, owner, name, schema, records,
                static body => body, done, note);
        }

        internal static void MutableKbWrite<R>(
            TryllConnection conn,
            object owner, string name, string schema, IReadOnlyList<MutableKbUpsertT> records,
            Func<MutableKbWriteResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildMutableKbWriteRequest(requestId, name, schema, records);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.MutableKbWriteRequest,
                MessageBody.MutableKbWriteResponse,
                msg => project(msg.BodyAsMutableKbWriteResponse().UnPack()),
                done,
                note);
        }


        /// <summary>MutableKbDelete.</summary>
        internal static void MutableKbDelete(
            TryllConnection conn,
            object owner, string name, IReadOnlyList<string> ids,
            Action<MutableKbDeleteResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            MutableKbDelete<MutableKbDeleteResponseT>(
                conn, owner, name, ids,
                static body => body, done, note);
        }

        internal static void MutableKbDelete<R>(
            TryllConnection conn,
            object owner, string name, IReadOnlyList<string> ids,
            Func<MutableKbDeleteResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildMutableKbDeleteRequest(requestId, name, ids);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.MutableKbDeleteRequest,
                MessageBody.MutableKbDeleteResponse,
                msg => project(msg.BodyAsMutableKbDeleteResponse().UnPack()),
                done,
                note);
        }


        /// <summary>MutableKbClear.</summary>
        internal static void MutableKbClear(
            TryllConnection conn,
            object owner, string name,
            Action<MutableKbClearResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            MutableKbClear<MutableKbClearResponseT>(
                conn, owner, name,
                static body => body, done, note);
        }

        internal static void MutableKbClear<R>(
            TryllConnection conn,
            object owner, string name,
            Func<MutableKbClearResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildMutableKbClearRequest(requestId, name);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.MutableKbClearRequest,
                MessageBody.MutableKbClearResponse,
                msg => project(msg.BodyAsMutableKbClearResponse().UnPack()),
                done,
                note);
        }


        /// <summary>MutableKbQuery.</summary>
        internal static void MutableKbQuery(
            TryllConnection conn,
            object owner, string name, string query_text, uint top_k, RetrievalMode retrieval_mode,
            Action<MutableKbQueryResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            MutableKbQuery<MutableKbQueryResponseT>(
                conn, owner, name, query_text, top_k, retrieval_mode,
                static body => body, done, note);
        }

        internal static void MutableKbQuery<R>(
            TryllConnection conn,
            object owner, string name, string query_text, uint top_k, RetrievalMode retrieval_mode,
            Func<MutableKbQueryResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildMutableKbQueryRequest(requestId, name, query_text, top_k, retrieval_mode);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.MutableKbQueryRequest,
                MessageBody.MutableKbQueryResponse,
                msg => project(msg.BodyAsMutableKbQueryResponse().UnPack()),
                done,
                note);
        }


        /// <summary>MutableKbGet.</summary>
        internal static void MutableKbGet(
            TryllConnection conn,
            object owner, string name, string id,
            Action<MutableKbGetResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            MutableKbGet<MutableKbGetResponseT>(
                conn, owner, name, id,
                static body => body, done, note);
        }

        internal static void MutableKbGet<R>(
            TryllConnection conn,
            object owner, string name, string id,
            Func<MutableKbGetResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildMutableKbGetRequest(requestId, name, id);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.MutableKbGetRequest,
                MessageBody.MutableKbGetResponse,
                msg => project(msg.BodyAsMutableKbGetResponse().UnPack()),
                done,
                note);
        }


        /// <summary>MutableKbList.</summary>
        internal static void MutableKbList(
            TryllConnection conn,
            object owner, string name, uint offset, uint limit,
            Action<MutableKbListResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            MutableKbList<MutableKbListResponseT>(
                conn, owner, name, offset, limit,
                static body => body, done, note);
        }

        internal static void MutableKbList<R>(
            TryllConnection conn,
            object owner, string name, uint offset, uint limit,
            Func<MutableKbListResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildMutableKbListRequest(requestId, name, offset, limit);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.MutableKbListRequest,
                MessageBody.MutableKbListResponse,
                msg => project(msg.BodyAsMutableKbListResponse().UnPack()),
                done,
                note);
        }


        /// <summary>MutableKbStats.</summary>
        internal static void MutableKbStats(
            TryllConnection conn,
            object owner, string name,
            Action<MutableKbStatsResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            MutableKbStats<MutableKbStatsResponseT>(
                conn, owner, name,
                static body => body, done, note);
        }

        internal static void MutableKbStats<R>(
            TryllConnection conn,
            object owner, string name,
            Func<MutableKbStatsResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildMutableKbStatsRequest(requestId, name);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.MutableKbStatsRequest,
                MessageBody.MutableKbStatsResponse,
                msg => project(msg.BodyAsMutableKbStatsResponse().UnPack()),
                done,
                note);
        }


        /// <summary>ExportMutableKb.</summary>
        internal static void ExportMutableKb(
            TryllConnection conn,
            object owner, string name,
            Action<ExportMutableKbResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            ExportMutableKb<ExportMutableKbResponseT>(
                conn, owner, name,
                static body => body, done, note);
        }

        internal static void ExportMutableKb<R>(
            TryllConnection conn,
            object owner, string name,
            Func<ExportMutableKbResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildExportMutableKbRequest(requestId, name);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.ExportMutableKbRequest,
                MessageBody.ExportMutableKbResponse,
                msg => project(msg.BodyAsExportMutableKbResponse().UnPack()),
                done,
                note);
        }


        /// <summary>ImportMutableKb.</summary>
        internal static void ImportMutableKb(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Action<ImportMutableKbResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            ImportMutableKb<ImportMutableKbResponseT>(
                conn, owner, encode,
                static body => body, done, note);
        }

        internal static void ImportMutableKb<R>(
            TryllConnection conn,
            object owner, Func<ulong, byte[]> encode,
            Func<ImportMutableKbResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = encode(requestId);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.ImportMutableKbRequest,
                MessageBody.ImportMutableKbResponse,
                msg => project(msg.BodyAsImportMutableKbResponse().UnPack()),
                done,
                note);
        }


        /// <summary>SaveGameMemory.</summary>
        internal static void SaveGameMemory(
            TryllConnection conn,
            object owner, string path, string save_id,
            Action<SaveGameMemoryResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            SaveGameMemory<SaveGameMemoryResponseT>(
                conn, owner, path, save_id,
                static body => body, done, note);
        }

        internal static void SaveGameMemory<R>(
            TryllConnection conn,
            object owner, string path, string save_id,
            Func<SaveGameMemoryResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildSaveGameMemoryRequest(requestId, path, save_id);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.SaveGameMemoryRequest,
                MessageBody.SaveGameMemoryResponse,
                msg => project(msg.BodyAsSaveGameMemoryResponse().UnPack()),
                done,
                note);
        }


        /// <summary>LoadGameMemory.</summary>
        internal static void LoadGameMemory(
            TryllConnection conn,
            object owner, string path, MutableKbCompatibility compatibility, MutableKbEmbedTiming embed_timing,
            Action<LoadGameMemoryResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            LoadGameMemory<LoadGameMemoryResponseT>(
                conn, owner, path, compatibility, embed_timing,
                static body => body, done, note);
        }

        internal static void LoadGameMemory<R>(
            TryllConnection conn,
            object owner, string path, MutableKbCompatibility compatibility, MutableKbEmbedTiming embed_timing,
            Func<LoadGameMemoryResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildLoadGameMemoryRequest(requestId, path, compatibility, embed_timing);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.LoadGameMemoryRequest,
                MessageBody.LoadGameMemoryResponse,
                msg => project(msg.BodyAsLoadGameMemoryResponse().UnPack()),
                done,
                note);
        }


        /// <summary>SaveDialog.</summary>
        internal static void SaveDialog(
            TryllConnection conn,
            object owner, ulong agent_id, string path, string save_id,
            Action<SaveDialogResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            SaveDialog<SaveDialogResponseT>(
                conn, owner, agent_id, path, save_id,
                static body => body, done, note);
        }

        internal static void SaveDialog<R>(
            TryllConnection conn,
            object owner, ulong agent_id, string path, string save_id,
            Func<SaveDialogResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildSaveDialogRequest(requestId, agent_id, path, save_id);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.SaveDialogRequest,
                MessageBody.SaveDialogResponse,
                msg => project(msg.BodyAsSaveDialogResponse().UnPack()),
                done,
                note);
        }


        /// <summary>LoadDialog.</summary>
        internal static void LoadDialog(
            TryllConnection conn,
            object owner, ulong agent_id, string path, DialogCompatibility compatibility,
            Action<LoadDialogResponseT, TryllError> done,
            Action<ulong> note = null)
        {
            LoadDialog<LoadDialogResponseT>(
                conn, owner, agent_id, path, compatibility,
                static body => body, done, note);
        }

        internal static void LoadDialog<R>(
            TryllConnection conn,
            object owner, ulong agent_id, string path, DialogCompatibility compatibility,
            Func<LoadDialogResponseT, R> project,
            Action<R, TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildLoadDialogRequest(requestId, agent_id, path, compatibility);
            conn.CallUnary(
                owner,
                requestId,
                frame,
                MessageBody.LoadDialogRequest,
                MessageBody.LoadDialogResponse,
                msg => project(msg.BodyAsLoadDialogResponse().UnPack()),
                done,
                note);
        }


        /// <summary>SetAgentVoice.</summary>
        internal static void SetAgentVoice(
            TryllConnection conn,
            object owner, ulong agent_id, VoiceProfileT voice,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildSetAgentVoiceRequest(requestId, agent_id, voice);
            conn.CallAck(owner, requestId, frame, MessageBody.SetAgentVoiceRequest, done, note);
        }


        /// <summary>SetAgentDeliveryMode.</summary>
        internal static void SetAgentDeliveryMode(
            TryllConnection conn,
            object owner, ulong agent_id, string mode, float intensity,
            Action<TryllError> done,
            Action<ulong> note = null)
        {
            var requestId = conn.NextRequestId();
            var frame = ClientCodec.BuildSetAgentDeliveryModeRequest(requestId, agent_id, mode, intensity);
            conn.CallAck(owner, requestId, frame, MessageBody.SetAgentDeliveryModeRequest, done, note);
        }
    }
}
