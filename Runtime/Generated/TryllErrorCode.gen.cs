// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/common/include/tryll/ErrorCodes.h
// See: docs/dev/codegen.md

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllErrorCode_gen {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Numeric error codes sent by the server in ErrorResponse.
    /// Generated from tryll/common/include/tryll/ErrorCodes.h.
    /// </summary>
    public enum TryllErrorCode
    {
        /// <summary>No error. Success sentinel.</summary>
        Ok = 0,
        /// <summary>TCP connect failed outright.</summary>
        ConnectionFailed = 1001,
        /// <summary>Established connection was dropped mid-session (peer close or socket error).</summary>
        ConnectionDropped = 1002,
        /// <summary>Client-side timeout waiting for a response frame; the socket itself may still be healthy.</summary>
        Timeout = 1003,
        /// <summary>Server is shutting down and no longer accepts requests on this session.</summary>
        ServerShuttingDown = 2001,
        /// <summary>A non-CreateSession request arrived before CreateSession created the session.</summary>
        SessionNotReady = 2002,
        /// <summary>A second CreateSessionRequest arrived on a connection that already has a session.</summary>
        SessionAlreadyExists = 2003,
        /// <summary>CreateSessionRequest.locale is not a well-formed BCP-47 tag.</summary>
        InvalidLocale = 2004,
        /// <summary>An entry in CreateSessionRequest.model_config carries an ordinal that is not a valid ModelRole, InferenceEngine or MemoryPlacement value. This is the *only* remaining cause. The engine set is advisory (spec §7.1): it may be empty, incomplete, or name an engine the session never uses, and none of that is an error. What cannot be tolerated is an out-of-range ordinal, because the wire value is cast to an enum and a bad cast is undefined behaviour rather than a wrong answer.</summary>
        InvalidEngineSet = 2005,
        /// <summary>Request referenced an agent_id the server does not know (wrong id or already destroyed).</summary>
        InvalidAgentId = 3001,
        /// <summary>Agent was previously destroyed; the id is no longer valid.</summary>
        AgentAlreadyDestroyed = 3002,
        /// <summary>Server failed to compile the workflow graph into an executable plan.</summary>
        GraphCompilationFailed = 3003,
        /// <summary>Agent is still processing the previous turn and cannot accept a new message yet. This is the re-entrancy rejection.</summary>
        AgentBusy = 3004,
        /// <summary>node_name not found in the agent's graph.</summary>
        UnknownNode = 3005,
        /// <summary>param_name is not settable on that node type.</summary>
        ParamNotMutable = 3006,
        /// <summary>param_name is settable but value failed validation.</summary>
        InvalidParamValue = 3007,
        /// <summary>(exit) field on a node references a target node that doesn't exist.</summary>
        InvalidExitTarget = 3008,
        /// <summary>node storage path escapes storage_root (absolute or `..`).</summary>
        StorageOutsideRoot = 3009,
        /// <summary>node storage filename is neither a containable file nor a registered virtual name.</summary>
        StorageFileNotFound = 3010,
        /// <summary>resolved storage kind does not match the consuming node.</summary>
        StorageKindMismatch = 3011,
        /// <summary>Resume requested but the agent is not paused.</summary>
        AgentNotPaused = 3012,
        /// <summary>UpdateAgentVariables targets a name not declared at CreateAgent.</summary>
        UnknownVariable = 3013,
        /// <summary>Assign/AddToSet/RemoveFromSet value type does not match the declared variable type.</summary>
        VariableTypeMismatch = 3014,
        /// <summary>Declaration name/limit violation (CreateAgent.variables).</summary>
        InvalidVariableValue = 3015,
        /// <summary>Resume tool_results are invalid for the current paused tool-call batch.</summary>
        InvalidToolResults = 3016,
        /// <summary>A node relies on the session locale but its model does not declare that language.</summary>
        ModelLanguageUnsupported = 3017,
        /// <summary>Export/import snapshot is malformed, missing required fields, has an invalid enum, or has inconsistent tool call/result pairing.</summary>
        DialogSnapshotInvalid = 3018,
        /// <summary>PersistedDialogSnapshot.format_version is newer than this server, or the standalone buffer's file identifier is not "TDLG".</summary>
        DialogFormatUnsupported = 3019,
        /// <summary>Import Strict: snapshot.graph_signature does not match the agent's create-time graph hash.</summary>
        DialogGraphMismatch = 3020,
        /// <summary>Encoded snapshot would overflow MaxDialogSnapshotBytes / the 1 MiB frame.</summary>
        DialogSnapshotTooLarge = 3021,
        /// <summary>Export hit an IComponent type this snapshot format does not know, or import saw an unknown PersistedComponent union arm.</summary>
        DialogUnsupportedComponent = 3022,
        /// <summary>A VoiceProfile field fails validation or the loaded TTS model cannot use it (CreateAgent, SetAgentVoice). The message names the field and, for delivery modes, the mode.</summary>
        InvalidVoiceProfile = 3023,
        /// <summary>SetAgentDeliveryMode named a mode the agent's voice does not define.</summary>
        UnknownDeliveryMode = 3024,
        /// <summary>A graph with a speaking node (SpeakNode, GenerateAndSpeakNode) was created without a voice, or SetAgentDeliveryMode targeted an agent that has no voice.</summary>
        AgentVoiceRequired = 3025,
        /// <summary>A generation or workflow node raised a non-recoverable inference error.</summary>
        InferenceFailed = 4001,
        /// <summary>A generation or workflow node exceeded its per-node time budget.</summary>
        InferenceTimeout = 4002,
        /// <summary>Workflow exceeded the maximum number of node transitions per turn.</summary>
        MaxStepsExceeded = 4003,
        /// <summary>Explicit PrefillKvCache / create-with-Prefill failed (allocation, format, or decode).</summary>
        KvCacheOperationFailed = 4004,
        /// <summary>Selected inference backend cannot provide the requested KV-cache lifecycle op.</summary>
        KvCacheManagementUnsupported = 4005,
        /// <summary>Received frame could not be decoded against the FlatBuffers schema.</summary>
        MalformedMessage = 5001,
        /// <summary>Received frame carries a message type the server does not recognize.</summary>
        UnknownMessageType = 5002,
        /// <summary>Outgoing or incoming frame exceeds the negotiated maximum size.</summary>
        FrameTooLarge = 5003,
        /// <summary>Client and server speak incompatible versions of the wire protocol.</summary>
        ProtocolVersionMismatch = 5004,
        /// <summary>Debug-command channel not in this build (Production) or disabled in config.</summary>
        DebugCommandsUnavailable = 5005,
        /// <summary>DebugCommandRequest.command is not a recognised verb.</summary>
        UnknownDebugCommand = 5006,
        /// <summary>Model download transport failed (HTTP error, interrupted transfer, checksum mismatch).</summary>
        DownloadFailed = 6001,
        /// <summary>Model is not registered in the catalog, or has no HuggingFace repo/files configured.</summary>
        DownloadNotAvailable = 6002,
        /// <summary>Target disk does not have enough free space for the model.</summary>
        InsufficientDiskSpace = 6003,
        /// <summary>A download for this model is already in progress on the server.</summary>
        DownloadAlreadyActive = 6004,
        /// <summary>Model not in catalog or engine not registered.</summary>
        ModelResolutionFailed = 6005,
        /// <summary>RESERVED AND CURRENTLY UNUSED. Would report a model whose variant declares a hard device requirement the host cannot meet. No catalog model declares one, so the check is deliberately not built — the number is reserved so the taxonomy has a documented home for it, and so nobody assigns 6007 to something else. Client placement requests are *hints* and cannot error: they are downgraded and the resolved value is reported instead. See engine-selection-and-model-placement-spec.md section 7.4.</summary>
        ModelPlacementUnavailable = 6007,
        /// <summary>A referenced model needs an engine that is not registered in this server build. The message names the model, its role, and the engine it needs. This is a statement about the *server binary*, not about client bookkeeping. Through the first cut of protocol v13 the same number meant "your session did not declare this engine" (`EngineNotDeclared`), which put the blame on a client field that is now advisory — and named a cause the reader could not act on. The guard that belongs earlier is the build-time one (spec §7.10).</summary>
        EngineNotAvailable = 6008,
        /// <summary>This deployment mode does not download or delete models. Client Prod ships every model with the game and rejects both requests.</summary>
        ModelManagementDisabled = 6009,
        /// <summary>Requested StringStorage name is empty, malformed, or not found on destroy.</summary>
        InvalidStringStorageName = 7001,
        /// <summary>StringStorage with this name is already registered for the session.</summary>
        StringStorageAlreadyExists = 7002,
        /// <summary>StringStorage content was rejected (unreadable file, bad format, empty array, etc.).</summary>
        InvalidStringStorageData = 7003,
        /// <summary>Virtual (inline) name collides with a real path or an existing entry.</summary>
        StorageNameInUse = 7004,
        InvalidEmbeddedStorageName = 8001,
        EmbeddedStorageAlreadyExists = 8002,
        EmbeddedStorageBuildFailed = 8003,
        /// <summary>Export/import/save/load refused because an agent is not strictly idle.</summary>
        StorageBusy = 8101,
        /// <summary>Named store exists under a different schema than the caller asked for.</summary>
        SchemaMismatch = 8102,
        /// <summary>Live-record cap reached; the overflowing insert was rejected.</summary>
        MaxRecordsExceeded = 8103,
        /// <summary>Unknown catalog schema name, or schema path failed to load.</summary>
        UnknownSchema = 8104,
        /// <summary>Snapshot format_version is newer, identifier is not TMKB, or the buffer failed verification.</summary>
        MutableKbSnapshotInvalid = 8105,
        /// <summary>Encoded snapshot would overflow MaxMutableKbSnapshotBytes (wire) or MaxGameMemorySnapshotBytes (file).</summary>
        StorageSnapshotTooLarge = 8106,
        /// <summary>LoadGameMemory embed_timing was Unspecified (omission is an error).</summary>
        InvalidEmbedTiming = 8107,
        /// <summary>Named store does not exist (get/query/export without ensure).</summary>
        UnknownMutableKb = 8108,
        /// <summary>SaveGameMemory / SaveDialog refused because server `saves.enabled` is false.</summary>
        SavesDisabled = 8109,
        /// <summary>Server failed to load the STT model requested by CreateVoiceInput.</summary>
        SttModelLoadFailed = 4100,
        /// <summary>Declared audio format is not supported (only 16-bit mono PCM is accepted).</summary>
        AudioFormatUnsupported = 4101,
        /// <summary>Request referenced a voice_input_id the server does not know.</summary>
        VoiceInputNotFound = 4102,
        /// <summary>BeginUtterance issued while an utterance is already active on this handle.</summary>
        UtteranceInProgress = 4103,
        /// <summary>End/Cancel issued with no utterance open.</summary>
        NoActiveUtterance = 4104,
        /// <summary>Utterance exceeded max_utterance_ms before being committed.</summary>
        UtteranceTimeout = 4105,
        /// <summary>A voice input supplied a hotwords lexicon for a model this session set HotwordBias.Off on. Refused rather than accepted-and-ignored: a lexicon that silently does nothing is the failure mode this whole feature spent months in.</summary>
        HotwordBiasDisabled = 4106,
        /// <summary>CreateVoiceInputRequest.hotwords_score is not a finite, non-negative number. It reaches the recognizer's beam search as a per-token bonus, so a NaN poisons every hypothesis comparison and an infinity makes the biased path win unconditionally — both surface as garbled transcripts with nothing in the log, which is why this is refused rather than clamped.</summary>
        InvalidHotwordsScore = 4107,
        /// <summary>Client-side only; the server never sends it. The voice input component found no usable capture device: none present, the configured device is not connected, or the capture stream failed to open. The message names the cause; the component's microphone status says the same.</summary>
        MicrophoneUnavailable = 4108,
        /// <summary>Client-side only; the server never sends it. The voice input component is (re)creating its voice input, because a microphone appeared or the device's sample rate changed, so this BeginUtterance was not started. Try again shortly.</summary>
        VoiceInputStarting = 4109,
    }
}
