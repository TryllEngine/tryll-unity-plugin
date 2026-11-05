// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;

namespace Tryll.Client
{
    /// <summary>
    /// Status of a model as reported by the server.
    /// Mirrors the FlatBuffers ModelStatus enum in messages.fbs — ordinal values must stay in sync.
    /// </summary>
    public enum TryllModelStatus : byte
    {
        /// <summary>Known in catalog but not on disk and not being downloaded.</summary>
        Absent      = 0,
        /// <summary>User-provided path; file on disk.</summary>
        Local       = 1,
        /// <summary>Transfer currently in progress.</summary>
        Downloading = 2,
        /// <summary>Currently loaded in memory.</summary>
        Loaded      = 3,
        /// <summary>On disk from HuggingFace download; can be loaded.</summary>
        Downloaded  = 4,
        /// <summary>Parent files are present but a required shared asset is absent or invalid.</summary>
        Incomplete  = 5,
    }

    /// <summary>Editor-facing status of one shared-asset dependency of a parent model.</summary>
    [Serializable]
    public struct TryllModelDependency
    {
        public string           Id;
        public string           Role;
        public TryllModelStatus Status;
        public long             SizeBytes;
        public string           Folder;
        public string           LocalPath;
        public string           FailureReason;
    }

    /// <summary>Catalog-advertised speaker/description preset.</summary>
    [Serializable]
    public struct TryllTtsVoicePreset
    {
        public string Id;
        public string Label;
        public string Description;
    }

    /// <summary>Model-native inline control token declared for authoring.</summary>
    [Serializable]
    public struct TryllTtsInlineTag
    {
        public string Token;
        public string Kind;
        public string Verification;
    }

    /// <summary>Structured TTS capabilities advertised by the catalog.</summary>
    [Serializable]
    public struct TryllTtsCapabilities
    {
        public bool                    VoiceDescription;
        public bool                    DeliveryInstruction;
        public bool                    ReferenceVoice;
        public bool                    Speed;
        public bool                    LanguagePerCall;
        /// <summary>Chatterbox-style exaggeration knob (voice profile Expressiveness).</summary>
        public bool                    Expressiveness;
        /// <summary>Number of speaker ids; the voice profile's SpeakerId is 0..NumSpeakers-1. 0 = undeclared.</summary>
        public int                     NumSpeakers;
        public TryllTtsVoicePreset[]   VoicePresets;
        public TryllTtsInlineTag[]     InlineTags;
    }

    /// <summary>Recommended sampling parameters, mirroring the server's default_sampling.</summary>
    [Serializable]
    public struct TryllSamplingParams
    {
        public float Temperature;
        public float TopP;
        public int   TopK;
        public float MinP;
        public int   MaxTokens;
        public uint  Seed;
        public float RepeatPenalty;
        public float PresencePenalty;
        public float FrequencyPenalty;
    }

    /// <summary>
    /// Summary information for one model returned by ListModels.
    /// Mirrors FTryllModelInfo from the Unreal plugin.
    /// </summary>
    [Serializable]
    public struct TryllModelInfo
    {
        /// <summary>Catalog name of the model (matches the name in models.json).</summary>
        public string          Name;

        /// <summary>Current status on the server.</summary>
        public TryllModelStatus Status;

        /// <summary>HuggingFace repo id (e.g. "Qwen/Qwen2.5-0.5B-Instruct-GGUF") or empty for local-only models.</summary>
        public string          HuggingFaceRepo;

        /// <summary>On-disk size in bytes when the model has been downloaded. Zero if Status == Absent.</summary>
        public long            SizeBytes;

        /// <summary>Model type: "language" | "embedding" | "stt" | "tts" | "vad".</summary>
        public string          ModelType;

        /// <summary>Resolved inference engine, e.g. "llama-cpp" | "sherpa-onnx".</summary>
        public string          Engine;

        /// <summary>Intended audience tier: "public" | "experimental" | "internal" | "".</summary>
        public string          Audience;

        /// <summary>
        /// Languages this model can handle, as lowercase BCP-47 primary language subtags
        /// ("en", "de", …). Read this to build a language menu instead of
        /// hardcoding one — it is how you discover that voice cloning is
        /// English-only, or which STT models cover the locale you want.
        ///
        /// Empty means <b>undeclared</b>, not "supports nothing": treat it as
        /// "no capability information" and keep offering the model.
        /// </summary>
        public string[]        Languages;

        /// <summary>Tool-call prompt/parse format: "chatml" | "llama3" | "mistral" | "generic" | "".</summary>
        public string          ToolCallFormat;

        /// <summary>
        /// Advisory catalog-declared tool-calling capability for language models:
        /// "supported" | "unsupported". Empty for non-language models (and when
        /// talking to older servers). The authoritative check happens
        /// server-side at agent creation from the model's own chat template.
        /// </summary>
        public string          ToolCallSupport;

        /// <summary>Variant context window in tokens; 0 means the engine default.</summary>
        public int             ContextSize;

        /// <summary>Files downloaded/resolved for the configured engine's variant.</summary>
        public string[]        Files;

        /// <summary>Resolved on-disk directory for the model; empty when not on disk.</summary>
        public string          LocalPath;

        /// <summary>True when the catalog supplies explicit default sampling parameters.</summary>
        public bool            HasDefaultSampling;

        /// <summary>Recommended sampling parameters; valid only when HasDefaultSampling.</summary>
        public TryllSamplingParams DefaultSampling;

        /// <summary>Shared-asset dependencies of this model. Empty when the variant has none.</summary>
        public TryllModelDependency[] Dependencies;

        /// <summary>True when the catalog advertised structured TTS capabilities.</summary>
        public bool HasTtsCapabilities;

        /// <summary>Catalog TTS capabilities; valid only when HasTtsCapabilities.</summary>
        public TryllTtsCapabilities TtsCapabilities;
    }
}
