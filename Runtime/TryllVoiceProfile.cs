// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// A character's voice as an asset (protocol v27): which TTS model speaks, the base
    /// identity (speaker, reference clip, description), speed and language, whether a
    /// viseme track is emitted, and named delivery modes.
    ///
    /// Assign it on <see cref="TryllAgentComponent"/> (or set
    /// <see cref="TryllAgentComponent.Voice"/> from code before the agent is created).
    /// Every speaking node of that agent uses this voice; a workflow with a Speak or
    /// GenerateAndSpeak node cannot create an agent without one
    /// (<see cref="TryllErrorCode.AgentVoiceRequired"/>). Because the voice lives here and
    /// not in the workflow, one workflow asset can drive many characters.
    ///
    /// Create via <b>Create ▸ Tryll ▸ Voice Profile</b>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewVoice.voice", menuName = "Tryll/Voice Profile", order = 101)]
    [HelpURL(TryllDocs.TryllVoiceProfile)]
    public sealed class TryllVoiceProfile : ScriptableObject
    {
        [Tooltip("The voice sent to the server. See TryllVoiceProfileData.")]
        public TryllVoiceProfileData Data = new TryllVoiceProfileData();

        /// <summary>Wire form of <see cref="Data"/>.</summary>
        public Tryll.VoiceProfileT ToWire() => (Data ?? new TryllVoiceProfileData()).ToWire();
    }
}
