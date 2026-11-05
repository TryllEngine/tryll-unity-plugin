// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using ClientVisemeEvent = Tryll.Client.TryllVisemeEvent;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Events the socket worker still posts for the main thread.
    /// Request-reply completions are posted as <see cref="Completion"/> by the registry.
    /// Pushes the registry does not consume stay as their own types.
    /// </summary>
    internal enum TryllConnEventType : byte
    {
        Connected,
        ConnectFailed,
        Disconnected,

        NodeEvent,
        TranscriptUpdate,

        TtsAudioFormat,
        TtsAudio,
        TtsViseme,

        /// <summary>A posted completion. <see cref="TryllConnEvent.Completion"/> runs on the main thread.</summary>
        Completion,
    }

    /// <summary>
    /// Event pushed from the socket worker to the main-thread event queue.
    /// Fields are populated selectively per type.
    /// </summary>
    internal sealed class TryllConnEvent
    {
        public TryllConnEventType Type       = TryllConnEventType.ConnectFailed;
        public System.Action      Completion;
        public ulong              RequestId  = 0;
        public ulong              AgentId    = 0;
        public uint               Code       = 0;
        public string             Message;
        public string             Text;
        public List<KeyValuePair<string, string>> KvPairs;

        public string             ServerExePath;
        public string             ServerBuildConfig;
        public string             ServerMode;
        public string             ServerVersion;
        public string             ServerRunId;
        public bool               ServerManaged              = false;
        public uint               ServerIdleTimeoutSeconds   = 0;

        public ulong              VoiceInputId    = 0;
        public uint               AudioMsConsumed = 0;
        public byte               TranscriptKind  = 0;

        public ushort             TtsChannels       = 0;
        public ushort             TtsBitsPerSample  = 0;
        public short[]            PcmBuffer;
        public int                PcmLength         = 0;
        public ClientVisemeEvent[] VisemeEvents;
    }
}
