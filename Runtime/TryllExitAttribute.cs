// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a serialized string field on a <see cref="TryllNodeParamsBase"/> subclass as
    /// a workflow-graph exit.  Emitted by codegen (driven by the <c>(exit)</c> attribute
    /// in <c>nodes.fbs</c>) on every per-node params class that declares routing.
    ///
    /// The field holds the *target node name* for the exit; an empty string means the
    /// graph terminates at this exit (the agent finishes the turn).
    ///
    /// Editor: a custom <see cref="UnityEditor.PropertyDrawer"/> looks up the enclosing
    /// <see cref="TryllGraphDescription"/> and renders the field as a dropdown of valid
    /// target node names (plus <c>&lt;End&gt;</c> for the empty / terminating choice).
    ///
    /// Wire: codegen on the server side reads this same field at <c>CreateAgent</c> time
    /// to build the runtime route table; exits are <c>(structural)</c> so they cannot be
    /// changed via <c>ChangeAgentParam</c> after the agent is constructed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllExitAttribute : PropertyAttribute
    {
        /// <summary>Wire-side exit name (the prefix of <c>&lt;exit_name&gt;_exit</c> in nodes.fbs).</summary>
        public string ExitName { get; }

        public TryllExitAttribute(string exitName)
        {
            ExitName = exitName ?? string.Empty;
        }
    }
}
