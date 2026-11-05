// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client
{
    /// <summary>
    /// Whether a human ever waits on an agent's turns. Purely a scheduling
    /// hint for the inference budget controller — it never changes node
    /// semantics. Set Background for world-logic/evaluator agents so their
    /// work is paced or deferred first under GPU pressure.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>Tryll.AgentWorkload</c> on the wire and Unreal's
    /// <c>ETryllWorkload</c> in <c>TryllThrottle.h</c>. Set at agent creation
    /// only — it is not a node parameter, so it cannot be changed with
    /// ChangeAgentParam; recreate the agent to change it.
    /// <para>
    /// The three consumer classes the server actually schedules against
    /// (interactive-visible, interactive-buffered, background) are derived
    /// server-side per turn from the graph, and are not declared here. This
    /// hint only forces the background case. See
    /// <c>docs/research/inference/phase-2-scheduler-policy-spec.md</c>.
    /// </para>
    /// </remarks>
    public enum TryllWorkload : byte { Interactive = 0, Background = 1 }
}
