// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll;

namespace Tryll.Client
{
    /// <summary>
    /// One viseme from a <c>TtsVisemeFrame</c>. Timing is turn-absolute at the
    /// TTS stream's sample rate. Duration is implied by the next event's offset
    /// (or the sentence end). <see cref="Weight"/> (0..1) follows how loud that stretch
    /// of speech is — scale the pose by it. Closures and rests are always 1.
    /// </summary>
    public readonly struct TryllVisemeEvent
    {
        public TryllVisemeEvent(uint sampleOffset, WireViseme viseme, float weight)
        {
            SampleOffset = sampleOffset;
            Viseme = viseme;
            Weight = weight;
        }

        public uint SampleOffset { get; }
        public WireViseme Viseme { get; }
        public float Weight { get; }
    }
}
