// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Tryll;

namespace Tryll.Client.Internal
{
    /// <summary>
    /// Maps a wire body to an observation scope for curated vs raw views.
    /// Unknown values fall into <see cref="TryllObservationScope.RawSession"/>.
    /// </summary>
    internal static class TryllObservationClassifier
    {
        public static TryllObservationScope Classify(TryllObservationNote note)
        {
            if (note == null || note.Connection != TryllConnectionObservation.None)
                return TryllObservationScope.RawSession;

            if (TryllObservationCurated.IsOverride(note.Body))
                return TryllObservationScope.CuratedAgent;

            var answersRequest = note.Direction == TryllObservationDirection.Inbound
                && note.RequestBody != MessageBody.NONE
                && note.RequestBody != note.Body;

            if (answersRequest)
            {
                // CreateAgent's request has no agent id; its error reply is still curated.
                // BeginUtterance is an override on the request only — its ack stays raw.
                if (note.RequestBody == MessageBody.CreateAgentRequest
                    && TryllObservationCurated.IsOverride(note.RequestBody))
                {
                    return TryllObservationScope.CuratedAgent;
                }

                if (HasAgentScope(note.RequestBody))
                    return TryllObservationScope.CuratedAgent;
            }
            else if (HasAgentScope(note.Body))
            {
                return TryllObservationScope.CuratedAgent;
            }

            if (IsMetadata(note.Body) || IsMetadata(note.RequestBody))
                return TryllObservationScope.MetadataOnly;

            return TryllObservationScope.RawSession;
        }

        static bool HasAgentScope(MessageBody body)
        {
            if (body == MessageBody.NONE)
                return false;
            if (!TryllMessageDescriptors.TryGet(body, out var descriptor))
                return false;
            return descriptor.ScopeKey == "agent_id";
        }

        static bool IsMetadata(MessageBody body)
            => body == MessageBody.AudioBuffer || body == MessageBody.SetInferenceThrottle;
    }
}
