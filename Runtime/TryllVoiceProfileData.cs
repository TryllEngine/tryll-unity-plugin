// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// When the server prepares a voice's reference clips. Mirrors the FlatBuffers
    /// <c>VoicePreparation</c> enum in <c>tryll/schema/voice.fbs</c>; ordinals must stay in sync.
    /// </summary>
    public enum TryllVoicePreparation : byte
    {
        /// <summary>Prepare every clip (base and all modes) when the voice is set.</summary>
        Eager = 0,
        /// <summary>Prepare each clip on first use. For experiments.</summary>
        Lazy  = 1,
    }

    /// <summary>
    /// One named delivery (mood or manner of speaking) of a voice. Hand-mirrors the
    /// FlatBuffers <c>DeliveryMode</c> table in <c>tryll/schema/voice.fbs</c>.
    ///
    /// What each field does depends on the TTS family. A field the loaded model cannot
    /// use is rejected with <see cref="TryllErrorCode.InvalidVoiceProfile"/> when the
    /// voice is set. A mode with no fields set behaves like the built-in <c>neutral</c>
    /// mode, so every profile can define the same mode names.
    /// </summary>
    [Serializable]
    public sealed class TryllDeliveryMode
    {
        [Tooltip("Mode name used by SetDeliveryModeAsync: lowercase letters, digits and '_' " +
                 "(1-32 chars), unique in the profile. 'neutral' is built in and may not be defined.")]
        public string Name = string.Empty;

        [Tooltip("Clone families (Pocket TTS, Chatterbox, Qwen3-TTS Base): a reference clip of " +
                 "this character speaking in this mood, relative to the storage root. Replaces " +
                 "the profile's Reference Voice while the mode is active.")]
        [TryllFilePicker]
        public string ReferenceVoice = string.Empty;

        [Tooltip("Maya1: sustained delivery clause appended to the voice description. A known " +
                 "keyword (angry, sad, whisper, ...) is rewritten into its tuned clause; anything " +
                 "else is appended as written.")]
        [TextArea(2, 6)]
        public string Instruction = string.Empty;

        [Tooltip("Chatterbox: exaggeration for this mode, 0.0-2.0. Negative = use the profile's Expressiveness.")]
        [Range(-1f, 2f)]
        public float Expressiveness = -1f;

        [Tooltip("Multiplies the profile speed while the mode is active, 0.5-2.0. Must stay 1.0 " +
                 "on models without speed control.")]
        [Range(0.5f, 2f)]
        public float SpeedScale = 1f;

        /// <summary>Deep copy.</summary>
        public TryllDeliveryMode Clone() => (TryllDeliveryMode)MemberwiseClone();

        /// <summary>Wire (FlatBuffers object API) form.</summary>
        public Tryll.DeliveryModeT ToWire() => new Tryll.DeliveryModeT
        {
            Name           = Name ?? string.Empty,
            ReferenceVoice = NullIfEmpty(ReferenceVoice),
            Instruction    = NullIfEmpty(Instruction),
            Expressiveness = Expressiveness,
            SpeedScale     = SpeedScale,
        };

        /// <summary>Build from the wire form (tests, tooling).</summary>
        public static TryllDeliveryMode FromWire(Tryll.DeliveryModeT wire)
        {
            if (wire == null) return null;
            return new TryllDeliveryMode
            {
                Name           = wire.Name ?? string.Empty,
                ReferenceVoice = wire.ReferenceVoice ?? string.Empty,
                Instruction    = wire.Instruction ?? string.Empty,
                Expressiveness = wire.Expressiveness,
                SpeedScale     = wire.SpeedScale,
            };
        }

        internal static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
    }

    /// <summary>
    /// The voice of one agent (protocol v27): one TTS model, a base identity, the
    /// speech-pipeline settings and named delivery modes. Every speaking node of the
    /// agent (Speak, GenerateAndSpeak) uses it; nodes hold no voice data.
    ///
    /// Hand-mirrors the FlatBuffers <c>VoiceProfile</c> table in
    /// <c>tryll/schema/voice.fbs</c>. Usually authored as a <see cref="TryllVoiceProfile"/>
    /// asset and assigned on <see cref="TryllAgentComponent.Voice"/>; can also be built in
    /// code and passed to <see cref="TryllClient.RequestCreateAgentAsync"/> or
    /// <see cref="TryllAgent.SetVoiceAsync(TryllVoiceProfileData)"/>.
    /// </summary>
    [Serializable]
    public sealed class TryllVoiceProfileData
    {
        /// <summary>The always-available mode: base identity with no delivery applied.</summary>
        public const string NeutralMode = "neutral";

        [Tooltip("TTS model catalog name. Required: there is no default model.")]
        [TryllModelType("tts")]
        public string TtsModelName = string.Empty;

        [Tooltip("Speaker / style index for multi-speaker models (Supertonic 3: 0-9, Orpheus and " +
                 "Maya1: preset index). Must be below the model's speaker count.")]
        [Min(0)]
        public int SpeakerId;

        [Tooltip("Reference clip for clone families (Pocket TTS, Chatterbox, Qwen3-TTS Base), " +
                 "relative to the storage root; locale-aware (voices/loc/<locale>/). Empty = the " +
                 "model's default voice (Qwen3-TTS Base requires a clip).")]
        [TryllFilePicker]
        public string ReferenceVoice = string.Empty;

        [Tooltip("Maya1: natural-language identity of the voice. Overrides Speaker Id.")]
        [TextArea(2, 6)]
        public string VoiceDescription = string.Empty;

        [Tooltip("Speech-rate multiplier. 1.0 = native. Must stay 1.0 on models without speed " +
                 "control (Orpheus, Maya1, Qwen3-TTS).")]
        [Range(0.1f, 3f)]
        public float Speed = 1f;

        [Tooltip("Language for multilingual models that pick it per call. Empty = session locale, " +
                 "then the model default. Rejected on models with a fixed language.")]
        public string Lang = string.Empty;

        [Tooltip("Chatterbox exaggeration used by 'neutral' and by modes that do not set their " +
                 "own, 0.0-2.0. Ignored by other families.")]
        [Range(0f, 2f)]
        public float Expressiveness = 0.5f;

        [Tooltip("When reference clips are prepared. Eager = every clip when the voice is set; " +
                 "Lazy = each clip on first use.")]
        public TryllVoicePreparation Prepare = TryllVoicePreparation.Eager;

        [Tooltip("Emit a viseme track for every sentence this agent speaks, from any speaking " +
                 "node. Turn on for lip-synced characters (TryllLipSync).")]
        public bool EmitVisemes;

        [Tooltip("Named delivery modes. Game code switches between them with SetDeliveryModeAsync.")]
        public List<TryllDeliveryMode> DeliveryModes = new List<TryllDeliveryMode>();

        [Tooltip("Mode active after the voice is set. Empty = 'neutral'.")]
        public string DefaultDeliveryMode = string.Empty;

        /// <summary>Deep copy (delivery modes included).</summary>
        public TryllVoiceProfileData Clone()
        {
            var c = (TryllVoiceProfileData)MemberwiseClone();
            c.DeliveryModes = new List<TryllDeliveryMode>();
            if (DeliveryModes != null)
                foreach (var m in DeliveryModes)
                    if (m != null) c.DeliveryModes.Add(m.Clone());
            return c;
        }

        /// <summary>Wire (FlatBuffers object API) form, as sent in CreateAgent / SetAgentVoice.</summary>
        public Tryll.VoiceProfileT ToWire()
        {
            var modes = new List<Tryll.DeliveryModeT>();
            if (DeliveryModes != null)
                foreach (var m in DeliveryModes)
                    if (m != null) modes.Add(m.ToWire());
            return new Tryll.VoiceProfileT
            {
                TtsModelName        = TtsModelName ?? string.Empty,
                SpeakerId           = SpeakerId,
                ReferenceVoice      = TryllDeliveryMode.NullIfEmpty(ReferenceVoice),
                VoiceDescription    = TryllDeliveryMode.NullIfEmpty(VoiceDescription),
                Speed               = Speed,
                Lang                = TryllDeliveryMode.NullIfEmpty(Lang),
                Expressiveness      = Expressiveness,
                Prepare             = (Tryll.VoicePreparation)(sbyte)Prepare,
                EmitVisemes         = EmitVisemes,
                DeliveryModes       = modes,
                DefaultDeliveryMode = TryllDeliveryMode.NullIfEmpty(DefaultDeliveryMode),
            };
        }

        /// <summary>Build from the wire form (tests, tooling).</summary>
        public static TryllVoiceProfileData FromWire(Tryll.VoiceProfileT wire)
        {
            if (wire == null) return null;
            var d = new TryllVoiceProfileData
            {
                TtsModelName        = wire.TtsModelName ?? string.Empty,
                SpeakerId           = wire.SpeakerId,
                ReferenceVoice      = wire.ReferenceVoice ?? string.Empty,
                VoiceDescription    = wire.VoiceDescription ?? string.Empty,
                Speed               = wire.Speed,
                Lang                = wire.Lang ?? string.Empty,
                Expressiveness      = wire.Expressiveness,
                Prepare             = (TryllVoicePreparation)(byte)wire.Prepare,
                EmitVisemes         = wire.EmitVisemes,
                DefaultDeliveryMode = wire.DefaultDeliveryMode ?? string.Empty,
            };
            if (wire.DeliveryModes != null)
                foreach (var m in wire.DeliveryModes)
                    if (m != null) d.DeliveryModes.Add(TryllDeliveryMode.FromWire(m));
            return d;
        }

        /// <summary>
        /// Every clip path the profile names (base and modes), non-empty only. Used by
        /// editor validation and tooling.
        /// </summary>
        public IEnumerable<(string owner, string path)> ReferenceClips()
        {
            if (!string.IsNullOrEmpty(ReferenceVoice))
                yield return ("ReferenceVoice", ReferenceVoice);
            if (DeliveryModes == null) yield break;
            foreach (var m in DeliveryModes)
                if (m != null && !string.IsNullOrEmpty(m.ReferenceVoice))
                    yield return ($"mode '{m.Name}' ReferenceVoice", m.ReferenceVoice);
        }

        /// <summary>
        /// Whether <paramref name="name"/> is a legal mode name: <c>[a-z0-9_]{1,32}</c>.
        /// (Not checked against <see cref="NeutralMode"/>; callers do that separately.)
        /// </summary>
        public static bool IsValidModeName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 32) return false;
            foreach (char ch in name)
                if (!((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_'))
                    return false;
            return true;
        }
    }
}
