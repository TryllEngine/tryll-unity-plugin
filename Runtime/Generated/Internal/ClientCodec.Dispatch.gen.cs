// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

// AUTO-GENERATED — do not edit by hand.
// Source of truth: tryll/schema/messages.fbs  +  tools/codegen/templates/protocol_codec_csharp_dispatch.cs.j2
// See: docs/dev/codegen.md

using System;
using Google.FlatBuffers;
using Tryll;

namespace Tryll.Generated.Internal {
    internal static class _FpCheck_ClientCodec_Dispatch_gen {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client.Internal
{
    internal static partial class ClientCodec
    {
        /// <summary>
        /// Decode a raw server payload byte array into a <see cref="TryllConnEvent"/>.
        /// Returns null when the payload is malformed or carries an unexpected body type.
        /// Per-message field mapping lives in the handwritten partial class.
        /// </summary>
        public static TryllConnEvent Decode(byte[] payload)
        {
            try
            {
                var bb  = new ByteBuffer(payload);
                var msg = Message.GetRootAsMessage(bb);
                return msg.BodyType switch
                {
                    MessageBody.ConnectionReady => OnIncomingConnectionReady(msg),
                    MessageBody.NodeEvent => OnIncomingNodeEvent(msg),
                    MessageBody.WireTranscriptUpdate => OnIncomingWireTranscriptUpdate(msg),
                    MessageBody.TtsAudioFormatFrame => OnIncomingTtsAudioFormatFrame(msg),
                    MessageBody.TtsAudioFrame => OnIncomingTtsAudioFrame(msg),
                    MessageBody.TtsVisemeFrame => OnIncomingTtsVisemeFrame(msg),
                    _ => UnknownBody(msg.BodyType),
                };
            }
            catch (Exception ex)
            {
                return DecodeException(ex);
            }
        }
    }
}
