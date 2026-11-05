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
    internal static class _FpCheck_TryllPauseParams {
        private static readonly System.Type _ = typeof(V_b5fd85e0d11e.Fingerprint);
    }
}

namespace Tryll.Client
{
    /// <summary>
    /// No-op node.  The executor pauses the turn after this node exits, giving the client a chance to inspect/mutate agent state (via ChangeAgentParam) before the graph continues. Resume with ResumeAgentRequest.
    /// </summary>
    [Serializable]
    [AddTypeMenu("Pause")]
    public sealed class TryllPauseParams : TryllNodeParamsBase    {
        public override TryllNodeType NodeType => TryllNodeType.Pause;

        /// <summary>Reference page of this node type, opened by the editor's help button.</summary>
        public override string DocsUrl => "https://docs.tryllengine.com/v0.10/reference/nodes/pause.html";

        [TryllGroup("Behavior", First = true)]
        [Tooltip("How long to wait for the game to resume, in ms. 0 = forever. Set a limit unless the game always resumes.")]
        [TryllStructural]
        [Min(0.0f)]
        public int PauseTimeoutMs = 0;

        [TryllGroup("Exits", First = true)]
        [Tooltip("Node to run when the game resumes without choosing one. Empty = end the turn.")]
        [TryllStructural]
        [TryllExit("default")]
        // Graph exit "default" — target node name; empty = END.
        public string DefaultExit;

        [TryllGroup("Exits")]
        [Tooltip("Node to run if the wait times out, such as a fallback line. Empty = end the turn as timed out. Unused when Pause Timeout Ms is 0.")]
        [TryllStructural]
        [TryllExit("timeout")]
        // Graph exit "timeout" — target node name; empty = END.
        public string TimeoutExit;


        public override (byte paramsType, int paramsOffset) Pack(FlatBufferBuilder fbb)
        {
            // Pack sub-tables and strings first (bottom-up).
            var default_exitOff = fbb.CreateString(DefaultExit ?? "");
            var timeout_exitOff = fbb.CreateString(TimeoutExit ?? "");

            Tryll.NodeParams.PauseParams.StartPauseParams(fbb);
            Tryll.NodeParams.PauseParams.AddDefaultExit(fbb, default_exitOff);
            Tryll.NodeParams.PauseParams.AddPauseTimeoutMs(fbb, PauseTimeoutMs);
            Tryll.NodeParams.PauseParams.AddTimeoutExit(fbb, timeout_exitOff);
            var offset = Tryll.NodeParams.PauseParams.EndPauseParams(fbb).Value;
            return ((byte)Tryll.NodeParams.NodeParams.PauseParams, offset);
        }

        public override TryllNodeParamsBase Clone()
        {
            var c = (TryllPauseParams)MemberwiseClone();
            return c;
        }
    }
}
