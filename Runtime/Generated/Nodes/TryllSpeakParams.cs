// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/nodes.fbs  +  tools/codegen/templates/unity_node.cs.j2
// See: docs/dev/codegen.md
using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_TryllSpeakParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// Standalone TTS-only node.  Voices its `input`-resolved slot (any upstream node's output — Generate, CannedResponse, etc. — or `user_message` itself), segments it into sentences, and synthesizes each on a dedicated TTS strand, emitting PCM frames via OnTtsAudio callbacks.  Unlike GenerateAndSpeak it runs no LLM inference, so it can voice canned/scripted lines or any upstream-produced answer without paying for a generation pass. The voice belongs to the agent (CreateAgentRequest.voice); agent creation fails without one (3025).
    /// </summary>
    [Serializable]
    [AddTypeMenu("Speak")]
    public sealed class TryllSpeakParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Speak;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/speak.html";

        /// <summary>This node speaks through the agent's voice (TryllVoiceProfile).</summary>
        public override bool IsTtsConsumer => true;

        [TryllGroup("Input", First = true)]
        [Tooltip("Text to speak: the player's message or an earlier node's output (for example a Canned Response). Empty = the player's message.")]
        [TryllStructural]
        [TryllSlotInput]
        // Optional string — null means "use server default".
        public string Input;

        [TryllGroup("Behavior", First = true)]
        [Tooltip("Minimum length of a spoken chunk before a sentence end cuts it (CJK characters count double). Lower = speech starts sooner, more TTS calls.")]
        [Min(1.0f)]
        public int MinSentenceChars = 12;

        [TryllGroup("Behavior")]
        [Tooltip("Lets the first chunk end early, at a comma, dash or space once it is this long, so speech starts sooner. 0 = off; try 48.")]
        [Min(0.0f)]
        public int FirstChunkMaxChars = 0;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run after the text is spoken. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var inputOff = fbb.CreateString(Input ?? "");
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");

            Tryll.NodeParams.SpeakParams.StartSpeakParams(fbb);
            Tryll.NodeParams.SpeakParams.AddInput(fbb, inputOff);
            Tryll.NodeParams.SpeakParams.AddMinSentenceChars(fbb, MinSentenceChars);
            Tryll.NodeParams.SpeakParams.AddDefaultExit(fbb, default_exitOff);
            Tryll.NodeParams.SpeakParams.AddFirstChunkMaxChars(fbb, FirstChunkMaxChars);
            var offset = Tryll.NodeParams.SpeakParams.EndSpeakParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.SpeakParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllSpeakParams)MemberwiseClone();
            return c;
        }
    }
}
