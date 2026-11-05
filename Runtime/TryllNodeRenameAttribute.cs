// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks <see cref="TryllNodeDescription.Name"/> as a rename-commit field.
    ///
    /// Editor: a custom <see cref="UnityEditor.PropertyDrawer"/> renders
    /// <see cref="UnityEditor.EditorGUI.DelayedTextField"/> (commits on Enter/focus-loss,
    /// not per-keystroke) and, on commit, enforces the identifier grammar and reserved-name
    /// (<c>user_message</c>) rule, uniquifies against existing node names, and propagates the
    /// old name into every exit field, <c>StartNode</c>, and — when this node's effective
    /// output slot equals its own (unoverridden) name — every <c>input</c> selector and
    /// <c>{{slot.&lt;old&gt;}}</c> template tag in the graph.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllNodeRenameAttribute : PropertyAttribute
    {
    }
}
