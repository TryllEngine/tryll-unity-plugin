// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Curated Agent Log messages that scope inference does not explain.
    /// <see cref="TryllMessageDescriptors"/> supplies name, direction, and
    /// <c>agent_id</c> / <c>voice_input_id</c>. CreateAgent has no agent id on
    /// the request, and BeginUtterance is voice-scoped but still curated.
    /// </summary>
    internal static class TryllObservationCurated
    {
        public static bool IsOverride(MessageBody body)
        {
            switch (body)
            {
            case MessageBody.CreateAgentRequest:
            case MessageBody.CreateAgentResponse:
            case MessageBody.BeginUtteranceRequest:
                return true;
            default:
                return false;
            }
        }
    }
}
