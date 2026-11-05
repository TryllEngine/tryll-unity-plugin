// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using UnityEngine;

namespace Tryll.Client
{
    /// <summary>
    /// Marks a serialized string field on a <see cref="TryllNodeParamsBase"/> subclass as
    /// naming this node's effective output slot (e.g. <c>GenerateParams.OutputName</c>,
    /// <c>InstructionParams.OutputName</c>). Emitted by codegen (driven by the
    /// <c>(tryll_slot_name)</c> attribute in <c>nodes.fbs</c>). Empty value = the node's
    /// own name.
    ///
    /// Editor: a custom <see cref="UnityEditor.PropertyDrawer"/> renders a rename-commit
    /// widget (<see cref="UnityEditor.EditorGUI.DelayedTextField"/>): on commit it enforces
    /// the identifier grammar and reserved-name (<c>user_message</c>) rule, uniquifies
    /// against existing node names/slots, and propagates the old slot name into every
    /// <c>input</c> selector and <c>{{slot.&lt;old&gt;}}</c> template tag in the graph.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TryllSlotNameAttribute : PropertyAttribute
    {
    }
}
