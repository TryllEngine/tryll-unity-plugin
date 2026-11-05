// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.
//
// Hand-written portion of ClientCodec — codec:"manual" carve-outs, dual-mode
// convenience wrappers, and inbound OnIncoming* field mappers.
// Generated Build... functions live in Runtime/Generated/Internal/ClientCodec.gen.cs.
// Generated Decode() dispatch switch lives in ClientCodec.Dispatch.gen.cs.
//
// This file is intentionally declared as `partial` so the compiler merges it
// with the generated partial class at compile time.

using System;
using System.Buffers;
using System.Collections.Generic;
using Google.FlatBuffers;

// The flatc --csharp output lives in the Tryll namespace.
using Tryll;
using NodeParamsUnion = Tryll.NodeParams.NodeParams;
using ClientVisemeEvent = Tryll.Client.TryllVisemeEvent;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// FlatBuffers encode/decode helpers. Called only from <see cref="TryllSocketWorker"/>.
    /// All methods are pure functions — no state, no allocation of Unity objects.
    /// </summary>
    internal static partial class ClientCodec
    {
        // ── codec:"manual" carve-outs ─────────────────────────────────────────

        public static byte[] BuildCreateAgentRequest(
            ulong requestId,
            TryllGraphDescription graph,
            bool enableDiagnostics,
            bool maintainDialogueHistory,
            List<TryllVariableDecl> variables = null,
            AgentKvCacheInitialization kvCacheInitialization = AgentKvCacheInitialization.AllocateOnly,
            AgentWorkload workload = AgentWorkload.Interactive,
            VoiceProfileT voice = null)
        {
            var fbb = new FlatBufferBuilder(1024);

            // Build nodes vector — each node carries a typed NodeParams union.
            var nodeOffsets = new Offset<NodeDescription>[graph.Nodes?.Count ?? 0];
            for (int ni = 0; ni < nodeOffsets.Length; ni++)
            {
                var n = graph.Nodes[ni];
                if (n.Params == null)
                    throw new ArgumentException(
                        $"Node '{n.Name}' has null Params. Every node must carry typed params.");

                // All strings must be created before starting the table.
                var nameOff = fbb.CreateString(n.Name ?? "");

                // Serialise the typed params union.
                var (paramsType, paramsOffset) = n.Params.Pack(fbb);

                NodeDescription.StartNodeDescription(fbb);
                NodeDescription.AddName(fbb, nameOff);
                NodeDescription.AddParamsType(fbb, (NodeParamsUnion)paramsType);
                NodeDescription.AddParams(fbb, paramsOffset);
                nodeOffsets[ni] = NodeDescription.EndNodeDescription(fbb);
            }

            // Graph wiring lives on typed node params (each [TryllExit]-annotated field
            // carries its target).  No separate routes vector.

            var nodesVec  = GraphDescription.CreateNodesVector(fbb, nodeOffsets);
            var startOff  = fbb.CreateString(graph.StartNode ?? "");
            var graphOff  = GraphDescription.CreateGraphDescription(fbb, nodesVec, startOff);

            StringOffset defaultModelOff = default;
            if (!string.IsNullOrEmpty(graph.DefaultModelName))
                defaultModelOff = fbb.CreateString(graph.DefaultModelName);

            // Declared per-agent variables — complete declaration, sent once.
            var variableOffsetList = new List<Offset<AgentVariable>>(variables?.Count ?? 0);
            if (variables != null)
            {
                foreach (var d in variables)
                {
                    if (d == null || string.IsNullOrEmpty(d.Name)) continue;
                    variableOffsetList.Add(AgentVariable.Pack(fbb, new AgentVariableT
                    {
                        Name                    = d.Name,
                        Value                   = d.ToVariableValueUnion(),
                        AllowOutputSubstitution = d.AllowOutputSubstitution,
                    }));
                }
            }
            var variablesVec = CreateAgentRequest.CreateVariablesVector(fbb, variableOffsetList.ToArray());

            // The agent's voice (protocol v27). Absent = no voice; the server
            // rejects a speaking graph without one (AgentVoiceRequired, 3025).
            var voiceOff = voice != null ? VoiceProfile.Pack(fbb, voice) : default(Offset<VoiceProfile>);

            CreateAgentRequest.StartCreateAgentRequest(fbb);
            CreateAgentRequest.AddRequestId(fbb, requestId);
            CreateAgentRequest.AddGraph(fbb, graphOff);
            if (defaultModelOff.Value != 0)
                CreateAgentRequest.AddDefaultModelName(fbb, defaultModelOff);
            CreateAgentRequest.AddEnableDiagnostics(fbb, enableDiagnostics);
            CreateAgentRequest.AddMaintainDialogueHistory(fbb, maintainDialogueHistory);
            CreateAgentRequest.AddVariables(fbb, variablesVec);
            CreateAgentRequest.AddKvCacheInitialization(fbb, kvCacheInitialization);
            CreateAgentRequest.AddWorkload(fbb, workload);
            if (voiceOff.Value != 0)
                CreateAgentRequest.AddVoice(fbb, voiceOff);
            var req = CreateAgentRequest.EndCreateAgentRequest(fbb);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.CreateAgentRequest, req.Value));
        }

        /// <summary>
        /// Serialise a batched per-agent Variables mutation (deferred-flush from
        /// <see cref="TryllAgentVariables.FlushIfDirty"/>) into an
        /// <c>UpdateAgentVariablesRequest</c> FlatBuffer.
        /// </summary>
        public static byte[] BuildUpdateAgentVariablesRequest(
            ulong requestId, ulong agentId, List<VariableUpdateT> updates)
        {
            var fbb = new FlatBufferBuilder(256);
            updates ??= new List<VariableUpdateT>();

            var updateOffsets = new Offset<VariableUpdate>[updates.Count];
            for (int i = 0; i < updates.Count; i++)
                updateOffsets[i] = VariableUpdate.Pack(fbb, updates[i]);
            var updatesVec = UpdateAgentVariablesRequest.CreateUpdatesVector(fbb, updateOffsets);

            UpdateAgentVariablesRequest.StartUpdateAgentVariablesRequest(fbb);
            UpdateAgentVariablesRequest.AddRequestId(fbb, requestId);
            UpdateAgentVariablesRequest.AddAgentId(fbb, agentId);
            UpdateAgentVariablesRequest.AddUpdates(fbb, updatesVec);
            var req = UpdateAgentVariablesRequest.EndUpdateAgentVariablesRequest(fbb);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.UpdateAgentVariablesRequest, req.Value));
        }

        /// <summary>
        /// Serialise ResumeAgentRequest with an optional nested ToolResult batch.
        /// </summary>
        public static byte[] BuildResumeAgentRequest(
            ulong requestId, ulong agentId, string resumeNode,
            List<ToolResultT> toolResults = null)
        {
            var fbb = new FlatBufferBuilder(512);
            toolResults ??= new List<ToolResultT>();

            var resultOffsets = new Offset<ToolResult>[toolResults.Count];
            for (int i = 0; i < toolResults.Count; i++)
                resultOffsets[i] = ToolResult.Pack(fbb, toolResults[i]);

            var resultsVec = ResumeAgentRequest.CreateToolResultsVector(fbb, resultOffsets);
            StringOffset resumeNodeOff = default;
            if (!string.IsNullOrEmpty(resumeNode))
                resumeNodeOff = fbb.CreateString(resumeNode);

            ResumeAgentRequest.StartResumeAgentRequest(fbb);
            ResumeAgentRequest.AddRequestId(fbb, requestId);
            ResumeAgentRequest.AddAgentId(fbb, agentId);
            if (resumeNodeOff.Value != 0)
                ResumeAgentRequest.AddResumeNode(fbb, resumeNodeOff);
            ResumeAgentRequest.AddToolResults(fbb, resultsVec);
            var req = ResumeAgentRequest.EndResumeAgentRequest(fbb);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.ResumeAgentRequest, req.Value));
        }

        /// <summary>
        /// Serialise an <c>AppendInteractionsRequest</c> FlatBuffer (manual: nested
        /// <c>[DialogInteraction]</c> vector).
        /// </summary>
        public static byte[] BuildAppendInteractionsRequest(
            ulong requestId, ulong agentId, List<TryllDialogInteraction> interactions)
        {
            var fbb = new FlatBufferBuilder(512);
            interactions ??= new List<TryllDialogInteraction>();

            var pairOffsets = new Offset<DialogInteraction>[interactions.Count];
            for (int i = 0; i < interactions.Count; i++)
            {
                var pair = interactions[i];
                StringOffset userOff = default;
                StringOffset assistantOff = default;
                if (!string.IsNullOrEmpty(pair.UserMessage))
                    userOff = fbb.CreateString(pair.UserMessage);
                if (!string.IsNullOrEmpty(pair.AssistantMessage))
                    assistantOff = fbb.CreateString(pair.AssistantMessage);
                pairOffsets[i] = DialogInteraction.CreateDialogInteraction(fbb, userOff, assistantOff);
            }

            var interactionsVec = AppendInteractionsRequest.CreateInteractionsVector(fbb, pairOffsets);
            AppendInteractionsRequest.StartAppendInteractionsRequest(fbb);
            AppendInteractionsRequest.AddRequestId(fbb, requestId);
            AppendInteractionsRequest.AddAgentId(fbb, agentId);
            AppendInteractionsRequest.AddInteractions(fbb, interactionsVec);
            var req = AppendInteractionsRequest.EndAppendInteractionsRequest(fbb);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.AppendInteractionsRequest, req.Value));
        }

        public static byte[] BuildImportDialogRequest(
            ulong requestId, ulong agentId, TryllDialogSnapshot snapshot, TryllDialogCompatibility compatibility)
        {
            var fbb = new FlatBufferBuilder(1024);
            var snapshotOff = TryllDialogSnapshot.Pack(fbb, snapshot);
            ImportDialogRequest.StartImportDialogRequest(fbb);
            ImportDialogRequest.AddRequestId(fbb, requestId);
            ImportDialogRequest.AddAgentId(fbb, agentId);
            ImportDialogRequest.AddSnapshot(fbb, snapshotOff);
            ImportDialogRequest.AddCompatibility(fbb, (DialogCompatibility)compatibility);
            var req = ImportDialogRequest.EndImportDialogRequest(fbb);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.ImportDialogRequest, req.Value));
        }

        public static byte[] BuildImportMutableKbRequest(
            ulong requestId, TryllMutableKbSnapshot snapshot, TryllMutableKbCompatibility compatibility)
        {
            var fbb = new FlatBufferBuilder(1024);
            var snapshotOff = TryllMutableKbSnapshot.Pack(fbb, snapshot);
            ImportMutableKbRequest.StartImportMutableKbRequest(fbb);
            ImportMutableKbRequest.AddRequestId(fbb, requestId);
            ImportMutableKbRequest.AddSnapshot(fbb, snapshotOff);
            ImportMutableKbRequest.AddCompatibility(fbb, (MutableKbCompatibility)compatibility);
            var req = ImportMutableKbRequest.EndImportMutableKbRequest(fbb);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.ImportMutableKbRequest, req.Value));
        }

        internal static List<MutableKbUpsertT> ToMutableKbUpsertT(List<TryllMutableKbUpsert> records)
        {
            var wire = new List<MutableKbUpsertT>(records?.Count ?? 0);
            if (records == null) return wire;
            foreach (var rec in records)
            {
                wire.Add(new MutableKbUpsertT
                {
                    Id = rec?.Id ?? "",
                    Text = rec?.Text ?? "",
                });
            }
            return wire;
        }

        /// <summary>
        /// Serialise a typed <see cref="TryllNodeParamsBase"/> into a <c>ChangeAgentParamRequest</c>
        /// FlatBuffer using the NodeParams union.  Replaces the old string key/value variant.
        /// </summary>
        public static byte[] BuildChangeAgentParamsRequest(
            ulong requestId, ulong agentId,
            string nodeName, TryllNodeParamsBase @params)
        {
            if (@params == null)
                throw new ArgumentNullException(nameof(@params),
                    "ChangeParams requires typed params (the wire union has no NONE slot for mutations).");

            var fbb     = new FlatBufferBuilder(256);
            var nodeOff = fbb.CreateString(nodeName ?? "");

            var (paramsType, paramsOffset) = @params.Pack(fbb);

            ChangeAgentParamRequest.StartChangeAgentParamRequest(fbb);
            ChangeAgentParamRequest.AddRequestId(fbb, requestId);
            ChangeAgentParamRequest.AddAgentId(fbb, agentId);
            ChangeAgentParamRequest.AddNodeName(fbb, nodeOff);
            ChangeAgentParamRequest.AddParamsType(fbb, (NodeParamsUnion)paramsType);
            ChangeAgentParamRequest.AddParams(fbb, paramsOffset);
            var req = ChangeAgentParamRequest.EndChangeAgentParamRequest(fbb);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.ChangeAgentParamRequest, req.Value));
        }

        public static byte[] BuildAudioBuffer(ulong voiceInputId, byte[] pcm)
        {
            var fbb    = new FlatBufferBuilder(pcm != null ? pcm.Length + 64 : 64);
            var pcmOff = AudioBuffer.CreatePcmVectorBlock(fbb, pcm ?? Array.Empty<byte>());
            var buf    = AudioBuffer.CreateAudioBuffer(fbb, voiceInputId, pcmOff);
            return Finish(fbb, Message.CreateMessage(fbb, MessageBody.AudioBuffer, buf.Value));
        }

        // ── Dual-mode convenience wrappers ────────────────────────────────────
        // These call the generated Build... with only the fields relevant to each
        // usage path, keeping TryllSocketWorker call sites unchanged.

        public static byte[] BuildCreateStringStorageRequest(ulong requestId, string name, List<string> strings)
        {
            return BuildCreateStringStorageRequest(
                requestId, name, StringStorageKind.List,
                strings, null, null);
        }

        public static byte[] BuildCreateKeyedStringStorageRequest(
            ulong requestId, string name,
            List<string> keys, List<string> values,
            StringStorageKind kind = StringStorageKind.Map)
        {
            return BuildCreateStringStorageRequest(
                requestId, name, kind,
                values, keys, null);
        }

        public static byte[] BuildCreateStringStorageFromFileRequest(ulong requestId, string name, string filePath)
        {
            return BuildCreateStringStorageRequest(
                requestId, name, StringStorageKind.List,
                null, null, filePath);
        }

        public static byte[] BuildCreateStringStorageFromFileRequest(
            ulong requestId, string name, string filePath,
            StringStorageKind kind)
        {
            return BuildCreateStringStorageRequest(
                requestId, name, kind,
                null, null, filePath);
        }

        public static byte[] BuildCreateEmbeddedStringStorageRequest(
            ulong requestId, string name, string configPath, string embeddingModel)
        {
            return BuildCreateEmbeddedStringStorageRequest(
                requestId, name, configPath, null, embeddingModel);
        }

        public static byte[] BuildCreateEmbeddedStringStorageFromStringsRequest(
            ulong requestId, string name, List<string> strings, string embeddingModel)
        {
            return BuildCreateEmbeddedStringStorageRequest(
                requestId, name, null, strings, embeddingModel);
        }

        /// <summary>
        /// Convenience overload: takes flat audio-format scalars instead of a
        /// <c>WireAudioFormatT</c> POCO, matching the TryllSocketWorker call site.
        /// </summary>
        public static byte[] BuildCreateVoiceInputRequest(
            ulong requestId, string modelName,
            uint sampleRate, ushort channels, ushort bitsPerSample,
            float vadThreshold, uint vadMinSilenceMs, uint vadSpeechPadMs,
            string hotwordsStoragePath = "", float hotwordsScore = 1.5f)
        {
            var fmt = new WireAudioFormatT
            {
                SampleRate    = sampleRate,
                Channels      = channels,
                BitsPerSample = bitsPerSample,
            };
            return BuildCreateVoiceInputRequest(
                requestId, modelName, fmt,
                vadThreshold, vadMinSilenceMs, vadSpeechPadMs,
                hotwordsStoragePath, hotwordsScore);
        }

        // ── Inbound decode handlers (called from generated Decode switch) ─────

        private static TryllConnEvent OnIncomingConnectionReady(Message msg)
        {
            // Connection-level hello (protocol v4): protocol_version +
            // codegen_fingerprint only.  No session_id — that arrives later in
            // CreateSessionResponse.
            var hello = msg.Body<ConnectionReady>();
            if (hello == null) return null;
            if (hello.Value.ProtocolVersion != Tryll.Client.TryllProtocol.Version)
            {
                return new TryllConnEvent
                {
                    Type    = TryllConnEventType.ConnectFailed,
                    Message = $"Protocol version mismatch: server={hello.Value.ProtocolVersion}, client={Tryll.Client.TryllProtocol.Version}",
                };
            }
            var fp = hello.Value.CodegenFingerprint;
            if (!string.IsNullOrEmpty(fp) && fp != Tryll.Generated.CodegenFingerprintInfo.Value)
                UnityEngine.Debug.LogWarning(
                    $"[Tryll] Codegen fingerprint mismatch: server={fp}, client={Tryll.Generated.CodegenFingerprintInfo.Value}");
            var e = new TryllConnEvent { Type = TryllConnEventType.Connected };
            // Server identity from ConnectionReady — verified on the main thread in
            // TryllClient.  No session_id here; it arrives in CreateSessionResponse.
            e.ServerExePath            = hello.Value.ExePath;
            e.ServerBuildConfig        = hello.Value.BuildConfig;
            e.ServerMode               = hello.Value.Mode ?? string.Empty;
            e.ServerVersion            = hello.Value.Version;
            e.ServerRunId              = hello.Value.RunId;
            e.ServerManaged            = hello.Value.Managed;
            e.ServerIdleTimeoutSeconds = hello.Value.IdleTimeoutSeconds;
            return e;
        }













        internal static List<Tryll.Client.TryllModelInfo> ModelsFrom(ListModelsResponseT body)
        {
            var models = new List<Tryll.Client.TryllModelInfo>();
            if (body?.Models == null)
                return models;
            foreach (var mv in body.Models)
            {
                if (mv == null) continue;
                var info = new Tryll.Client.TryllModelInfo
                {
                    Name            = mv.Name,
                    Status          = (Tryll.Client.TryllModelStatus)(byte)mv.Status,
                    HuggingFaceRepo = mv.HuggingfaceRepo,
                    SizeBytes       = (long)mv.SizeBytes,
                    ModelType       = mv.ModelType,
                    Engine          = mv.Engine,
                    Audience        = mv.Audience,
                    ToolCallFormat  = mv.ToolCallFormat,
                    ToolCallSupport = mv.ToolCallSupport,
                    ContextSize     = mv.ContextSize,
                    Files           = mv.Files == null ? Array.Empty<string>() : mv.Files.ToArray(),
                    Languages       = mv.Languages == null ? Array.Empty<string>() : mv.Languages.ToArray(),
                    LocalPath       = mv.LocalPath,
                    Dependencies    = DecodeDependencies(mv),
                };
                if (mv.TtsCapabilities != null)
                {
                    info.HasTtsCapabilities = true;
                    info.TtsCapabilities = DecodeTtsCapabilities(mv.TtsCapabilities);
                }
                if (mv.DefaultSampling != null)
                {
                    var s = mv.DefaultSampling;
                    info.HasDefaultSampling = true;
                    info.DefaultSampling = new Tryll.Client.TryllSamplingParams
                    {
                        Temperature      = s.Temperature,
                        TopP             = s.TopP,
                        TopK             = s.TopK,
                        MinP             = s.MinP,
                        MaxTokens        = s.MaxTokens,
                        Seed             = s.Seed,
                        RepeatPenalty    = s.RepeatPenalty,
                        PresencePenalty  = s.PresencePenalty,
                        FrequencyPenalty = s.FrequencyPenalty,
                    };
                }
                models.Add(info);
            }
            return models;
        }

        private static Tryll.Client.TryllModelDependency[] DecodeDependencies(ModelInfoT mv)
        {
            if (mv.Dependencies == null)
                return Array.Empty<Tryll.Client.TryllModelDependency>();
            var deps = new Tryll.Client.TryllModelDependency[mv.Dependencies.Count];
            for (int i = 0; i < mv.Dependencies.Count; i++)
            {
                var dv = mv.Dependencies[i];
                if (dv == null) continue;
                deps[i] = new Tryll.Client.TryllModelDependency
                {
                    Id            = dv.Id,
                    Role          = dv.Role,
                    Status        = (Tryll.Client.TryllModelStatus)(byte)dv.Status,
                    SizeBytes     = (long)dv.SizeBytes,
                    Folder        = dv.Folder,
                    LocalPath     = dv.LocalPath,
                    FailureReason = dv.FailureReason,
                };
            }
            return deps;
        }

        private static Tryll.Client.TryllTtsCapabilities DecodeTtsCapabilities(TtsCapabilitiesInfoT caps)
        {
            var presets = caps.VoicePresets == null
                ? Array.Empty<Tryll.Client.TryllTtsVoicePreset>()
                : new Tryll.Client.TryllTtsVoicePreset[caps.VoicePresets.Count];
            if (caps.VoicePresets != null)
            {
                for (int i = 0; i < caps.VoicePresets.Count; i++)
                {
                    var pv = caps.VoicePresets[i];
                    if (pv == null) continue;
                    presets[i] = new Tryll.Client.TryllTtsVoicePreset
                    {
                        Id          = pv.Id,
                        Label       = pv.Label,
                        Description = pv.Description,
                    };
                }
            }
            var tags = caps.InlineTags == null
                ? Array.Empty<Tryll.Client.TryllTtsInlineTag>()
                : new Tryll.Client.TryllTtsInlineTag[caps.InlineTags.Count];
            if (caps.InlineTags != null)
            {
                for (int i = 0; i < caps.InlineTags.Count; i++)
                {
                    var tv = caps.InlineTags[i];
                    if (tv == null) continue;
                    tags[i] = new Tryll.Client.TryllTtsInlineTag
                    {
                        Token        = tv.Token,
                        Kind         = tv.Kind,
                        Verification = tv.Verification,
                    };
                }
            }
            return new Tryll.Client.TryllTtsCapabilities
            {
                VoiceDescription    = caps.VoiceDescription,
                DeliveryInstruction = caps.DeliveryInstruction,
                ReferenceVoice      = caps.ReferenceVoice,
                Speed               = caps.Speed,
                LanguagePerCall     = caps.LanguagePerCall,
                Expressiveness      = caps.Expressiveness,
                NumSpeakers         = caps.NumSpeakers,
                VoicePresets        = presets,
                InlineTags          = tags,
            };
        }

        private static TryllModelDependency[] DecodeDependencies(ModelInfo mv)
        {
            var deps = new TryllModelDependency[mv.DependenciesLength];
            for (int i = 0; i < mv.DependenciesLength; i++)
            {
                var d = mv.Dependencies(i);
                if (d == null) continue;
                var dv = d.Value;
                deps[i] = new TryllModelDependency
                {
                    Id             = dv.Id,
                    Role           = dv.Role,
                    Status         = (TryllModelStatus)(byte)dv.Status,
                    SizeBytes      = (long)dv.SizeBytes,
                    Folder         = dv.Folder,
                    LocalPath      = dv.LocalPath,
                    FailureReason  = dv.FailureReason,
                };
            }
            return deps;
        }

        private static TryllTtsCapabilities DecodeTtsCapabilities(TtsCapabilitiesInfo caps)
        {
            var presets = new TryllTtsVoicePreset[caps.VoicePresetsLength];
            for (int i = 0; i < caps.VoicePresetsLength; i++)
            {
                var p = caps.VoicePresets(i);
                if (p == null) continue;
                var pv = p.Value;
                presets[i] = new TryllTtsVoicePreset
                {
                    Id          = pv.Id,
                    Label       = pv.Label,
                    Description = pv.Description,
                };
            }
            var tags = new TryllTtsInlineTag[caps.InlineTagsLength];
            for (int i = 0; i < caps.InlineTagsLength; i++)
            {
                var t = caps.InlineTags(i);
                if (t == null) continue;
                var tv = t.Value;
                tags[i] = new TryllTtsInlineTag
                {
                    Token        = tv.Token,
                    Kind         = tv.Kind,
                    Verification = tv.Verification,
                };
            }
            return new TryllTtsCapabilities
            {
                VoiceDescription     = caps.VoiceDescription,
                DeliveryInstruction  = caps.DeliveryInstruction,
                ReferenceVoice       = caps.ReferenceVoice,
                Speed                = caps.Speed,
                LanguagePerCall      = caps.LanguagePerCall,
                Expressiveness       = caps.Expressiveness,
                NumSpeakers          = caps.NumSpeakers,
                VoicePresets         = presets,
                InlineTags           = tags,
            };
        }











        private static TryllConnEvent OnIncomingNodeEvent(Message msg)
        {
            var ev = msg.Body<NodeEvent>();
            if (ev == null) return null;
            var kvs = new List<KeyValuePair<string, string>>(ev.Value.KvPairsLength);
            for (int i = 0; i < ev.Value.KvPairsLength; ++i)
            {
                var p = ev.Value.KvPairs(i);
                if (p == null) continue;
                kvs.Add(new KeyValuePair<string, string>(p.Value.Key ?? string.Empty,
                                                         p.Value.Value ?? string.Empty));
            }
            return new TryllConnEvent
            {
                Type      = TryllConnEventType.NodeEvent,
                RequestId = (ulong)ev.Value.RequestId,
                AgentId   = (ulong)ev.Value.AgentId,
                Text      = ev.Value.NodeName,
                Message   = ev.Value.EventType,
                KvPairs   = kvs,
            };
        }



        private static TryllConnEvent OnIncomingWireTranscriptUpdate(Message msg)
        {
            var upd = msg.Body<WireTranscriptUpdate>();
            if (upd == null) return null;
            return new TryllConnEvent
            {
                Type            = TryllConnEventType.TranscriptUpdate,
                VoiceInputId    = (ulong)upd.Value.VoiceInputId,
                Text            = upd.Value.Text,
                TranscriptKind  = (byte)upd.Value.Kind,
                AudioMsConsumed = (uint)upd.Value.AudioMsConsumed,
            };
        }

        // TtsAudioFormatFrame / TtsAudioFrame are codec:"manual" Streaming frames;
        // the generated Decode switch routes them here for per-agent fan-out.
        private static TryllConnEvent OnIncomingTtsAudioFormatFrame(Message msg)
        {
            var f = msg.Body<TtsAudioFormatFrame>();
            if (f == null) return null;
            var fmt = f.Value.Format;
            if (fmt == null) return null;
            return new TryllConnEvent
            {
                Type             = TryllConnEventType.TtsAudioFormat,
                RequestId        = (ulong)f.Value.RequestId,
                AgentId          = (ulong)f.Value.AgentId,
                Code             = (uint)fmt.Value.SampleRate,
                TtsChannels      = (ushort)fmt.Value.Channels,
                TtsBitsPerSample = (ushort)fmt.Value.BitsPerSample,
                Message          = f.Value.NodeName,
            };
        }

        private static TryllConnEvent OnIncomingTtsAudioFrame(Message msg)
        {
            var f = msg.Body<TtsAudioFrame>();
            if (f == null) return null;
            int byteLen   = f.Value.PcmLength;
            int sampleLen = byteLen / 2;
            short[] buf = null;
            if (sampleLen > 0)
            {
                buf = ArrayPool<short>.Shared.Rent(sampleLen);
                // Decode int16 LE. Generated GetPcmBytes() returns ArraySegment<byte>?
                // referencing the live FlatBuffers backing array — safe to read here.
                var seg = f.Value.GetPcmBytes();
                if (seg.HasValue)
                {
                    var raw    = seg.Value.Array;
                    int offset = seg.Value.Offset;
                    for (int i = 0; i < sampleLen; i++)
                        buf[i] = (short)(raw[offset + i * 2] | (raw[offset + i * 2 + 1] << 8));
                }
            }
            return new TryllConnEvent
            {
                Type      = TryllConnEventType.TtsAudio,
                RequestId = (ulong)f.Value.RequestId,
                AgentId   = (ulong)f.Value.AgentId,
                PcmBuffer = buf,
                PcmLength = sampleLen,
                Message   = f.Value.NodeName,
            };
        }

        private static TryllConnEvent OnIncomingTtsVisemeFrame(Message msg)
        {
            var f = msg.Body<TtsVisemeFrame>();
            if (f == null) return null;
            var list = f.Value.Events;
            int n = list.HasValue ? list.Value.EventsLength : 0;
            var events = n > 0 ? new ClientVisemeEvent[n] : Array.Empty<ClientVisemeEvent>();
            for (int i = 0; i < n; i++)
            {
                var e = list.Value.Events(i);
                if (!e.HasValue) continue;
                events[i] = new ClientVisemeEvent(e.Value.SampleOffset, e.Value.Viseme, e.Value.Weight);
            }
            return new TryllConnEvent
            {
                Type         = TryllConnEventType.TtsViseme,
                RequestId    = (ulong)f.Value.RequestId,
                AgentId      = (ulong)f.Value.AgentId,
                Message      = f.Value.NodeName,
                VisemeEvents = events,
            };
        }

















































        private static List<string> ReadWarnings(int length, Func<int, string> read)
        {
            var warnings = new List<string>(length);
            for (int i = 0; i < length; i++)
                warnings.Add(read(i) ?? "");
            return warnings;
        }





        private static TryllConnEvent UnknownBody(MessageBody bt)
        {
            TryllLog.Warning($"ClientCodec: unexpected body_type={bt}");
            return null;
        }

        private static TryllConnEvent DecodeException(Exception ex)
        {
            TryllLog.Warning($"ClientCodec.Decode exception: {ex.Message}");
            return null;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static byte[] Finish(FlatBufferBuilder fbb, Offset<Message> root)
        {
            Message.FinishMessageBuffer(fbb, root);
            return fbb.SizedByteArray();
        }
    }
}
