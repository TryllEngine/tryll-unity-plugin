// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/messages.fbs  +  tools/codegen/templates/protocol_codec_csharp.cs.j2
// See: docs/dev/codegen.md

using System.Collections.Generic;
using Google.FlatBuffers;
using Tryll;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_ClientCodec_gen {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client.Internal
{
    internal static partial class ClientCodec
    {
        // ── Private helper ────────────────────────────────────────────────────

        private static byte[] WrapMessage<T>(FlatBufferBuilder fbb, Offset<T> off, MessageBody bodyType)
            where T : struct
        {
            var msgOff = Message.CreateMessage(fbb, bodyType, off.Value);
            Message.FinishMessageBuffer(fbb, msgOff);
            return fbb.SizedByteArray();
        }

        // ── Generated encoders: non-manual Request + FireAndForget messages ───
        internal static byte[] BuildCreateSessionRequest(ulong request_id, string game_name, string locale, SessionModelConfigT model_config)
        {
            var fbb = new FlatBufferBuilder(256);
            var game_name_off = !string.IsNullOrEmpty(game_name) ? fbb.CreateString(game_name) : default(StringOffset);
            var locale_off = !string.IsNullOrEmpty(locale) ? fbb.CreateString(locale) : default(StringOffset);
            var model_config_off = SessionModelConfig.Pack(fbb, model_config);
            var body = CreateSessionRequest.CreateCreateSessionRequest(fbb, request_id, game_name_off, locale_off, model_config_off);
            return WrapMessage(fbb, body, MessageBody.CreateSessionRequest);
        }

        internal static byte[] BuildSendMessageRequest(ulong request_id, ulong agent_id, string text)
        {
            var fbb = new FlatBufferBuilder(256);
            var text_off = !string.IsNullOrEmpty(text) ? fbb.CreateString(text) : default(StringOffset);
            var body = SendMessageRequest.CreateSendMessageRequest(fbb, request_id, agent_id, text_off);
            return WrapMessage(fbb, body, MessageBody.SendMessageRequest);
        }

        internal static byte[] BuildDestroyAgentRequest(ulong request_id, ulong agent_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = DestroyAgentRequest.CreateDestroyAgentRequest(fbb, request_id, agent_id);
            return WrapMessage(fbb, body, MessageBody.DestroyAgentRequest);
        }

        internal static byte[] BuildListModelsRequest(ulong request_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = ListModelsRequest.CreateListModelsRequest(fbb, request_id);
            return WrapMessage(fbb, body, MessageBody.ListModelsRequest);
        }

        internal static byte[] BuildDownloadModelRequest(ulong request_id, string model_name)
        {
            var fbb = new FlatBufferBuilder(256);
            var model_name_off = !string.IsNullOrEmpty(model_name) ? fbb.CreateString(model_name) : default(StringOffset);
            var body = DownloadModelRequest.CreateDownloadModelRequest(fbb, request_id, model_name_off);
            return WrapMessage(fbb, body, MessageBody.DownloadModelRequest);
        }

        internal static byte[] BuildCreateStringStorageRequest(ulong request_id, string name, StringStorageKind kind, IReadOnlyList<string> strings, IReadOnlyList<string> keys, string file_path)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            VectorOffset strings_off = default;
            if (strings != null && strings.Count > 0)
            {
                var _strings_strs = new StringOffset[strings.Count];
                for (int i = 0; i < _strings_strs.Length; i++)
                    _strings_strs[i] = fbb.CreateString(strings[i] ?? "");
                strings_off = CreateStringStorageRequest.CreateStringsVector(fbb, _strings_strs);
            }
            VectorOffset keys_off = default;
            if (keys != null && keys.Count > 0)
            {
                var _keys_strs = new StringOffset[keys.Count];
                for (int i = 0; i < _keys_strs.Length; i++)
                    _keys_strs[i] = fbb.CreateString(keys[i] ?? "");
                keys_off = CreateStringStorageRequest.CreateKeysVector(fbb, _keys_strs);
            }
            var file_path_off = !string.IsNullOrEmpty(file_path) ? fbb.CreateString(file_path) : default(StringOffset);
            var body = CreateStringStorageRequest.CreateCreateStringStorageRequest(fbb, request_id, name_off, kind, strings_off, keys_off, file_path_off);
            return WrapMessage(fbb, body, MessageBody.CreateStringStorageRequest);
        }

        internal static byte[] BuildDestroyStringStorageRequest(ulong request_id, string name)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var body = DestroyStringStorageRequest.CreateDestroyStringStorageRequest(fbb, request_id, name_off);
            return WrapMessage(fbb, body, MessageBody.DestroyStringStorageRequest);
        }

        internal static byte[] BuildLoadModelRequest(ulong request_id, string model_name, MemoryPlacement placement, bool placement_is_override)
        {
            var fbb = new FlatBufferBuilder(256);
            var model_name_off = !string.IsNullOrEmpty(model_name) ? fbb.CreateString(model_name) : default(StringOffset);
            var body = LoadModelRequest.CreateLoadModelRequest(fbb, request_id, model_name_off, placement, placement_is_override);
            return WrapMessage(fbb, body, MessageBody.LoadModelRequest);
        }

        internal static byte[] BuildUnloadModelRequest(ulong request_id, string model_name)
        {
            var fbb = new FlatBufferBuilder(256);
            var model_name_off = !string.IsNullOrEmpty(model_name) ? fbb.CreateString(model_name) : default(StringOffset);
            var body = UnloadModelRequest.CreateUnloadModelRequest(fbb, request_id, model_name_off);
            return WrapMessage(fbb, body, MessageBody.UnloadModelRequest);
        }

        internal static byte[] BuildCreateEmbeddedStringStorageRequest(ulong request_id, string name, string config_path, IReadOnlyList<string> strings, string embedding_model)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var config_path_off = !string.IsNullOrEmpty(config_path) ? fbb.CreateString(config_path) : default(StringOffset);
            VectorOffset strings_off = default;
            if (strings != null && strings.Count > 0)
            {
                var _strings_strs = new StringOffset[strings.Count];
                for (int i = 0; i < _strings_strs.Length; i++)
                    _strings_strs[i] = fbb.CreateString(strings[i] ?? "");
                strings_off = CreateEmbeddedStringStorageRequest.CreateStringsVector(fbb, _strings_strs);
            }
            var embedding_model_off = !string.IsNullOrEmpty(embedding_model) ? fbb.CreateString(embedding_model) : default(StringOffset);
            var body = CreateEmbeddedStringStorageRequest.CreateCreateEmbeddedStringStorageRequest(fbb, request_id, name_off, config_path_off, strings_off, embedding_model_off);
            return WrapMessage(fbb, body, MessageBody.CreateEmbeddedStringStorageRequest);
        }

        internal static byte[] BuildDestroyEmbeddedStringStorageRequest(ulong request_id, string name)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var body = DestroyEmbeddedStringStorageRequest.CreateDestroyEmbeddedStringStorageRequest(fbb, request_id, name_off);
            return WrapMessage(fbb, body, MessageBody.DestroyEmbeddedStringStorageRequest);
        }

        internal static byte[] BuildCreateVoiceInputRequest(ulong request_id, string model_name, WireAudioFormatT input_format, float vad_threshold, uint vad_min_silence_ms, uint vad_speech_pad_ms, string hotwords_storage_path, float hotwords_score)
        {
            var fbb = new FlatBufferBuilder(256);
            var model_name_off = !string.IsNullOrEmpty(model_name) ? fbb.CreateString(model_name) : default(StringOffset);
            var input_format_off = WireAudioFormat.Pack(fbb, input_format);
            var hotwords_storage_path_off = !string.IsNullOrEmpty(hotwords_storage_path) ? fbb.CreateString(hotwords_storage_path) : default(StringOffset);
            var body = CreateVoiceInputRequest.CreateCreateVoiceInputRequest(fbb, request_id, model_name_off, input_format_off, vad_threshold, vad_min_silence_ms, vad_speech_pad_ms, hotwords_storage_path_off, hotwords_score);
            return WrapMessage(fbb, body, MessageBody.CreateVoiceInputRequest);
        }

        internal static byte[] BuildBeginUtteranceRequest(ulong request_id, ulong voice_input_id, bool auto_finish_on_silence, uint max_utterance_ms)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = BeginUtteranceRequest.CreateBeginUtteranceRequest(fbb, request_id, voice_input_id, auto_finish_on_silence, max_utterance_ms);
            return WrapMessage(fbb, body, MessageBody.BeginUtteranceRequest);
        }

        internal static byte[] BuildEndUtteranceRequest(ulong request_id, ulong voice_input_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = EndUtteranceRequest.CreateEndUtteranceRequest(fbb, request_id, voice_input_id);
            return WrapMessage(fbb, body, MessageBody.EndUtteranceRequest);
        }

        internal static byte[] BuildCancelUtteranceRequest(ulong request_id, ulong voice_input_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = CancelUtteranceRequest.CreateCancelUtteranceRequest(fbb, request_id, voice_input_id);
            return WrapMessage(fbb, body, MessageBody.CancelUtteranceRequest);
        }

        internal static byte[] BuildDestroyVoiceInputRequest(ulong request_id, ulong voice_input_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = DestroyVoiceInputRequest.CreateDestroyVoiceInputRequest(fbb, request_id, voice_input_id);
            return WrapMessage(fbb, body, MessageBody.DestroyVoiceInputRequest);
        }

        internal static byte[] BuildDeleteModelRequest(ulong request_id, string model_name)
        {
            var fbb = new FlatBufferBuilder(256);
            var model_name_off = !string.IsNullOrEmpty(model_name) ? fbb.CreateString(model_name) : default(StringOffset);
            var body = DeleteModelRequest.CreateDeleteModelRequest(fbb, request_id, model_name_off);
            return WrapMessage(fbb, body, MessageBody.DeleteModelRequest);
        }

        internal static byte[] BuildDebugCommandRequest(ulong request_id, string command, string payload)
        {
            var fbb = new FlatBufferBuilder(256);
            var command_off = !string.IsNullOrEmpty(command) ? fbb.CreateString(command) : default(StringOffset);
            var payload_off = !string.IsNullOrEmpty(payload) ? fbb.CreateString(payload) : default(StringOffset);
            var body = DebugCommandRequest.CreateDebugCommandRequest(fbb, request_id, command_off, payload_off);
            return WrapMessage(fbb, body, MessageBody.DebugCommandRequest);
        }

        internal static byte[] BuildCancelRequest(ulong request_id, ulong agent_id, CancelMode mode)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = CancelRequest.CreateCancelRequest(fbb, request_id, agent_id, mode);
            return WrapMessage(fbb, body, MessageBody.CancelRequest);
        }

        internal static byte[] BuildGetAgentVariablesRequest(ulong request_id, ulong agent_id, IReadOnlyList<string> names)
        {
            var fbb = new FlatBufferBuilder(256);
            VectorOffset names_off = default;
            if (names != null && names.Count > 0)
            {
                var _names_strs = new StringOffset[names.Count];
                for (int i = 0; i < _names_strs.Length; i++)
                    _names_strs[i] = fbb.CreateString(names[i] ?? "");
                names_off = GetAgentVariablesRequest.CreateNamesVector(fbb, _names_strs);
            }
            var body = GetAgentVariablesRequest.CreateGetAgentVariablesRequest(fbb, request_id, agent_id, names_off);
            return WrapMessage(fbb, body, MessageBody.GetAgentVariablesRequest);
        }

        internal static byte[] BuildPrefillAgentKvCacheRequest(ulong request_id, ulong agent_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = PrefillAgentKvCacheRequest.CreatePrefillAgentKvCacheRequest(fbb, request_id, agent_id);
            return WrapMessage(fbb, body, MessageBody.PrefillAgentKvCacheRequest);
        }

        internal static byte[] BuildEvictAgentKvCacheRequest(ulong request_id, ulong agent_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = EvictAgentKvCacheRequest.CreateEvictAgentKvCacheRequest(fbb, request_id, agent_id);
            return WrapMessage(fbb, body, MessageBody.EvictAgentKvCacheRequest);
        }

        internal static byte[] BuildGetAgentKvCacheStatusRequest(ulong request_id, ulong agent_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = GetAgentKvCacheStatusRequest.CreateGetAgentKvCacheStatusRequest(fbb, request_id, agent_id);
            return WrapMessage(fbb, body, MessageBody.GetAgentKvCacheStatusRequest);
        }

        internal static byte[] BuildRemoveInteractionsFromEndRequest(ulong request_id, ulong agent_id, uint count)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = RemoveInteractionsFromEndRequest.CreateRemoveInteractionsFromEndRequest(fbb, request_id, agent_id, count);
            return WrapMessage(fbb, body, MessageBody.RemoveInteractionsFromEndRequest);
        }

        internal static byte[] BuildSetInferenceThrottle(float throttle)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = SetInferenceThrottle.CreateSetInferenceThrottle(fbb, throttle);
            return WrapMessage(fbb, body, MessageBody.SetInferenceThrottle);
        }

        internal static byte[] BuildSetSessionLocaleRequest(ulong request_id, string locale)
        {
            var fbb = new FlatBufferBuilder(256);
            var locale_off = !string.IsNullOrEmpty(locale) ? fbb.CreateString(locale) : default(StringOffset);
            var body = SetSessionLocaleRequest.CreateSetSessionLocaleRequest(fbb, request_id, locale_off);
            return WrapMessage(fbb, body, MessageBody.SetSessionLocaleRequest);
        }

        internal static byte[] BuildExportDialogRequest(ulong request_id, ulong agent_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = ExportDialogRequest.CreateExportDialogRequest(fbb, request_id, agent_id);
            return WrapMessage(fbb, body, MessageBody.ExportDialogRequest);
        }

        internal static byte[] BuildGetMemoryConsumptionRequest(ulong request_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = GetMemoryConsumptionRequest.CreateGetMemoryConsumptionRequest(fbb, request_id);
            return WrapMessage(fbb, body, MessageBody.GetMemoryConsumptionRequest);
        }

        internal static byte[] BuildEnsureMutableKbRequest(ulong request_id, string name, string schema)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var schema_off = !string.IsNullOrEmpty(schema) ? fbb.CreateString(schema) : default(StringOffset);
            var body = EnsureMutableKbRequest.CreateEnsureMutableKbRequest(fbb, request_id, name_off, schema_off);
            return WrapMessage(fbb, body, MessageBody.EnsureMutableKbRequest);
        }

        internal static byte[] BuildEnumerateMutableKbsRequest(ulong request_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var body = EnumerateMutableKbsRequest.CreateEnumerateMutableKbsRequest(fbb, request_id);
            return WrapMessage(fbb, body, MessageBody.EnumerateMutableKbsRequest);
        }

        internal static byte[] BuildMutableKbWriteRequest(ulong request_id, string name, string schema, IReadOnlyList<MutableKbUpsertT> records)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var schema_off = !string.IsNullOrEmpty(schema) ? fbb.CreateString(schema) : default(StringOffset);
            VectorOffset records_off = default;
            if (records != null && records.Count > 0)
            {
                var _records_offs = new Offset<MutableKbUpsert>[records.Count];
                for (int i = 0; i < _records_offs.Length; i++)
                    _records_offs[i] = MutableKbUpsert.Pack(fbb, records[i]);
                records_off = MutableKbWriteRequest.CreateRecordsVector(fbb, _records_offs);
            }
            var body = MutableKbWriteRequest.CreateMutableKbWriteRequest(fbb, request_id, name_off, schema_off, records_off);
            return WrapMessage(fbb, body, MessageBody.MutableKbWriteRequest);
        }

        internal static byte[] BuildMutableKbDeleteRequest(ulong request_id, string name, IReadOnlyList<string> ids)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            VectorOffset ids_off = default;
            if (ids != null && ids.Count > 0)
            {
                var _ids_strs = new StringOffset[ids.Count];
                for (int i = 0; i < _ids_strs.Length; i++)
                    _ids_strs[i] = fbb.CreateString(ids[i] ?? "");
                ids_off = MutableKbDeleteRequest.CreateIdsVector(fbb, _ids_strs);
            }
            var body = MutableKbDeleteRequest.CreateMutableKbDeleteRequest(fbb, request_id, name_off, ids_off);
            return WrapMessage(fbb, body, MessageBody.MutableKbDeleteRequest);
        }

        internal static byte[] BuildMutableKbClearRequest(ulong request_id, string name)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var body = MutableKbClearRequest.CreateMutableKbClearRequest(fbb, request_id, name_off);
            return WrapMessage(fbb, body, MessageBody.MutableKbClearRequest);
        }

        internal static byte[] BuildMutableKbQueryRequest(ulong request_id, string name, string query_text, uint top_k, RetrievalMode retrieval_mode)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var query_text_off = !string.IsNullOrEmpty(query_text) ? fbb.CreateString(query_text) : default(StringOffset);
            var body = MutableKbQueryRequest.CreateMutableKbQueryRequest(fbb, request_id, name_off, query_text_off, top_k, retrieval_mode);
            return WrapMessage(fbb, body, MessageBody.MutableKbQueryRequest);
        }

        internal static byte[] BuildMutableKbGetRequest(ulong request_id, string name, string id)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var id_off = !string.IsNullOrEmpty(id) ? fbb.CreateString(id) : default(StringOffset);
            var body = MutableKbGetRequest.CreateMutableKbGetRequest(fbb, request_id, name_off, id_off);
            return WrapMessage(fbb, body, MessageBody.MutableKbGetRequest);
        }

        internal static byte[] BuildMutableKbListRequest(ulong request_id, string name, uint offset, uint limit)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var body = MutableKbListRequest.CreateMutableKbListRequest(fbb, request_id, name_off, offset, limit);
            return WrapMessage(fbb, body, MessageBody.MutableKbListRequest);
        }

        internal static byte[] BuildMutableKbStatsRequest(ulong request_id, string name)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var body = MutableKbStatsRequest.CreateMutableKbStatsRequest(fbb, request_id, name_off);
            return WrapMessage(fbb, body, MessageBody.MutableKbStatsRequest);
        }

        internal static byte[] BuildExportMutableKbRequest(ulong request_id, string name)
        {
            var fbb = new FlatBufferBuilder(256);
            var name_off = !string.IsNullOrEmpty(name) ? fbb.CreateString(name) : default(StringOffset);
            var body = ExportMutableKbRequest.CreateExportMutableKbRequest(fbb, request_id, name_off);
            return WrapMessage(fbb, body, MessageBody.ExportMutableKbRequest);
        }

        internal static byte[] BuildSaveGameMemoryRequest(ulong request_id, string path, string save_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var path_off = !string.IsNullOrEmpty(path) ? fbb.CreateString(path) : default(StringOffset);
            var save_id_off = !string.IsNullOrEmpty(save_id) ? fbb.CreateString(save_id) : default(StringOffset);
            var body = SaveGameMemoryRequest.CreateSaveGameMemoryRequest(fbb, request_id, path_off, save_id_off);
            return WrapMessage(fbb, body, MessageBody.SaveGameMemoryRequest);
        }

        internal static byte[] BuildLoadGameMemoryRequest(ulong request_id, string path, MutableKbCompatibility compatibility, MutableKbEmbedTiming embed_timing)
        {
            var fbb = new FlatBufferBuilder(256);
            var path_off = !string.IsNullOrEmpty(path) ? fbb.CreateString(path) : default(StringOffset);
            var body = LoadGameMemoryRequest.CreateLoadGameMemoryRequest(fbb, request_id, path_off, compatibility, embed_timing);
            return WrapMessage(fbb, body, MessageBody.LoadGameMemoryRequest);
        }

        internal static byte[] BuildSaveDialogRequest(ulong request_id, ulong agent_id, string path, string save_id)
        {
            var fbb = new FlatBufferBuilder(256);
            var path_off = !string.IsNullOrEmpty(path) ? fbb.CreateString(path) : default(StringOffset);
            var save_id_off = !string.IsNullOrEmpty(save_id) ? fbb.CreateString(save_id) : default(StringOffset);
            var body = SaveDialogRequest.CreateSaveDialogRequest(fbb, request_id, agent_id, path_off, save_id_off);
            return WrapMessage(fbb, body, MessageBody.SaveDialogRequest);
        }

        internal static byte[] BuildLoadDialogRequest(ulong request_id, ulong agent_id, string path, DialogCompatibility compatibility)
        {
            var fbb = new FlatBufferBuilder(256);
            var path_off = !string.IsNullOrEmpty(path) ? fbb.CreateString(path) : default(StringOffset);
            var body = LoadDialogRequest.CreateLoadDialogRequest(fbb, request_id, agent_id, path_off, compatibility);
            return WrapMessage(fbb, body, MessageBody.LoadDialogRequest);
        }

        internal static byte[] BuildSetAgentVoiceRequest(ulong request_id, ulong agent_id, VoiceProfileT voice)
        {
            var fbb = new FlatBufferBuilder(256);
            var voice_off = VoiceProfile.Pack(fbb, voice);
            var body = SetAgentVoiceRequest.CreateSetAgentVoiceRequest(fbb, request_id, agent_id, voice_off);
            return WrapMessage(fbb, body, MessageBody.SetAgentVoiceRequest);
        }

        internal static byte[] BuildSetAgentDeliveryModeRequest(ulong request_id, ulong agent_id, string mode, float intensity)
        {
            var fbb = new FlatBufferBuilder(256);
            var mode_off = !string.IsNullOrEmpty(mode) ? fbb.CreateString(mode) : default(StringOffset);
            var body = SetAgentDeliveryModeRequest.CreateSetAgentDeliveryModeRequest(fbb, request_id, agent_id, mode_off, intensity);
            return WrapMessage(fbb, body, MessageBody.SetAgentDeliveryModeRequest);
        }

    }
}
